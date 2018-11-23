// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    sealed class EditorToolContext : ScriptableSingleton<EditorToolContext>
    {
        [SerializeField]
        List<EditorTool> m_ToolSingletons = new List<EditorTool>();

        [SerializeField]
        EditorTool m_ActiveTool;

        static bool s_ChangingActiveTool;

        internal static EditorTool activeTool
        {
            get
            {
                return instance.m_ActiveTool;
            }

            set
            {
                if (s_ChangingActiveTool)
                    throw new InvalidOperationException("Attempting to set the active tool from EditorTool.OnActivate or EditorTool.OnDeactivate. This is not allowed.");

                var tool = value;

                if (tool == null)
                    tool = GetSingleton<NoneTool>();

                if (tool == instance.m_ActiveTool)
                    return;

                s_ChangingActiveTool = true;

                int index = instance.m_ToolHistory.IndexOf(tool);

                if (index > -1)
                    instance.m_ToolHistory.RemoveAt(index);

                instance.m_ToolHistory.Add(tool);

                if (instance.m_ActiveTool != null)
                    instance.m_ActiveTool.OnDeactivate();

                var previous = instance.m_ActiveTool;
                instance.m_ActiveTool = tool;

                tool.OnActivate();

                Tools.SyncToolEnum();

                s_ChangingActiveTool = false;

                if (toolChanged != null)
                    toolChanged(previous, tool);
            }
        }

        [SerializeField]
        List<EditorTool> m_ToolHistory = new List<EditorTool>();

        // The currently available CustomEditor EditorTools.
        HashSet<EditorTool> m_CustomEditorTools = new HashSet<EditorTool>();

        [Serializable]
        struct CustomEditorToolContext : ISerializationCallbackReceiver
        {
            [SerializeField]
            string m_EditorToolType;

            [SerializeField]
            string m_EditorToolState;

            public Type editorToolType;

            public UObject[] targetObjects;

            public string editorToolState
            {
                get { return m_EditorToolState; }
            }

            public static readonly CustomEditorToolContext Empty = new CustomEditorToolContext(null);

            public CustomEditorToolContext(EditorTool tool)
            {
                if (tool != null)
                {
                    editorToolType = tool.GetType();
                    targetObjects = tool.targets.ToArray();
                    m_EditorToolState = EditorJsonUtility.ToJson(tool);
                }
                else
                {
                    editorToolType = null;
                    targetObjects = null;
                    m_EditorToolState = null;
                }

                m_EditorToolType = null;
            }

            public bool IsEqual(EditorTool other)
            {
                if (editorToolType != (other == null ? null : other.GetType()))
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
        CustomEditorToolContext m_PreviousCustomEditorToolContext;

        public static event Action<EditorTool, EditorTool> toolChanged;

        // EditorApplication.isPlayingOrWillEnterPlayMode doesn't handle exiting.
        [SerializeField]
        PlayModeStateChange m_PlayModeState;

        EditorToolContext() {}

        void OnEnable()
        {
            RebuildAvailableCustomEditorTools();

            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            Undo.undoRedoPerformed += UndoRedoPerformed;
            ActiveEditorTracker.editorTrackerRebuilt += TrackerRebuilt;
            Selection.selectedObjectWasDestroyed += SelectedObjectWasDestroyed;

            if (m_ActiveTool != null)
                m_ActiveTool.OnActivate();
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Selection.selectedObjectWasDestroyed -= SelectedObjectWasDestroyed;
            ActiveEditorTracker.editorTrackerRebuilt -= TrackerRebuilt;

            ClearCustomEditorTools();

            if (m_ActiveTool != null)
                m_ActiveTool.OnDeactivate();
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
            if (m_PlayModeState == PlayModeStateChange.EnteredPlayMode)
                RebuildAvailableCustomEditorTools();
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
            if (m_CustomEditorTools.Contains(m_ActiveTool) &&
                m_ActiveTool.m_Targets.Any(x => x == null || x.GetInstanceID() == id))
            {
                m_PreviousCustomEditorToolContext = new CustomEditorToolContext(m_ActiveTool);
                RestorePreviousTool();
            }
        }

        void UndoRedoPerformed()
        {
            RestoreCustomEditorTool();
        }

        void RestoreCustomEditorTool()
        {
            var restored = m_CustomEditorTools.FirstOrDefault(m_PreviousCustomEditorToolContext.IsEqual);

            if (restored != null)
            {
                var targets = restored.targets.ToArray();
                EditorJsonUtility.FromJsonOverwrite(m_PreviousCustomEditorToolContext.editorToolState, restored);
                restored.m_Targets = targets;
                activeTool = restored;
            }

            m_PreviousCustomEditorToolContext = CustomEditorToolContext.Empty;
        }

        void ClearCustomEditorTools()
        {
            foreach (var tool in m_CustomEditorTools)
            {
                if (tool != null && tool != m_ActiveTool)
                {
                    DestroyImmediate(tool);
                }
            }

            m_CustomEditorTools.Clear();
        }

        void CleanupToolSingletons()
        {
            for (int i = m_ToolSingletons.Count - 1; i > -1; i--)
            {
                if (m_ToolSingletons[i] == null)
                    m_ToolSingletons.RemoveAt(i);
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

        internal static T GetSingleton<T>() where T : EditorTool
        {
            return (T)GetSingleton(typeof(T));
        }

        internal static EditorTool GetSingleton(Type type)
        {
            instance.CleanupToolSingletons();
            var res = instance.m_ToolSingletons.FirstOrDefault(x => x.GetType() == type);
            if (res != null)
                return res;
            res = (EditorTool)CreateInstance(type);
            res.hideFlags = HideFlags.DontSave;
            instance.m_ToolSingletons.Add(res);
            instance.Save(true);
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
            if (Tools.s_Hidden || instance.m_ActiveTool == null)
                return;

            var current = instance.m_ActiveTool;

            using (new EditorGUI.DisabledScope(!current.IsAvailable()))
            {
                current.OnToolGUI(window);
            }
        }

        bool IsCustomEditorTool(EditorTool tool)
        {
            return tool != null
                && tool.m_Targets != null
                && tool.m_Targets.Length > 0;
        }

        void RebuildAvailableCustomEditorTools()
        {
            // Do not rebuild the cache since objects are serialized, destroyed, deserialized during this phase
            if (m_PlayModeState == PlayModeStateChange.ExitingEditMode ||
                m_PlayModeState == PlayModeStateChange.ExitingPlayMode)
                return;

            var isCustomEditorTool = IsCustomEditorTool(m_ActiveTool);

            ClearCustomEditorTools();

            foreach (var kvp in EditorToolUtility.FindActiveCustomEditorTools())
            {
                var toolType = kvp.Key;
                var toolInfo = kvp.Value.ToArray();

                if (isCustomEditorTool && m_ActiveTool.GetType() == toolType)
                {
                    m_ActiveTool.m_Targets = toolInfo;
                    m_CustomEditorTools.Add(m_ActiveTool);
                }
                else
                {
                    var toolInstance = (EditorTool)CreateInstance(toolType, x => { ((EditorTool)x).m_Targets = toolInfo; });
                    toolInstance.hideFlags = HideFlags.DontSave;
                    m_CustomEditorTools.Add(toolInstance);
                }
            }

            if (isCustomEditorTool && !m_CustomEditorTools.Contains(m_ActiveTool))
            {
                m_PreviousCustomEditorToolContext = new CustomEditorToolContext(m_ActiveTool);
                DestroyImmediate(m_ActiveTool);
                RestorePreviousTool();
            }
        }

        internal static EditorTool GetCustomEditorToolOfType(Type type)
        {
            foreach (var tool in instance.m_CustomEditorTools)
                if (tool != null && tool.GetType() == type)
                    return tool;
            return null;
        }

        internal static void GetCustomEditorTools(List<EditorTool> list)
        {
            list.Clear();

            foreach (var customEditorTool in instance.m_CustomEditorTools)
                list.Add(customEditorTool);
        }

        internal static void GetCustomEditorTools(Type type, List<EditorTool> list)
        {
            if (type == null)
                return;

            list.Clear();

            foreach (var customEditorTool in instance.m_CustomEditorTools)
            {
                if (EditorToolUtility.GetCustomEditorToolTargetType(customEditorTool) == type)
                    list.Add(customEditorTool);
            }
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
    }
}
