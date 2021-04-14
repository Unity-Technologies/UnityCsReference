// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    sealed class EditorToolManager : ScriptableSingleton<EditorToolManager>
    {
        [SerializeField]
        List<ScriptableObject> m_SingletonObjects = new List<ScriptableObject>();

        [SerializeField]
        EditorTool m_ActiveTool;

        [SerializeField]
        List<EditorTool> m_ToolHistory = new List<EditorTool>();

        static bool s_ChangingActiveTool, s_ChangingActiveContext;

        // Mimic behavior of Tools.toolChanged for backwards compatibility until existing tools are converted to the new
        // apis.
        internal static event Action<EditorTool, EditorTool> activeToolChanged;

        [SerializeField]
        List<ComponentEditor> m_ComponentTools = new List<ComponentEditor>();

        [SerializeField]
        List<ComponentEditor> m_ComponentContexts = new List<ComponentEditor>();

        [SerializeField]
        EditorToolContext m_ActiveToolContext;

        internal static EditorToolContext activeToolContext
        {
            get
            {
                if (instance.m_ActiveToolContext == null)
                    instance.m_ActiveToolContext = GetSingleton<GameObjectToolContext>();
                return instance.m_ActiveToolContext;
            }

            set
            {
                if (s_ChangingActiveContext)
                {
                    // pop the changing state so that we don't lock the active tool after an exception is thrown.
                    s_ChangingActiveContext = false;
                    throw new InvalidOperationException("Setting the active context from EditorToolContext.OnActivated or EditorToolContext.OnWillBeDeactivated is not allowed.");
                }

                var ctx = value == null ? GetSingleton<GameObjectToolContext>() : value;

                if (ctx == activeToolContext)
                    return;

                s_ChangingActiveContext = true;

                // Make sure to get the active tool enum prior to setting the context, otherwise we'll be comparing
                // apples to oranges. Ie, the transform tools will be different despite being the same `Tool` enum value.
                var tool = Tools.current;
                var prev = instance.m_ActiveToolContext;

                if (prev != null)
                    prev.OnWillBeDeactivated();

                ToolManager.ActiveContextWillChange();
                instance.m_ActiveToolContext = ctx;
                ToolManager.ActiveContextDidChange();

                ctx.OnActivated();

                instance.RebuildAvailableTools();

                // Remap the history for manipulation tools to use their correctly resolved EditorTool instances
                RebuildToolHistoryWithContext(prev, ctx);

                var resolved = EditorToolUtility.GetEditorToolWithEnum(tool);

                // Always try to resolve to a valid tool when switching contexts, even if it means changing the active tool type
                for (int i = (int)Tool.Move; (resolved == null || resolved is NoneTool) && i < (int)Tool.Custom; i++)
                    resolved = EditorToolUtility.GetEditorToolWithEnum((Tool)i);

                // If resolved is null at this point, the setter for activeTool will substitute an instance of NoneTool for us.
                ToolManager.SetActiveTool(resolved);

                s_ChangingActiveContext = false;
            }
        }

        internal static EditorTool activeTool
        {
            get { return instance.m_ActiveTool; }

            set
            {
                if (s_ChangingActiveTool)
                {
                    // pop the changing state so that we don't lock the active tool after an exception is thrown.
                    s_ChangingActiveTool = false;
                    throw new InvalidOperationException("Attempting to set the active tool from EditorTool.OnActivate or EditorTool.OnDeactivate. This is not allowed.");
                }

                var tool = value;

                if (tool == null)
                    tool = GetSingleton<NoneTool>();

                if (tool == instance.m_ActiveTool)
                    return;

                s_ChangingActiveTool = true;

                int index = instance.m_ToolHistory.IndexOf(tool);

                if (index > -1)
                    instance.m_ToolHistory.RemoveAt(index);

                // Never add `None` tool to history
                if (!(tool is NoneTool))
                    instance.m_ToolHistory.Add(tool);

                ToolManager.ActiveToolWillChange();

                var previous = instance.m_ActiveTool;

                if (previous != null)
                    previous.OnWillBeDeactivated();

                instance.m_ActiveTool = tool;

                instance.m_ActiveTool.OnActivated();

                ToolManager.ActiveToolDidChange();

                if (activeToolChanged != null)
                    activeToolChanged(previous, instance.m_ActiveTool);

                Tools.SyncToolEnum();

                Tools.InvalidateHandlePosition();

                s_ChangingActiveTool = false;
            }
        }

        [Serializable]
        struct ComponentToolCache : ISerializationCallbackReceiver
        {
            [SerializeField]
            string m_ToolType;
            [SerializeField]
            string m_ContextType;

            public Type contextType;
            public Type toolType;
            public UnityObject targetObject;
            public UnityObject[] targetObjects;

            public static readonly ComponentToolCache Empty = new ComponentToolCache(null, null);

            public ComponentToolCache(EditorToolContext context, EditorTool tool)
            {
                bool customTool = IsCustomEditorTool(tool);
                bool customContext = IsCustomToolContext(context);

                if (customTool || customContext)
                {
                    toolType = customTool ? tool.GetType() : null;
                    contextType = customContext ? context.GetType() : null;
                    targetObject = tool.target;
                    targetObjects = tool.targets.ToArray();
                }
                else
                {
                    toolType = null;
                    contextType = null;
                    targetObject = null;
                    targetObjects = null;
                }

                m_ToolType = null;
                m_ContextType = null;
            }

            public bool IsEqual(ComponentEditor other)
            {
                var editor = other?.GetEditor<EditorTool>();

                if (editor == null || targetObjects == null || editor.targets == null)
                    return false;

                // todo need to cache ComponentEditor targets
                return toolType == editor.GetType() && targetObjects.SequenceEqual(editor.targets);
            }

            public override string ToString()
            {
                return $"Tool: {toolType} Context: {contextType}";
            }

            public void OnBeforeSerialize()
            {
                m_ToolType = toolType != null ? toolType.AssemblyQualifiedName : null;
                m_ContextType = contextType != null ? contextType.AssemblyQualifiedName : null;
            }

            public void OnAfterDeserialize()
            {
                if (!string.IsNullOrEmpty(m_ToolType))
                    toolType = Type.GetType(m_ToolType);
                if (!string.IsNullOrEmpty(m_ContextType))
                    contextType = Type.GetType(m_ContextType);
            }
        }

        [SerializeField]
        ComponentToolCache m_PreviousComponentToolCache;

        void SaveComponentTool()
        {
            m_PreviousComponentToolCache = new ComponentToolCache(m_ActiveToolContext, m_ActiveTool);
        }

        EditorToolManager() {}

        void OnEnable()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
            ActiveEditorTracker.editorTrackerRebuilt += TrackerRebuilt;
            Selection.selectedObjectWasDestroyed += SelectedObjectWasDestroyed;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;

            if (activeTool != null)
                EditorApplication.delayCall += activeTool.OnActivated;
            if (activeToolContext != null)
                EditorApplication.delayCall += activeToolContext.OnActivated;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            ActiveEditorTracker.editorTrackerRebuilt -= TrackerRebuilt;
            Selection.selectedObjectWasDestroyed -= SelectedObjectWasDestroyed;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
        }

        void BeforeAssemblyReload()
        {
            if (m_ActiveTool != null)
                m_ActiveTool.OnWillBeDeactivated();

            if (m_ActiveToolContext != null)
                m_ActiveToolContext.OnWillBeDeactivated();
        }

        // used by tests
        internal static void ForceTrackerRebuild()
        {
            instance.TrackerRebuilt();
        }

        void TrackerRebuilt()
        {
            // when entering play mode there is an intermediate tracker rebuild where nothing is selected. ignore it.
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                return;

            RebuildAvailableContexts();
            RebuildAvailableTools();
            EnsureCurrentToolIsNotNull();
        }

        void EnsureCurrentToolIsNotNull()
        {
            if (m_ActiveTool == null)
            {
                instance.CleanupToolHistory();
                var previous = GetLastTool();
                activeTool = previous != null ? previous : GetSingleton<MoveTool>();
            }
        }

        void SelectedObjectWasDestroyed(int id)
        {
            bool componentToolActive = m_ComponentTools.Any(
                x => x?.GetEditor<EditorTool>() == m_ActiveTool)
                && m_ActiveTool.m_Targets.Any(x => x == null || x.GetInstanceID() == id);

            bool componentContextActive = m_ComponentContexts.Any(
                x => x?.GetEditor<EditorToolContext>() == m_ActiveToolContext)
                && m_ActiveToolContext.targets.Any(x => x == null || x.GetInstanceID() == id);

            if (componentToolActive || componentContextActive)
            {
                SaveComponentTool();
                RestorePreviousTool();
            }
        }

        void UndoRedoPerformed()
        {
            RestoreCustomEditorTool();
        }

        void RestoreCustomEditorTool()
        {
            var restored = m_ComponentTools.FirstOrDefault(m_PreviousComponentToolCache.IsEqual);

            if (restored != null)
            {
                // todo Use generated Context
                if (m_PreviousComponentToolCache.contextType != null)
                    activeToolContext = GetComponentContext(m_PreviousComponentToolCache.contextType);

                activeTool = restored.GetEditor<EditorTool>();
            }

            m_PreviousComponentToolCache = ComponentToolCache.Empty;
        }

        // destroy invalid custom editor tools
        void ClearCustomEditorTools()
        {
            foreach (var customEditorTool in m_ComponentTools)
            {
                if (customEditorTool.editor == m_ActiveTool)
                    m_ActiveTool.OnWillBeDeactivated();
                DestroyImmediate(customEditorTool.editor);
            }

            m_ComponentTools.Clear();
        }

        void ClearComponentContexts()
        {
            foreach (var context in m_ComponentContexts)
            {
                if (context.GetEditor<EditorToolContext>() == m_ActiveToolContext)
                    m_ActiveToolContext.OnWillBeDeactivated();
                DestroyImmediate(context.editor);
            }

            m_ComponentContexts.Clear();
        }

        void CleanupSingletons()
        {
            for (int i = m_SingletonObjects.Count - 1; i > -1; i--)
            {
                if (m_SingletonObjects[i] == null)
                    m_SingletonObjects.RemoveAt(i);
            }
        }

        void CleanupToolHistory()
        {
            for (int i = m_ToolHistory.Count - 1; i > -1; i--)
            {
                if (m_ToolHistory[i] == null)
                    m_ToolHistory.RemoveAt(i);
            }
        }

        internal static T GetSingleton<T>() where T : ScriptableObject
        {
            return (T)GetSingleton(typeof(T));
        }

        internal static ScriptableObject GetSingleton(Type type)
        {
            instance.CleanupSingletons();
            if (type == null)
                return null;
            var res = instance.m_SingletonObjects.FirstOrDefault(x => x.GetType() == type);
            if (res != null)
                return res;
            res = CreateInstance(type);
            res.hideFlags = HideFlags.DontSave;
            instance.m_SingletonObjects.Add(res);
            return res;
        }

        public static EditorTool GetActiveTool()
        {
            instance.EnsureCurrentToolIsNotNull();
            return instance.m_ActiveTool;
        }

        internal static void GetToolHistory(List<EditorTool> tools, bool customToolsOnly = false)
        {
            tools.Clear();
            instance.CleanupToolHistory();

            for (int i = instance.m_ToolHistory.Count - 1; i > -1; i--)
            {
                if (!customToolsOnly || EditorToolUtility.GetEnumWithEditorTool(instance.m_ToolHistory[i]) == Tool.Custom)
                    tools.Add(instance.m_ToolHistory[i]);
            }
        }

        static void RebuildToolHistoryWithContext(EditorToolContext src, EditorToolContext dst)
        {
            var history = instance.m_ToolHistory;

            for (int i = history.Count - 1; i > 0; i--)
            {
                var tool = EditorToolUtility.GetEnumWithEditorTool(history[i], src);

                if (EditorToolUtility.IsManipulationTool(tool))
                    history[i] = EditorToolUtility.GetEditorToolWithEnum(tool, dst);

                if (history[i] is NoneTool)
                    DestroyImmediate(history[i]);

                if (history[i] == null)
                    history.RemoveAt(i);
            }
        }

        internal static EditorTool GetLastTool(Func<EditorTool, bool> predicate = null)
        {
            instance.CleanupToolHistory();

            return predicate == null
                ? instance.m_ToolHistory.LastOrDefault()
                : instance.m_ToolHistory.LastOrDefault(predicate);
        }

        internal static EditorTool GetLastCustomTool()
        {
            for (int i = instance.m_ToolHistory.Count - 1; i > -1; i--)
                if (EditorToolUtility.GetEnumWithEditorTool(instance.m_ToolHistory[i]) == Tool.Custom)
                    return instance.m_ToolHistory[i];
            return null;
        }

        public static void RestorePreviousPersistentTool()
        {
            var last = GetLastTool(x => x && !EditorToolUtility.IsCustomEditorTool(x.GetType()));

            if (last != null)
                activeTool = last;
            else
                activeTool = GetSingleton<MoveTool>();
        }

        public static void RestorePreviousTool()
        {
            activeTool = GetLastTool(x => x && x != instance.m_ActiveTool);
        }

        internal static void OnToolGUI(EditorWindow window)
        {
            activeToolContext.OnToolGUI(window);

            if (Tools.s_Hidden || instance.m_ActiveTool == null)
                return;

            var current = instance.m_ActiveTool;

            using (new EditorGUI.DisabledScope(!current.IsAvailable()))
            {
                current.OnToolGUI(window);
            }
        }

        static bool IsCustomEditorTool(EditorTool tool)
        {
            return EditorToolUtility.IsCustomEditorTool(tool != null ? tool.GetType() : null);
        }

        static bool IsCustomToolContext(EditorToolContext context)
        {
            return context != null && context.GetType() != typeof(GameObjectToolContext);
        }

        void RebuildAvailableContexts()
        {
            var activeContextType = activeToolContext.GetType();
            ClearComponentContexts();
            EditorToolUtility.InstantiateComponentContexts(m_ComponentContexts);
            var restoredContext = m_ComponentContexts.Find(x => x.editorType == activeContextType);
            if (restoredContext != null)
                activeToolContext = restoredContext.GetEditor<EditorToolContext>();
        }

        void RebuildAvailableTools()
        {
            ComponentToolCache activeComponentTool = new ComponentToolCache(m_ActiveToolContext, activeTool);
            ClearCustomEditorTools();

            EditorToolUtility.InstantiateComponentTools(activeToolContext, m_ComponentTools);

            if (activeComponentTool.toolType != null)
            {
                var restoredTool = m_ComponentTools.Find(x => x.editorType == activeComponentTool.toolType);

                if (restoredTool != null)
                {
                    activeTool = restoredTool.GetEditor<EditorTool>();
                }
                else
                {
                    m_PreviousComponentToolCache = activeComponentTool;
                    RestorePreviousPersistentTool();
                }
            }
        }

        // Used by tests
        public static T GetComponentContext<T>(bool searchLockedInspectors = false) where T : EditorToolContext
        {
            return GetComponentContext(typeof(T), searchLockedInspectors) as T;
        }

        // Used by tests
        public static EditorToolContext GetComponentContext(Type type, bool searchLockedInspectors = false)
        {
            return GetComponentContext(x => x.editorType == type && (searchLockedInspectors || !x.lockedInspector));
        }

        // Used by tests
        internal static EditorToolContext GetComponentContext(Func<ComponentEditor, bool> predicate)
        {
            foreach (var ctx in instance.m_ComponentContexts)
            {
                if (predicate(ctx))
                    return ctx.GetEditor<EditorToolContext>();
            }

            return null;
        }

        // Used by tests
        public static void GetComponentContexts(Func<ComponentEditor, bool> predicate, List<EditorToolContext> list)
        {
            list.Clear();

            foreach (var ctx in instance.m_ComponentContexts)
            {
                if (predicate(ctx))
                    list.Add(ctx.GetEditor<EditorToolContext>());
            }
        }

        internal static int GetCustomEditorToolsCount(bool includeLockedInspectorTools)
        {
            if (includeLockedInspectorTools)
                return instance.m_ComponentTools.Count;
            return instance.m_ComponentTools.Count(x => !x.lockedInspector);
        }

        // Used by tests.
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static T GetComponentTool<T>(bool searchLockedInspectors = false)
            where T : EditorTool
        {
            return GetComponentTool(typeof(T), searchLockedInspectors) as T;
        }

        internal static EditorTool GetComponentTool(Type type, bool searchLockedInspectors = false)
        {
            return GetComponentTool(x => x.editorType == type, searchLockedInspectors);
        }

        // Get the first component tool matching a predicate.
        internal static EditorTool GetComponentTool(Func<ComponentEditor, bool> predicate, bool searchLockedInspectors)
        {
            foreach (var customEditorTool in instance.m_ComponentTools)
            {
                if (!searchLockedInspectors && customEditorTool.lockedInspector)
                    continue;

                if (predicate(customEditorTool))
                    return customEditorTool.GetEditor<EditorTool>();
            }

            return null;
        }

        // Collect all instantiated EditorTools for the current selection, not including locked inspectors. This is
        // what should be used to get component tools in 99% of cases. The exception is locked Inspectors, in which
        // case you can use `GetComponentTools(x => x.inspector == editor)`.
        public static void GetComponentToolsForSharedTracker(List<EditorTool> list)
        {
            GetComponentTools(x => x.typeAssociation.targetContext == null, list, false);
        }

        // Used by tests.
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static void GetComponentTools(List<EditorTool> list, bool searchLockedInspectors)
        {
            GetComponentTools(x => true, list, searchLockedInspectors);
        }

        internal static void GetComponentTools(Func<ComponentEditor, bool> predicate,
            List<EditorTool> list,
            bool searchLockedInspectors = false)
        {
            list.Clear();

            foreach (var customEditorTool in instance.m_ComponentTools)
            {
                if (!searchLockedInspectors && customEditorTool.lockedInspector)
                    continue;

                if (predicate(customEditorTool))
                    list.Add(customEditorTool.GetEditor<EditorTool>());
            }
        }

        internal static void InvokeOnSceneGUICustomEditorTools()
        {
            foreach (var context in instance.m_ComponentContexts)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (context.editor is IDrawSelectedHandles handle)
                    handle.OnDrawHandles();
            }

            foreach (var tool in instance.m_ComponentTools)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (tool.editor is IDrawSelectedHandles handle)
                    handle.OnDrawHandles();
            }
        }
    }
}
