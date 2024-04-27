using GymTycoon.Code.Common;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using GymTycoon.Code.Data;

namespace GymTycoon.Code.AI
{
    public class OffscreenGuest
    {
        public const float MinLifetimeHappiness = -100;
        public const float MaxLifetimeHappiness = 100;

        public float LifetimeHappiness = 0f;

        public DateTime LastVisit = new DateTime();
        public int LifetimeVisits = 0;

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

        public OffscreenGuest(IEnumerable<TraitData> traits)
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

        public int GetExerciseExperience(Tag exercise)
        {
            if (ExerciseExperience.ContainsKey(exercise))
            {
                return ExerciseExperience[exercise];
            }

            ExerciseExperience[exercise] = 0;
            return 0;
        }

        public void AddExerciseExperience(Tag exercise, int increment = 1)
        {
            if (ExerciseExperience.ContainsKey(exercise))
            {
                ExerciseExperience[exercise] += increment;
            }
            else
            {
                ExerciseExperience[exercise] = increment;
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

    public class Guest
    {
        public const float MinHappiness = -1000f;
        public const float MaxHappiness = 1000f;

        public Needs Needs;

        public int WorldPosition;
        public bool PendingRemoval;
        public Stack<Point3> Path = [];
        public Point3 FinalDestination = Point3.Zero;
        public bool HasDestination = false;
        public int AnimFrame = 0;
        public float AverageNeeds = 0;
        public List<DynamicObjectInstance> HeldObjects = [];
        public Stack<BehaviorInstance> _activeBehaviors = [];

        public Vector3 TileOffset = Vector3.Zero;
        public Vector3 Destination = Vector3.Zero;
        public float MoveSpeed = 2f; // tiles per second

        public Direction Direction = Direction.SOUTH;
        public SpriteInstance Sprite;

        public bool HasCheckedIn = false;
        public float Happiness = 0;

        public int Money = 0;
        public int RemainingStayTime = 5;

        public float MinAvgNeeds = Needs.MinValue;
        public float MaxAvgNeeds = 0;

        public OffscreenGuest OffscreenGuest;
        public bool FollowCam = false;

        public Guest(OffscreenGuest offscreenGuest, int worldIndex, SpriteInstance sprite)
        {
            WorldPosition = worldIndex;
            OffscreenGuest = offscreenGuest;
            Sprite = sprite;

            // TODO: initial needs values!
            Needs = GameInstance.Instance.NeedsManager.CreateNeeds();
            foreach (var spawnModifier in offscreenGuest.SpawnNeedModifiers)
            {
                Needs[spawnModifier.Key] = spawnModifier.Value;
            }
        }

        public void Update(float deltaTime)
        {
            Sprite.Update(deltaTime);

            if (GameInstance.Instance.Time.DidChangeHour)
            {
                RemainingStayTime--;
            }

            if (HasDestination)
            {
                Point3 currentGridPos = GameInstance.Instance.World.GetPosition(WorldPosition);
                Vector3 currentPosition = currentGridPos.ToVector3() + TileOffset;

                while (deltaTime > 0)
                {
                    float dist = Vector3.Distance(Destination, currentPosition);
                    Vector3 dir = dist == 0 ? Vector3.Zero : Vector3.Normalize(Destination - currentPosition);

                    float distToMove = MoveSpeed * deltaTime;
                    if (distToMove >= dist)
                    {
                        currentPosition = Destination;
                        deltaTime -= dist / MoveSpeed;
                        if (Path.Count > 0)
                        {
                            Destination = Path.Pop().ToVector3();
                        }
                        else
                        {
                            HasDestination = false;
                            deltaTime = 0f;
                        }
                    }
                    else
                    {
                        deltaTime = 0f;
                        if (dist < 0.001f)
                        {
                            // at destination
                            currentPosition = Destination;
                            if (Path.Count > 0)
                            {
                                Destination = Path.Pop().ToVector3();
                            }
                            else
                            {
                                HasDestination = false;
                            }
                        } else
                        {
                            currentPosition += dir * distToMove;
                        }
                    }
                }

                SetPosition(currentPosition);
            }
        }

        public void Tick()
        {
            if (PendingRemoval)
            {
                return;
            }

            TickNeeds();
            TickBehavior();
        }

        public void AddBehavior(AdvertisedBehavior advertisedBehavior)
        {
            AddBehavior(new BehaviorInstance(advertisedBehavior, this));
        }

        public void AddBehavior(BehaviorInstance behavior)
        {
            if (_activeBehaviors.Count > 0)
            {
                _activeBehaviors.Peek().Pause();
            }

            _activeBehaviors.Push(behavior);
            behavior.Reset();
        }

        public void RemoveActiveBehavior()
        {
            if (_activeBehaviors.Count == 0)
            {
                return;
            }

            BehaviorInstance inst = _activeBehaviors.Pop();
            inst.Release();

            if (_activeBehaviors.Count > 0)
            {
                _activeBehaviors.Peek().Resume();
            }
        }

        public void TickNeeds()
        {
            AverageNeeds = 0f;
            int count = 0;

            NeedsManager needsMgr = GameInstance.Instance.NeedsManager;
            foreach (var kvp in Needs.All())
            {
                int value = Needs[kvp.Key] + needsMgr.GetIdleChangeRate(kvp.Key);
                Needs[kvp.Key] = needsMgr.Clamp(kvp.Key, value);
                float eval = needsMgr.EvaluateHappinessFunction(kvp.Key, kvp.Value);
                Happiness -= eval;
                AverageNeeds += Needs[kvp.Key];
                count++;
            }

            AverageNeeds /= count;
            if (AverageNeeds < MinAvgNeeds)
            {
                MinAvgNeeds = AverageNeeds;
            }

            if (AverageNeeds > MaxAvgNeeds)
            {
                MaxAvgNeeds = AverageNeeds;
            }
        }

        public void TickBehavior()
        {
            if (_activeBehaviors.Count == 0)
            {
                FindBehavior();
                return;
            }

            // true return means script is complete
            if (_activeBehaviors.Peek().Tick())
            {
                RemoveActiveBehavior();
                return;
            }
        }

        public void FindBehavior()
        {
            AdvertisedBehavior bestBehavior = null;
            float bestUtility = float.MinValue;

            foreach (AdvertisedBehavior behavior in GameInstance.Instance.World.GetAdvertisedBehaviors())
            {
                float utility = behavior.GetUtility(this);
                if (utility > bestUtility)
                {
                    bestUtility = utility;
                    bestBehavior = behavior;
                }
            }

            if (bestBehavior == null)
            {
                return;
            }

            AddBehavior(bestBehavior);
        }

        public void Think(string thought)
        {
            Debug.WriteLine($"Thought: {thought}");
        }

        public void SetPosition(Vector3 position)
        {
            Point3 point = new Point3(position);
            Point3 lastPos = GameInstance.Instance.World.GetPosition(WorldPosition);
            Point3 dir = (point - lastPos);
            if (Math.Abs(dir.X) > Math.Abs(dir.Y))
            {
                Direction = dir.X > 0 ? Direction.EAST : Direction.WEST;
            }
            else
            {
                Direction = dir.Y > 0 ? Direction.SOUTH : Direction.NORTH;
            }

            int nextIndex = GameInstance.Instance.World.GetIndex(point);
            WorldPosition = nextIndex;
            TileOffset = new Vector3(position.X % 1f, position.Y % 1f, position.Z % 1f);

            GameInstance.Instance.World.SocialLayer.InvalidateCacheInRadius2D(WorldPosition);
        }

        public bool NavigateTo(Point3 point)
        {
            Point3 worldPos = GameInstance.Instance.World.GetPosition(WorldPosition);
            if (worldPos == point)
            {
                return true;
            }

            if (HasDestination && FinalDestination == point)
            {
                return false;
            }

            HasDestination = Navigation.Pathfinding(GameInstance.Instance.World, worldPos, point, Path);
            if (HasDestination)
            {
                FinalDestination = point;
                Destination = Path.Pop().ToVector3();
            }

            return false;
        }

        public void DrawImGui()
        {
            ImGui.Begin("Guest");
            ImGui.Text($"Position = {GameInstance.Instance.World.GetPosition(WorldPosition)}");
            ImGui.Text($"CheckedIn = {HasCheckedIn}");
            ImGui.Text($"HasDestination = {HasDestination}");
            ImGui.Text($"Destination = {FinalDestination}");
            ImGui.Text($"AvgNeeds = {AverageNeeds}");
            ImGui.Text($"Happiness = {Happiness}");
            ImGui.Text($"LifetimeHappiness = {OffscreenGuest.LifetimeHappiness}");
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

        public bool IsHolding(DynamicObjectInstance obj)
        {
            return HeldObjects.Contains(obj);
        }

        public bool IsHoldingObjectOfType(DynamicObject obj)
        {
            for (int i = 0; i < HeldObjects.Count; i++)
            {
                if (HeldObjects[i] != null && HeldObjects[i].Data == obj)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddHeldObj(DynamicObjectInstance obj)
        {
            HeldObjects.Add(obj);
            obj.HeldBy = this;
        }

        public bool RemoveHeldObj(DynamicObjectInstance obj)
        {
            obj.HeldBy = null;
            return HeldObjects.Remove(obj);
        }

        public void AddBurst(EBurstType burstType, float life = 2.5f)
        {
            Point3 worldPos = GameInstance.Instance.World.GetPosition(WorldPosition);
            GameInstance.Instance.WorldRenderer.AddBurst(worldPos, burstType, life);
        }

        public void AnimateIdle()
        {
            Sprite.SetActiveLayerSheet(new ScopedName("Default"), 0);
        }

        public void UpdateHappiness(float delta)
        {
            Happiness = MathHelper.Clamp(Happiness + delta, MinHappiness, MaxHappiness);
            OffscreenGuest.LifetimeHappiness = MathHelper.Clamp(OffscreenGuest.LifetimeHappiness + delta, OffscreenGuest.MinLifetimeHappiness, OffscreenGuest.MaxLifetimeHappiness);
        }
    }
}
