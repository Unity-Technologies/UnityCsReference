using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class Builder : BuilderPaneWindow, IBuilderViewportWindow, IHasCustomMenu
    {
        BuilderSelection m_Selection;

        BuilderToolbar m_Toolbar;
        BuilderLibrary m_Library;
        BuilderViewport m_Viewport;
        BuilderInspector m_Inspector;
        BuilderUxmlPreview m_UxmlPreview;
        BuilderUssPreview m_UssPreview;
        BuilderHierarchy m_Hierarchy;

        TwoPaneSplitView m_MiddleSplitView;

        HighlightOverlayPainter m_HighlightOverlayPainter;

        public BuilderSelection selection => m_Selection;
        public BuilderViewport viewport => m_Viewport;
        public BuilderToolbar toolbar => m_Toolbar;
        public VisualElement documentRootElement => m_Viewport.documentRootElement;
        public BuilderCanvas canvas => m_Viewport.canvas;
        public BuilderHierarchy hierarchy => m_Hierarchy;

        internal override bool liveReloadPreferenceDefault => true;

        public bool codePreviewVisible
        {
            get { return document.codePreviewVisible; }
            set
            {
                document.codePreviewVisible = value;
                UpdatePreviewsVisibility();
            }
        }

        void UpdatePreviewsVisibility()
        {
            if (codePreviewVisible)
            {
                m_MiddleSplitView.UnCollapse();
            }
            else
            {
                m_MiddleSplitView.CollapseChild(1);
            }
        }

        public HighlightOverlayPainter highlightOverlayPainter => m_HighlightOverlayPainter;

        [MenuItem(BuilderConstants.BuilderMenuEntry)]
        public static Builder ShowWindow()
        {
            return GetWindow<Builder>();
        }

        public static Builder ActiveWindow
        {
            get
            {
                var builderWindows =  Resources.FindObjectsOfTypeAll<Builder>();
                if (builderWindows.Length > 0)
                {
                    return builderWindows.First();
                }

                return null;
            }
        }

        static GUIContent s_WarningContent;

        public static void ShowWarning(string message)
        {
            if (s_WarningContent == null)
                s_WarningContent = new GUIContent(string.Empty, EditorGUIUtility.FindTexture("console.warnicon"));

            s_WarningContent.text = message;
            ActiveWindow.ShowNotification(s_WarningContent, 4);
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;
            titleContent = GetLocalizedTitleContent();
            saveChangesMessage = BuilderConstants.SaveDialogSaveChangesPromptMessage;

            // Load assets.
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/Builder.uxml");

            // Load templates.
            builderTemplate.CloneTree(root);

            // Create overlay painter.
            m_HighlightOverlayPainter = new HighlightOverlayPainter();

            // Fetch the tooltip previews.
            var styleSheetsPaneTooltipPreview = root.Q<BuilderTooltipPreview>("stylesheets-pane-tooltip-preview");
            var libraryTooltipPreview = root.Q<BuilderTooltipPreview>("library-tooltip-preview");

            // Create selection.
            m_Selection = new BuilderSelection(root, this);

            // Create Element Context Menu Manipulator
            var contextMenuManipulator = new BuilderElementContextMenu(this, selection);

            // Create viewport first.
            m_Viewport = new BuilderViewport(this, selection, contextMenuManipulator);
            selection.documentRootElement = m_Viewport.documentRootElement;
            var overlayHelper = viewport.Q<OverlayPainterHelperElement>();
            overlayHelper.painter = m_HighlightOverlayPainter;

            // Create the rest of the panes.
            var classDragger = new BuilderClassDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker);
            var styleSheetsDragger = new BuilderStyleSheetsDragger(this, root, selection);
            var styleSheetsPane = new BuilderStyleSheets(this, m_Viewport, selection, classDragger, styleSheetsDragger, m_HighlightOverlayPainter, styleSheetsPaneTooltipPreview);
            var hierarchyDragger = new BuilderHierarchyDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker) { builderStylesheetRoot = styleSheetsPane.container };

            m_Hierarchy = new BuilderHierarchy(this, m_Viewport, selection, classDragger, hierarchyDragger, contextMenuManipulator, m_HighlightOverlayPainter);

            var libraryDragger = new BuilderLibraryDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker, hierarchy.container, libraryTooltipPreview) { builderStylesheetRoot = styleSheetsPane.container };
            m_Viewport.viewportDragger.builderHierarchyRoot = hierarchy.container;
            m_Library = new BuilderLibrary(this, m_Viewport, selection, libraryDragger, libraryTooltipPreview);
            m_Inspector = new BuilderInspector(this, selection, m_HighlightOverlayPainter);
            m_Toolbar = new BuilderToolbar(this, selection, m_Viewport, hierarchy, m_Library, m_Inspector, libraryTooltipPreview);
            m_UxmlPreview = new BuilderUxmlPreview(this);
            m_UssPreview = new BuilderUssPreview(this, selection);
            root.Q("viewport").Add(m_Viewport);
            m_Viewport.toolbar.Add(m_Toolbar);
            root.Q("library").Add(m_Library);
            root.Q("style-sheets").Add(styleSheetsPane);
            root.Q("hierarchy").Add(hierarchy);
            root.Q("uxml-preview").Add(m_UxmlPreview);
            root.Q("uss-preview").Add(m_UssPreview);
            root.Q("inspector").Add(m_Inspector);

            // Init selection.
            selection.AssignNotifiers(new IBuilderSelectionNotifier[]
            {
                document,
                m_Viewport,
                styleSheetsPane,
                hierarchy,
                m_Inspector,
                m_Library,
                m_UxmlPreview,
                m_UssPreview,
                m_Toolbar,
                m_Viewport.parentTracker,
                m_Viewport.resizer,
                m_Viewport.mover,
                m_Viewport.anchorer,
                m_Viewport.selectionIndicator
            });

            // Command Handler
            commandHandler.RegisterPane(styleSheetsPane);
            commandHandler.RegisterPane(hierarchy);
            commandHandler.RegisterPane(m_Viewport);
            commandHandler.RegisterToolbar(m_Toolbar);

            m_MiddleSplitView = rootVisualElement.Q<TwoPaneSplitView>("middle-column");
            m_MiddleSplitView.RegisterCallback<GeometryChangedEvent>(OnFirstDisplay);

            OnEnableAfterAllSerialization();
        }

        void OnFirstDisplay(GeometryChangedEvent evt)
        {
            UpdatePreviewsVisibility();

            m_MiddleSplitView.UnregisterCallback<GeometryChangedEvent>(OnFirstDisplay);
        }

        public override void OnEnableAfterAllSerialization()
        {
            // Perform post-serialization functions.
            document.OnAfterBuilderDeserialize(m_Viewport.documentRootElement);
            m_Toolbar.OnAfterBuilderDeserialize();
            m_Library.OnAfterBuilderDeserialize();
            m_Inspector.OnAfterBuilderDeserialize();

            // Restore selection.
            selection.RestoreSelectionFromDocument(m_Viewport.sharedStylesAndDocumentElement);

            // We claim the change is coming from the Document because we don't
            // want the document hasUnsavedChanges flag to be set at this time.
            m_Selection.NotifyOfStylingChange(document);
            m_Selection.NotifyOfHierarchyChange(document);
        }

        public override void LoadDocument(VisualTreeAsset asset, bool unloadAllSubdocuments = true)
        {
            m_Toolbar.LoadDocument(asset, unloadAllSubdocuments);
        }

        public override bool NewDocument(bool checkForUnsavedChanges = true, bool unloadAllSubdocuments = true)
        {
            return m_Toolbar.NewDocument(checkForUnsavedChanges, unloadAllSubdocuments);
        }

        public override void SaveChanges()
        {
            m_Toolbar.SaveDocument(false);

            if (!document.hasUnsavedChanges)
                base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            // Restore UXML and USS assets from backup
            document.RestoreAssetsFromBackup();

            // If the asset is not saved yet then reset to blank document
            if (string.IsNullOrEmpty(document.uxmlFileName))
            {
                document.NewDocument(m_Viewport.documentRootElement);
            }

            base.DiscardChanges();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            minSize = new Vector2(200, 200);
            SetTitleContent(BuilderConstants.BuilderWindowTitle, BuilderConstants.BuilderWindowIcon);

            if (rootVisualElement.panel != null)
                SetupPanel();
            // Sometimes, the panel is not already set
            else
                rootVisualElement.RegisterCallback<AttachToPanelEvent>(e => SetupPanel());
        }

        void SetupPanel()
        {
            var panel = rootVisualElement.panel as BaseVisualElementPanel;
            var styleUpdater = panel.GetUpdater(VisualTreeUpdatePhase.Styles) as VisualTreeStyleUpdater;

            styleUpdater.traversal = new BuilderVisualTreeStyleUpdaterTraversal(m_Viewport.documentRootElement);
        }

        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as VisualTreeAsset;
            if (asset == null)
                return false;

            // Special case: we use a magic value to distinguish between opening in the UI Builder and opening in the
            // IDE.
            if (line == BuilderConstants.OpenInIDELineNumber)
                return false;

            var builderWindow = ActiveWindow;

            if (builderWindow == null)
            {
                builderWindow = ShowWindow();
            }
            else
            {
                builderWindow.Focus();
            }

            if (builderWindow.document.visualTreeAsset != asset)
            {
                builderWindow.LoadDocument(asset);
            }

            return true;
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Reset UI Builder Layout"), false, () =>
            {
                ClearPersistentViewData();
                m_Parent.Reload(this);

                var window = GetWindow<Builder>();
                window.RepaintImmediately();
                window.m_Viewport.FitCanvas();
            });
        }
    }
}
