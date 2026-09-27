#if UNITY_EDITOR

namespace Verve.Editor
{
    using Verve;
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;
    using System.Collections.Generic;
    
    /// <summary>
    ///   <para>配置表数据编辑窗口。</para>
    /// </summary>
    internal sealed class ConfigTableEditorWindow : EditorWindow
    {
        /// <summary>
        ///   <para>已加载资源路径。</para>
        /// </summary>
        [SerializeField] private string loadedAssetPath = string.Empty;
    
        /// <summary>
        ///   <para>待处理编辑状态。</para>
        /// </summary>
        private static ConfigTableEditState pendingEditState;
        /// <summary>
        ///   <para>编辑状态。</para>
        /// </summary>
        private ConfigTableEditState editState;
        /// <summary>
        ///   <para>网格布局。</para>
        /// </summary>
        private readonly ConfigTableGridLayout gridLayout = new ConfigTableGridLayout();
        /// <summary>
        ///   <para>关闭提示显示中。</para>
        /// </summary>
        private bool closePromptShowing;
        /// <summary>
        ///   <para>允许关闭。</para>
        /// </summary>
        private bool allowClose;
        /// <summary>
        ///   <para>窗口销毁时保留编辑状态。</para>
        /// </summary>
        private bool preserveEditStateOnDestroy;
        /// <summary>
        ///   <para>滚动位置。</para>
        /// </summary>
        private Vector2 scrollPosition;
        /// <summary>
        ///   <para>搜索条件。</para>
        /// </summary>
        private EditorSearchFilter search = new(string.Empty);
        /// <summary>
        ///   <para>搜索框；在 GUI 绘制阶段创建。</para>
        /// </summary>
        private SearchField searchField;
        /// <summary>
        ///   <para>搜索查询。</para>
        /// </summary>
        private ConfigTableSearchQuery searchQuery = ConfigTableSearchQuery.Empty;
    
        /// <summary>
        ///   <para>打开指定配置表。</para>
        /// </summary>
        /// <param name="assetPath">配置表资产路径。</param>
        /// <param name="initialSearchText">首次打开时的搜索文本。</param>
        /// <param name="initialRequiresExactLength">首次打开时是否要求匹配长度相同。</param>
        /// <param name="initialIsCaseSensitive">首次打开时是否区分大小写。</param>
        public static void Open(string assetPath, string initialSearchText = null,
            bool initialRequiresExactLength = false, bool initialIsCaseSensitive = false)
        {
            ConfigTableEditorWindow[] windows = Resources.FindObjectsOfTypeAll<ConfigTableEditorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                ConfigTableEditorWindow window = windows[i];
                string windowAssetPath = window.editState == null ? window.loadedAssetPath : window.editState.AssetPath;
                if (string.IsNullOrEmpty(windowAssetPath) || !string.Equals(windowAssetPath, assetPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
    
                window.Show();
                window.Focus();
                return;
            }
    
            var newWindow = CreateInstance<ConfigTableEditorWindow>();
            newWindow.titleContent = new GUIContent(Path.GetFileNameWithoutExtension(assetPath));
            newWindow.Load(assetPath);
            newWindow.SetInitialSearch(initialSearchText, initialRequiresExactLength, initialIsCaseSensitive);
            newWindow.Show();
        }
    
        /// <summary>
        ///   <para>设置初始搜索。</para>
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="requiresExactLength">要求精确长度。</param>
        /// <param name="isCaseSensitive">是否区分大小写。</param>
        private void SetInitialSearch(string value, bool requiresExactLength, bool isCaseSensitive)
        {
            search = new EditorSearchFilter(value, requiresExactLength, isCaseSensitive);
            searchQuery = ConfigTableSearch.Parse(search.Text, search.RequiresExactLength, search.IsCaseSensitive);
        }
    
        /// <summary>
        ///   <para>启用时初始化。</para>
        /// </summary>
        private void OnEnable()
        {
            if (editState == null && !string.IsNullOrEmpty(loadedAssetPath))
            {
                Load(loadedAssetPath);
            }
        }
    
        /// <summary>
        ///   <para>销毁时清理。</para>
        /// </summary>
        private void OnDestroy()
        {
            if (preserveEditStateOnDestroy)
            {
                DetachEditState(false);
                return;
            }
    
            DisposeEditState();
        }
        
        /// <summary>
        ///   <para>禁用时清理。</para>
        /// </summary>
        private void OnDisable()
        {
            if (allowClose || closePromptShowing || editState == null || !editState.IsDirty ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }
    
            closePromptShowing = true;
            bool cancelClose = EditorUtility.DisplayDialog(
                "未保存的配置表",
                "当前配置表有未保存的数据。是否退出编辑器？",
                "取消",
                "退出");
            closePromptShowing = false;
    
            if (cancelClose)
            {
                preserveEditStateOnDestroy = true;
                pendingEditState = editState;
                EditorApplication.delayCall += ReopenAfterCancelledClose;
                return;
            }
    
            allowClose = true;
        }
        
        /// <summary>
        ///   <para>绘制界面。</para>
        /// </summary>
        private void OnGUI()
        {
            HandleShortcuts();
            if (editState == null)
            {
                EditorGUILayout.HelpBox("配置表数据不可用。", MessageType.Error);
                return;
            }
    
            DrawToolbar();
            if (editState.Table == null)
            {
                EditorGUILayout.HelpBox(editState.Error, MessageType.Error);
                return;
            }
    
            ConfigTableGridEditor.Draw(editState, gridLayout, ref scrollPosition, searchQuery, position.height);
        }
    
        /// <summary>
        ///   <para>处理快捷键。</para>
        /// </summary>
        private void HandleShortcuts()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown ||
                !(currentEvent.control || currentEvent.command) || currentEvent.alt)
            {
                return;
            }
    
            if (currentEvent.keyCode == KeyCode.S)
            {
                if (editState != null && editState.IsDirty)
                {
                    editState.Save(true);
                }
                currentEvent.Use(); 
                return;
            }
    
            if (currentEvent.keyCode == KeyCode.R)
            {
                if (editState != null && editState.IsDirty)
                {
                    editState.Reload();
                    scrollPosition = Vector2.zero;
                }
                currentEvent.Use();
            }
        }
    
        /// <summary>
        ///   <para>绘制工具栏。</para>
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawAssetField();
            searchField ??= new SearchField();
            var searchRect = GUILayoutUtility.GetRect(228f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            if (CoreEditorUtility.DrawSearchToolbar(searchRect, searchField, ref search))
                searchQuery = ConfigTableSearch.Parse(search.Text, search.RequiresExactLength, search.IsCaseSensitive);
            GUILayout.FlexibleSpace();
    
            bool hasCsvSource = ConfigTableImporter.HasCsvSource(editState.AssetPath);
            using (new EditorGUI.DisabledGroupScope(!hasCsvSource))
            {
                if (GUILayout.Button(new GUIContent("CSV目录",
                    "打开 CSV 文件所在文件夹"),
                    EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    ConfigTableImporter.TryGetCsvSourcePath(editState.AssetPath, out string csvPath);
                    EditorUtility.RevealInFinder(csvPath);
                }
                if (GUILayout.Button(new GUIContent("打开 CSV",
                    "使用系统默认程序打开 CSV 文件"),
                    EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    ConfigTableImporter.TryGetCsvSourcePath(editState.AssetPath, out string csvPath);
                    EditorUtility.OpenWithDefaultApp(csvPath);
                }
            }
            GUILayout.Space(5f);
            using (new EditorGUI.DisabledGroupScope(!hasCsvSource))
            {
                if (GUILayout.Button(new GUIContent("CSV构建",
                    "从 CSV 源重新构建当前配置表、替换当前数据并重新生成访问代码"),
                    EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    BuildCurrentTableFromCsv();
                }
            }
            using (new EditorGUI.DisabledGroupScope(!ConfigTableImporter.CanExportToCsv(editState.AssetPath)))
            {
                if (GUILayout.Button(new GUIContent("CSV导出",
                    $"将当前 .{ConfigTableModule.FileExtension} 数据合并导出为 CSV 源文件"),
                    EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    ExportCurrentTableToCsv();
                }
            }
            GUILayout.Space(5f);
            using (new EditorGUI.DisabledGroupScope(!editState.IsDirty))
            {
                if (GUILayout.Button(new GUIContent("还原"),
                    EditorStyles.toolbarButton, GUILayout.Width(48f)))
                {
                    editState.Reload();
                    scrollPosition = Vector2.zero;
                }
                if (GUILayout.Button(new GUIContent("保存"),
                    EditorStyles.toolbarButton, GUILayout.Width(48f)))
                {
                    editState.Save(true);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    
        /// <summary>
        ///   <para>绘制资源字段。</para>
        /// </summary>
        private void DrawAssetField()
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(editState.AssetPath);
            Rect fieldRect = GUILayoutUtility.GetRect(150f, EditorGUIUtility.singleLineHeight, GUILayout.Width(150f));
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.ObjectField(fieldRect, GUIContent.none, asset, typeof(UnityEngine.Object), false);
            }
    
            Event currentEvent = Event.current;
            if (asset != null && currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && fieldRect.Contains(currentEvent.mousePosition))
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                currentEvent.Use();
            }
        }
    
        /// <summary>
        ///   <para>从 CSV 构建当前表。</para>
        /// </summary>
        private void BuildCurrentTableFromCsv()
        {
            if (!CanRunFileOperation() || !EditorUtility.DisplayDialog("从 CSV 构建配置表", "这将重新构建所有 CSV 配置表，并替换当前配置表及生成的访问代码。", "构建", "取消"))
            {
                return;
            }
    
            if (ConfigTableImporter.BuildTableFromCsv(editState.AssetPath))
            {
                editState.Reload();
                gridLayout.Clear();
                scrollPosition = Vector2.zero;
            }
        }
    
        /// <summary>
        ///   <para>将当前表导出为 CSV。</para>
        /// </summary>
        private void ExportCurrentTableToCsv()
        {
            if (!CanRunFileOperation() || !EditorUtility.DisplayDialog("从 ." + ConfigTableModule.FileExtension + " 导出 CSV",
                "这将合并当前源表的所有分表，并覆盖 CSV 源文件。", "导出", "取消"))
            {
                return;
            }
    
            ConfigTableImporter.ExportTableToCsv(editState.AssetPath);
        }
    
        /// <summary>
        ///   <para>判断是否允许执行文件操作。</para>
        /// </summary>
        private bool CanRunFileOperation()
        {
            if (!editState.IsDirty)
            {
                return true;
            }
    
            EditorUtility.DisplayDialog("未保存的配置表", "在构建或导出 CSV 之前，请保存或还原当前更改。", "确定");
            return false;
        }
    
        /// <summary>
        ///   <para>加载。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        private void Load(string assetPath)
        {
            DisposeEditState();
            AttachEditState(new ConfigTableEditState(assetPath));
        }
    
        /// <summary>
        ///   <para>绑定编辑状态。</para>
        /// </summary>
        /// <param name="state">状态。</param>
        private void AttachEditState(ConfigTableEditState state)
        {
            loadedAssetPath = state.AssetPath;
            titleContent = new GUIContent(Path.GetFileNameWithoutExtension(state.AssetPath));
            editState = state;
            editState.Changed += Repaint;
            closePromptShowing = false;
            allowClose = false;
            preserveEditStateOnDestroy = false;
            search = new EditorSearchFilter(string.Empty, search.RequiresExactLength, search.IsCaseSensitive);
            searchQuery = ConfigTableSearchQuery.Empty;
            scrollPosition = Vector2.zero;
            gridLayout.Clear();
            Repaint();
        }
    
        /// <summary>
        ///   <para>释放编辑状态。</para>
        /// </summary>
        private void DisposeEditState() => DetachEditState(true);
    
        /// <summary>
        ///   <para>解除绑定编辑状态。</para>
        /// </summary>
        /// <param name="dispose">是否释放。</param>
        private void DetachEditState(bool dispose)
        {
            if (editState == null)
            {
                return;
            }
    
            editState.Changed -= Repaint;
            if (dispose)
            {
                editState.Dispose();
            }
            editState = null;
        }
    
        /// <summary>
        ///   <para>取消关闭后重新打开窗口。</para>
        /// </summary>
        private static void ReopenAfterCancelledClose()
        {
            ConfigTableEditState state = pendingEditState;
            pendingEditState = null;
            if (state == null)
            {
                return;
            }
    
            var window = CreateInstance<ConfigTableEditorWindow>();
            window.AttachEditState(state);
            window.Show();
            window.Focus();
        }
    }
    
    /// <summary>
    ///   <para>配置表编辑数据状态。</para>
    /// </summary>
    internal sealed class ConfigTableEditState
    {
        /// <summary>
        ///   <para>资源路径。</para>
        /// </summary>
        private readonly string assetPath;
        /// <summary>
        ///   <para>表。</para>
        /// </summary>
        private ConfigTableAsset table;
        /// <summary>
        ///   <para>撤销状态。</para>
        /// </summary>
        private ConfigTableDataAsset undoState;
        /// <summary>
        ///   <para>错误。</para>
        /// </summary>
        private string error;
        /// <summary>
        ///   <para>已保存表数据。</para>
        /// </summary>
        private string savedTableData;
        /// <summary>
        ///   <para>是否脏标记。</para>
        /// </summary>
        private bool isDirty;
        /// <summary>
        ///   <para>布局版本。</para>
        /// </summary>
        private int layoutVersion;
        /// <summary>
        ///   <para>标识版本。</para>
        /// </summary>
        private int idVersion;
    
        /// <summary>
        ///   <para>创建指定资产的编辑状态。</para>
        /// </summary>
        /// <param name="assetPath">配置表资产路径。</param>
        internal ConfigTableEditState(string assetPath)
        {
            this.assetPath = assetPath;
            Reload();
        }
    
        /// <summary>
        ///   <para>正在编辑的资产路径。</para>
        /// </summary>
        internal string AssetPath => assetPath;
    
        /// <summary>
        ///   <para>当前可编辑的数据。</para>
        /// </summary>
        internal ConfigTableAsset Table => table;
    
        /// <summary>
        ///   <para>当前首个错误信息。</para>
        /// </summary>
        internal string Error => error;
    
        /// <summary>
        ///   <para>指示当前数据是否有未保存修改。</para>
        /// </summary>
        internal bool IsDirty => isDirty;
    
        /// <summary>
        ///   <para>获取用于刷新网格布局的版本号。</para>
        /// </summary>
        internal int LayoutVersion => layoutVersion;
    
        /// <summary>
        ///   <para>获取用于刷新重复 ID 缓存的版本号。</para>
        /// </summary>
        internal int IdVersion => idVersion;
    
        /// <summary>
        ///   <para>数据或校验状态变化事件。</para>
        /// </summary>
        internal event Action Changed;
    
        /// <summary>
        ///   <para>记录首个编辑错误并通知界面刷新。</para>
        /// </summary>
        /// <param name="message">错误信息。</param>
        internal void ReportError(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || !string.IsNullOrEmpty(error))
            {
                return;
            }
    
            SetError(message);
            Changed?.Invoke();
        }
    
        /// <summary>
        ///   <para>从磁盘重新加载数据并清空撤销状态。</para>
        /// </summary>
        internal void Reload()
        {
            try
            {
                table = ConfigTableImporter.LoadCtable(assetPath);
                error = null;
                savedTableData = JsonUtility.ToJson(table);
                isDirty = false;
                ResetUndoState();
                layoutVersion++;
                idVersion++;
            }
            catch (Exception exception)
            {
                table = null;
                SetError(exception.Message);
                savedTableData = null;
                isDirty = false;
                ResetUndoState();
                layoutVersion++;
                idVersion++;
            }
        }
    
        /// <summary>
        ///   <para>校验并保存当前数据，可选触发 Unity 重新导入。</para>
        /// </summary>
        /// <param name="reimport">保存后是否重新导入资产。</param>
        internal bool Save(bool reimport)
        {
            if (table == null)
            {
                SetError("配置表数据不可用。");
                return false;
            }
    
            try
            {
                UpdateRecordIds();
                ConfigTableImporter.SaveCtable(assetPath, table);
                if (reimport)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
                error = null;
                savedTableData = JsonUtility.ToJson(table);
                isDirty = false;
                return true;
            }
            catch (Exception exception)
            {
                SetError(exception.Message);
                return false;
            }
        }
    
        /// <summary>
        ///   <para>检查当前数据是否满足保存条件。</para>
        /// </summary>
        internal bool CanSave()
        {
            if (table == null)
            {
                return false;
            }
    
            try
            {
                UpdateRecordIds();
                ConfigTableSchema.ValidateAsset(table);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                SetError(exception.Message);
                return false;
            }
        }
        
        /// <summary>
        ///   <para>追加一条空记录。</para>
        /// </summary>
        internal void AddRow()
        {
            var columnCount = table.fields.Length;
            var rows = table.rows ?? Array.Empty<ConfigTableRecord>();
            var updatedRows = new ConfigTableRecord[rows.Length + 1];
            Array.Copy(rows, updatedRows, rows.Length);
            var values = new string[columnCount];
            RecordUndo();
            updatedRows[rows.Length] = new ConfigTableRecord
            {
                id = string.Empty,
                values = values
            };
            table.rows = updatedRows;
            MarkDirty(true, true);
        }
    
        /// <summary>
        ///   <para>删除指定索引的记录。</para>
        /// </summary>
        /// <param name="index">记录索引。</param>
        internal void RemoveRow(int index)
        {
            var rows = table.rows;
            if (rows == null || index < 0 || index >= rows.Length)
            {
                return;
            }
    
            var updatedRows = new ConfigTableRecord[rows.Length - 1];
            if (index > 0)
            {
                Array.Copy(rows, 0, updatedRows, 0, index);
            }
            if (index < rows.Length - 1)
            {
                Array.Copy(rows, index + 1, updatedRows, index, rows.Length - index - 1);
            }
            RecordUndo();
            table.rows = updatedRows;
            MarkDirty(true, true);
        }
    
        /// <summary>
        ///   <para>追加一个字符串类型字段。</para>
        /// </summary>
        internal void AddColumn()
        {
            var columnCount = table.fields.Length;
            RecordUndo();
            Array.Resize(ref table.fields, columnCount + 1);
            Array.Resize(ref table.types, columnCount + 1);
            Array.Resize(ref table.comments, columnCount + 1);
            Array.Resize(ref table.references, columnCount + 1);
            table.fields[columnCount] = "field" + (columnCount + 1).ToString();
            table.types[columnCount] = "string";
            table.comments[columnCount] = string.Empty;
            table.references[columnCount] = string.Empty;
    
            var rows = table.rows ?? Array.Empty<ConfigTableRecord>();
            for (int i = 0; i < rows.Length; i++)
            {
                Array.Resize(ref rows[i].values, columnCount + 1);
                rows[i].values[columnCount] = string.Empty;
            }
            MarkDirty(true, true);
        }
    
        /// <summary>
        ///   <para>删除指定索引的非 ID 字段。</para>
        /// </summary>
        /// <param name="index">字段索引。</param>
        internal void RemoveColumn(int index)
        {
            if (index <= 0 || index >= table.fields.Length)
            {
                return;
            }
    
            RecordUndo();
            table.fields = RemoveAt(table.fields, index);
            table.types = RemoveAt(table.types, index);
            table.comments = RemoveAt(table.comments, index);
            table.references = RemoveAt(table.references, index);
    
            var rows = table.rows ?? Array.Empty<ConfigTableRecord>();
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].values = RemoveAt(rows[i].values, index);
            }
            MarkDirty(true, true);
        }
    
        /// <summary>
        ///   <para>更新字段名称。</para>
        /// </summary>
        /// <param name="index">索引。</param>
        /// <param name="value">字段名称。</param>
        internal void SetField(int index, string value) => SetSchemaValue(table.fields, index, value);
    
        /// <summary>
        ///   <para>更新字段类型并刷新布局。</para>
        /// </summary>
        /// <param name="index">索引。</param>
        /// <param name="value">字段类型。</param>
        internal void SetType(int index, string value) => SetSchemaValue(table.types, index, value, true);
    
        /// <summary>
        ///   <para>更新字段注释。</para>
        /// </summary>
        /// <param name="index">索引。</param>
        /// <param name="value">字段注释。</param>
        internal void SetComment(int index, string value) => SetSchemaValue(table.comments, index, value);
    
        /// <summary>
        ///   <para>更新字段引用定义。</para>
        /// </summary>
        /// <param name="index">索引。</param>
        /// <param name="value">字段引用。</param>
        internal void SetReference(int index, string value) => SetSchemaValue(table.references, index, value);
    
        /// <summary>
        ///   <para>设置结构值。</para>
        /// </summary>
        /// <param name="values">值。</param>
        /// <param name="index">索引。</param>
        /// <param name="value">值。</param>
        /// <param name="layoutChanged">布局变化。</param>
        private void SetSchemaValue(string[] values, int index, string value, bool layoutChanged = false)
        {
            if (string.Equals(values[index], value, StringComparison.Ordinal))
            {
                return;
            }
            RecordUndo();
            values[index] = value;
            MarkDirty(layoutChanged);
        }
    
        /// <summary>
        ///   <para>更新单元格值并同步 ID 列记录标识。</para>
        /// </summary>
        /// <param name="rowIndex">记录索引。</param>
        /// <param name="columnIndex">字段索引。</param>
        /// <param name="value">单元格值。</param>
        internal void SetValue(int rowIndex, int columnIndex, string value)
        {
            var record = table.rows[rowIndex];
            if (string.Equals(record.values[columnIndex], value, StringComparison.Ordinal))
            {
                return;
            }
            RecordUndo();
            record.values[columnIndex] = value;
            if (columnIndex == 0)
            {
                record.id = value;
            }
            MarkDirty(false, columnIndex == 0);
        }
    
        /// <summary>
        ///   <para>移除指定位置的元素。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="index">索引。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        private static T[] RemoveAt<T>(T[] source, int index)
        {
            var result = new T[source.Length - 1];
            if (index > 0)
            {
                Array.Copy(source, 0, result, 0, index);
            }
            if (index < source.Length - 1)
            {
                Array.Copy(source, index + 1, result, index, source.Length - index - 1);
            }
            return result;
        }
    
        /// <summary>
        ///   <para>释放撤销对象和事件订阅。</para>
        /// </summary>
        internal void Dispose()
        {
            Undo.undoRedoPerformed -= RestoreUndoState;
            if (undoState == null)
            {
                return;
            }
    
            Undo.ClearUndo(undoState);
            UnityEngine.Object.DestroyImmediate(undoState);
            undoState = null;
        }
    
        /// <summary>
        ///   <para>重置撤销状态。</para>
        /// </summary>
        private void ResetUndoState()
        {
            if (table == null)
            {
                Dispose();
                return;
            }
    
            if (undoState == null)
            {
                undoState = ScriptableObject.CreateInstance<ConfigTableDataAsset>();
                undoState.hideFlags = HideFlags.HideAndDontSave;
                Undo.undoRedoPerformed += RestoreUndoState;
            }
    
            undoState.SetTableData(table);
            Undo.ClearUndo(undoState);
        }
    
        /// <summary>
        ///   <para>记录撤销。</para>
        /// </summary>
        private void RecordUndo() => Undo.RecordObject(undoState, "编辑配置表");
    
        /// <summary>
        ///   <para>恢复撤销状态。</para>
        /// </summary>
        private void RestoreUndoState()
        {
            if (undoState == null)
            {
                return;
            }
    
            var restoredTable = undoState.CreateTableData();
            string currentTableData = JsonUtility.ToJson(table);
            string restoredTableData = JsonUtility.ToJson(restoredTable);
            if (string.Equals(currentTableData, restoredTableData, StringComparison.Ordinal))
            {
                return;
            }
    
            table = restoredTable;
            error = null;
            isDirty = !string.Equals(restoredTableData, savedTableData, StringComparison.Ordinal);
            layoutVersion++;
            idVersion++;
            Changed?.Invoke();
        }
        
        /// <summary>
        ///   <para>更新记录标识。</para>
        /// </summary>
        private void UpdateRecordIds()
        {
            var rows = table.rows ?? Array.Empty<ConfigTableRecord>();
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].id = rows[i].values[0];
            }
        }
    
        /// <summary>
        ///   <para>标记为已修改。</para>
        /// </summary>
        /// <param name="layoutChanged">布局变化。</param>
        /// <param name="idsChanged">标识变化。</param>
        private void MarkDirty(bool layoutChanged = false, bool idsChanged = false)
        {
            error = null;
            isDirty = true;
            if (layoutChanged)
            {
                layoutVersion++;
            }
            if (idsChanged)
            {
                idVersion++;
            }
        }
    
        /// <summary>
        ///   <para>设置错误。</para>
        /// </summary>
        /// <param name="message">错误信息。</param>
        private void SetError(string message) => error = GetFirstError(message);
    
        /// <summary>
        ///   <para>获取首个错误。</para>
        /// </summary>
        /// <param name="message">错误信息。</param>
        private static string GetFirstError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }
    
            string[] lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length == 0 ? null : lines[0].Trim();
        }
    
    }
    
    /// <summary>
    ///   <para>配置表网格编辑工具。</para>
    /// </summary>
    internal static class ConfigTableGridEditor
    {
        /// <summary>
        ///   <para>默认列宽。</para>
        /// </summary>
        private const float DefaultColumnWidth = 180f;
        /// <summary>
        ///   <para>最小列宽。</para>
        /// </summary>
        private const float MinimumColumnWidth = 120f;
        /// <summary>
        ///   <para>最大列宽。</para>
        /// </summary>
        private const float MaximumColumnWidth = 520f;
        /// <summary>
        ///   <para>行表头宽度。</para>
        /// </summary>
        private const float RowHeaderWidth = 82f;
        /// <summary>
        ///   <para>命令列宽。</para>
        /// </summary>
        private const float CommandColumnWidth = 88f;
        /// <summary>
        ///   <para>引用打开按钮宽度。</para>
        /// </summary>
        private const float ReferenceOpenButtonWidth = 28f;
        /// <summary>
        ///   <para>行高。</para>
        /// </summary>
        private const float RowHeight = 24f;
        /// <summary>
        ///   <para>资源模式宽度。</para>
        /// </summary>
        private const float ResourceModeWidth = 72f;
        /// <summary>
        ///   <para>结构行数。</para>
        /// </summary>
        private const int SchemaRowCount = 6;
        /// <summary>
        ///   <para>结构高度。</para>
        /// </summary>
        private const float SchemaHeight = RowHeight * SchemaRowCount;
        /// <summary>
        ///   <para>调整大小句柄宽度。</para>
        /// </summary>
        private const float ResizeHandleWidth = 6f;
        /// <summary>
        ///   <para>虚拟化内边距。</para>
        /// </summary>
        private const float VirtualizationPadding = RowHeight * 8f;
        /// <summary>
        ///   <para>引用表名称缓存时长；单位为秒。</para>
        /// </summary>
        private const double ReferenceTableNameCacheSeconds = 3d;
    
        /// <summary>
        ///   <para>表头样式。</para>
        /// </summary>
        private static readonly GUIStyle HeaderStyle = CreateStyle(EditorStyles.toolbarButton, TextAnchor.MiddleCenter);
        /// <summary>
        ///   <para>行表头样式。</para>
        /// </summary>
        private static readonly GUIStyle RowHeaderStyle = CreateStyle(EditorStyles.label, TextAnchor.MiddleCenter);
        /// <summary>
        ///   <para>单元格样式。</para>
        /// </summary>
        private static readonly GUIStyle CellStyle = CreateStyle(EditorStyles.textField, TextAnchor.MiddleLeft);
        /// <summary>
        ///   <para>按钮样式。</para>
        /// </summary>
        private static readonly GUIStyle ButtonStyle = CreateStyle(GUI.skin.button, TextAnchor.MiddleCenter);
        /// <summary>
        ///   <para>打开表按钮样式。</para>
        /// </summary>
        private static readonly GUIStyle OpenTableButtonStyle = CreateStyle(EditorStyles.miniButton, TextAnchor.MiddleCenter);
        /// <summary>
        ///   <para>空单元格样式。</para>
        /// </summary>
        private static readonly GUIStyle EmptyCellStyle = CreateStyle(EditorStyles.helpBox, TextAnchor.MiddleCenter);
        /// <summary>
        ///   <para>弹窗样式。</para>
        /// </summary>
        private static readonly GUIStyle PopupStyle = CreateStyle(EditorStyles.popup, TextAnchor.MiddleLeft);
        /// <summary>
        ///   <para>文本区域样式。</para>
        /// </summary>
        private static readonly GUIStyle TextAreaStyle = CreateTextAreaStyle();
        /// <summary>
        ///   <para>测量内容。</para>
        /// </summary>
        private static readonly GUIContent MeasurementContent = new GUIContent();
        /// <summary>
        ///   <para>支持的类型。</para>
        /// </summary>
        private static readonly string[] SupportedTypes = ConfigTableSchema.GetSupportedTypes();
        /// <summary>
        ///   <para>标识支持的类型。</para>
        /// </summary>
        private static readonly string[] IdSupportedTypes = { "int", "string" };
        /// <summary>
        ///   <para>类型格式提示。</para>
        /// </summary>
        private static readonly string TypeFormatTooltip =
            "数组值使用 " + ConfigTableSchema.ArraySeparator +
            " 分隔。元组字段使用 " + ConfigTableSchema.TupleSeparator +
            " 分隔，例如：(int" + ConfigTableSchema.TupleSeparator +
            "int)[] 和 2" + ConfigTableSchema.TupleSeparator + "10" +
            ConfigTableSchema.ArraySeparator + "3" + ConfigTableSchema.TupleSeparator + "5。";
        /// <summary>
        ///   <para>无效引用类型提示。</para>
        /// </summary>
        private const string InvalidReferenceTypeTooltip =
            "引用字段需使用 int 或 string 类型；数组或元组需至少包含一个 int 或 string 元素。";
        /// <summary>
        ///   <para>布尔值列表。</para>
        /// </summary>
        private static readonly string[] BooleanValues = { string.Empty, "false", "true" };
        /// <summary>
        ///   <para>布尔值选项标签。</para>
        /// </summary>
        private static readonly string[] BooleanOptionLabels = { "（空）", "否", "是" };
        /// <summary>
        ///   <para>资源模式选项。</para>
        /// </summary>
        private static readonly GUIContent[] ResourceModeOptions =
        {
            new GUIContent("（无）", "资源加载方式，仅用于编辑器预览，不会写入配置表数据。"),
            new GUIContent("资源", "使用 Resources.Load 预览并选择资源。"),
        };
        /// <summary>
        ///   <para>换行字符。</para>
        /// </summary>
        private static readonly char[] LineBreakCharacters = { '\r', '\n' };
        /// <summary>
        ///   <para>无效值颜色。</para>
        /// </summary>
        private static readonly Color InvalidValueColor = new Color(1f, 0.38f, 0.38f, 1f);
        /// <summary>
        ///   <para>搜索匹配颜色。</para>
        /// </summary>
        private static readonly Color SearchMatchColor = new Color(0.2f, 0.62f, 1f, 1f);
        /// <summary>
        ///   <para>冻结列颜色。</para>
        /// </summary>
        private static readonly Color FrozenColumnColor = new Color(0.48f, 0.53f, 0.58f, 1f);
        /// <summary>
        ///   <para>冻结列背景颜色。</para>
        /// </summary>
        private static readonly Color FrozenColumnBackgroundColor = new Color(0.14f, 0.16f, 0.18f, 1f);
        /// <summary>
        ///   <para>缓存的引用表名称。</para>
        /// </summary>
        private static string[] cachedReferenceTableNames;
        /// <summary>
        ///   <para>缓存的引用表路径。</para>
        /// </summary>
        private static Dictionary<string, string> cachedReferenceTablePaths;
        /// <summary>
        ///   <para>引用表名称过期时间。</para>
        /// </summary>
        private static double referenceTableNamesExpireTime;
        /// <summary>
        ///   <para>分表路径缓存时长；单位为秒。</para>
        /// </summary>
        private const double PartitionAssetPathCacheSeconds = 3d;
        /// <summary>
        ///   <para>缓存的分表资源路径。</para>
        /// </summary>
        private static string cachedPartitionAssetPath;
        /// <summary>
        ///   <para>缓存的分表资源路径。</para>
        /// </summary>
        private static string[] cachedPartitionAssetPaths;
        /// <summary>
        ///   <para>分表资源路径过期时间。</para>
        /// </summary>
        private static double partitionAssetPathsExpireTime;
    
        /// <summary>
        ///   <para>绘制不带搜索条件的配置表网格。</para>
        /// </summary>
        /// <param name="editState">当前编辑状态。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="scrollPosition">当前滚动位置。</param>
        /// <param name="viewportHeight">可视区域高度。</param>
        internal static void Draw(ConfigTableEditState editState, ConfigTableGridLayout gridLayout, ref Vector2 scrollPosition,
            float viewportHeight = 0f) => Draw(editState, gridLayout, ref scrollPosition, ConfigTableSearchQuery.Empty, viewportHeight);
    
        /// <summary>
        ///   <para>绘制配置表网格并高亮搜索匹配项。</para>
        /// </summary>
        /// <param name="editState">当前编辑状态。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="scrollPosition">当前滚动位置。</param>
        /// <param name="searchQuery">用于高亮的搜索查询。</param>
        /// <param name="viewportHeight">可视区域高度。</param>
        internal static void Draw(ConfigTableEditState editState, ConfigTableGridLayout gridLayout, ref Vector2 scrollPosition,
            ConfigTableSearchQuery searchQuery, float viewportHeight = 0f)
        {
            var table = editState.Table;
            bool isManagedPartition = ConfigTableImporter.IsSplitPartition(editState.AssetPath);
            searchQuery = searchQuery ?? ConfigTableSearchQuery.Empty;
            EnsureColumnWidths(gridLayout, table.fields.Length);
            EnsureRowMetrics(gridLayout, table, editState.LayoutVersion);
            EnsureDuplicateIds(gridLayout, table.rows, editState.IdVersion);
            DrawStatus(editState, table);
            if (isManagedPartition)
            {
                EditorGUILayout.HelpBox("当前为分表。可直接修改非 ID 数据值并保存；保存后会刷新运行时数据。字段、类型、标识列与行结构必须从 CSV 修改后重新构建。使用“导出 CSV”可根据分表恢复 CSV。", MessageType.Info);
            }
    
            HandleFrozenRowInput(gridLayout, scrollPosition);
            HandleFrozenColumnInput(gridLayout, scrollPosition);
            HandleMiddleMousePan(gridLayout, ref scrollPosition);
    
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawColumnCommands(editState, gridLayout, isManagedPartition);
            DrawColumnNumbers(table.fields.Length, gridLayout);
            DrawSchemaRow("字段", table.fields, editState.SetField, ConfigTableSearchTarget.Field, searchQuery, gridLayout, 2, isManagedPartition);
            DrawTypeRow(table.types, editState.SetType, gridLayout, isManagedPartition);
            DrawSchemaRow("注释", table.comments, editState.SetComment, ConfigTableSearchTarget.Comment, searchQuery, gridLayout, 4, isManagedPartition);
            DrawReferenceRow(table.types, table.references, editState.SetReference, gridLayout, isManagedPartition);
            DrawRecords(table, editState, searchQuery, gridLayout, scrollPosition.y, viewportHeight, isManagedPartition);
            DrawAddRow(editState, table.fields.Length, gridLayout.columnWidths, isManagedPartition);
            EditorGUILayout.EndScrollView();
            gridLayout.scrollViewRect = GUILayoutUtility.GetLastRect();
            DrawFrozenColumn(gridLayout.scrollViewRect, table, gridLayout, scrollPosition, viewportHeight);
            DrawFrozenRow(gridLayout.scrollViewRect, table, gridLayout, scrollPosition);
            DrawErrorLog(editState);
        }
        
        /// <summary>
        ///   <para>绘制状态。</para>
        /// </summary>
        /// <param name="editState">编辑状态。</param>
        /// <param name="table">表。</param>
        private static void DrawStatus(ConfigTableEditState editState, ConfigTableAsset table)
        {
            var rowCount = table.rows?.Length ?? 0;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(rowCount + " 行  |  " + table.fields.Length + " 列", EditorStyles.miniBoldLabel);
            DrawPartitionSelector(editState.AssetPath);
            EditorGUILayout.EndHorizontal();
        }
    
        /// <summary>
        ///   <para>绘制错误日志。</para>
        /// </summary>
        /// <param name="editState">编辑状态。</param>
        private static void DrawErrorLog(ConfigTableEditState editState)
        {
            if (string.IsNullOrEmpty(editState.Error))
            {
                return;
            }
    
            EditorGUILayout.LabelField("错误日志", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(editState.Error, MessageType.Error);
        }
    
        /// <summary>
        ///   <para>绘制分表选择器。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        private static void DrawPartitionSelector(string assetPath)
        {
            string[] partitionAssetPaths = GetPartitionAssetPaths(assetPath);
            if (partitionAssetPaths.Length <= 1)
            {
                return;
            }
    
            GUILayout.FlexibleSpace();
            var labels = new string[partitionAssetPaths.Length];
            int selectedIndex = 0;
            for (int i = 0; i < partitionAssetPaths.Length; i++)
            {
                labels[i] = Path.GetFileNameWithoutExtension(partitionAssetPaths[i]);
                if (string.Equals(partitionAssetPaths[i], assetPath, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                }
            }
    
            int nextIndex = EditorGUILayout.Popup(selectedIndex, labels, EditorStyles.toolbarPopup, GUILayout.Width(180f));
            if (nextIndex != selectedIndex)
            {
                ConfigTableEditorWindow.Open(partitionAssetPaths[nextIndex]);
                GUIUtility.ExitGUI();
            }
        }
    
        /// <summary>
        ///   <para>获取分表资源路径。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        private static string[] GetPartitionAssetPaths(string assetPath)
        {
            if (cachedPartitionAssetPaths != null &&
                string.Equals(cachedPartitionAssetPath, assetPath, StringComparison.OrdinalIgnoreCase) &&
                EditorApplication.timeSinceStartup < partitionAssetPathsExpireTime)
            {
                return cachedPartitionAssetPaths;
            }
    
            cachedPartitionAssetPath = assetPath;
            cachedPartitionAssetPaths = ConfigTableImporter.GetRelatedClientPartitionAssetPaths(assetPath);
            partitionAssetPathsExpireTime = EditorApplication.timeSinceStartup + PartitionAssetPathCacheSeconds;
            return cachedPartitionAssetPaths;
        }
    
        /// <summary>
        ///   <para>绘制列命令。</para>
        /// </summary>
        /// <param name="editState">编辑状态。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="schemaReadOnly">结构只读。</param>
        private static void DrawColumnCommands(ConfigTableEditState editState, ConfigTableGridLayout gridLayout, bool schemaReadOnly)
        {
            var columnCount = editState.Table.fields.Length;
            BeginGridRow();
            DrawFrozenRowToggle(gridLayout, 0, GUIContent.none, HeaderStyle);
            for (int i = 0; i < columnCount; i++)
            {
                GUI.enabled = !schemaReadOnly && i > 0;
                if (GUILayout.Button(new GUIContent("删除 " + (i + 1).ToString(), i == 0 ? "标识列不能被移除。" : "删除列"), ButtonStyle, CellOptions(gridLayout.columnWidths[i])))
                {
                    editState.RemoveColumn(i);
                    RemoveColumnWidth(gridLayout, i);
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = true;
            }
            GUI.enabled = !schemaReadOnly;
            if (GUILayout.Button(new GUIContent("+ 列", "添加列"), ButtonStyle, CellOptions(CommandColumnWidth)))
            {
                editState.AddColumn();
                AddColumnWidth(gridLayout);
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
            EndGridRow();
        }
    
        /// <summary>
        ///   <para>绘制列号。</para>
        /// </summary>
        /// <param name="columnCount">列数。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        private static void DrawColumnNumbers(int columnCount, ConfigTableGridLayout gridLayout)
        {
            BeginGridRow();
            DrawFrozenRowToggle(gridLayout, 1, GUIContent.none, HeaderStyle);
            for (int i = 0; i < columnCount; i++)
            {
                Rect headerRect = GUILayoutUtility.GetRect(GUIContent.none, HeaderStyle, CellOptions(gridLayout.columnWidths[i]));
                DrawResizeHandle(headerRect, i, gridLayout);
                Color backgroundColor = GUI.backgroundColor;
                if (gridLayout.frozenColumn == i)
                {
                    GUI.backgroundColor = new Color(0.34f, 0.38f, 0.42f, 1f);
                }
                if (Event.current.type != EventType.Used && GUI.Button(headerRect,
                    new GUIContent((i + 1).ToString(), gridLayout.frozenColumn == i
                        ? "取消固定列"
                        : "固定当前列"), HeaderStyle))
                {
                    gridLayout.frozenColumn = gridLayout.frozenColumn == i ? -1 : i;
                    GUI.backgroundColor = backgroundColor;
                    GUI.changed = true;
                }
                GUI.backgroundColor = backgroundColor;
                if (gridLayout.frozenColumn == i && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f), FrozenColumnColor);
                }
            }
            DrawCommandPlaceholder();
            EndGridRow();
        }
    
        /// <summary>
        ///   <para>绘制冻结行切换。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="rowIndex">记录索引。</param>
        /// <param name="content">内容。</param>
        /// <param name="style">样式。</param>
        private static void DrawFrozenRowToggle(ConfigTableGridLayout gridLayout, int rowIndex, GUIContent content, GUIStyle style)
        {
            Rect rowHeaderRect = GUILayoutUtility.GetRect(content, style, CellOptions(RowHeaderWidth));
            bool isFrozen = gridLayout.frozenRow == rowIndex;
            Color backgroundColor = GUI.backgroundColor;
            if (isFrozen)
            {
                GUI.backgroundColor = FrozenColumnBackgroundColor;
            }
    
            if (Event.current.type != EventType.Used && GUI.Button(rowHeaderRect,
                new GUIContent(content.text, isFrozen ? "取消固定行" : "固定当前行"), style))
            {
                gridLayout.frozenRow = isFrozen ? -1 : rowIndex;
                GUI.changed = true;
            }
            GUI.backgroundColor = backgroundColor;
    
            if (isFrozen && Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(rowHeaderRect.x, rowHeaderRect.yMax - 1f, rowHeaderRect.width, 1f), FrozenColumnColor);
            }
        }
    
        /// <summary>
        ///   <para>处理鼠标中键平移。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="scrollPosition">当前滚动位置。</param>
        private static void HandleMiddleMousePan(ConfigTableGridLayout gridLayout, ref Vector2 scrollPosition)
        {
            const int panControlHint = 173813;
            int controlId = GUIUtility.GetControlID(panControlHint, FocusType.Passive);
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 2)
            {
                gridLayout.panStartMousePosition = currentEvent.mousePosition;
                gridLayout.panStartScrollPosition = scrollPosition;
                gridLayout.isPanning = true;
                GUIUtility.hotControl = controlId;
                EditorWindow.focusedWindow?.Repaint();
                currentEvent.Use();
            }
            else if (gridLayout.isPanning && (currentEvent.type == EventType.MouseDrag || currentEvent.type == EventType.MouseMove))
            {
                Vector2 delta = gridLayout.panStartMousePosition - currentEvent.mousePosition;
                float contentWidth = RowHeaderWidth + CommandColumnWidth;
                for (int i = 0; i < gridLayout.columnWidths.Length; i++)
                {
                    contentWidth += gridLayout.columnWidths[i];
                }
    
                float contentHeight = SchemaHeight + RowHeight;
                if (gridLayout.rowOffsets != null && gridLayout.rowOffsets.Length > 0)
                {
                    contentHeight += gridLayout.rowOffsets[gridLayout.rowOffsets.Length - 1];
                }
    
                scrollPosition = new Vector2(
                    Mathf.Clamp(gridLayout.panStartScrollPosition.x + delta.x, 0f, Mathf.Max(0f, contentWidth - gridLayout.scrollViewRect.width)),
                    Mathf.Clamp(gridLayout.panStartScrollPosition.y + delta.y, 0f, Mathf.Max(0f, contentHeight - gridLayout.scrollViewRect.height)));
                GUI.changed = true;
                EditorWindow.focusedWindow?.Repaint();
                currentEvent.Use();
            }
            else if (gridLayout.isPanning && currentEvent.type == EventType.MouseUp && currentEvent.button == 2)
            {
                if (GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                }
                gridLayout.isPanning = false;
                currentEvent.Use();
            }
        }
    
        /// <summary>
        ///   <para>处理冻结行输入。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="scrollPosition">当前滚动位置。</param>
        private static void HandleFrozenRowInput(ConfigTableGridLayout gridLayout, Vector2 scrollPosition)
        {
            int rowIndex = gridLayout.frozenRow;
            Rect frozenRect = GetFrozenRowRect(gridLayout.scrollViewRect, rowIndex);
            Rect rowHeaderRect = new Rect(frozenRect.x - scrollPosition.x, frozenRect.y, RowHeaderWidth, frozenRect.height);
            if (HandleFrozenInput(frozenRect, rowHeaderRect, 173815, ref gridLayout.frozenRowInputControl, ref gridLayout.cancelFrozenRow))
            {
                gridLayout.frozenRow = -1;
                GUI.changed = true;
            }
        }
    
        /// <summary>
        ///   <para>处理冻结列输入。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="scrollPosition">当前滚动位置。</param>
        private static void HandleFrozenColumnInput(ConfigTableGridLayout gridLayout, Vector2 scrollPosition)
        {
            int columnIndex = gridLayout.frozenColumn;
            Rect frozenRect = GetFrozenColumnRect(gridLayout.scrollViewRect, gridLayout, columnIndex);
            Rect indexRect = new Rect(frozenRect.x, frozenRect.y + RowHeight - scrollPosition.y, frozenRect.width, RowHeight);
            if (HandleFrozenInput(frozenRect, indexRect, 173814, ref gridLayout.frozenColumnInputControl, ref gridLayout.cancelFrozenColumn))
            {
                gridLayout.frozenColumn = -1;
                GUI.changed = true;
            }
        }
    
        /// <summary>
        ///   <para>处理冻结输入。</para>
        /// </summary>
        /// <param name="frozenRect">冻结区域。</param>
        /// <param name="cancelRect">取消区域。</param>
        /// <param name="controlHint">控件标识提示。</param>
        /// <param name="inputControl">输入控件。</param>
        /// <param name="cancel">取消。</param>
        private static bool HandleFrozenInput(Rect frozenRect, Rect cancelRect, int controlHint, ref int inputControl, ref bool cancel)
        {
            if (frozenRect.width <= 0f || frozenRect.height <= 0f)
            {
                return false;
            }
    
            int controlId = GUIUtility.GetControlID(controlHint, FocusType.Passive, frozenRect);
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && frozenRect.Contains(currentEvent.mousePosition))
            {
                inputControl = controlId;
                cancel = cancelRect.Contains(currentEvent.mousePosition);
                GUIUtility.hotControl = controlId;
                currentEvent.Use();
                return false;
            }
    
            if (currentEvent.type != EventType.MouseUp || currentEvent.button != 0 || GUIUtility.hotControl != inputControl)
            {
                return false;
            }
    
            bool canceled = cancel && cancelRect.Contains(currentEvent.mousePosition);
            inputControl = 0;
            cancel = false;
            GUIUtility.hotControl = 0;
            currentEvent.Use();
            return canceled;
        }
    
        /// <summary>
        ///   <para>绘制冻结列。</para>
        /// </summary>
        /// <param name="viewRect">页面区域。</param>
        /// <param name="table">表。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="scrollPosition">当前滚动位置。</param>
        /// <param name="viewportHeight">可视区域高度。</param>
        private static void DrawFrozenColumn(Rect viewRect, ConfigTableAsset table, ConfigTableGridLayout gridLayout, Vector2 scrollPosition,
            float viewportHeight)
        {
            int columnIndex = gridLayout.frozenColumn;
            Rect frozenRect = GetFrozenColumnRect(viewRect, gridLayout, columnIndex);
            if (frozenRect.width <= 0f || frozenRect.height <= 0f)
            {
                return;
            }
    
            GUI.BeginClip(frozenRect);
            float width = frozenRect.width;
            Color backgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.34f, 0.38f, 0.42f, 1f);
            EditorGUI.DrawRect(new Rect(0f, 0f, width, frozenRect.height), FrozenColumnBackgroundColor);
            EditorGUI.DrawRect(new Rect(0f, 0f, 1f, frozenRect.height), FrozenColumnColor);
            EditorGUI.DrawRect(new Rect(width - 1f, 0f, 1f, frozenRect.height), FrozenColumnColor);
            float y = -scrollPosition.y;
            GUI.Box(new Rect(0f, y, width, RowHeight), "已固定", ButtonStyle);
            y += RowHeight;
            GUI.Button(new Rect(0f, y, width, RowHeight),
                new GUIContent((columnIndex + 1).ToString(), "取消固定列"), HeaderStyle);
            y += RowHeight;
            GUI.Box(new Rect(0f, y, width, RowHeight), table.fields[columnIndex], CellStyle);
            y += RowHeight;
            GUI.Box(new Rect(0f, y, width, RowHeight), table.types[columnIndex], PopupStyle);
            y += RowHeight;
            GUI.Box(new Rect(0f, y, width, RowHeight), table.comments[columnIndex], CellStyle);
            y += RowHeight;
            GUI.Box(new Rect(0f, y, width, RowHeight), GetReferenceLabel(table.references[columnIndex]), PopupStyle);
    
            ConfigTableRecord[] rows = table.rows ?? Array.Empty<ConfigTableRecord>();
            GetVisibleRowRange(gridLayout, rows.Length, scrollPosition.y, Mathf.Max(viewRect.height, viewportHeight), out var firstRowIndex, out var lastRowIndexExclusive);
            y = SchemaHeight + gridLayout.rowOffsets[firstRowIndex] - scrollPosition.y;
            for (int rowIndex = firstRowIndex; rowIndex < lastRowIndexExclusive; rowIndex++)
            {
                ConfigTableRecord row = rows[rowIndex];
                string value = row != null && row.values != null && columnIndex < row.values.Length ? row.values[columnIndex] : string.Empty;
                GUI.Box(new Rect(0f, y, width, gridLayout.rowHeights[rowIndex]), value,
                    IsMultilineString(table.types[columnIndex]) ? TextAreaStyle : CellStyle);
                y += gridLayout.rowHeights[rowIndex];
            }
            GUI.backgroundColor = backgroundColor;
            GUI.EndClip();
        }
    
        /// <summary>
        ///   <para>绘制冻结行。</para>
        /// </summary>
        /// <param name="viewRect">页面区域。</param>
        /// <param name="table">表。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="scrollPosition">当前滚动位置。</param>
        private static void DrawFrozenRow(Rect viewRect, ConfigTableAsset table, ConfigTableGridLayout gridLayout, Vector2 scrollPosition)
        {
            int rowIndex = gridLayout.frozenRow;
            Rect frozenRect = GetFrozenRowRect(viewRect, rowIndex);
            if (frozenRect.width <= 0f || frozenRect.height <= 0f)
            {
                return;
            }
    
            GUI.BeginClip(viewRect);
            float x = -scrollPosition.x;
            float y = rowIndex * RowHeight;
            Color backgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.34f, 0.38f, 0.42f, 1f);
            EditorGUI.DrawRect(new Rect(0f, y, viewRect.width, RowHeight), FrozenColumnBackgroundColor);
    
            if (rowIndex == 0)
            {
                GUI.Box(new Rect(x, y, RowHeaderWidth, RowHeight), GUIContent.none, HeaderStyle);
                x += RowHeaderWidth;
                for (int i = 0; i < table.fields.Length; i++)
                {
                    GUI.Box(new Rect(x, y, gridLayout.columnWidths[i], RowHeight), "删除 " + (i + 1).ToString(), ButtonStyle);
                    x += gridLayout.columnWidths[i];
                }
                GUI.Box(new Rect(x, y, CommandColumnWidth, RowHeight), "+ 列", ButtonStyle);
            }
            else if (rowIndex == 1)
            {
                GUI.Box(new Rect(x, y, RowHeaderWidth, RowHeight), GUIContent.none, HeaderStyle);
                x += RowHeaderWidth;
                for (int i = 0; i < table.fields.Length; i++)
                {
                    GUI.Box(new Rect(x, y, gridLayout.columnWidths[i], RowHeight), (i + 1).ToString(), HeaderStyle);
                    x += gridLayout.columnWidths[i];
                }
                GUI.Box(new Rect(x, y, CommandColumnWidth, RowHeight), GUIContent.none, EmptyCellStyle);
            }
            else
            {
                string label;
                string[] values;
                GUIStyle valueStyle;
                switch (rowIndex)
                {
                    case 2:
                        label = "字段";
                        values = table.fields;
                        valueStyle = CellStyle;
                        break;
                    case 3:
                        label = "类型";
                        values = table.types;
                        valueStyle = PopupStyle;
                        break;
                    case 4:
                        label = "注释";
                        values = table.comments;
                        valueStyle = CellStyle;
                        break;
                    default:
                        label = "引用";
                        values = table.references;
                        valueStyle = PopupStyle;
                        break;
                }
    
                GUI.Box(new Rect(x, y, RowHeaderWidth, RowHeight), label, RowHeaderStyle);
                x += RowHeaderWidth;
                for (int i = 0; i < values.Length; i++)
                {
                    GUI.Box(new Rect(x, y, gridLayout.columnWidths[i], RowHeight), rowIndex == 5 ? GetReferenceLabel(values[i]) : values[i], valueStyle);
                    x += gridLayout.columnWidths[i];
                }
                GUI.Box(new Rect(x, y, CommandColumnWidth, RowHeight), GUIContent.none, EmptyCellStyle);
            }
    
            EditorGUI.DrawRect(new Rect(0f, y + RowHeight - 1f, viewRect.width, 1f), FrozenColumnColor);
            GUI.backgroundColor = backgroundColor;
            GUI.EndClip();
        }
    
        /// <summary>
        ///   <para>获取冻结行区域。</para>
        /// </summary>
        /// <param name="viewRect">页面区域。</param>
        /// <param name="rowIndex">记录索引。</param>
        private static Rect GetFrozenRowRect(Rect viewRect, int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= SchemaRowCount || viewRect.width <= 0f)
            {
                return Rect.zero;
            }
    
            float y = viewRect.y + rowIndex * RowHeight;
            float height = Mathf.Min(RowHeight, viewRect.yMax - y);
            return height > 0f ? new Rect(viewRect.x, y, viewRect.width, height) : Rect.zero;
        }
    
        /// <summary>
        ///   <para>获取冻结列区域。</para>
        /// </summary>
        /// <param name="viewRect">页面区域。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="columnIndex">字段索引。</param>
        private static Rect GetFrozenColumnRect(Rect viewRect, ConfigTableGridLayout gridLayout, int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= gridLayout.columnWidths.Length)
            {
                return Rect.zero;
            }
    
            float width = Mathf.Min(gridLayout.columnWidths[columnIndex], viewRect.width - RowHeaderWidth);
            return width > 0f ? new Rect(viewRect.x + RowHeaderWidth, viewRect.y, width, viewRect.height) : Rect.zero;
        }
    
        /// <summary>
        ///   <para>绘制列宽调整手柄。</para>
        /// </summary>
        /// <param name="headerRect">表头区域。</param>
        /// <param name="columnIndex">字段索引。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        private static void DrawResizeHandle(Rect headerRect, int columnIndex, ConfigTableGridLayout gridLayout)
        {
            Rect handleRect = new Rect(headerRect.xMax - ResizeHandleWidth * 0.5f, headerRect.y, ResizeHandleWidth, headerRect.height);
            int controlId = GUIUtility.GetControlID(columnIndex + 81201, FocusType.Passive, handleRect);
            Event currentEvent = Event.current;
    
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal, controlId);
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && handleRect.Contains(currentEvent.mousePosition))
            {
                gridLayout.resizingColumn = columnIndex;
                gridLayout.resizeStartX = currentEvent.mousePosition.x;
                gridLayout.resizeStartWidth = gridLayout.columnWidths[columnIndex];
                GUIUtility.hotControl = controlId;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && GUIUtility.hotControl == controlId && gridLayout.resizingColumn == columnIndex)
            {
                float width = gridLayout.resizeStartWidth + currentEvent.mousePosition.x - gridLayout.resizeStartX;
                gridLayout.columnWidths[columnIndex] = Mathf.Clamp(width, MinimumColumnWidth, MaximumColumnWidth);
                GUI.changed = true;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                GUIUtility.hotControl = 0;
                gridLayout.resizingColumn = -1;
                currentEvent.Use();
            }
    
            if (Event.current.type == EventType.Repaint)
            {
                Color color = gridLayout.resizingColumn == columnIndex ? new Color(0.35f, 0.7f, 1f, 1f) : new Color(1f, 1f, 1f, 0.22f);
                EditorGUI.DrawRect(new Rect(headerRect.xMax - 1f, headerRect.y + 2f, 2f, headerRect.height - 4f), color);
            }
        }
    
        /// <summary>
        ///   <para>绘制结构行。</para>
        /// </summary>
        /// <param name="label">标签。</param>
        /// <param name="values">值。</param>
        /// <param name="setValue">值写入回调。</param>
        /// <param name="target">目标。</param>
        /// <param name="searchQuery">用于高亮的搜索查询。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="rowIndex">记录索引。</param>
        /// <param name="schemaReadOnly">结构只读。</param>
        private static void DrawSchemaRow(string label, string[] values, Action<int, string> setValue, ConfigTableSearchTarget target,
            ConfigTableSearchQuery searchQuery, ConfigTableGridLayout gridLayout, int rowIndex, bool schemaReadOnly)
        {
            BeginGridRow();
            DrawFrozenRowToggle(gridLayout, rowIndex, new GUIContent(label), RowHeaderStyle);
            for (int i = 0; i < values.Length; i++)
            {
                Rect cellRect = GUILayoutUtility.GetRect(GUIContent.none, CellStyle, CellOptions(gridLayout.columnWidths[i]));
                using (new EditorGUI.DisabledGroupScope(schemaReadOnly))
                {
                    EditorGUI.BeginChangeCheck();
                    string value = EditorGUI.TextField(cellRect, values[i], CellStyle);
                    if (EditorGUI.EndChangeCheck())
                    {
                        setValue(i, value);
                    }
                }
                DrawSearchMatchBorder(cellRect, ConfigTableSearch.MatchesCell(values[i], target, searchQuery));
            }
            DrawCommandPlaceholder();
            EndGridRow();
        }
        
        /// <summary>
        ///   <para>绘制类型行。</para>
        /// </summary>
        /// <param name="types">字段类型。</param>
        /// <param name="setType">类型写入回调。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="schemaReadOnly">结构只读。</param>
        private static void DrawTypeRow(string[] types, Action<int, string> setType, ConfigTableGridLayout gridLayout, bool schemaReadOnly)
        {
            BeginGridRow();
            DrawFrozenRowToggle(gridLayout, 3, new GUIContent("类型"), RowHeaderStyle);
            for (int i = 0; i < types.Length; i++)
            {
                Rect cellRect = GUILayoutUtility.GetRect(GUIContent.none, PopupStyle, CellOptions(gridLayout.columnWidths[i]));
                Rect typeRect = cellRect;
                if (IsMultilineString(types[i]))
                {
                    float resourceModeWidth = Mathf.Min(ResourceModeWidth, cellRect.width * 0.5f);
                    Rect resourceModeRect = new Rect(cellRect.xMax - resourceModeWidth, cellRect.y, resourceModeWidth, cellRect.height);
                    typeRect.width -= resourceModeWidth;
                    ConfigTableResourcePreviewMode resourceMode = GetResourcePreviewMode(gridLayout, i);
                    int nextResourceModeIndex = EditorGUI.Popup(resourceModeRect, (int)resourceMode, ResourceModeOptions, PopupStyle);
                    var nextResourceMode = (ConfigTableResourcePreviewMode)Mathf.Clamp(
                        nextResourceModeIndex, 0, ResourceModeOptions.Length - 1);
                    if (nextResourceMode != resourceMode)
                    {
                        gridLayout.resourceModes[i] = nextResourceMode;
                        GUI.changed = true;
                        GUIUtility.ExitGUI();
                    }
                }
    
                int columnIndex = i;
                string[] supportedTypes = i == 0 ? IdSupportedTypes : SupportedTypes;
                using (new EditorGUI.DisabledGroupScope(schemaReadOnly))
                {
                    if (DrawSearchablePopup(typeRect, types[i], supportedTypes, TypeFormatTooltip))
                    {
                        ShowSearchablePopup(typeRect, types[i], supportedTypes, value => setType(columnIndex, value));
                    }
                }
            }
            DrawCommandPlaceholder();
            EndGridRow();
        }
        
        /// <summary>
        ///   <para>绘制引用行。</para>
        /// </summary>
        /// <param name="types">字段类型。</param>
        /// <param name="references">字段引用。</param>
        /// <param name="setReference">引用写入回调。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="schemaReadOnly">结构只读。</param>
        private static void DrawReferenceRow(string[] types, string[] references, Action<int, string> setReference,
            ConfigTableGridLayout gridLayout, bool schemaReadOnly)
        {
            EnsureReferenceTableCache();
            string[] tableNames = cachedReferenceTableNames;
            BeginGridRow();
            DrawFrozenRowToggle(gridLayout, 5, new GUIContent("引用"), RowHeaderStyle);
            for (int i = 0; i < references.Length; i++)
            {
                string[] options = GetReferenceOptions(tableNames, types[i], references[i]);
                Rect cellRect = GUILayoutUtility.GetRect(GUIContent.none, PopupStyle, CellOptions(gridLayout.columnWidths[i]));
                string tableName = ConfigTableSchema.GetReferenceTargetName(references[i]);
                string assetPath = string.IsNullOrEmpty(tableName) ||
                    !cachedReferenceTablePaths.TryGetValue(tableName, out var referenceAssetPath)
                    ? string.Empty
                    : referenceAssetPath;
                bool canOpenReferencedTable = !string.IsNullOrEmpty(assetPath);
                Rect selectorRect = canOpenReferencedTable
                    ? new Rect(cellRect.x, cellRect.y, cellRect.width - ReferenceOpenButtonWidth, cellRect.height)
                    : cellRect;
                int columnIndex = i;
                string currentLabel = GetReferenceLabel(references[i]);
                bool supportsReference = CanReferenceTableId(types[i]);
                using (new EditorGUI.DisabledGroupScope(schemaReadOnly || !supportsReference))
                {
                    if (DrawSearchablePopup(selectorRect, currentLabel, options,
                        supportsReference ? null : InvalidReferenceTypeTooltip))
                    {
                        ShowSearchablePopup(selectorRect, currentLabel, options, value => setReference(columnIndex, GetReferenceValue(value)));
                    }
                }
                if (canOpenReferencedTable)
                {
                    Rect openRect = new Rect(selectorRect.xMax, cellRect.y, ReferenceOpenButtonWidth, cellRect.height);
                    DrawReferenceOpenMenu(openRect, tableName, assetPath);
                }
            }
            DrawCommandPlaceholder();
            EndGridRow();
        }
    
        /// <summary>
        ///   <para>获取引用选项。</para>
        /// </summary>
        /// <param name="tableNames">表名称。</param>
        /// <param name="type">类型。</param>
        /// <param name="currentReference">当前引用。</param>
        private static string[] GetReferenceOptions(string[] tableNames, string type, string currentReference)
        {
            type = ConfigTableSchema.NormalizeType(type);
            var options = new List<string> { "（无）" };
            bool isTuple = ConfigTableSchema.TryGetTupleArrayElementTypes(type, out var tupleElementTypes);
            for (int i = 0; i < tableNames.Length; i++)
            {
                if (!isTuple)
                {
                    if (CanReferenceTableId(type))
                    {
                        options.Add(tableNames[i]);
                    }
                    continue;
                }
    
                for (int elementIndex = 0; elementIndex < tupleElementTypes.Length; elementIndex++)
                {
                    if (IsReferenceIdType(tupleElementTypes[elementIndex]))
                    {
                        options.Add(tableNames[i] + "[" + elementIndex.ToString() + "]");
                    }
                }
            }
    
            if (!string.IsNullOrEmpty(currentReference) && !options.Contains(currentReference))
            {
                options.Insert(0, "（无效） " + currentReference);
            }
            return options.ToArray();
        }
    
        /// <summary>
        ///   <para>判断是否允许引用表标识。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static bool CanReferenceTableId(string type)
        {
            type = ConfigTableSchema.NormalizeType(type);
            if (ConfigTableSchema.TryGetTupleArrayElementTypes(type, out var tupleElementTypes))
            {
                for (int i = 0; i < tupleElementTypes.Length; i++)
                {
                    if (IsReferenceIdType(tupleElementTypes[i]))
                    {
                        return true;
                    }
                }
                return false;
            }
    
            string elementType = type.EndsWith("[]", StringComparison.Ordinal) ? type.Substring(0, type.Length - 2) : type;
            return IsReferenceIdType(elementType);
        }
    
        /// <summary>
        ///   <para>判断是否为引用标识类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static bool IsReferenceIdType(string type)
        {
            return string.Equals(type, "int", StringComparison.Ordinal) ||
                string.Equals(type, "string", StringComparison.Ordinal);
        }
    
        /// <summary>
        ///   <para>获取引用值。</para>
        /// </summary>
        /// <param name="option">选项。</param>
        private static string GetReferenceValue(string option) => option.StartsWith("（无效） ", StringComparison.Ordinal) || option == "（无）" ? string.Empty : option;
    
        /// <summary>
        ///   <para>获取引用标签。</para>
        /// </summary>
        /// <param name="reference">引用。</param>
        private static string GetReferenceLabel(string reference) => string.IsNullOrEmpty(reference) ? "（无）" : reference;
    
        /// <summary>
        ///   <para>绘制引用打开菜单。</para>
        /// </summary>
        /// <param name="cellRect">单元格区域。</param>
        /// <param name="tableName">表名称。</param>
        /// <param name="assetPath">资源路径。</param>
        private static void DrawReferenceOpenMenu(Rect cellRect, string tableName, string assetPath)
        {
            if (GUI.Button(cellRect, GetOpenTableIcon(), OpenTableButtonStyle))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent(tableName), false, () => ConfigTableEditorWindow.Open(assetPath));
                menu.DropDown(cellRect);
            }
        }
    
        /// <summary>
        ///   <para>确保引用表缓存。</para>
        /// </summary>
        private static void EnsureReferenceTableCache()
        {
            if (cachedReferenceTableNames != null && cachedReferenceTablePaths != null &&
                EditorApplication.timeSinceStartup < referenceTableNamesExpireTime)
            {
                return;
            }
    
            var tablePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var assetPaths = ConfigTableImporter.GetCtableAssetPaths();
            for (int i = 0; i < assetPaths.Count; i++)
            {
                string assetPath = assetPaths[i];
                string tableName = GetReferenceTableName(assetPath);
                if (string.IsNullOrEmpty(tableName))
                {
                    continue;
                }
    
                if (!tablePaths.ContainsKey(tableName) ||
                    string.Equals(Path.GetFileNameWithoutExtension(assetPath), tableName, StringComparison.OrdinalIgnoreCase))
                {
                    tablePaths[tableName] = assetPath;
                }
            }
    
            var tableNames = new List<string>(tablePaths.Keys);
            tableNames.Sort(StringComparer.OrdinalIgnoreCase);
            cachedReferenceTableNames = tableNames.ToArray();
            cachedReferenceTablePaths = tablePaths;
            referenceTableNamesExpireTime = EditorApplication.timeSinceStartup + ReferenceTableNameCacheSeconds;
        }
    
        /// <summary>
        ///   <para>获取引用表名称。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        private static string GetReferenceTableName(string assetPath)
        {
            if (ConfigTableImporter.TryGetSourceTableName(assetPath, out var sourceTableName))
            {
                return sourceTableName;
            }
    
            return Path.GetFileNameWithoutExtension(assetPath);
        }
    
        /// <summary>
        ///   <para>获取打开表图标。</para>
        /// </summary>
        private static GUIContent GetOpenTableIcon()
        {
            var icon = EditorGUIUtility.IconContent("d_FolderOpened Icon", "打开引用的配置表");
            return icon.image == null ? EditorGUIUtility.IconContent("FolderOpened Icon", "打开引用的配置表") : icon;
        }
    
        /// <summary>
        ///   <para>绘制可搜索弹窗。</para>
        /// </summary>
        /// <param name="cellRect">单元格区域。</param>
        /// <param name="value">值。</param>
        /// <param name="options">选项。</param>
        /// <param name="tooltip">提示。</param>
        private static bool DrawSearchablePopup(Rect cellRect, string value, string[] options, string tooltip = null)
        {
            var clicked = EditorGUI.DropdownButton(
                cellRect,
                new GUIContent(value, tooltip),
                FocusType.Keyboard,
                PopupStyle);
            return clicked && options != null && options.Length > 0;
        }
    
        /// <summary>
        ///   <para>显示可搜索弹窗。</para>
        /// </summary>
        /// <param name="cellRect">单元格区域。</param>
        /// <param name="currentValue">当前值。</param>
        /// <param name="options">选项。</param>
        /// <param name="onSelected">选中回调。</param>
        private static void ShowSearchablePopup(Rect cellRect, string currentValue, string[] options, Action<string> onSelected) => PopupWindow.Show(cellRect, new SearchableOptionsPopup(currentValue, options, onSelected));
        
        /// <summary>
        ///   <para>绘制记录。</para>
        /// </summary>
        /// <param name="table">表。</param>
        /// <param name="editState">编辑状态。</param>
        /// <param name="searchQuery">用于高亮的搜索查询。</param>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="scrollY">滚动纵向。</param>
        /// <param name="viewportHeight">可视区域高度。</param>
        /// <param name="lockStructure">是否锁定结构编辑。</param>
        private static void DrawRecords(ConfigTableAsset table, ConfigTableEditState editState, ConfigTableSearchQuery searchQuery,
            ConfigTableGridLayout gridLayout, float scrollY, float viewportHeight, bool lockStructure)
        {
            var rows = table.rows ?? Array.Empty<ConfigTableRecord>();
            if (rows.Length == 0)
            {
                return;
            }
    
            int firstRowIndex;
            int lastRowIndexExclusive;
            if (Event.current.type == EventType.Layout || gridLayout.visibleRowCount != rows.Length)
            {
                GetVisibleRowRange(gridLayout, rows.Length, scrollY, viewportHeight, out firstRowIndex, out lastRowIndexExclusive);
                gridLayout.visibleFirstRowIndex = firstRowIndex;
                gridLayout.visibleLastRowIndexExclusive = lastRowIndexExclusive;
                gridLayout.visibleRowCount = rows.Length;
            }
            else
            {
                firstRowIndex = gridLayout.visibleFirstRowIndex;
                lastRowIndexExclusive = gridLayout.visibleLastRowIndexExclusive;
            }
            if (firstRowIndex > 0)
            {
                GUILayout.Space(gridLayout.rowOffsets[firstRowIndex]);
            }
    
            for (int rowIndex = firstRowIndex; rowIndex < lastRowIndexExclusive; rowIndex++)
            {
                var row = rows[rowIndex];
                float rowHeight = gridLayout.rowHeights[rowIndex];
    
                BeginGridRow();
                GUI.enabled = !lockStructure;
                if (GUILayout.Button(new GUIContent("删除 " + (rowIndex + 1).ToString(), "删除行"), ButtonStyle, CellOptions(RowHeaderWidth, rowHeight)))
                {
                    editState.RemoveRow(rowIndex);
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = true;
    
                for (int columnIndex = 0; columnIndex < row.values.Length; columnIndex++)
                {
                    bool isValid = columnIndex == 0
                        ? IsValidId(row.values[columnIndex], gridLayout.duplicateIds)
                        : ConfigTableSchema.IsValidValue(table.types[columnIndex], row.values[columnIndex]);
                    Color backgroundColor = GUI.backgroundColor;
                    if (!isValid)
                    {
                        GUI.backgroundColor = InvalidValueColor;
                    }
    
                    bool isBoolean = IsBoolean(table.types[columnIndex]);
                    bool isMultiline = IsMultilineString(table.types[columnIndex]);
                    ConfigTableResourcePreviewMode resourceMode = isMultiline
                        ? GetResourcePreviewMode(gridLayout, columnIndex)
                        : ConfigTableResourcePreviewMode.None;
                    bool useObjectField = resourceMode != ConfigTableResourcePreviewMode.None;
                    GUIStyle cellStyle = useObjectField ? EditorStyles.objectField : isBoolean ? PopupStyle : isMultiline ? TextAreaStyle : CellStyle;
                    Rect cellRect = GUILayoutUtility.GetRect(GUIContent.none, cellStyle, CellOptions(gridLayout.columnWidths[columnIndex], rowHeight));
                    using (new EditorGUI.DisabledGroupScope(lockStructure && columnIndex == 0))
                    {
                        EditorGUI.BeginChangeCheck();
                        string value = row.values[columnIndex];
                        UnityEngine.Object selectedResource = null;
                        if (isBoolean)
                        {
                            value = DrawBooleanValue(value, cellRect);
                        }
                        else if (resourceMode == ConfigTableResourcePreviewMode.Resources)
                        {
                            UnityEngine.Object resource = string.IsNullOrEmpty(value) ? null : Resources.Load(value);
                            selectedResource = EditorGUI.ObjectField(cellRect, GUIContent.none, resource, typeof(UnityEngine.Object), false);
                        }
                        else if (isMultiline)
                        {
                            value = EditorGUI.TextArea(cellRect, value, TextAreaStyle);
                        }
                        else
                        {
                            value = EditorGUI.TextField(cellRect, value, CellStyle);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            bool resourcePathValid = !useObjectField;
                            if (resourceMode == ConfigTableResourcePreviewMode.Resources)
                            {
                                resourcePathValid = TryGetResourcesPath(selectedResource, out value, out var resourceError);
                                if (!resourcePathValid)
                                {
                                    editState.ReportError(resourceError);
                                }
                            }
    
                            if (resourcePathValid)
                            {
                                editState.SetValue(rowIndex, columnIndex, value);
                                RefreshRowMetric(gridLayout, table, rowIndex);
                                if (columnIndex == 0)
                                {
                                    gridLayout.duplicateIdVersion = -1;
                                }
                            }
                        }
                    }
                    GUI.backgroundColor = backgroundColor;
                    bool matchesContent = ConfigTableSearch.MatchesCell(row.values[columnIndex], ConfigTableSearchTarget.Content, searchQuery);
                    bool matchesColumnValue = ConfigTableSearch.MatchesColumnValue(table.fields[columnIndex], row.values[columnIndex], searchQuery);
                    DrawSearchMatchBorder(cellRect, matchesContent || matchesColumnValue);
                }
                DrawCommandPlaceholder(rowHeight);
                EndGridRow();
            }
    
            float remainingHeight = gridLayout.rowOffsets[rows.Length] - gridLayout.rowOffsets[lastRowIndexExclusive];
            if (remainingHeight > 0f)
            {
                GUILayout.Space(remainingHeight);
            }
        }
    
        /// <summary>
        ///   <para>更新行高与偏移缓存。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="table">表。</param>
        /// <param name="layoutVersion">布局版本。</param>
        private static void EnsureRowMetrics(ConfigTableGridLayout gridLayout, ConfigTableAsset table, int layoutVersion)
        {
            var rows = table.rows ?? Array.Empty<ConfigTableRecord>();
            var widthHash = GetColumnWidthHash(gridLayout.columnWidths);
            var widthsChanged = gridLayout.resizingColumn < 0 && gridLayout.rowHeightWidthHash != widthHash;
            if (gridLayout.rowHeights != null && gridLayout.rowHeights.Length == rows.Length &&
                gridLayout.rowOffsets != null && gridLayout.rowOffsets.Length == rows.Length + 1 &&
                gridLayout.rowHeightVersion == layoutVersion && !widthsChanged)
            {
                return;
            }
    
            gridLayout.rowHeights = new float[rows.Length];
            gridLayout.rowOffsets = new float[rows.Length + 1];
            for (int i = 0; i < rows.Length; i++)
            {
                gridLayout.rowHeights[i] = GetRecordHeight(table.types, rows[i], gridLayout.columnWidths);
                gridLayout.rowOffsets[i + 1] = gridLayout.rowOffsets[i] + gridLayout.rowHeights[i];
            }
            gridLayout.rowHeightVersion = layoutVersion;
            gridLayout.rowHeightWidthHash = widthHash;
        }
    
        /// <summary>
        ///   <para>更新指定行的高度与偏移。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="table">表。</param>
        /// <param name="rowIndex">记录索引。</param>
        private static void RefreshRowMetric(ConfigTableGridLayout gridLayout, ConfigTableAsset table, int rowIndex)
        {
            var rows = table.rows ?? Array.Empty<ConfigTableRecord>();
            if (gridLayout.rowHeights == null || gridLayout.rowOffsets == null || rowIndex < 0 || rowIndex >= rows.Length ||
                gridLayout.rowHeights.Length != rows.Length || gridLayout.rowOffsets.Length != rows.Length + 1)
            {
                return;
            }
    
            var previousHeight = gridLayout.rowHeights[rowIndex];
            var nextHeight = GetRecordHeight(table.types, rows[rowIndex], gridLayout.columnWidths);
            if (Mathf.Approximately(previousHeight, nextHeight))
            {
                return;
            }
    
            float delta = nextHeight - previousHeight;
            gridLayout.rowHeights[rowIndex] = nextHeight;
            for (int i = rowIndex + 1; i < gridLayout.rowOffsets.Length; i++)
            {
                gridLayout.rowOffsets[i] += delta;
            }
        }
    
        /// <summary>
        ///   <para>更新重复标识缓存。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="rows">行。</param>
        /// <param name="idVersion">标识版本。</param>
        private static void EnsureDuplicateIds(ConfigTableGridLayout gridLayout, ConfigTableRecord[] rows, int idVersion)
        {
            if (gridLayout.duplicateIds != null && gridLayout.duplicateIdVersion == idVersion)
            {
                return;
            }
    
            gridLayout.duplicateIds = GetDuplicateIds(rows);
            gridLayout.duplicateIdVersion = idVersion;
        }
    
        /// <summary>
        ///   <para>获取列宽哈希。</para>
        /// </summary>
        /// <param name="columnWidths">列宽列表。</param>
        private static int GetColumnWidthHash(float[] columnWidths)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < columnWidths.Length; i++)
                {
                    hash = hash * 31 + Mathf.RoundToInt(columnWidths[i] * 10f);
                }
                return hash;
            }
        }
    
        /// <summary>
        ///   <para>获取可见行范围。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="rowCount">行数。</param>
        /// <param name="scrollY">滚动纵向。</param>
        /// <param name="viewportHeight">可视区域高度。</param>
        /// <param name="firstRowIndex">起始行索引。</param>
        /// <param name="lastRowIndexExclusive">结束行索引；不包含该行。</param>
        private static void GetVisibleRowRange(ConfigTableGridLayout gridLayout, int rowCount, float scrollY, float viewportHeight,
            out int firstRowIndex, out int lastRowIndexExclusive)
        {
            float visibleHeight = viewportHeight > 0f ? viewportHeight : 600f;
            float startOffset = Mathf.Max(0f, scrollY - SchemaHeight - VirtualizationPadding);
            float endOffset = Mathf.Max(0f, scrollY + visibleHeight - SchemaHeight + VirtualizationPadding);
            firstRowIndex = FindRowIndex(gridLayout.rowOffsets, rowCount, startOffset);
            lastRowIndexExclusive = Mathf.Min(rowCount, FindRowIndex(gridLayout.rowOffsets, rowCount, endOffset) + 1);
        }
    
        /// <summary>
        ///   <para>查找行索引。</para>
        /// </summary>
        /// <param name="rowOffsets">行偏移。</param>
        /// <param name="rowCount">行数。</param>
        /// <param name="offset">偏移量。</param>
        private static int FindRowIndex(float[] rowOffsets, int rowCount, float offset)
        {
            if (rowCount <= 1 || offset <= 0f)
            {
                return 0;
            }
    
            if (offset >= rowOffsets[rowCount])
            {
                return rowCount - 1;
            }
    
            int low = 0;
            int high = rowCount;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (rowOffsets[middle + 1] <= offset)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }
            return low;
        }
    
        /// <summary>
        ///   <para>获取重复标识。</para>
        /// </summary>
        /// <param name="rows">行。</param>
        private static HashSet<string> GetDuplicateIds(ConfigTableRecord[] rows)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (rows != null)
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    string id = rows[i] == null || rows[i].values == null || rows[i].values.Length == 0 ? string.Empty : rows[i].values[0];
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
    
                    counts.TryGetValue(id, out var count);
                    counts[id] = count + 1;
                }
            }
    
            var duplicateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, int> item in counts)
            {
                if (item.Value > 1)
                {
                    duplicateIds.Add(item.Key);
                }
            }
            return duplicateIds;
        }
    
        /// <summary>
        ///   <para>判断是否为有效标识。</para>
        /// </summary>
        /// <param name="id">标识。</param>
        /// <param name="duplicateIds">重复标识。</param>
        private static bool IsValidId(string id, HashSet<string> duplicateIds) => !string.IsNullOrEmpty(id) && string.Equals(id, id.Trim(), StringComparison.Ordinal) && !duplicateIds.Contains(id);
    
        /// <summary>
        ///   <para>绘制搜索匹配边框。</para>
        /// </summary>
        /// <param name="cellRect">单元格区域。</param>
        /// <param name="isMatch">是否匹配。</param>
        private static void DrawSearchMatchBorder(Rect cellRect, bool isMatch)
        {
            if (!isMatch || Event.current.type != EventType.Repaint)
            {
                return;
            }
    
            const float borderWidth = 2f;
            EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, borderWidth), SearchMatchColor);
            EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.yMax - borderWidth, cellRect.width, borderWidth), SearchMatchColor);
            EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, borderWidth, cellRect.height), SearchMatchColor);
            EditorGUI.DrawRect(new Rect(cellRect.xMax - borderWidth, cellRect.y, borderWidth, cellRect.height), SearchMatchColor);
        }
    
        /// <summary>
        ///   <para>获取记录高度。</para>
        /// </summary>
        /// <param name="types">字段类型。</param>
        /// <param name="row">表记录。</param>
        /// <param name="columnWidths">列宽列表。</param>
        private static float GetRecordHeight(string[] types, ConfigTableRecord row, float[] columnWidths)
        {
            float height = RowHeight;
            if (row == null || row.values == null)
            {
                return height;
            }
    
            for (int i = 0; i < row.values.Length && i < types.Length && i < columnWidths.Length; i++)
            {
                string value = row.values[i];
                if (!IsMultilineString(types[i]) || string.IsNullOrEmpty(value) || !NeedsHeightCalculation(value, columnWidths[i]))
                {
                    continue;
                }
    
                MeasurementContent.text = value;
                float contentHeight = TextAreaStyle.CalcHeight(MeasurementContent, columnWidths[i]);
                height = Mathf.Max(height, Mathf.Ceil(contentHeight + 4f));
            }
            return height;
        }
    
        /// <summary>
        ///   <para>判断是否需要重新计算行高。</para>
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="columnWidth">列宽。</param>
        private static bool NeedsHeightCalculation(string value, float columnWidth) => value.IndexOfAny(LineBreakCharacters) >= 0 || value.Length > Mathf.Max(8, Mathf.FloorToInt(columnWidth / 10f));
    
        /// <summary>
        ///   <para>绘制布尔值控件。</para>
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="cellRect">单元格区域。</param>
        private static string DrawBooleanValue(string value, Rect cellRect)
        {
            int optionIndex = GetBooleanOption(value);
            string[] values = BooleanValues;
            string[] labels = BooleanOptionLabels;
            if (optionIndex < 0)
            {
                values = new[] { value, string.Empty, "false", "true" };
                labels = new[] { "（无效） " + value, "（空）", "否", "是" };
                optionIndex = 0;
            }
    
            int nextIndex = EditorGUI.Popup(cellRect, optionIndex, labels, PopupStyle);
            return values[nextIndex];
        }
    
        /// <summary>
        ///   <para>获取布尔值选项。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static int GetBooleanOption(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }
    
            if (!ConfigTableSchema.TryParseBool(value, out var result))
            {
                return -1;
            }
            return result ? 2 : 1;
        }
    
        /// <summary>
        ///   <para>判断是否为布尔值。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static bool IsBoolean(string type) => string.Equals(type, "bool", StringComparison.Ordinal);
    
        /// <summary>
        ///   <para>判断是否为多行字符串。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static bool IsMultilineString(string type) => string.Equals(type, "string", StringComparison.Ordinal);
    
        /// <summary>
        ///   <para>获取资源预览模式。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="columnIndex">字段索引。</param>
        private static ConfigTableResourcePreviewMode GetResourcePreviewMode(ConfigTableGridLayout gridLayout, int columnIndex)
        {
            if (gridLayout.resourceModes == null || columnIndex < 0 || columnIndex >= gridLayout.resourceModes.Length)
            {
                return ConfigTableResourcePreviewMode.None;
            }
    
            int mode = (int)gridLayout.resourceModes[columnIndex];
            return (ConfigTableResourcePreviewMode)Mathf.Clamp(mode, 0, ResourceModeOptions.Length - 1);
        }
    
        /// <summary>
        ///   <para>尝试获取资源路径。</para>
        /// </summary>
        /// <param name="resource">资源。</param>
        /// <param name="resourcePath">资源路径。</param>
        /// <param name="error">错误。</param>
        private static bool TryGetResourcesPath(UnityEngine.Object resource, out string resourcePath, out string error)
        {
            resourcePath = string.Empty;
            error = null;
            if (resource == null)
            {
                return true;
            }
    
            string assetPath = AssetDatabase.GetAssetPath(resource);
            const string resourcesDirectory = "/Resources/";
            int resourcesIndex = string.IsNullOrEmpty(assetPath) ? -1 : assetPath.LastIndexOf(resourcesDirectory, StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath) || resourcesIndex < 0)
            {
                error = "资源必须位于 Resources 文件夹中：" +
                    (string.IsNullOrEmpty(assetPath) ? resource.name : assetPath);
                return false;
            }
    
            resourcePath = Path.ChangeExtension(assetPath[(resourcesIndex + resourcesDirectory.Length)..], null)
                .Replace('\\', '/');
            if (string.IsNullOrEmpty(resourcePath) || Resources.Load(resourcePath) == null)
            {
                error = "资源无法通过 Resources.Load 加载：" + resourcePath + "\n" + assetPath;
                resourcePath = string.Empty;
                return false;
            }
            return true;
        }
    
        /// <summary>
        ///   <para>绘制添加行。</para>
        /// </summary>
        /// <param name="editState">编辑状态。</param>
        /// <param name="columnCount">列数。</param>
        /// <param name="columnWidths">列宽列表。</param>
        /// <param name="readOnly">只读。</param>
        private static void DrawAddRow(ConfigTableEditState editState, int columnCount, float[] columnWidths, bool readOnly)
        {
            BeginGridRow();
            GUI.enabled = !readOnly;
            if (GUILayout.Button(new GUIContent("+ 行", "添加行"), ButtonStyle, CellOptions(RowHeaderWidth)))
            {
                editState.AddRow();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
            for (int i = 0; i < columnCount; i++)
            {
                GUILayout.Box(GUIContent.none, EmptyCellStyle, CellOptions(columnWidths[i]));
            }
            DrawCommandPlaceholder();
            EndGridRow();
        }
    
        /// <summary>
        ///   <para>补齐缺失的列宽。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="columnCount">列数。</param>
        private static void EnsureColumnWidths(ConfigTableGridLayout gridLayout, int columnCount)
        {
            if (gridLayout.columnWidths != null && gridLayout.columnWidths.Length == columnCount)
            {
                if (gridLayout.resourceModes == null || gridLayout.resourceModes.Length != columnCount)
                {
                    Array.Resize(ref gridLayout.resourceModes, columnCount);
                }
                return;
            }
    
            var updatedWidths = new float[columnCount];
            var updatedResourceModes = new ConfigTableResourcePreviewMode[columnCount];
            for (int i = 0; i < updatedWidths.Length; i++)
            {
                updatedWidths[i] = gridLayout.columnWidths != null && i < gridLayout.columnWidths.Length
                    ? gridLayout.columnWidths[i]
                    : DefaultColumnWidth;
                updatedResourceModes[i] = gridLayout.resourceModes != null && i < gridLayout.resourceModes.Length
                    ? gridLayout.resourceModes[i]
                    : ConfigTableResourcePreviewMode.None;
            }
            gridLayout.columnWidths = updatedWidths;
            gridLayout.resourceModes = updatedResourceModes;
        }
    
        /// <summary>
        ///   <para>添加列宽。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        private static void AddColumnWidth(ConfigTableGridLayout gridLayout)
        {
            int count = gridLayout.columnWidths?.Length ?? 0;
            Array.Resize(ref gridLayout.columnWidths, count + 1);
            Array.Resize(ref gridLayout.resourceModes, count + 1);
            gridLayout.columnWidths[count] = DefaultColumnWidth;
        }
    
        /// <summary>
        ///   <para>移除列宽。</para>
        /// </summary>
        /// <param name="gridLayout">网格布局缓存。</param>
        /// <param name="index">索引。</param>
        private static void RemoveColumnWidth(ConfigTableGridLayout gridLayout, int index)
        {
            gridLayout.columnWidths = RemoveAt(gridLayout.columnWidths, index);
            gridLayout.resourceModes = RemoveAt(gridLayout.resourceModes, index);
        }
    
        /// <summary>
        ///   <para>移除指定位置的元素。</para>
        /// </summary>
        /// <param name="values">值。</param>
        /// <param name="index">索引。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        private static T[] RemoveAt<T>(T[] values, int index)
        {
            var updatedValues = new T[values.Length - 1];
            if (index > 0)
            {
                Array.Copy(values, 0, updatedValues, 0, index);
            }
            if (index < values.Length - 1)
            {
                Array.Copy(values, index + 1, updatedValues, index, values.Length - index - 1);
            }
            return updatedValues;
        }
    
        /// <summary>
        ///   <para>绘制命令占位符。</para>
        /// </summary>
        /// <param name="rowHeight">行高。</param>
        private static void DrawCommandPlaceholder(float rowHeight = RowHeight) => GUILayout.Box(GUIContent.none, EmptyCellStyle, CellOptions(CommandColumnWidth, rowHeight));
        
        /// <summary>
        ///   <para>开始网格行。</para>
        /// </summary>
        private static void BeginGridRow() => GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
    
        /// <summary>
        ///   <para>结束网格行。</para>
        /// </summary>
        private static void EndGridRow() => GUILayout.EndHorizontal();
    
        /// <summary>
        ///   <para>单元格选项。</para>
        /// </summary>
        /// <param name="width">宽度。</param>
        private static GUILayoutOption[] CellOptions(float width) => CellOptions(width, RowHeight);
    
        /// <summary>
        ///   <para>单元格选项。</para>
        /// </summary>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        private static GUILayoutOption[] CellOptions(float width, float height) => new[] { GUILayout.Width(width), GUILayout.Height(height) };
    
        /// <summary>
        ///   <para>创建文本区域样式。</para>
        /// </summary>
        private static GUIStyle CreateTextAreaStyle()
        {
            GUIStyle style = CreateStyle(EditorStyles.textArea, TextAnchor.UpperLeft);
            style.wordWrap = true;
            return style;
        }
    
        /// <summary>
        ///   <para>创建样式。</para>
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="alignment">对齐。</param>
        private static GUIStyle CreateStyle(GUIStyle source, TextAnchor alignment)
        {
            var style = new GUIStyle(source)
            {
                alignment = alignment,
                margin = new RectOffset(0, 0, 0, 0)
            };
            return style;
        }
    }
}

#endif