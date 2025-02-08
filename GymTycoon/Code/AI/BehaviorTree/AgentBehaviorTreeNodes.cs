using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code.AI.BehaviorTree
{
    /// <summary>
    /// <b>Behavior:</b> Try to occupy a claim slot
    /// </br/>
    /// <b>Reads</b>: TargetDynamicObjectInstance
    /// <br/>
    /// <b>Sets</b>: TargetClaimSlotIndex
    /// </summary>
    public class BTClaimSlot : BTBehavior
    {
        private string targetKey;
        private string indexKey;

        public BTClaimSlot(string target = DefaultTargetKey, string index = DefaultIndexKey)
        {
            targetKey = target;
            indexKey = index;
        }

        protected override BTState Update()
        {
            DynamicObjectInstance instance;
            if (Blackboard.TryGetValue(targetKey, out instance))
            {
                int slotIndex = instance.TryOccupyGuestClaimSlot(Agent);
                if (slotIndex == -1)
                {
                    slotIndex = instance.GetOccupiedClaimSlot(Agent);
                }

                if (slotIndex != -1)
                {
                    Blackboard.SetValue(indexKey, slotIndex);
                    return BTState.SUCCESS;
                }
            }

            return BTState.FAILURE;
        }
    }

    /// <summary>
    /// <b>Behavior:</b> Try to un-occupy a claim slot
    /// </br/>
    /// <b>Reads</b>: TargetDynamicObjectInstance
    /// <br/>
    /// <b>Clears</b>: TargetClaimSlotIndex
    /// </summary>
    public class BTReleaseSlot : BTBehavior
    {
        private string targetKey;
        private string indexKey;

        public BTReleaseSlot(string target = DefaultTargetKey, string index = DefaultIndexKey)
        {
            targetKey = target;
            indexKey = index;
        }

        protected override BTState Update()
        {
            DynamicObjectInstance instance;
            if (Blackboard.TryGetValue(targetKey, out instance))
            {
                if (instance.TryReleaseGuestClaimSlot(Agent))
                {
                    Blackboard.ClearValue(indexKey);
                    return BTState.SUCCESS;
                }
            }

            return BTState.FAILURE;
        }
    }

    public class BTCheckIn : BTBehavior
    {
        private const string checkInCounterKey = "CheckInCounter";

        protected override void Initialize()
        {
            Blackboard.SetValue(checkInCounterKey, 10);
            base.Initialize();
        }

        protected override BTState Update()
        {
            Agent.AnimateIdle();
            if (Agent.HasCheckedIn)
            {
                return BTState.SUCCESS;
            }

            if (Blackboard.TryGetValue(checkInCounterKey, out int counter))
            {
                if (counter > 0)
                {
                    counter--;
                    Blackboard.SetValue(checkInCounterKey, counter);
                    return BTState.RUNNING;
                }

                Agent.HasCheckedIn = true;
                Agent.Happiness += 100;
                if (Agent.OffscreenAgent.LifetimeVisits <= 1)
                {
                    Agent.AddBurst(EBurstType.Money);
                }

                return BTState.SUCCESS;
            }

            return BTState.FAILURE;
        }
    }

    public class BTMoveTo : BTBehavior
    {
        private string moveToKey;

        public BTMoveTo(string moveTo = DefaultMoveToKey)
        {
            moveToKey = moveTo;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override BTState Update()
        {
            if (Blackboard.TryGetValue(moveToKey, out Point3 destination))
            {
                Agent.Sprite.SetActiveLayerSheet(new ScopedName("Default"), new ScopedName("Walk"));
                if (Agent.NavigateTo(destination))
                {
                    Blackboard.ClearValue(moveToKey);
                    return BTState.SUCCESS;
                }

                if (Agent.HasDestination)
                {
                    return BTState.RUNNING;
                }
            }

            return BTState.FAILURE;
        }

        protected override void Terminate(BTState status)
        {
            base.Terminate(status);
        }
    }

    public class BTSelectTargetSlotAsDestination : BTBehavior
    {
        private string targetKey;
        private string indexKey;
        private string moveToKey;

        public BTSelectTargetSlotAsDestination(
            string target = DefaultTargetKey,
            string index = DefaultIndexKey,
            string moveTo = DefaultMoveToKey
        )
        {
            targetKey = target;
            indexKey = index;
            moveToKey = moveTo;
        }

        protected override BTState Update()
        {
            if (Blackboard.TryGetValue(targetKey, out DynamicObjectInstance inst))
            {
                if (Blackboard.TryGetValue(indexKey, out int slotIndex))
                {
                    Point3 pos = GameInstance.Instance.World.GetPosition(inst.WorldPosition);
                    Point[] slots = inst.GetGuestSlots(inst.Direction);
                    Point3 slot = pos + slots[slotIndex];
                    Blackboard.SetValue(moveToKey, slot);
                    return BTState.SUCCESS;
                }
            }

            return BTState.FAILURE;
        }
    }

    public class BTSelectParentAs : BTBehavior
    {
        private string targetKey;
        private string parentKey;

        public BTSelectParentAs(string target = DefaultTargetKey, string parent = DefaultParentKey)
        {
            targetKey = target;
            parentKey = parent;
        }

        protected override BTState Update()
        {
            if (Blackboard.TryGetValue(targetKey, out DynamicObjectInstance inst))
            {
                if (inst.Parent != null)
                {
                    Blackboard.SetValue(parentKey, inst.Parent);
                    return BTState.SUCCESS;
                }
            }

            return BTState.FAILURE;
        }
    }

    public class BTSelectDesirableExerciseLocation : BTBehavior
    {
        private string moveToKey;

        public BTSelectDesirableExerciseLocation(string moveTo = DefaultMoveToKey)
        {
            moveToKey = moveTo;
        }

        protected override BTState Update()
        {
            List<int> destinations = GameInstance.Instance.World.FindTilesWithProperties(TileProperties.Navigable)
                .Where(GameInstance.Instance.CanExerciseOnTile)
                .ToList();

            if (destinations.Count == 0)
            {
                return BTState.FAILURE;
            }

            int index = Random.Shared.Next(destinations.Count);
            Point3 pos = GameInstance.Instance.World.GetPosition(destinations[index]);
            Blackboard.SetValue(moveToKey, pos);
            return BTState.SUCCESS;
        }
    }

    public class BTFindRandomDestination : BTBehavior
    {
        private string moveToKey;
        private TileProperties tileProperties;
        public BTFindRandomDestination(TileProperties prop = TileProperties.Navigable, string moveTo = DefaultMoveToKey)
        {
            tileProperties = prop;
            moveToKey = moveTo;
        }

        protected override void Initialize()
        {
            Blackboard.ClearValue(moveToKey);
            base.Initialize();
        }

        protected override BTState Update()
        {
            List<int> destinations = GameInstance.Instance.World.FindTilesWithProperties(tileProperties);
            if (!Agent.HasCheckedIn)
            {
                destinations = destinations.Where(GameInstance.Instance.CanNavigatePreCheckInOnTile).ToList();
            }

            if (destinations.Count == 0)
            {
                return BTState.FAILURE;
            }

            int index = Random.Shared.Next(destinations.Count);
            Point3 pos = GameInstance.Instance.World.GetPosition(destinations[index]);
            Blackboard.SetValue(moveToKey, pos);
            return BTState.SUCCESS;
        }
    }

    public class BTIsHoldable : BTBehavior
    {
        private string targetKey;

        public BTIsHoldable(string target = DefaultTargetKey)
        {
            targetKey = target;
        }

        protected override BTState Update()
        {
            if (Blackboard.TryGetValue(targetKey, out DynamicObjectInstance inst))
            {
                if (!inst.Data.Holdable)
                {
                    return BTState.FAILURE;
                }

                return BTState.SUCCESS;
            }

            return BTState.FAILURE;
        }
    }

    public class BTPickUp : BTBehavior
    {
        private string targetKey;

        public BTPickUp(string target = DefaultTargetKey)
        {
            targetKey = target;
        }

        protected override BTState Update()
        {
            Agent.AnimateIdle();
            if (Blackboard.TryGetValue(targetKey, out DynamicObjectInstance inst))
            {
                if (inst.Held)
                {
                    if (Agent.IsHolding(inst))
                    {
                        return BTState.SUCCESS;
                    }

                    return BTState.FAILURE;
                }

                if (!inst.Data.Holdable)
                {
                    return BTState.FAILURE;
                }

                if (inst.Racked && inst.Parent != null)
                {
                    inst.Racked = false;
                    inst.Parent.UpdateRemainingQuantity(inst, -1);
                    inst.Parent.TryReleaseGuestClaimSlot(Agent);
                }
                else
                {
                    GameInstance.Instance.World.RemoveDynamicObject(inst);

                }

                inst.Held = true;
                Agent.AddHeldObj(inst);
                return BTState.SUCCESS;
            }

            return BTState.FAILURE;
        }
    }

    public class BTPutDown : BTBehavior
    {
        public enum RackBehavior
        {
            RACK,
            NO_RACK
        }

        private string targetKey;
        private bool rack;

        public BTPutDown(RackBehavior rackBehavior = RackBehavior.NO_RACK, string target = DefaultTargetKey)
        {
            targetKey = target;
            rack = rackBehavior == RackBehavior.RACK;
        }

        protected override BTState Update()
        {
            if (Blackboard.TryGetValue(targetKey, out DynamicObjectInstance inst))
            {
                Agent.AnimateIdle();
                bool isValid = Agent.RemoveHeldObj(inst);
                if (isValid)
                {
                    if (rack && inst.Parent != null)
                    {
                        inst.WorldPosition = inst.Parent.WorldPosition;
                        inst.Racked = true;
                        inst.Parent.UpdateRemainingQuantity(inst, 1);
                        inst.Parent.TryReleaseGuestClaimSlot(Agent);
                    }
                    else
                    {
                        inst.WorldPosition = Agent.WorldPosition;
                        GameInstance.Instance.World.AddDynamicObject(inst);
                        inst.WorldPosition = Agent.WorldPosition;
                    }

                    inst.Held = false;
                    inst.TryReleaseGuestClaimSlot(Agent);
                    return BTState.SUCCESS;
                }
            }

            return BTState.FAILURE;
        }
    }

    public class BTExercise : BTBehavior
    {
        private const string CountdownKey = "ExerciseCountdown";
        private const int CountdownDefault = 20;
        private const float EquipmentModifierDefault = 1f;
        private const string EquipmentModifierKey = "EquipmentModifier";
        private const string ExerciseSpriteIndexKey = "ExerciseSpriteIndex";
        private const string ExerciseKey = "Exercise";

        public BTExercise() { }

        protected override void Initialize()
        {
            Blackboard.ClearValue(CountdownKey);
        }

        protected override BTState Update()
        {
            Exercise exercise = Context.Behavior.Exercise;
            if (exercise != null && Blackboard.TryGetValue(DefaultTargetKey, out DynamicObjectInstance target))
            {
                float modifier = target.Data.FitnessModifier;

                int countdown = Blackboard.GetValueWithDefault(CountdownKey, CountdownDefault);
                if (!Blackboard.TryGetValue(ExerciseSpriteIndexKey, out int spriteIndex))
                {
                    spriteIndex = Random.Shared.Next(0, exercise.Sprites.Length - 1);
                    Blackboard.SetValue(ExerciseSpriteIndexKey, spriteIndex);
                }

                countdown--;

                Agent.Sprite.SetActiveLayerSheet(new ScopedName("Default"), exercise.Sprites[spriteIndex]);
                Blackboard.SetValue(CountdownKey, countdown);

                foreach (var kvp in exercise.NeedModifiers)
                {
                    if (Agent.Needs.HasNeed(kvp.Key))
                    {
                        int delta = (int)(exercise.NeedModifiers[kvp.Key] * modifier);
                        Agent.Happiness += delta;
                        Agent.Needs.AddValue(kvp.Key, -delta);
                        Agent.Needs.AddValue("Rest", delta);
                    }
                }

                if (countdown > 0)
                {
                    return BTState.RUNNING;
                }

                Agent.AnimateIdle();
                Agent.AddBurst(EBurstType.Fitness);
                foreach (var kvp in exercise.NeedModifiers)
                {
                    Agent.AddExperience(kvp.Key, 1);
                }

                return BTState.SUCCESS;
            }

            return BTState.FAILURE;
        }
    }

    public class BTIsHoldingObject : BTDecorator
    {
        public BTIsHoldingObject() { }

        protected override BTState Update()
        {
            if (Agent.HeldObjects.Count == 0)
            {
                return BTState.SUCCESS;
            }

            return Child.Tick();
        }
    }

    public class BTSelectFirstHeldObject : BTBehavior
    {
        private string heldObjectKey;

        public BTSelectFirstHeldObject(string heldObject = DefaultHeldObjectKey)
        {
            heldObjectKey = heldObject;
        }

        protected override BTState Update()
        {
            if (Agent.HeldObjects.Count == 0)
            {
                return BTState.FAILURE;
            }

            DynamicObjectInstance inst = Agent.HeldObjects.First();
            Blackboard.SetValue(heldObjectKey, inst);
            return BTState.SUCCESS;
        }
    }

    public class BTFindHeldObjectDropPoint : BTBehavior
    {
        private string heldObjectKey;
        private string moveToKey;

        public BTFindHeldObjectDropPoint(string heldObject = DefaultHeldObjectKey, string moveTo = DefaultMoveToKey)
        {
            heldObjectKey = heldObject;
            moveToKey = moveTo;
        }

        protected override BTState Update()
        {
            if (Blackboard.TryGetValue(heldObjectKey, out DynamicObjectInstance inst))
            {
                Point3 guestPos = GameInstance.Instance.World.GetPosition(Agent.WorldPosition);
                int worldIndex;
                if (BehaviorScript.FindClosestTileWithProperties(guestPos, TileProperties.Navigable, out worldIndex))
                {
                    Point3 worldPos = GameInstance.Instance.World.GetPosition(worldIndex);
                    Blackboard.SetValue(moveToKey, worldPos);
                    return BTState.SUCCESS;
                }
            }

            return BTState.FAILURE;
        }
    }

    public class BTExit : BTBehavior
    {
        public BTExit() { }
        protected override BTState Update()
        {
            Agent.PendingRemoval = true;
            return BTState.SUCCESS;
        }
    }

    public class BTUseToilet : BTBehavior
    {
        protected override BTState Update()
        {
            Agent.AnimateIdle();
            Agent.Needs.AddValue("Toilet", -25);
            Agent.Happiness += 25;
            if (Agent.Needs["Toilet"] <= 0)
            {
                Agent.Needs["Toilet"] = 0;
                return BTState.SUCCESS;
            }

            return BTState.RUNNING;
        }
    }

    public class BTUseVendingMachine : BTBehavior
    {
        private string targetKey;

        public BTUseVendingMachine(string target = DefaultTargetKey)
        {
            targetKey = target;
        }

        protected override BTState Update()
        {
            if (Blackboard.TryGetValue(targetKey, out DynamicObjectInstance inst))
            {
                // TODO: Prompt vendor to dispense child direct to guest held state
            }

            return BTState.FAILURE;
        }
    }

    public class BTConsumeHeldItem : BTBehavior
    {
        private string targetKey;

        public BTConsumeHeldItem(string target = DefaultTargetKey)
        {
            targetKey = target;
        }

        protected override BTState Update()
        {
            if (Blackboard.TryGetValue(targetKey, out DynamicObjectInstance inst))
            {
                // TODO: Consume some of the held item, if it's empty, optionally leave garbage behind
            }

            return BTState.FAILURE;
        }
    }
}