using GymTycoon.Code.Data;
using ImGuiNET;
using System;

namespace GymTycoon.Code.Cursors
{
    internal class PlaceCursor : ICursor
    {
        public DynamicObject PlaceObjectType;
        public SpriteInstance PlaceObjectSpriteInstance;
        public Direction PlaceDirection = Direction.SOUTH;
        public bool DidPlace = false;

        public PlaceCursor(DynamicObject objectType) {
            PlaceObjectType = objectType;
        }

        public PlaceCursor(PlaceCursor other)
        {
            PlaceObjectType = other.PlaceObjectType;
            PlaceDirection = other.PlaceDirection;
        }

        public virtual void Initialize()
        {
            if (PlaceObjectSpriteInstance == null) {
                PlaceObjectSpriteInstance = GameInstance.Instance.Instances.InstantiateSprite(PlaceObjectType.SpriteAliases["Default"]);
            }
        }

        public virtual void Destroy()
        {
            PlaceObjectType = null;
            PlaceObjectSpriteInstance = null;
        }

        public virtual void Update()
        {
            DidPlace = false;
            GameInstance.Instance.WorldRenderer.DrawBuildCursor = PlaceObjectType != null;

            if (GameInstance.Instance.Input.GetBinaryAction(GameInstance.SymbolInputRotate).Pressed)
            {
                Rotate();
            }

            if (GameInstance.Instance.Input.MouseIsOnScreen)
            {
                if (GameInstance.Instance.Input.GetBinaryAction(GameInstance.SymbolInputSelect).Pressed)
                {
                    // select
                    Place();
                }
                else if (GameInstance.Instance.Input.GetBinaryAction(GameInstance.SymbolInputAltSelect).Pressed)
                {
                    GameInstance.Instance.Cursor.SetCursor(new SelectCursor());
                }
            }
        }

        public virtual void Place()
        {
            if (PlaceObjectType != null)
            {
                int position = GameInstance.Instance.World.GetIndex(GameInstance.Instance.WorldRenderer.GetWorldCursor());
                if (GameInstance.Instance.World.CanPlaceDynamicObject(PlaceObjectType, position, PlaceDirection))
                {
                    DynamicObjectInstance obj = GameInstance.Instance.Instances.InstantiateDynamicObject(PlaceObjectType, position, PlaceDirection, GameInstance.Instance.World);
                    GameInstance.Instance.World.AddDynamicObject(obj);
                    DidPlace = true;

                    GameInstance.Instance.UpdateZoneForObjectType(obj.Data.Category);

                    if (GameInstance.Instance.Input.GetBinaryAction(GameInstance.SymbolInputMultiPlace).IsDown)
                    {
                        GameInstance.Instance.Cursor.SetCursor(new PlaceCursor(this));
                        return;
                    }

                    GameInstance.Instance.Cursor.SetCursor(new SelectCursor(obj));
                    return;
                }
                else
                {
                    Console.WriteLine("Object placement blocked!");
                }
            }
        }

        public void Rotate()
        {
            PlaceDirection++;
            if ((int)PlaceDirection > 3)
            {
                PlaceDirection = 0;
            }
        }

        public virtual void DrawImGui()
        {
            ImGui.Text($"Direction = {PlaceDirection}");
            if (PlaceObjectType != null)
            {
                ImGui.Text($"Placing: {PlaceObjectType.GetFullName()}");
            }

            if (ImGui.SmallButton("Rotate"))
            {
                Rotate();
            }

        }
    }
}
