using GymTycoon.Code.Common;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using GymTycoon.Code.Data;

namespace GymTycoon.Code.AI
{
    public enum NeedType
    {
        Toilet,
        Rest,
        Beauty
    }

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
        public Preferences Preferences;
        public NeedModifier NeedModifier;
        public List<TraitData> Traits;

        public OffscreenGuest(IEnumerable<TraitData> traits)
        {
            Schedule = new Schedule();
            Routine = new Routine();
            Preferences = new Preferences();
            NeedModifier = new NeedModifier();
            WealthTier = WealthTier.Medium;
            Traits = new(traits);

            foreach (var trait in traits)
            {
                if (trait.WealthTier.HasValue)
                {
                    WealthTier = trait.WealthTier.Value;
                }

                if (trait.PreferenceModifier.HasValue)
                {
                    Preferences = Preferences + trait.PreferenceModifier.Value;
                }

                if (trait.NeedModifier != null)
                {
                    NeedModifier = NeedModifier + trait.NeedModifier;
                }

                if (trait.Routine.HasValue)
                {
                    Routine = trait.Routine.Value;
                }

                if (trait.Schedule.HasValue)
                {
                    Schedule = trait.Schedule.Value;
                }
            }

            Tint = new Color(55 + Random.Shared.Next(0, 200), 55 + Random.Shared.Next(0, 200), 55 + Random.Shared.Next(0, 200), 255);
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
        public const float MinHappiness = -100f;
        public const float MaxHappiness = 100f;

        public static float MaxNeed = 100f;
        public static float MinNeedThreshold = 60f;

        public Dictionary<NeedType, float> NeedsValue = [];
        public Dictionary<ExerciseProperties, float> ExerciseNeedsValue = [];

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

        public float MinAvgNeeds = MaxNeed;
        public float MaxAvgNeeds = 0;

        public OffscreenGuest OffscreenGuest;
        public bool FollowCam = false;

        public Guest(OffscreenGuest offscreenGuest, int worldIndex, SpriteInstance sprite)
        {
            WorldPosition = worldIndex;
            NeedsValue = [];
            OffscreenGuest = offscreenGuest;
            foreach (NeedType e in Enum.GetValues(typeof(NeedType)))
            {
                NeedsValue[e] = MaxNeed;
            }

            foreach (ExerciseProperties prop in Enum.GetValues(typeof(ExerciseProperties)))
            {
                if (prop != ExerciseProperties.None && prop != ExerciseProperties.All)
                {
                    ExerciseNeedsValue[prop] = MaxNeed;
                }
            }

            Sprite = sprite;
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

        public float GetBaseDecayRate(NeedType needType)
        {
            switch(needType)
            {
                case NeedType.Beauty:
                    return GameInstance.Instance.World.GetBeautyAt(WorldPosition);
                case NeedType.Rest:
                    return 0f;
                case NeedType.Toilet:
                    return -1f;
                default:
                    return 0f;
            }
        }

        public float GetExerciseDecayRate(ExerciseProperties prop)
        {
            return -1f;
        }

        public void TickNeeds()
        {
            AverageNeeds = 0f;
            int count = 0;

            foreach (var kvp in NeedsValue)
            {
                ModifyNeed(kvp.Key, GetBaseDecayRate(kvp.Key));
                AverageNeeds += NeedsValue[kvp.Key];
                count++;
            }

            {
                float exerciseTotal = 0;
                int exerciseCount = 0;
                foreach (var inner in ExerciseNeedsValue)
                {
                    ModifyExerciseNeed(inner.Key, GetExerciseDecayRate(inner.Key));
                    exerciseTotal += ExerciseNeedsValue[inner.Key];
                    exerciseCount++;
                }

                float avgExercise = exerciseTotal / exerciseCount;
                AverageNeeds += avgExercise;
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

            

            if (NeedsValue[NeedType.Toilet] < 50)
            {
                float rate = (0.5f - (NeedsValue[NeedType.Toilet] / 50f));
                Happiness -= rate;
            }

            if (NeedsValue[NeedType.Rest] < 50)
            {
                float rate = (0.5f - (NeedsValue[NeedType.Rest] / 50f));
                Happiness -= rate;
            }

            if (NeedsValue[NeedType.Beauty] < 50)
            {
                float rate = OffscreenGuest.Preferences.Beauty * (0.5f - (NeedsValue[NeedType.Beauty] / 50f));
                Happiness -= rate;
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
            float bestUtlity = float.MinValue;

            foreach (AdvertisedBehavior behavior in GameInstance.Instance.World.GetAdvertisedBehaviors())
            {
                float utility = behavior.GetUtility(this);
                if (utility > bestUtlity)
                {
                    bestUtlity = utility;
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
                foreach (var kvp in NeedsValue)
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

        public void ModifyNeed(NeedType needType, float delta)
        {
            SetNeed(needType, NeedsValue[needType] + delta);
        }

        public void SetNeed(NeedType needType, float value)
        {
            NeedsValue[needType] = MathF.Min(MaxNeed, MathF.Max(0, value));
        }

        public void ModifyExerciseNeed(ExerciseProperties prop, float delta)
        {
            SetExerciseNeed(prop, ExerciseNeedsValue[prop] + delta);
        }

        public void SetExerciseNeed(ExerciseProperties prop, float value)
        {
            ExerciseNeedsValue[prop] = MathF.Min(MaxNeed, MathF.Max(0, value));
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
