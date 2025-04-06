namespace Verve.ECS.Editor
{
#if UNITY_EDITOR
    using ECS;
    using UnityEditor;
    
    [CustomEditor(typeof(ECSWorld))]
    public class ECSRunnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
#endif
}