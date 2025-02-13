﻿using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GymTycoon.Code
{
    public class ClaimSlots
    {
        public Agent[] Slots;

        public ClaimSlots(int count)
        {
            Slots = new Agent[count];
        }

        public Agent this[int index]
        {
            get
            {
                return Slots[index];
            }
            private set
            {
                Slots[index] = value;
            }
        }

        public bool HasAnyClaims()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        public int FindOpenClaimSlot()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        public int TryOccupyClaimSlot(Agent agent)
        {
            int index = FindOpenClaimSlot();
            if (index == -1)
            {
                return index;
            }

            Slots[index] = agent;
            return index;
        }

        public int GetOccupiedClaimSlot(Agent agent)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] == agent)
                {
                    return i;
                }
            }

            return -1;
        }
        public bool TryReleaseClaimSlot(Agent agent)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] == agent)
                {
                    Slots[i] = null;
                    return true;
                }
            }

            return false;
        }

        public void ClearClaims()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] == null)
                {
                    continue;
                }

                Slots[i].TerminateBehavior();

                if (Slots[i] != null)
                {
                    // agent didn't leave?
                    Debug.WriteLine("Warning: A agent did not remove their claim on dynamic object");
                }
            }
        }

        public int CountOccupiedSlots()
        {
            int count = 0;
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null)
                {
                    count++;
                }
            }

            return count;
        }
    }


    public class DynamicObjectInstance
    {
        private static Queue<int> _dynamicObjectIdPool;
        private static int _maxDynamicObjectId;

        public static int ReserveDynamicObjectId()
        {
            if (_dynamicObjectIdPool == null)
            {
                _dynamicObjectIdPool = new Queue<int>();
                _maxDynamicObjectId = 0;
            }

            if (_dynamicObjectIdPool.Count > 0)
            {
                return _dynamicObjectIdPool.Dequeue();
            }

            return _maxDynamicObjectId++;
        }

        public static void ReleaseDynamicObjectId(int id)
        {
            _dynamicObjectIdPool.Enqueue(id);
        }


        public readonly DynamicObject Data;
        public readonly Dictionary<ScopedName, SpriteInstance> Sprites; // stored by local alias
        public readonly List<Behavior> Behaviors;
        public readonly int Id;

        public int Condition;
        public int WorldPosition;
        public string ActiveSpriteAlias;
        public Direction Direction;
        public int PurchaseValue = 0;
        public int[] RemainingQuantities;
        public bool Navigable;

        public ClaimSlots GuestClaimSlots;
        public ClaimSlots StaffClaimSlots;

        public bool Held;
        public Agent HeldBy;

        public bool Racked;
        public int ParentQuantityIndex = -1;
        public DynamicObjectInstance Parent;
        public List<DynamicObjectInstance> Children;


        public DynamicObjectInstance(
            DynamicObject data,
            int worldPosition,
            Direction direction,
            Dictionary<ScopedName, SpriteInstance> sprites,
            List<Behavior> behaviors
            )
        {
            Id = ReserveDynamicObjectId();
            Data = data;
            WorldPosition = worldPosition;
            Direction = direction;
            GuestClaimSlots = new ClaimSlots(data.GetNumGuestSlots());
            StaffClaimSlots = new ClaimSlots(data.GetNumStaffSlots());
            Held = false;
            HeldBy = null;
            Sprites = sprites;
            Behaviors = behaviors;
            ActiveSpriteAlias = "Default";
            Racked = false;
            Parent = null;
            Children = [];
            RemainingQuantities = [];

        }

        ~DynamicObjectInstance()
        {
            ReleaseDynamicObjectId(Id);
        }

        public override string ToString()
        {
            return $"{Id}:{Data}";
        }

        public SpriteInstance GetActiveSprite()
        {
            return Sprites[Data.SpriteAliases[ActiveSpriteAlias]];
        }

        public void SetActiveSpriteAlias(string spriteAlias)
        {
            if (!Sprites.ContainsKey(Data.SpriteAliases[ActiveSpriteAlias]))
            {
                throw new System.Exception($"Failed attempt to set active sprite alias to unknown alias '{spriteAlias}'");
            }

            ActiveSpriteAlias = spriteAlias;
        }

        public void SpawnChildren(World world)
        {
            Children.Clear();
            RemainingQuantities = new int[Data.DispensedObjects.Length];

            for (int i = 0; i < Data.DispensedObjects.Length; i++)
            {
                ScopedName name = Data.DispensedObjects[i];
                int quantity = Data.DispenseQuantity[i];
                if (quantity <= 0)
                {
                    continue;
                }

                for (int j = 0; j < quantity; j++)
                {
                    DynamicObjectInstance inst = GameInstance.Instance.Instances.InstantiateDynamicObject(name, WorldPosition, Direction, world);
                    inst.Racked = true;
                    inst.Parent = this;
                    inst.ParentQuantityIndex = i;
                    Children.Add(inst);
                }

                RemainingQuantities[i] = quantity;
            }

            foreach (var childType in Data.DispensedObjects) {
                UpdateDispenserSprite(childType);
            }
        }

        public List<AdvertisedBehavior> GetAdvertisedBehaviors()
        {
            List<AdvertisedBehavior> AdvertisedBehaviors = [];
            foreach (var behavior in Behaviors)
            {
                AdvertisedBehavior ad = new AdvertisedBehavior(
                        this,
                        behavior,
                        GameInstance.Instance.Instances.GetBehaviorScript(behavior.Script)
                    );
                AdvertisedBehaviors.Add(ad);
            }
            
            foreach (var child in Children)
            {
                if (child.Racked)
                {
                    AdvertisedBehaviors.AddRange(child.GetAdvertisedBehaviors());
                }
            }

            return AdvertisedBehaviors;
        }

        public bool CanAdvertiseBehaviors()
        {
            return Data.Category != DynamicObjectCategory.None
                && Data.Category != DynamicObjectCategory.Decoration
                && (Data.Behaviors.Length > 0 || Data.DispensedObjects.Length > 0);
        }

        public Point[] GetGuestSlots(Direction direction)
        {
            if (Racked && Parent != null)
            {
                return Parent.GetGuestSlots(direction);
            }

            return Data.GetGuestSlots(direction);
        }

        public Point[] GetStaffSlots(Direction direction)
        {
            if (Racked && Parent != null)
            {
                return Parent.GetStaffSlots(direction);
            }

            return Data.GetStaffSlots(direction);
        }

        public void AddBurst(EBurstType burstType, float life = 2.5f)
        {
            Point3 worldPos = GameInstance.Instance.World.GetPosition(WorldPosition);
            GameInstance.Instance.WorldRenderer.AddBurst(worldPos, burstType, life);
        }

        public int GetRefundValue()
        {
            return (int)MathF.Ceiling(PurchaseValue * 0.5f); // TODO: smart refund values
        }

        public int FindOpenGuestClaimSlot()
        {
            // TODO: Optimize by setting flag when adding/removing guests?
            if (Racked && Parent != null)
            {
                return Parent.FindOpenGuestClaimSlot();
            }

            return GuestClaimSlots.FindOpenClaimSlot();
        }

        public int TryOccupyGuestClaimSlot(Agent agent)
        {
            if (Racked && Parent != null)
            {
                return Parent.TryOccupyGuestClaimSlot(agent);
            }

            return GuestClaimSlots.TryOccupyClaimSlot(agent);
        }

        public int GetOccupiedClaimSlot(Agent agent)
        {
            if (Racked && Parent != null)
            {
                return Parent.GetOccupiedClaimSlot(agent);
            }

            return GuestClaimSlots.GetOccupiedClaimSlot(agent);
        }

        public bool TryReleaseGuestClaimSlot(Agent agent)
        {
            return GuestClaimSlots.TryReleaseClaimSlot(agent);
        }

        public bool HasAnyGuestClaims()
        {
            return GuestClaimSlots.HasAnyClaims();
        }

        public int FindOpenStaffClaimSlot()
        {
            // TODO: Optimize by setting flag when adding/removing guests?
            if (Racked && Parent != null)
            {
                return Parent.FindOpenStaffClaimSlot();
            }

            return StaffClaimSlots.FindOpenClaimSlot();
        }

        public int TryOccupyStaffClaimSlot(Agent agent)
        {
            if (Racked && Parent != null)
            {
                return Parent.TryOccupyStaffClaimSlot(agent);
            }

            return StaffClaimSlots.TryOccupyClaimSlot(agent);
        }

        public int GetOccupiedStaffClaimSlot(Agent agent)
        {
            if (Racked && Parent != null)
            {
                return Parent.GetOccupiedStaffClaimSlot(agent);
            }

            return StaffClaimSlots.GetOccupiedClaimSlot(agent);
        }

        public bool TryReleaseStaffClaimSlot(Agent agent)
        {
            return StaffClaimSlots.TryReleaseClaimSlot(agent);
        }

        public bool HasAnyStaffClaims()
        {
            return StaffClaimSlots.HasAnyClaims();
        }

        public int CountOccupiedStaffSlots()
        {
            if (Racked && Parent != null)
            {
                return Parent.CountOccupiedStaffSlots();
            }

            return StaffClaimSlots.CountOccupiedSlots();
        }

        public void ClearAllClaims()
        {
            GuestClaimSlots.ClearClaims();
            StaffClaimSlots.ClearClaims();
        }
        
        public void Rotate()
        {
            ClearAllClaims();
            Direction++;
            if ((int)Direction > 3)
            {
                Direction = 0;
            }

            foreach (var child in Children)
            {
                if (child.Racked)
                {
                    child.Direction = Direction;
                }
            }
        }

        public void UpdateRemainingQuantity(DynamicObjectInstance child, int delta)
        {
            int nextValue = RemainingQuantities[child.ParentQuantityIndex] + delta;
            if (nextValue < 0)
            {
                nextValue = 0;
            }

            RemainingQuantities[child.ParentQuantityIndex] = nextValue;
            UpdateDispenserSprite(child.Data.Name);
        }

        public void UpdateDispenserSprite(ScopedName childTypeName)
        {
            for (int i = 0; i < Data.DispensedObjects.Length; i++)
            {
                if (Data.DispensedObjects[i] != childTypeName)
                {
                    continue;
                }

                if (RemainingQuantities[i] <= 0)
                {
                    GetActiveSprite().HideActiveLayerSheet(childTypeName);
                }
                else
                {
                    GetActiveSprite().SetActiveLayerSheet(childTypeName, RemainingQuantities[i] - 1);
                }
            }
        }
    }
}
