using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Toolbar = UnityEditor.UIElements.Toolbar;
using System;

namespace Unity.UI.Builder
{
    internal class BuilderToolbar : VisualElement, IBuilderAssetModificationProcessor, IBuilderSelectionNotifier
    {
        public const string FitCanvasButtonName = "fit-canvas-button";
        public const string PreviewToggleName = "preview-button";
        public const string BreadcrumbsToolbarName = "breadcrumbs-toolbar";
        public const string BreadcrumbsName = "breadcrumbs-view";

        BuilderPaneWindow m_PaneWindow;
        BuilderSelection m_Selection;
        BuilderViewport m_Viewport;
        BuilderExplorer m_Explorer;
        BuilderLibrary m_Library;
        BuilderInspector m_Inspector;
        BuilderTooltipPreview m_TooltipPreview;

        ToolbarMenu m_FileMenu;
        ToolbarMenu m_ZoomMenu;
        ToolbarButton m_FitCanvasButton;
        ToolbarMenu m_CanvasThemeMenu;
        ToolbarMenu m_SettingsMenu;
        Toolbar m_BreadcrumbsToolbar;
        ToolbarBreadcrumbs m_Breadcrumbs = new ToolbarBreadcrumbs();

        string m_LastSavePath = "Assets";

        string m_BuilderPackageVersion;

        BuilderDocument document
        {
            get { return m_PaneWindow.document; }
        }

        public BuilderToolbar(
            BuilderPaneWindow paneWindow,
            BuilderSelection selection,
            BuilderViewport viewport,
            BuilderExplorer explorer,
            BuilderLibrary library,
            BuilderInspector inspector,
            BuilderTooltipPreview tooltipPreview)
        {
            m_PaneWindow = paneWindow;
            m_Selection = selection;
            m_Viewport = viewport;
            m_Explorer = explorer;
            m_Library = library;
            m_Inspector = inspector;
            m_TooltipPreview = tooltipPreview;

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderToolbar.uxml");
            template.CloneTree(this);

            // File Menu
            m_FileMenu = this.Q<ToolbarMenu>("file-menu");
            SetUpFileMenu();

            // Zoom Menu
            m_ZoomMenu = this.Q<ToolbarMenu>("zoom-menu");
            SetUpZoomMenu();

            // Fit canvas
            m_FitCanvasButton = this.Q<ToolbarButton>(FitCanvasButtonName);
            m_FitCanvasButton.clickable.clicked += () => m_Viewport.FitCanvas();

            // Preview Button
            var previewButton = this.Q<ToolbarToggle>(PreviewToggleName);
            previewButton.RegisterValueChangedCallback(TogglePreviewMode);

            m_CanvasThemeMenu = this.Q<ToolbarMenu>("canvas-theme-menu");
            SetUpCanvasThemeMenu();
            ChangeCanvasTheme(document.currentCanvasTheme);
            UpdateCanvasThemeMenuStatus();
            SetViewportSubTitle();

            // Track unsaved changes state change.
            SetCanvasTitle();

            m_SettingsMenu = this.Q<ToolbarMenu>("settings-menu");
            SetupSettingsMenu();

            // Breadcrumbs & BreadCrumbs Toolbar
            m_BreadcrumbsToolbar = this.Q<Toolbar>(BreadcrumbsToolbarName);
            m_Breadcrumbs = this.Q<ToolbarBreadcrumbs>(BreadcrumbsName);
            SetToolbarBreadCrumbs();

            // Get Builder package version.
            var packageInfo = PackageInfo.FindForAssetPath("Packages/" + BuilderConstants.BuilderPackageName);
            if (packageInfo == null)
                m_BuilderPackageVersion = null;
            else
                m_BuilderPackageVersion = packageInfo.version;

            RegisterCallback<AttachToPanelEvent>(RegisterCallbacks);
        }

        void RegisterCallbacks(AttachToPanelEvent evt)
        {
            RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacks);
            BuilderAssetModificationProcessor.Register(this);
        }

        void UnregisterCallbacks(DetachFromPanelEvent evt)
        {
            UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacks);
            BuilderAssetModificationProcessor.Unregister(this);
        }

        public void SetToolbarBreadCrumbs()
        {
            m_Breadcrumbs.Clear();
            var allHierarchyDocuments = new List<BuilderDocumentOpenUXML>();
            var allOpenDocuments = m_PaneWindow.document.openUXMLFiles;

            foreach (var Doc in allOpenDocuments)
                if (Doc.openSubDocumentParentIndex > -1 || allOpenDocuments.IndexOf(Doc) == 0)
                    allHierarchyDocuments.Add(Doc);

            if (allHierarchyDocuments.Count == 1)
            {
                m_BreadcrumbsToolbar.style.display = DisplayStyle.None;
                return;
            }
            
            m_BreadcrumbsToolbar.style.display = DisplayStyle.Flex;

            foreach (var Doc in allHierarchyDocuments)
            {
                string docName = BreadcrumbFileName(Doc);
                Action onBreadCrumbClick = () => document.GoToSubdocument(m_Viewport.documentRootElement, m_PaneWindow, Doc);
                bool clickedOnSameDocument = document.activeOpenUXMLFile == Doc;
                m_Breadcrumbs.PushItem(docName, clickedOnSameDocument ? null : onBreadCrumbClick);
            }
        }

        string BreadcrumbFileName(BuilderDocumentOpenUXML breadDoc)
        {
            var newFileName = breadDoc.uxmlFileName;

            if (string.IsNullOrEmpty(newFileName))
                newFileName = BuilderConstants.ToolbarUnsavedFileDisplayMessage;
            else if (breadDoc.hasUnsavedChanges)
                newFileName = newFileName + BuilderConstants.ToolbarUnsavedFileSuffix;

            return newFileName;
        }

        public void OnAssetChange() { }

        public AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            if (IsFileActionCompatible(assetPath, "delete"))
                return AssetDeleteResult.DidNotDelete;
            else
                return AssetDeleteResult.FailedDelete;
        }

        public AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            var sourcePathDirectory = Path.GetDirectoryName(sourcePath);
            var destinationPathDirectory =  Path.GetDirectoryName(destinationPath);

            var actionName = sourcePathDirectory.Equals(destinationPathDirectory) ? "rename" : "move";
            if (IsFileActionCompatible(sourcePath, actionName))
                return AssetMoveResult.DidNotMove;
            else
                return AssetMoveResult.FailedMove;
        }

        bool IsFileActionCompatible(string assetPath, string actionName)
        {
            if (assetPath.Equals(document.uxmlPath) || document.ussPaths.Contains(assetPath))
            {
                var fileName = Path.GetFileName(assetPath);
                var acceptAction = BuilderDialogsUtility.DisplayDialog(BuilderConstants.ErrorDialogNotice,
                    string.Format(BuilderConstants.ErrorIncompatibleFileActionMessage, actionName, fileName),
                    BuilderConstants.DialogDiscardOption,
                    string.Format(BuilderConstants.DialogAbortActionOption, actionName.ToPascalCase()));

                if (acceptAction)
                    NewDocument(false);

                return acceptAction;
            }

            return true;
        }

        string OpenLoadFileDialog(string title, string extension)
        {
            var loadPath = EditorUtility.OpenFilePanel(
                title,
                Path.GetDirectoryName(m_LastSavePath),
                extension);

            return loadPath;
        }

        public bool NewDocument(bool checkForUnsavedChanges = true, bool unloadAllSubdocuments = true)
        {
            if (checkForUnsavedChanges && !document.CheckForUnsavedChanges())
                return false;

            if (unloadAllSubdocuments)
                document.GoToRootDocument(m_Viewport.documentRootElement, m_PaneWindow, true);

            m_Selection.ClearSelection(null);

            document.NewDocument(m_Viewport.documentRootElement);

            m_Viewport.ResetView();
            m_Inspector?.canvasInspector.Refresh();

            m_Selection.NotifyOfHierarchyChange(document);
            m_Selection.NotifyOfStylingChange(document);

            m_Library?.ResetCurrentlyLoadedUxmlStyles();

            SetCanvasTitle();

            return true;
        }

        void NewTestDocument()
        {
            if (!document.CheckForUnsavedChanges())
                return;

            var testAsset =
                BuilderConstants.UIBuilderPackagePath +
                "/SampleDocument/BuilderSampleCanvas.uxml";
            var originalAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(testAsset);
            LoadDocument(originalAsset);
        }

        void NewTestVariablesDocument()
        {
            if (!document.CheckForUnsavedChanges())
                return;

            var testAsset =
                BuilderConstants.UIBuilderPackagePath +
                "/SampleDocument/BuilderVariableSampleCanvas.uxml";
            var originalAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(testAsset);
            LoadDocument(originalAsset);
        }

        internal void SaveDocument(bool isSaveAs)
        {
            var viewportWindow = m_PaneWindow as IBuilderViewportWindow;
            if (viewportWindow == null)
                return;

            // Set asset.
            var userConfirmed = document.SaveNewDocument(viewportWindow.documentRootElement, isSaveAs, out var needFullRefresh);
            if (!userConfirmed)
                return;

            // Update any uses out there of the currently edited and saved USS.
            RetainedMode.FlagStyleSheetChange();

            // Save last save path.
            m_LastSavePath = Path.GetDirectoryName(document.uxmlPath);

            // Set doc field value.
            SetCanvasTitle();

            // Only updating UI to remove "*" from file names.
            m_Selection.ResetUnsavedChanges();

            if (needFullRefresh)
                m_PaneWindow.OnEnableAfterAllSerialization();
            else
                m_Selection.NotifyOfHierarchyChange(document);
        }

        public void OnAfterBuilderDeserialize()
        {
            SetCanvasTitle();
            SetViewportSubTitle();
            ChangeCanvasTheme(document.currentCanvasTheme);
            SetToolbarBreadCrumbs();
        }

        public bool LoadDocument(VisualTreeAsset visualTreeAsset, bool unloadAllSubdocuments = true, bool assetModifiedExternally = false)
        {
            if (!document.CheckForUnsavedChanges(assetModifiedExternally))
                return false;
            
            if (unloadAllSubdocuments)
                document.GoToRootDocument(m_Viewport.documentRootElement, m_PaneWindow);

            LoadDocumentInternal(visualTreeAsset);

            return true;
        }

        void LoadDocumentInternal(VisualTreeAsset visualTreeAsset)
        {
            m_Selection.ClearSelection(null);

            document.LoadDocument(visualTreeAsset, m_Viewport.documentRootElement);

            m_Viewport.SetViewFromDocumentSetting();
            m_Inspector?.canvasInspector.Refresh();

            m_Selection.NotifyOfStylingChange(document);
            m_Selection.NotifyOfHierarchyChange(document);

            m_Library?.ResetCurrentlyLoadedUxmlStyles();

            try
            {
                m_LastSavePath = Path.GetDirectoryName(document.uxmlPath);
            }
            catch
            {
                m_LastSavePath = "Assets";
            }

            OnAfterBuilderDeserialize();
        }

        void SetUpFileMenu()
        {
            m_FileMenu.menu.AppendAction("New", a =>
            {
                NewDocument();
            });

            if (Unsupported.IsDeveloperMode())
            {
                m_FileMenu.menu.AppendAction("New (Test)", a =>
                {
                    NewTestDocument();
                });

                m_FileMenu.menu.AppendAction("New (Test Variables)", a =>
                {
                    NewTestVariablesDocument();
                });
            }

            m_FileMenu.menu.AppendAction("Open...", a =>
            {
                var path = OpenLoadFileDialog(BuilderConstants.ToolbarLoadUxmlDialogTitle, BuilderConstants.Uxml);
                if (string.IsNullOrEmpty(path))
                    return;

                var appPath = Application.dataPath;
                if (path.StartsWith(appPath))
                {
                    path = "Assets/" + path.Substring(appPath.Length);
                }
                else
                {
                    Debug.LogError(BuilderConstants.ToolbarCannotLoadUxmlOutsideProjectMessage);
                    return;
                }

                var asset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(path);
                if (asset == null)
                {
                    Debug.LogError(BuilderConstants.ToolbarSelectedAssetIsInvalidMessage);
                    return;
                }

                LoadDocument(asset);
            });

            m_FileMenu.menu.AppendSeparator();

            m_FileMenu.menu.AppendAction("Save", a =>
            {
                SaveDocument(false);
            });
            m_FileMenu.menu.AppendAction("Save As...", a =>
            {
                SaveDocument(true);
            });
        }

        static string GetTextForZoomScale(float scale)
        {
            return (int) (scale * 100) + "%";
        }

        void UpdateZoomMenuText()
        {
            m_ZoomMenu.text = GetTextForZoomScale(m_Viewport.zoomScale);
        }

        void SetUpZoomMenu()
        {
            foreach (var zoomScale in m_Viewport.zoomer.zoomScaleValues)
            {
                m_ZoomMenu.menu.AppendAction(GetTextForZoomScale(zoomScale), a => { m_Viewport.zoomScale = zoomScale; }
                    , a => (m_Viewport.zoomScale == zoomScale) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
            m_Viewport.canvas.RegisterCallback<GeometryChangedEvent>(e => UpdateZoomMenuText());
            UpdateZoomMenuText();
        }

        void SetUpCanvasThemeMenu()
        {
            m_CanvasThemeMenu.menu.AppendAction("Default", a =>
                {
                    ChangeCanvasTheme(BuilderDocument.CanvasTheme.Default);
                    UpdateCanvasThemeMenuStatus();
                },
                a => document.currentCanvasTheme == BuilderDocument.CanvasTheme.Default
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);

            m_CanvasThemeMenu.menu.AppendAction("Dark", a =>
                {
                    ChangeCanvasTheme(BuilderDocument.CanvasTheme.Dark);
                    UpdateCanvasThemeMenuStatus();
                },
                a => document.currentCanvasTheme == BuilderDocument.CanvasTheme.Dark
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);

            m_CanvasThemeMenu.menu.AppendAction("Light", a =>
                {
                    ChangeCanvasTheme(BuilderDocument.CanvasTheme.Light);
                    UpdateCanvasThemeMenuStatus();
                },
                a => document.currentCanvasTheme == BuilderDocument.CanvasTheme.Light
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);

            m_CanvasThemeMenu.menu.AppendAction("Runtime", a =>
            {
                ChangeCanvasTheme(BuilderDocument.CanvasTheme.Runtime);
                UpdateCanvasThemeMenuStatus();
            },
                a => document.currentCanvasTheme == BuilderDocument.CanvasTheme.Runtime
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);

        }

        void ChangeCanvasTheme(BuilderDocument.CanvasTheme theme)
        {
            ApplyCanvasTheme(m_Viewport.sharedStylesAndDocumentElement, theme);
            ApplyCanvasBackground(m_Viewport.canvas.defaultBackgroundElement, theme);
            ApplyCanvasTheme(m_TooltipPreview, theme);
            ApplyCanvasBackground(m_TooltipPreview, theme);

            document.ChangeDocumentTheme(m_Viewport.documentRootElement, theme);
            m_Inspector?.selection.NotifyOfStylingChange(null, null, BuilderStylingChangeType.RefreshOnly);
        }

        void ApplyCanvasTheme(VisualElement element, BuilderDocument.CanvasTheme theme)
        {
            if (element == null)
                return;

            // Find the runtime stylesheet.
            var runtimeStyleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.RuntimeThemeUSSPath);
            if (runtimeStyleSheet == null)
                runtimeStyleSheet = UIElementsEditorUtility.s_DefaultCommonLightStyleSheet;

            element.styleSheets.Remove(UIElementsEditorUtility.s_DefaultCommonDarkStyleSheet);
            element.styleSheets.Remove(UIElementsEditorUtility.s_DefaultCommonLightStyleSheet);
            element.styleSheets.Remove(runtimeStyleSheet);
            m_Viewport.canvas.defaultBackgroundElement.style.display = DisplayStyle.Flex;

            StyleSheet themeStyleSheet = null;
            m_Viewport.canvas.checkerboardBackgroundElement.style.display = DisplayStyle.None;

            switch (theme)
            {
                case BuilderDocument.CanvasTheme.Dark:
                    themeStyleSheet = UIElementsEditorUtility.s_DefaultCommonDarkStyleSheet;
                    break;
                case BuilderDocument.CanvasTheme.Light:
                    themeStyleSheet = UIElementsEditorUtility.s_DefaultCommonLightStyleSheet;
                    break;
                case BuilderDocument.CanvasTheme.Runtime:
                    themeStyleSheet = runtimeStyleSheet;
                    m_Viewport.canvas.defaultBackgroundElement.style.display = DisplayStyle.None;
                    m_Viewport.canvas.checkerboardBackgroundElement.style.display = DisplayStyle.Flex;
                    break;
                case BuilderDocument.CanvasTheme.Default:
                    themeStyleSheet = null;
                    break;
            }

            if (themeStyleSheet != null)
                element.styleSheets.Add(themeStyleSheet);
        }

        void ApplyCanvasBackground(VisualElement element, BuilderDocument.CanvasTheme theme)
        {
            if (element == null)
                return;

            element.RemoveFromClassList(BuilderConstants.CanvasContainerDarkStyleClassName);
            element.RemoveFromClassList(BuilderConstants.CanvasContainerLightStyleClassName);
            element.RemoveFromClassList(BuilderConstants.CanvasContainerRuntimeStyleClassName);

            switch (theme)
            {
                case BuilderDocument.CanvasTheme.Dark:
                    element.AddToClassList(BuilderConstants.CanvasContainerDarkStyleClassName);
                    break;
                case BuilderDocument.CanvasTheme.Light:
                    element.AddToClassList(BuilderConstants.CanvasContainerLightStyleClassName);
                    break;
                case BuilderDocument.CanvasTheme.Runtime:
                    element.AddToClassList(BuilderConstants.CanvasContainerRuntimeStyleClassName);
                    break;
                case BuilderDocument.CanvasTheme.Default:
                    string defaultClass = EditorGUIUtility.isProSkin
                        ? BuilderConstants.CanvasContainerDarkStyleClassName
                        : BuilderConstants.CanvasContainerLightStyleClassName;
                    element.AddToClassList(defaultClass);
                    break;
            }
        }

        void UpdateCanvasThemeMenuStatus()
        {
            foreach (var item in m_CanvasThemeMenu.menu.MenuItems())
            {
                var action = item as DropdownMenuAction;
                action.UpdateActionStatus(null);

                var theme = document.currentCanvasTheme;

                if (action.status == DropdownMenuAction.Status.Checked)
                    m_CanvasThemeMenu.text = theme + " Theme  ";
            }
        }

        void TogglePreviewMode(ChangeEvent<bool> evt)
        {
            m_Viewport.SetPreviewMode(evt.newValue);

            if (evt.newValue)
                m_Explorer?.ClearHighlightOverlay();
            else
                m_Explorer?.ResetHighlightOverlays();
        }

        void SetViewportSubTitle()
        {
            var subTitle = string.Empty;
            if (!string.IsNullOrEmpty(m_BuilderPackageVersion))
                subTitle += $"UI Builder {m_BuilderPackageVersion}";

            m_Viewport.subTitle = subTitle;
        }

        void SetCanvasTitle()
        {
            var newFileName = document.uxmlFileName;

            if (string.IsNullOrEmpty(newFileName))
                newFileName = BuilderConstants.ToolbarUnsavedFileDisplayMessage;
            else if (document.hasUnsavedChanges)
                newFileName = newFileName + BuilderConstants.ToolbarUnsavedFileSuffix;

            m_Viewport.canvas.titleLabel.text = newFileName;
            m_Viewport.canvas.titleLabel.tooltip = document.uxmlPath;
        }

        void SetupSettingsMenu()
        {
            Builder builder = m_PaneWindow as Builder;

            if (builder == null)
                return;

            m_SettingsMenu.menu.AppendAction(
                "Show UXML \u2215 USS Previews",
                a => builder.codePreviewVisible = !builder.codePreviewVisible,
                a => builder.codePreviewVisible ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            m_SettingsMenu.menu.AppendAction(
                "Show Notifications",
                a => m_Viewport.notifications.ResetNotifications(),
                a => m_Viewport.notifications.hasPendingNotifications ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            AddBuilderProjectSettingsMenu();
        }

        void AddBuilderProjectSettingsMenu()
        {
            m_SettingsMenu.menu.AppendAction("Settings"
                , a => ShowSettingsWindow()
                , a => DropdownMenuAction.Status.Normal);
        }

        void ShowSettingsWindow()
        {
            var projectSettingsWindow = EditorWindow.GetWindow<ProjectSettingsWindow>();
            projectSettingsWindow.Show();
            projectSettingsWindow.SelectProviderByName(BuilderSettingsProvider.name);
        }

        public void SelectionChanged() {}

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            SetToolbarBreadCrumbs();
            SetCanvasTitle();
        }

        public void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            SetToolbarBreadCrumbs();
            SetCanvasTitle();
        }

    }
}
