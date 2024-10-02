// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Create a VisualElement inspector from a SerializedObject.
    /// </summary>
    /// <remarks>
    /// Upon Bind(), the InspectorElement will generate PropertyFields inside according to the SerializedProperties inside the bound SerializedObject.
    /// </remarks>
    public class InspectorElement : BindableElement
    {
        /// <summary>
        /// The framework that should be used to build and run the UI. This is only used for default inspectors (i.e. editors of type <see cref="GenericInspector"/>)
        /// </summary>
        internal enum DefaultInspectorFramework
        {
            /// <summary>
            /// UIToolkit should be used to generate property fields for default inspectors.
            /// </summary>
            UIToolkit,

            /// <summary>
            /// IMGUI should be used to generate property fields for default inspectors. These will be placed inside of a <see cref="IMGUIContainer"/>.
            /// </summary>
            IMGUI
        }

        static readonly ProfilerMarker k_CreateInspectorElementFromSerializedObject = new ProfilerMarker("InspectorElement.CreateInspectorElementFromSerializedObject");

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-inspector-element";

        /// <summary>
        /// USS class name of custom inspector elements in elements of this type.
        /// </summary>
        public static readonly string customInspectorUssClassName = ussClassName + "__custom-inspector-container";

        /// <summary>
        /// USS class name of IMGUI containers in elements of this type.
        /// </summary>
        public static readonly string iMGUIContainerUssClassName = ussClassName + "__imgui-container";

        /// <summary>
        /// USS class name of elements of this type, when they are displayed in IMGUI inspector mode.
        /// </summary>
        public static readonly string iMGUIInspectorVariantUssClassName = ussClassName + "--imgui";

        /// <summary>
        /// USS class name of elements of this type, when they are displayed in UIElements inspector mode.
        /// </summary>
        public static readonly string uIEInspectorVariantUssClassName = ussClassName + "--uie";

        /// <summary>
        /// USS class name of elements of this type, when no inspector is found.
        /// </summary>
        public static readonly string noInspectorFoundVariantUssClassName = ussClassName + "--no-inspector-found";

        /// <summary>
        /// USS class name of elements of this type, when they are displayed in UIElements custom mode.
        /// </summary>
        public static readonly string uIECustomVariantUssClassName = ussClassName + "--uie-custom";

        /// <summary>
        /// USS class name of elements of this type, when they are displayed in IMGUI custom mode.
        /// </summary>
        public static readonly string iMGUICustomVariantUssClassName = ussClassName + "--imgui-custom";

        /// <summary>
        /// USS class name of elements of this type, when they are displayed in IMGUI default mode.
        /// </summary>
        public static readonly string iMGUIDefaultVariantUssClassName = ussClassName + "--imgui-default";

        /// <summary>
        /// USS class name of elements of this type, when they are displayed in UIElements default mode.
        /// </summary>
        public static readonly string uIEDefaultVariantUssClassName = ussClassName + "--uie-default";

        /// <summary>
        /// USS class name of elements of this type, when they are displayed in debug USS mode.
        /// </summary>
        public static readonly string debugVariantUssClassName = ussClassName + "--debug";

        /// <summary>
        /// USS class name of elements of this type, when they are displayed in debug internal mode.
        /// </summary>
        public static readonly string debugInternalVariantUssClassName = ussClassName + "--debug-internal";

        /// <summary>
        /// USS class name of elements of this type, when they are displayed without a missing script type.
        /// </summary>
        internal static readonly string noScriptErrorContainerName = "unity-inspector-no-script-error-container";

        internal static bool disabledThrottling { get; set; } = false;

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            public override object CreateInstance() => new InspectorElement();
        }

        /// <summary>
        /// Instantiates a <see cref="InspectorElement"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<InspectorElement, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="InspectorElement"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                m_PickingMode.defaultValue = PickingMode.Ignore;
            }
        }

        /// <summary>
        /// Gets the default backend to use based on the current editor settings.
        /// </summary>
        /// <returns>The default backend type to use.</returns>
        static DefaultInspectorFramework GetDefaultInspectorFramework() => EditorSettings.inspectorUseIMGUIDefaultInspector ? DefaultInspectorFramework.IMGUI : DefaultInspectorFramework.UIToolkit;

        /// <summary>
        /// The editor this element is inspecting.
        /// <remarks>
        /// An <see cref="InspectorElement"/> must ALWAYS be backed by an <see cref="Editor"/>. If one does not exist the inspector will create and manage one.
        /// </remarks>
        /// </summary>
        Editor m_Editor;

        /// <summary>
        /// Flag indicating if the InspectorElement is managing it's own editor instance.
        /// </summary>
        bool m_OwnsEditor;

        /// <summary>
        /// The currently bound serialized object.
        /// </summary>
        SerializedObject m_BoundObject;

        /// <summary>
        /// The currently bound object type, this can be used as an optional optimization to avoid rebuilding the UI if the type doesn't change.
        /// </summary>
        Type m_BoundObjectType;

        /// <summary>
        /// The root element of this inspector. This can either be a full visual hierarchy or an IMGUIContainer.
        /// </summary>
        VisualElement m_InspectorElement;

        /// <summary>
        /// The default framework to use for generic inspectors.
        /// </summary>
        DefaultInspectorFramework m_DefaultInspectorFramework;

        /// <summary>
        /// The cached tracker name.
        /// </summary>
        string m_TrackerName;

        bool m_IgnoreOnInspectorGUIErrors;
        bool m_IsOpenForEdit;
        bool m_InvalidateGUIBlockCache = true;
        bool m_Rebind;
        VisualElement m_ContextWidthElement;

        /// <summary>
        /// Gets or sets the editor backing this inspector element.
        /// </summary>
        internal Editor editor => m_Editor;

        /// <summary>
        /// Returns true if the inspector element owns and manages it's own editor instance.
        /// </summary>
        internal bool ownsEditor => m_OwnsEditor;

        /// <summary>
        /// Returns the current tracker name for this element. This is used for editor performance tracking.
        /// </summary>
        internal string trackerName => m_TrackerName ??= GetInspectorTrackerName(this);

        /// <summary>
        /// The currently bound object.
        /// </summary>
        internal SerializedObject boundObject => m_BoundObject;

        /// <summary>
        /// Visual element reference for prefab override bar.
        /// </summary>
        internal VisualElement prefabOverrideBlueBarsContainer { get; private set; }

        /// <summary>
        /// Visual element reference for live property bar.
        /// </summary>
        internal VisualElement livePropertyYellowBarsContainer { get; private set; }

        internal EditorGUIUtility.ComparisonViewMode comparisonViewMode { get; set; }

        /// <summary>
        /// Gets or sets the default inspector framework to use for this inspector. This will take affect during the next bind.
        /// </summary>
        internal DefaultInspectorFramework defaultInspectorFramework
        {
            get => m_DefaultInspectorFramework;
            set
            {
                m_Rebind = true;
                m_DefaultInspectorFramework = value;
            }
        }

        /// <summary>
        /// Returns the editor performance tracker name for this inspector element.
        /// </summary>
        /// <param name="element">The element to generate a name for.</param>
        /// <returns>The generated performance tracker name.</returns>
        internal static string GetInspectorTrackerName(VisualElement element)
        {
            var editorElementParent = element.parent as EditorElement;

            return editorElementParent == null
                ? $"Editor.Unknown.OnInspectorGUI"
                : $"Editor.{editorElementParent.name}.OnInspectorGUI";
        }

        /// <summary>
        /// Constructs a <see cref="SerializedObject"/> instance for a given target.
        /// </summary>
        static SerializedObject CreateSerializedObjectForTarget(Object target)
        {
            if (target == null)
            {
                // ReSharper disable once ExpressionIsAlwaysNull
                // Check if this is a 'fake null' object (i.e. equals operator reports null but the object still exists)
                if (!GenericInspector.ObjectIsMonoBehaviourOrScriptableObjectWithoutScript(target))
                    return null;
            }

            return new SerializedObject(target);
        }

        /// <summary>
        /// Initialized a new instance of <see cref="InspectorElement"/>.
        /// </summary>
        public InspectorElement() : this(null as Object) {}

        /// <summary>
        /// Initialized a new instance of <see cref="InspectorElement"/> for the specified <see cref="Object"/>.
        /// </summary>
        /// <param name="obj">The object to bind to.</param>
        public InspectorElement(Object obj) : this(CreateSerializedObjectForTarget(obj), GetDefaultInspectorFramework()) {}

        /// <summary>
        /// Initialized a new instance of <see cref="InspectorElement"/> for the specified <see cref="SerializedObject"/>.
        /// </summary>
        /// <param name="obj">The object to bind to.</param>
        public InspectorElement(SerializedObject obj) : this(obj, GetDefaultInspectorFramework()) {}

        /// <summary>
        /// Initialized a new instance of <see cref="InspectorElement"/> for the specified <see cref="Editor"/>.
        /// </summary>
        /// <param name="editor">The editor to bind to.</param>
        public InspectorElement(Editor editor) : this(editor, GetDefaultInspectorFramework()) {}

        internal InspectorElement(Object obj, DefaultInspectorFramework defaultInspectorFramework) : this(new SerializedObject(obj), null, defaultInspectorFramework) { }
        internal InspectorElement(SerializedObject serializedObject, DefaultInspectorFramework defaultInspectorFramework) : this(serializedObject, null, defaultInspectorFramework) {}
        internal InspectorElement(Editor editor, DefaultInspectorFramework defaultInspectorFramework) : this(editor.serializedObject, editor, defaultInspectorFramework) {}

        InspectorElement(SerializedObject obj, Editor editor, DefaultInspectorFramework defaultInspectorFramework)
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList(ussClassName);

            // Register ui callbacks
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            prefabOverrideBlueBarsContainer = new VisualElement
            {
                name = BindingExtensions.prefabOverrideBarContainerName,
                style = { position = Position.Absolute }
            };

            livePropertyYellowBarsContainer = new VisualElement
            {
                name = BindingExtensions.livePropertyBarContainerName,
                style = { position = Position.Absolute }
            };

            Add(prefabOverrideBlueBarsContainer);
            Add(livePropertyYellowBarsContainer);

            m_DefaultInspectorFramework = defaultInspectorFramework;

            // Find or construct an editor for this object.
            if (editor == null)
            {
                if (obj == null)
                    return;

                m_Editor = Editor.CreateEditor(obj.targetObjects);
                m_OwnsEditor = true;
            }
            else
            {
                m_Editor = editor;

                // If an editor was provided but the targets are not set.
                // This should never happen in normal operation since all multi argument constructors are internal/private.
                if (m_Editor.targets.Length == 0)
                    return;
            }

            // We have a valid target, initiate binding.
            this.Bind(obj);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (boundObject != null && boundObject.isValid && m_Editor == null)
            {
                m_Rebind = true;
                this.Bind(boundObject);
            }

            var currentElement = parent;
            while (currentElement != null)
            {
                if (!currentElement.ClassListContains(PropertyEditor.s_MainContainerClassName))
                {
                    currentElement = currentElement.parent;
                    continue;
                }

                m_ContextWidthElement = currentElement;
                break;
            }
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            DestroyOwnedEditor();
        }

        /// <summary>
        /// Destroys and cleans up the editor instance managed by this inspector element. If any.
        /// </summary>
        void DestroyOwnedEditor()
        {
            if (!m_OwnsEditor || m_Editor == null)
                return;

            Object.DestroyImmediate(m_Editor);

            m_Editor = null;
            m_OwnsEditor = false;
        }

        [EventInterest(typeof(SerializedObjectBindEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            var bindEvent = evt as SerializedObjectBindEvent;
            if (bindEvent == null)
                return;

            // Case 1336093. nested InspectorElement for other editors have their own BindTree processes,
            // so we need to ignore SerializedObjectBindEvent that aren't meant for them.
            // We use the DataSource property to store a reference to the target object that is being bound
            // so we can ignore the binding process that targets a parent inspector.
            var dataSource = GetProperty(BindingExtensions.s_DataSourceProperty);
            if (dataSource != null && dataSource != bindEvent.bindObject)
            {
                evt.StopPropagation();
                return;
            }

            // Determine if we need to rebuild. This should only be done when our serialized object target changes or something forces us to rebuild (i.e. backend changing).
            var shouldRebuildElements = bindEvent.bindObject != null && bindEvent.bindObject.isValid && (m_Rebind || m_BoundObject != bindEvent.bindObject);

            if (shouldRebuildElements)
            {
                m_Rebind = false;
                CreateInspectorElementFromSerializedObject(bindEvent.bindObject);
            }
        }

        [EventInterest(EventInterestOptions.Inherit)]
        [Obsolete("ExecuteDefaultActionAtTarget override has been removed because default event handling was migrated to HandleEventBubbleUp. Please use HandleEventBubbleUp.", false)]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
        }

        /// <summary>
        /// Assigns the given editor to this inspector element instance. This will trigger a re-bind.
        /// </summary>
        /// <param name="value">The editor to assign.</param>
        internal void SetEditor(Editor value)
        {
            // NOTE: We need a special check here for undo/redo cases when the script is changing.
            var targetTypeMatches = value.target && value.target.GetType() == m_BoundObjectType;
            var editorInstanceMatches = m_Editor == value;

            if (targetTypeMatches && editorInstanceMatches)
                return;

            DestroyOwnedEditor();

            m_Editor = value;
            m_Rebind = true;

            this.Bind(m_Editor.serializedObject);
        }

        void ClearInspectorElement()
        {
            // Clear any previously generated element.
            m_InspectorElement?.RemoveFromHierarchy();

            // Clear all top level styles
            RemoveFromClassList(iMGUIInspectorVariantUssClassName);
            RemoveFromClassList(uIEInspectorVariantUssClassName);
            RemoveFromClassList(noInspectorFoundVariantUssClassName);
            RemoveFromClassList(uIECustomVariantUssClassName);
            RemoveFromClassList(iMGUICustomVariantUssClassName);
            RemoveFromClassList(iMGUIDefaultVariantUssClassName);
            RemoveFromClassList(uIEDefaultVariantUssClassName);
            RemoveFromClassList(debugVariantUssClassName);
            RemoveFromClassList(debugInternalVariantUssClassName);
        }

        void CreateInspectorElementFromSerializedObject(SerializedObject bindObject)
        {
            k_CreateInspectorElementFromSerializedObject.Begin();

            // Unpack the given serialized object. We want to cache the target type to facilitate re-using UI later down the pipeline.
            var targetObject = bindObject?.targetObject;
            var targetObjectType = targetObject != null ? targetObject.GetType() : null;

            var bindObjectTypeMatches = m_BoundObjectType == targetObjectType;

            m_BoundObject = bindObject;
            m_BoundObjectType = targetObjectType;

            if (m_BoundObject == null)
            {
                ClearInspectorElement();
                return;
            }

            m_Editor = GetOrCreateEditor(bindObject);

            if (m_Editor != null)
            {
                if (m_Editor is GenericInspector)
                {
                    // Only re-build default inspectors when the type changes or when using the IMGUI backend.
                    // NOTE: When using IMGUI the delegate captures locals so we must rebuild it.
                    if (!bindObjectTypeMatches || m_InspectorElement == null || m_DefaultInspectorFramework == DefaultInspectorFramework.IMGUI)
                    {
                        ClearInspectorElement();

                        // When looking at a generic inspector we choose the backend based on a user provided default.
                        switch (m_DefaultInspectorFramework)
                        {
                            case DefaultInspectorFramework.UIToolkit:
                                m_InspectorElement = CreateInspectorElementUsingUIToolkit(m_Editor);
                                break;
                            case DefaultInspectorFramework.IMGUI:
                                m_InspectorElement = CreateInspectorElementUsingIMGUI(m_Editor);
                                break;
                        }
                    }
                }
                else
                {
                    // Always clear and re-build when dealing with custom inspectors. User code is assumed to take a reference to the ScriptableObject in `CreateInspectorGUI()`.
                    ClearInspectorElement();

                    // This is a custom editor type. Try to use UI toolkit first with an IMGUI fallback.
                    m_InspectorElement = CreateInspectorElementUsingUIToolkit(m_Editor) ?? CreateInspectorElementUsingIMGUI(m_Editor);
                    m_InspectorElement.AddToClassList(customInspectorUssClassName);
                }

                // Re-add the generated element if it was re-created.
                if (m_InspectorElement != null && m_InspectorElement.parent != this)
                    hierarchy.Add(m_InspectorElement);
            }

            k_CreateInspectorElementFromSerializedObject.End();
        }

        /// <summary>
        /// This method handles all the internals of getting a <see cref="Editor"/> object for the given serialized object.
        /// </summary>
        /// <param name="serializedObject">The serialized object to get an editor for.</param>
        /// <returns>The found or created instance.</returns>
        Editor GetOrCreateEditor(SerializedObject serializedObject)
        {
            Object[] targets = null;

            if (serializedObject != null && serializedObject.m_NativeObjectPtr != IntPtr.Zero)
                targets = serializedObject.targetObjects;

            if (m_Editor != null)
            {
                // First try to re-use the instance we have on hand. If this matches our given object we can simply re-use it.
                if (m_Editor.serializedObject == serializedObject || ArrayUtility.ArrayReferenceEquals(m_Editor.targets, targets))
                    return m_Editor;

                // We need to generate a new editor instance, first cleanup any owned editor resources we have.
                DestroyOwnedEditor();
            }

            // Fallback to creating our own editor instance for the given object.
            m_Editor = Editor.CreateEditor(targets);
            m_OwnsEditor = true;

            return m_Editor;
        }

        /// <summary>
        /// Adds default inspector property fields under a container VisualElement
        /// </summary>
        /// <param name="container">The parent VisualElement</param>
        /// <param name="serializedObject">The SerializedObject to inspect</param>
        /// <param name="editor">The editor currently used</param>
        public static void FillDefaultInspector(VisualElement container, SerializedObject serializedObject, Editor editor)
        {
            if (serializedObject == null)
                return;

            bool isPartOfPrefabInstance = editor != null && GenericInspector.IsAnyMonoBehaviourTargetPartOfPrefabInstance(editor);

            SerializedProperty property = serializedObject.GetIterator();
            if (property.NextVisible(true)) // Expand first child.
            {
                do
                {
                    var field = new PropertyField(property)
                    {
                        name = "PropertyField:" + property.propertyPath
                    };

                    if (property.propertyPath == "m_Script")
                    {
                        // Allow script re-assignment in debug mode.
                        var isDebugMode = editor != null && (editor.inspectorMode == InspectorMode.Debug || editor.inspectorMode == InspectorMode.DebugInternal);

                        if (!isDebugMode && (serializedObject.targetObject != null || property.objectReferenceValue != null || isPartOfPrefabInstance))
                            field.SetEnabled(false);
                    }

                    container.Add(field);
                }
                while (property.NextVisible(false));
            }

            if (serializedObject.targetObject == null)
            {
                var scriptProperty = serializedObject.FindProperty("m_Script");

                if (scriptProperty != null)
                {
                    var noScriptErrorContainer = new IMGUIContainer(() =>
                    {
                        if (scriptProperty.isValid)
                            GenericInspector.ShowScriptNotLoadedWarning(scriptProperty, isPartOfPrefabInstance);
                    });
                    noScriptErrorContainer.name = noScriptErrorContainerName;
                    container.Add(noScriptErrorContainer);
                }
            }
        }

        VisualElement CreateInspectorElementUsingUIToolkit(Editor targetEditor)
        {
            var element = targetEditor.CreateInspectorGUI();

            if (element != null)
            {
                // Decorate the InspectorElement based on the editor type (i.e. custom vs generic).
                if (targetEditor is GenericInspector)
                {
                    AddToClassList(uIEDefaultVariantUssClassName);
                    AddToClassList(uIEInspectorVariantUssClassName);

                    switch (targetEditor.inspectorMode)
                    {
                        case InspectorMode.Debug:
                            AddToClassList(debugVariantUssClassName);
                            break;
                        case InspectorMode.DebugInternal:
                            AddToClassList(debugInternalVariantUssClassName);
                            break;
                    }
                }
                else
                {
                    AddToClassList(uIECustomVariantUssClassName);

                    if (editor.UseDefaultMargins())
                        AddToClassList(uIEInspectorVariantUssClassName);
                }
            }

            return element;
        }

        VisualElement CreateInspectorElementUsingIMGUI(Editor targetEditor)
        {
            if (targetEditor is GenericInspector)
            {
                AddToClassList(iMGUIDefaultVariantUssClassName);

                switch (targetEditor.inspectorMode)
                {
                    case InspectorMode.Debug:
                        AddToClassList(debugVariantUssClassName);
                        break;
                    case InspectorMode.DebugInternal:
                        AddToClassList(debugInternalVariantUssClassName);
                        break;
                }
            }
            else
            {
                AddToClassList(iMGUICustomVariantUssClassName);
            }

            // Always try to re-use the imgui container if possible.
            var inspector = m_InspectorElement as IMGUIContainer ?? new IMGUIContainer();

            m_IgnoreOnInspectorGUIErrors = false;
            inspector.onGUIHandler = () =>
            {
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
                if (targetEditor.m_SerializedObject == null)
                {
                    targetEditor.Repaint();
                    m_IgnoreOnInspectorGUIErrors = true;
                }

                if ((targetEditor.target == null && !GenericInspector.ObjectIsMonoBehaviourOrScriptableObjectWithoutScript(targetEditor.target)) ||
                    !targetEditor.serializedObject.isValid)
                {
                    return;
                }

                EditorGUIUtility.ResetGUIState();
                GUI.color = playModeTintColor;

                using (new EditorGUI.DisabledScope(!targetEditor.IsEnabled() || !enabledInHierarchy))
                {
                    //set the current PropertyHandlerCache to the current editor
                    ScriptAttributeUtility.propertyHandlerCache = targetEditor.propertyHandlerCache;

                    var originalViewWidth = EditorGUIUtility.currentViewWidth;
                    var originalHierarchyMode = EditorGUIUtility.hierarchyMode;
                    var originalComparisonMode = EditorGUIUtility.comparisonViewMode;
                    EditorGUIUtility.hierarchyMode = true;
                    EditorGUIUtility.comparisonViewMode = comparisonViewMode;

                    var originalWideMode = SetWideModeForWidth(m_ContextWidthElement ?? inspector);

                    GUIStyle editorWrapper = (targetEditor.UseDefaultMargins() && targetEditor.CanBeExpandedViaAFoldoutWithoutUpdate()
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
                                var layoutCacheState = GUILayoutUtility.current.State;
                                try
                                {
                                    var rebuildOptimizedGUIBlocks = GetRebuildOptimizedGUIBlocks(targetEditor.target);
                                    rebuildOptimizedGUIBlocks |= targetEditor.isInspectorDirty;

                                    if (targetEditor.GetOptimizedGUIBlock(rebuildOptimizedGUIBlocks, visible, out var height))
                                    {
                                        var contentHeightRect = GUILayoutUtility.GetRect(0, visible ? height : 0);

                                        // Layout events are ignored in the optimized code path
                                        // The exception is when we are drawing a GenericInspector, they always use the optimized path and must therefore run at least one layout calculation in it
                                        if (Event.current.type == EventType.Layout && !(targetEditor is GenericInspector))
                                        {
                                            return;
                                        }

                                        InspectorWindowUtils.DrawAddedComponentBackground(contentHeightRect, targetEditor.targets);

                                        // Draw content
                                        if (visible)
                                        {
                                            GUI.changed = false;
                                            targetEditor.OnOptimizedInspectorGUI(contentHeightRect);
                                        }
                                    }
                                    else
                                    {
                                        InspectorWindowUtils.DrawAddedComponentBackground(contentRect, targetEditor.targets);
                                        using (new EditorPerformanceTracker(trackerName))
                                            targetEditor.OnInspectorGUI();
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
                                    GUILayoutUtility.current.CopyState(layoutCacheState);
                                }
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }
                    finally
                    {
                        if (GUI.changed)
                        {
                            // This forces a re-layout of all imgui containers in this inspector window.
                            // fixes part of case 1148706
                            var element = inspector.GetFirstAncestorOfType<EditorElement>();
                            if (element != null)
                                EditorElement.InvalidateIMGUILayouts(element.parent);
                        }
                        EditorGUIUtility.wideMode = originalWideMode;
                        EditorGUIUtility.hierarchyMode = originalHierarchyMode;
                        EditorGUIUtility.currentViewWidth = originalViewWidth;
                        EditorGUIUtility.comparisonViewMode = originalComparisonMode;
                    }
                }
            };

            inspector.style.overflow = Overflow.Visible;

            inspector.AddToClassList(iMGUIContainerUssClassName);

            AddToClassList(iMGUIInspectorVariantUssClassName);

            return inspector;
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
                EditorGUIUtility.currentViewWidth = inspectorWidth;
            }
            else
            {
                EditorGUIUtility.wideMode = true;
            }

            return previousWideMode;
        }

        bool GetRebuildOptimizedGUIBlocks(Object inspectedObject)
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
