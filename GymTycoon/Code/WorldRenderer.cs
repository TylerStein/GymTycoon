﻿using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using GymTycoon.Code.Cursors;
using GymTycoon.Code.Data;
using GymTycoon.Code.DebugTools;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GymTycoon.Code
{
    public class WorldRenderer
    {
        // 0.0000 to 0.001 TILE
        // 0.0011 to 0.002 DYNAMIC OBJECT BACKGROUND
        // 0.0021 to 0.003 GUEST
        // 0.0031 to 0.004 DYNAMIC OBJECT FOREGROUND
        // 0.0041 +        RESERVED

        public const int SpriteSize = 32;

        private const float _depthMinTile                       = 0.0000f;
        private const float _depthMaxTile                       = 0.0010f;
        private const float _depthMinDynamicObjectBackground    = 0.0011f;
        private const float _depthMaxDynamicObjectBackground    = 0.0020f;
        private const float _depthMinGuest                      = 0.0021f;
        private const float _depthMaxGuest                      = 0.0030f;
        private const float _depthMinDynamicObjectForeground    = 0.0031f;
        private const float _depthMaxDynamicObjectForeground    = 0.0040f;
        private const float _depthBurst                         = 0.0041f;
        private const float _depthGhost                         = 0.0042f;
        private const float _depthBeautyOverlay                 = 0.0043f;
        private const float _depthZoneOverlay                   = 0.0044f;
        private const float _depthDebugDraw                     = 0.0045f;

        private readonly Color _validGhostColor = new(0.6f, 0.6f, 0.6f);
        private readonly Color _invalidGhostColor = new(0.6f, 0.2f, 0.2f);
        private readonly Color _semiTransparent = new(0.25f, 0.25f, 0.25f, 0.1f);

        private readonly Dictionary<DynamicObjectCategory, Color> _zoneColors = new()
        {
            { DynamicObjectCategory.Toilet, Color.Cyan },
            { DynamicObjectCategory.Reception, Color.Cornsilk }
        };

        private SpriteBatch _spriteBatch;

        private Point _viewportSize = new(1, 1);

        private int _drawTileSize = SpriteSize; // aka zoom
        private Point _worldTileSize = new(SpriteSize, SpriteSize / 2);

        private Point _camera = new(0, 0);
        private int _viewLayer = 1;

        // FX SPRITES
        public List<Burst> Bursts = [];

        public bool DrawBuildCursor = false;

        // DEBUG FLAGS
        private bool _viewDebugTiles = false;
        private bool _enableHiding = true;
        private bool _enableTransparency = true;
        private bool _drawHitboxes = false;
        private bool _drawBeauty = false;
        private bool _drawSocial = false;
        private bool _drawZones = false;
        private bool _drawBlockedSpaces = false;
        private bool _drawGuestSlots = false;

        // DEBUG INFO
        private Point3 _worldCursor = Point3.Zero;
        private int _worldCursorIndex = 0;
        private float _cursorDepth = 0f;

        private int _drawTileCount = 0;
        private int _drawObjectCount = 0;
        private int _drawBurstCount = 0;

        public void Initialize(Game game)
        {
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);
        }

        public void CenterCamera()
        {
            Point3 size = GameInstance.Instance.World.GetSize();
            _camera = new Point((int)(size.X * _drawTileSize / 1.5f), size.Y * _drawTileSize / 8);
        }

        public Point GetViewportSize()
        {
            return _viewportSize;
        }

        public Point3 GetWorldCursor()
        {
            return _worldCursor;
        }

        public float GetWorldCursorDepth()
        {
            return _cursorDepth;
        }

        public int GetDrawTileSize()
        {
            return _drawTileSize;
        }

        public bool IsTileCulled(Point3 worldPosition, Point screenPosition)
        {
            if (worldPosition.Z - _viewLayer > 1) return true;
            return (screenPosition.X < -_drawTileSize
                || screenPosition.X > _viewportSize.X + _drawTileSize
                || screenPosition.Y < -_drawTileSize
                || screenPosition.Y > _viewportSize.Y + _drawTileSize);
        }

        public bool IsTileVisible(TileType tileType)
        {
            if (!tileType.HasProperty(TileProperties.Visible))
            {
                return false;
            }

            if (tileType.HasProperty(TileProperties.Editor) && !_viewDebugTiles)
            {
                return false;
            }

            return true;
        }

        public Point WorldToScreen(Point3 worldPosition)
        {
            return IsoGrid.GridToScreen(worldPosition, _camera, _drawTileSize);
        }

        public Point WorldToScreenNoCamera(Vector3 worldPosition)
        {
            return IsoGrid.GridToScreen(worldPosition, Point.Zero, _drawTileSize);
        }

        public Point WorldToScreen(Vector3 worldPosition)
        {
            return IsoGrid.GridToScreen(worldPosition, _camera, _drawTileSize);
        }

        public Point WorldToScreen(Point3 worldPosition, Vector2 offset)
        {
            return IsoGrid.GridToScreen(worldPosition, _camera, _drawTileSize);
        }

        public Point3 ScreenToWorld(Point screen)
        {
            return IsoGrid.ScreenToGrid(screen, _camera, _viewLayer, _drawTileSize);
        }

        protected Color GetDrawColor(Point3 worldPosition, Color baseColor)
        {
            if (worldPosition.Z - _viewLayer == 1)
            {
                return new Color(baseColor.ToVector4() * 0.25f);
            }
            return baseColor;
        }

        public void SetViewportSize(int width, int height)
        {
            _viewportSize.X = width;
            _viewportSize.Y = height;
        }

        public void MoveCamera(Point offset)
        {
            _camera += offset;
        }

        public void ZoomCamera(int delta, Point screenTarget)
        {
            Point3 worldPositionBeforeZoom = ScreenToWorld(screenTarget);

            _drawTileSize = Math.Clamp(_drawTileSize + delta, 16, 128);

            _worldTileSize.X = _drawTileSize;
            _worldTileSize.Y = _drawTileSize / 2;

            Point screenPositionAfterZoom = WorldToScreen(worldPositionBeforeZoom);
            _camera += screenTarget - screenPositionAfterZoom;
        }

        public void ChangeViewLayer(int delta, int min, int max)
        {
            _viewLayer += delta;
            if (_viewLayer < min) _viewLayer = min;
            else if (_viewLayer > max) _viewLayer = max;
        }

        public void AddBurst(Point3 sourcePosition, EBurstType type, float life = 2.5f)
        {
            Burst burst = new Burst(type, sourcePosition, life);
            Bursts.Add(burst);
        }

        private void DrawTile(Point3 worldPosition, Texture2D texture, float depth, int sheetOffsetX, int sheetOffsetY, Color baseColor)
        {
            Point screen = WorldToScreen(worldPosition);
            if (IsTileCulled(worldPosition, screen))
            {
                return;
            }

            DrawScreen(screen, texture, depth, sheetOffsetX, sheetOffsetY, GetDrawColor(worldPosition, baseColor));
            _drawTileCount++;
        }

        private void DrawGhost(SpriteInstance sprite, Point3 worldPosition, Direction direction, float depth, bool valid)
        {
            Point screen = WorldToScreen(worldPosition);
            if (IsTileCulled(worldPosition, screen))
            {
                return;
            }

            Rectangle rect;
            Rectangle destinationRectangle = new(screen.X, screen.Y, _drawTileSize, _drawTileSize);

            Texture2D tex = sprite.GetTexture(0, direction, out rect);
            _spriteBatch.Draw(tex, destinationRectangle, rect, GetDrawColor(worldPosition, valid ? _validGhostColor : _invalidGhostColor), 0f, Vector2.Zero, SpriteEffects.None, depth);
            _drawObjectCount++;
        }

        private void DrawObject(DynamicObjectInstance obj, Point3 worldPosition, Direction direction, float depth, bool isSelected)
        {
            Point screen = WorldToScreen(worldPosition);
            if (IsTileCulled(worldPosition, screen))
            {
                return;
            }

            Rectangle rect;
            Rectangle destinationRectangle = new(screen.X, screen.Y, _drawTileSize, _drawTileSize);
            SpriteInstance sprite = obj.GetActiveSprite();
            int numLayers = sprite.GetNumLayers();
            float layerStep = (_depthMaxDynamicObjectBackground - _depthMinDynamicObjectBackground) / numLayers;
            for (int i = 0; i < numLayers; i++)
            {
                if (sprite.LayerIsHidden(i))
                {
                    continue;
                }

                Texture2D tex = sprite.GetTexture(i, direction, out rect);
                Color color = isSelected ? Color.BlueViolet : Color.White;
                _spriteBatch.Draw(tex, destinationRectangle, rect, GetDrawColor(worldPosition, color), 0f, Vector2.Zero, SpriteEffects.None, depth + (layerStep * i));
                _drawObjectCount++;
            }
        }

        private void DrawScreen(Point screen, Texture2D texture, float depth, int sheetOffsetX, int sheetOffsetY, Color color, SpriteEffects spriteEffects = SpriteEffects.None)
        {
            Rectangle destinationRectangle = new(screen.X, screen.Y, _drawTileSize, _drawTileSize);
            Rectangle sourceRectangle = new Rectangle(sheetOffsetX * SpriteSize, sheetOffsetY * SpriteSize, SpriteSize, SpriteSize);
            _spriteBatch.Draw(texture, destinationRectangle, sourceRectangle, color, 0f, Vector2.Zero, spriteEffects, depth);
        }

        private void DrawGuest(Guest guest, Point3 gridPosition, Vector3 worldPosition, float depth)
        {
            Point screen = WorldToScreen(worldPosition);
            Rectangle destinationRectangle = new(screen.X, screen.Y, _drawTileSize, _drawTileSize);
            Rectangle sourceRectangle;
            SpriteInstance sprite = guest.Sprite;
            int numLayers = sprite.GetNumLayers();
            float layerStep = (_depthMaxGuest - _depthMinGuest) / numLayers;
            for (int i = 0; i < numLayers; i++)
            {
                if (sprite.LayerIsHidden(i))
                {
                    continue;
                }

                Texture2D tex = sprite.GetTexture(i, guest.Direction, out sourceRectangle);
                _spriteBatch.Draw(tex, destinationRectangle, sourceRectangle, GetDrawColor(gridPosition, guest.OffscreenGuest.Tint), 0f, Vector2.Zero, SpriteEffects.None, depth + (layerStep * i));
                _drawObjectCount++;
            }

        }

        private bool IsOccluded(GameInstance game, Point3 worldPosition)
        {
            if (_enableHiding && worldPosition.Z < _viewLayer)
            {
                for (int z = 1; z < _viewLayer - worldPosition.Z + 1; z++)
                {
                    Point3 inFrontPosition = worldPosition + new Point3(1, 1, z);
                    int inFrontIndex = game.World.GetIndex(inFrontPosition);
                    if (inFrontIndex >= 0 && inFrontIndex < game.World.GetTileCount())
                    {
                        TileType inFrontTile = game.World.GetTile(inFrontIndex);
                        if (!IsTileVisible(inFrontTile))
                        {
                            continue;
                        }

                        if (inFrontTile.HasProperty(TileProperties.Editor))
                        {
                            continue;
                        }

                        if (inFrontTile.HasProperty(TileProperties.Transparency) && _enableTransparency)
                        {
                            continue;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public static float GetDepth(Point3 worldPosition, Point3 worldSize)
        {
            //int weight = width + height + 1;
            //return worldPosition.X + worldPosition.Y + worldPosition.Z * weight;
            return 1f - IsoGrid.GetDepth(worldPosition, worldSize, worldSize);
        }

        public static float GetDepth(Vector3 worldPosition, Point3 worldSize)
        {
            //int weight = width + height + 1;
            //return worldPosition.X + worldPosition.Y + worldPosition.Z * weight;
            return 1f - IsoGrid.GetDepth(worldPosition, worldSize, worldSize);
        }

        public void Draw(float deltaTime, GameInstance game)
        {
            _drawTileCount = 0;
            _drawObjectCount = 0;
            _drawBurstCount = 0;

            Point3 worldSize = game.World.GetSize();

            _spriteBatch.Begin(SpriteSortMode.FrontToBack, null, SamplerState.PointClamp);

            GameInstance.Instance.Performance.Start("Tiles");
            for (int i = 0; i < game.World.GetTileCount(); i++)
            {
                TileType tileType = game.World.GetTile(i);
                bool isVisible = IsTileVisible(tileType);
                Point3 worldPosition = game.World.GetPosition(i);
                float tileDepth = GetDepth(worldPosition, worldSize);

                if (_drawBlockedSpaces && worldPosition.Z == _viewLayer)
                {
                    if (!Navigation.IsTileNavigable(game.World, i))
                    {
                        DrawTile(worldPosition, game.Resources.GetDebugCube(), tileDepth + _depthDebugDraw, 0, 0, Color.Red);
                    }
                }

                if (!isVisible || IsOccluded(game, worldPosition))
                {
                    continue;
                }


                Texture2D tileTexture = game.Resources.GetTileTexture(tileType.ID);
                DrawTile(worldPosition, tileTexture, tileDepth, 0, 0, Color.White);
            }
            GameInstance.Instance.Performance.Stop();


            if (_drawBeauty)
            {
                for (int i = 0; i < game.World.GetTileCount(); i++)
                {
                    Point3 worldPosition = game.World.GetPosition(i);
                    if (IsOccluded(game, worldPosition))
                    {
                        continue;
                    }

                    float beautyPct = game.World.BeautyLayer.GetBeautyPercentAt(i);
                    float tileDepth = GetDepth(worldPosition, worldSize);

                    float hue = MathHelper.Lerp(0.5f, 1f, beautyPct);
                    Color color = new HSVColor(hue, 1f, 1f).ToColor(0.24f);
                    DrawTile(worldPosition, game.Resources.GetDebugCube(), tileDepth + _depthBeautyOverlay, 0, 0, color); 
                }
            }
            else if (_drawSocial)
            {
                for (int i = 0; i < game.World.GetTileCount(); i++)
                {
                    Point3 worldPosition = game.World.GetPosition(i);
                    if (IsOccluded(game, worldPosition))
                    {
                        continue;
                    }

                    float socialPct = game.World.SocialLayer.GetSocialPercentat(i);
                    float tileDepth = GetDepth(worldPosition, worldSize);

                    float hue = MathHelper.Lerp(0.5f, 1f, socialPct);
                    Color color = new HSVColor(hue, 1f, 1f).ToColor(0.24f);
                    DrawTile(worldPosition, game.Resources.GetDebugCube(), tileDepth + _depthZoneOverlay, 0, 0, color);
                }
            }
            else if (_drawZones)
            {
                foreach (var kvp in game.Zones)
                {
                    DynamicObjectCategory category = kvp.Key;
                    Color color;
                    if (!_zoneColors.TryGetValue(category, out color))
                    {
                        color = Color.Orange;
                    }

                    color.A = (byte)100;
                    foreach (var index in kvp.Value.Data)
                    {
                        Point3 worldPosition = game.World.GetPosition(index);
                        float tileDepth = GetDepth(worldPosition, worldSize);

                        DrawTile(worldPosition, game.Resources.GetDebugCube(), tileDepth + _depthDebugDraw, 0, 0, color);
                    }
                }
            }

            _worldCursor = ScreenToWorld(game.Input.MousePosition);
            _worldCursor.Z = _viewLayer;
            _worldCursor.X -= 1;
            _worldCursorIndex = GameInstance.Instance.World.GetIndex(_worldCursor);
            _cursorDepth = GetDepth(_worldCursor, worldSize);

            PlaceCursor placeCursor;
            bool isPlaceCursor = game.Cursor.TryGetCursor(out placeCursor);

            if (isPlaceCursor)
            {
                DrawGhost(placeCursor.PlaceObjectSpriteInstance, _worldCursor, placeCursor.PlaceDirection, _cursorDepth + _depthGhost, true); // TODO: Update placement validity (maybe on new tile? debounce?)
            }
            else
            {
                DrawTile(_worldCursor, game.Resources.GetDebugCube(), _cursorDepth + _depthDebugDraw, 0, 0, Color.White);
            }

            SelectCursor selectCursor;
            bool isSelectCursor = game.Cursor.TryGetCursor(out selectCursor);

            GameInstance.Instance.Performance.Start("DynamicObjects");
            foreach (var dynamicObject in game.World.GetAllDynamicObjects())
            {
                if (dynamicObject.Held)
                {
                    // not visible
                    continue;
                }

                Point3 dynamicObjectPosition = game.World.GetPosition(dynamicObject.WorldPosition);
                float dynamicObjectDepth = GetDepth(dynamicObjectPosition, worldSize);
                bool isSelected = isSelectCursor && selectCursor.IsSelected(dynamicObject);
                DrawObject(dynamicObject, dynamicObjectPosition, dynamicObject.Direction, dynamicObjectDepth + _depthMinDynamicObjectBackground, isSelected);

                if (_drawGuestSlots && dynamicObjectPosition.Z == _viewLayer && !dynamicObject.Racked)
                {
                    foreach (var slotPosition in dynamicObject.GetGuestSlots(dynamicObject.Direction))
                    {
                        DrawTile(dynamicObjectPosition + slotPosition, game.Resources.GetDebugCube(), dynamicObjectDepth + _depthDebugDraw, 0, 0, Color.Green);
                    }

                    foreach (var slotPosition in dynamicObject.GetStaffSlots(dynamicObject.Direction))
                    {
                        DrawTile(dynamicObjectPosition + slotPosition, game.Resources.GetDebugCube(), dynamicObjectDepth + _depthDebugDraw, 0, 0, Color.Yellow);
                    }
                }
            }
            GameInstance.Instance.Performance.Stop();

            GameInstance.Instance.Performance.Start("Guests");
            for (int i = 0; i < game.Director.ActiveGuests.Count; i++)
            {
                Guest guest = game.Director.ActiveGuests[i];
                int guestIndex = guest.WorldPosition;
                Point3 guestGridPos = game.World.GetPosition(guestIndex);
                Vector3 guestPosition = guestGridPos.ToVector3() + guest.TileOffset;
                float guestDepth = GetDepth(guestPosition, worldSize);
                DrawGuest(guest, guestGridPos, guestPosition, guestDepth + _depthMinGuest);

                if (guest.FollowCam)
                {
                    Point3 size = GameInstance.Instance.World.GetSize();
                    _camera = WorldToScreenNoCamera(-guestPosition);
                    _camera += new Point(_viewportSize.X / 2, _viewportSize.Y / 2);
                }
            }
            GameInstance.Instance.Performance.Stop();

            for (int i = Bursts.Count - 1; i >= 0; i--)
            {
                Point3 burstPos = Bursts[i].WorldPos;
                int yOff = (int)(Bursts[i].Offset * (float)_drawTileSize);
                Point screen = WorldToScreen(burstPos) - new Point(0, _drawTileSize + yOff);
                float burstDepth = GetDepth(burstPos, worldSize);
                Texture2D burstTexture = game.Resources.GetBurstTexture(Bursts[i].BurstType);
                Color burstColor = new Color(Bursts[i].Alpha, Bursts[i].Alpha, Bursts[i].Alpha, Bursts[i].Alpha);
                DrawScreen(screen, burstTexture, burstDepth + _depthBurst, 0, 0, GetDrawColor(burstPos, burstColor));
                _drawBurstCount++;

                Bursts[i].Update(deltaTime);
                if (Bursts[i].PendingRemoval)
                {
                    Bursts.RemoveAt(i);
                }

            }

            _spriteBatch.End();

            _spriteBatch.Begin();
            if (_drawHitboxes && isSelectCursor)
            {
                foreach (Hitbox hitbox in selectCursor.GetHitboxes())
                {
                    _spriteBatch.DrawRectangle(hitbox.Rect, Color.GreenYellow);
                }
            }

            if (DebugNav.DebugNavEnabled)
            {
                if (DebugNav.DebugNavPath.Count > 0)
                {
                    for (int i = 0; i < DebugNav.DebugNavPath.Count; i++)
                    {
                        DrawTile(DebugNav.DebugNavPath[i], game.Resources.GetDebugCube(), _cursorDepth + _depthDebugDraw, 0, 0, Color.White);
                    }
                }
                else
                {
                    if (DebugNav.DebugNavStart.HasValue)
                    {
                        DrawTile(DebugNav.DebugNavStart.Value, game.Resources.GetDebugCube(), _cursorDepth + _depthDebugDraw, 0, 0, Color.White);
                    }

                    if (DebugNav.DebugNavEnd.HasValue)
                    {
                        DrawTile(DebugNav.DebugNavStart.Value, game.Resources.GetDebugCube(), _cursorDepth + _depthDebugDraw, 0, 0, Color.White);
                    }
                }
            }

            // _spriteBatch.DrawCircle(game.Input.MousePosition.ToVector2(), 16f, 16, Color.Red);
            _spriteBatch.End();
        }

        public void DrawImGui()
        {
            ImGui.Begin("[DEBUG] Renderer");

            ImGui.Text($"ZOOM ({_drawTileSize})");
            ImGui.Text($"LAYER ({_viewLayer})");
            ImGui.Text($"CAMERA ({_camera.X}, {_camera.Y})");
            ImGui.Text($"TILES ({_drawTileCount})");
            ImGui.Text($"BURSTS ({_drawBurstCount})");
            ImGui.Text($"CURSOR P ({_worldCursor.X}, {_worldCursor.Y}, {_worldCursor.Z})");
            ImGui.Text($"CURSOR I ({_worldCursorIndex})");

            if (ImGui.CollapsingHeader("Tile Properties"))
            {
                TileType tile = GameInstance.Instance.World.GetTile(_worldCursorIndex);
                if (tile.HasProperty(TileProperties.Spawn)) ImGui.Text("Spawn");
                if (tile.HasProperty(TileProperties.Transparency)) ImGui.Text("Transparency");
                if (tile.HasProperty(TileProperties.Visible)) ImGui.Text("Visible");
                if (tile.HasProperty(TileProperties.Navigable)) ImGui.Text("Navigable");
            }

            ImGui.Text($"DEPTH ({_cursorDepth})");
            ImGui.Text($"BEAUTY ({GameInstance.Instance.World.GetBeautyAt(_worldCursorIndex)}");
            ImGui.Separator();
            if (ImGui.Button("Reset Camera"))
            {
                CenterCamera();
            }
            ImGui.Checkbox("Debug Tiles", ref _viewDebugTiles);
            ImGui.Checkbox("Hiding", ref _enableHiding);
            ImGui.Checkbox("Transparency", ref _enableTransparency);
            ImGui.Checkbox("Draw Hitboxes", ref _drawHitboxes);

            if (!_drawSocial && !_drawZones)
            {
                ImGui.Checkbox("Draw Beauty", ref _drawBeauty);
            }

            if (!_drawBeauty && !_drawZones)
            {
                ImGui.Checkbox("Draw Social", ref _drawSocial);
            }

            if (!_drawBeauty && !_drawSocial)
            {
                ImGui.Checkbox("Draw Zones", ref _drawZones);
            }

            ImGui.Checkbox("Draw Blocked Spaces", ref _drawBlockedSpaces);
            ImGui.Checkbox("Draw Guest Slots", ref _drawGuestSlots);

            ImGui.End();
        }
    }
}
