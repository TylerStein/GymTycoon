using GymTycoon.Code.Common;
using ImGuiNET;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymTycoon.Code.Data
{
    public class ScenarioData
    {
        public MetaData MetaData;

        [JsonProperty]
        public string Name;

        [JsonProperty]
        public int StartingCapital;

        [JsonProperty]
        public string Map;

        [JsonProperty]
        public bool InfiniteMoney;

        [JsonProperty]
        public int StartingYear;

        [JsonProperty]
        public int StartingMonth;

        [JsonProperty]
        public int StartingDay;

        [JsonProperty]
        public int InitialGymRating;

        [JsonProperty]
        public float InitialBoost;

        [JsonProperty]
        public float InitialBoostDecayRate;

        [JsonProperty]
        public float MarketingDecayRate;

        [JsonProperty]
        public float NewEquipmentDecayRate;

        [JsonProperty]
        public int FailConditionMoney = 0;

        [JsonProperty]
        public int FailConditionMoneyMinutes = 0;

        [JsonProperty]
        public bool FailConditionMoneyEnabled = false;

        [JsonProperty]
        public int FailConditionReputation = 0;

        [JsonProperty]
        public int FailConditionReputationMinutes = 0;

        [JsonProperty]
        public bool FailConditionReputationEnabled = false;
    }

   public class Scenario
    {
        public ScopedName Name;
        public int StartingCapital;
        public string Map;
        public bool InfiniteMoney;
        public DateOnly StartDate;
        public int InitialGymRating;
        public float InitialBoost;
        public float InitialBoostDecayRate;
        public float MarketingDecayRate;
        public float NewEquipmentDecayRate;

        public int FailConditionMoney;
        public int FailConditionMoneyMinutes;
        public bool FailConditionMoneyEnabled;

        public int FailConditionReputation;
        public int FailConditionReputationMinutes;
        public bool FailConditionReputationEnabled;

        private int _failConditionMoneyCounter = 0;
        private int _failConditionReputationCounter = 0;

        private bool _continuePostFailure = false;

        private bool _failingReputation = false;
        private bool _failingMoney = false;

        private bool _failedReputation = false;
        private bool _failedMoney = false;

        private bool HasFailed => _failedReputation || _failedMoney;

        public static Scenario Load(ScenarioData data, ContentManager content)
        {
            Scenario scenario = new Scenario()
            {
                Name = new ScopedName(new string[] { data.MetaData.Type, data.MetaData.Package, data.Name }),
                StartingCapital = data.StartingCapital,
                Map = data.Map,
                InfiniteMoney = data.InfiniteMoney,
                StartDate = new DateOnly(data.StartingYear, data.StartingMonth, data.StartingDay),
                InitialGymRating = data.InitialGymRating,
                InitialBoost = data.InitialBoost,
                InitialBoostDecayRate = data.InitialBoostDecayRate,
                MarketingDecayRate = data.MarketingDecayRate,
                NewEquipmentDecayRate = data.NewEquipmentDecayRate,
                FailConditionMoney = data.FailConditionMoney,
                FailConditionMoneyMinutes = data.FailConditionMoneyMinutes,
                FailConditionMoneyEnabled = data.FailConditionMoneyEnabled,
                FailConditionReputation = data.FailConditionReputation,
                FailConditionReputationMinutes = data.FailConditionReputationMinutes,
                FailConditionReputationEnabled = data.FailConditionReputationEnabled,
            };

            return scenario;
        }

        public void Update()
        {
            if (HasFailed && !_continuePostFailure)
            {
                GameInstance.Instance.Time.PauseTimeScale();
            }

            if (GameInstance.Instance.Time.DidChangeMinute && !HasFailed)
            {
                if (FailConditionReputationEnabled && GameInstance.Instance.Economy.GymRating < FailConditionReputation)
                {
                    _failConditionReputationCounter++;
                    _failingReputation = true;
                    if (_failConditionReputationCounter > FailConditionReputationMinutes)
                    {
                        _failedReputation = true;
                        GameInstance.Instance.Time.PauseTimeScale();
                    }
                }
                else
                {
                    _failingReputation = false;
                    _failConditionReputationCounter = 0;
                }

                if (FailConditionMoneyEnabled && GameInstance.Instance.Economy.PlayerMoney < FailConditionMoney)
                {
                    _failConditionMoneyCounter++;
                    _failingMoney = true;
                    if (_failConditionMoneyCounter > FailConditionMoneyMinutes)
                    {
                        _failedMoney = true;
                        GameInstance.Instance.Time.PauseTimeScale();
                    }
                }
                else
                {
                    _failingMoney = false;
                    _failConditionMoneyCounter = 0;
                }
            }
        }

        public void DrawImGui()
        {
            ImGui.Begin("Scenario");
            if (HasFailed)
            {
                ImGui.Text("Scenario failed, but you can keep playing!");
            }

            ImGui.SeparatorText("Fail Conditions");
            if (FailConditionReputationEnabled)
            {
                ImGui.Text($"Reputation is {FailConditionReputation} for {FailConditionReputationMinutes / 60} hours");
                if (_failingReputation)
                {
                    ImGui.SameLine();
                    ImGui.Text("(!!!)");
                }
            }

            if (FailConditionMoneyEnabled)
            {
                ImGui.Text($"Money is {FailConditionMoney} for {FailConditionMoneyMinutes / 60} hours");
                if (_failingMoney)
                {
                    ImGui.SameLine();
                    ImGui.Text("(!!!)");
                }
            }

            ImGui.End();

            if (HasFailed && !_continuePostFailure)
            {
                ImGui.Begin("Game Over");

                if (_failedMoney)
                {
                    ImGui.Text("You did not meet the money requirements for this scenario.");
                }

                if (_failedReputation)
                {
                    ImGui.Text("You did not meet the reputation requirements for this scenario.");
                }

                if (ImGui.Button("Continue Playing"))
                {
                    _continuePostFailure = true;
                }

                ImGui.End();
            }
        }
    }
}
