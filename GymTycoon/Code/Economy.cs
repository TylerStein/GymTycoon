using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GymTycoon.Code
{
    public enum WealthTier
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Premium = 4
    }

    public enum TransactionType
    {
        None = 0,
        Membership = 1,
        Refund = 2,
        Build = 3,
    }

    public class Economy
    {
        public float[] MembershipPriceTiers = [0, 10, 20, 30, 40];

        public int PlayerMoney = 0;
        public bool InfiniteMoney = false;
        public int GymMembershipCost = 5;

        public float InitialBoost = 0f;
        public float InitialBoostDecayRate = 1f;

        public float MarketingBoost = 0f;
        public float NewEquipmentValueBoost = 0f;

        public float MarketingDecayRate = 1f;
        public float NewEquipmentValueDecayRate = 1f;

        public int MembershipPrice = 10;

        public int GymRating = 0;

        private float _avgNeeds = 0f;

        public void Update(float deltaTime)
        {
            if (GameInstance.Instance.Director.ActiveGuests.Count > 0)
            {
                _avgNeeds = 0f;
                foreach (Guest guest in GameInstance.Instance.Director.ActiveGuests)
                {
                    _avgNeeds += guest.AverageNeeds;
                }

                _avgNeeds /= GameInstance.Instance.Director.ActiveGuests.Count;
            }

            Decay(ref MarketingBoost, MarketingDecayRate, deltaTime);
            Decay(ref NewEquipmentValueBoost, NewEquipmentValueDecayRate, deltaTime);
            Decay(ref InitialBoost, InitialBoostDecayRate, deltaTime);

            GymRating = (int)MathF.Round(
                (_avgNeeds * 0.4f) +
                (MarketingBoost * 0.2f) +
                (NewEquipmentValueBoost * 0.2f) +
                (InitialBoost * 0.2f)
            );
        }

        public void Decay(ref float value, float decayRate, float deltaTime)
        {
            if (value > 0f)
            {
                value -= deltaTime * decayRate;
                if (value < 0f)
                {
                    value = 0f;
                }
            }
        }

        public void AddMarketingBoost(float value)
        {
            MarketingBoost += value;
        }

        public void AddNewEquipmentBoost(float equipmentCost)
        {
            NewEquipmentValueBoost += equipmentCost;
        }

        public void Transaction(int value, TransactionType source)
        {
            PlayerMoney += value;
        }

        public void DrawImGui()
        {
            ImGui.Begin("Economy");

            if (InfiniteMoney)
            {
                ImGui.Text($"$Inf");
            } else
            {
                ImGui.Text($"${PlayerMoney}");
            }

            ImGui.Text($"R{GymRating}");

            ImGui.Text("Membership Cost");
            if (ImGui.ArrowButton("MembershipCostDecrement", ImGuiDir.Left))
            {
                if (MembershipPrice > 10)
                {
                    MembershipPrice -= 5;
                }
            }
            ImGui.SameLine();
            ImGui.Text($"${MembershipPrice}");
            ImGui.SameLine();
            if (ImGui.ArrowButton("MembershipCostIncrement", ImGuiDir.Right))
            {
                if (MembershipPrice < 95)
                {
                    MembershipPrice += 5;
                }
            }

            if (ImGui.CollapsingHeader("Debug"))
            {
                if (ImGui.Button("Give $10"))
                {
                    Transaction(10, TransactionType.None);
                }

                if (ImGui.Button("Give $100"))
                {
                    Transaction(100, TransactionType.None);
                }

                ImGui.Separator();
                ImGui.Text($"AvgNeeds: {_avgNeeds}");
                ImGui.Text($"InitialBoost: {InitialBoost}");
                ImGui.Text($"MarketingBoost: {MarketingBoost}");
                ImGui.Text($"NewEquipmentBoost: {NewEquipmentValueBoost}");
            }

            ImGui.End();
        }
    }
}
