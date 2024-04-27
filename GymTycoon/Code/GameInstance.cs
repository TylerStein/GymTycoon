﻿using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using GymTycoon.Code.Cursors;
using GymTycoon.Code.Data;
using GymTycoon.Code.DebugTools;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.ImGuiNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace GymTycoon.Code
{
    public class GameInstance : Game
    {
        private static GameInstance _instance;
        public static GameInstance Instance => _instance;

        public const string SymbolInputDevMenu = "DevMenu";
        public const string SymbolInputHorizontal = "Horizontal";
        public const string SymbolInputVertical = "Vertical";
        public const string SymbolInputDepth = "Depth";
        public const string SymbolInputZoom = "Zoom";
        public const string SymbolInputSelect = "Select";
        public const string SymbolInputExit = "Exit";
        public const string SymbolInputDebug = "Debug";
        public const string SymbolInputRotate = "Rotate";
        public const string SymbolInputDelete = "Delete";
        public const string SymbolInputMultiPlace = "MultiPlace";
        public const string SymbolInputAltSelect = "AltSelect";

        public World World;
        public Resources Resources;
        public Input Input;
        public WorldRenderer WorldRenderer;
        public GraphicsDeviceManager Graphics;
        public ImGuiRenderer ImGuiRenderer;
        public Navigation Navigation;
        public Instances Instances;
        public Economy Economy;
        public Director Director;
        public Time Time;
        public Cursor Cursor;
        public Build Build;
        public Stats Stats;
        public Scenario Scenario;
        public NeedsManager NeedsManager;

        private readonly int _cameraMoveSpeed = 10;
        private readonly float _cameraZoomSpeed = 0.1f;

        private float _tickRate = 0.3f;
        private float _tickCounter = 0f;
        private bool _worldDirty = false;

        public float DeltaTime = 0f;


        public GameInstance(Scenario scenario, int viewportWidth = 1920, int viewportHeight = 1080)
        {
            Content.RootDirectory = "Content";
            Graphics = new(this);
            Graphics.PreferredBackBufferWidth = viewportWidth;
            Graphics.PreferredBackBufferHeight = viewportHeight;

            World = new World();
            Resources = new Resources();
            Input = new Input();
            WorldRenderer = new WorldRenderer();
            Navigation = new Navigation();
            Instances = new Instances(Resources);
            Economy = new Economy();
            Time = new Time();
            Director = new Director();
            Cursor = new Cursor();
            Build = new Build();
            Stats = new Stats();
            NeedsManager = new NeedsManager();
            Scenario = scenario;

            Economy.PlayerMoney = scenario.StartingCapital;
            Economy.InfiniteMoney = scenario.InfiniteMoney;
            Economy.InitialBoost = scenario.InitialBoost;
            Economy.InitialBoostDecayRate = scenario.InitialBoostDecayRate;
            Economy.MarketingDecayRate = scenario.MarketingDecayRate;
            Economy.NewEquipmentValueDecayRate = scenario.NewEquipmentDecayRate;

            NeedsManager.AddNeedDefs(NeedsManager.DefaultNeedDefs);

            Time.SetDate(scenario.StartDate, 0);
        }

        protected override void Initialize()
        {
            _instance = this;

            InputFilters inputFilter = InputFilters.MouseOnScreen | InputFilters.MouseNotOnGui | InputFilters.KeyboardNotOnGui;
            Input.RegisterBinaryActionKey(SymbolInputDevMenu, Keys.OemTilde, InputFilters.None);
            Input.RegisterLinearActionKey(SymbolInputHorizontal, Keys.A, Keys.D, InputFilters.KeyboardNotOnGui);
            Input.RegisterLinearActionKey(SymbolInputHorizontal, Keys.Left, Keys.Right, InputFilters.KeyboardNotOnGui);
            Input.RegisterLinearActionKey(SymbolInputVertical, Keys.W, Keys.S, InputFilters.KeyboardNotOnGui);
            Input.RegisterLinearActionKey(SymbolInputVertical, Keys.Up, Keys.Down, InputFilters.KeyboardNotOnGui);
            Input.RegisterLinearActionKey(SymbolInputDepth, Keys.Q, Keys.E, inputFilter);
            Input.RegisterLinearActionKey(SymbolInputZoom, Keys.Z, Keys.X, inputFilter);
            Input.RegisterLinearActionScrollWheel(SymbolInputZoom, false, inputFilter);
            Input.RegisterBinaryActionMouseButton(SymbolInputSelect, MouseButton.Left, inputFilter);
            Input.RegisterBinaryActionKey(SymbolInputExit, Keys.Escape, inputFilter);
            Input.RegisterBinaryActionKey(SymbolInputDebug, Keys.OemTilde, inputFilter);
            Input.RegisterBinaryActionKey(SymbolInputRotate, Keys.R, inputFilter);
            Input.RegisterBinaryActionKey(SymbolInputDelete, Keys.Delete, inputFilter);
            Input.RegisterBinaryActionKey(SymbolInputMultiPlace, Keys.LeftShift, inputFilter);
            Input.RegisterBinaryActionMouseButton(SymbolInputAltSelect, MouseButton.Right, inputFilter);

            IsMouseVisible = false;
            Window.AllowUserResizing = true;
            WorldRenderer.Initialize(this);
            WorldRenderer.CenterCamera();

            ImGuiRenderer = new ImGuiRenderer(this);
            ImGuiRenderer.RebuildFontAtlas();

            Director.Initialize();

            Graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            Resources.LoadContent(Content);
            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            if (Input.GetBinaryAction(SymbolInputExit).Pressed)
            {
                Exit();
                return;
            }

            if (Input.GetBinaryAction(SymbolInputDebug).Pressed)
            {
                Input.LockMouse = !Input.LockMouse;
            }

            IsMouseVisible = Input.LockMouse || Input.MouseIsOnGui;
            DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds * Time.GetTimeScale();

            bool tick = false;
            _tickCounter += DeltaTime;
            if (_tickCounter > _tickRate)
            {
                tick = true;
                _tickCounter = 0f;
            }

            Time.Update(DeltaTime);
            Economy.Update(DeltaTime);
            Director.Update(DeltaTime, tick);
            DebugNav.Update(); // must update before cursor to consume inputs
            Cursor.Update();
            Stats.Update();
            Scenario.Update();

            Point3 worldSize = World.GetSize();
            int depthChange = Input.GetLinearAction(SymbolInputDepth).Delta;
            if (depthChange != 0)
            {
                WorldRenderer.ChangeViewLayer(-depthChange, 0, worldSize.Z);
            }

            WorldRenderer.SetViewportSize(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Input.Update(gameTime, Window, GraphicsDevice.Viewport.Bounds);

            Vector2 move = new(
                Input.GetLinearAction(SymbolInputHorizontal).Value * (float)gameTime.ElapsedGameTime.TotalSeconds,
                Input.GetLinearAction(SymbolInputVertical).Value * (float)gameTime.ElapsedGameTime.TotalSeconds
            );

            if (move.LengthSquared() > 0f) {
                move.Normalize();
                move *= _cameraMoveSpeed;
                WorldRenderer.MoveCamera(new Point((int)MathF.Floor(move.X), (int)MathF.Floor(move.Y)));
            }

            int zoomDelta = Input.GetLinearAction(SymbolInputZoom).Delta;
            if (zoomDelta != 0)
            {
                WorldRenderer.ZoomCamera((int)MathF.Round(zoomDelta * _cameraZoomSpeed), Input.MousePosition);
            }

            if (tick || _worldDirty)
            {
                World.UpdateAdvertisedBehaviors();
            }

            _worldDirty = false;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Gray);

            WorldRenderer.Draw(DeltaTime, this);

            ImGuiRenderer.BeginLayout(gameTime);

            // Gameplay
            Time.DrawImGui();
            Economy.DrawImGui();
            Build.DrawImGui();
            Cursor.DrawImGui();
            Stats.DrawImGui();

            // Debug only
            WorldRenderer.DrawImGui();
            Resources.DrawImGui();
            World.DrawImGui();
            Input.DrawImGui();
            Director.DrawImGui();
            DebugNav.DrawImGui();
            Scenario.DrawImGui();

            ImGuiRenderer.EndLayout();

            base.Draw(gameTime);
        }
    }
}
