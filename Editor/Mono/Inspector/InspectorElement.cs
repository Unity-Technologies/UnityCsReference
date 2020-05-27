// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using AssetImporterEditor = UnityEditor.Experimental.AssetImporters.AssetImporterEditor;

namespace UnityEditor.UIElements
{
    public class InspectorElement : BindableElement
    {
        public static readonly string ussClassName = "unity-inspector-element";
        public static readonly string customInspectorUssClassName = ussClassName + "__custom-inspector-container";
        public static readonly string iMGUIContainerUssClassName = ussClassName + "__imgui-container";

        public static readonly string iMGUIInspectorVariantUssClassName = ussClassName + "--imgui";
        public static readonly string uIEInspectorVariantUssClassName = ussClassName + "--uie";

        public static readonly string noInspectorFoundVariantUssClassName = ussClassName + "--no-inspector-found";
        public static readonly string uIECustomVariantUssClassName = ussClassName + "--uie-custom";
        public static readonly string iMGUICustomVariantUssClassName = ussClassName + "--imgui-custom";
        public static readonly string iMGUIDefaultVariantUssClassName = ussClassName + "--imgui-default";
        public static readonly string uIEDefaultVariantUssClassName = ussClassName + "--uie-default";
        public static readonly string debugVariantUssClassName = ussClassName + "--debug";
        public static readonly string debugInternalVariantUssClassName = ussClassName + "--debug-internal";

        public new class UxmlFactory : UxmlFactory<InspectorElement, UxmlTraits> {}

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            public UxmlTraits()
            {
                m_PickingMode.defaultValue = PickingMode.Ignore;
            }
        }

        [Flags]
        internal enum Mode
        {
            UIECustom = 1 << 0,
            IMGUICustom = 1 << 1,
            IMGUIDefault = 1 << 2,
            UIEDefault = 1 << 3,

            DebugMod = 1 << 4,
            DebugInternalMod = 1 << 5,

            Normal = UIECustom | IMGUICustom | IMGUIDefault | UIEDefault,
            Default = IMGUIDefault | UIEDefault,
            Custom = UIECustom | IMGUICustom,
            IMGUI = IMGUICustom | IMGUIDefault,
            UIE = UIECustom | UIEDefault,

            Debug = Default | DebugMod,
            DebugInternal = Default | DebugInternalMod
        }

        internal Mode mode { get; private set; }

        internal Editor editor
        {
            get { return m_Editor; }
            private set
            {
                if (m_Editor != value)
                {
                    DestroyOwnedEditor();
                    m_Editor = value;
                    ownsEditor = false;
                }
            }
        }

        private string m_TrackerName;
        internal string trackerName => m_TrackerName ?? (m_TrackerName = GetInspectorTrackerName(this));

        internal bool ownsEditor { get; private set; } = false;

        internal SerializedObject boundObject { get; private set; }

        internal VisualElement prefabOverrideBlueBarsContainer { get; private set; }

        private bool m_IgnoreOnInspectorGUIErrors;

        public InspectorElement() : this(null as Object) {}

        public InspectorElement(Object obj) : this(obj, Mode.Normal) {}

        internal InspectorElement(Object obj, Mode mode)
        {
            m_IgnoreOnInspectorGUIErrors = false;

            pickingMode = PickingMode.Ignore;
            AddToClassList(ussClassName);

            this.mode = mode;
            if (obj == null)
            {
                if (!GenericInspector.ObjectIsMonoBehaviourOrScriptableObject(obj))
                {
                    return;
                }
            }

            this.Bind(new SerializedObject(obj));
        }

        public InspectorElement(SerializedObject obj) : this(obj, Mode.Normal) {}

        internal InspectorElement(SerializedObject obj, Mode mode)
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList(ussClassName);

            this.mode = mode;
            if (obj.targetObject == null)
            {
                if (!GenericInspector.ObjectIsMonoBehaviourOrScriptableObject(obj.targetObject))
                {
                    return;
                }
            }

            this.Bind(obj);
        }

        public InspectorElement(Editor editor) : this(editor, Mode.Normal) {}

        internal InspectorElement(Editor editor, Mode mode)
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList(ussClassName);

            this.mode = mode;

            this.editor = editor;

            if (editor.targets.Length == 0)
            {
                return;
            }

            var targetObject = editor.targets[0];
            if (targetObject == null)
            {
                if (!GenericInspector.ObjectIsMonoBehaviourOrScriptableObject(targetObject))
                {
                    return;
                }
            }

            this.Bind(editor.serializedObject);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            DestroyOwnedEditor();
        }

        internal void AssignExistingEditor(Editor value)
        {
            if (m_Editor != value)
            {
                editor = value;
                PartialReset(m_Editor.serializedObject);
            }
        }

        void DestroyOwnedEditor()
        {
            if (ownsEditor && editor != null)
            {
                Object.DestroyImmediate(editor);
                editor = null;
                ownsEditor = false;
                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            }

            UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Reset(boundObject);
            UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        internal static Mode GetModeFromInspectorMode(InspectorMode mode)
        {
            switch (mode)
            {
                case InspectorMode.Debug:
                    return Mode.Debug;
                case InspectorMode.DebugInternal:
                    return Mode.DebugInternal;
                default:
                    return Mode.Normal;
            }
        }

        private void Reset(SerializedObject bindObject)
        {
            Clear();

            prefabOverrideBlueBarsContainer = new VisualElement();
            prefabOverrideBlueBarsContainer.name = BindingExtensions.prefabOverrideBarContainerName;
            prefabOverrideBlueBarsContainer.style.position = Position.Absolute;
            Add(prefabOverrideBlueBarsContainer);

            RemoveFromClassList(iMGUIInspectorVariantUssClassName);
            RemoveFromClassList(uIEInspectorVariantUssClassName);
            RemoveFromClassList(noInspectorFoundVariantUssClassName);
            RemoveFromClassList(uIECustomVariantUssClassName);
            RemoveFromClassList(iMGUICustomVariantUssClassName);
            RemoveFromClassList(iMGUIDefaultVariantUssClassName);
            RemoveFromClassList(uIEDefaultVariantUssClassName);
            RemoveFromClassList(debugVariantUssClassName);
            RemoveFromClassList(debugInternalVariantUssClassName);

            if (bindObject == null)
                return;

            var editor = GetOrCreateEditor(bindObject);
            if (editor == null)
            {
                return;
            }

            boundObject = bindObject;

            var customInspector = CreateInspectorElementFromEditor(editor);
            if (customInspector == null)
            {
                customInspector = CreateDefaultInspector(bindObject);
            }

            if (customInspector != null && customInspector != this)
                hierarchy.Add(customInspector);
        }

        private void PartialReset(SerializedObject bindObject)
        {
            boundObject = bindObject;
            if (boundObject == null)
            {
                Reset(null);
                return;
            }

            var customInspector = CreateInspectorElementFromEditor(editor, true);
            if (customInspector == null)
            {
                customInspector = CreateDefaultInspector(boundObject);
            }

            Clear();
            if (customInspector != null && customInspector != this)
                hierarchy.Add(customInspector);

            customInspector?.Bind(boundObject);
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            var bindEvent = evt as SerializedObjectBindEvent;
            if (bindEvent == null)
                return;

            Reset(bindEvent.bindObject);
        }

        private Editor GetOrCreateEditor(SerializedObject serializedObject)
        {
            if (editor != null)
                return editor;

            var target = serializedObject?.targetObject;

            foreach (var inspectorWindow in InspectorWindow.GetInspectors())
            {
                foreach (var trackerEditor in inspectorWindow.tracker.activeEditors)
                {
                    if (trackerEditor.target == target || trackerEditor.serializedObject == serializedObject)
                    {
                        return editor = trackerEditor;
                    }
                }
            }

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            var ed = Editor.CreateEditor(serializedObject?.targetObject);
            editor = ed;
            ownsEditor = true;

            return ed;
        }

        private VisualElement CreateDefaultInspector(SerializedObject serializedObject)
        {
            if (serializedObject == null)
                return null;

            SerializedProperty property = serializedObject.GetIterator();
            if (property.NextVisible(true)) // Expand first child.
            {
                do
                {
                    var field = new PropertyField(property);
                    field.name = "PropertyField:" + property.propertyPath;

                    if (property.propertyPath == "m_Script" && serializedObject.targetObject != null)
                        field.SetEnabled(false);

                    hierarchy.Add(field);
                }
                while (property.NextVisible(false));
            }

            if (serializedObject.targetObject == null)
                AddMissingScriptLabel(serializedObject);

            AddToClassList(uIEDefaultVariantUssClassName);
            AddToClassList(uIEInspectorVariantUssClassName);

            return this;
        }

        bool AddMissingScriptLabel(SerializedObject serializedObject)
        {
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty != null)
            {
                hierarchy.Add(new IMGUIContainer(() => GenericInspector.ShowScriptNotLoadedWarning(scriptProperty)));
                return true;
            }

            return false;
        }

        internal static bool SetWideModeForWidth(VisualElement displayElement)
        {
            var previousWideMode = EditorGUIUtility.wideMode;

            float inspectorWidth = 0;

            // the inspector's width can be NaN if this is our first layout check.
            // or when the inspector display is changed from none to flex, the width will be zero during the measuring phase.
            // we try to find a parent with a a width. If none are found, we'll set wideMode to true to avoid computing
            // too tall an inspector on the first layout calculation
            while (displayElement != null && (float.IsNaN(inspectorWidth) || inspectorWidth == 0))
            {
                inspectorWidth = displayElement.layout.width;
                displayElement = displayElement.hierarchy.parent;
            }

            if (!float.IsNaN(inspectorWidth) && inspectorWidth > 0)
            {
                EditorGUIUtility.wideMode = inspectorWidth > Editor.k_WideModeMinWidth;
            }
            else
            {
                EditorGUIUtility.wideMode = true;
            }

            return previousWideMode;
        }

        IMGUIContainer m_IMGUIContainer;

        private VisualElement CreateIMGUIInspectorFromEditor(SerializedObject serializedObject, Editor editor,
            bool reuseIMGUIContainer)
        {
            if ((mode & (Mode.IMGUICustom | Mode.IMGUIDefault)) == 0)
                return null;

            if ((mode & Mode.IMGUICustom) > 0 && (mode & Mode.IMGUIDefault) == 0 && editor is GenericInspector)
                return null;

            if ((mode & Mode.IMGUICustom) == 0 && (mode & Mode.IMGUIDefault) > 0 && !(editor is GenericInspector) && !(editor is AssetImporterEditor) && !(editor is GameObjectInspector))
            {
                editor = ScriptableObject.CreateInstance<GenericInspector>();
                editor.hideFlags = HideFlags.HideAndDontSave;
                editor.InternalSetTargets(new[] { serializedObject.targetObject });
            }

            if (editor is GenericInspector)
            {
                AddToClassList(iMGUIDefaultVariantUssClassName);
                if ((mode & Mode.DebugMod) > 0)
                {
                    AddToClassList(debugVariantUssClassName);
                    editor.inspectorMode = InspectorMode.Debug;
                }
                else if ((mode & Mode.DebugInternalMod) > 0)
                {
                    AddToClassList(debugInternalVariantUssClassName);
                    editor.inspectorMode = InspectorMode.DebugInternal;
                }
            }
            else
            {
                AddToClassList(iMGUICustomVariantUssClassName);
            }

            IMGUIContainer inspector;
            // Reusing the existing IMGUIContainer allows us to re-use the existing gui state, when we are drawing the same inspector this will let us keep the same control ids
            if (reuseIMGUIContainer && m_IMGUIContainer != null)
            {
                inspector = m_IMGUIContainer;
            }
            else
            {
                inspector = new IMGUIContainer();
            }

            m_IgnoreOnInspectorGUIErrors = false;
            inspector.onGUIHandler = () =>
            {
                if (editor && editor.target && (editor.target.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                    return;

                // It's possible to run 2-3 frames after the tracker of this inspector window has
                // been recreated, and with it the Editor and its SerializedObject. One example of
                // when this happens is when the Preview window is detached from a *second* instance
                // of an InspectorWindow and re-attached.
                //
                // This is only a problem for the *second* (or third, forth, etc) instance of
                // the InspectorWindow because only the first instance can use the
                // ActiveEditorTracker.sharedTracker in InspectorWindow.CreateTracker(). The
                // other instances have to create a new tracker...each time.
                //
                // Not an ideal solution, but basically we temporarily hold the printing to console
                // for errors for which GUIUtility.ShouldRethrowException(e) returns false.
                // The errors that may occur during this brief "bad state" are SerializedProperty
                // errors. If the custom Editor created and remembered references to some
                // SerializedProperties during its OnEnable(), those references will be invalid
                // when the tracker is refreshed, until this GUIHandler is reassigned. This fix
                // just ignores those errors.
                //
                // We don't simply early return here because that can break some tests that
                // rely heavily on yields and timing of UI redraws. Yes..
                //
                // case 1119612
                if (editor.m_SerializedObject == null)
                {
                    editor.Repaint();
                    m_IgnoreOnInspectorGUIErrors = true;
                }

                if ((editor.target == null && !GenericInspector.ObjectIsMonoBehaviourOrScriptableObject(editor.target)) ||
                    !editor.serializedObject.isValid)
                {
                    return;
                }

                EditorGUIUtility.ResetGUIState();
                using (new EditorGUI.DisabledScope(!editor.IsEnabled()))
                {
                    var genericEditor = editor as GenericInspector;
                    if (genericEditor != null)
                    {
                        switch (mode)
                        {
                            case Mode.Normal:
                                genericEditor.inspectorMode = InspectorMode.Normal;
                                break;
                            case Mode.Default:
                                genericEditor.inspectorMode = InspectorMode.Debug;
                                break;
                            case Mode.Custom:
                                genericEditor.inspectorMode = InspectorMode.DebugInternal;
                                break;
                            case Mode.IMGUI:
                                break;
                        }
                    }

                    //set the current PropertyHandlerCache to the current editor
                    ScriptAttributeUtility.propertyHandlerCache = editor.propertyHandlerCache;

                    var originalHierarchyMode = EditorGUIUtility.hierarchyMode;
                    EditorGUIUtility.hierarchyMode = true;

                    var originalWideMode = SetWideModeForWidth(inspector);

                    GUIStyle editorWrapper = (editor.UseDefaultMargins() && editor.CanBeExpandedViaAFoldoutWithoutUpdate()
                        ? EditorStyles.inspectorDefaultMargins
                        : GUIStyle.none);
                    try
                    {
                        GUI.changed = false;

                        using (new InspectorWindowUtils.LayoutGroupChecker())
                        {
                            EditorGUILayout.BeginVertical(editorWrapper);
                            {
                                // we have no guarantees regarding what happens in the try/catch block below,
                                // so we need to save state here and restore it afterwards,
                                // the natural thing to do would be using SavedGUIState,
                                // but it implicitly resets keyboards bindings and it breaks functionality.
                                // We have identified issues with layout so we just save that for the time being.
                                var layoutCache = new GUILayoutUtility.LayoutCache(GUILayoutUtility.current);
                                try
                                {
                                    var rebuildOptimizedGUIBlocks = GetRebuildOptimizedGUIBlocks(editor.target);
                                    rebuildOptimizedGUIBlocks |= editor.isInspectorDirty;
                                    float height;
                                    if (editor.GetOptimizedGUIBlock(rebuildOptimizedGUIBlocks, visible, out height))
                                    {
                                        var contentRect = GUILayoutUtility.GetRect(0, visible ? height : 0);

                                        // Layout events are ignored in the optimized code path
                                        // The exception is when we are drawing a GenericInspector, they always use the optimized path and must therefore run at least one layout calculation in it
                                        if (Event.current.type == EventType.Layout && !(editor is GenericInspector))
                                        {
                                            return;
                                        }

                                        InspectorWindowUtils.DrawAddedComponentBackground(contentRect, editor.targets);

                                        // Draw content
                                        if (visible)
                                        {
                                            GUI.changed = false;
                                            editor.OnOptimizedInspectorGUI(contentRect);
                                        }
                                    }
                                    else
                                    {
                                        InspectorWindowUtils.DrawAddedComponentBackground(contentRect, editor.targets);
                                        using (new EditorPerformanceTracker(trackerName))
                                            editor.OnInspectorGUI();
                                    }
                                }
                                catch (Exception e)
                                {
                                    if (GUIUtility.ShouldRethrowException(e))
                                    {
                                        throw;
                                    }

                                    if (!m_IgnoreOnInspectorGUIErrors)
                                        Debug.LogException(e);
                                }
                                finally
                                {
                                    GUILayoutUtility.current = layoutCache;
                                }
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }
                    finally
                    {
                        if (GUI.changed)
                        {
                            // This forces a relayout of all imguicontainers in this inspector window.
                            // fixes part of case 1148706
                            var element = inspector.GetFirstAncestorOfType<EditorElement>();
                            if (element != null)
                                EditorElement.InvalidateIMGUILayouts(element.parent);
                        }
                        EditorGUIUtility.wideMode = originalWideMode;
                        EditorGUIUtility.hierarchyMode = originalHierarchyMode;
                    }
                }
            };

            inspector.style.overflow = Overflow.Visible;
            m_IMGUIContainer = inspector;

            if (!(editor is GenericInspector))
                inspector.AddToClassList(customInspectorUssClassName);

            inspector.AddToClassList(iMGUIContainerUssClassName);

            AddToClassList(iMGUIInspectorVariantUssClassName);

            return inspector;
        }

        internal static string GetInspectorTrackerName(VisualElement el)
        {
            var editorElementParent = el.parent as EditorElement;
            if (editorElementParent == null)
                return $"Editor.Unknown.OnInspectorGUI";

            return $"Editor.{editorElementParent.name}.OnInspectorGUI";
        }

        private VisualElement CreateInspectorElementFromEditor(Editor editor, bool reuseIMGUIContainer = false)
        {
            var serializedObject = editor.serializedObject;
            var target = editor.targets[0];
            if (target == null)
            {
                if (!GenericInspector.ObjectIsMonoBehaviourOrScriptableObject(target))
                {
                    return null;
                }
            }

            VisualElement inspectorElement = null;

            if ((mode & Mode.UIECustom) > 0)
            {
                inspectorElement = editor.CreateInspectorGUI();
                if (inspectorElement != null)
                {
                    AddToClassList(uIECustomVariantUssClassName);
                    if (editor.UseDefaultMargins())
                        AddToClassList(uIEInspectorVariantUssClassName);
                    inspectorElement.AddToClassList(customInspectorUssClassName);
                }
            }

            if (inspectorElement == null)
                inspectorElement = CreateIMGUIInspectorFromEditor(serializedObject, editor, reuseIMGUIContainer);

            if (inspectorElement == null && (mode & Mode.UIEDefault) > 0)
                inspectorElement = CreateDefaultInspector(serializedObject);

            if (inspectorElement == null)
            {
                AddToClassList(noInspectorFoundVariantUssClassName);
                AddToClassList(uIEInspectorVariantUssClassName);
                inspectorElement = new Label("No inspector found given the current Inspector.Mode.");
            }

            return inspectorElement;
        }

        bool m_IsOpenForEdit;
        bool m_InvalidateGUIBlockCache = true;
        Editor m_Editor;

        private bool GetRebuildOptimizedGUIBlocks(Object inspectedObject)
        {
            var rebuildOptimizedGUIBlocks = false;

            if (Event.current.type == EventType.Repaint)
            {
                if (inspectedObject != null
                    && m_IsOpenForEdit != Editor.IsAppropriateFileOpenForEdit(inspectedObject))
                {
                    m_IsOpenForEdit = !m_IsOpenForEdit;
                    rebuildOptimizedGUIBlocks = true;
                }

                if (m_InvalidateGUIBlockCache)
                {
                    rebuildOptimizedGUIBlocks = true;
                    m_InvalidateGUIBlockCache = false;
                }
            }
            else if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == EventCommandNames.EyeDropperUpdate)
            {
                rebuildOptimizedGUIBlocks = true;
            }

            return rebuildOptimizedGUIBlocks;
        }
    }
}
