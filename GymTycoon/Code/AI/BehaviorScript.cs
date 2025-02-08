using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using System;
using System.Collections.Generic;

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
        public virtual float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            return 0;
        }

        public static bool CanBeClaimedBy(DynamicObjectInstance target, Agent agent)
        {
            if (target.Held && !agent.IsHolding(target)) return false;
            if (target.GuestClaimSlots[0] != null && target.GuestClaimSlots[0] != agent) return false;
            return true;
        }

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
            if (target.FindOpenGuestClaimSlot() == -1)
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
        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            return 0f;
        }
    }

    public class BLeave : BehaviorScript
    {
        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            if (agent.Happiness <= Agent.MinHappiness || agent.RemainingStayTime < 1)
            {
                return float.MaxValue;
            }


            return agent.AverageNeeds * -1f;
            // return float.MinValue;
        }
    }

    public class BBasicExercise : BehaviorScript
    {
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
    }

    public class BReturnToDispenser : BehaviorScript
    {
        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            if (target.Parent == null || target.Racked == true || target.HasAnyGuestClaims())
            {
                return float.MinValue;
            }

            float pickUpDistancePenalty = DistancePenalty(agent, target.WorldPosition);
            float deliverDistancePenalty = DistancePenalty(target.WorldPosition, target.Parent.WorldPosition);
            return pickUpDistancePenalty * deliverDistancePenalty; // TODO: Agent traits for cleanup behavior? re-use Dispose?
        }
    }

    public class BCheckIn : BehaviorScript
    {
        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            // TODO: Queue penalty, is staffed penalty
            if (agent.HasCheckedIn)
            {
                return float.MinValue;
            }

            return float.MaxValue * DistancePenalty(agent, target.WorldPosition) * QueuePenalty(target);
        }
    }

    public class BDumbellRack : BehaviorScript
    {
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
    }

    public class BToilet : BehaviorScript
    {
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
    }

    public class BUseVendingMachine : BehaviorScript
    {
        public override float GetUtility(DynamicObjectInstance target, Behavior behavior, Agent agent)
        {
            if (!agent.HasCheckedIn)
            {
                return float.MinValue;
            }

            if (agent.Needs["Thirst"] < 50)
            {
                return float.MinValue;
            }

            return NeedUrgency(agent, "Thirst") * QueuePenalty(target) * DistancePenalty(agent, target.WorldPosition);
        }
    }
}
