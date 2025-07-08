#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using Verve.AI;
    using UnityEngine;


    /// <summary>
    /// AI功能组件
    /// </summary>
    public partial class AIFeatureComponent : GameFeatureComponent
    {
        private void Update()
        {
            for (int i = 0; i < BehaviorTree.AllBehaviorTrees.Count; i++)
            {
                BehaviorTree.AllBehaviorTrees[i]?.Update(Time.deltaTime);
            }
        }
    }
}

#endif