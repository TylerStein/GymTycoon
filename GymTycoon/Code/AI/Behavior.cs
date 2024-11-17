using GymTycoon.Code.Data;
using System.Collections.Generic;

namespace GymTycoon.Code.AI
{
    public class BehaviorInstance
    {
        public readonly Behavior Data;
        public readonly Dictionary<string, dynamic> Blackboard;
        public readonly BehaviorScript Script;

        public DynamicObjectInstance Target;
        public Agent Owner;

        public BehaviorInstance(Behavior data, BehaviorScript script, DynamicObjectInstance target, Agent owner)
        {
            Data = data;
            Script = script;
            Owner = owner;
            Target = target;
            Blackboard = [];
        }

        public BehaviorInstance(AdvertisedBehavior ad, Agent owner) : this(ad.Behavior, ad.Script, ad.Target, owner) { }

        public void SetOwner(DynamicObjectInstance owner)
        {
            Target = owner;
        }

        public void Reset()
        {
            Script.Reset(this);
        }

        public void Release()
        {
            Script.Complete(this, Owner);
            Blackboard.Clear();
        }

        public void Pause()
        {
            Script.Paused(this, Owner);
        }

        public void Resume()
        {
            Script.Resume(this, Owner);
        }

        public bool Tick()
        {
            return Script.Tick(this, Owner);
        }

        public T TryGetBlackboardValue<T>(string key, T defaultValue)
        {
            if (Blackboard.ContainsKey(key))
            {
                return Blackboard[key];
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
