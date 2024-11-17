using GymTycoon.Code.Common;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using GymTycoon.Code.Data;

namespace GymTycoon.Code.AI
{
    public class OffscreenGuest : OffscreenAgent
    {
        public DateTime LastVisit = new DateTime();

        public string Name;
        public Color Tint;

        public WealthTier WealthTier;
        public Schedule Schedule;
        public Routine Routine;
        public float? SpeedModifier;
        public float? TidynessModifier;
        public Dictionary<string, float> BasicNeedModifiers;
        public Dictionary<string, int> SpawnNeedModifiers;
        public Dictionary<string, NeedFilter> AdvancedNeedModifiers;

        public List<TraitData> Traits;

        public Dictionary<Tag, int> ExerciseExperience;

        public OffscreenGuest(IEnumerable<TraitData> traits) : base()
        {
            Schedule = new Schedule();
            Routine = new Routine();
            BasicNeedModifiers = [];
            SpawnNeedModifiers = [];
            AdvancedNeedModifiers = [];
            ExerciseExperience = [];
            WealthTier = WealthTier.Medium;
            Traits = new(traits);

            foreach (var trait in traits)
            {
                if (trait.WealthTier.HasValue)
                {
                    WealthTier = trait.WealthTier.Value;
                }

                if (trait.BasicNeedModifiers != null)
                {
                    foreach (var basicNeedMod in trait.BasicNeedModifiers)
                    {
                        if (BasicNeedModifiers.ContainsKey(basicNeedMod.Key))
                        {
                            BasicNeedModifiers[basicNeedMod.Key] *= basicNeedMod.Value;
                        }
                        else
                        {
                            BasicNeedModifiers[basicNeedMod.Key] = basicNeedMod.Value;
                        }
                    }
                }

                if (trait.SpawnNeedModifiers != null)
                {
                    foreach (var spawnNeedMod in trait.SpawnNeedModifiers)
                    {
                        if (SpawnNeedModifiers.ContainsKey(spawnNeedMod.Key))
                        {
                            SpawnNeedModifiers[spawnNeedMod.Key] *= spawnNeedMod.Value;
                        }
                        else
                        {
                            SpawnNeedModifiers[spawnNeedMod.Key] = spawnNeedMod.Value;
                        }
                    }
                }

                if (trait.AdvancedNeedModifiers != null)
                {
                    foreach (var advNeedMod in trait.AdvancedNeedModifiers)
                    {
                        Debug.Assert(AdvancedNeedModifiers.ContainsKey(advNeedMod.Key) == false, $"Advanced need modifier for {advNeedMod.Key} is being overridden!");

                        // overrides
                        AdvancedNeedModifiers[advNeedMod.Key] = advNeedMod.Value;
                    }
                }

                if (trait.Routine.HasValue)
                {
                    Routine = trait.Routine.Value;
                    Routine.RandomIndex();
                }

                if (trait.Schedule.HasValue)
                {
                    Schedule = trait.Schedule.Value;
                }
            }

            Tint = new Color(55 + Random.Shared.Next(0, 200), 55 + Random.Shared.Next(0, 200), 55 + Random.Shared.Next(0, 200), 255);
        }

        public int GetExperience(Tag key)
        {
            if (ExerciseExperience.ContainsKey(key))
            {
                return ExerciseExperience[key];
            }

            ExerciseExperience[key] = 0;
            return 0;
        }

        public void AddExperience(Tag key, int increment = 1)
        {
            if (ExerciseExperience.ContainsKey(key))
            {
                ExerciseExperience[key] += increment;
            }
            else
            {
                ExerciseExperience[key] = increment;
            }
        }

        public float GetHappinessChangeFromWealthTierVsCost(int cost)
        {
            switch (WealthTier)
            {
                case WealthTier.Low:
                    if (cost < 5) return 1;
                    else if (cost < 10) return 0;
                    else if (cost < 20) return -1;
                    else if (cost < 30) return -2;
                    else if (cost < 40) return -3;
                    break;
                case WealthTier.Medium:
                    if (cost < 5) return 2;
                    else if (cost < 10) return 1;
                    else if (cost < 20) return 0;
                    else if (cost < 30) return -1;
                    else if (cost < 40) return -2;
                    break;
                case WealthTier.High:
                    if (cost < 5) return 3;
                    else if (cost < 10) return 2;
                    else if (cost < 20) return 1;
                    else if (cost < 30) return 0;
                    else if (cost < 40) return -1;
                    break;
                case WealthTier.Premium:
                    if (cost < 5) return 4;
                    else if (cost < 10) return 3;
                    else if (cost < 20) return 2;
                    else if (cost < 30) return 1;
                    else if (cost < 40) return 0;
                    break;
            }

            return -100;
        }
    }

    public class Guest : Agent
    {
        public int Money = 0;

        public OffscreenGuest OffscreenGuest => (OffscreenGuest)OffscreenAgent;

        public Guest(OffscreenGuest offscreenGuest, int worldIndex, SpriteInstance sprite) : base(worldIndex, sprite)
        {
            OffscreenAgent = offscreenGuest;

            // TODO: initial needs values!
            Needs = GameInstance.Instance.NeedsManager.CreateNeeds();
            foreach (var spawnModifier in offscreenGuest.SpawnNeedModifiers)
            {
                Needs[spawnModifier.Key] = spawnModifier.Value;
            }
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (GameInstance.Instance.Time.DidChangeHour)
            {
                RemainingStayTime--;
            }
        }


        public void Think(string thought)
        {
            Debug.WriteLine($"Thought: {thought}");
        }

        public override int GetExperience(Tag key)
        {
            return OffscreenGuest.GetExperience(key);
        }

        public override void AddExperience(Tag key, int increment = 1)
        {
            OffscreenGuest.AddExperience(key, increment);
        }

        public override void DrawImGui()
        {
            ImGui.Begin("Guest");
            ImGui.Text($"Position = {GameInstance.Instance.World.GetPosition(WorldPosition)}");
            ImGui.Text($"CheckedIn = {HasCheckedIn}");
            ImGui.Text($"HasDestination = {HasDestination}");
            ImGui.Text($"Destination = {FinalDestination}");
            ImGui.Text($"AvgNeeds = {AverageNeeds}");
            ImGui.Text($"Happiness = {Happiness}");
            ImGui.Text($"LifetimeHappiness = {OffscreenAgent.LifetimeHappiness}");
            ImGui.Checkbox("FollowCam", ref FollowCam);
            foreach (var behavior in _activeBehaviors ) { 
                ImGui.Text($"Behavior = {behavior.Script.GetType()}");
                if (behavior.Target != null)
                {
                    ImGui.Text($"Context = {behavior.Target.GetType()}");
                }

                if (ImGui.CollapsingHeader("Blackboard"))
                {
                    foreach (var item in behavior.Blackboard)
                    {
                        ImGui.Text($"{item.Key} = {item.Value}");
                    }
                }
            }
            if (ImGui.CollapsingHeader("Needs"))
            {
                foreach (var kvp in Needs.All())
                {
                    ImGui.Text($"{kvp.Key} = {kvp.Value}");
                }
            }

            if (ImGui.CollapsingHeader("Traits"))
            {
                foreach (var trait in OffscreenGuest.Traits)
                {
                    ImGui.Text($"{trait.Name}");
                }
            }

            if (ImGui.CollapsingHeader("Experience"))
            {
                foreach (var kvp in OffscreenGuest.ExerciseExperience)
                {
                    ImGui.Text($"{kvp.Key} = {kvp.Value}");
                }
            }

            foreach (var held in HeldObjects)
            {
                ImGui.Text($"Holding: {held}");
            }

            ImGui.Separator();
            ImGui.End();
        }

    }
}
