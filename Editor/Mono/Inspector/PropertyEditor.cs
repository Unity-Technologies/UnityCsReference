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
using UnityEditor.StyleSheets;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

using Object = UnityEngine.Object;
using AssetImporterEditor = UnityEditor.Experimental.AssetImporters.AssetImporterEditor;
using JetBrains.Annotations;

namespace UnityEditor
{
    interface IPropertyView
    {
        ActiveEditorTracker tracker { get; }
        InspectorMode inspectorMode { get; }
        bool useUIElementsDefaultInspector { get; }
        HashSet<int> editorsWithImportedObjectLabel { get; }
        Editor lastInteractedEditor { get; set; }
        GUIView parent { get; }
        EditorDragging editorDragging { get; }

        IMGUIContainer CreateIMGUIContainer(Action headerOnGUI, string v);
        bool IsMultiEditingSupported(Editor editor, Object target);
        bool WasEditorVisible(Editor[] editors, int editorIndex, Object target);
        bool ShouldCullEditor(Editor[] editors, int editorIndex);
        void Repaint();
    }

    interface IPropertySourceOpener
    {
        Object hoveredObject { get; }
    }

    class PropertyEditor : EditorWindow, IPropertyView, IHasCustomMenu
    {
        internal const string k_AssetPropertiesMenuItemName = "Assets/Properties... _&P";
        protected const string s_MultiEditClassName = "unity-inspector-no-multi-edit-warning";
        protected const string s_EditorListClassName = "unity-inspector-editors-list";
        protected const string s_AddComponentClassName = "unity-inspector-add-component-button";
        protected const string s_HeaderInfoClassName = "unity-inspector-header-info";
        protected const string s_FooterInfoClassName = "unity-inspector-footer-info";
        internal const string s_MainContainerClassName = "unity-inspector-main-container";

        protected const float kBottomToolbarHeight = 21f;
        protected const float kAddComponentButtonHeight = 45f;
        internal const float kEditorElementPaddingBottom = 2f;
        protected const float k_MinAreaAbovePreview = 130;
        protected const float k_InspectorPreviewMinHeight = 130;
        protected const float k_InspectorPreviewMinTotalHeight = k_InspectorPreviewMinHeight + kBottomToolbarHeight;
        protected const int k_MinimumRootVisualHeight = 81;
        protected const int k_MinimumWindowWidth = 275;
        protected const int k_AutoScrollZoneHeight = 24;

        protected const long delayRepaintWhilePlayingAnimation = 150; // Delay between repaints in milliseconds while playing animation
        protected long m_LastUpdateWhilePlayingAnimation = 0;

        [SerializeField] protected List<Object> m_ObjectsLockedBeforeSerialization = new List<Object>();
        [SerializeField] protected List<int> m_InstanceIDsLockedBeforeSerialization = new List<int>();
        [SerializeField] protected PreviewResizer m_PreviewResizer = new PreviewResizer();
        [SerializeField] protected LabelGUI m_LabelGUI = new LabelGUI();
        [SerializeField] protected int m_LastInspectedObjectInstanceID = -1;
        [SerializeField] protected float m_LastVerticalScrollValue = 0;
        [SerializeField] protected string m_AssetGUID = "";
        [SerializeField] protected int m_InstanceID = 0;

        private Object m_InspectedObject;
        private static PropertyEditor s_LastPropertyEditor;
        protected int m_LastInitialEditorInstanceID;
        protected Component[] m_ComponentsInPrefabSource;
        protected HashSet<Component> m_RemovedComponents;
        protected bool m_ResetKeyboardControl;
        internal bool m_OpenAddComponentMenu = false;
        protected ActiveEditorTracker m_Tracker;
        protected AssetBundleNameGUI m_AssetBundleNameGUI = new AssetBundleNameGUI();
        protected TypeSelectionList m_TypeSelectionList = null;
        protected double m_lastRenderedTime;
        protected List<IPreviewable> m_Previews;
        protected Dictionary<Type, List<Type>> m_PreviewableTypes;
        protected IPreviewable m_SelectedPreview;
        protected VisualElement m_EditorsElement;
        protected VisualElement editorsElement => m_EditorsElement ?? (m_EditorsElement = FindVisualElementInTreeByClassName(s_EditorListClassName));
        protected VisualElement m_RemovedPrefabComponentsElement;
        protected VisualElement m_PreviewAndLabelElement;
        protected VisualElement previewAndLabelElement => m_PreviewAndLabelElement ?? (m_PreviewAndLabelElement = FindVisualElementInTreeByClassName(s_FooterInfoClassName));
        protected VisualElement m_VersionControlElement;
        protected VisualElement versionControlElement => m_VersionControlElement ?? (m_VersionControlElement = FindVisualElementInTreeByClassName(s_HeaderInfoClassName));
        protected static Dictionary<Editor, VersionControlBarState> m_VersionControlBarState = new Dictionary<Editor, VersionControlBarState>();
        protected VisualElement m_MultiEditLabel;
        protected ScrollView m_ScrollView;
        protected bool m_TrackerResetInserted;
        internal bool m_FirstInitialize;
        protected float m_PreviousFooterHeight = -1;
        protected bool m_PreviousPreviewExpandedState;
        protected bool m_HasPreview;
        protected HashSet<int> m_DrawnSelection = new HashSet<int>();
        protected InspectorMode m_InspectorMode = InspectorMode.Normal;

        public GUIView parent => m_Parent;
        public HashSet<int> editorsWithImportedObjectLabel { get; } = new HashSet<int>();
        public EditorDragging editorDragging { get; }
        public bool useUIElementsDefaultInspector { get; internal set; } = false;
        public Editor lastInteractedEditor { get; set; }
        internal static PropertyEditor CurrentPropertyEditor { get; private set; }

        public InspectorMode inspectorMode
        {
            get { return m_InspectorMode; }
            set { SetMode(value); }
        }

        public ActiveEditorTracker tracker
        {
            get
            {
                CreateTracker();
                return m_Tracker;
            }
        }

        protected Rect bottomAreaDropRectangle
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

        protected static class Styles
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
            public static readonly GUIContent vcsSettings = EditorGUIUtility.TrTextContent("Settings");
            public static readonly GUIContent vcsCheckout = EditorGUIUtility.TrTextContent("Check Out");
            public static readonly GUIContent vcsCheckoutMeta = EditorGUIUtility.TrTextContent("Check Out Meta");
            public static readonly GUIContent vcsAdd = EditorGUIUtility.TrTextContent("Add");
            public static readonly GUIContent vcsLock = EditorGUIUtility.TrTextContent("Lock");
            public static readonly GUIContent vcsUnlock = EditorGUIUtility.TrTextContent("Unlock");
            public static readonly GUIContent vcsSubmit = EditorGUIUtility.TrTextContent("Submit");
            public static readonly GUIContent vcsRevert = EditorGUIUtility.TrTextContent("Revert");
            public static readonly GUIContent vcsRevertUnchanged = EditorGUIUtility.TrTextContent("Revert Unchanged");
            public static readonly GUIContent[] vcsRevertMenuNames = {vcsRevertUnchanged};
            public static readonly GenericMenu.MenuFunction2[] vcsRevertMenuActions = {DoRevertUnchanged};
            public static readonly GUIStyle vcsButtonStyle = EditorStyles.miniButton;
            public static GUIStyle vcsRevertStyle = new GUIStyle(EditorStyles.dropDownList);
            public static readonly GUIStyle vcsBarStyleOneRow = EditorStyles.toolbar;
            public static GUIStyle vcsBarStyleTwoRows = new GUIStyle(EditorStyles.toolbar);
            public static readonly string objectDisabledModuleWarningFormat = L10n.Tr(
                "The built-in package '{0}', which implements this component type, has been disabled in Package Manager. This object will be removed in play mode and from any builds you make."
            );
            public static readonly string objectDisabledModuleWithDependencyWarningFormat = L10n.Tr(
                "The built-in package '{0}', which is required by the package '{1}', which implements this component type, has been disabled in Package Manager. This object will be removed in play mode and from any builds you make."
            );

            public static SVC<float> lineSeparatorOffset = new SVC<float>("AC-Button", "--separator-line-top-offset");
            public static SVC<Color> lineSeparatorColor = new SVC<Color>("--theme-line-separator-color", Color.red);

            static Styles()
            {
                vcsRevertStyle.padding.right = 15;
                vcsBarStyleTwoRows.fixedHeight *= 2;
            }
        }

        protected class VersionControlBarState
        {
            public bool settings;
            public bool revert;
            public bool revertUnchanged;
            public bool checkout;
            public bool add;
            public bool submit;
            public bool @lock;
            public bool unlock;
            public Editor editor;
            public AssetList assets = new AssetList();

            public int GetButtonCount()
            {
                var c = 0;
                if (settings) ++c;
                if (revert) ++c; // revertUnchanged is same button in a drop-down
                if (checkout) ++c;
                if (add) ++c;
                if (submit) ++c;
                if (@lock) ++c;
                if (unlock) ++c;
                return c;
            }

            public static VersionControlBarState Calculate(Editor assetEditor, Asset asset, bool connected)
            {
                var res = new VersionControlBarState();
                if (!connected)
                {
                    res.settings = true;
                    return res;
                }

                var isFolder = asset.isFolder && !Provider.isVersioningFolders;

                res.assets.AddRange(assetEditor.targets.Select(o => Provider.GetAssetByPath(AssetDatabase.GetAssetPath(o))));
                res.assets = Provider.ConsolidateAssetList(res.assets, CheckoutMode.Both);
                res.editor = assetEditor;

                res.revert = Provider.RevertIsValid(res.assets, RevertMode.Normal);
                res.revertUnchanged = Provider.RevertIsValid(res.assets, RevertMode.Unchanged);

                bool checkoutBoth = assetEditor.target == null || Provider.NeedToCheckOutBoth(assetEditor.target);
                res.checkout = isFolder || Provider.CheckoutIsValid(res.assets, checkoutBoth ? CheckoutMode.Both : CheckoutMode.Meta);
                res.add = Provider.AddIsValid(res.assets);
                res.submit = Provider.SubmitIsValid(null, res.assets);
                res.@lock = Provider.hasLockingSupport && !isFolder && Provider.LockIsValid(res.assets);
                res.unlock = Provider.hasLockingSupport && !isFolder && Provider.UnlockIsValid(res.assets);
                return res;
            }
        }

        internal PropertyEditor()
        {
            editorDragging = new EditorDragging(this);
            minSize = new Vector2(k_MinimumWindowWidth, minSize.y);
        }

        [UsedImplicitly]
        protected virtual void OnDestroy()
        {
            if (m_Tracker != null)
                m_Tracker.Destroy();
        }

        [UsedImplicitly]
        protected virtual void OnFocusChanged(bool focus)
        {
            // focusing away from the editor flushes VCS state cache and might get
            // updated states from external clients; make sure to recalculate which VCS
            // buttons should be visible
            ClearVersionControlBarState();
        }

        [UsedImplicitly]
        protected virtual void OnEnable()
        {
            LoadVisualTreeFromUxml();
            m_PreviewResizer.localFrame = true;
            m_PreviewResizer.Init("InspectorPreview");
            m_LabelGUI.OnEnable();
            m_FirstInitialize = true;
            CreateTracker();

            EditorApplication.focusChanged += OnFocusChanged;
        }

        [UsedImplicitly]
        protected virtual void OnDisable()
        {
            // save vertical scroll position
            m_LastInspectedObjectInstanceID = GetInspectedObject()?.GetInstanceID() ?? -1;
            m_LastVerticalScrollValue = m_ScrollView?.verticalScroller.value ?? 0;

            EditorApplication.focusChanged -= OnFocusChanged;
        }

        [UsedImplicitly]
        protected virtual void OnLostFocus()
        {
            m_LabelGUI.OnLostFocus();
        }

        protected virtual bool CloseIfEmpty()
        {
            // It should never close if its tracker is not locked.
            if (!tracker.isLocked)
            {
                return false;
            }

            // We can rely on the tracker to always keep valid Objects
            // even after an assemblyreload or assetdatabase refresh.
            List<Object> locked = new List<Object>();
            tracker.GetObjectsLockedByThisTracker(locked);
            if (locked.Any(o => o != null))
                return false;

            EditorApplication.delayCall += Close;
            return true;
        }

        [UsedImplicitly]
        protected virtual void OnInspectorUpdate()
        {
            if (CloseIfEmpty())
                return;

            // Check if scripts have changed without calling set dirty
            tracker.VerifyModifiedMonoBehaviours();

            if (!tracker.isDirty || !ReadyToRepaint())
                return;

            Repaint();
        }

        [UsedImplicitly]
        protected virtual void OnGUI()
        {
            if (m_FirstInitialize)
                RebuildContentsContainers();
        }

        static Editor[] s_Editors = new Editor[10];

        [UsedImplicitly]
        protected virtual void Update()
        {
            ActiveEditorTracker.Internal_GetActiveEditorsNonAlloc(tracker, s_Editors);
            if (s_Editors.Length == 0)
                return;

            bool wantsRepaint = false;
            foreach (var myEditor in s_Editors)
            {
                if (myEditor != null && myEditor.RequiresConstantRepaint() && !myEditor.hideInspector)
                    wantsRepaint = true;
            }

            if (wantsRepaint && m_lastRenderedTime + 0.033f < EditorApplication.timeSinceStartup)
            {
                m_lastRenderedTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        protected void SetMode(InspectorMode mode)
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

        protected void SetTitle(Object obj)
        {
            var objTitle = ObjectNames.GetInspectorTitle(obj);
            var titleTooltip = objTitle;
            if (!String.IsNullOrEmpty(m_AssetGUID))
                titleTooltip = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            else
            {
                var go = obj as GameObject;
                if (go)
                    titleTooltip = EditorUtility.GetHierarchyPath(go);
                else if (obj is Component c)
                    titleTooltip = $"{EditorUtility.GetHierarchyPath(c.gameObject)} ({objTitle})";
            }

            titleContent = EditorGUIUtility.TrTextContentWithIcon(obj.name, titleTooltip, "UnityEditor.InspectorWindow");
            titleContent.image = AssetPreview.GetMiniThumbnail(obj);
        }

        protected virtual void RefreshTitle()
        {
            var obj = GetInspectedObject();
            if (!obj)
                return;
            SetTitle(obj);
        }

        private VisualElement FindVisualElementInTreeByClassName(string elementClassName)
        {
            return rootVisualElement.Q(className: elementClassName);
        }

        internal static void ClearVersionControlBarState()
        {
            m_VersionControlBarState.Clear();
        }

        protected void LoadVisualTreeFromUxml()
        {
            var tpl = EditorGUIUtility.Load("UXML/InspectorWindow/InspectorWindow.uxml") as VisualTreeAsset;
            var fContainer = rootVisualElement.Query(null, s_MainContainerClassName).First();
            VisualElement container = fContainer ?? tpl.Instantiate();
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

        private void OnGeometryChanged(GeometryChangedEvent e)
        {
            if (m_PreviewResizer.GetExpanded())
            {
                if (previewAndLabelElement.layout.height > 0 &&
                    rootVisualElement.layout.height <= k_MinimumRootVisualHeight + m_PreviewResizer.containerMinimumHeightExpanded)
                {
                    m_PreviewResizer.SetExpanded(false);
                }
            }
            RestoreVerticalScrollIfNeeded();
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            if (IsAnyComponentCollapsed())
                menu.AddItem(EditorGUIUtility.TrTextContent("Expand All Components"), false, ExpandAllComponents);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Expand All Components"));

            if (IsAnyComponentExpanded())
                menu.AddItem(EditorGUIUtility.TrTextContent("Collapse All Components"), false, CollapseAllComponents);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Collapse All Components"));

            if (m_Tracker != null)
            {
                bool addedSeparator = false;
                foreach (var editor in m_Tracker.activeEditors)
                {
                    var menuContainer = editor as IHasCustomMenu;
                    if (menuContainer != null)
                    {
                        if (!addedSeparator)
                        {
                            menu.AddSeparator(String.Empty);
                            addedSeparator = true;
                        }
                        menuContainer.AddItemsToMenu(menu);
                    }
                }
            }

            menu.AddSeparator("");
            menu.AddItem(EditorGUIUtility.TrTextContent("Ping"), false, () => EditorGUIUtility.PingObject(GetInspectedObject()));
        }

        protected void ExpandAllComponents()
        {
            var editors = tracker.activeEditors;
            for (int i = 1; i < editors.Length; i++)
                tracker.SetVisible(i, 1);
        }

        protected bool IsAnyComponentCollapsed()
        {
            if (Selection.activeGameObject == null)
                return false; // If the selection is not a game object then disable the option.

            var editors = tracker.activeEditors;
            for (int i = 1; i < editors.Length; i++)
            {
                if (tracker.GetVisible(i) == 0)
                    return true;
            }
            return false;
        }

        protected void CollapseAllComponents()
        {
            var editors = tracker.activeEditors;
            for (int i = 1; i < editors.Length; i++)
                tracker.SetVisible(i, 0);
        }

        protected bool IsAnyComponentExpanded()
        {
            if (Selection.activeGameObject == null)
                return false;

            var editors = this.tracker.activeEditors;
            for (int i = 1; i < editors.Length; i++)
            {
                if (this.tracker.GetVisible(i) == 1)
                    return true;
            }
            return false;
        }

        protected bool LoadPersistedObject()
        {
            if (String.IsNullOrEmpty(m_AssetGUID) && m_InstanceID == 0)
                return false;

            if (!String.IsNullOrEmpty(m_AssetGUID))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
                m_InspectedObject = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            }
            else if (m_InstanceID != 0)
            {
                m_InspectedObject = FindObjectFromInstanceID(m_InstanceID) ?? ForceLoadFromInstanceID(m_InstanceID);
            }

            if (m_InspectedObject)
            {
                SetTitle(m_InspectedObject);
                m_Tracker.SetObjectsLockedByThisTracker(new List<Object> { m_InspectedObject });
            }
            else
            {
                // Failed to load object, lets close this property editor.
                EditorApplication.delayCall += Close;
                return false;
            }

            return true;
        }

        protected virtual void CreateTracker()
        {
            if (m_Tracker != null)
                return;

            m_Tracker = new ActiveEditorTracker { inspectorMode = InspectorMode.Normal };
            if (LoadPersistedObject())
                m_Tracker.RebuildIfNecessary();
        }

        private void OnTrackerRebuilt()
        {
            ExtractPrefabComponents();
            // tracker gets rebuilt when selection or objects change; make sure to recalc which VCS
            // buttons are shown
            ClearVersionControlBarState();
        }

        private void ExtractPrefabComponents()
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

        protected void CreatePreviewables()
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
            // We initialize this list once per PropertyEditor, instead of globally.
            // This means that if the user is debugging an IPreviewable structure,
            // the PropertyEditor can be closed and reopened to refresh this list.
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

        private void ClearTrackerDirtyOnRepaint()
        {
            if (Event.current.type == EventType.Repaint)
            {
                tracker.ClearDirty();
            }
        }

        public IMGUIContainer CreateIMGUIContainer(Action onGUIHandler, string name = null)
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

            if (name != null)
            {
                result.name = name;
            }

            return result;
        }

        protected virtual void BeginRebuildContentContainers() {}
        protected virtual void EndRebuildContentContainers() {}
        internal virtual void RebuildContentsContainers()
        {
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

            BeginRebuildContentContainers();

            ResetKeyboardControl();

            var addComponentButton = rootVisualElement.Q(className: s_AddComponentClassName);
            if (addComponentButton != null)
                addComponentButton.Clear();
            if (versionControlElement != null)
                versionControlElement.Clear();
            if (previewAndLabelElement != null)
                previewAndLabelElement.Clear();

            Editor[] editors = tracker.activeEditors;

            if (editors.Any() && versionControlElement != null)
            {
                versionControlElement.Add(CreateIMGUIContainer(
                    () => VersionControlBar(InspectorWindowUtils.GetFirstNonImportInspectorEditor(editors))));
            }

            DrawEditors(editors);

            var labelMustBeAdded = editorsElement != null && m_MultiEditLabel.parent != editorsElement;

            // The PrefabImporterEditor can hide its imported objects if it detects missing scripts. In this case
            // do not add the multi editing warning
            var assetImporter = GetAssetImporter(editors);
            if (assetImporter != null && !assetImporter.showImportedObject)
                labelMustBeAdded = false;

            if (tracker.hasComponentsWhichCannotBeMultiEdited)
            {
                if (editors.Length == 0 && !tracker.isLocked && Selection.objects.Length > 0 && editorsElement != null)
                {
                    editorsElement.Add(CreateIMGUIContainer(DrawSelectionPickerList));
                }
                else
                {
                    if (labelMustBeAdded && editorsElement != null)
                    {
                        editorsElement.Add(m_MultiEditLabel);
                    }
                }
            }
            else if (m_MultiEditLabel != null)
            {
                m_MultiEditLabel.RemoveFromHierarchy();
            }

            if (addComponentButton != null && editors.Any() && RootEditorUtils.SupportsAddComponent(editors))
            {
                addComponentButton.Add(CreateIMGUIContainer(() =>
                {
                    EditorGUI.indentLevel = 0;
                    AddComponentButton(editors);
                }));
            }

            if (m_PreviewResizer != null && editors.Any())
            {
                var previewAndLabelsContainer = CreateIMGUIContainer(DrawPreviewAndLabels, "preview-container");
                m_PreviewResizer.SetContainer(previewAndLabelsContainer, kBottomToolbarHeight);
                if (previewAndLabelElement != null)
                    previewAndLabelElement.Add(previewAndLabelsContainer);
            }

            rootVisualElement.MarkDirtyRepaint();

            ScriptAttributeUtility.ClearGlobalCache();

            rootVisualElement.RegisterCallback<DragUpdatedEvent>(DragOverBottomArea);
            rootVisualElement.RegisterCallback<DragPerformEvent>(DragPerformInBottomArea);
            rootVisualElement.RegisterCallback<MouseEnterEvent>((e) => { CurrentPropertyEditor = this; });
            rootVisualElement.RegisterCallback<MouseLeaveEvent>((e) => { CurrentPropertyEditor = null; });

            EndRebuildContentContainers();

            Repaint();
            RefreshTitle();
        }

        internal void AutoScroll(Vector2 mousePosition)
        {
            if (m_ScrollView != null)
            {
                // implement auto-scroll for easier component drag'n'drop,
                // we define a zone of height = k_AutoScrollZoneHeight
                // at the top/bottom of the scrollView viewport,
                // while dragging, when the mouse moves in these zones,
                // we automatically scroll up/down
                var localDragPosition = m_ScrollView.contentViewport.WorldToLocal(mousePosition);

                if (localDragPosition.y < k_AutoScrollZoneHeight)
                    m_ScrollView.verticalScroller.ScrollPageUp();
                else if (localDragPosition.y > m_ScrollView.contentViewport.rect.height - k_AutoScrollZoneHeight)
                    m_ScrollView.verticalScroller.ScrollPageDown();
            }
        }

        private void DragOverBottomArea(DragUpdatedEvent dragUpdatedEvent)
        {
            if (DragAndDrop.objectReferences.Any())
            {
                if (editorsElement != null && editorsElement.ContainsPoint(editorsElement.WorldToLocal(dragUpdatedEvent.mousePosition)))
                {
                    AutoScroll(dragUpdatedEvent.mousePosition);
                    return;
                }

                if (editorsElement != null)
                {
                    var lastChild = editorsElement.Children().LastOrDefault();
                    if (lastChild == null)
                        return;

                    editorDragging.HandleDraggingInBottomArea(tracker.activeEditors, bottomAreaDropRectangle, lastChild.layout);
                }
            }
        }

        private void DragPerformInBottomArea(DragPerformEvent dragPerformedEvent)
        {
            if (editorsElement == null)
                return;

            if (editorsElement.ContainsPoint(editorsElement.WorldToLocal(dragPerformedEvent.mousePosition)))
                return;

            var lastChild = editorsElement.Children().LastOrDefault();
            if (lastChild == null)
                return;

            editorDragging.HandleDragPerformInBottomArea(tracker.activeEditors, bottomAreaDropRectangle, lastChild.layout);
        }

        internal virtual Editor GetLastInteractedEditor()
        {
            return lastInteractedEditor;
        }

        protected IPreviewable GetEditorThatControlsPreview(IPreviewable[] editors)
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

        protected IPreviewable[] GetEditorsWithPreviews(Editor[] editors)
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

            if (m_Previews == null) return new IPreviewable[] {};

            foreach (var previewable in m_Previews)
            {
                if (previewable.HasPreviewGUI())
                    editorsWithPreview.Add(previewable);
            }

            return editorsWithPreview.ToArray();
        }

        internal virtual Object GetInspectedObject()
        {
            return m_InspectedObject;
        }

        private void ResetKeyboardControl()
        {
            if (m_ResetKeyboardControl)
            {
                GUIUtility.keyboardControl = 0;
                m_ResetKeyboardControl = false;
            }
        }

        private static bool HasLabel(Object target)
        {
            return HasLabel(target, AssetDatabase.GetAssetPath(target));
        }

        private static bool HasLabel(Object target, string assetPath)
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

        protected virtual bool BeginDrawPreviewAndLabels() { return true; }
        protected virtual void EndDrawPreviewAndLabels(Event evt, Rect rect, Rect dragRect) {}
        private void DrawPreviewAndLabels()
        {
            CreatePreviewables();
            var hasPreview = BeginDrawPreviewAndLabels();

            IPreviewable[] editorsWithPreviews = GetEditorsWithPreviews(tracker.activeEditors);
            IPreviewable previewEditor = GetEditorThatControlsPreview(editorsWithPreviews);

            // Do we have a preview?
            m_HasPreview = previewEditor != null && previewEditor.HasPreviewGUI() && hasPreview;

            m_PreviewResizer.containerMinimumHeightExpanded = m_HasPreview ? k_InspectorPreviewMinTotalHeight : 0;

            Object[] assets = GetInspectedAssets();
            bool hasLabels = assets.Length > 0;
            bool hasBundleName = assets.Any(a => !(a is MonoScript) && AssetDatabase.IsMainAsset(a));

            if (!m_HasPreview && !hasLabels)
                return;

            Event evt = Event.current;

            // Preview / Asset Labels toolbar
            Rect dragRect;
            Rect dragIconRect = new Rect();
            const float dragPadding = 3f;
            const float minDragWidth = 20f;
            Rect rect = EditorGUILayout.BeginHorizontal(GUIContent.none, Styles.preToolbar, GUILayout.Height(kBottomToolbarHeight));
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

                EndDrawPreviewAndLabels(evt, rect, dragRect);
            }
            EditorGUILayout.EndHorizontal();

            // Logic for resizing and collapsing
            float previewSize;
            if (m_HasPreview)
            {
                // If we have a preview we'll use the ResizerControl which handles both resizing and collapsing
                previewSize = m_PreviewResizer.ResizeHandle(position, k_InspectorPreviewMinTotalHeight, k_MinAreaAbovePreview, kBottomToolbarHeight, dragRect);
            }
            else
            {
                // If we don't have a preview, just toggle the collapsible state with a button
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    m_PreviewResizer.ToggleExpanded();

                previewSize = 0;
            }

            // If collapsed, early out
            if (!m_PreviewResizer.GetExpanded())
            {
                if (m_PreviousPreviewExpandedState)
                {
                    UIEventRegistration.MakeCurrentIMGUIContainerDirty();
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
                UIEventRegistration.MakeCurrentIMGUIContainerDirty();
            }

            m_PreviousFooterHeight = previewSize;
            m_PreviousPreviewExpandedState = m_PreviewResizer.GetExpanded();
        }

        private void OnPreviewSelected(object userData, string[] options, int selected)
        {
            IPreviewable[] availablePreviews = userData as IPreviewable[];
            m_SelectedPreview = availablePreviews[selected];
        }

        internal static void VersionControlBar(Editor assetEditor)
        {
            if (!Provider.enabled)
                return;
            var vcsMode = VersionControlSettings.mode;
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

            // Cache AssetList for current selection and
            // figure out which buttons we'll need to show for them.
            VersionControlBarState state;
            if (!m_VersionControlBarState.TryGetValue(assetEditor, out state))
            {
                state = VersionControlBarState.Calculate(assetEditor, asset, connected);
                m_VersionControlBarState.Add(assetEditor, state);
            }
            // Based on that and the current inspector width, we might want to layout the VCS
            // bar in two rows to better fit status label & buttons.
            var approxSpaceForButtons = state.GetButtonCount() * 50;
            var useTwoRows = GUIView.current != null && GUIView.current.position.width * 0.5f < approxSpaceForButtons;

            var lineHeight = Styles.vcsBarStyleOneRow.fixedHeight;
            var barStyle = useTwoRows ? Styles.vcsBarStyleTwoRows : Styles.vcsBarStyleOneRow;
            GUILayout.BeginHorizontal(GUIContent.none, barStyle);
            var barRect = GUILayoutUtility.GetRect(GUIContent.none, barStyle, GUILayout.ExpandWidth(true));

            var icon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;
            var overlayRect = new Rect(barRect.x, barRect.y + 1, 28, 16);
            var iconRect = overlayRect;
            iconRect.x += 6;
            iconRect.width = 16;
            if (icon != null)
                GUI.DrawTexture(iconRect, icon);
            Overlay.DrawOtherOverlay(asset, metaAsset ?? asset, overlayRect);

            if (currentMetaState != currentState)
            {
                if (hasAssetState && hasMetaState)
                    currentState = currentState + "; meta: " + currentMetaState;
                else if (hasAssetState && metaAsset != null) // project settings don't even have .meta files; no point in adding "asset only" for them
                    currentState = currentState + " (asset only)";
                else if (hasMetaState)
                    currentState = currentMetaState + " (meta only)";
            }

            var buttonsRect = barRect;
            buttonsRect.yMin = buttonsRect.yMax - lineHeight;
            var buttonX = VersionControlBarButtons(state, buttonsRect, connected);
            var textRect = barRect;
            textRect.height = lineHeight;
            textRect.xMin += 26;
            if (!useTwoRows)
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

        private static void VersionControlCheckoutHint(Editor assetEditor, bool connected)
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

        internal static void CheckoutForInspector(Object[] assets)
        {
            var inspectorAssets = Provider.GetInspectorAssets(assets);
            Provider.Checkout(inspectorAssets, CheckoutMode.Exact);
        }

        private static void DoRevertUnchanged(object o)
        {
            var al = (AssetList)o;
            Provider.Revert(al, RevertMode.Unchanged);
        }

        private static float VersionControlBarButtons(VersionControlBarState presence, Rect rect, bool connected)
        {
            var buttonX = rect.xMax - 7;

            var buttonRect = rect;
            var buttonStyle = Styles.vcsButtonStyle;
            buttonRect.y += 1;
            buttonRect.height = buttonStyle.CalcSize(Styles.vcsAdd).y;

            if (!connected)
            {
                if (presence.settings)
                {
                    if (VersionControlActionButton(buttonRect, ref buttonX, Styles.vcsSettings))
                        SettingsService.OpenProjectSettings("Project/Version Control");
                }
                return buttonX;
            }

            if (presence.revert && !presence.revertUnchanged)
            {
                // just a simple revert button
                if (VersionControlActionButton(buttonRect, ref buttonX, Styles.vcsRevert))
                {
                    WindowRevert.Open(presence.assets);
                    GUIUtility.ExitGUI();
                }
            }
            else if (presence.revert || presence.revertUnchanged)
            {
                // revert + revert unchanged dropdown button
                if (VersionControlActionDropdownButton(buttonRect, ref buttonX, Styles.vcsRevert,
                    Styles.vcsRevertMenuNames, Styles.vcsRevertMenuActions, presence.assets))
                {
                    if (presence.revert)
                    {
                        WindowRevert.Open(presence.assets);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (presence.checkout)
            {
                if (VersionControlActionButton(buttonRect, ref buttonX, Provider.NeedToCheckOutBoth(presence.editor.target) ? Styles.vcsCheckout : Styles.vcsCheckoutMeta))
                    CheckoutForInspector(presence.editor.targets);
            }
            if (presence.add)
            {
                if (VersionControlActionButton(buttonRect, ref buttonX, Styles.vcsAdd))
                    Provider.Add(presence.assets, true).Wait();
            }
            if (presence.submit)
            {
                if (VersionControlActionButton(buttonRect, ref buttonX, Styles.vcsSubmit))
                    WindowChange.Open(presence.assets, true);
            }
            if (presence.@lock)
            {
                if (VersionControlActionButton(buttonRect, ref buttonX, Styles.vcsLock))
                    Provider.Lock(presence.assets, true).Wait();
            }
            if (presence.unlock)
            {
                if (VersionControlActionButton(buttonRect, ref buttonX, Styles.vcsUnlock))
                    Provider.Lock(presence.assets, false).Wait();
            }

            return buttonX;
        }

        private static bool VersionControlActionDropdownButton(Rect buttonRect, ref float buttonX, GUIContent content, GUIContent[] menuNames, GenericMenu.MenuFunction2[] menuActions, object context)
        {
            var dropdownStyle = Styles.vcsRevertStyle;
            const float kDropDownButtonWidth = 20f;
            buttonRect.width = dropdownStyle.CalcSize(content).x + 6;
            buttonRect.x = buttonX - buttonRect.width;
            buttonX -= buttonRect.width;

            var dropDownRect = buttonRect;
            dropDownRect.xMin = dropDownRect.xMax - kDropDownButtonWidth;

            if (Event.current.type == EventType.MouseDown && dropDownRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                for (var i = 0; i < menuNames.Length; ++i)
                    menu.AddItem(menuNames[i], false, menuActions[i], context);
                menu.DropDown(buttonRect);
                Event.current.Use();
            }
            else
            {
                return GUI.Button(buttonRect, content, dropdownStyle);
            }
            return false;
        }

        private static bool VersionControlActionButton(Rect buttonRect, ref float buttonX, GUIContent content)
        {
            var buttonStyle = Styles.vcsButtonStyle;
            buttonRect.width = buttonStyle.CalcSize(content).x;
            buttonRect.x = buttonX - buttonRect.width;
            buttonX -= buttonRect.width;
            return GUI.Button(buttonRect, content, buttonStyle);
        }

        private void DrawEditors(Editor[] editors)
        {
            if (editorsElement == null)
                return;

            Dictionary<int, IEditorElement> mapping = null;

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
                editors[editorIndex].propertyViewer = this;
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

                if (editorTarget && (editorTarget?.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                    continue;

                try
                {
                    if (mapping == null || !mapping.TryGetValue(editors[editorIndex].target.GetInstanceID(), out var editorContainer))
                    {
                        string editorTitle = editorTarget == null ?
                            "Nothing Selected" :
                            $"{editor.GetType().Name}_{editorTarget.GetType().Name}_{editorTarget.GetInstanceID()}";
                        editorContainer = EditorUIService.instance.CreateEditorElement(editorIndex, this, editorTitle);
                        editorsElement.Add(editorContainer as VisualElement);
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

        private void RestoreVerticalScrollIfNeeded()
        {
            if (m_LastInspectedObjectInstanceID == -1)
                return;
            var inspectedObjectInstanceID = GetInspectedObject()?.GetInstanceID() ?? -1;
            if (inspectedObjectInstanceID == m_LastInspectedObjectInstanceID && inspectedObjectInstanceID != -1)
                m_ScrollView.verticalScroller.value = m_LastVerticalScrollValue;
            m_LastInspectedObjectInstanceID = -1; // reset to make sure the restore occurs once
        }

        private void AddRemovedPrefabComponentElement(GameObject targetGameObject, Component nextInSource, VisualElement element)
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

        private bool ShouldDisplayRemovedComponent(GameObject go, Component comp)
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

        private static void DisplayRemovedComponent(GameObject go, Component comp)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar);
            EditorGUI.RemovedComponentTitlebar(rect, go, comp);
        }

        public bool WasEditorVisible(Editor[] editors, int editorIndex, Object target)
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

        public bool IsMultiEditingSupported(Editor editor, Object target)
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

        public bool ShouldCullEditor(Editor[] editors, int editorIndex)
        {
            if (editors[editorIndex].hideInspector)
                return true;

            Object currentTarget = editors[editorIndex].target;

            // Editors that should always be hidden
            if (currentTarget is ParticleSystemRenderer
                || currentTarget is UnityEngine.VFX.VFXRenderer)
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

        private void DrawSelectionPickerList()
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

        private AssetImporterEditor GetAssetImporter(Editor[] editors)
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
                if (timeNow - m_LastUpdateWhilePlayingAnimation < delayRepaintWhilePlayingAnimation)
                    return false;
                m_LastUpdateWhilePlayingAnimation = timeNow;
            }

            return true;
        }

        private void DrawSplitLine(float y)
        {
            Rect position = new Rect(0, y - Styles.lineSeparatorOffset, m_Pos.width + 1, 1);
            using (new GUI.ColorScope(Styles.lineSeparatorColor * GUI.color))
                GUI.DrawTexture(position, EditorGUIUtility.whiteTexture);
        }

        private Dictionary<int, IEditorElement> ProcessEditorElementsToRebuild(Editor[] editors)
        {
            Dictionary<int, IEditorElement> editorToElementMap = new Dictionary<int, IEditorElement>();
            var currentElements = editorsElement.Children().OfType<IEditorElement>().ToList();
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
                var currentElement = currentElements[previousEditorsIndex];
                var currentEditor = currentElement.editor;

                if (currentEditor == null)
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

                if (currentEditor && ed.target != currentEditor.target)
                {
                    return null;
                }

                currentElement.Reinit(newEditorsIndex);
                editorToElementMap[ed.target.GetInstanceID()] = currentElement;
                ++newEditorsIndex;
                ++previousEditorsIndex;
            }

            // Remove any elements at the end of the PropertyEditor that don't have matching Editors
            for (int j = previousEditorsIndex; j < currentElements.Count; ++j)
            {
                currentElements[j].RemoveFromHierarchy();
            }

            return editorToElementMap;
        }

        [UsedImplicitly, MenuItem(k_AssetPropertiesMenuItemName, validate = true)]
        internal static bool ValidatePropertyEditorOnSelection()
        {
            return Selection.activeObject;
        }

        [UsedImplicitly, MenuItem(k_AssetPropertiesMenuItemName, priority = 99999)]
        internal static void OpenPropertyEditorOnSelection()
        {
            if (Selection.objects.Length == 1)
            {
                foreach (var obj in Selection.objects)
                    OpenPropertyEditor(obj);
            }
            else
            {
                var firstPropertyEditor = OpenPropertyEditor(Selection.objects[0]);
                EditorApplication.delayCall += () =>
                {
                    var dock = firstPropertyEditor.m_Parent as DockArea;
                    for (int i = 1; i < Selection.objects.Length; ++i)
                        dock.AddTab(OpenPropertyEditor(Selection.objects[i], false));
                };
            }
        }

        internal static PropertyEditor OpenPropertyEditor(Object obj, bool showWindow = true)
        {
            if (!obj)
                return null;

            var assetPath = AssetDatabase.GetAssetPath(obj) ?? String.Empty;
            var propertyEditor = CreateInstance<PropertyEditor>();
            propertyEditor.tracker.SetObjectsLockedByThisTracker(new List<Object> { obj });

            if (!String.IsNullOrEmpty(assetPath))
                propertyEditor.m_AssetGUID = AssetDatabase.AssetPathToGUID(assetPath);
            else
                propertyEditor.m_InstanceID = obj.GetInstanceID();
            propertyEditor.m_InspectedObject = obj;

            propertyEditor.SetTitle(obj);
            if (showWindow)
                ShowPropertyEditorWindow(propertyEditor);
            return propertyEditor;
        }

        private static void ShowPropertyEditorWindow(PropertyEditor propertyEditor)
        {
            propertyEditor.Show();

            // Offset new window instance.
            var pos = s_LastPropertyEditor ? s_LastPropertyEditor.position : propertyEditor.m_Parent.screenPosition;
            propertyEditor.position = new Rect(pos.x + 30, pos.y + 30, propertyEditor.position.width, propertyEditor.position.height);
            s_LastPropertyEditor = propertyEditor;
        }

        [ShortcutManagement.Shortcut("PropertyEditor/OpenMouseOver")]
        static void OpenHoveredItemPropertyEditor(ShortcutManagement.ShortcutArguments args)
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            if (windows.Length == 0)
                return;

            foreach (var w in windows)
            {
                var pso = w as IPropertySourceOpener;
                if (pso == null)
                    continue;

                if (!w.m_Parent || !w.m_Parent.window)
                    continue;

                if (pso.hoveredObject)
                    OpenPropertyEditor(pso.hoveredObject);
            }
        }
    }
}
