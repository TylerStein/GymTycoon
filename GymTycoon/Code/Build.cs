using GymTycoon.Code.Common;
using GymTycoon.Code.Cursors;
using GymTycoon.Code.Data;
using ImGuiNET;

namespace GymTycoon.Code
{
    public class Build
    {
        public void Update()
        {
        }

        public void DrawImGui()
        {
            ImGui.Begin("Build");

            foreach (ScopedName name in GameInstance.Instance.Resources.DynamicObjectTypes)
            {
                if (ImGui.Button($"Build {name.GetName()}"))
                {
                    DynamicObject type = GameInstance.Instance.Resources.GetDynamicObjectType(name);
                    GameInstance.Instance.Cursor.SetCursor(new BuildCursor(type, type.BuildCost));
                }
            }


            if (ImGui.CollapsingHeader("Debug"))
            {
                foreach (ScopedName name in GameInstance.Instance.Resources.DynamicObjectTypes)
                {
                    if (ImGui.Button(name.GetFullName()))
                    {
                        DynamicObject type = GameInstance.Instance.Resources.GetDynamicObjectType(name);
                        GameInstance.Instance.Cursor.SetCursor(new PlaceCursor(type));
                    }
                }
            }

            ImGui.End();
        }

    }
}
