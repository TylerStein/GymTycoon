using GymTycoon.Code.Data;
using ImGuiNET;
using System;

namespace GymTycoon.Code.Cursors
{
    internal class BuildCursor : PlaceCursor
    {
        private int _buildCost;

        public BuildCursor(DynamicObject objectType, int buildCost) : base(objectType)
        {
            _buildCost = buildCost;
        }

        public override void Place()
        {
            if (GameInstance.Instance.Economy.PlayerMoney < _buildCost)
            {
                Console.WriteLine("Not enough money!");
                return;
            }

            base.Place();

            if (DidPlace)
            {
                GameInstance.Instance.Economy.Transaction(-_buildCost, TransactionType.Build);
            }
        }

        public override void DrawImGui()
        {
            ImGui.Text("== BUILD MODE ==");
            base.DrawImGui();
        }
    }
}
