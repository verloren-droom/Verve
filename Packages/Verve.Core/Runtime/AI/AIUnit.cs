namespace Verve.AI
{
    using Unit;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// AI单元
    /// </summary>
    [CustomUnit("AI"), Serializable]
    public partial class AIUnit : UnitBase
    { 
        private readonly Dictionary<int, IBehaviorTree> m_Trees = new Dictionary<int, IBehaviorTree>();
        private readonly Dictionary<int, Blackboard> m_Blackboards = new Dictionary<int, Blackboard>();
        
        
        protected override void OnStartup(params object[] args)
        {
            base.OnStartup(args);
            CanEverTick = true;
        }

        protected override void OnTick(float deltaTime, float unscaledTime)
        {
            base.OnTick(deltaTime, unscaledTime);
            for (int i = 0; i < m_Trees.Values.Count; i++)
            {
                m_Trees.Values.ElementAt(i)?.Update(deltaTime);
            }
        }

        public BTType CreateBT<BTType>(bool isActive = true, int initialCapacity = 128, Blackboard bb = null)
            where BTType : class, IBehaviorTree
        {
            var tree = new BehaviorTree(initialCapacity, bb);
            tree.IsActive = isActive;
            m_Trees.Add(tree.ID, tree);
            m_Blackboards.Add(tree.ID, tree.BB);
            return tree as BTType;
        }

        public BTType CreateOrGetBT<BTType>(int id, bool isActive = true, int initialCapacity = 64, Blackboard bb = null)
            where BTType : class, IBehaviorTree
        {
            if (m_Trees.TryGetValue(id, out var tree))
            {
                return tree as BTType;
            }
            return CreateBT<BTType>(isActive, initialCapacity, bb);
        }
        
        public void DestroyBT(int id, bool isDestroyBB = true)
        {
            if (m_Trees.TryGetValue(id, out var tree))
            {
                tree?.Dispose();
                if (isDestroyBB)
                {
                    if (m_Blackboards.TryGetValue(id, out var bb))
                    {
                        bb?.Dispose();
                    }
                    m_Blackboards.Remove(id);
                }
                m_Trees.Remove(id);
            }
        }

        public BTType GetBT<BTType>(int id)
            where BTType : class, IBehaviorTree
        {
            if (m_Trees.TryGetValue(id, out var tree))
            {
                return tree as BTType;
            }
            return null;
        }

        public Blackboard GetBlackboard(int id)
        {
            if (m_Blackboards.TryGetValue(id, out var bb))
            {
                return bb;
            }
            return null;
        }
    }
}