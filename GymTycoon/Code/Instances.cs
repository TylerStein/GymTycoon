using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using System.Collections.Generic;

namespace GymTycoon.Code
{
    /// <summary>
    /// The Instances class is responsible for creating and storing instances of various types.
    /// It manages object pooling and shared instance optimizations.
    /// A Resources class must be provided to source the data types that instances are based on.
    /// </summary>
    public class Instances
    {

        private readonly Dictionary<ScopedName, SpriteInstance> _sharedSpriteInstances = [];
        private readonly List<DynamicObjectInstance> _dynamicObjects;
        private readonly Resources _resources;

        public Instances(Resources resources)
        {
            _resources = resources;
        }

        public SpriteInstance InstantiateSprite(Sprite spriteType)
        {
            if (spriteType.Shared)
            {
                SpriteInstance inst;
                if (_sharedSpriteInstances.TryGetValue(spriteType.Name, out inst))
                {
                    return inst;
                }

                inst = new SpriteInstance(spriteType);
                _sharedSpriteInstances[spriteType.Name] = inst;
                return inst;
            }

            return new SpriteInstance(spriteType);
        }

        public SpriteInstance InstantiateSprite(ScopedName spriteName)
        {
            Sprite type = _resources.GetSprite(spriteName);
            if (type == null)
            {
                throw new System.Exception($"Failed to instantiate unknown sprite '{spriteName}'");
            }

            return InstantiateSprite(type);
        }

        public DynamicObjectInstance InstantiateDynamicObject(DynamicObject dynamicObjectType, int position, Direction direction, World world)
        {
            Dictionary<ScopedName, SpriteInstance> spriteInstances = [];
            foreach (var item in dynamicObjectType.SpriteAliases)
            {
                SpriteInstance spriteInst = InstantiateSprite(item.Value);
                spriteInstances[item.Value] = spriteInst;
            }

            List<Behavior> behaviors = [];
            foreach (ScopedName behaviorName in dynamicObjectType.Behaviors)
            {
                behaviors.Add(GetBehavior(behaviorName));
            }

            var objInst = new DynamicObjectInstance(dynamicObjectType, position, direction, spriteInstances, behaviors);
            objInst.SpawnChildren(world);
            return objInst;
        }

        public DynamicObjectInstance InstantiateDynamicObject(ScopedName dynamicObjectName, int position, Direction direction, World world)
        {
            DynamicObject type = _resources.GetDynamicObjectType(dynamicObjectName);
            if (type == null)
            {
                throw new System.Exception($"Failed to instantiate unknown dynamic object '{dynamicObjectName}'");
            }

            return InstantiateDynamicObject(type, position, direction, world);
        }

        public BehaviorScript GetBehaviorScript(ScopedName scriptName)
        {
            BehaviorScript script = BehaviorFactory.Create(scriptName);
            if (script == null)
            {
                throw new System.Exception($"Failed to instantiate unknown behavior script '{scriptName}'");
            }

            return script;
        }

        public Behavior GetBehavior(ScopedName behaviorName)
        {
            Behavior behavior = _resources.GetBehavior(behaviorName);
            if (behavior == null)
            {
                throw new System.Exception($"Failed to instantiate unknown behavior '{behaviorName}'");
            }

            return behavior;
        }

        public AdvertisedBehavior GetAdvertisedBehavior(ScopedName behaviorName, DynamicObjectInstance inst = null)
        {
            Behavior behavior = GetBehavior(behaviorName);
            AdvertisedBehavior ad = new AdvertisedBehavior(inst, behavior, GetBehaviorScript(behavior.Script));
            return ad;

        }
    }
}
