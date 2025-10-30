#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.AI
{
    /// <summary>
    ///   <para>AI游戏功能模块</para>
    /// </summary>
    [System.Serializable, GameFeatureModule("Verve/AI", Description = "AI游戏功能模块", SelectionMode = SubmoduleSelectionMode.Locked)]
    internal sealed class AIGameFeature : GameFeatureModule
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            Add<AISubmodule>();
        }
    }
}

#endif