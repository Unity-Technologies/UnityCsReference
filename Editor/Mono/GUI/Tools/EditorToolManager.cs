// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    sealed class EditorToolManager : ScriptableSingleton<EditorToolManager>
    {
        [SerializeField]
        List<ScriptableObject> m_SingletonObjects = new List<ScriptableObject>();

        [SerializeField]
        EditorTool m_ActiveTool;

        static ActiveEditorTracker m_Tracker;

        [SerializeField]
        List<EditorTool> m_ToolHistory = new List<EditorTool>();

        static bool s_ChangingActiveTool;

        // Mimic behavior of Tools.toolChanged for backwards compatibility until existing tools are converted to the new
        // apis.
        internal static event Action<EditorTool, EditorTool> activeToolChanged;

        // EditorTools that are created as custom editor tools. This list represents only the shared tracker.
        List<EditorTool> m_CustomEditorTools = new List<EditorTool>();

        // This list represents any custom editor tools created by locked inspectors. They are not shown in the scene
        // or context menu.
        List<EditorTool> m_LockedCustomEditorTools = new List<EditorTool>();

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
                var ctx = value == null ? GetSingleton<GameObjectToolContext>() : value;

                if (ctx == activeToolContext)
                    return;

                // Make sure to get the active tool enum prior to setting the context, otherwise we'll be comparing
                // apples to oranges
                var tool = Tools.current;
                var prev = instance.m_ActiveToolContext;

                // Remap the history for manipulation tools to use their correctly resolved EditorTool instances
                RebuildToolHistoryWithContext(prev, ctx);

                ToolManager.ActiveContextWillChange();
                instance.m_ActiveToolContext = ctx;
                ToolManager.ActiveContextDidChange();

                DestroyImmediate(prev);

                var resolved = EditorToolUtility.GetEditorToolWithEnum(tool);

                // Always try to resolve to a valid tool when switching contexts, even if it means changing the active tool type
                for (int i = (int)Tool.Move; (resolved == null || resolved is NoneTool) && i < (int)Tool.Custom; i++)
                    resolved = EditorToolUtility.GetEditorToolWithEnum((Tool)i);

                // If resolved is null at this point, the setter for activeTool will substitute an instance of NoneTool for us.
                ToolManager.SetActiveTool(resolved);
            }
        }

        internal static Type activeToolContextType
        {
            get { return activeToolContext.GetType(); }
            set
            {
                var mode = value != null ? value : typeof(GameObjectToolContext);
                if (!typeof(EditorToolContext).IsAssignableFrom(mode))
                    throw new ArgumentException("ActiveToolMode property must derive from EditorToolMode", "value");
                activeToolContext = GetSingleton(mode) as EditorToolContext;
            }
        }

        internal static EditorTool activeTool
        {
            get
            {
                return instance.m_ActiveTool;
            }

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

        static ActiveEditorTracker tracker
        {
            get
            {
                if (m_Tracker == null)
                    m_Tracker = new ActiveEditorTracker();
                return m_Tracker;
            }
        }

        [Serializable]
        struct ComponentToolCache : ISerializationCallbackReceiver
        {
            [SerializeField]
            string m_EditorToolType;

            [SerializeField]
            string m_EditorToolState;

            public Type editorToolType;

            public UnityObject targetObject;

            public UnityObject[] targetObjects;

            public string editorToolState
            {
                get { return m_EditorToolState; }
            }

            public static readonly ComponentToolCache Empty = new ComponentToolCache(null);

            public ComponentToolCache(EditorTool tool)
            {
                if (tool != null)
                {
                    editorToolType = tool.GetType();
                    targetObject = tool.target;
                    targetObjects = tool.targets.ToArray();
                    m_EditorToolState = EditorJsonUtility.ToJson(tool);
                }
                else
                {
                    editorToolType = null;
                    targetObject = null;
                    targetObjects = null;
                    m_EditorToolState = null;
                }

                m_EditorToolType = null;
            }

            public bool IsEqual(EditorTool other)
            {
                if (other == null || editorToolType != other.GetType())
                    return false;

                if (ReferenceEquals(targetObjects, other.m_Targets))
                    return true;

                if (targetObjects == null || other.m_Targets == null)
                    return false;

                return targetObjects.SequenceEqual(other.m_Targets);
            }

            public override string ToString()
            {
                return editorToolType != null ? editorToolType.ToString() : "null";
            }

            public void OnBeforeSerialize()
            {
                m_EditorToolType = editorToolType != null ? editorToolType.AssemblyQualifiedName : null;
            }

            public void OnAfterDeserialize()
            {
                if (!string.IsNullOrEmpty(m_EditorToolType))
                    editorToolType = Type.GetType(m_EditorToolType);
            }
        }

        [SerializeField]
        ComponentToolCache m_PreviousComponentToolCache;

        // EditorApplication.isPlayingOrWillEnterPlayMode doesn't handle exiting.
        [SerializeField]
        PlayModeStateChange m_PlayModeState;

        EditorToolManager() {}

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            Undo.undoRedoPerformed += UndoRedoPerformed;
            ActiveEditorTracker.editorTrackerRebuilt += TrackerRebuilt;
            Selection.selectedObjectWasDestroyed += SelectedObjectWasDestroyed;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
            // Delay calls for functions that require all objects have run through "OnEnable"
            EditorApplication.delayCall += RebuildAvailableCustomEditorTools;
            if (activeTool != null)
                EditorApplication.delayCall += activeTool.OnActivated;
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            ActiveEditorTracker.editorTrackerRebuilt -= TrackerRebuilt;
            Selection.selectedObjectWasDestroyed -= SelectedObjectWasDestroyed;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            ClearCustomEditorTools();
        }

        void PlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    m_PlayModeState = PlayModeStateChange.EnteredEditMode;
                    break;

                case PlayModeStateChange.ExitingEditMode:
                    m_PlayModeState = PlayModeStateChange.ExitingEditMode;
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    m_PlayModeState = PlayModeStateChange.EnteredPlayMode;
                    break;

                case PlayModeStateChange.ExitingPlayMode:

                    // ExitPlayMode tests invoke this callback twice
                    if (EditorApplication.isPlaying)
                        m_PlayModeState = PlayModeStateChange.ExitingPlayMode;
                    break;
            }

            // TrackerRebuilt is called during the ExitingEditMode phase, but the selection might not be valid yet.
            if (m_PlayModeState == PlayModeStateChange.EnteredPlayMode || m_PlayModeState == PlayModeStateChange.EnteredEditMode)
                RebuildAvailableCustomEditorTools();
        }

        void BeforeAssemblyReload()
        {
            if (m_ActiveTool != null)
                m_ActiveTool.OnWillBeDeactivated();
        }

        void TrackerRebuilt()
        {
            RebuildAvailableCustomEditorTools();
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
            if (m_CustomEditorTools.Any(x => x == m_ActiveTool) &&
                m_ActiveTool.m_Targets.Any(x => x == null || x.GetInstanceID() == id))
            {
                m_PreviousComponentToolCache = new ComponentToolCache(m_ActiveTool);
                RestorePreviousTool();
            }
        }

        void UndoRedoPerformed()
        {
            RestoreCustomEditorTool();
        }

        void RestoreCustomEditorTool()
        {
            var restored = m_CustomEditorTools.FirstOrDefault(m_PreviousComponentToolCache.IsEqual);

            // Check for existence in locked inspectors too, but only if the locked inspector target is being inspected
            if (restored == null
                && m_PreviousComponentToolCache.targetObject != null
                && Selection.objects.Any(x => x.Equals(m_PreviousComponentToolCache.targetObject)))
                restored = m_LockedCustomEditorTools.FirstOrDefault(m_PreviousComponentToolCache.IsEqual);

            if (restored != null)
            {
                var targets = restored.targets.ToArray();
                EditorJsonUtility.FromJsonOverwrite(m_PreviousComponentToolCache.editorToolState, restored);
                restored.m_Targets = targets;
                restored.m_Target = targets.Last();
                activeTool = restored;
            }

            m_PreviousComponentToolCache = ComponentToolCache.Empty;
        }

        // destroy invalid custom editor tools
        void ClearCustomEditorTools()
        {
            foreach (var customEditorTool in m_CustomEditorTools)
            {
                if (customEditorTool != null && customEditorTool != m_ActiveTool)
                    DestroyImmediate(customEditorTool);
            }

            foreach (var customEditorTool in m_LockedCustomEditorTools)
            {
                if (customEditorTool != null && customEditorTool != m_ActiveTool)
                    DestroyImmediate(customEditorTool);
            }

            m_LockedCustomEditorTools.Clear();
            m_CustomEditorTools.Clear();
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

        static List<CustomEditorTool> s_CustomEditorTools = new List<CustomEditorTool>();

        void RebuildAvailableCustomEditorTools()
        {
            EditorApplication.delayCall -= RebuildAvailableCustomEditorTools;

            // Do not rebuild the cache since objects are serialized, destroyed, deserialized during this phase
            if (m_PlayModeState == PlayModeStateChange.ExitingEditMode ||
                m_PlayModeState == PlayModeStateChange.ExitingPlayMode)
                return;

            var preservedActiveTool = false;

            ClearCustomEditorTools();

            var inspectors = InspectorWindow.GetInspectors();

            // If the shared tracker is locked, use our own tracker instance so that the current selection is always
            // represented. Addresses case where a single locked inspector is open.
            var shared = ActiveEditorTracker.sharedTracker;

            m_CustomEditorTools.Clear();
            m_LockedCustomEditorTools.Clear();

            // Collect editor tools for the shared tracker first
            EditorToolUtility.GetEditorToolsForTracker(shared.isLocked ? tracker : shared, s_CustomEditorTools);

            foreach (var customEditorTool in s_CustomEditorTools)
            {
                if (m_CustomEditorTools.Any(x => x.GetType() == customEditorTool.editorToolType && x.target == customEditorTool.owner.target))
                    continue;
                EditorTool tool;
                preservedActiveTool |= CreateOrRestoreTool(customEditorTool, out tool);
                m_CustomEditorTools.Add(tool);
            }

            // Next, collect tools from locked inspectors
            foreach (var inspector in inspectors)
            {
                if (inspector.isLocked)
                {
                    EditorToolUtility.GetEditorToolsForTracker(inspector.tracker, s_CustomEditorTools);

                    foreach (var customEditorTool in s_CustomEditorTools)
                    {
                        // Don't add duplicate tools to either another locked inspector with the same target, or a shared tracker
                        if (m_CustomEditorTools.Any(x => x.GetType() == customEditorTool.editorToolType && x.target == customEditorTool.owner.target)
                            || m_LockedCustomEditorTools.Any(x => x.GetType() == customEditorTool.editorToolType && x.target == customEditorTool.owner.target))
                            continue;
                        EditorTool tool;
                        preservedActiveTool |= CreateOrRestoreTool(customEditorTool, out tool);
                        m_LockedCustomEditorTools.Add(tool);
                    }
                }
            }

            if (IsCustomEditorTool(m_ActiveTool) && !preservedActiveTool)
            {
                var previous = m_ActiveTool;
                m_PreviousComponentToolCache = new ComponentToolCache(m_ActiveTool);
                RestorePreviousTool();
                DestroyImmediate(previous);
            }
        }

        bool CreateOrRestoreTool(CustomEditorTool customEditorTool, out EditorTool customEditorToolInstance)
        {
            var toolType = customEditorTool.editorToolType;
            var toolOwner = customEditorTool.owner;
            var targets = customEditorTool.targets;
            var target = customEditorTool.owner.target;
            var activeIsCustomEditorTool = IsCustomEditorTool(m_ActiveTool);
            bool preservedActiveTool = false;

            // The only case where a custom editor tool is serialized is when it is the active tool. All other
            // instances are discarded and rebuilt on any tracker rebuild.
            if (activeIsCustomEditorTool && CustomEditorToolIsMatch(toolOwner, toolType, m_ActiveTool))
            {
                preservedActiveTool = true;

                m_ActiveTool.m_Targets = targets;
                m_ActiveTool.m_Target = target;

                // domain reload - the owning editor was destroyed and therefore we need to reset the EditMode active
                if (m_ActiveTool is EditModeTool && toolOwner.GetInstanceID() != UnityEditorInternal.EditMode.ownerID)
                    UnityEditorInternal.EditMode.EditModeToolStateChanged(toolOwner, ((EditModeTool)m_ActiveTool).editMode);

                customEditorToolInstance = m_ActiveTool;
            }
            else
            {
                customEditorToolInstance = (EditorTool)CreateInstance(toolType, x =>
                {
                    ((EditorTool)x).m_Targets = targets;
                    ((EditorTool)x).m_Target = target;
                });

                customEditorToolInstance.hideFlags = HideFlags.DontSave;
            }

            var editModeTool = customEditorToolInstance as EditModeTool;

            if (editModeTool != null)
                editModeTool.owner = toolOwner;

            return preservedActiveTool;
        }

        static bool CustomEditorToolIsMatch(Editor editor, Type toolType, EditorTool tool)
        {
            if (editor == null || toolType != tool.GetType())
                return false;

            // if it's an EditModeTool we need to be stricter about ownership for backwards compatibility.
            var editModeTool = tool as EditModeTool;

            if (editModeTool != null)
                return editModeTool.owner == (IToolModeOwner)editor || editModeTool.target == editor.target;

            // otherwise just check if it's a valid type
            return true;
        }

        internal static EditorTool GetCustomEditorToolOfType(Type type, bool searchLockedInspectors = true)
        {
            foreach (var customEditorTool in instance.m_CustomEditorTools)
                if (customEditorTool != null && customEditorTool.GetType() == type)
                    return customEditorTool;

            if (searchLockedInspectors)
            {
                foreach (var customEditorTool in instance.m_LockedCustomEditorTools)
                    if (customEditorTool != null && customEditorTool.GetType() == type)
                        return customEditorTool;
            }

            return null;
        }

        internal static EditorTool GetCustomEditorToolsForType(Type targetType, List<EditorTool> list, bool searchLockedInspectors)
        {
            foreach (var customEditorTool in instance.m_CustomEditorTools)
                if (customEditorTool != null &&
                    EditorToolUtility.GetCustomEditorToolTargetType(customEditorTool) == targetType)
                    list.Add(customEditorTool);

            if (searchLockedInspectors)
            {
                foreach (var customEditorTool in instance.m_LockedCustomEditorTools)
                    if (customEditorTool != null && EditorToolUtility.GetCustomEditorToolTargetType(customEditorTool) == targetType)
                        list.Add(customEditorTool);
            }

            return null;
        }

        internal static void GetCustomEditorTools(List<EditorTool> list, bool includeLockedInspectorTools)
        {
            list.Clear();

            foreach (var customEditorTool in instance.m_CustomEditorTools)
                list.Add(customEditorTool);

            if (includeLockedInspectorTools)
            {
                foreach (var customEditorTool in instance.m_LockedCustomEditorTools)
                    list.Add(customEditorTool);
            }
        }

        internal static void GetCustomEditorToolsForTarget(UnityObject target, List<EditorTool> list, bool searchLockedInspectors)
        {
            list.Clear();

            if (target == null)
                return;

            foreach (var tool in instance.m_CustomEditorTools)
            {
                if (tool.targets.Contains(target))
                    list.Add(tool);
            }

            if (searchLockedInspectors)
            {
                foreach (var tool in instance.m_LockedCustomEditorTools)
                {
                    if (tool.targets.Contains(target))
                        list.Add(tool);
                }
            }
        }

        internal static EditorTool GetCustomEditorTool(Func<EditorTool, bool> predicate, bool searchLockedInspectors)
        {
            foreach (var tool in instance.m_CustomEditorTools)
                if (predicate(tool))
                    return tool;

            if (searchLockedInspectors)
                foreach (var tool in instance.m_LockedCustomEditorTools)
                    if (predicate(tool))
                        return tool;

            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static ScriptableObject CreateInstance(Type type, Action<ScriptableObject> initialize)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(type))
                throw new ArgumentException("Type must inherit ScriptableObject.", "type");

            var res = CreateScriptableObjectInstanceFromType(type, false);

            try
            {
                initialize(res);
            }
            finally
            {
                ResetAndApplyDefaultInstances(res);
            }

            return res;
        }

        internal static void InvokeOnSceneGUICustomEditorTools()
        {
            foreach (var tool in instance.m_CustomEditorTools)
            {
                var handle = tool as IDrawSelectedHandles;

                if (handle != null)
                    handle.OnDrawHandles();
            }
        }
    }
}
