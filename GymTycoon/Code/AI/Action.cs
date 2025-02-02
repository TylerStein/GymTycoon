using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
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
        public EActionState Tick(Agent agent);
        public bool IsValid();
        public bool IsComplete();
    }

    public abstract class Action : IAction
    {
        protected bool _isValid { get; set; }
        protected bool _isComplete { get; set; }
        public abstract EActionState Tick(Agent agent);
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

        public override EActionState Tick(Agent agent)
        {
            agent.Sprite.SetActiveLayerSheet(new ScopedName("Default"), new ScopedName("Walk"));
            if (agent.NavigateTo(Destination))
            {
                _isValid = true;
                _isComplete = true;
                return EActionState.SUCCESS;
            }
            
            if (agent.HasDestination)
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

        public APickUp(Agent agent, DynamicObjectInstance target)
        {
            Target = target;
            _isValid = target.Data.Holdable && !agent.IsHolding(target) && target.Held == false;
        }

        public override EActionState Tick(Agent agent)
        {
            agent.AnimateIdle();
            if (Target.Held)
            {
                if (agent.IsHolding(Target))
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
                Target.Parent.TryReleaseGuestClaimSlot(agent);
            }
            else
            {
                GameInstance.Instance.World.RemoveDynamicObject(Target);

            }

            Target.Held = true;
            agent.AddHeldObj(Target);
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

        public APutDown(Agent agent, DynamicObjectInstance target, bool rackAtDestination = false)
        {
            Target = target;
            _isValid = agent.IsHolding(target);
            RackAtDestination = rackAtDestination;
        }

        public override EActionState Tick(Agent agent)
        {
            agent.AnimateIdle();
            _isValid = agent.RemoveHeldObj(Target);
            if (_isValid)
            {
                if (RackAtDestination && Target.Parent != null)
                {
                    Target.WorldPosition = Target.Parent.WorldPosition;
                    Target.Racked = true;
                    Target.Parent.UpdateRemainingQuantity(Target, 1);
                    Target.Parent.TryReleaseGuestClaimSlot(agent);
                }
                else
                {
                    Target.WorldPosition = agent.WorldPosition;
                    GameInstance.Instance.World.AddDynamicObject(Target);
                    Target.WorldPosition = agent.WorldPosition;
                }

                Target.Held = false;
                Target.TryReleaseGuestClaimSlot(agent);
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

        public AExercise(Exercise exercise, float modifier = 1f)
        {
            Exercise = exercise;
            EquipmentModifier = modifier;
            _isValid = true;
            _isComplete = false;
            SpriteIndex = Random.Shared.Next(0, exercise.Sprites.Length - 1);
        }

        public override EActionState Tick(Agent agent)
        {
            // TODO: Pick an exercise, increase needs
            if (countdown > 0)
            {
                countdown--;
                agent.Sprite.SetActiveLayerSheet(new ScopedName("Default"), Exercise.Sprites[SpriteIndex]);

                foreach (var kvp in Exercise.NeedModifiers)
                {
                    if (agent.Needs.HasNeed(kvp.Key))
                    {
                        int delta = (int)(Exercise.NeedModifiers[kvp.Key] * EquipmentModifier);
                        agent.Happiness += delta;
                        agent.Needs.AddValue(kvp.Key, -delta);
                        agent.Needs.AddValue("Rest", delta);
                    }
                }

                return EActionState.WAITING;
            }

            agent.AnimateIdle();
            agent.AddBurst(EBurstType.Fitness);
            foreach (var kvp in Exercise.NeedModifiers)
            {
                agent.AddExperience(kvp.Key, 1);
            }

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

        public override EActionState Tick(Agent agent)
        {
            agent.AnimateIdle();
            if (countdown > 0)
            {
                countdown--;

                // TODO: Check if equipment is still valid

                return EActionState.WAITING;
            }

            agent.HasCheckedIn = true;
            agent.Happiness += 100;
            if (agent.OffscreenAgent.LifetimeVisits <= 1)
            {
                agent.AddBurst(EBurstType.Money);
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

        public override EActionState Tick(Agent agent)
        {
            agent.AnimateIdle();
            agent.Needs.AddValue("Toilet", -25);
            agent.Happiness += 25;

            if (agent.Needs["Toilet"] <= 0)
            {
                agent.Needs["Toilet"] = 0;
                _isComplete = true;
                return EActionState.SUCCESS;
            }

            return EActionState.WAITING;
        }
    }

    public class ASaffStation : Action
    {
        int countdown = 10;
        public DynamicObjectInstance Target = null;

        public ASaffStation(DynamicObjectInstance target)
        {
            _isValid = true;
            _isComplete = false;
            Target = target;
        }

        public override EActionState Tick(Agent agent)
        {
            // TODO: How to set an object as being staffed so guests can interact?

            _isValid = (agent is Staff);
            if (!_isValid)
            {
                return EActionState.FAILED;
            }

            agent.AnimateIdle();
            countdown--;
            if (countdown <= 0)
            {
                return EActionState.SUCCESS;
            }

            return EActionState.WAITING;
        }
    }
}
