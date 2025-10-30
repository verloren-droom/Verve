#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.AI
{
    /// <summary>
    ///   <para>AI子子模块</para>
    /// </summary>
    [GameFeatureSubmodule(typeof(AIGameFeature))]
    public sealed partial class AISubmodule : TickableGameFeatureSubmodule
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