// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddComponent;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEditorInternal;
using UnityEditorInternal.VersionControl;
using UnityEditor.UIElements;
using UnityEditor.StyleSheets;
using UnityEngine.Assertions.Comparers;
using UnityEngine.Scripting;

// includes specific to UIElements/IMGui
using UnityEngine.UIElements;
using UnityEngine.Profiling;

using Object = UnityEngine.Object;
using Overflow = UnityEngine.UIElements.Overflow;

using AssetImporterEditor = UnityEditor.Experimental.AssetImporters.AssetImporterEditor;
using UnityEditor.SceneManagement;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Inspector", useTypeNameAsIconName = true)]
    internal class InspectorWindow : EditorWindow, IHasCustomMenu
    {
        InspectorMode   m_InspectorMode = InspectorMode.Normal;

        internal InspectorMode inspectorMode
        {
            get { return m_InspectorMode; }
            set { SetMode(value); }
        }

        internal bool m_UseUIElementsDefaultInspector = false;

        static readonly List<InspectorWindow> m_AllInspectors = new List<InspectorWindow>();
        static bool s_AllOptimizedGUIBlocksNeedsRebuild;

        protected const float kBottomToolbarHeight = 21f;
        const float kAddComponentButtonHeight = 45f;
        internal const int kInspectorPaddingLeft = 8 + 10;
        internal const int kInspectorPaddingRight = 4;
        internal const int kInspectorPaddingTop = 4;
        internal const float kEditorElementPaddingBottom = 2f;

        const float k_MinAreaAbovePreview = 130;
        const float k_InspectorPreviewMinHeight = 130;
        const float k_InspectorPreviewMinTotalHeight = k_InspectorPreviewMinHeight + kBottomToolbarHeight;
        const int k_MinimumRootVisualHeight = 81;
        const int k_MinimumWindowWidth = 275;
        const int k_AutoScrollZoneHeight = 24;

        private const long delayRepaintWhilePlayingAnimation = 150; // Delay between repaints in milliseconds while playing animation
        private long s_LastUpdateWhilePlayingAnimation = 0;

        bool m_ResetKeyboardControl;
        public bool m_OpenAddComponentMenu = false;
        protected ActiveEditorTracker m_Tracker;

        [SerializeField]
        protected List<Object> m_ObjectsLockedBeforeSerialization = new List<Object>();

        [SerializeField]
        protected List<int> m_InstanceIDsLockedBeforeSerialization = new List<int>();

        [SerializeField]
        EditorGUIUtility.EditorLockTrackerWithActiveEditorTracker m_LockTracker = new EditorGUIUtility.EditorLockTrackerWithActiveEditorTracker();

        internal Editor lastInteractedEditor { get; set; }
        int m_LastInitialEditorInstanceID;
        Component[] m_ComponentsInPrefabSource;
        HashSet<Component> m_RemovedComponents;

        [SerializeField]
        PreviewResizer m_PreviewResizer = new PreviewResizer();
        [SerializeField]
        PreviewWindow m_PreviewWindow;

        LabelGUI m_LabelGUI = new LabelGUI();
        AssetBundleNameGUI m_AssetBundleNameGUI = new AssetBundleNameGUI();

        TypeSelectionList m_TypeSelectionList = null;

        private double m_lastRenderedTime;
        private List<IPreviewable> m_Previews;
        private Dictionary<Type, List<Type>> m_PreviewableTypes;
        private IPreviewable m_SelectedPreview;

        internal HashSet<int> editorsWithImportedObjectLabel { get; } = new HashSet<int>();

        internal EditorDragging editorDragging { get; }

        IMGUIContainer m_TrackerResetter;

        VisualElement m_EditorsElement;
        VisualElement editorsElement => m_EditorsElement ?? (m_EditorsElement = FindVisualElementInTreeByClassName(s_EditorListClassName));
        VisualElement m_RemovedPrefabComponentsElement;

        VisualElement m_PreviewAndLabelElement;
        VisualElement previewAndLabelElement => m_PreviewAndLabelElement ?? (m_PreviewAndLabelElement = FindVisualElementInTreeByClassName(s_FooterInfoClassName));

        VisualElement m_VersionControlElement;
        VisualElement versionControlElement => m_VersionControlElement ?? (m_VersionControlElement = FindVisualElementInTreeByClassName(s_HeaderInfoClassName));

        VisualElement m_MultiEditLabel;

        ScrollView m_ScrollView;
        [SerializeField] int m_LastInspectedObjectInstanceID = -1;
        [SerializeField] float m_LastVerticalScrollValue = 0;

        VisualElement FindVisualElementInTreeByClassName(string elementClassName)
        {
            var element = rootVisualElement.Q(className: elementClassName);
            if (element == null)
            {
                LoadVisualTreeFromUxml();
                element = rootVisualElement.Q(className: elementClassName);
            }

            return element;
        }


        internal static class Styles
        {
            public static readonly GUIStyle preToolbar = "preToolbar";
            public static readonly GUIStyle preToolbar2 = "preToolbar2";
            public static readonly GUIStyle preToolbarLabel = "ToolbarBoldLabel";
            public static readonly GUIStyle preDropDown = "preDropDown";
            public static readonly GUIStyle dragHandle = "RL DragHandle";
            public static readonly GUIStyle lockButton = "IN LockButton";
            public static readonly GUIStyle insertionMarker = "InsertionMarker";
            public static readonly GUIContent preTitle = EditorGUIUtility.TrTextContent("Preview");
            public static readonly GUIContent labelTitle = EditorGUIUtility.TrTextContent("Asset Labels");
            public static readonly GUIContent addComponentLabel = EditorGUIUtility.TrTextContent("Add Component");
            public static GUIStyle preBackground = "preBackground";
            public static GUIStyle footer = "IN Footer";
            public static GUIStyle preMargins = new GUIStyle() {margin = new RectOffset(0, 0, 0, 4)};
            public static GUIStyle preOptionsButton = new GUIStyle(EditorStyles.toolbarButtonRight) { padding = new RectOffset(), contentOffset = new Vector2(1, 0) };
            public static GUIStyle addComponentArea = EditorStyles.inspectorTitlebar;
            public static GUIStyle addComponentButtonStyle = "AC Button";
            public static readonly GUIContent menuIcon = EditorGUIUtility.TrIconContent("_Menu");
            public static GUIStyle previewMiniLabel = EditorStyles.whiteMiniLabel;
            public static GUIStyle typeSelection = "IN TypeSelection";

            public static readonly GUIContent vcsCheckoutHint = EditorGUIUtility.TrTextContent("Under Version Control\nCheck out this asset in order to make changes.", EditorGUIUtility.GetHelpIcon(MessageType.Info));
            public static readonly GUIContent vcsNotConnected = EditorGUIUtility.TrTextContent("VCS ({0}) is not connected");
            public static readonly GUIContent vcsOffline = EditorGUIUtility.TrTextContent("Work Offline option is active");
            public static readonly GUIContent vcsCheckout = EditorGUIUtility.TrTextContent("Check Out");
            public static readonly GUIContent vcsAdd = EditorGUIUtility.TrTextContent("Add");
            public static readonly GUIContent vcsLock = EditorGUIUtility.TrTextContent("Lock");
            public static readonly GUIContent vcsUnlock = EditorGUIUtility.TrTextContent("Unlock");
            public static readonly GUIContent vcsSubmit = EditorGUIUtility.TrTextContent("Submit");
            public static readonly GUIStyle vcsButtonStyle = EditorStyles.miniButton;
            public static readonly string objectDisabledModuleWarningFormat = L10n.Tr(
                "The built-in package '{0}', which implements this component type, has been disabled in Package Manager. This object will be removed in play mode and from any builds you make."
            );
            public static readonly string objectDisabledModuleWithDependencyWarningFormat = L10n.Tr(
                "The built-in package '{0}', which is required by the package '{1}', which implements this component type, has been disabled in Package Manager. This object will be removed in play mode and from any builds you make."
            );

            public static SVC<float> lineSeparatorOffset = new SVC<float>("AC-Button", "--separator-line-top-offset");
            public static SVC<Color> lineSeparatorColor = new SVC<Color>("--theme-line-separator-color", Color.red);
        }


        internal static readonly string s_MultiEditClassName = "unity-inspector-no-multi-edit-warning";
        internal static readonly string s_MultiEditLabelClassName = "unity-inspector-no-multi-edit-warning__label";
        internal static readonly string s_ScrollViewClassName = "unity-inspector-root-scrollview";
        internal static readonly string s_EditorListClassName = "unity-inspector-editors-list";
        internal static readonly string s_AddComponentClassName = "unity-inspector-add-component-button";
        internal static readonly string s_HeaderInfoClassName = "unity-inspector-header-info";
        internal static readonly string s_FooterInfoClassName = "unity-inspector-footer-info";
        internal static readonly string s_MainContainerClassName = "unity-inspector-main-container";

        internal InspectorWindow()
        {
            editorDragging = new EditorDragging(this);
            minSize = new Vector2(k_MinimumWindowWidth, minSize.y);
        }

        void Awake()
        {
            AddInspectorWindow(this);
        }

        protected virtual void OnDestroy()
        {
            if (m_PreviewWindow != null)
                m_PreviewWindow.Close();
            if (m_Tracker != null && !m_Tracker.Equals(ActiveEditorTracker.sharedTracker))
                m_Tracker.Destroy();
            if (m_TrackerResetter != null)
            {
                m_TrackerResetter.Dispose();
                m_TrackerResetter = null;
            }
        }

        protected virtual void OnEnable()
        {
            RefreshTitle();
            AddInspectorWindow(this);

            LoadVisualTreeFromUxml();

            m_PreviewResizer.localFrame = true;
            m_PreviewResizer.Init("InspectorPreview");
            m_LabelGUI.OnEnable();

            CreateTracker();

            RestoreLockStateFromSerializedData();

            if (m_LockTracker == null)
            {
                m_LockTracker = new EditorGUIUtility.EditorLockTrackerWithActiveEditorTracker();
            }

            m_LockTracker.tracker = tracker;
            m_LockTracker.lockStateChanged.AddListener(LockStateChanged);



            EditorApplication.projectWasLoaded += OnProjectWasLoaded;

            m_FirstInitialize = true;
        }

        private void LoadVisualTreeFromUxml()
        {
            var tpl = EditorGUIUtility.Load("UXML/InspectorWindow/InspectorWindow.uxml") as VisualTreeAsset;
            var container = tpl.CloneTree();
            container.AddToClassList(s_MainContainerClassName);
            rootVisualElement.hierarchy.Add(container);
            m_ScrollView = container.Q<ScrollView>();

            var multiContainer = rootVisualElement.Q(className: s_MultiEditClassName);
            multiContainer.Query<TextElement>().ForEach((label) => label.text = L10n.Tr(label.text));
            multiContainer.RemoveFromHierarchy();

            m_MultiEditLabel = multiContainer;

            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            rootVisualElement.AddStyleSheetPath("StyleSheets/InspectorWindow/InspectorWindow.uss");
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            if (m_PreviewResizer.GetExpanded())
            {
                if (previewAndLabelElement.layout.height > 0 &&
                    rootVisualElement.layout.height <= k_MinimumRootVisualHeight +  m_PreviewResizer.containerMinimumHeightExpanded)
                {
                    m_PreviewResizer.SetExpanded(false);
                }
            }
            RestoreVerticalScrollIfNeeded();
        }

        private void OnProjectWasLoaded()
        {
            // EditorApplication.projectWasLoaded, which acalls this, fires after OnEnabled
            // therefore the logic in OnEnabled already tried to de-serialize the locked opbjects, including those it only had InstanceIDs of
            // This needs to get fixed here as the InstanceIDs have been reshuffled with the new session and could resolving to random Objects
            if (m_InstanceIDsLockedBeforeSerialization.Count > 0)
            {
                // Game objects will have new instanceIDs in a new Unity session, so take out all objects that where reconstructed from InstanceIDs
                for (int i = m_InstanceIDsLockedBeforeSerialization.Count - 1; i >= 0; i--)
                {
                    for (int j = m_ObjectsLockedBeforeSerialization.Count - 1; j >= 0; j--)
                    {
                        if (m_ObjectsLockedBeforeSerialization[j] == null || m_ObjectsLockedBeforeSerialization[j].GetInstanceID() == m_InstanceIDsLockedBeforeSerialization[i])
                        {
                            m_ObjectsLockedBeforeSerialization.RemoveAt(j);
                            break;
                        }
                    }
                }
                m_InstanceIDsLockedBeforeSerialization.Clear();
                RestoreLockStateFromSerializedData();
            }
        }

        protected virtual void OnDisable()
        {
            // save vertical scroll position
            m_LastInspectedObjectInstanceID = GetInspectedObject()?.GetInstanceID() ?? -1;
            m_LastVerticalScrollValue = m_ScrollView?.verticalScroller.value ?? 0;

            RemoveInspectorWindow(this);
            m_LockTracker?.lockStateChanged.RemoveListener(LockStateChanged);

            EditorApplication.projectWasLoaded -= OnProjectWasLoaded;
        }

        void OnLostFocus()
        {
            m_LabelGUI.OnLostFocus();
        }

        static internal void RepaintAllInspectors()
        {
            foreach (var win in m_AllInspectors)
                win.Repaint();
        }

        internal static List<InspectorWindow> GetInspectors()
        {
            return m_AllInspectors;
        }

        [UsedByNativeCode]
        private static void RedrawFromNative()
        {
            foreach (var inspector in m_AllInspectors)
            {
                inspector.RebuildContentsContainers();
            }
        }

        internal static InspectorWindow[] GetAllInspectorWindows()
        {
            return m_AllInspectors.ToArray();
        }

        private void OnInspectorUpdate()
        {
            // Check if scripts have changed without calling set dirty
            tracker.VerifyModifiedMonoBehaviours();

            if (!tracker.isDirty || !ReadyToRepaint())
            {
                return;
            }

            Repaint();
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Normal"), m_InspectorMode == InspectorMode.Normal, SetNormal);
            menu.AddItem(EditorGUIUtility.TrTextContent("Debug"), m_InspectorMode == InspectorMode.Debug, SetDebug);

            if (Unsupported.IsDeveloperMode())
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Debug-Internal"), m_InspectorMode == InspectorMode.DebugInternal, SetDebugInternal);
                menu.AddItem(EditorGUIUtility.TrTextContent("Use UIElements Default Inspector"), m_UseUIElementsDefaultInspector, SetUseUIEDefaultInspector);
            }

            menu.AddSeparator(String.Empty);

            if (IsAnyComponentCollapsed())
                menu.AddItem(EditorGUIUtility.TrTextContent("Expand All Components"), false, ExpandAllComponents);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Expand All Components"));

            if (IsAnyComponentExpanded())
                menu.AddItem(EditorGUIUtility.TrTextContent("Collapse All Components"), false, CollapseAllComponents);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Collapse All Components"));

            menu.AddSeparator(String.Empty);

            m_LockTracker.AddItemsToMenu(menu);

            if (m_Tracker != null)
            {
                foreach (var editor in m_Tracker.activeEditors)
                {
                    var menuContainer = editor as IHasCustomMenu;
                    if (menuContainer != null)
                    {
                        menuContainer.AddItemsToMenu(menu);
                    }
                }
            }
        }

        void RefreshTitle()
        {
            string iconName = "UnityEditor.InspectorWindow";
            if (m_InspectorMode == InspectorMode.Normal)
                titleContent = EditorGUIUtility.TrTextContentWithIcon("Inspector", iconName);
            else
                titleContent = EditorGUIUtility.TrTextContentWithIcon("Debug", iconName);
        }

        void SetUseUIEDefaultInspector()
        {
            m_UseUIElementsDefaultInspector = !m_UseUIElementsDefaultInspector;
            // Clear the editors Element so that a real rebuild is done
            editorsElement.Clear();
            RebuildContentsContainers();
        }

        void SetMode(InspectorMode mode)
        {
            if (m_InspectorMode != mode)
            {
                m_InspectorMode = mode;
                RefreshTitle();
                // Clear the editors Element so that a real rebuild is done
                editorsElement.Clear();
                tracker.inspectorMode = mode;
                m_ResetKeyboardControl = true;
            }
        }

        void SetDebug()
        {
            inspectorMode = InspectorMode.Debug;
        }

        void SetNormal()
        {
            inspectorMode = InspectorMode.Normal;
        }

        void SetDebugInternal()
        {
            inspectorMode = InspectorMode.DebugInternal;
        }

        internal void ExpandAllComponents()
        {
            var editors = this.tracker.activeEditors;
            for (int i = 1; i < editors.Length; i++)
            {
                this.tracker.SetVisible(i, 1);
            }
        }

        private bool IsAnyComponentCollapsed()
        {
            if (Selection.activeGameObject == null)
                return false;                               //If the selection is not a gameobject then disable the option.

            var editors = this.tracker.activeEditors;
            for (int i = 1; i < editors.Length; i++)
            {
                if (this.tracker.GetVisible(i) == 0)
                    return true;
            }
            return false;
        }

        internal void CollapseAllComponents()
        {
            var editors = this.tracker.activeEditors;
            for (int i = 1; i < editors.Length; i++)
            {
                this.tracker.SetVisible(i, 0);
            }
        }

        private bool IsAnyComponentExpanded()
        {
            if (Selection.activeGameObject == null)
                return false;                               //If the selection is not a gameobject then disable the option.

            var editors = this.tracker.activeEditors;
            for (int i = 1; i < editors.Length; i++)
            {
                if (this.tracker.GetVisible(i) == 1)
                    return true;
            }
            return false;
        }

        public bool isLocked
        {
            get
            {
                //this makes sure the getter for InspectorWindow.tracker gets called and creates an ActiveEditorTracker if needed
                m_LockTracker.tracker = tracker;
                return m_LockTracker.isLocked;
            }
            set
            {
                //this makes sure the getter for InspectorWindow.tracker gets called and creates an ActiveEditorTracker if needed
                m_LockTracker.tracker = tracker;
                m_LockTracker.isLocked = value;
            }
        }

        internal ActiveEditorTracker tracker
        {
            get
            {
                CreateTracker();
                return m_Tracker;
            }
        }

        protected virtual void CreateTracker()
        {
            if (m_Tracker != null)
            {
                // Ensure that inspector mode
                // This shouldn't be necessary but there are some non-reproducable bugs objects showing up as not able to multi-edit
                // because this state goes out of sync.
                m_Tracker.inspectorMode = m_InspectorMode;
                return;
            }

            var sharedTracker = ActiveEditorTracker.sharedTracker;
            bool sharedTrackerInUse = m_AllInspectors.Any(i => i.m_Tracker != null && i.m_Tracker.Equals(sharedTracker));
            m_Tracker = sharedTrackerInUse ? new ActiveEditorTracker() : ActiveEditorTracker.sharedTracker;
            m_Tracker.inspectorMode = m_InspectorMode;
            m_Tracker.RebuildIfNecessary();
        }

        void OnTrackerRebuilt()
        {
            ExtractPrefabComponents();
        }

        void ExtractPrefabComponents()
        {
            m_LastInitialEditorInstanceID = m_Tracker.activeEditors[0].GetInstanceID();

            m_ComponentsInPrefabSource = null;
            if (m_RemovedComponents == null)
                m_RemovedComponents = new HashSet<Component>();
            m_RemovedComponents.Clear();

            if (m_Tracker.activeEditors.Length == 0)
                return;
            if (m_Tracker.activeEditors[0].targets.Length != 1)
                return;

            GameObject go = m_Tracker.activeEditors[0].target as GameObject;
            if (go == null && m_Tracker.activeEditors[0] is PrefabImporterEditor)
                go = m_Tracker.activeEditors[1].target as GameObject;
            if (go == null)
                return;

            GameObject sourceGo = PrefabUtility.GetCorrespondingConnectedObjectFromSource(go);
            if (sourceGo == null)
                return;

            m_ComponentsInPrefabSource = sourceGo.GetComponents<Component>();
            var removedComponentsList = PrefabOverridesUtility.GetRemovedComponentsForSingleGameObject(go);
            for (int i = 0; i < removedComponentsList.Count; i++)
            {
                m_RemovedComponents.Add(removedComponentsList[i].assetComponent);
            }
        }

        protected virtual void CreatePreviewables()
        {
            if (m_Previews != null)
                return;

            m_Previews = new List<IPreviewable>();

            var activeEditors = tracker?.activeEditors;
            if (activeEditors == null || activeEditors.Length == 0)
                return;

            foreach (var editor in activeEditors)
            {
                IEnumerable<IPreviewable> previews = GetPreviewsForType(editor);
                foreach (var preview in previews)
                {
                    m_Previews.Add(preview);
                }
            }
        }

        private Dictionary<Type, List<Type>> GetPreviewableTypes()
        {
            // We initialize this list once per InspectorWindow, instead of globally.
            // This means that if the user is debugging an IPreviewable structure,
            // the InspectorWindow can be closed and reopened to refresh this list.
            //
            if (m_PreviewableTypes == null)
            {
                InspectorWindowUtils.GetPreviewableTypes(out m_PreviewableTypes);
            }

            return m_PreviewableTypes;
        }

        private IEnumerable<IPreviewable> GetPreviewsForType(Editor editor)
        {
            List<IPreviewable> previews = new List<IPreviewable>();

            // Retrieve the type we are looking for.
            if (editor == null || editor.target == null)
                return previews;
            Type targetType = editor.target.GetType();
            if (!GetPreviewableTypes().ContainsKey(targetType))
                return previews;

            var previewerList = GetPreviewableTypes()[targetType];
            if (previewerList == null)
                return previews;

            foreach (var previewerType in previewerList)
            {
                var preview = Activator.CreateInstance(previewerType) as IPreviewable;
                preview.Initialize(editor.targets);
                previews.Add(preview);
            }

            return previews;
        }

        protected virtual void ShowButton(Rect r)
        {
            m_LockTracker.ShowButton(r, Styles.lockButton);
        }

        private void LockStateChanged(bool lockeState)
        {
            if (lockeState)
            {
                PrepareLockedObjectsForSerialization();
            }
            else
            {
                ClearSerializedLockedObjects();
            }

            tracker.RebuildIfNecessary();

        }

        private void ClearTrackerDirtyOnRepaint()
        {
            if (Event.current.type == EventType.Repaint)
            {
                tracker.ClearDirty();
            }
        }

        private bool m_TrackerResetInserted;

        internal IMGUIContainer CreateIMGUIContainer(Action onGUIHandler, string name = null)
        {
            IMGUIContainer result = null;
            if (m_TrackerResetInserted)
            {
                result = new IMGUIContainer(onGUIHandler);
            }
            else
            {
                m_TrackerResetInserted = true;
                result = new IMGUIContainer(() =>
                {
                    ClearTrackerDirtyOnRepaint();
                    onGUIHandler();
                });
            }

            result.style.overflow = Overflow.Visible;
            if (name != null)
            {
                result.name = name;
            }

            return result;
        }

        internal virtual void RebuildContentsContainers()
        {
            Profiler.BeginSample("InspectorWindow.RebuildContentsContainers()");
            m_Previews = null;
            m_SelectedPreview = null;
            m_TypeSelectionList = null;
            m_FirstInitialize = false;
            editorsWithImportedObjectLabel.Clear();
            m_LastInitialEditorInstanceID = 0;

            if (m_RemovedPrefabComponentsElement != null)
            {
                m_RemovedPrefabComponentsElement.RemoveFromHierarchy();
                m_RemovedPrefabComponentsElement = null;
            }

            FlushAllOptimizedGUIBlocksIfNeeded();

            ResetKeyboardControl();

            var addComponentButton = rootVisualElement.Q(className: s_AddComponentClassName);
            addComponentButton.Clear();
            versionControlElement.Clear();
            previewAndLabelElement.Clear();

            if (m_TrackerResetter == null)
            {
                m_TrackerResetInserted = false;
                m_TrackerResetter = CreateIMGUIContainer(() => {}, "activeEditorTrackerResetter");
                rootVisualElement.Insert(0, m_TrackerResetter);
            }

            Editor[] editors = tracker.activeEditors;

            if (editors.Any())
            {
                Profiler.BeginSample("InspectorWindow.RebuildContentsContainers()::versionControlElement");
                versionControlElement.Add(CreateIMGUIContainer(
                    () => VersionControlBar(InspectorWindowUtils.GetFirstNonImportInspectorEditor(editors))));
                Profiler.EndSample();
            }


            Profiler.BeginSample("InspectorWindow.DrawEditors()");
            DrawEditors(editors);
            Profiler.EndSample();

            var labelMustBeAdded = m_MultiEditLabel.parent != editorsElement;

            // The PrefabImporterEditor can hide its imported objects if it detects missing scripts. In this case
            // do not add the multi editing warning
            var assetImporter = GetAssetImporter(editors);
            if (assetImporter != null && !assetImporter.showImportedObject)
                labelMustBeAdded = false;

            if (tracker.hasComponentsWhichCannotBeMultiEdited)
            {
                Profiler.BeginSample("InspectorWindow.RebuildContentsContainers()::hasComponentsWhichCannotBeMultiEdited");
                if (editors.Length == 0 && !tracker.isLocked && Selection.objects.Length > 0)
                {
                    editorsElement.Add(CreateIMGUIContainer(DrawSelectionPickerList));
                }
                else
                {
                    if (labelMustBeAdded)
                    {
                        editorsElement.Add(m_MultiEditLabel);
                    }
                }
                Profiler.EndSample();
            }
            else
            {
                m_MultiEditLabel.RemoveFromHierarchy();
            }

            if (editors.Any() && RootEditorUtils.SupportsAddComponent(editors))
            {
                Profiler.BeginSample("InspectorWindow.RebuildContentsContainers()::addComponentButton");
                addComponentButton.Add(CreateIMGUIContainer(() =>
                {
                    EditorGUI.indentLevel = 0;
                    AddComponentButton(editors);
                }));
                Profiler.EndSample();
            }

            if (editors.Any())
            {
                Profiler.BeginSample("InspectorWindow.RebuildContentsContainers()::previewAndLabelElement");
                var previewAndLabelsContainer = CreateIMGUIContainer(DrawPreviewAndLabels, "preview-container");
                m_PreviewResizer.SetContainer(previewAndLabelsContainer, kBottomToolbarHeight);
                previewAndLabelElement.Add(previewAndLabelsContainer);
                Profiler.EndSample();
            }

            rootVisualElement.MarkDirtyRepaint();

            ScriptAttributeUtility.ClearGlobalCache();

            rootVisualElement.RegisterCallback<DragUpdatedEvent>(DragOverBottomArea);
            rootVisualElement.RegisterCallback<DragPerformEvent>(DragPerformInBottomArea);

            Repaint();
            Profiler.EndSample();
        }

        private Rect bottomAreaDropRectangle
        {
            get
            {
                var worldEditorRect = editorsElement.LocalToWorld(editorsElement.rect);
                var worldRootRect = rootVisualElement.LocalToWorld(rootVisualElement.rect);
                return new Rect(
                    worldEditorRect.x,
                    worldEditorRect.y + worldEditorRect.height,
                    worldEditorRect.width,
                    worldRootRect.y + worldRootRect.height - worldEditorRect.height - worldEditorRect.y);
            }
        }

        void DragOverBottomArea(DragUpdatedEvent dragUpdatedEvent)
        {
            if (DragAndDrop.objectReferences.Any())
            {
                if (editorsElement.ContainsPoint(editorsElement.WorldToLocal(dragUpdatedEvent.mousePosition)))
                {
                    if (m_ScrollView != null)
                    {
                        // implement auto-scroll for easier component drag'n'drop,
                        // we define a zone of height = k_AutoScrollZoneHeight
                        // at the top/bottom of the scrollView viewport,
                        // while dragging, when the mouse moves in these zones,
                        // we automatically scroll up/down
                        var localDragPosition = m_ScrollView.contentViewport.WorldToLocal(dragUpdatedEvent.mousePosition);

                        if (localDragPosition.y < k_AutoScrollZoneHeight)
                            m_ScrollView.verticalScroller.ScrollPageUp();
                        else if (localDragPosition.y > m_ScrollView.contentViewport.rect.height - k_AutoScrollZoneHeight)
                            m_ScrollView.verticalScroller.ScrollPageDown();
                    }

                    return;
                }

                var lastChild = editorsElement.Children().LastOrDefault();
                if (lastChild == null)
                {
                    return;
                }

                editorDragging.HandleDraggingInBottomArea(tracker.activeEditors, bottomAreaDropRectangle, lastChild.layout);
            }
        }

        void DragPerformInBottomArea(DragPerformEvent dragPerformedEvent)
        {
            if (editorsElement.ContainsPoint(editorsElement.WorldToLocal(dragPerformedEvent.mousePosition)))
            {
                return;
            }

            var lastChild = editorsElement.Children().LastOrDefault();
            if (lastChild == null)
            {
                return;
            }

            editorDragging.HandleDragPerformInBottomArea(tracker.activeEditors, bottomAreaDropRectangle, lastChild.layout);
        }

        protected bool m_FirstInitialize;
        protected virtual void OnGUI()
        {
            if (m_FirstInitialize)
            {
                RebuildContentsContainers();
            }
        }

        internal virtual Editor GetLastInteractedEditor()
        {
            return lastInteractedEditor;
        }

        internal IPreviewable GetEditorThatControlsPreview(IPreviewable[] editors)
        {
            if (editors.Length == 0)
                return null;

            if (m_SelectedPreview != null)
            {
                return m_SelectedPreview;
            }

            // Find last interacted editor, if not found check if we had an editor of similar type,
            // if not found use first editor that can show a preview otherwise return null.

            IPreviewable lastInteractedEditor = GetLastInteractedEditor();
            Type lastType = lastInteractedEditor?.GetType();

            IPreviewable firstEditorThatHasPreview = null;
            IPreviewable similarEditorAsLast = null;
            foreach (IPreviewable e in editors)
            {
                if (e == null || e.target == null)
                    continue;

                // If target is an asset, but not the same asset as the asset
                // of the first editor, then ignore it. This will prevent showing
                // preview of materials attached to a GameObject but should cover
                // future use cases as well.
                if (EditorUtility.IsPersistent(e.target) &&
                    AssetDatabase.GetAssetPath(e.target) != AssetDatabase.GetAssetPath(editors[0].target))
                    continue;

                // If main editor is an asset importer editor and this is an editor of the imported object, ignore.
                if (editors[0] is AssetImporterEditor && !(e is AssetImporterEditor))
                    continue;

                if (e.HasPreviewGUI())
                {
                    if (e == lastInteractedEditor)
                        return e;

                    if (similarEditorAsLast == null && e.GetType() == lastType)
                        similarEditorAsLast = e;

                    if (firstEditorThatHasPreview == null)
                        firstEditorThatHasPreview = e;
                }
            }

            if (similarEditorAsLast != null)
                return similarEditorAsLast;

            if (firstEditorThatHasPreview != null)
                return firstEditorThatHasPreview;

            // Found no valid editor
            return null;
        }

        internal IPreviewable[] GetEditorsWithPreviews(Editor[] editors)
        {
            IList<IPreviewable> editorsWithPreview = new List<IPreviewable>();

            int i = -1;
            foreach (Editor e in editors)
            {
                ++i;
                if (e.target == null)
                    continue;

                // If target is an asset, but not the same asset as the asset
                // of the first editor, then ignore it. This will prevent showing
                // preview of materials attached to a GameObject but should cover
                // future use cases as well.
                if (EditorUtility.IsPersistent(e.target) &&
                    AssetDatabase.GetAssetPath(e.target) != AssetDatabase.GetAssetPath(editors[0].target))
                    continue;

                if (!EditorUtility.IsPersistent(editors[0].target) && EditorUtility.IsPersistent(e.target))
                    continue;

                if (ShouldCullEditor(editors, i))
                    continue;

                // If main editor is an asset importer editor and this is an editor of the imported object, ignore.
                if (editors[0] is AssetImporterEditor && !(e is AssetImporterEditor))
                    continue;

                if (e.HasPreviewGUI())
                {
                    editorsWithPreview.Add(e);
                }
            }

            foreach (var previewable in m_Previews)
            {
                if (previewable.HasPreviewGUI())
                    editorsWithPreview.Add(previewable);
            }

            return editorsWithPreview.ToArray();
        }

        internal Object GetInspectedObject()
        {
            Editor editor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(tracker.activeEditors);
            if (editor == null)
                return null;
            return editor.target;
        }

        private static void MoveFocusOnKeyPress()
        {
            var key = Event.current.keyCode;

            if (Event.current.type != EventType.KeyDown || (key != KeyCode.DownArrow && key != KeyCode.UpArrow && key != KeyCode.Tab))
                return;

            if (key != KeyCode.Tab)
                EditorGUIUtility.MoveFocusAndScroll(key == KeyCode.DownArrow);
            else
                EditorGUIUtility.ScrollForTabbing(!Event.current.shift);

            Event.current.Use();
        }

        private void ResetKeyboardControl()
        {
            if (m_ResetKeyboardControl)
            {
                GUIUtility.keyboardControl = 0;
                m_ResetKeyboardControl = false;
            }
        }

        static bool HasLabel(Object target)
        {
            return HasLabel(target, AssetDatabase.GetAssetPath(target));
        }

        static bool HasLabel(Object target, string assetPath)
        {
            return EditorUtility.IsPersistent(target) && assetPath.StartsWith("assets", StringComparison.OrdinalIgnoreCase);
        }

        private Object[] GetInspectedAssets()
        {
            // We use this technique to support locking of the inspector. An inspector locks via an editor, so we need to use an editor to get the selection
            Editor assetEditor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(tracker.activeEditors);
            if (assetEditor != null && assetEditor.targets.Length == 1)
            {
                string assetPath = AssetDatabase.GetAssetPath(assetEditor.target);
                if (HasLabel(assetEditor.target, assetPath) && !Directory.Exists(assetPath))
                    return assetEditor.targets;
            }

            // This is used if more than one asset is selected
            // Ideally the tracker should be refactored to track not just editors but also the selection that caused them, so we wouldn't need this
            return Selection.objects.Where(HasLabel).ToArray();
        }

        float m_PreviousFooterHeight = -1;
        bool m_PreviousPreviewExpandedState;
        bool m_HasPreview;

        private void DrawPreviewAndLabels()
        {
            CreatePreviewables();

            if (m_PreviewWindow && Event.current.type == EventType.Repaint)
                m_PreviewWindow.Repaint();

            IPreviewable[] editorsWithPreviews = GetEditorsWithPreviews(tracker.activeEditors);
            IPreviewable previewEditor = GetEditorThatControlsPreview(editorsWithPreviews);

            // Do we have a preview?
            m_HasPreview = previewEditor != null && previewEditor.HasPreviewGUI() && m_PreviewWindow == null;

            m_PreviewResizer.containerMinimumHeightExpanded = m_HasPreview ? k_InspectorPreviewMinTotalHeight : 0;

            Object[] assets = GetInspectedAssets();
            bool hasLabels = assets.Length > 0;
            bool hasBundleName = assets.Any(a => !(a is MonoScript) && AssetDatabase.IsMainAsset(a));

            if (!m_HasPreview && !hasLabels)
                return;

            Event evt = Event.current;

            // Preview / Asset Labels toolbar
            Rect rect = EditorGUILayout.BeginHorizontal(GUIContent.none, Styles.preToolbar, GUILayout.Height(kBottomToolbarHeight));
            Rect dragRect;
            Rect dragIconRect = new Rect();
            const float dragPadding = 3f;
            const float minDragWidth = 20f;
            {
                GUILayout.FlexibleSpace();
                dragRect = GUILayoutUtility.GetLastRect();

                // The label rect is also needed to know which area should be draggable.
                GUIContent title;
                if (m_HasPreview)
                {
                    GUIContent userDefinedTitle = previewEditor.GetPreviewTitle();
                    title = userDefinedTitle ?? Styles.preTitle;
                }
                else
                {
                    title = Styles.labelTitle;
                }

                dragIconRect.x = dragRect.x + dragPadding;
                dragIconRect.y = dragRect.y + (kBottomToolbarHeight - Styles.dragHandle.fixedHeight) / 2;
                dragIconRect.width = dragRect.width - dragPadding * 2;
                dragIconRect.height = Styles.dragHandle.fixedHeight;

                //If we have more than one component with Previews, show a DropDown menu.
                if (editorsWithPreviews.Length > 1)
                {
                    Vector2 foldoutSize = Styles.preDropDown.CalcSize(title);
                    float maxFoldoutWidth = (dragIconRect.xMax - dragRect.xMin) - dragPadding - minDragWidth;
                    float foldoutWidth = Mathf.Min(maxFoldoutWidth, foldoutSize.x);
                    Rect foldoutRect = new Rect(dragRect.x, dragRect.y, foldoutWidth, foldoutSize.y);
                    dragRect.xMin += foldoutWidth;
                    dragIconRect.xMin += foldoutWidth;

                    GUIContent[] panelOptions = new GUIContent[editorsWithPreviews.Length];
                    int selectedPreview = -1;
                    for (int index = 0; index < editorsWithPreviews.Length; index++)
                    {
                        IPreviewable currentEditor = editorsWithPreviews[index];
                        GUIContent previewTitle = currentEditor.GetPreviewTitle() ?? Styles.preTitle;

                        string fullTitle;
                        if (previewTitle == Styles.preTitle)
                        {
                            string componentTitle = ObjectNames.GetTypeName(currentEditor.target);
                            if (NativeClassExtensionUtilities.ExtendsANativeType(currentEditor.target))
                            {
                                componentTitle = MonoScript.FromScriptedObject(currentEditor.target).GetClass()
                                    .Name;
                            }

                            fullTitle = previewTitle.text + " - " + componentTitle;
                        }
                        else
                        {
                            fullTitle = previewTitle.text;
                        }

                        panelOptions[index] = new GUIContent(fullTitle);
                        if (editorsWithPreviews[index] == previewEditor)
                        {
                            selectedPreview = index;
                        }
                    }

                    if (GUI.Button(foldoutRect, title, Styles.preDropDown))
                    {
                        EditorUtility.DisplayCustomMenu(foldoutRect, panelOptions, selectedPreview,
                            OnPreviewSelected, editorsWithPreviews);
                    }
                }
                else
                {
                    float maxLabelWidth = (dragIconRect.xMax - dragRect.xMin) - dragPadding - minDragWidth;
                    float labelWidth = Mathf.Min(maxLabelWidth, Styles.preToolbar2.CalcSize(title).x);
                    Rect labelRect = new Rect(dragRect.x, dragRect.y, labelWidth, dragRect.height);

                    dragIconRect.xMin = labelRect.xMax + dragPadding;

                    GUI.Label(labelRect, title, Styles.preToolbarLabel);
                }

                if (m_HasPreview && Event.current.type == EventType.Repaint)
                {
                    // workaround: To properly center the image because it already has a 1px bottom padding
                    dragIconRect.y += 1;
                    Styles.dragHandle.Draw(dragIconRect, GUIContent.none, false, false, false, false);
                }

                if (m_HasPreview && m_PreviewResizer.GetExpandedBeforeDragging())
                    previewEditor.OnPreviewSettings();

                if (m_HasPreview || m_PreviewWindow != null)
                {
                    if (EditorGUILayout.DropdownButton(Styles.menuIcon, FocusType.Passive, Styles.preOptionsButton))
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(
                            EditorGUIUtility.TrTextContent(m_PreviewWindow == null
                                ? "Convert to Floating Window"
                                : "Dock Preview to Inspector"), false,
                            () =>
                            {
                                if (m_PreviewWindow == null)
                                    DetachPreview(false);
                                else
                                    m_PreviewWindow.Close();
                            });
                        menu.AddItem(
                            EditorGUIUtility.TrTextContent(m_PreviewResizer.GetExpanded()
                                ? "Minimize in Inspector"
                                : "Restore in Inspector"), false,
                            () =>
                            {
                                m_PreviewResizer.SetExpanded(position, k_InspectorPreviewMinTotalHeight,
                                    k_MinAreaAbovePreview, kBottomToolbarHeight, dragRect,
                                    !m_PreviewResizer.GetExpanded());
                            });
                        menu.ShowAsContext();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // Detach preview on right click in preview title bar
            if (evt.type == EventType.MouseUp && evt.button == 1 && rect.Contains(evt.mousePosition) && m_PreviewWindow == null)
                DetachPreview();

            // Logic for resizing and collapsing
            float previewSize;
            if (m_HasPreview)
            {
                // If we have a preview we'll use the ResizerControl which handles both resizing and collapsing
                previewSize = m_PreviewResizer.ResizeHandle(position, k_InspectorPreviewMinTotalHeight, k_MinAreaAbovePreview, kBottomToolbarHeight, dragRect);
            }
            else
            {
                // If we don't have a preview, just toggle the collapseble state with a button
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    m_PreviewResizer.ToggleExpanded();

                previewSize = 0;
            }

            // If collapsed, early out
            if (!m_PreviewResizer.GetExpanded())
            {
                if (m_PreviousPreviewExpandedState)
                {
                    UIElementsUtility.MakeCurrentIMGUIContainerDirty();
                    m_PreviousPreviewExpandedState = false;
                }
                m_PreviousFooterHeight = previewSize;
                return;
            }

            // The preview / label area (not including the toolbar)
            GUILayout.BeginVertical(Styles.preBackground, GUILayout.Height(previewSize));
            {
                // Draw preview
                if (m_HasPreview)
                {
                    var previewRect = GUILayoutUtility.GetRect(0, 10240, 64, 10240);
                    if (!float.IsNaN(previewRect.height) && !float.IsNaN(previewRect.width))
                    {
                        previewEditor.DrawPreview(previewRect);
                    }
                }

                GUILayout.BeginVertical(Styles.footer);
                if (hasLabels)
                {
                    using (new EditorGUI.DisabledScope(assets.Any(a => EditorUtility.IsPersistent(a) && !Editor.IsAppropriateFileOpenForEdit(a))))
                    {
                        m_LabelGUI.OnLabelGUI(assets);
                    }
                }

                if (hasBundleName)
                {
                    m_AssetBundleNameGUI.OnAssetBundleNameGUI(assets);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();

            if (m_PreviousFooterHeight >= 0f && !FloatComparer.s_ComparerWithDefaultTolerance.Equals(previewSize, m_PreviousFooterHeight))
            {
                UIElementsUtility.MakeCurrentIMGUIContainerDirty();
            }

            m_PreviousFooterHeight = previewSize;
            m_PreviousPreviewExpandedState = m_PreviewResizer.GetExpanded();
        }

        internal Object[] GetTargetsForPreview(IPreviewable previewEditor)
        {
            Editor editorForType = null;
            foreach (var editor in tracker.activeEditors)
            {
                if (editor.target.GetType() != previewEditor.target.GetType())
                {
                    continue;
                }

                editorForType = editor;
                break;
            }
            return editorForType.targets;
        }

        private void OnPreviewSelected(object userData, string[] options, int selected)
        {
            IPreviewable[] availablePreviews = userData as IPreviewable[];
            m_SelectedPreview = availablePreviews[selected];
        }

        private void DetachPreview(bool exitGUI = true)
        {
            if (Event.current != null)
                Event.current.Use();
            m_PreviewWindow = CreateInstance(typeof(PreviewWindow)) as PreviewWindow;
            m_PreviewWindow.SetParentInspector(this);
            m_PreviewWindow.Show();
            Repaint();
            UIElementsUtility.MakeCurrentIMGUIContainerDirty();
            if (exitGUI)
                GUIUtility.ExitGUI();
        }

        internal static void VersionControlBar(Editor assetEditor)
        {
            if (!Provider.enabled)
                return;
            var vcsMode = EditorSettings.externalVersionControl;
            if (vcsMode == ExternalVersionControl.Generic || vcsMode == ExternalVersionControl.Disabled || vcsMode == ExternalVersionControl.AutoDetect)
                return;

            var assetPath = AssetDatabase.GetAssetPath(assetEditor.target);
            Asset asset = Provider.GetAssetByPath(assetPath);
            if (asset == null)
                return;

            if (!Provider.PathIsVersioned(asset.path))
                return;

            var connected = Provider.isActive;

            // Note: files under project settings do not have .meta files next to them,
            // but Provider.GetAssetByPath API helpfully (or unhelpfully, in this case)
            // checks if passed file ends with "meta" and says "here, take this asset instead"
            // if it exists -- so for files under project settings, it ends up returning
            // a valid entry for the non-existing meta file. So just don't do it.
            Asset metaAsset = null;
            if (Provider.PathHasMetaFile(asset.path))
                metaAsset = Provider.GetAssetByPath(assetPath.Trim('/') + ".meta");

            string currentState = asset.StateToString();
            string currentMetaState = metaAsset == null ? String.Empty : metaAsset.StateToString();

            // If VCS is enabled but disconnected, the assets will have "Updating" state most of the time,
            // but what we want to displays is a note that VCS is not connected.
            if (!connected)
            {
                if (EditorUserSettings.WorkOffline)
                    currentState = Styles.vcsOffline.text;
                else
                    currentState = string.Format(Styles.vcsNotConnected.text, Provider.GetActivePlugin().name);
                currentMetaState = currentState;
            }

            var hasAssetState = !string.IsNullOrEmpty(currentState);
            var hasMetaState = !string.IsNullOrEmpty(currentMetaState);

            GUILayout.BeginHorizontal(GUIContent.none, EditorStyles.toolbar);
            var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toolbar, GUILayout.ExpandWidth(true));

            var icon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;
            var overlayRect = new Rect(rect.x, rect.y + 1, 28, 16);
            var iconRect = overlayRect;
            iconRect.x += 6;
            iconRect.width = 16;
            if (icon != null)
                GUI.DrawTexture(iconRect, icon);
            Overlay.DrawOverlay(asset, metaAsset ?? asset, overlayRect);

            if (currentMetaState != currentState)
            {
                if (hasAssetState && hasMetaState)
                    currentState = currentState + "; meta: " + currentMetaState;
                else if (hasAssetState && metaAsset != null) // project settings don't even have .meta files; no point in adding "asset only" for them
                    currentState = currentState + " (asset only)";
                else if (hasMetaState)
                    currentState = currentMetaState + " (meta only)";
            }

            var buttonX = VersionControlBarButtons(assetEditor, rect, asset, connected);
            var textRect = rect;
            textRect.xMin += 26;
            textRect.xMax = buttonX;

            var content = GUIContent.Temp(currentState);
            var fullState = Asset.AllStateToString(asset.state);
            var fullMetaState = metaAsset != null ? Asset.AllStateToString(metaAsset.state) : string.Empty;
            if (fullState != fullMetaState)
                fullState = $"Asset state: {fullState}\nMeta state: {fullMetaState}";
            else
                fullState = "State: " + fullState;
            content.tooltip = fullState;
            GUI.Label(textRect, content, EditorStyles.label);
            GUILayout.EndHorizontal();

            VersionControlCheckoutHint(assetEditor, connected);
        }

        static void VersionControlCheckoutHint(Editor assetEditor, bool connected)
        {
            if (!connected)
                return;

            const string prefKeyName = "vcs_CheckoutHintClosed";
            var removedHint = EditorPrefs.GetBool(prefKeyName);
            if (removedHint)
                return;
            if (Editor.IsAppropriateFileOpenForEdit(assetEditor.target))
                return;

            // allow clicking the help note to make it go away via prefs
            if (GUILayout.Button(Styles.vcsCheckoutHint, EditorStyles.helpBox))
                EditorPrefs.SetBool(prefKeyName, true);

            GUILayout.Space(4);
        }

        static float VersionControlBarButtons(Editor assetEditor, Rect rect, Asset asset, bool connected)
        {
            var buttonX = rect.xMax - 7;
            if (!connected)
                return buttonX;

            var buttonRect = rect;
            var buttonStyle = Styles.vcsButtonStyle;
            buttonRect.y += 1;
            buttonRect.height = buttonStyle.CalcSize(Styles.vcsAdd).y;

            var isFolder = asset.isFolder && !Provider.isVersioningFolders;

            var assetList = new AssetList();
            foreach (var o in assetEditor.targets)
            {
                assetList.Add(Provider.GetAssetByPath(AssetDatabase.GetAssetPath(o)));
            }

            if (!Editor.IsAppropriateFileOpenForEdit(assetEditor.target))
            {
                if (VersionControlActionButton(buttonRect, ref buttonX, Styles.vcsCheckout))
                    Provider.Checkout(assetList, CheckoutMode.Both).Wait(); // TODO: Retrieve default CheckoutMode from VC settings (depends on asset type; native vs. imported)
            }
            if (!isFolder && Provider.AddIsValid(assetList))
            {
                if (VersionControlActionButton(buttonRect, ref buttonX, Styles.vcsAdd))
                    Provider.Add(assetList, false).Wait();
            }
            if (Provider.SubmitIsValid(null, assetList))
            {
                if (VersionControlActionButton(buttonRect, ref buttonX, Styles.vcsSubmit))
                    WindowChange.Open(assetList, true);
            }
            if (!isFolder && Provider.LockIsValid(assetList))
            {
                if (VersionControlActionButton(buttonRect, ref buttonX, Styles.vcsLock))
                    Provider.Lock(assetList, true).Wait();
            }
            if (!isFolder && Provider.UnlockIsValid(assetList))
            {
                if (VersionControlActionButton(buttonRect, ref buttonX, Styles.vcsUnlock))
                    Provider.Lock(assetList, false).Wait();
            }

            return buttonX;
        }

        static bool VersionControlActionButton(Rect buttonRect, ref float buttonX, GUIContent content)
        {
            var buttonStyle = Styles.vcsButtonStyle;
            buttonRect.width = buttonStyle.CalcSize(content).x;
            buttonRect.x = buttonX - buttonRect.width;
            buttonX -= buttonRect.width;
            return GUI.Button(buttonRect, content, buttonStyle);
        }

        HashSet<int> m_DrawnSelection = new HashSet<int>();
        void DrawEditors(Editor[] editors)
        {
            Dictionary<int, EditorElement> mapping = null;

            var selection = new HashSet<int>(Selection.instanceIDs);
            if (m_DrawnSelection.SetEquals(selection))
            {
                if (editorsElement.childCount > 0 && m_DrawnSelection.Any()) // do we already have a hierarchy
                {
                    mapping = ProcessEditorElementsToRebuild(editors);
                }
            }
            else
            {
                m_DrawnSelection.Clear();
                m_DrawnSelection = selection;
            }

            if (mapping == null)
            {
                editorsElement.Clear();
            }

            if (editors.Length == 0)
            {
                return;
            }

            Editor.m_AllowMultiObjectAccess = true;

            if (editors.Length > 0 && editors[0].GetInstanceID() != m_LastInitialEditorInstanceID)
                OnTrackerRebuilt();
            if (m_RemovedComponents == null)
                ExtractPrefabComponents(); // needed after assembly reload (due to HashSet not being serializable)

            bool checkForRemovedComponents = m_ComponentsInPrefabSource != null;
            int prefabComponentIndex = -1;
            int targetGameObjectIndex = -1;
            GameObject targetGameObject = null;
            if (checkForRemovedComponents)
            {
                targetGameObjectIndex = editors[0] is PrefabImporterEditor ? 1 : 0;
                targetGameObject = (GameObject)editors[targetGameObjectIndex].target;
            }

            for (int editorIndex = 0; editorIndex < editors.Length; editorIndex++)
            {
                VisualElement prefabsComponentElement = new VisualElement() { name = "PrefabComponentElement" };
                if (checkForRemovedComponents && editorIndex > targetGameObjectIndex)
                {
                    if (prefabComponentIndex == -1)
                        prefabComponentIndex = 0;
                    while (prefabComponentIndex < m_ComponentsInPrefabSource.Length)
                    {
                        Object target = editors[editorIndex].target;
                        // This is possible if there's a component with a missing script.
                        if (target != null)
                        {
                            Object correspondingSource = PrefabUtility.GetCorrespondingObjectFromSource(target);
                            Component nextInSource = m_ComponentsInPrefabSource[prefabComponentIndex];

                            if (correspondingSource == nextInSource)
                                break;
                            AddRemovedPrefabComponentElement(targetGameObject, nextInSource, prefabsComponentElement);
                        }
                        prefabComponentIndex++;
                    }
                    prefabComponentIndex++;
                }

                if (ShouldCullEditor(editors, editorIndex))
                {
                    editors[editorIndex].isInspectorDirty = false;
                    continue;
                }

                var editor = editors[editorIndex];
                Object editorTarget = editor.targets[0];

                string editorTitle = ObjectNames.GetInspectorTitle(editorTarget);
                EditorElement editorContainer;

                try
                {
                    if (mapping == null || !mapping.TryGetValue(editors[editorIndex].target.GetInstanceID(), out editorContainer))
                    {
                        editorContainer = new EditorElement(editorIndex, this) { name = editorTitle };
                        editorsElement.Add(editorContainer);
                    }

                    if (prefabsComponentElement.childCount > 0)
                    {
                        editorContainer.AddPrefabComponent(prefabsComponentElement);
                    }
                    else
                    {
                        editorContainer.AddPrefabComponent(null);
                    }
                }
                catch (Editor.SerializedObjectNotCreatableException)
                {
                    // This can happen after a domain reload when the
                    // target is a pure c# object, like a MonoBehaviour
                    // We'll just attempt to recreate the EditorElement on the next frame
                    // see case 1147234
                }
            }

            // Make sure to display any remaining removed components that come after the last component on the GameObject.
            if (checkForRemovedComponents)
            {
                VisualElement prefabsComponentElement = new VisualElement() { name = "RemainingPrefabComponentElement" };
                while (prefabComponentIndex < m_ComponentsInPrefabSource.Length)
                {
                    Component nextInSource = m_ComponentsInPrefabSource[prefabComponentIndex];
                    AddRemovedPrefabComponentElement(targetGameObject, nextInSource, prefabsComponentElement);

                    prefabComponentIndex++;
                }

                if (prefabsComponentElement.childCount > 0)
                {
                    editorsElement.Add(prefabsComponentElement);
                    m_RemovedPrefabComponentsElement = prefabsComponentElement;
                }
            }
        }

        void RestoreVerticalScrollIfNeeded()
        {
            if (m_LastInspectedObjectInstanceID == -1)
                return;
            var inspectedObjectInstanceID = GetInspectedObject()?.GetInstanceID() ?? -1;
            if (inspectedObjectInstanceID == m_LastInspectedObjectInstanceID && inspectedObjectInstanceID != -1)
                m_ScrollView.verticalScroller.value = m_LastVerticalScrollValue;
            m_LastInspectedObjectInstanceID = -1; // reset to make sure the restore occurs once
        }

        void AddRemovedPrefabComponentElement(GameObject targetGameObject, Component nextInSource, VisualElement element)
        {
            if (ShouldDisplayRemovedComponent(targetGameObject, nextInSource))
            {
                string missingComponentTitle = ObjectNames.GetInspectorTitle(nextInSource);
                var removedComponentElement =
                    CreateIMGUIContainer(() => DisplayRemovedComponent(targetGameObject, nextInSource), missingComponentTitle);
                removedComponentElement.style.paddingBottom = kEditorElementPaddingBottom;

                element.Add(removedComponentElement);
            }
        }

        bool ShouldDisplayRemovedComponent(GameObject go, Component comp)
        {
            if (m_ComponentsInPrefabSource == null || m_RemovedComponents == null)
                return false;
            if (go == null)
                return false;
            if (comp == null)
                return false;
            if ((comp.hideFlags & HideFlags.HideInInspector) != 0)
                return false;
            if (!m_RemovedComponents.Contains(comp))
                return false;
            if (comp.IsCoupledComponent())
                return false;

            return true;
        }

        static void DisplayRemovedComponent(GameObject go, Component comp)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar);
            EditorGUI.RemovedComponentTitlebar(rect, go, comp);
        }

        internal bool WasEditorVisible(Editor[] editors, int editorIndex, Object target)
        {
            int wasVisibleState = tracker.GetVisible(editorIndex);
            bool wasVisible;

            if (wasVisibleState == -1)
            {
                // Some inspectors (MaterialEditor) needs to be told when they are the main visible asset.
                if (editorIndex == 0 || (editorIndex == 1 && ShouldCullEditor(editors, 0)))
                {
                    editors[editorIndex].firstInspectedEditor = true;
                }

                // Init our state with last state
                // Large headers should always be considered visible
                // because they need to at least update their Icons when they have a static preview (Material for exemple)
                wasVisible = InternalEditorUtility.GetIsInspectorExpanded(target) || EditorHasLargeHeader(editorIndex, editors);
                tracker.SetVisible(editorIndex, wasVisible ? 1 : 0);
            }
            else
            {
                wasVisible = wasVisibleState == 1;
            }

            return wasVisible;
        }

        internal bool IsMultiEditingSupported(Editor editor, Object target)
        {
            // Culling of editors that can't be properly shown.
            // If the current editor is a GenericInspector even though a custom editor for it exists,
            // then it's either a fallback for a custom editor that doesn't support multi-object editing,
            // or we're in debug mode.
            bool multiEditingSupported = true;
            if (editor is GenericInspector && CustomEditorAttributes.FindCustomEditorType(target, false) != null)
            {
                if (m_InspectorMode == InspectorMode.DebugInternal)
                {
                    // Do nothing
                }
                else if (m_InspectorMode == InspectorMode.Normal)
                {
                    // If we're not in debug mode and it thus must be a fallback,
                    // hide the editor and show a notification.
                    multiEditingSupported = false;
                }
                else if (target is AssetImporter)
                {
                    // If we are in debug mode and it's an importer type,
                    // hide the editor and show a notification.
                    multiEditingSupported = false;
                }

                // If we are in debug mode and it's an NOT importer type,
                // just show the debug inspector as usual.
            }

            return multiEditingSupported;
        }

        internal static bool EditorHasLargeHeader(int editorIndex, Editor[] trackerActiveEditors)
        {
            return trackerActiveEditors[editorIndex].firstInspectedEditor || trackerActiveEditors[editorIndex].HasLargeHeader();
        }

        internal bool ShouldCullEditor(Editor[] editors, int editorIndex)
        {
            if (editors[editorIndex].hideInspector)
                return true;

            Object currentTarget = editors[editorIndex].target;

            // Editors that should always be hidden
            if (currentTarget is ParticleSystemRenderer)
                return true;

            // Hide regular AssetImporters (but not inherited types)
            if (currentTarget != null && currentTarget.GetType() == typeof(AssetImporter))
                return true;

            // Let asset importers decide if the imported object should be shown or not
            if (m_InspectorMode == InspectorMode.Normal && editorIndex != 0)
            {
                AssetImporterEditor importerEditor = GetAssetImporter(editors);
                if (importerEditor != null && !importerEditor.showImportedObject)
                    return true;
            }

            return false;
        }

        void DrawSelectionPickerList()
        {
            if (m_TypeSelectionList == null)
                m_TypeSelectionList = new TypeSelectionList(Selection.objects);

            // Force header to be flush with the top of the window
            GUILayout.Space(0);

            Editor.DrawHeaderGUI(null, Selection.objects.Length + " Objects");

            GUILayout.Label("Narrow the Selection:", EditorStyles.label);
            GUILayout.Space(4);

            Vector2 oldSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(16, 16));

            foreach (TypeSelection ts in m_TypeSelectionList.typeSelections)
            {
                Rect r = GUILayoutUtility.GetRect(16, 16, GUILayout.ExpandWidth(true));
                if (GUI.Button(r, ts.label, Styles.typeSelection))
                {
                    Selection.objects = ts.objects;
                    Event.current.Use();
                }
                if (GUIUtility.hotControl == 0)
                    EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
                GUILayout.Space(4);
            }

            EditorGUIUtility.SetIconSize(oldSize);
        }

        AssetImporterEditor GetAssetImporter(Editor[] editors)
        {
            if (editors == null || editors.Length == 0)
                return null;

            return editors[0] as AssetImporterEditor;
        }

        private void AddComponentButton(Editor[] editors)
        {
            // Don't show the Add Component button if we are not showing imported objects for Asset Importers
            var assetImporter = GetAssetImporter(editors);
            if (assetImporter != null && !assetImporter.showImportedObject)
                return;

            Editor editor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(editors);
            if (editor != null && editor.target != null && editor.target is GameObject && editor.IsEnabled())
            {
                EditorGUILayout.BeginHorizontal(GUIContent.none, GUIStyle.none, GUILayout.Height(kAddComponentButtonHeight));
                {
                    GUILayout.FlexibleSpace();
                    var content = Styles.addComponentLabel;
                    Rect rect = GUILayoutUtility.GetRect(content, Styles.addComponentButtonStyle);

                    // Visually separates the Add Component button from the existing components
                    if (Event.current.type == EventType.Repaint)
                        DrawSplitLine(rect.y);
                    rect.y += 9;

                    if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, Styles.addComponentButtonStyle) ||
                        m_OpenAddComponentMenu && Event.current.type == EventType.Repaint)
                    {
                        m_OpenAddComponentMenu = false;
                        if (AddComponentWindow.Show(rect, editor.targets.Select(o => (GameObject)o).ToArray()))
                        {
                            GUIUtility.ExitGUI();
                        }
                    }

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private bool ReadyToRepaint()
        {
            if (AnimationMode.InAnimationPlaybackMode())
            {
                long timeNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                if (timeNow - s_LastUpdateWhilePlayingAnimation < delayRepaintWhilePlayingAnimation)
                    return false;
                s_LastUpdateWhilePlayingAnimation = timeNow;
            }

            return true;
        }

        private void DrawSplitLine(float y)
        {
            Rect position = new Rect(0, y - Styles.lineSeparatorOffset, m_Pos.width + 1, 1);
            using (new GUI.ColorScope(Styles.lineSeparatorColor * GUI.color))
                GUI.DrawTexture(position, EditorGUIUtility.whiteTexture);
        }

        // Invoked from C++
        internal static void ShowWindow()
        {
            GetWindow(typeof(InspectorWindow));
        }

        // Called from OptimizedGUIBlock::FlushAll();
        private static void FlushOptimizedGUI()
        {
            // Delay flushing since FlushOptimizedGUI is called when going into playmode (assembly reload)
            // for every TextMeshGenerator::Flush () in c++.
            // By delaying we also ensure that our editor trackers are fully constructed before flushing (fixes case 718491)
            s_AllOptimizedGUIBlocksNeedsRebuild = true;
        }

        private static void FlushAllOptimizedGUIBlocksIfNeeded()
        {
            if (!s_AllOptimizedGUIBlocksNeedsRebuild)
                return;
            s_AllOptimizedGUIBlocksNeedsRebuild = false;
        }

        private void Update()
        {
            Editor[] editors = tracker.activeEditors;
            if (editors == null)
                return;

            bool wantsRepaint = false;
            foreach (var myEditor in editors)
            {
                if (myEditor.RequiresConstantRepaint() && !myEditor.hideInspector)
                    wantsRepaint = true;
            }

            if (wantsRepaint && m_lastRenderedTime + 0.033f < EditorApplication.timeSinceStartup)
            {
                m_lastRenderedTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        void PrepareLockedObjectsForSerialization()
        {
            ClearSerializedLockedObjects();

            if (m_Tracker != null && m_Tracker.isLocked)
            {
                m_Tracker.GetObjectsLockedByThisTracker(m_ObjectsLockedBeforeSerialization);

                // take out non persistent and track them in a list of instance IDs, because they wouldn't survive serialization as Objects
                for (int i = m_ObjectsLockedBeforeSerialization.Count - 1; i >= 0; i--)
                {
                    if (!EditorUtility.IsPersistent(m_ObjectsLockedBeforeSerialization[i]))
                    {
                        if (m_ObjectsLockedBeforeSerialization[i] != null)
                            m_InstanceIDsLockedBeforeSerialization.Add(m_ObjectsLockedBeforeSerialization[i].GetInstanceID());
                        m_ObjectsLockedBeforeSerialization.RemoveAt(i);
                    }
                }
            }
        }

        void ClearSerializedLockedObjects()
        {
            m_ObjectsLockedBeforeSerialization.Clear();

            m_InstanceIDsLockedBeforeSerialization.Clear();
        }

        internal void GetObjectsLocked(List<Object> objs)
        {
            m_Tracker.GetObjectsLockedByThisTracker(objs);
        }

        internal void SetObjectsLocked(List<Object> objs)
        {
            m_LockTracker.isLocked = true;
            m_Tracker.SetObjectsLockedByThisTracker(objs);
        }

        void RestoreLockStateFromSerializedData()
        {
            if (m_Tracker == null)
            {
                return;
            }

            // try to retrieve all Objects from their stored instance ids in the list.
            // this is only used for non persistent objects (scene objects)

            if (m_InstanceIDsLockedBeforeSerialization.Count > 0)
            {
                for (int i = 0; i < m_InstanceIDsLockedBeforeSerialization.Count; i++)
                {
                    Object instance = EditorUtility.InstanceIDToObject(m_InstanceIDsLockedBeforeSerialization[i]);
                    //don't add null objects (i.e.
                    if (instance)
                    {
                        m_ObjectsLockedBeforeSerialization.Add(instance);
                    }
                }
            }

            for (int i = m_ObjectsLockedBeforeSerialization.Count - 1; i >= 0; i--)
            {
                if (m_ObjectsLockedBeforeSerialization[i] == null)
                {
                    m_ObjectsLockedBeforeSerialization.RemoveAt(i);
                }
            }

            // set the tracker to the serialized list. if it contains nulls or is empty, the tracker won't lock
            // this fixes case 775007
            m_Tracker.SetObjectsLockedByThisTracker(m_ObjectsLockedBeforeSerialization);
            // since this method likely got called during OnEnable, and rebuilding the tracker could call OnDisable on all Editors,
            // some of which might not have gotten their enable yet, the rebuilding needs to happen delayed in EditorApplication.update
            new DelayedCallback(tracker.RebuildIfNecessary, 0f);
        }

        internal static bool AddInspectorWindow(InspectorWindow window)
        {
            if (m_AllInspectors.Contains(window))
            {
                return false;
            }

            m_AllInspectors.Add(window);
            return true;
        }

        internal static void RemoveInspectorWindow(InspectorWindow window)
        {
            m_AllInspectors.Remove(window);
        }

        /*
         * ProcessEditorElementsToRebuild is required as there are times when the ActiveEditorTracker
         * is rebuilt even though the selection does not change and there are IMGUI controls outside
         * the InspectorWindow that depend on currently valid control IDs
         * An example is the Object Selector
         *
         * When a rebuild of the InspectorWindow is requested, we attempt to match up existing Editor references to current ones.
         * We can't rely on the Editor instance ids those will have been recreated and our previously drawn ones will now be invalid
         * Instead we can find matching Editor instances by comparing the target InstanceIDs.
         */
        Dictionary<int, EditorElement> ProcessEditorElementsToRebuild(Editor[] editors)
        {
            Dictionary<int, EditorElement> editorToElementMap = new Dictionary<int, EditorElement>();
            var currentElements = editorsElement.Children().OfType<EditorElement>().ToList();
            if (editors.Length == 0)
            {
                return null;
            }

            if (rootVisualElement.panel == null)
            {
                return null;
            }

            var newEditorsIndex = 0;
            var previousEditorsIndex = 0;
            while (newEditorsIndex < editors.Length && previousEditorsIndex < currentElements.Count)
            {
                var ed = editors[newEditorsIndex];
                var currentEd = currentElements[previousEditorsIndex];

                if (currentEd.editor == null)
                {
                    ++previousEditorsIndex;
                    continue;
                }

                // We won't have an EditorElement for editors that are normally culled so we should skip this
                if (ShouldCullEditor(editors, newEditorsIndex))
                {
                    ++newEditorsIndex;
                    continue;
                }

                if (ed.target != currentEd.editor.target)
                {
                    return null;
                }

                currentEd.Reinit(newEditorsIndex);
                editorToElementMap[ed.target.GetInstanceID()] = currentEd;
                ++newEditorsIndex;
                ++previousEditorsIndex;
            }

            // Remove any elements at the end of the InspectorWindow that don't have matching Editors
            for (int j = previousEditorsIndex; j < currentElements.Count; ++j)
            {
                currentElements[j].RemoveFromHierarchy();
            }

            return editorToElementMap;
        }
    }
}
