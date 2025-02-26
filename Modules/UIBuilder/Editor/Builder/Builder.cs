// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    sealed class Builder : BuilderPaneWindow, IBuilderViewportWindow, IHasCustomMenu, IDisposable
    {
        static Builder()
        {
            EditorApplication.fileMenuSaved += () =>
            {
                var builder = ActiveWindow;

                if (builder != null)
                {
                    // Make sure changes are committed before saving the file.
                    builder.inspector.BeforeSelectionChanged();

                    if (builder.document.hasUnsavedChanges)
                        builder.SaveChanges();
                }
            };
        }

        BuilderSelection m_Selection;

        BuilderToolbar m_Toolbar;
        BuilderLibrary m_Library;
        BuilderViewport m_Viewport;
        BuilderInspector m_Inspector;
        BuilderUxmlPreview m_UxmlPreview;
        BuilderUssPreview m_UssPreview;
        BuilderHierarchy m_Hierarchy;
        BuilderStyleSheets m_StyleSheets;
        BuilderBindingsCache m_BindingsCache;

        TwoPaneSplitView m_MiddleSplitView;

        HighlightOverlayPainter m_HighlightOverlayPainter;

        public BuilderSelection selection => m_Selection;
        public BuilderViewport viewport => m_Viewport;
        public BuilderToolbar toolbar => m_Toolbar;
        public VisualElement documentRootElement => m_Viewport.documentRootElement;
        public BuilderCanvas canvas => m_Viewport.canvas;
        public BuilderInspector inspector => m_Inspector;
        public BuilderHierarchy hierarchy => m_Hierarchy;
        public BuilderStyleSheets styleSheets => m_StyleSheets;

        internal override bool liveReloadPreferenceDefault => true;
        internal override BindingLogLevel defaultBindingLogLevel => BindingLogLevel.None;

        readonly Action m_UnregisterBuilderLibraryContentProcessors = BuilderLibraryContent.UnregisterProcessors;

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

        public static GUIContent lastWarning => s_WarningContent;

        bool m_IsInUndoRedo;
        internal bool isInUndoRedo => m_IsInUndoRedo;

        public static void ShowWarning(string message)
        {
            if (s_WarningContent == null)
                s_WarningContent = new GUIContent(string.Empty, EditorGUIUtility.FindTexture("console.warnicon"));

            s_WarningContent.text = message;
            ActiveWindow.ShowNotification(s_WarningContent, 4);
        }

        public static void ResetWarning()
        {
            s_WarningContent = null;
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;
            titleContent = GetLocalizedTitleContent();
            saveChangesMessage = BuilderConstants.SaveDialogSaveChangesPromptMessage;

            m_BindingsCache = new BuilderBindingsCache();
            m_BindingsCache.onBindingBecameUnresolved += OnEnableAfterAllSerialization;

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
            m_Viewport = new BuilderViewport(this, selection, contextMenuManipulator, m_BindingsCache);
            selection.documentRootElement = m_Viewport.documentRootElement;
            var overlayHelper = viewport.Q<OverlayPainterHelperElement>();
            overlayHelper.painter = m_HighlightOverlayPainter;

            // Create the rest of the panes.
            var classDragger = new BuilderClassDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker);
            var styleSheetsDragger = new BuilderStyleSheetsDragger(this, root, selection);
            m_StyleSheets = new BuilderStyleSheets(this, m_Viewport, selection, classDragger, styleSheetsDragger, m_HighlightOverlayPainter, styleSheetsPaneTooltipPreview);
            var hierarchyDragger = new BuilderHierarchyDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker) { builderStylesheetRoot = m_StyleSheets.container };

            m_Hierarchy = new BuilderHierarchy(this, m_Viewport, selection, classDragger, hierarchyDragger, contextMenuManipulator, m_HighlightOverlayPainter);

            var libraryDragger = new BuilderLibraryDragger(this, root, selection, m_Viewport, m_Viewport.parentTracker, hierarchy.container, libraryTooltipPreview) { builderStylesheetRoot = m_StyleSheets.container };
            m_Viewport.viewportDragger.builderHierarchyRoot = hierarchy.container;
            m_Library = new BuilderLibrary(this, m_Viewport, selection, libraryDragger, libraryTooltipPreview);
            m_Inspector = new BuilderInspector(this, selection, m_HighlightOverlayPainter, m_BindingsCache, m_Viewport.notifications);
            m_Toolbar = new BuilderToolbar(this, selection, m_Viewport, hierarchy, m_Library, m_Inspector, libraryTooltipPreview);
            m_UxmlPreview = new BuilderUxmlPreview(this);
            m_UssPreview = new BuilderUssPreview(this, selection);

            root.Q("viewport").Add(m_Viewport);
            m_Viewport.toolbar.Add(m_Toolbar);
            root.Q("library").Add(m_Library);
            root.Q("style-sheets").Add(m_StyleSheets);
            root.Q("hierarchy").Add(hierarchy);
            root.Q("uxml-preview").Add(m_UxmlPreview);
            root.Q("uss-preview").Add(m_UssPreview);
            root.Q("inspector").Add(m_Inspector);

            // Init selection.
            selection.AssignNotifiers(new IBuilderSelectionNotifier[]
            {
                document,
                m_Viewport,
                m_StyleSheets,
                hierarchy,
                m_Inspector,
                m_Library,
                m_UxmlPreview,
                m_UssPreview,
                m_Toolbar,
                m_Viewport.parentTracker,
                m_Viewport.resizer,
                m_Viewport.mover,
                m_Viewport.selectionIndicator,
                m_Inspector.preview
            });

            // Command Handler
            commandHandler.RegisterPane(m_StyleSheets);
            commandHandler.RegisterPane(hierarchy);
            commandHandler.RegisterPane(m_Viewport);
            commandHandler.RegisterToolbar(m_Toolbar);

            m_MiddleSplitView = rootVisualElement.Q<TwoPaneSplitView>("middle-column");
            m_MiddleSplitView.RegisterCallback<GeometryChangedEvent>(OnFirstDisplay);

            OnEnableAfterAllSerialization();
            closing += m_UnregisterBuilderLibraryContentProcessors;
        }

        // Message received when we dock/undock the window.
        // ReSharper disable once UnusedMember.Local
        void OnAddedAsTab()
        {
            m_BindingsCache?.Clear();
        }

        private void UpdateBindingsCache()
        {
            m_BindingsCache?.UpdateCache(rootVisualElement.panel as Panel);
        }

        void OnFirstDisplay(GeometryChangedEvent evt)
        {
            UpdatePreviewsVisibility();

            m_MiddleSplitView.UnregisterCallback<GeometryChangedEvent>(OnFirstDisplay);
        }

        public override void OnEnableAfterAllSerialization()
        {
            m_BindingsCache?.Clear();

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

        internal override void OnUndoRedo()
        {
            m_IsInUndoRedo = true;
            OnEnableAfterAllSerialization();
            m_IsInUndoRedo = false;
        }

        public override bool LoadDocument(VisualTreeAsset asset, bool unloadAllSubdocuments = true)
        {
            return m_Toolbar.LoadDocument(asset, unloadAllSubdocuments);
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

        public bool ReloadDocument()
        {
            return m_Toolbar.ReloadDocument();
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
                rootVisualElement.RegisterCallback<AttachToPanelEvent>(SetupPanelAttach);
        }

        void SetupPanelAttach(AttachToPanelEvent evt)
        {
            SetupPanel();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Dispose();
        }

        public void Dispose()
        {
            closing -= m_UnregisterBuilderLibraryContentProcessors;
            rootVisualElement.UnregisterCallback<AttachToPanelEvent>(SetupPanelAttach);
            rootVisualElement.Clear();
            m_Inspector.Dispose();
        }

        private void Update()
        {
            UpdateBindingsCache();
            m_Inspector.UpdateBoundFields();
        }

        void SetupPanel()
        {
            var panel = rootVisualElement.panel as BaseVisualElementPanel;
            var styleUpdater = panel.GetUpdater(VisualTreeUpdatePhase.Styles) as VisualTreeStyleUpdater;

            styleUpdater.traversal = new BuilderVisualTreeStyleUpdaterTraversal(m_Viewport.documentRootElement);

            // We don't want the Builder to live reload anything except text elements.
            panel.liveReloadSystem.enabledTrackers = LiveReloadTrackers.Text;
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

            bool validAsset;
            if (builderWindow.document.visualTreeAsset != asset)
            {
                validAsset = builderWindow.LoadDocument(asset);
            }
            else
            {
                validAsset = builderWindow.ReloadDocument();
            }

            if (!validAsset)
            {
                builderWindow.NewDocument();
                return false; // Let user open the asset in the IDE.
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
                window.m_Viewport.ResizeCanvasToFitViewport();
            });
        }
    }
}
