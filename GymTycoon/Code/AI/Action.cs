using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using ImGuiNET;
using Microsoft.Xna.Framework;
using System;

namespace GymTycoon.Code.AI
{
    public enum EActionState
    {
        SUCCESS = 0,
        WAITING = 1,
        FAILED = 2,
    }

    public interface IAction
    {
        public EActionState Tick(Guest guest);
        public bool IsValid();
        public bool IsComplete();
    }

    public abstract class Action : IAction
    {
        protected bool _isValid { get; set; }
        protected bool _isComplete { get; set; }
        public abstract EActionState Tick(Guest guest);
        public bool IsValid() => _isValid;
        public bool IsComplete() => _isComplete;
    }

    /// <summary>
    /// Given a destination, agent attempts to move to it.
    /// </summary>
    public class AMoveTo : Action
    {
        public Point3 Destination;

        public AMoveTo(Point3 destination)
        {
            Destination = destination;
        }

        public override EActionState Tick(Guest guest)
        {
            guest.Sprite.SetActiveLayerSheet(new ScopedName("Default"), new ScopedName("Walk"));
            if (guest.NavigateTo(Destination))
            {
                _isValid = true;
                _isComplete = true;
                return EActionState.SUCCESS;
            }
            
            if (guest.HasDestination)
            {
                _isValid = true;
                _isComplete = false;
                return EActionState.WAITING;
            }

            _isValid = false;
            _isComplete = false;
            return EActionState.FAILED;
        }
    }

    /// <summary>
    /// Put a DynamicObject in to an Agent's inventory if it is not already Held.
    /// </summary>
    public class APickUp : Action
    {
        public DynamicObjectInstance Target = null;

        public APickUp(Guest guest, DynamicObjectInstance target)
        {
            Target = target;
            _isValid = target.Data.Holdable && !guest.IsHolding(target) && target.Held == false;
        }

        public override EActionState Tick(Guest guest)
        {
            guest.AnimateIdle();
            if (Target.Held)
            {
                if (guest.IsHolding(Target))
                {
                    _isValid = true;
                    _isComplete = true;
                    return EActionState.SUCCESS;
                }

                _isValid = false;
                return EActionState.FAILED;
            }

            if (Target.Racked && Target.Parent != null)
            {
                Target.Racked = false;
                Target.Parent.UpdateRemainingQuantity(Target, -1);
                Target.Parent.TryReleaseClaimSlot(guest);
            }
            else
            {
                GameInstance.Instance.World.RemoveDynamicObject(Target);

            }

            Target.Held = true;
            guest.AddHeldObj(Target);
            _isComplete = true;
            return EActionState.SUCCESS;
        }
    }

    /// <summary>
    /// Remove a DynamicObject from an Agent's inventory if it is Held and place it in the world where the Agent is.
    /// </summary>
    public class APutDown : Action
    {
        public DynamicObjectInstance Target = null;
        public bool RackAtDestination = false;

        public APutDown(Guest guest, DynamicObjectInstance target, bool rackAtDestination = false)
        {
            Target = target;
            _isValid = guest.IsHolding(target);
            RackAtDestination = rackAtDestination;
        }

        public override EActionState Tick(Guest guest)
        {
            guest.AnimateIdle();
            _isValid = guest.RemoveHeldObj(Target);
            if (_isValid)
            {
                if (RackAtDestination && Target.Parent != null)
                {
                    Target.WorldPosition = Target.Parent.WorldPosition;
                    Target.Racked = true;
                    Target.Parent.UpdateRemainingQuantity(Target, 1);
                }
                else
                {
                    Target.WorldPosition = guest.WorldPosition;
                    GameInstance.Instance.World.AddDynamicObject(Target);
                    Target.WorldPosition = guest.WorldPosition;
                }

                Target.Held = false;
                _isComplete = true;
                return EActionState.SUCCESS;
            }

            return EActionState.FAILED;
        }
    }

    public class AExercise : Action
    {
        public Exercise Exercise;
        public float EquipmentModifier;
        public int SpriteIndex = 0;

        // TODO: Real exercise logic
        int countdown = 20;

        public AExercise(Exercise exercise, float modifier)
        {
            Exercise = exercise;
            EquipmentModifier = modifier;
            _isValid = true;
            _isComplete = false;
            SpriteIndex = Random.Shared.Next(0, exercise.Sprites.Length - 1);
        }

        public override EActionState Tick(Guest guest)
        {
            // TODO: Pick an exercise, increase needs
            if (countdown > 0)
            {
                countdown--;
                guest.Sprite.SetActiveLayerSheet(new ScopedName("Default"), Exercise.Sprites[SpriteIndex]);

                foreach (ExerciseProperties prop in Enum.GetValues(typeof(ExerciseProperties)))
                {
                    if (prop != ExerciseProperties.None && prop != ExerciseProperties.All && Exercise.AvailableExerciseProperties.HasFlag(prop))
                    {
                        guest.ModifyExerciseNeed(prop, Exercise.Fitness[prop] * EquipmentModifier);
                    }
                }

                return EActionState.WAITING;
            }

            guest.AnimateIdle();
            guest.AddBurst(EBurstType.Fitness);
            _isComplete = true;
            return EActionState.SUCCESS;
        }
    }

    public class ACheckIn : Action
    {
        // TODO: Real timing logic
        int countdown = 10;

        public ACheckIn()
        {
            _isValid = true;
            _isComplete = false;
        }

        public override EActionState Tick(Guest guest)
        {
            guest.AnimateIdle();
            if (countdown > 0)
            {
                countdown--;

                // TODO: Check if equipment is still valid

                return EActionState.WAITING;
            }

            guest.HasCheckedIn = true;
            if (guest.OffscreenGuest.LifetimeVisits <= 1)
            {
                guest.AddBurst(EBurstType.Money);
            }

            _isComplete = true;
            return EActionState.SUCCESS;
        }
    }

    public class AUseToilet : Action
    {
        public AUseToilet()
        {
            _isValid = true;
            _isComplete = false;
        }

        public override EActionState Tick(Guest guest)
        {
            guest.AnimateIdle();
            guest.ModifyNeed(NeedType.Toilet, 25);
            if (guest.NeedsValue[NeedType.Toilet] >= Guest.MaxNeed)
            {
                _isComplete = true;
                return EActionState.SUCCESS;
            }

            return EActionState.WAITING;
        }
    }
}
