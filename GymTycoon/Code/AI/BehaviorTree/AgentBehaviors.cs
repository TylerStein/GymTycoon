using GymTycoon.Code.Common;
using System;
using System.Collections.Generic;

namespace GymTycoon.Code.AI.BehaviorTree
{
    public static class BehaviorTreeFactory
    {
        private static Dictionary<ScopedName, BehaviorTreeBuilder> _cachedBehaviorTreeBuilders = new();

        public static Dictionary<ScopedName, BehaviorTreeBuilder> AllBehaviorTreeTypes = new()
        {
            { new ScopedName("Script.Default.Wander"), CreateWander() },
            { new ScopedName("Script.Default.Leave"), CreateLeave() },
            { new ScopedName("Script.Default.ExerciseWithPortableMat"), CreateBasicExercise() },
            { new ScopedName("Script.Default.RunWithTreadmill"), CreateBasicExercise() },
            { new ScopedName("Script.Default.Reception"), CreateCheckIn() },
            { new ScopedName("Script.Default.DumbellCurl"), CreateBasicExercise() },
            { new ScopedName("Script.Default.ReturnToDispenser"), CreateReturnToDispenser() },
            { new ScopedName("Script.Default.Toilet"), CreateUseToilet() },
            { new ScopedName("Script.Default.VendingMachine"), CreateUseVendingMachine() },
            { new ScopedName("Script.Default.ConsumeHeldItem"), CreateConsumeHeldItem() }
        };

        public static BehaviorTreeBuilder Create(ScopedName name)
        {
            BehaviorTreeBuilder builder;
            if (_cachedBehaviorTreeBuilders.TryGetValue(name, out builder))
            {
                return builder;
            }

            if (AllBehaviorTreeTypes.TryGetValue(name, out builder))
            {
                _cachedBehaviorTreeBuilders[name] = builder;
                return builder;
            }

            return null;
        }

        public static BehaviorTreeBuilder CreateWander()
        {
            BehaviorTreeBuilder builder = new BehaviorTreeBuilder();
            return builder.Composite(() => new BTSequence())
                .Node(() => new BTFindRandomDestination())
                .Node(() => new BTMoveTo())
            .End();
        }

        public static BehaviorTreeBuilder CreateDropHeldObject()
        {
            BehaviorTreeBuilder builder = new BehaviorTreeBuilder();
            return builder.Decorator(() =>new BTIsHoldingObject())
                .Composite(() =>new BTSequence())
                    .Decorator(() => new BTRetry(10))
                        .Node(() =>new BTSelectFirstHeldObject())
                    .Node(() =>new BTFindHeldObjectDropPoint())
                    .Node(() =>new BTMoveTo())
                    .Node(() =>new BTPutDown())
                .End()
            .End();
        }

        public static BehaviorTreeBuilder CreateLeave()
        {
            BehaviorTreeBuilder builder = new BehaviorTreeBuilder();
            return builder.Composite(() => new BTSequence())
                .Node(() => new BTBehaviorTreeWrapper(CreateDropHeldObject()))
                .Node(() => new BTFindRandomDestination(TileProperties.Spawn))
                .Node(() => new BTMoveTo())
                .Node(() => new BTExit())
            .End();
        }

        public static BehaviorTreeBuilder CreateBasicExercise()
        {
            BehaviorTreeBuilder builder = new BehaviorTreeBuilder();
            return builder.Composite(() => new BTAll())
                .Composite(() => new BTSequence())
                    .Node(() => new BTClaimSlot())
                    .Node(() => new BTSelectTargetSlotAsDestination())
                    .Node(() => new BTMoveTo())

                    // Branch for holdable objects
                    .Composite(() => new BTIfThenElse())
                        // IF Holdable
                        .Node(() => new BTIsHoldable())

                        // THEN Pick Up / Move / Place
                        .Composite(() => new BTSequence())
                            .Node(() => new BTPickUp())
                            .Node(() => new BTSelectDesirableExerciseLocation())
                            .Node(() => new BTMoveTo())
                            .Node(() => new BTPutDown())
                        .End()
                    .End()

                    // Finally exercise
                    .Node(() => new BTExercise())
                .End()

                // Cleanup
                .Composite(() => new BTAll())
                    .Node(() => new BTPutDown())
                    .Node(() => new BTReleaseSlot())
                .End()
            .End();
        }

        public static BehaviorTreeBuilder CreateCheckIn()
        {
            BehaviorTreeBuilder builder = new BehaviorTreeBuilder();
            return builder.Composite(() => new BTAll())
                .Composite(() => new BTSequence())
                    .Node(() => new BTClaimSlot())
                    .Node(() => new BTSelectTargetSlotAsDestination())
                    .Node(() => new BTMoveTo())
                    .Node(() => new BTCheckIn())
                .End()

                // Cleanup
                .Composite(() => new BTAll())
                    .Node(() => new BTReleaseSlot())
                .End()
            .End();
        }

        public static BehaviorTreeBuilder CreateReturnToDispenser()
        {
            BehaviorTreeBuilder builder = new BehaviorTreeBuilder();
            return builder.Composite(() => new BTSequence())
                .Node(() => new BTClaimSlot())
                .Node(() => new BTSelectTargetSlotAsDestination())
                .Node(() => new BTMoveTo())
                .Node(() => new BTPickUp())
                .Node(() => new BTSelectParentAs(BTBehavior.DefaultTargetKey, BTBehavior.DefaultParentKey))
                .Node(() => new BTClaimSlot(BTBehavior.DefaultParentKey))
                .Node(() => new BTSelectTargetSlotAsDestination(BTBehavior.DefaultParentKey))
                .Node(() => new BTMoveTo())
                .Node(() => new BTPutDown(BTPutDown.RackBehavior.RACK))
            .End();
        }

        public static BehaviorTreeBuilder CreateUseToilet()
        {
            BehaviorTreeBuilder builder = new BehaviorTreeBuilder();
            return builder.Composite(() => new BTSequence())
                .Node(() => new BTClaimSlot())
                .Node(() => new BTSelectTargetSlotAsDestination())
                .Node(() => new BTMoveTo())
                .Node(() => new BTUseToilet())
            .End();
        }

        public static BehaviorTreeBuilder CreateUseVendingMachine()
        {
            BehaviorTreeBuilder builder = new BehaviorTreeBuilder();
            return builder.Composite(() => new BTSequence())
                .Node(() => new BTClaimSlot())
                .Node(() => new BTSelectTargetSlotAsDestination())
                .Node(() => new BTMoveTo())
                .Node(() => new BTUseVendingMachine())
            .End();
        }

        public static BehaviorTreeBuilder CreateConsumeHeldItem()
        {
            BehaviorTreeBuilder builder = new BehaviorTreeBuilder();
            return builder.Composite(() => new BTSequence())
                .Node(() => new BTSelectDesirableExerciseLocation())
                .Node(() => new BTMoveTo())
                .Node(() => new BTConsumeHeldItem())
            .End();
        }
    }
}
