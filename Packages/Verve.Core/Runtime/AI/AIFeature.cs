namespace Verve.AI
{
    using System.Linq;
    
    
    /// <summary>
    /// AI功能
    /// </summary>
    [System.Serializable]
    public class AIFeature : GameFeature
    {
        public void Update(float deltaTime)
        {
            for (int i = 0; i < BehaviorTree.AllBehaviorTrees.Count; i++)
            {
                BehaviorTree.AllBehaviorTrees[i]?.Update(deltaTime);
            }
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            for (int i = 0; i < BehaviorTree.AllBehaviorTrees.Count; i++)
            {
                BehaviorTree.AllBehaviorTrees[i]?.Dispose();
            }
        }
        
        public BTType CreateBehaviorTree<BTType>(int initialCapacity = 128, Blackboard bb = null)
            where BTType : class, IBehaviorTree
        {
            var tree = new BehaviorTree(initialCapacity, bb);
            return tree as BTType;
        }
        
        public BTType CreateOrGetBehaviorTree<BTType>(int id, int initialCapacity = 64, Blackboard bb = null)
            where BTType : class, IBehaviorTree
        {
            var existing = BehaviorTree.AllBehaviorTrees.FirstOrDefault(x => x.ID == id);
            if (existing != null) return existing as BTType;
            return CreateBehaviorTree<BTType>(initialCapacity, bb);
        }
        
        public void DestroyBehaviorTree(int id, bool isDestroyBB = true)
        {
            BehaviorTree bt = BehaviorTree.AllBehaviorTrees.FirstOrDefault(x => x.ID == id);
            if (bt != null)
            {
                bt.Dispose();
                if (isDestroyBB)
                {
                    Blackboard.AllBlackboards.FirstOrDefault(x => x.ID == id)?.Dispose();
                }
            }
        }
        
        public BTType GetBehaviorTree<BTType>(int id)
            where BTType : class, IBehaviorTree
        {
            return BehaviorTree.AllBehaviorTrees
                .FirstOrDefault(t => t.ID == id) as BTType;
        }
        
        public Blackboard GetBlackboard(int id)
        {
            return Blackboard.AllBlackboards
                .FirstOrDefault(t => t.ID == id);
        }

    }
}