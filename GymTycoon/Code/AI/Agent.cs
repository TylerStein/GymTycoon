using GymTycoon.Code.Common;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GymTycoon.Code.Data;
using GymTycoon.Code.AI.BehaviorTree;

namespace GymTycoon.Code.AI
{
    public abstract class OffscreenAgent
    {
        public const float MinLifetimeHappiness = -100;
        public const float MaxLifetimeHappiness = 100;

        public float LifetimeHappiness = 0f;
        public int LifetimeVisits = 0;
    }

    // TODO: Offscreen can probably be "Guest" specific
    public class NullOffscreenAgent : OffscreenAgent
    {

    }

    public class AgentBTUtility
    {
        public bool HasActiveNeed => ActiveNeed != Tag.EMPTY;

        public Tag ActiveNeed;
        public DynamicObjectInstance TargetInstance;
        public int TargetSlot;

        public void ClearActiveNeed()
        {
            ActiveNeed = Tag.EMPTY;
        }

        public void ClearTarget()
        {
            TargetInstance = null;
            TargetSlot = -1;
        }
    }

    public abstract class Agent
    {
        public static int NextId = 0;
        public int Id;

        public const float MinHappiness = -1000f;
        public const float MaxHappiness = 1000f;

        public Needs Needs;
        public Blackboard Blackboard;

        public int WorldPosition;
        public bool PendingRemoval;

        public Stack<Point3> Path = [];
        public Point3 FinalDestination = Point3.Zero;
        public bool HasDestination = false;
        public int AnimFrame = 0;
        public float AverageNeeds = 0;
        public List<DynamicObjectInstance> HeldObjects = [];
        public BehaviorInstance _activeBehavior = null;

        public Vector3 TileOffset = Vector3.Zero;
        public Vector3 Destination = Vector3.Zero;
        public float MoveSpeed = 2f; // tiles per second

        public Direction Direction = Direction.SOUTH;
        public SpriteInstance Sprite;

        public bool HasCheckedIn = false;
        public float Happiness = 0;

        public bool FollowCam = false;

        public float MinAvgNeeds = Needs.MinValue;
        public float MaxAvgNeeds = 0;

        public int RemainingStayTime = 5;

        public OffscreenAgent OffscreenAgent;

        //public BTNode BehaviorTreeRootNode = new BTSuccessNode();
        //public AgentBTUtility BTUtility = new AgentBTUtility();

        public Agent(int worldIndex, SpriteInstance sprite)
        {
            WorldPosition = worldIndex;
            Sprite = sprite;
            Needs = new Needs([]);
            Blackboard = new Blackboard();
            Id = NextId++;
        }

        public abstract void DrawImGui();
        public abstract void AddExperience(Tag key, int increment);
        public abstract int GetExperience(Tag key);

        public override string ToString()
        {
            return Id.ToString();
        }

        public virtual void Update(float deltaTime)
        {
            Sprite.Update(deltaTime);

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
                        }
                        else
                        {
                            currentPosition += dir * distToMove;
                        }
                    }
                }

                SetPosition(currentPosition);
            }
        }

        public virtual void Tick()
        {
            if (PendingRemoval)
            {
                return;
            }

            TickNeeds();
            TickBehavior();
        }

        public virtual void SetBehavior(AdvertisedBehavior advertisedBehavior)
        {
            if (_activeBehavior != null)
            {
                throw new Exception("Cannot set behavior while one is active");
            }

            if (PendingRemoval)
            {
                return;
            }

            _activeBehavior = new BehaviorInstance(advertisedBehavior, this);
        }

        public virtual void TickNeeds()
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

        public virtual void TickBehavior()
        {
            if (_activeBehavior == null)
            {
                FindBehavior();
                return;
            }

            bool result = _activeBehavior.Tick();
            if (result)
            {
                _activeBehavior = null;
            }
        }

        public virtual void TerminateBehavior()
        {
            if (_activeBehavior != null)
            {
                _activeBehavior.Terminate();
                _activeBehavior = null;
            }
        }

        public virtual void FindBehavior()
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

            SetBehavior(bestBehavior);
        }

        public virtual void SetPosition(Vector3 position)
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

        public virtual bool NavigateTo(Point3 point)
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


        public virtual bool IsHolding(DynamicObjectInstance obj)
        {
            return HeldObjects.Contains(obj);
        }

        public virtual bool IsHoldingObjectOfType(DynamicObject obj)
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

        public virtual void AddHeldObj(DynamicObjectInstance obj)
        {
            HeldObjects.Add(obj);
            obj.HeldBy = this;
        }

        public virtual bool RemoveHeldObj(DynamicObjectInstance obj)
        {
            obj.HeldBy = null;
            return HeldObjects.Remove(obj);
        }

        public virtual void AddBurst(EBurstType burstType, float life = 2.5f)
        {
            Point3 worldPos = GameInstance.Instance.World.GetPosition(WorldPosition);
            GameInstance.Instance.WorldRenderer.AddBurst(worldPos, burstType, life);
        }

        public virtual void AnimateIdle()
        {
            Sprite.SetActiveLayerSheet(new ScopedName("Default"), 0);
        }

        public virtual void UpdateHappiness(float delta)
        {
            Happiness = MathHelper.Clamp(Happiness + delta, MinHappiness, MaxHappiness);
            OffscreenAgent.LifetimeHappiness = MathHelper.Clamp(OffscreenAgent.LifetimeHappiness + delta, OffscreenAgent.MinLifetimeHappiness, OffscreenAgent.MaxLifetimeHappiness);
        }
    }
}
