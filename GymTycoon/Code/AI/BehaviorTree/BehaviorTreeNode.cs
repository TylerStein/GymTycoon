using GymTycoon.Code.Data;
using System.Collections.Generic;
using ImGuiNET;
using System.Runtime.CompilerServices;

namespace GymTycoon.Code.AI.BehaviorTree
{
    // CORE

    public enum BTState
    {
        SUCCESS = 0,
        FAILURE = 1,
        RUNNING = 2,
        WAITING = 3
    }

    public class BehaviorContext
    {
        public Behavior Behavior;
        public Agent Agent;

        public BehaviorContext()
        {
            Behavior = null;
            Agent = null;
        }

        public BehaviorContext(Behavior behavior, Agent agent, Blackboard blackboard)
        {
            Behavior = behavior;
            Agent = agent;
        }
    }

    public class BehaviorTree
    {
        public static int NextId = 0;
        public int Id;

        public BehaviorContext Context;
        public BTBehavior Root;

        public Agent Agent => Context.Agent;
        public Blackboard Blackboard => Context.Agent.Blackboard;
        public Behavior Behavior => Context.Behavior;

        public BehaviorTree(BehaviorContext context)
        {
            Context = context;
            Id = NextId++;
        }

        public BTState Tick()
        {
            return Root.Tick();
        }

        public void Terminate()
        {
            Root.RequestTerminate();
        }

        public void DrawImGui()
        {
            Root.DrawImGui();
        }
    }

    public abstract class BTBehavior
    {
        public const string DefaultTargetKey = "TargetDynamicObjectInstance";
        public const string DefaultParentKey = "ParentDynamicObjectInstance";
        public const string DefaultMoveToKey = "MoveTo";
        public const string DefaultIndexKey = "ClaimSlotIndex";
        public const string DefaultRackKey = "RackAtDestination";
        public const string DefaultHeldObjectKey = "TargetHeldObject";

        public BTState Status;
        public BehaviorTree Owner;

        public BehaviorContext Context => Owner.Context;
        public Agent Agent => Owner.Context.Agent;
        public Blackboard Blackboard => Owner.Context.Agent.Blackboard;
        public Behavior Behavior => Owner.Context.Behavior;

        protected virtual void Initialize() { }
        protected virtual BTState Update() { return BTState.SUCCESS; }
        protected virtual void Terminate(BTState status) { }

        public BTBehavior()
        {
            Status = BTState.WAITING;
        }

        public BTState Tick()
        {
            if (Status != BTState.RUNNING)
            {
                Initialize();
            }

            Status = Update();

            if (Status != BTState.RUNNING)
            {
                Terminate(Status);
            }

            return Status;
        }

        public void RequestTerminate()
        {
            Terminate(Status);
        }

        public virtual void DrawImGui()
        {
            ImGui.Text($"{GetType().Name} - {Status}");
        }
    }

    public class BTBehaviorTreeWrapper : BTBehavior
    {
        protected BehaviorTreeBuilder builder;
        protected BehaviorTree tree;

        public BTBehaviorTreeWrapper(BehaviorTreeBuilder builder)
        {
            this.builder = builder;
        }

        protected override void Initialize()
        {
            tree = builder.Build(Context);
        }

        protected override BTState Update()
        {
            tree.Tick();
            return tree.Root.Status;
        }

        protected override void Terminate(BTState status)
        {
            tree.Terminate();
        }

        public override void DrawImGui()
        {
            base.DrawImGui();
            ImGui.Indent();
            tree.DrawImGui();
            ImGui.Unindent();
        }
    }

    public class BTDecorator : BTBehavior
    {
        protected BTBehavior Child;

        public BTDecorator() { }

        public void SetChild(BTBehavior child)
        {
            Child = child;
        }

        protected override void Terminate(BTState status)
        {
            Child.RequestTerminate();
        }

        public override void DrawImGui()
        {
            base.DrawImGui();
            ImGui.Indent();
            Child.DrawImGui();
            ImGui.Unindent();
        }
    }

    public class BTComposite : BTBehavior
    {
        protected LinkedList<BTBehavior> Children;

        public BTComposite()
        {
            Children = [];
        }

        public void AddChild(BTBehavior child)
        {
            child.Owner = Owner;
            Children.AddLast(child);
        }

        public void RemoveChild(BTBehavior child)
        {
            child.Owner = null;
            Children.Remove(child);
        }

        public void ClearChildren()
        {
            foreach (BTBehavior child in Children)
            {
                child.Owner = null;
            }

            Children.Clear();
        }

        protected override void Terminate(BTState status)
        {
            foreach (BTBehavior child in Children)
            {
                child.RequestTerminate();
            }
        }

        public override void DrawImGui()
        {
            base.DrawImGui();
            ImGui.Indent();
            foreach (BTBehavior child in Children)
            {
                ImGui.Indent();
                child.DrawImGui();
                ImGui.Unindent();
            }
            ImGui.Unindent();
        }
    }

    public class BTRepeat : BTDecorator
    {
        private int counter = 0;

        public BTRepeat(int count) : base()
        {
            counter = count;
        }

        protected override BTState Update()
        {
            BTState childStatus = Child.Tick();
            switch (childStatus)
            {
                case BTState.SUCCESS:
                    counter--;
                    if (counter <= 0)
                    {
                        return BTState.SUCCESS;
                    }
                    break;
                case BTState.FAILURE:
                    return BTState.FAILURE;
            }

            return BTState.RUNNING;
        }

        protected override void Terminate(BTState status)
        {
            Child.RequestTerminate();
        }
    }

    public class BTRetry : BTDecorator
    {
        private int counter = 0;

        public BTRetry(int count) : base()
        {
            counter = count;
        }

        protected override BTState Update()
        {
            BTState childStatus = Child.Tick();
            switch (childStatus)
            {
                case BTState.SUCCESS:
                    return BTState.SUCCESS;
                case BTState.FAILURE:
                    counter--;
                    if (counter <= 0)
                    {
                        return BTState.FAILURE;
                    }
                    return BTState.RUNNING;
            }

            return BTState.RUNNING;
        }

        protected override void Terminate(BTState status)
        {
            Child.RequestTerminate();
        }
    }

    /// <summary>
    /// Return success if child fails, return failure if child succeeds
    /// </summary>
    public class BTInverter : BTDecorator
    {
        protected override BTState Update()
        {
            BTState childStatus = Child.Tick();
            switch (childStatus)
            {
                case BTState.SUCCESS:
                    return BTState.FAILURE;
                case BTState.FAILURE:
                    return BTState.SUCCESS;
            }
            return BTState.RUNNING;
        }

        protected override void Terminate(BTState status)
        {
            Child.RequestTerminate();
        }
    }

    /// <summary>
    /// Return success if all children succeed (AND)
    /// </summary>
    public class BTSequence : BTComposite
    {
        private LinkedListNode<BTBehavior> currentChildNode = null;

        public BTSequence() : base() { }

        protected override void Initialize()
        {
            currentChildNode = Children.First;
        }

        protected override BTState Update()
        {
            BTState childState = currentChildNode.Value.Tick();
            switch (childState)
            {
                case BTState.SUCCESS:
                    currentChildNode = currentChildNode.Next;
                    if (currentChildNode == null)
                    {
                        return BTState.SUCCESS;
                    }
                    break;
                case BTState.RUNNING:
                    return BTState.RUNNING;
                case BTState.FAILURE:
                    currentChildNode = Children.First;
                    return BTState.FAILURE;
            }

            return BTState.RUNNING;
        }

        protected override void Terminate(BTState status)
        {
            foreach (BTBehavior child in Children)
            {
                child.RequestTerminate();
            }
        }
    }

    public class BTFilter : BTSequence
    {
        public BTFilter() : base() { }

        public void AddCondition(BTBehavior condition)
        {
            Children.AddLast(condition);
        }

        public void AddAction(BTBehavior action)
        {
            Children.AddFirst(action);
        }
    }

    /// <summary>
    /// Return success on first successful child (OR)
    /// </summary>
    public class BTSelector : BTComposite
    {
        protected LinkedListNode<BTBehavior> currentChildNode = null;

        public BTSelector() : base() { }

        protected override void Initialize()
        {
            currentChildNode = Children.First;
        }

        protected override BTState Update()
        {
            BTState childState = currentChildNode.Value.Tick();
            switch (childState)
            {
                case BTState.SUCCESS:
                    currentChildNode = Children.First;
                    return BTState.SUCCESS;
                case BTState.RUNNING:
                    return BTState.RUNNING;
                case BTState.FAILURE:
                    currentChildNode = currentChildNode.Next;
                    if (currentChildNode == null)
                    {
                        return BTState.FAILURE;
                    }
                    return BTState.RUNNING;
            }

            return BTState.FAILURE;
        }
    }

    /// <summary>
    /// Waits for all children to have any result in sequence
    /// </summary>
    public class BTAll : BTComposite
    {
        private LinkedListNode<BTBehavior> currentChildNode = null;

        public BTAll() : base() { }

        protected override void Initialize()
        {
            currentChildNode = Children.First;
        }

        protected override BTState Update()
        {
            BTState childState = currentChildNode.Value.Tick();
            if (childState != BTState.RUNNING)
            {
                currentChildNode = currentChildNode.Next;
                if (currentChildNode == null)
                {
                    return BTState.SUCCESS;
                }
            }

            return BTState.RUNNING;
        }
    }

    /// <summary>
    /// Waits for all children to have any result in parallel
    /// </summary>
    public class BTParallel : BTComposite
    {
        public enum Policy
        {
            RequireOne = 0,
            RequireAll = 1
        }

        private List<BTState> results;
        private int successCount = 0;
        private int failCount = 0;
        private Policy successPolicy;
        private Policy failurePolicy;

        public BTParallel(Policy success, Policy failure) : base()
        {
            successPolicy = success;
            failurePolicy = failure;
        }

        public List<BTState> GetResults()
        {
            return results;
        }

        protected override void Initialize()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                results.Add(BTState.RUNNING);
            }

            successCount = 0;
            failCount = 0;
        }

        protected override BTState Update()
        {
            int successCount = 0;
            int failCount = 0;
            int index = -1;
            foreach (BTBehavior child in Children)
            {
                index++;
                switch (results[index])
                {
                    case BTState.SUCCESS:
                        successCount++;
                        break;
                    case BTState.FAILURE:
                        failCount++;
                        break;
                    default:
                        BTState childState = child.Tick();
                        results[index] = childState;
                        if (childState == BTState.SUCCESS)
                        {
                            successCount++;
                        }

                        if (childState == BTState.FAILURE)
                        {
                            failCount++;
                        }
                        break;
                }
            }

            if ((successCount > 0 && successPolicy == Policy.RequireOne) || (successCount == Children.Count && successPolicy == Policy.RequireAll))
            {
                return BTState.SUCCESS;
            }

            if ((failCount > 0 && failurePolicy == Policy.RequireOne) || (failCount == Children.Count && failurePolicy == Policy.RequireAll))
            {
                return BTState.SUCCESS;
            }

            return BTState.RUNNING;
        }

        protected override void Terminate(BTState status)
        {
            foreach (BTBehavior child in Children)
            {
                if (child.Status == BTState.RUNNING)
                {
                    child.RequestTerminate();
                }
            }
        }
    }

    /// <summary>
    /// Provide two to three children as "If", "Else", "Then" branches, where "Then" is optional
    /// Convenience class for what could otherwise be accomplished with a Selector
    /// </summary>
    public class BTIfThenElse : BTComposite
    {
        private enum BranchStatus
        {
            IF = 0,
            THEN = 1,
            ELSE = 2
        }

        private BranchStatus _branchStatus;

        protected override void Initialize()
        {
            base.Initialize();
            _branchStatus = BranchStatus.IF;
            if (Children.Count < 2 || Children.Count > 3)
            {
                throw new System.Exception("IfThenElse must have 2 - 3 children (If, Then, (Else))");
            }
        }

        protected override BTState Update()
        {
            switch (_branchStatus)
            {
                case BranchStatus.IF:
                    Status = Children.First.Value.Tick();
                    switch (Status)
                    {
                        case BTState.SUCCESS:
                            _branchStatus = BranchStatus.THEN;
                            break;
                        case BTState.FAILURE:
                            if (Children.Count == 2)
                            {
                                // No ELSE
                                return BTState.SUCCESS;
                            }

                            _branchStatus = BranchStatus.ELSE;
                            break;
                    }

                    break;
                case BranchStatus.THEN:
                    return Children.First.Next.Value.Tick();
                case BranchStatus.ELSE:
                    return Children.Last.Value.Tick();
            }

            return BTState.RUNNING;
        }
    }

    public class BTSuccess : BTBehavior
    {
        protected override BTState Update()
        {
            return BTState.SUCCESS;
        }
    }

    public class BTFailure : BTBehavior
    {
        protected override BTState Update()
        {
            return BTState.FAILURE;
        }
    }
}
