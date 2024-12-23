﻿using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code.AI
{
    public static class BehaviorFactory
    {

        public static Dictionary<ScopedName, Type> AllBehaviorTypes = new()
        {
            { new ScopedName("Script.Default.Wander"), typeof(BWander) },
            { new ScopedName("Script.Default.Leave"), typeof(BLeave) },
            { new ScopedName("Script.Default.ExerciseWithPortableMat"), typeof(BBasicExercise) },
            { new ScopedName("Script.Default.RunWithTreadmill"), typeof(BBasicExercise) },
            { new ScopedName("Script.Default.Reception"), typeof(BCheckIn) },
            { new ScopedName("Script.Default.DumbellCurl"), typeof (BBasicExercise) },
            { new ScopedName("Script.Default.ReturnToDispenser"), typeof (BReturnToDispenser) },
            { new ScopedName("Script.Default.Toilet"), typeof(BToilet) },
        };

        public static BehaviorScript Create(Type type)
        {
            if (type != null && typeof(BehaviorScript).IsAssignableFrom(type))
            {
                // TODO: Pooling
                BehaviorScript script = Activator.CreateInstance(type) as BehaviorScript;
                return script;
            }

            return null;
        }

        public static BehaviorScript Create(ScopedName name)
        {
            Type type;
            if (AllBehaviorTypes.TryGetValue(name, out type))
            {
                return Create(type);
            }

            return null;
        }
    }

    public class BehaviorScript
    {
        /// <summary>
        /// Set up any blackboard values and related state
        /// </summary>
        /// <param name="inst"></param>
        public virtual void Reset(BehaviorInstance inst)
        {
            //
        }

        /// <summary>
        /// This behavior is to be removed, clean up any state
        /// </summary>
        public virtual void Complete(BehaviorInstance inst, Agent agent)
        {
            //
        }

        /// <summary>
        /// Another behavior has become active, this behavior may be resumed
        /// </summary>
        public virtual void Paused(BehaviorInstance inst, Agent agent)
        {
            //
        }

        /// <summary>
        /// Another behavior has been completed and this behavior is starting
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="agent"></param>
        public virtual void Resume(BehaviorInstance inst, Agent agent)
        {

        }

        public virtual float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            return 0;
        }

        public virtual bool Tick(BehaviorInstance inst, Agent agent)
        {
            return false;
        }

        public static bool CanBeClaimedBy(DynamicObjectInstance target, Agent agent)
        {
            if (target.Held && !agent.IsHolding(target)) return false;
            if (target.ClaimSlots[0] != null && target.ClaimSlots[0] != agent) return false;
            return true;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static float NeedUrgency(float max, float value)
        //{
        //    return max - value;
        //}

        public static float NeedUrgency(Agent agent, Tag type)
        {
            if (agent.Needs.TryGetValue(type, out int value))
            {
                return value;
            }

            return 0;
        }

        public static float ExerciseNeedUrgency(Agent agent, Dictionary<Tag, int> properties)
        {
            float totalUrgency = 0;
            int totalCount = 0;
            foreach (var property in properties)
            {
                if (agent.Needs.HasNeed(property.Key))
                {
                    totalCount++;
                    totalUrgency += NeedUrgency(agent, property.Key) * property.Value; // agent need urgency * exercise effectiveness
                }
            }

            if (totalCount == 0)
            {
                return int.MinValue;
            }

            return totalUrgency;
        }

        public static float DistancePenalty(int a, int b)
        {
            float distance = Point3.Distance(
                GameInstance.Instance.World.GetPosition(a),
                GameInstance.Instance.World.GetPosition(b));
            return 1f / (1f + distance);
        }

        public static float DistancePenalty(Agent agent, int worldIndex)
        {
            return DistancePenalty(worldIndex, agent.WorldPosition);
        }

        public static float QueuePenalty(DynamicObjectInstance target)
        {
            // TODO: Queue penalty
            if (target.FindOpenClaimSlot() == -1)
            {
                return 0f;
            }

            return 1f;
        }

        public static bool FindClosestTileWithProperties(Point3 agentPos, TileProperties properties, out int worldIndex, int maxCheck = int.MaxValue)
        {
            List<int> destinations = GameInstance.Instance.World.FindTilesWithProperties(properties, maxCheck);
            if (destinations.Count == 0)
            {
                worldIndex = 0;
                return false;
            }

            float closest = float.MaxValue;
            int closestIndex = 0;
            for (int i = 0; i < destinations.Count; i++)
            {
                float dist = Point3.Distance(agentPos, GameInstance.Instance.World.GetPosition(destinations[i]));
                if (dist < closest)
                {
                    closest = dist;
                    closestIndex = i;
                }
            }

            worldIndex = destinations[closestIndex];
            return true;
        }
    }

    public class BWander : BehaviorScript
    {
        private const string SymbolActiveAction = "Active";

        public override void Reset(BehaviorInstance inst)
        {
            inst.Blackboard[SymbolActiveAction] = null;
        }

        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            return 0f;
        }

        public override bool Tick(BehaviorInstance inst, Agent agent)
        {
            Action activeAction = inst.Blackboard[SymbolActiveAction];
            if (activeAction == null)
            {
                List<int> destinations = GameInstance.Instance.World.FindTilesWithProperties(TileProperties.Navigable);
                if (!agent.HasCheckedIn)
                {
                    destinations = destinations.Where(GameInstance.Instance.CanNavigatePreCheckInOnTile).ToList();
                }

                int index = Random.Shared.Next(destinations.Count);
                activeAction = new AMoveTo(GameInstance.Instance.World.GetPosition(destinations[index]));
                inst.Blackboard[SymbolActiveAction] = activeAction;
            }

            EActionState state = activeAction.Tick(agent);
            if (state != EActionState.WAITING)
            {
                inst.Blackboard[SymbolActiveAction] = null;
                return true;
            }

            // TODO: revisit resting
            foreach (var kvp in agent.Needs.All())
            {
                if (kvp.Value < 0)
                {
                    int setValue = MathHelper.Clamp(kvp.Value + 1, kvp.Value, 0);
                    agent.Needs.SetValue(kvp.Key, setValue);
                }
            }

            return false;
        }
    }

    public class BLeave : BehaviorScript
    {
        private const string SymbolActiveAction = "Active";
        private const string SymbolHasDestination = "HasDest";
        private const string SymbolDestination = "Destination";
        private const string SymbolState = "State";

        private const int StateCheckHeldObject = 0;
        private const int StateFindHeldObjectDropPoint = 1;
        private const int StateMoveToHeldObjectDropPoint = 2;
        private const int StateDropHeldObject = 3;
        private const int StateLeave = 4;

        public override void Reset(BehaviorInstance inst)
        {
            inst.Blackboard[SymbolActiveAction] = null;
            inst.Blackboard[SymbolHasDestination] = false;
            inst.Blackboard[SymbolDestination] = 0;
            inst.Blackboard[SymbolState] = 0;
        }

        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            if (agent.Happiness <= Agent.MinHappiness || agent.RemainingStayTime < 1)
            {
                return float.MaxValue;
            }


            return agent.AverageNeeds * -1f;
            // return float.MinValue;
        }


        private void UpdateDestination(BehaviorInstance inst, Agent agent)
        {
            inst.Blackboard[SymbolHasDestination] = false;
            Point3 guestPos = GameInstance.Instance.World.GetPosition(agent.WorldPosition);
            int worldIndex;
            if (FindClosestTileWithProperties(guestPos, TileProperties.Spawn, out worldIndex))
            {
                inst.Blackboard[SymbolDestination] = worldIndex;
                inst.Blackboard[SymbolHasDestination] = true;
            }
        }

        public override bool Tick(BehaviorInstance inst, Agent agent)
        {
            switch (inst.Blackboard[SymbolState])
            {
                case StateCheckHeldObject:
                    if (agent.HeldObjects.Count > 0)
                    {
                        inst.Blackboard[SymbolState] = StateFindHeldObjectDropPoint;
                    }
                    else
                    {
                        inst.Blackboard[SymbolState] = StateLeave;
                    }

                    break;
                case StateFindHeldObjectDropPoint:
                    Point3 guestPos = GameInstance.Instance.World.GetPosition(agent.WorldPosition);
                    int worldIndex;
                    if (FindClosestTileWithProperties(guestPos, TileProperties.Navigable, out worldIndex))
                    {
                        Point3 worldPos = GameInstance.Instance.World.GetPosition(worldIndex);
                        inst.Blackboard[SymbolActiveAction] = new AMoveTo(worldPos);
                        inst.Blackboard[SymbolState] = StateMoveToHeldObjectDropPoint;
                        return false;
                    }

                    break;
                case StateMoveToHeldObjectDropPoint:
                    Action moveAction = inst.Blackboard[SymbolActiveAction];
                    if (moveAction == null)
                    {
                        inst.Blackboard[SymbolState] = StateCheckHeldObject;
                        return false;
                    }

                    if (moveAction.Tick(agent) == EActionState.SUCCESS)
                    {
                        inst.Blackboard[SymbolActiveAction] = new APutDown(agent, agent.HeldObjects[0], false);
                        inst.Blackboard[SymbolState] = StateDropHeldObject;
                        return false;
                    }

                    break;
                case StateDropHeldObject:
                    Action dropAction = inst.Blackboard[SymbolActiveAction];
                    if (dropAction == null)
                    {
                        inst.Blackboard[SymbolState] = StateCheckHeldObject;
                        break;
                    }

                    if (dropAction.Tick(agent) == EActionState.SUCCESS)
                    {
                        inst.Blackboard[SymbolState] = StateLeave;
                        return false;
                    }

                    break;
                case StateLeave:
                    bool hasDestination = inst.Blackboard[SymbolHasDestination];
                    if (hasDestination)
                    {
                        int dest = inst.Blackboard[SymbolDestination];
                        TileType tile = GameInstance.Instance.World.GetTile(dest);
                        if (tile == null || !tile.HasProperty(TileProperties.Spawn))
                        {
                            UpdateDestination(inst, agent);
                        }

                        Action activeAction = inst.Blackboard[SymbolActiveAction];
                        if (activeAction == null)
                        {
                            dest = inst.Blackboard[SymbolDestination];
                            activeAction = new AMoveTo(GameInstance.Instance.World.GetPosition(dest));
                            inst.Blackboard[SymbolActiveAction] = activeAction;
                        }

                        activeAction.Tick(agent);
                        if (activeAction.IsComplete())
                        {
                            if (agent.HeldObjects.Count > 0)
                            {
                                // never leave with held objects
                                inst.Blackboard[SymbolState] = StateCheckHeldObject;
                                break;
                            }

                            agent.PendingRemoval = true;
                            return true;
                        }
                    }
                    else
                    {
                        UpdateDestination(inst, agent);
                    }
                    break;
            }


            return false;
        }
    }

    public class BBasicExercise : BehaviorScript
    {
        private const string SymbolClaimed = "Claimed";
        private const string SymbolStep = "Step";
        private const string SymbolActiveAction = "Active";

        private const int ExerciseStep = 5;

        public override void Reset(BehaviorInstance inst)
        {
            inst.Blackboard[SymbolClaimed] = false;
            inst.Blackboard[SymbolStep] = 0;
            inst.Blackboard[SymbolActiveAction] = null;
        }

        public override void Complete(BehaviorInstance inst, Agent agent)
        {
            if ((bool)inst.Blackboard[SymbolClaimed])
            {
                inst.Target.TryReleaseClaimSlot(agent);
            }
        }

        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            if (!agent.HasCheckedIn)
            {
                return float.MinValue;
            }

            return ExerciseNeedUrgency(agent, behavior.Exercise.NeedModifiers)
                * DistancePenalty(agent, target.WorldPosition)
                * QueuePenalty(target);
            // * SkillModifier(agent, GuestSkillLevel.Beginner)
            // * FunModifier(ExerciseMat.ObjectType)
            // * QualityModifier(ExerciseMat.ObjectType);
        }

        public override bool Tick(BehaviorInstance inst, Agent agent)
        {
            int step = inst.Blackboard[SymbolStep];
            switch (step)
            {
                case 0:
                    int slotIndex = inst.Target.TryOccupyClaimSlot(agent);
                    if (slotIndex == -1)
                    {
                        slotIndex = inst.Target.GetOccupiedClaimSlot(agent);
                    }

                    if (slotIndex != -1)
                    {
                        inst.Blackboard[SymbolClaimed] = true;
                        Point3 equipmentPos = GameInstance.Instance.World.GetPosition(inst.Target.WorldPosition);
                        Point[] guestSlots = inst.Target.GetGuestSlots(inst.Target.Direction);
                        Point3 slot = equipmentPos + guestSlots[slotIndex];
                        inst.Blackboard[SymbolActiveAction] = new AMoveTo(slot);
                        inst.Blackboard[SymbolStep] = step + 1;
                        break;
                    }

                    // failed to claim a slot
                    return true;
                case 1:
                    {
                        Action action = inst.Blackboard[SymbolActiveAction];
                        if (action.IsComplete())
                        {
                            if (inst.Target.Data.Holdable)
                            {
                                inst.Blackboard[SymbolStep] = step + 1;
                                action = new APickUp(agent, inst.Target);
                                inst.Blackboard[SymbolActiveAction] = action;
                                break;
                            }
                            else
                            {
                                action = new AExercise(inst.Data.Exercise, inst.Target.Data.FitnessModifier);
                                inst.Blackboard[SymbolActiveAction] = action;
                                inst.Blackboard[SymbolStep] = ExerciseStep;
                                break;
                            }
                        }

                        // tick move to target
                        if (action.Tick(agent) == EActionState.FAILED)
                        {
                            // move failed
                            return true;
                        }
                        break;
                    }
                case 2:
                    {
                        Action action = inst.Blackboard[SymbolActiveAction];
                        if (action.IsComplete())
                        {
                            // TODO: Desired location logic
                            List<int> destinations = GameInstance.Instance.World.FindTilesWithProperties(TileProperties.Navigable)
                                .Where(GameInstance.Instance.CanExerciseOnTile)
                                .ToList();

                            int index = Random.Shared.Next(destinations.Count);
                            action = new AMoveTo(GameInstance.Instance.World.GetPosition(destinations[index]));
                            inst.Blackboard[SymbolActiveAction] = action;
                            inst.Blackboard[SymbolStep] = step + 1;
                            break;
                        }

                        // tick pick up
                        action.Tick(agent);
                        break;
                    }
                case 3:
                    {
                        Action action = inst.Blackboard[SymbolActiveAction];
                        if (action.IsComplete())
                        {
                            inst.Blackboard[SymbolStep] = step + 1;
                            action = new APutDown(agent, inst.Target);
                            inst.Blackboard[SymbolActiveAction] = action;
                            break;
                        }

                        // tick move to new location
                        if (action.Tick(agent) == EActionState.FAILED)
                        {
                            // on fail, retry!
                            List<int> destinations = GameInstance.Instance.World.FindTilesWithProperties(TileProperties.Navigable);
                            int index = Random.Shared.Next(destinations.Count);
                            action = new AMoveTo(GameInstance.Instance.World.GetPosition(destinations[index]));
                            inst.Blackboard[SymbolActiveAction] = action;
                            return false;
                        }

                        break;
                    }
                case 4:
                    {
                        Action action = inst.Blackboard[SymbolActiveAction];
                        if (action.IsComplete())
                        {
                            inst.Blackboard[SymbolStep] = step + 1;
                            action = new AExercise(inst.Data.Exercise, inst.Target.Data.FitnessModifier);
                            inst.Blackboard[SymbolActiveAction] = action;
                            break;
                        }

                        // tick put down
                        action.Tick(agent);
                        break;
                    }
                case 5:
                    {
                        Action action = inst.Blackboard[SymbolActiveAction];
                        if (action.IsComplete())
                        {
                            // behavior complete!
                            inst.Blackboard[SymbolActiveAction] = null;
                            return true;
                        }

                        // tick exercise
                        action.Tick(agent);
                        break;
                    }
            }

            return false;
        }
    }

    public class BReturnToDispenser : BehaviorScript
    {
        private const string SymbolStep = "Step";
        private const string SymbolAction = "Action";
        private const string SymbolClaimed = "Claimed";
        private const string SymbolParentClaimed = "ParentClaimed";
        private const string SymbolParentSlot = "ParentSlot";

        public override void Reset(BehaviorInstance inst)
        {
            inst.Blackboard[SymbolStep] = 0;
            inst.Blackboard[SymbolAction] = null;
            inst.Blackboard[SymbolClaimed] = false;
            inst.Blackboard[SymbolParentClaimed] = false;
            inst.Blackboard[SymbolParentSlot] = -1;
        }

        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            if (target.Parent == null || target.Racked == true || target.HasAnyClaims())
            {
                return float.MinValue;
            }

            float pickUpDistancePenalty = DistancePenalty(agent, target.WorldPosition);
            float deliverDistancePenalty = DistancePenalty(target.WorldPosition, target.Parent.WorldPosition);
            return pickUpDistancePenalty * deliverDistancePenalty; // TODO: Agent traits for cleanup behavior? re-use Dispose?
        }

        public override bool Tick(BehaviorInstance inst, Agent agent)
        {

            int step = inst.Blackboard[SymbolStep];
            switch (step)
            {
                case 0:
                    {
                        // claim item slot
                        int slotIndex = inst.Target.TryOccupyClaimSlot(agent);
                        if (slotIndex == -1)
                        {
                            slotIndex = inst.Target.GetOccupiedClaimSlot(agent);
                        }

                        if (slotIndex != -1)
                        {
                            inst.Blackboard[SymbolClaimed] = true;
                            Point3 equipmentPos = GameInstance.Instance.World.GetPosition(inst.Target.WorldPosition);
                            Point[] guestSlots = inst.Target.GetGuestSlots(inst.Target.Direction);
                            Point3 slot = equipmentPos + guestSlots[slotIndex];
                            inst.Blackboard[SymbolAction] = new AMoveTo(slot);
                            inst.Blackboard[SymbolStep] = step + 1;
                            break;
                        }

                        // failed to claim a slot
                        return true;
                    }
                case 1:
                    {
                        // move to item
                        Action action = inst.Blackboard[SymbolAction];
                        if (action.IsComplete())
                        {
                            inst.Blackboard[SymbolStep] = step + 1;
                            action = new APickUp(agent, inst.Target);
                            inst.Blackboard[SymbolAction] = action;
                            break;
                        }

                        // tick move to target
                        if (action.Tick(agent) == EActionState.FAILED)
                        {
                            // move failed
                            return true;
                        }
                        break;
                    }
                case 2:
                    {
                        // pick up item
                        Action action = inst.Blackboard[SymbolAction];
                        if (action.IsComplete())
                        {
                            int slotIndex = inst.Target.Parent.TryOccupyClaimSlot(agent);
                            if (slotIndex == -1)
                            {
                                slotIndex = inst.Target.Parent.GetOccupiedClaimSlot(agent);
                            }

                            if (slotIndex != -1)
                            {
                                inst.Blackboard[SymbolParentClaimed] = true;
                                Point3 equipmentPos = GameInstance.Instance.World.GetPosition(inst.Target.Parent.WorldPosition);
                                Point[] guestSlots = inst.Target.GetGuestSlots(inst.Target.Parent.Direction);
                                Point3 slot = equipmentPos + guestSlots[slotIndex];
                                inst.Blackboard[SymbolAction] = new AMoveTo(slot);
                                inst.Blackboard[SymbolStep] = step + 1;
                                break;
                            }

                            // wait for parent to be free (?)
                            break;
                        }

                        // tick pick up
                        action.Tick(agent);
                        break;
                    }
                case 3:
                    {
                        // claim parent slot
                        Action action = inst.Blackboard[SymbolAction];
                        if (action.IsComplete())
                        {
                            action = new AMoveTo(GameInstance.Instance.World.GetPosition(inst.Target.Parent.WorldPosition));
                            inst.Blackboard[SymbolAction] = action;
                            inst.Blackboard[SymbolStep] = step + 1;
                            break;
                        }

                        // tick move
                        action.Tick(agent);
                        break;
                    }
                case 4:
                    {
                        // move to dispenser
                        Action action = inst.Blackboard[SymbolAction];
                        if (action.IsComplete())
                        {
                            inst.Blackboard[SymbolStep] = step + 1;
                            action = new APutDown(agent, inst.Target, true);
                            inst.Blackboard[SymbolAction] = action;
                            break;
                        }

                        // tick move to target
                        if (action.Tick(agent) == EActionState.FAILED)
                        {
                            // TODO: Handle failed move (don't eat the gear!)
                            Reset(inst);
                            return false;
                        }

                        break;
                    }
                case 5:
                    {
                        Action action = inst.Blackboard[SymbolAction];
                        if (action.IsComplete())
                        {
                            return true;
                        }

                        // tick put down
                        action.Tick(agent);
                        break;
                    }

            }

            return false;
        }
    }

    public class BCheckIn : BehaviorScript
    {
        private const string ActiveActionSymbol = "Active";
        private const string ClaimedSymbol = "Claimed";
        private const string StepSymbol = "Step";
        private const string TriesSymbol = "Tries";

        public override void Reset(BehaviorInstance inst)
        {
            inst.Blackboard[ActiveActionSymbol] = null;
            inst.Blackboard[ClaimedSymbol] = false;
            inst.Blackboard[StepSymbol] = 0;
            inst.Blackboard[TriesSymbol] = 0;
        }

        public override void Complete(BehaviorInstance inst, Agent agent)
        {
            dynamic claimed = false;
            if (inst.Blackboard.TryGetValue(ClaimedSymbol, out claimed))
            {
                if ((bool)claimed)
                {
                    inst.Target.TryReleaseClaimSlot(agent);
                }
            }
        }

        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            // TODO: Queue penalty, is staffed penalty
            if (agent.HasCheckedIn)
            {
                return float.MinValue;
            }

            return float.MaxValue * DistancePenalty(agent, target.WorldPosition) * QueuePenalty(target);
        }

        public override bool Tick(BehaviorInstance inst, Agent agent)
        {
            if (agent.HasCheckedIn)
            {
                return true;
            }

            // TODO: Steps for queue, waiting for staff to be on receiption
            if (inst.Blackboard.ContainsKey(StepSymbol) == false)
            {
                string stack = new Exception().StackTrace;
                throw new Exception($"Symbol {StepSymbol} is not in Blackboard for obj {inst.GetType()}\n{stack}");
            }

            int step = inst.Blackboard[StepSymbol];
            switch (step)
            {
                case 0:
                    {
                        int slotIndex = inst.Target.TryOccupyClaimSlot(agent);
                        if (slotIndex == -1)
                        {
                            slotIndex = inst.Target.GetOccupiedClaimSlot(agent);
                        }

                        if (slotIndex != -1)
                        {
                            inst.Blackboard[ClaimedSymbol] = true;
                            Point3 kiosPos = GameInstance.Instance.World.GetPosition(inst.Target.WorldPosition);
                            Point[] guestSlots = inst.Target.GetGuestSlots(inst.Target.Direction);
                            Point3 slot = kiosPos + guestSlots[slotIndex];
                            inst.Blackboard[ActiveActionSymbol] = new AMoveTo(slot);
                            inst.Blackboard[StepSymbol] = step + 1;
                            break;
                        }

                        int tries = (int)inst.Blackboard[TriesSymbol];
                        if (tries > 1)
                        {
                            // stop trying
                            return true;
                        }

                        // go for a walk and try again
                        inst.Blackboard[TriesSymbol] = inst.Blackboard[TriesSymbol] + 1;
                        agent.AddBehavior(GameInstance.Instance.Instances.GetAdvertisedBehavior(new ScopedName("Behavior.Default.Wander")));
                        return false;
                    }
                case 1:
                    {
                        Action action = inst.Blackboard[ActiveActionSymbol];
                        if (action.IsComplete())
                        {
                            action = new ACheckIn();
                            inst.Blackboard[ActiveActionSymbol] = action;
                            inst.Blackboard[StepSymbol] = step + 1;
                            break;
                        }

                        // tick move to kiosk
                        if (action.Tick(agent) == EActionState.FAILED)
                        {
                            // TODO: Handle failed nav?
                            return true;
                        }

                        break;
                    }
                case 2:
                    {
                        Action action = inst.Blackboard[ActiveActionSymbol];
                        if (action.IsComplete())
                        {
                            inst.Blackboard[ActiveActionSymbol] = null;
                            return true;
                        }

                        // tick use kiosk
                        action.Tick(agent);
                        break;
                    }
            }


            return false;
        }
    }

    public class BDumbellRack : BehaviorScript
    {
        private const string SymbolClaimed = "Claimed";
        private const string SymbolStep = "Step";
        private const string SymbolActiveAction = "Active";

        private const int ExerciseStep = 5;

        public override void Reset(BehaviorInstance inst)
        {
            inst.Blackboard[SymbolClaimed] = false;
            inst.Blackboard[SymbolStep] = 0;
            inst.Blackboard[SymbolActiveAction] = null;
        }

        public override void Complete(BehaviorInstance inst, Agent agent)
        {
            if ((bool)inst.Blackboard[SymbolClaimed])
            {
                inst.Target.TryReleaseClaimSlot(agent);
            }
        }

        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            if (!agent.HasCheckedIn)
            {
                return float.MinValue;
            }

            return ExerciseNeedUrgency(agent, behavior.Exercise.NeedModifiers)
                * DistancePenalty(agent, target.WorldPosition)
                * QueuePenalty(target);
            // * SkillModifier(agent, GuestSkillLevel.Beginner)
            // * FunModifier(ExerciseMat.ObjectType)
            // * QualityModifier(ExerciseMat.ObjectType);
        }

        public override bool Tick(BehaviorInstance inst, Agent agent)
        {
            int step = inst.Blackboard[SymbolStep];
            switch (step)
            {
                case 0:
                    int slotIndex = inst.Target.TryOccupyClaimSlot(agent);
                    if (slotIndex == -1)
                    {
                        slotIndex = inst.Target.GetOccupiedClaimSlot(agent);
                    }

                    if (slotIndex != -1)
                    {
                        inst.Blackboard[SymbolClaimed] = true;
                        Point3 equipmentPos = GameInstance.Instance.World.GetPosition(inst.Target.WorldPosition);
                        Point[] guestSlots = inst.Target.GetGuestSlots(inst.Target.Direction);
                        Point3 slot = equipmentPos + guestSlots[slotIndex];
                        inst.Blackboard[SymbolActiveAction] = new AMoveTo(slot);
                        inst.Blackboard[SymbolStep] = step + 1;
                        break;
                    }

                    // failed to claim a slot
                    return true;
                case 1:
                    {
                        Action action = inst.Blackboard[SymbolActiveAction];
                        if (action.IsComplete())
                        {
                            if (inst.Target.Data.Holdable)
                            {
                                inst.Blackboard[SymbolStep] = step + 1;
                                action = new APickUp(agent, inst.Target);
                                inst.Blackboard[SymbolActiveAction] = action;
                                break;
                            }
                            else
                            {
                                action = new AExercise(inst.Data.Exercise, inst.Target.Data.FitnessModifier);
                                inst.Blackboard[SymbolActiveAction] = action;
                                inst.Blackboard[SymbolStep] = ExerciseStep;
                                break;
                            }
                        }

                        // tick move to target
                        if (action.Tick(agent) == EActionState.FAILED)
                        {
                            // move failed
                            return true;
                        }
                        break;
                    }
                case 2:
                    {
                        Action action = inst.Blackboard[SymbolActiveAction];
                        if (action.IsComplete())
                        {
                            // TODO: Desired location logic
                            List<int> destinations = GameInstance.Instance.World.FindTilesWithProperties(TileProperties.Navigable);
                            int index = Random.Shared.Next(destinations.Count);
                            action = new AMoveTo(GameInstance.Instance.World.GetPosition(destinations[index]));
                            inst.Blackboard[SymbolActiveAction] = action;
                            inst.Blackboard[SymbolStep] = step + 1;
                            break;
                        }

                        // tick pick up
                        action.Tick(agent);
                        break;
                    }
                case 3:
                    {
                        Action action = inst.Blackboard[SymbolActiveAction];
                        if (action.IsComplete())
                        {
                            inst.Blackboard[SymbolStep] = step + 1;
                            action = new APutDown(agent, inst.Target);
                            inst.Blackboard[SymbolActiveAction] = action;
                            break;
                        }

                        // tick move to new location
                        action.Tick(agent);
                        break;
                    }
                case 4:
                    {
                        Action action = inst.Blackboard[SymbolActiveAction];
                        if (action.IsComplete())
                        {
                            inst.Blackboard[SymbolStep] = step + 1;
                            action = new AExercise(inst.Data.Exercise, inst.Target.Data.FitnessModifier);
                            inst.Blackboard[SymbolActiveAction] = action;
                            break;
                        }

                        // tick put down
                        action.Tick(agent);
                        break;
                    }
                case 5:
                    {
                        Action action = inst.Blackboard[SymbolActiveAction];
                        if (action.IsComplete())
                        {
                            // behavior complete!
                            inst.Blackboard[SymbolActiveAction] = null;
                            return true;
                        }

                        // tick exercise
                        action.Tick(agent);
                        break;
                    }
            }

            return false;
        }
    }

    public class BToilet : BehaviorScript
    {
        private const string SymbolClaimed = "Claimed";
        private const string SymbolStep = "Step";
        private const string SymbolAction = "Active";

        private const int StepClaim = 0;
        private const int StepMoveTo = 1;
        private const int StepUse = 2;

        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            if (!agent.HasCheckedIn)
            {
                return float.MinValue;
            }

            if (agent.Needs["Toilet"] < 50)
            {
                return float.MinValue;
            }

            return NeedUrgency(agent, "Toilet") * QueuePenalty(target) * DistancePenalty(agent, target.WorldPosition);
        }

        public override void Reset(BehaviorInstance inst)
        {
            inst.Blackboard[SymbolStep] = StepClaim;
            inst.Blackboard[SymbolAction] = null;
            inst.Blackboard[SymbolClaimed] = false;
        }

        public override bool Tick(BehaviorInstance inst, Agent agent)
        {
            if (!agent.HasCheckedIn)
            {
                return true;
            }

            int step = inst.Blackboard[SymbolStep];
            switch (step)
            {
                case StepClaim:
                    {
                        int slotIndex = inst.Target.TryOccupyClaimSlot(agent);
                        if (slotIndex == -1)
                        {
                            slotIndex = inst.Target.GetOccupiedClaimSlot(agent);
                        }

                        if (slotIndex != -1)
                        {
                            inst.Blackboard[SymbolClaimed] = true;
                            Point3 kiosPos = GameInstance.Instance.World.GetPosition(inst.Target.WorldPosition);
                            Point[] guestSlots = inst.Target.GetGuestSlots(inst.Target.Direction);
                            Point3 slot = kiosPos + guestSlots[slotIndex];
                            inst.Blackboard[SymbolAction] = new AMoveTo(slot);
                            inst.Blackboard[SymbolStep] = StepMoveTo;
                            break;
                        }

                        return true;
                    }
                case StepMoveTo:
                    {
                        Action action = inst.Blackboard[SymbolAction];
                        if (action.IsComplete())
                        {
                            action = new AUseToilet();
                            inst.Blackboard[SymbolAction] = action;
                            inst.Blackboard[SymbolStep] = StepUse;
                            break;
                        }

                        if (action.Tick(agent) == EActionState.FAILED)
                        {
                            return true;
                        }

                        break;
                    }
                case StepUse:
                    {
                        Action action = inst.Blackboard[SymbolAction];
                        if (action.IsComplete())
                        {
                            inst.Target.TryReleaseClaimSlot(agent);
                            inst.Blackboard[SymbolAction] = null;
                            return true;
                        }

                        action.Tick(agent);
                        break;
                    }
            }


            return false;
        }
    }
}
