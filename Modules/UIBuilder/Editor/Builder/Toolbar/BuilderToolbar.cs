// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.StyleSheets;
using Toolbar = UnityEditor.UIElements.Toolbar;

namespace Unity.UI.Builder
{
    internal class BuilderToolbar : VisualElement, IBuilderAssetModificationProcessor, IBuilderSelectionNotifier
    {
        public const string FitViewportButtonName = "fit-viewport-button";
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
        ToolbarButton m_FitViewportButton;
        ToolbarMenu m_CanvasThemeMenu;
        ToolbarButton m_ResetThemeButton;
        ToolbarMenu m_SettingsMenu;
        Toolbar m_BreadcrumbsToolbar;
        ToolbarBreadcrumbs m_Breadcrumbs;

        ThemeStyleSheetManager m_ThemeManager;
        ThemeStyleSheet m_LastCustomTheme;

        string m_LastSavePath = "Assets/";

        public BuilderDocument document
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

            m_ThemeManager = new ThemeStyleSheetManager(this);
            m_ThemeManager.selection = m_Selection;
            ThemeUtility.themeFilesChanged += UpdateCanvasThemeMenuStatus;

            // File Menu
            m_FileMenu = this.Q<ToolbarMenu>("file-menu");
            SetUpFileMenu();

            // Zoom Menu
            m_ZoomMenu = this.Q<ToolbarMenu>("zoom-menu");
            SetUpZoomMenu();

            // Fit viewport
            m_FitViewportButton = this.Q<ToolbarButton>(FitViewportButtonName);
            m_FitViewportButton.clicked += () => m_Viewport.FitViewport();

            // Preview Button
            var previewButton = this.Q<ToolbarToggle>(PreviewToggleName);
            previewButton.RegisterValueChangedCallback(TogglePreviewMode);

            m_Viewport.SetPreviewMode(false);

            m_CanvasThemeMenu = this.Q<ToolbarMenu>("canvas-theme-menu");

            m_ResetThemeButton = this.Q<ToolbarButton>("reset-theme-button");
            m_ResetThemeButton.clicked += ResetThemeToProjectSettings;

            InitCanvasTheme();
            UpdateResetThemeButtonState();

            SetViewportSubTitle();

            // Track unsaved changes state change.
            UpdateHasUnsavedChanges();

            m_SettingsMenu = this.Q<ToolbarMenu>("settings-menu");
            SetupSettingsMenu();

            // Breadcrumbs & BreadCrumbs Toolbar
            m_BreadcrumbsToolbar = this.Q<Toolbar>(BreadcrumbsToolbarName);
            m_Breadcrumbs = this.Q<ToolbarBreadcrumbs>(BreadcrumbsName);
            SetToolbarBreadCrumbs();

            RegisterCallback<AttachToPanelEvent>(RegisterCallbacks);
        }

        /// Initializes the themes using a 2-layer hierarchy:
        /// 1. User override
        /// 2. Project settings if it exists, otherwise built-in defaults
        internal void InitCanvasTheme()
        {
            // Get the effective theme
            var (theme, themeSheet) = ThemeUtility.GetEffectiveTheme(document.fileSettings.editorExtensionMode);

            // Apply the theme
            ChangeCanvasTheme(theme, themeSheet, true);
            UpdateCanvasThemeMenuStatus();
        }

        void RegisterCallbacks(AttachToPanelEvent evt)
        {
            RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacks);
            BuilderAssetModificationProcessor.Register(this);
            if (m_ThemeManager != null)
                BuilderAssetPostprocessor.Register(m_ThemeManager);
            UIToolkitProjectSettings.onThemeChanged += OnProjectThemeChanged;
        }

        void UnregisterCallbacks(DetachFromPanelEvent evt)
        {
            UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacks);
            BuilderAssetModificationProcessor.Unregister(this);
            if (m_ThemeManager != null)
                BuilderAssetPostprocessor.Unregister(m_ThemeManager);
            UIToolkitProjectSettings.onThemeChanged -= OnProjectThemeChanged;
            ThemeUtility.themeFilesChanged -= UpdateCanvasThemeMenuStatus;
        }

        void OnProjectThemeChanged()
        {
            // Refresh the canvas theme when project settings change
            InitCanvasTheme();
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
                Action onBreadCrumbClick = () =>
                {
                    document.GoToSubdocument(m_Viewport.documentRootElement, m_PaneWindow, Doc);
                    m_Viewport.SetViewFromDocumentSetting();
                };
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
            // let Unity move it, we support that with the project:// URL syntax
            // note that if assets were not serialized with this Builder version, or authored manually with this new
            // URL syntax, then this will break references.
            return AssetMoveResult.DidNotMove;
        }

        internal static bool IsAssetUsedInDocument(BuilderDocument document, string assetPath)
        {
            // Check current document.
            var isAssetUsedInDocument = assetPath.Equals(document.uxmlPath) || document.ussPaths.Contains(assetPath);

            if (!isAssetUsedInDocument)
            {
                // Check uxml and uss paths in document dependencies.
                isAssetUsedInDocument = IsAssetUsedInDependencies(document.visualTreeAsset, assetPath);
            }

            return isAssetUsedInDocument;
        }

        static bool IsAssetUsedInDependencies(VisualTreeAsset visualTreeAsset, string assetPath)
        {
            foreach (var styleSheet in visualTreeAsset.GetAllReferencedStyleSheets())
            {
                if (AssetDatabase.GetAssetPath(styleSheet) == assetPath)
                {
                    return true;
                }
            }

            foreach (var vta in visualTreeAsset.templateDependencies)
            {
                var path = visualTreeAsset.GetPathFromTemplateName(vta.name);
                if (path == assetPath)
                {
                    return true;
                }

                return IsAssetUsedInDependencies(vta, assetPath);
            }

            return false;
        }

        bool IsFileActionCompatible(string assetPath, string actionName)
        {
            if (IsAssetUsedInDocument(document, assetPath))
            {
                var fileName = Path.GetFileName(assetPath);
                var acceptAction = BuilderDialogsUtility.DisplayDialog(BuilderConstants.ErrorDialogNotice,
                    string.Format(BuilderConstants.ErrorIncompatibleFileActionMessage, actionName, fileName),
                    string.Format(BuilderConstants.DialogDiscardOption, actionName.ToPascalCase()),
                    string.Format(BuilderConstants.DialogAbortActionOption, actionName.ToPascalCase()));

                if (acceptAction)
                {
                    // Open a new, empty document
                    NewDocument(false);
                }

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

            UpdateHasUnsavedChanges();

            return true;
        }

        internal void SaveDocument(bool isSaveAs)
        {
            var viewportWindow = m_PaneWindow as IBuilderViewportWindow;
            if (viewportWindow == null)
                return;

            m_Selection.NotifyPreSaveDocument();
            m_Explorer.elementHierarchyView.RegisterTreeState();

            // Set asset.
            var userConfirmed = document.SaveNewDocument(viewportWindow.documentRootElement, isSaveAs, out var needFullRefresh);
            if (!userConfirmed)
                return;

            // Save last save path.
            m_LastSavePath = Path.GetDirectoryName(document.uxmlPath);

            // Set doc field value.
            UpdateHasUnsavedChanges();

            // Only updating UI to remove "*" from file names.
            m_Selection.ResetUnsavedChanges();

            if (needFullRefresh)
                m_PaneWindow.OnEnableAfterAllSerialization();
            else
                m_Selection.NotifyOfHierarchyChange(document);
        }

        public void OnAfterBuilderDeserialize()
        {
            UpdateHasUnsavedChanges();
            SetViewportSubTitle();
            ChangeCanvasTheme(document.currentCanvasTheme, document.currentCanvasThemeStyleSheet, true);
            SetToolbarBreadCrumbs();
        }

        public bool ReloadDocument()
        {
            return LoadDocument(document.visualTreeAsset, false);
        }

        public bool LoadDocument(VisualTreeAsset visualTreeAsset, string assetPath)
        {
            return LoadDocument(visualTreeAsset, true, false, assetPath);
        }

        public bool LoadDocument(VisualTreeAsset visualTreeAsset, bool unloadAllSubdocuments = true, bool assetModifiedExternally = false, string assetPath = null)
        {
            if (!BuilderAssetUtilities.ValidateAsset(visualTreeAsset, assetPath))
                return false;

            var hasUnsavedChanges = document.CheckForUnsavedChanges(assetModifiedExternally);
            if (!hasUnsavedChanges && assetModifiedExternally)
            {
                // Needed to refresh the document after an external change.
                document.OnAfterBuilderDeserialize(m_Viewport.documentRootElement);
                m_Selection.NotifyOfHierarchyChange(document);
                m_Selection.ClearSelection(this);
            }

            if (!hasUnsavedChanges)
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
            InitCanvasTheme();

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

            m_FileMenu.menu.AppendAction("Open...", a =>
            {
                var path = OpenLoadFileDialog(BuilderConstants.ToolbarLoadUxmlDialogTitle, BuilderConstants.Uxml);
                if (string.IsNullOrEmpty(path))
                    return;

                if (BuilderAssetUtilities.IsPathInProject(path))
                {
                    path = BuilderAssetUtilities.GetPathRelativeToProject(path, false);
                }
                else
                {
                    Debug.LogError(BuilderConstants.ToolbarCannotLoadUxmlOutsideProjectMessage);
                    return;
                }

                var asset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(path);

                LoadDocument(asset, false, false, path);
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
            return $"{(int)(scale*100f)}%";
        }

        void UpdateZoomMenuText()
        {
            m_ZoomMenu.text = GetTextForZoomScale(m_Viewport.zoomScale);
        }

        void SetUpZoomMenu()
        {
            foreach (var zoomScale in m_Viewport.zoomer.zoomMenuScaleValues)
            {
                m_ZoomMenu.menu.AppendAction(GetTextForZoomScale(zoomScale),
                    a => { m_Viewport.zoomScale = zoomScale; },
                    a => Mathf.Approximately(m_Viewport.zoomScale, zoomScale) ?
                        DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
            m_Viewport.canvas.RegisterCallback<GeometryChangedEvent>(e => UpdateZoomMenuText());
            UpdateZoomMenuText();
        }

        void SetUpCanvasThemeMenu()
        {
            m_CanvasThemeMenu.menu.ClearItems();

            // Get project default theme
            var (projectTheme, projectThemeSheet) = ThemeUtility.GetProjectDefaultTheme(document.fileSettings.editorExtensionMode);

            // Add editor themes if in editor extension mode
            if (document.fileSettings.editorExtensionMode)
            {
                var editorThemes = ThemeUtility.GetEditorThemesToDisplayName();
                foreach (var themeKvp in editorThemes)
                {
                    var canvasTheme = themeKvp.Key;
                    var displayName = themeKvp.Value;

                    // Add project suffix to the project default
                    if (canvasTheme == projectTheme)
                    {
                        displayName += ThemeUtility.ProjectThemeSuffix;
                        AddThemeMenuAction(displayName, canvasTheme, null, 0);
                        m_CanvasThemeMenu.menu.InsertSeparator(null, 1);
                    }
                    else
                        AddThemeMenuAction(displayName, canvasTheme, null);
                }

                m_CanvasThemeMenu.menu.AppendSeparator();
            }

            // Add runtime themes
            var runtimeThemes = ThemeUtility.GetRuntimeThemesToDisplayName();
            foreach (var themeKvp in runtimeThemes)
            {
                var themeSheet = themeKvp.Key;
                var displayName = themeKvp.Value;

                // Add project suffix to the project default
                if (themeSheet == projectThemeSheet)
                {
                    displayName += ThemeUtility.ProjectThemeSuffix;
                    AddThemeMenuAction(displayName, CanvasTheme.Custom, themeSheet, 0);
                    m_CanvasThemeMenu.menu.InsertSeparator(null, 1);
                }
                else
                    AddThemeMenuAction(displayName, CanvasTheme.Custom, themeSheet);
            }

            m_CanvasThemeMenu.menu.AppendSeparator();
            m_CanvasThemeMenu.menu.AppendAction("Preview Theme Settings...", a => ShowSettingsWindow());
        }

        void AddThemeMenuAction(string displayName, CanvasTheme theme, ThemeStyleSheet themeSheet, int index = -1)
        {
            if (index != -1)
            {
                m_CanvasThemeMenu.menu.InsertAction(
                    index,
                    displayName,
                    a => ChangeCanvasTheme(theme, themeSheet),
                    a => IsThemeSelected(theme, themeSheet) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
                );
            }
            else
            {
                m_CanvasThemeMenu.menu.AppendAction(
                    displayName,
                    a => ChangeCanvasTheme(theme, themeSheet),
                    a => IsThemeSelected(theme, themeSheet) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
                );
            }
        }

        bool IsThemeSelected(CanvasTheme theme, ThemeStyleSheet themeSheet)
        {
            // For built-in themes, check both theme and null sheet
            if (theme != CanvasTheme.Custom)
                return document.currentCanvasTheme == theme && document.currentCanvasThemeStyleSheet == null;

            // For custom themes, check the stylesheet
            return document.currentCanvasThemeStyleSheet == themeSheet;
        }

        string GetThemeDisplayName(CanvasTheme theme, ThemeStyleSheet themeSheet)
        {
            // Fallback
            if (theme == CanvasTheme.ProjectSettings)
            {
                if (document.fileSettings.editorExtensionMode)
                    theme = CanvasTheme.Default;
                else
                {
                    theme = CanvasTheme.Custom;
                    themeSheet = ThemeUtility.builtInDefaultRuntimeTheme;
                }
            }

            if (document.fileSettings.editorExtensionMode && ThemeUtility.IsEditorCanvasTheme(theme))
            {
                var editorThemes = ThemeUtility.GetEditorThemesToDisplayName();
                return editorThemes.TryGetValue(theme, out var displayName) ? displayName : null;
            }

            return ThemeUtility.NicifyThemeName(themeSheet);
        }

        public void ChangeCanvasTheme(CanvasTheme theme, ThemeStyleSheet customThemeStyleSheet = null, bool isInit = false)
        {
            m_Viewport.canvas.defaultBackgroundElement.style.display = theme == CanvasTheme.Custom ? DisplayStyle.None : DisplayStyle.Flex;
            m_Viewport.canvas.checkerboardBackgroundElement.style.display = theme == CanvasTheme.Custom ? DisplayStyle.Flex : DisplayStyle.None;

            StyleSheet activeThemeStyleSheet = ThemeUtility.GetStyleSheetForTheme(theme, customThemeStyleSheet);

            ApplyCanvasTheme(m_Viewport.sharedStylesAndDocumentElement, activeThemeStyleSheet, m_LastCustomTheme);
            ApplyCanvasTheme(m_Viewport.documentRootElement, activeThemeStyleSheet, m_LastCustomTheme);
            ApplyCanvasBackground(m_Viewport.canvas.defaultBackgroundElement, theme);
            ApplyCanvasTheme(m_TooltipPreview, activeThemeStyleSheet, m_LastCustomTheme);
            ApplyCanvasBackground(m_TooltipPreview, theme);
            document.ChangeDocumentTheme(m_Viewport.documentRootElement, theme, customThemeStyleSheet, saveOverride: !isInit);
            m_LastCustomTheme = customThemeStyleSheet;

            m_Inspector?.selection.NotifyOfStylingChange(null, null, BuilderStylingChangeType.RefreshOnly);

            // Update reset button state and menu status after changing theme
            UpdateResetThemeButtonState();
            UpdateCanvasThemeMenuStatus();
        }

        internal void ResetThemeToProjectSettings()
        {
            // Reset document theme preferences to project settings
            ThemeUtility.ClearThemeOverrides();
            InitCanvasTheme(); // Refresh the current theme
            UpdateResetThemeButtonState();
        }

        void UpdateResetThemeButtonState()
        {
            // Show button only if document has any non-default theme preferences
            var (projectCanvasTheme, projectThemeSheet) = ThemeUtility.GetProjectDefaultTheme(document.fileSettings.editorExtensionMode);
            var (effectiveCanvasTheme, effectiveProjectThemeSheet) = ThemeUtility.GetEffectiveTheme(document.fileSettings.editorExtensionMode);
            StyleSheet effectiveProjectStyleSheet = effectiveProjectThemeSheet;
            StyleSheet projectStyleSheet = projectThemeSheet;
            if (document.fileSettings.editorExtensionMode)
            {
                projectStyleSheet = ThemeUtility.GetStyleSheetForTheme(projectCanvasTheme, projectThemeSheet);
                effectiveProjectStyleSheet = ThemeUtility.GetStyleSheetForTheme(effectiveCanvasTheme, effectiveProjectThemeSheet);
            }
            var hasOverride = ThemeUtility.HasLocalThemeOverride() && projectStyleSheet != effectiveProjectStyleSheet;
            m_ResetThemeButton.style.display = hasOverride ? DisplayStyle.Flex : DisplayStyle.None;

            // Update tooltip with project theme name
            if (hasOverride)
            {
                var projectThemeName = GetThemeDisplayName(projectCanvasTheme, projectThemeSheet);
                m_ResetThemeButton.tooltip = $"Reset to the preferred preview theme ({projectThemeName}) for this project.";
            }
        }

        void ApplyCanvasTheme(VisualElement element, StyleSheet newThemeStyleSheet, StyleSheet oldCustomThemeStyleSheet)
        {
            if (element == null)
                return;

            // Remove any null stylesheet. This may occur if an used theme has been deleted.
            // This should be handle by ui toolkit
            var i = 0;

            if (element.styleSheetList != null)
            {
                while (i < element.styleSheetList.Count)
                {
                    var sheet = element.styleSheetList[i];
                    if (sheet == null)
                    {
                        element.styleSheetList?.Remove(sheet);
                        if (element.styleSheetList.Count == 0)
                        {
                            element.styleSheetList = null;
                            break;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            // We verify whether the styles are loaded beforehand because calling GetCommonXXXStyleSheet() will load them unnecessarily in this case
            if (UIElementsEditorUtility.IsCommonDarkStyleSheetLoaded())
                element.styleSheets.Remove(UIElementsEditorUtility.GetCommonDarkStyleSheet());
            if (UIElementsEditorUtility.IsCommonLightStyleSheetLoaded())
                element.styleSheets.Remove(UIElementsEditorUtility.GetCommonLightStyleSheet());
            if (oldCustomThemeStyleSheet != null)
                element.styleSheets.Remove(oldCustomThemeStyleSheet);

            // Ensure theme style are applied first
            if (newThemeStyleSheet != null)
                element.styleSheets.Insert(0, newThemeStyleSheet);

            element.SetProperty(BuilderConstants.ElementLinkedActiveThemeStyleSheetVEPropertyName, newThemeStyleSheet);
        }

        void ApplyCanvasBackground(VisualElement element, CanvasTheme theme)
        {
            if (element == null)
                return;

            element.RemoveFromClassList(BuilderConstants.CanvasContainerDarkStyleClassName);
            element.RemoveFromClassList(BuilderConstants.CanvasContainerLightStyleClassName);
            element.RemoveFromClassList(BuilderConstants.CanvasContainerRuntimeStyleClassName);

            switch (theme)
            {
                case CanvasTheme.Dark:
                    element.AddToClassList(BuilderConstants.CanvasContainerDarkStyleClassName);
                    break;
                case CanvasTheme.Light:
                    element.AddToClassList(BuilderConstants.CanvasContainerLightStyleClassName);
                    break;
                case CanvasTheme.Default:
                    string defaultClass = EditorGUIUtility.isProSkin
                        ? BuilderConstants.CanvasContainerDarkStyleClassName
                        : BuilderConstants.CanvasContainerLightStyleClassName;
                    element.AddToClassList(defaultClass);
                    break;
                case CanvasTheme.Custom:
                    element.AddToClassList(BuilderConstants.CanvasContainerRuntimeStyleClassName);
                    break;
            }
        }

        void UpdateCanvasThemeMenuStatus()
        {
            SetUpCanvasThemeMenu();

            if (m_CanvasThemeMenu.menu.MenuItems().Count == 0)
            {
                m_CanvasThemeMenu.tooltip = BuilderConstants.ToolbarCanvasThemeMenuEmptyTooltip;
                var editorThemes = ThemeUtility.GetEditorThemesToDisplayName();
                m_CanvasThemeMenu.text = editorThemes.TryGetValue(CanvasTheme.Default, out var displayName) ? displayName : "Active Editor Theme";
                return;
            }

            m_CanvasThemeMenu.tooltip = document.fileSettings.editorExtensionMode
                ? BuilderConstants.ToolbarCanvasThemeMenuEditorTooltip
                : BuilderConstants.ToolbarCanvasThemeMenuTooltip;

            // Update menu text to match currently selected theme
            m_CanvasThemeMenu.text = GetCurrentThemeMenuText();
        }

        string GetCurrentThemeMenuText()
        {
            var currentTheme = document.currentCanvasTheme;
            var currentThemeSheet = document.currentCanvasThemeStyleSheet;

            // Get project default to check if current theme matches it
            var (projectTheme, projectThemeSheet) = ThemeUtility.GetProjectDefaultTheme(document.fileSettings.editorExtensionMode);

            if (!document.fileSettings.editorExtensionMode && currentThemeSheet == null)
                currentThemeSheet = projectThemeSheet;

            // Check if current theme is the project default
            bool isProjectDefault = (currentTheme == projectTheme && currentThemeSheet == projectThemeSheet) ||
                                   (currentTheme == CanvasTheme.Custom && currentThemeSheet == projectThemeSheet);

            var displayName = GetThemeDisplayName(currentTheme, currentThemeSheet);

            return isProjectDefault ? displayName + ThemeUtility.ProjectThemeSuffix : displayName;
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
            m_Viewport.subTitle = string.Empty;
        }

        void UpdateHasUnsavedChanges()
        {
            m_PaneWindow.SetHasUnsavedChanges(document.hasUnsavedChanges);
            SetCanvasTitle();
        }

        void SetCanvasTitle()
        {
            var newFileName = document.uxmlFileName;
            bool hasUSSChanges = ((m_Selection.selectionType == BuilderSelectionType.StyleSheet) || (m_Selection.selectionType == BuilderSelectionType.StyleSelector) || (m_Selection.selectionType == BuilderSelectionType.ParentStyleSelector));

            if (string.IsNullOrEmpty(newFileName))
                newFileName = BuilderConstants.ToolbarUnsavedFileDisplayMessage;
            else if (document.hasUnsavedChanges && !hasUSSChanges)
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
                "Reset Notifications",
                _ => BuilderProjectSettings.ResetNotifications(),
                _ => BuilderProjectSettings.HasBlockedNotifications() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            m_SettingsMenu.menu.AppendSeparator();

            m_SettingsMenu.menu.AppendAction("Settings"
                , a => ShowSettingsWindow()
                , a => DropdownMenuAction.Status.Normal);
        }

        void ShowSettingsWindow()
        {
            var projectSettingsWindow = EditorWindow.GetWindow<ProjectSettingsWindow>();
            projectSettingsWindow.Show();
            projectSettingsWindow.SelectProviderByName(UIToolkitSettingsProvider.name);
        }

        public void SelectionChanged() { }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            SetToolbarBreadCrumbs();
            UpdateHasUnsavedChanges();
        }

        public void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            SetToolbarBreadCrumbs();
            UpdateHasUnsavedChanges();
        }
    }

    class ThemeStyleSheetManager : IBuilderAssetPostprocessor
    {
        BuilderToolbar m_ToolBar;

        BuilderDocument document => m_ToolBar.document;
        public BuilderSelection selection { get; set; }

        public ThemeStyleSheetManager(BuilderToolbar toolbar)
        {
            m_ToolBar = toolbar;
        }

        public void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool listChanged = false;

            // Check if any .tss files were moved
            foreach (var movedAssetPath in movedFromAssetPaths)
            {
                if (movedAssetPath.EndsWith(BuilderConstants.TssExtension))
                    listChanged = true;
            }

            foreach (var assetPath in importedAssets)
            {
                if (!assetPath.EndsWith(BuilderConstants.TssExtension))
                    continue;

                // If the current theme has changed then update the UI
                if (document.currentCanvasThemeStyleSheet && assetPath == AssetDatabase.GetAssetPath(document.currentCanvasThemeStyleSheet))
                {
                    selection.NotifyOfStylingChange(null, null, BuilderStylingChangeType.RefreshOnly);
                }

                listChanged = true;
            }

            foreach (var assetPath in deletedAssets)
            {
                if (!assetPath.EndsWith(BuilderConstants.TssExtension))
                    continue;

                listChanged = true;
            }

            // Refresh the centralized theme list before checking for removed themes
            if (listChanged)
            {
                ThemeUtility.RefreshRuntimeThemeFiles();
            }

            // Check if current theme was removed and revert to default if needed
            foreach (var assetPath in deletedAssets)
            {
                if (!assetPath.EndsWith(BuilderConstants.TssExtension))
                    continue;

                // Current theme has been removed, revert to the default one
                if (!ThemeUtility.IsEditorCanvasTheme(document.currentCanvasTheme) &&
                    document.currentCanvasThemeStyleSheet == null)
                {
                    var projectDefaultRuntimeAsset = ThemeUtility.FindProjectDefaultRuntimeThemeAsset();

                    if (document.fileSettings.editorExtensionMode)
                    {
                        m_ToolBar.ChangeCanvasTheme(CanvasTheme.Default, null, true);
                    }
                    else
                    {
                        m_ToolBar.ChangeCanvasTheme(CanvasTheme.Custom,
                            projectDefaultRuntimeAsset ?? ThemeUtility.builtInDefaultRuntimeTheme,
                            isInit: true);
                    }
                }
            }
        }
    }
}
