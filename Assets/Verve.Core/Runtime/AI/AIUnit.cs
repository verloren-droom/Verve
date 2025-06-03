namespace Verve.AI
{
    using Unit;
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// AI单元
    /// </summary>
    [CustomUnit("AI"), Serializable]
    public partial class AIUnit : UnitBase
    { 
        private readonly Dictionary<int, WeakReference<IBehaviorTree>> m_Trees = new Dictionary<int, WeakReference<IBehaviorTree>>();
        private readonly Dictionary<int, WeakReference<Blackboard>> m_Blackboards = new Dictionary<int, WeakReference<Blackboard>>();
        
        
        protected override void OnTick(float deltaTime, float unscaledTime)
        {
            base.OnTick(deltaTime, unscaledTime);
            foreach (var tree in m_Trees.Values)
            {
                tree.TryGetTarget(out var behaviorTree);
                (behaviorTree as IBehaviorTree)?.Update(unscaledTime);
            }
        }
        
        public BTType CreateTree<BTType>(int initialCapacity = 64, Blackboard bb = null) where BTType : class, IBehaviorTree
        {
            var tree = new BehaviorTree(initialCapacity, bb);
            m_Trees.Add(tree.ID, new WeakReference<IBehaviorTree>(tree));
            m_Blackboards.Add(tree.ID, new WeakReference<Blackboard>(tree.BB));
            return tree as BTType;
        }
        
        public void DestroyTree(int id)
        {
            if (m_Trees.TryGetValue(id, out var tree))
            {
                if (tree.TryGetTarget(out var behaviorTree))
                {
                    behaviorTree.Dispose();
                    if (m_Blackboards.TryGetValue(id, out var bb))
                    {
                        bb.TryGetTarget(out var blackboard);
                        blackboard?.Dispose();
                    }
                }
                m_Trees.Remove(id);
                m_Blackboards.Remove(id);
            }
        }

        public IBehaviorTree GetTree(int id)
        {
            if (m_Trees.TryGetValue(id, out var tree))
            {
                tree.TryGetTarget(out var behaviorTree);
                return behaviorTree;
            }
            return null;
        }
        
        public Blackboard GetBlackboard(int id)
        {
            if (m_Blackboards.TryGetValue(id, out var bb))
            {
                bb.TryGetTarget(out var blackboard);
                return blackboard;
            }
            return null;
        }
    }
}