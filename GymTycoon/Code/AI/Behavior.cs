using GymTycoon.Code.Data;
using GymTycoon.Code.AI.BehaviorTree;

namespace GymTycoon.Code.AI
{
    public class BehaviorInstance
    {
        public readonly Behavior Data;
        public readonly BehaviorScript Script;
        public readonly BehaviorTree.BehaviorTree BehaviorTree;

        public DynamicObjectInstance Target;
        public Agent Owner;

        public BehaviorInstance(Behavior data, BehaviorScript script, DynamicObjectInstance target, Agent owner)
        {
            Data = data;
            Script = script;
            Owner = owner;
            Target = target;

            BehaviorTreeBuilder builder = BehaviorTreeFactory.Create(data.Script);
            if (builder != null)
            {
                owner.Blackboard.Clear();
                owner.Blackboard.SetValue(BTBehavior.DefaultTargetKey, Target);
                BehaviorContext context = new BehaviorContext(data, owner, owner.Blackboard);
                BehaviorTree = builder.Build(context);
            }

        }

        public BehaviorInstance(AdvertisedBehavior ad, Agent owner) : this(ad.Behavior, ad.Script, ad.Target, owner) { }

        public void SetOwner(DynamicObjectInstance owner)
        {
            Target = owner;
        }

        public void Terminate()
        {
            BehaviorTree.Terminate();
        }

        public bool Tick()
        {
            BTState state = BehaviorTree.Tick();
            switch (state)
            {
                case BTState.SUCCESS:
                case BTState.FAILURE:
                    return true;
                default:
                    return false;
            }
        }

        public T TryGetBlackboardValue<T>(string key, T defaultValue)
        {
            if (Owner.Blackboard.TryGetValue(key, out T value))
            {
                return value;
            }

            return defaultValue;
        }
    }

    public class AdvertisedBehavior
    {
        public DynamicObjectInstance Target;
        public Behavior Behavior;
        public BehaviorScript Script;

        public AdvertisedBehavior(
            DynamicObjectInstance target,
            Behavior behavior,
            BehaviorScript script
            )
        {
            Target = target;
            Behavior = behavior;
            Script = script;    
        }

        public float GetUtility(Agent agent)
        {
            return Script.GetUtility(Target, Behavior, agent);
        }

        public override string ToString()
        {
            return Behavior.ToString();
        }
    }
}
