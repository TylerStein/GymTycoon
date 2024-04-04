using GymTycoon.Code.Common;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code.DebugTools
{
    public static class DebugNav
    {
        public static Point3? DebugNavStart = null;
        public static Point3? DebugNavEnd = null;
        public static List<Point3> DebugNavPath = [];
        public static bool DebugNavEnabled = false;

        public static void Update()
        {
            if (DebugNavEnabled)
            {
                if (GameInstance.Instance.Input.MouseIsOnScreen && GameInstance.Instance.Input.GetBinaryAction(GameInstance.SymbolInputSelect).ConsumePressed())
                {
                    int position = GameInstance.Instance.World.GetIndex(GameInstance.Instance.WorldRenderer.GetWorldCursor());
                    TileType tile = GameInstance.Instance.World.GetTile(position);
                    if (tile.HasProperty(TileProperties.Navigable))
                    {
                        if (!DebugNavStart.HasValue)
                        {
                            DebugNavStart = GameInstance.Instance.World.GetPosition(position);
                        }
                        else if (!DebugNavEnd.HasValue)
                        {
                            DebugNavEnd = GameInstance.Instance.World.GetPosition(position);
                            Stack<Point3> tmp = [];
                            if (Navigation.Pathfinding(GameInstance.Instance.World, DebugNavStart.Value, DebugNavEnd.Value, tmp))
                            {
                                // successful pathfinding
                                DebugNavPath = tmp.ToList();
                            }
                            else
                            {
                                // failed pathfinding
                                DebugNavStart = null;
                                DebugNavEnd = null;
                                DebugNavPath.Clear();
                            }
                        }
                        else
                        {
                            DebugNavStart = GameInstance.Instance.World.GetPosition(position);
                            DebugNavEnd = null;
                            DebugNavPath.Clear();
                        }

                    }

                    return;
                }
            }
        }

        public static void DrawImGui()
        {
            ImGui.Begin("[DEBUG] Navigation");
            if (ImGui.Checkbox("Nav Test", ref DebugNavEnabled))
            {
                DebugNavStart = null;
                DebugNavEnd = null;
                DebugNavPath.Clear();
            }
            ImGui.End();
        }
    }
}
