#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.Callbacks;
    using UnityEditor.AssetImporters;

    /// <summary>
    ///   <para>自定义配置表资源导入编辑器。</para>
    /// </summary>
    [CustomEditor(typeof(ConfigTableScriptedImporter))]
    internal sealed class ConfigTableScriptedImporterEditor : ScriptedImporterEditor
    {
        /// <summary>
        ///   <para>编辑状态。</para>
        /// </summary>
        private ConfigTableEditState editState;
        /// <summary>
        ///   <para>网格布局。</para>
        /// </summary>
        private readonly ConfigTableGridLayout gridLayout = new ();
        /// <summary>
        ///   <para>滚动位置。</para>
        /// </summary>
        private Vector2 scrollPosition;
    
        /// <inheritdoc />
        public override bool showImportedObject => false;
    
        /// <summary>
        ///   <para>打开表编辑器。</para>
        /// </summary>
        /// <param name="instanceId">实例标识。</param>
        /// <param name="line">行。</param>
        [OnOpenAsset]
        private static bool OpenTableEditor(int instanceId, int line)
        {
            var assetPath = AssetDatabase.GetAssetPath(instanceId);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith($".{ConfigTableModule.FileExtension}", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
    
            ConfigTableEditorWindow.Open(assetPath);
            return true;
        }
    
        /// <inheritdoc />
        public override void OnEnable()
        {
            base.OnEnable();
            LoadEditState();
        }
    
        /// <inheritdoc />
        public override void OnDisable()
        {
            var pendingState = editState != null && editState.IsDirty &&
                !EditorApplication.isCompiling && !EditorApplication.isUpdating ? editState : null;
    
            base.OnDisable();
    
            if (pendingState != null)
            {
                ConfigTableUnsavedChanges.Queue(pendingState, null);
            }
    
            if (editState != null)
            {
                editState.Changed -= Repaint;
                editState.Dispose();
                editState = null;
            }
        }
    
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            if (editState == null || editState.Table == null)
            {
                EditorGUILayout.HelpBox(editState == null ? "无法加载配置表。" : editState.Error, MessageType.Error);
            }
            else
            {
                if (GUILayout.Button("打开配置表编辑器", GUILayout.Height(24f)))
                {
                    ConfigTableEditorWindow.Open(editState.AssetPath);
                }
    
                ConfigTableGridEditor.Draw(editState, gridLayout, ref scrollPosition);
            }
    
            ApplyRevertGUI();
        }
    
        /// <inheritdoc />
        public override bool HasModified() => base.HasModified() || editState != null && editState.IsDirty;
    
        /// <inheritdoc />
        protected override bool CanApply() => editState != null && editState.IsDirty && editState.CanSave();
    
        /// <inheritdoc />
        protected override void Apply()
        {
            if (editState != null && editState.Save(false))
            {
                base.Apply();
            }
        }
    
#if UNITY_2022_1_OR_NEWER
        /// <inheritdoc />
        public override void DiscardChanges()
        {
            base.DiscardChanges();
#else
        /// <inheritdoc />
        protected override void ResetValues()
        {
            base.ResetValues();
#endif
            if (editState != null)
            {
                editState.Reload();
                gridLayout.Clear();
                scrollPosition = Vector2.zero;
            }
        }
    
        /// <summary>
        ///   <para>加载编辑状态。</para>
        /// </summary>
        private void LoadEditState()
        {
            var importer = target as AssetImporter;
            editState = importer == null ? null : new ConfigTableEditState(importer.assetPath);
            if (editState != null)
            {
                editState.Changed += Repaint;
            }
            gridLayout.Clear();
            scrollPosition = Vector2.zero;
        }
    }
}

#endif