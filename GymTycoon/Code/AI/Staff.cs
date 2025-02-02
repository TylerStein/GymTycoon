using GymTycoon.Code.Common;
using ImGuiNET;
using System;
using System.Drawing;
using System.Numerics;

namespace GymTycoon.Code.AI
{
    public enum EStaffType
    {
        RECEPTION = 0,
        MAINTINANCE = 1,
        JANITOR = 2
    }

    public class Staff : Agent
    {
        private EStaffType _staffType;

        public string Name;
        public Color Tint;
        public DateTime HireDate = new DateTime();
        public int Wage = 1;

        public EStaffType StaffType => _staffType;

        public Staff(int worldIndex, SpriteInstance sprite) : base(worldIndex, sprite)
        {
            // TODO: Need offscreen??
            OffscreenAgent = new NullOffscreenAgent();
        }


        public override void AddExperience(Tag key, int increment)
        {
            // TODO
        }

        public override void DrawImGui()
        {
            ImGui.Begin("Staff");

            // TODO

            ImGui.End();
        }

        public override int GetExperience(Tag key)
        {
            // TODO
            return 0;
        }
    }
}
