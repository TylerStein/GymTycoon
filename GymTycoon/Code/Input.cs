using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace GymTycoon.Code
{
    public class BinaryAction
    {
        public string Name { get; internal set; }
        public bool IsDown { get; internal set; }
        public bool Pressed { get; internal set; }
        public bool Released { get; internal set; }
        public float DownTime { get; internal set; }
        public bool Enabled { get; internal set; }

        internal List<Keys> Keys = [];
        internal bool leftMouse = false;
        internal bool rightMouse = false;

        public bool ConsumePressed()
        {
            if (Pressed)
            {
                Pressed = false;
                Released = true;
                IsDown = false;
                return true;
            }

            return false;
        }

        public void UpdateFromState(KeyboardState keyboardState, MouseState mouseState, float deltaTime)
        {
            foreach (var key in Keys)
            {
                if (keyboardState.IsKeyDown(key))
                {
                    if (!IsDown)
                    {
                        Pressed = true;
                    }
                    else
                    {
                        Pressed = false;
                        DownTime += deltaTime;
                    }

                    IsDown = true;
                    return;
                }
                else
                {
                    if (IsDown)
                    {
                        Released = true;
                    }
                    else
                    {
                        Released = false;
                        DownTime = 0f;
                        return;
                    }

                    IsDown = false;
                }
            }

            if (leftMouse)
            {
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (!IsDown)
                    {
                        Pressed = true;
                    }
                    else
                    {
                        Pressed = false;
                        DownTime += deltaTime;
                    }

                    IsDown = true;
                    return;
                }
                else
                {
                    if (IsDown)
                    {
                        Released = true;
                    }
                    else
                    {
                        Released = false;
                        DownTime = 0f;
                        return;
                    }

                    IsDown = false;
                }
            }

            if (rightMouse)
            {
                if (mouseState.RightButton == ButtonState.Pressed)
                {
                    if (!IsDown)
                    {
                        Pressed = true;
                    }
                    else
                    {
                        Pressed = false;
                        DownTime += deltaTime;
                    }

                    IsDown = true;
                    return;
                }
                else
                {
                    if (IsDown)
                    {
                        Released = true;
                    }
                    else
                    {
                        Released = false;
                        DownTime = 0f;
                        return;
                    }

                    IsDown = false;
                }
            }
        }
    }

    public class LinearAction
    {
        public string Name { get; internal set; }
        public int Value { get; internal set; } = 0;
        public int Delta { get; internal set; } = 0;
        public bool Enabled { get; internal set; }

        internal List<Keys> PositiveKeys = [];
        internal List<Keys> NegativeKeys = [];
        internal bool scrollWheel = false;
        internal bool invertScrollWheel = false;
        internal int lastScrollWheelValue = 0;

        public void UpdateFromState(KeyboardState keyboardState, MouseState mouseState, float deltaTime)
        {
            bool setValue = false;
            foreach (var key in PositiveKeys)
            {
                if (keyboardState.IsKeyDown(key))
                {
                    if (Value < 1)
                    {
                        Delta = 1;
                    } else
                    {
                        Delta = 0;
                    }

                    setValue = true;
                    Value = 1;
                    break;
                }
            }

            foreach (var key in NegativeKeys)
            {
                if (keyboardState.IsKeyDown(key))
                {
                    if (Value > -1)
                    {
                        Delta = -1;
                    } else
                    {
                        Delta = 0;
                    }

                    setValue = true;
                    Value = -1;
                    break;
                }
            }

            if (scrollWheel)
            {
                setValue = true;
                int scrollWheelValue = mouseState.ScrollWheelValue;
                int scrollDelta = lastScrollWheelValue - scrollWheelValue;
                if (scrollDelta != 0)
                {
                    int nextValue = invertScrollWheel ? scrollDelta : -scrollDelta;
                    Delta = nextValue - Value;
                    Value = nextValue;
                } else
                {
                    Delta = 0;
                }
            }

            if (!setValue)
            {
                Value = 0;
            }
        }
    }

    public enum MouseButton { Left, Right };

    public class Input
    {
        readonly Dictionary<string, BinaryAction> BinaryActions = [];
        readonly Dictionary<string, LinearAction> LinearActions = [];
        public Point MousePosition;
        public bool LockMouse = false;
        public bool MouseIsOnScreen = false;

        public BinaryAction GetBinaryAction(string name)
        {
            BinaryActions.TryGetValue(name, out BinaryAction action);
            return action;
        }

        public LinearAction GetLinearAction(string name)
        {
            LinearActions.TryGetValue(name, out LinearAction action);
            return action;
        }

        public void RegisterBinaryActionKey(string name, Keys key)
        {
            if (!BinaryActions.ContainsKey(name))
            {
                BinaryActions.Add(name, new BinaryAction());
            }

            BinaryActions[name].Keys.Add(key);
        }

        public void RegisterBinaryActionMouseButton(string name, MouseButton button)
        {
            if (!BinaryActions.ContainsKey(name))
            {
                BinaryActions.Add(name, new BinaryAction());
            }

            switch (button)
            {
                case MouseButton.Left:
                    BinaryActions[name].leftMouse = true;
                    return;
                case MouseButton.Right:
                    BinaryActions[name].rightMouse = true;
                    return;

            }
        }

        public void RegisterLinearActionKey(string name, Keys positive, Keys negative)
        {
            if (!LinearActions.ContainsKey(name))
            {
                LinearActions.Add(name, new LinearAction());
            }

            LinearActions[name].PositiveKeys.Add(positive);
            LinearActions[name].NegativeKeys.Add(negative);
        }

        public void RegisterLinearActionScrollWheel(string name, bool invert)
        {
            if (!LinearActions.ContainsKey(name))
            {
                LinearActions.Add(name, new LinearAction());
            }

            LinearActions[name].scrollWheel = true;
            LinearActions[name].invertScrollWheel = invert;
        }

        public void SetBinaryActionEnabled(string name, bool enabled)
        {
            if (BinaryActions.ContainsKey(name))
            {
                BinaryActions[name].Enabled = enabled;
            }
        }
        public void SetLinearActionEnabled(string name, bool enabled)
        {
            if (LinearActions.ContainsKey(name))
            {
                LinearActions[name].Enabled = enabled;
            }
        }

        public void Update(GameTime gameTime, GameWindow window, Rectangle viewport)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState(window);

            foreach (var kvp in BinaryActions)
            {
                kvp.Value.UpdateFromState(keyboardState, mouseState, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            foreach (var kvp in LinearActions)
            {
                kvp.Value.UpdateFromState(keyboardState, mouseState, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            if (!LockMouse)
            {
                MousePosition = mouseState.Position;
            }

            MouseIsOnScreen = viewport.Contains(MousePosition);

        }

        public void DrawImGui()
        {
            ImGui.Begin("[DEBUG] Input");

            ImGui.Checkbox("Lock Mouse", ref LockMouse);
            ImGui.LabelText("MousePos", $"{MousePosition.X}, {MousePosition.Y}");
            ImGui.LabelText("MouseOnScreen", MouseIsOnScreen.ToString());

            ImGui.Text("BinaryActions");
            foreach(var kvp in BinaryActions)
            {
                ImGui.LabelText(kvp.Key, kvp.Value.IsDown.ToString());
            }

            ImGui.Separator();

            ImGui.Text("LinearActions");
            foreach (var kvp in LinearActions)
            {
                ImGui.LabelText(kvp.Key, kvp.Value.Value.ToString());
            }

            ImGui.End();
        }
    }
}
