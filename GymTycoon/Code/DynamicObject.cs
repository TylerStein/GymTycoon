using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GymTycoon.Code
{

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
        public Guest[] ClaimSlots;
        public string ActiveSpriteAlias;
        public Direction Direction;
        public int PurchaseValue = 0;
        public int[] RemainingQuantities;
        public bool Navigable;

        public bool Held;
        public Guest HeldBy;

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
            ClaimSlots = new Guest[data.GetNumGuestSlots()];
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

        public bool HasAnyClaims()
        {
            for (int i = 0; i < ClaimSlots.Length; i++)
            {
                if (ClaimSlots[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        public int FindOpenClaimSlot()
        {
            // TODO: Optimize by setting flag when adding/removing guests?
            if (Racked && Parent != null)
            {
                return Parent.FindOpenClaimSlot();
            }

            for (int i = 0; i < ClaimSlots.Length; i++)
            {
                if (ClaimSlots[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        public int TryOccupyClaimSlot(Guest guest)
        {
            if (Racked && Parent != null)
            {
                return Parent.TryOccupyClaimSlot(guest);
            }

            int index = FindOpenClaimSlot();
            if (index == -1)
            {
                return index;
            }

            ClaimSlots[index] = guest;
            return index;
        }

        public int GetOccupiedClaimSlot(Guest guest)
        {
            if (Racked && Parent != null)
            {
                return Parent.GetOccupiedClaimSlot(guest);
            }

            for (int i = 0; i < ClaimSlots.Length; i++)
            {
                if (ClaimSlots[i] == guest)
                {
                    return i;
                }
            }

            return -1;
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

        public bool TryReleaseClaimSlot(Guest guest)
        {
            for (int i = 0; i < ClaimSlots.Length; i++)
            {
                if (ClaimSlots[i] == guest)
                {
                    ClaimSlots[i] = null;
                    return true;
                }
            }

            return false;
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

        public void ClearClaims()
        {
            for (int i = 0; i < ClaimSlots.Length; i++)
            {
                if (ClaimSlots[i] == null)
                {
                    continue;
                }

                ClaimSlots[i].RemoveActiveBehavior();

                if (ClaimSlots[i] != null)
                {
                    // guest didn't leave?
                    Debug.WriteLine("Warning: A guest did not remove their claim on equipment");
                }
            }
        }
        
        public void Rotate()
        {
            ClearClaims();
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
