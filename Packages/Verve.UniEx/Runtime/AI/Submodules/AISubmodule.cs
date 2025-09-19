#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.AI
{
    [GameFeatureSubmodule(typeof(AIGameFeature))]
    public class AISubmodule : TickableGameFeatureSubmodule
    {
        protected override void OnTick(in GameFeatureContext ctx)
        {
            for (int i = 0; i < BehaviorTree.AllBehaviorTrees.Count; i++)
            {
                BehaviorTree.AllBehaviorTrees[i].Update(ctx.Time);
            }
        }
    }
}

#endif