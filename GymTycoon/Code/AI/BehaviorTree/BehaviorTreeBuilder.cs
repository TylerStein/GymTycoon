using System.Collections.Generic;
using System;

namespace GymTycoon.Code.AI.BehaviorTree
{
    public interface IBehaviorTreeBuilder
    {
        public BTBehavior Build(BehaviorTree owner);
    }

    public class BehaviorTreeBuilder
    {
        private IBehaviorTreeBuilder _rootBuilder;

        public BehaviorTreeBuilder()
        {

        }

        public BehaviorTree Build(BehaviorContext context)
        {
            BehaviorTree tree = new BehaviorTree(context);
            tree.Root = _rootBuilder.Build(tree);
            return tree;
        }

        public BehaviorTreeBuilder Node<TBehavior>(Func<TBehavior> factory)
            where TBehavior : BTBehavior
        {
#if DEBUG
            if (_rootBuilder != null)
            {
                throw new Exception("BehaviorTreeBuilder attempting to override root, invalid tree structure?");
            }
#endif
            var builder = new BTNodeBuilder<TBehavior>(factory);
            _rootBuilder = builder;
            return this;
        }

        public BTCompositeBuilder<BehaviorTreeBuilder> Composite<TBehavior>(Func<TBehavior> factory)
            where TBehavior : BTComposite
        {
#if DEBUG
            if (_rootBuilder != null)
            {
                throw new Exception("BehaviorTreeBuilder attempting to override root, invalid tree structure?");
            }
#endif
            var builder = new BTCompositeBuilder<BehaviorTreeBuilder>(factory, this);
            _rootBuilder = builder;
            return builder;
        }

        public BTDecoratorBuilder<BehaviorTreeBuilder> Decorator<TBehavior>(Func<TBehavior> factory)
            where TBehavior : BTDecorator
        {
#if DEBUG
            if (_rootBuilder != null)
            {
                throw new Exception("BehaviorTreeBuilder attempting to override root, invalid tree structure?");
            }
#endif
            var builder = new BTDecoratorBuilder<BehaviorTreeBuilder>(factory, this);
            _rootBuilder = builder;
            return builder;
        }

        public BehaviorTreeBuilder End()
        {
            return this;
        }
    }

    public class BTNodeBuilder<TBehavior> : IBehaviorTreeBuilder
        where TBehavior : BTBehavior
    {
        private Func<TBehavior> _factory;

        public BTNodeBuilder(Func<TBehavior> factory)
        {
            _factory = factory;
        }

        public BTBehavior Build(BehaviorTree tree)
        {
            BTBehavior behavior = _factory();
            behavior.Owner = tree;
            return behavior;
        }
    }

    public class BTCompositeBuilder<TParent> : IBehaviorTreeBuilder
        where TParent : class
    {
        private TParent _parent;

        private Func<BTComposite> _factory;
        private List<IBehaviorTreeBuilder> _childBuilders;

        public BTCompositeBuilder(Func<BTComposite> factory, TParent parent)
        {
            _factory = factory;
            _parent = parent;
            _childBuilders = [];
        }

        public BTBehavior Build(BehaviorTree owner)
        {
            BTComposite composite = _factory();
            composite.Owner = owner;

            foreach (var factory in _childBuilders)
            {
                BTBehavior child = factory.Build(owner);
                child.Owner = owner;
                composite.AddChild(child);
            }

            return composite;
        }

        public BTCompositeBuilder<TParent> Node<TBehavior>(Func<TBehavior> factory)
            where TBehavior : BTBehavior
        {
            var builder = new BTNodeBuilder<TBehavior>(factory);
            _childBuilders.Add(builder);
            return this;
        }

        public BTCompositeBuilder<BTCompositeBuilder<TParent>> Composite<TBehavior>(Func<TBehavior> factory)
            where TBehavior : BTComposite
        {
            var builder = new BTCompositeBuilder<BTCompositeBuilder<TParent>>(factory, this);
            _childBuilders.Add(builder);
            return builder;
        }

        public BTDecoratorBuilder<BTCompositeBuilder<TParent>> Decorator<TBehavior>(Func<TBehavior> factory)
            where TBehavior : BTDecorator
        {
            var builder = new BTDecoratorBuilder<BTCompositeBuilder<TParent>>(factory, this);
            _childBuilders.Add(builder);
            return builder;
        }

        public TParent End()
        {
            return _parent;
        }
    }

    public class BTDecoratorBuilder<TParent> : IBehaviorTreeBuilder
        where TParent : class
    {
        private TParent _parent;

        private Func<BTDecorator> _factory;
        private IBehaviorTreeBuilder _childBuilder;

        public BTDecoratorBuilder(Func<BTDecorator> factory, TParent parent)
        {
            _factory = factory; 
            _parent = parent;
        }

        public BTBehavior Build(BehaviorTree owner)
        {
            BTDecorator decorator = _factory();
            decorator.Owner = owner;

            BTBehavior child = _childBuilder.Build(owner);
            decorator.SetChild(child);

            return decorator;
        }

        public TParent Node<TBehavior>(Func<TBehavior> factory)
            where TBehavior : BTBehavior
        {
            _childBuilder = new BTNodeBuilder<TBehavior>(factory);
            return _parent;
        }

        public BTCompositeBuilder<BTDecoratorBuilder<TParent>> Composite<TBehavior>(Func<TBehavior> factory)
            where TBehavior : BTComposite
        {
#if DEBUG
            if (_childBuilder != null)
            {
                throw new Exception("Decorator attempting to override child, invalid tree structure?");
            }
#endif
            var builder = new BTCompositeBuilder<BTDecoratorBuilder<TParent>>(factory, this);
            _childBuilder = builder;
            return builder;
        }

        public BTDecoratorBuilder<BTDecoratorBuilder<TParent>> Decorator<TBehavior>(Func<TBehavior> factory)
            where TBehavior : BTDecorator
        {
#if DEBUG
            if (_childBuilder != null)
            {
                throw new Exception("Decorator attempting to override child, invalid tree structure?");
            }
#endif
            var builder = new BTDecoratorBuilder<BTDecoratorBuilder<TParent>>(factory, this);
            _childBuilder = builder;
            return builder;
        }

        public TParent End()
        {
            return _parent;
        }
    }
}
