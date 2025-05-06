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
            m_ThemeManager.themeFilesChanged += UpdateCanvasThemeMenuStatus;

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

            InitCanvasTheme();

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

        public void InitCanvasTheme()
        {
            var projectDefaultTssAsset = m_ThemeManager.FindProjectDefaultRuntimeThemeAsset();

            // If we find a Default Runtime Theme in the project, use that as the default theme
            // Otherwise we use the built-in Default Runtime Theme
            var defaultTssAsset = projectDefaultTssAsset == null ? m_ThemeManager.builtInDefaultRuntimeTheme : projectDefaultTssAsset;
            InitCanvasTheme(defaultTssAsset);
        }

        private void InitCanvasTheme(ThemeStyleSheet defaultTssAsset)
        {
            var currentTheme = document.currentCanvasTheme;
            var currentThemeSheet = document.currentCanvasThemeStyleSheet;

            // If canvas theme is editor-only without Editor Extensions mode enabled, treat this as a Runtime theme
            if (!document.fileSettings.editorExtensionMode && IsEditorCanvasTheme(currentTheme))
            {
                currentTheme = BuilderDocument.CanvasTheme.Runtime;
            }
            else if (currentTheme == BuilderDocument.CanvasTheme.Custom && currentThemeSheet == null)
            {
                // Theme file was deleted, fallback to default theme
                currentTheme = BuilderDocument.CanvasTheme.Runtime;
            }
            else if (currentTheme == BuilderDocument.CanvasTheme.Custom
                     && currentThemeSheet == m_ThemeManager.builtInDefaultRuntimeTheme
                     && defaultTssAsset != m_ThemeManager.builtInDefaultRuntimeTheme)
            {
                // If a new Default Runtime Theme was added to the project, use that instead of the built-in one
                currentTheme = BuilderDocument.CanvasTheme.Runtime;
            }

            // If canvas theme is equal to the obsolete Runtime enum, search for the Unity default runtime theme
            // in the current project.  If that can't be found, try one of the custom themes, otherwise
            // fallback to default Editor theme
            if (currentTheme == BuilderDocument.CanvasTheme.Runtime)
            {
                if (defaultTssAsset != null)
                {
                    currentTheme = BuilderDocument.CanvasTheme.Custom;
                    currentThemeSheet = defaultTssAsset;
                }
                else
                {
                    // Fall back on first custom theme we find or default editor theme
                    if (m_ThemeManager != null && m_ThemeManager.themeFiles.Count > 0)
                    {
                        var customThemeFile = m_ThemeManager.themeFiles[0];
                        currentTheme = BuilderDocument.CanvasTheme.Custom;
                        currentThemeSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(customThemeFile);
                    }
                    else
                    {
                        currentTheme = BuilderDocument.CanvasTheme.Default;
                        currentThemeSheet = null;
                    }
                }
            }

            ChangeCanvasTheme(currentTheme, currentThemeSheet, true);
            UpdateCanvasThemeMenuStatus();
        }

        void RegisterCallbacks(AttachToPanelEvent evt)
        {
            RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacks);
            BuilderAssetModificationProcessor.Register(this);
            if (m_ThemeManager != null)
                BuilderAssetPostprocessor.Register(m_ThemeManager);
        }

        void UnregisterCallbacks(DetachFromPanelEvent evt)
        {
            UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacks);
            BuilderAssetModificationProcessor.Unregister(this);
            if (m_ThemeManager != null)
                BuilderAssetPostprocessor.Unregister(m_ThemeManager);
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

            m_Explorer.elementHierarchyView.RegisterTreeState();

            // Set asset.
            var userConfirmed = document.SaveNewDocument(viewportWindow.documentRootElement, isSaveAs, out var needFullRefresh);
            if (!userConfirmed)
                return;

            // Update any uses out there of the currently edited and saved USS.
            RetainedMode.FlagStyleSheetChange();

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
            return LoadDocument(document.visualTreeAsset, false, false, null, m_ThemeManager);
        }

        public bool LoadDocument(VisualTreeAsset visualTreeAsset, string assetPath)
        {
            return LoadDocument(visualTreeAsset, true, false, assetPath, m_ThemeManager);
        }

        public bool LoadDocument(VisualTreeAsset visualTreeAsset, bool unloadAllSubdocuments = true, bool assetModifiedExternally = false, string assetPath = null, ThemeStyleSheetManager themeStyleSheetManager = null)
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

            LoadDocumentInternal(visualTreeAsset, themeStyleSheetManager);

            return true;
        }

        void LoadDocumentInternal(VisualTreeAsset visualTreeAsset, ThemeStyleSheetManager themeStyleSheetManager = null)
        {
            m_Selection.ClearSelection(null);

            document.LoadDocument(visualTreeAsset, m_Viewport.documentRootElement, themeStyleSheetManager);

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

            m_FileMenu.menu.AppendAction("Open...", a =>
            {
                var path = OpenLoadFileDialog(BuilderConstants.ToolbarLoadUxmlDialogTitle, BuilderConstants.Uxml);
                if (string.IsNullOrEmpty(path))
                    return;

                if (BuilderAssetUtilities.IsPathInProject(path))
                {
                    path = BuilderAssetUtilities.GetPathRelativeToProject(path);
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

        internal static string GetEditorThemeText(BuilderDocument.CanvasTheme theme)
        {
            switch (theme)
            {
                case BuilderDocument.CanvasTheme.Default: return "Active Editor Theme";
                case BuilderDocument.CanvasTheme.Dark: return "Dark Editor Theme";
                case BuilderDocument.CanvasTheme.Light: return "Light Editor Theme";
                default:
                    break;
            }

            return null;
        }

        void SetUpCanvasThemeMenu()
        {
            m_CanvasThemeMenu.menu.ClearItems();

            if (document.fileSettings.editorExtensionMode)
            {
                m_CanvasThemeMenu.menu.AppendAction(GetEditorThemeText(BuilderDocument.CanvasTheme.Default), a =>
                    {
                        ChangeCanvasTheme(BuilderDocument.CanvasTheme.Default, null);
                        UpdateCanvasThemeMenuStatus();
                    },
                    a => document.currentCanvasTheme == BuilderDocument.CanvasTheme.Default
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);

                m_CanvasThemeMenu.menu.AppendAction(GetEditorThemeText(BuilderDocument.CanvasTheme.Dark), a =>
                    {
                        ChangeCanvasTheme(BuilderDocument.CanvasTheme.Dark);
                        UpdateCanvasThemeMenuStatus();
                    },
                    a => document.currentCanvasTheme == BuilderDocument.CanvasTheme.Dark
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);

                m_CanvasThemeMenu.menu.AppendAction(GetEditorThemeText(BuilderDocument.CanvasTheme.Light), a =>
                    {
                        ChangeCanvasTheme(BuilderDocument.CanvasTheme.Light);
                        UpdateCanvasThemeMenuStatus();
                    },
                    a => document.currentCanvasTheme == BuilderDocument.CanvasTheme.Light
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);
            }

            if (m_ThemeManager != null && m_ThemeManager.themeFiles.Count > 0)
            {
                m_CanvasThemeMenu.menu.AppendSeparator();
                m_ThemeManager.themeFiles.Sort((a, b) => Path.GetFileName(a).CompareTo(Path.GetFileName(b)));

                foreach (var themeFile in m_ThemeManager.themeFiles)
                {
                    var isBuiltInDefaultRuntimeTheme = themeFile == ThemeRegistry.k_DefaultStyleSheetPath;
                    var themeName = isBuiltInDefaultRuntimeTheme ?
                        BuilderConstants.ToolbarBuiltInDefaultRuntimeThemeName : ObjectNames.NicifyVariableName(Path.GetFileNameWithoutExtension(themeFile));

                    m_CanvasThemeMenu.menu.AppendAction(themeName, a =>
                    {
                        var theme = isBuiltInDefaultRuntimeTheme ? m_ThemeManager.builtInDefaultRuntimeTheme : AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themeFile);
                        ChangeCanvasTheme(BuilderDocument.CanvasTheme.Custom, theme);
                        UpdateCanvasThemeMenuStatus();
                    },
                        a => document.currentCanvasThemeStyleSheet != null
                             && (AssetDatabase.GetAssetPath(document.currentCanvasThemeStyleSheet) == themeFile || isBuiltInDefaultRuntimeTheme && document.currentCanvasThemeStyleSheet == m_ThemeManager.builtInDefaultRuntimeTheme)
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);
                }
            }
        }

        bool IsEditorCanvasTheme(BuilderDocument.CanvasTheme theme)
        {
            return theme is BuilderDocument.CanvasTheme.Default or BuilderDocument.CanvasTheme.Dark or BuilderDocument.CanvasTheme.Light;
        }

        public void ChangeCanvasTheme(BuilderDocument.CanvasTheme theme, ThemeStyleSheet customThemeStyleSheet = null, bool isInit = false)
        {
            m_Viewport.canvas.defaultBackgroundElement.style.display = theme == BuilderDocument.CanvasTheme.Custom ? DisplayStyle.None : DisplayStyle.Flex;
            m_Viewport.canvas.checkerboardBackgroundElement.style.display = theme == BuilderDocument.CanvasTheme.Custom ? DisplayStyle.Flex : DisplayStyle.None;

            StyleSheet activeThemeStyleSheet = null;

            switch (theme)
            {
                case BuilderDocument.CanvasTheme.Dark:
                    activeThemeStyleSheet = UIElementsEditorUtility.GetCommonDarkStyleSheet();
                    break;
                case BuilderDocument.CanvasTheme.Light:
                    activeThemeStyleSheet = UIElementsEditorUtility.GetCommonLightStyleSheet();
                    break;
                case BuilderDocument.CanvasTheme.Default:
                    activeThemeStyleSheet = EditorGUIUtility.isProSkin ? UIElementsEditorUtility.GetCommonDarkStyleSheet() : UIElementsEditorUtility.GetCommonLightStyleSheet();
                    break;
                case BuilderDocument.CanvasTheme.Custom:
                    activeThemeStyleSheet = customThemeStyleSheet;
                    break;
            }

            ApplyCanvasTheme(m_Viewport.sharedStylesAndDocumentElement, activeThemeStyleSheet, m_LastCustomTheme);
            ApplyCanvasTheme(m_Viewport.documentRootElement, activeThemeStyleSheet, m_LastCustomTheme);
            ApplyCanvasBackground(m_Viewport.canvas.defaultBackgroundElement, theme);
            ApplyCanvasTheme(m_TooltipPreview, activeThemeStyleSheet, m_LastCustomTheme);
            ApplyCanvasBackground(m_TooltipPreview, theme);
            document.ChangeDocumentTheme(m_Viewport.documentRootElement, theme, customThemeStyleSheet, m_ThemeManager, isInit);
            UpdateCanvasThemeMenuStatus();
            m_LastCustomTheme = customThemeStyleSheet;

            m_Inspector?.selection.NotifyOfStylingChange(null, null, BuilderStylingChangeType.RefreshOnly);
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
                case BuilderDocument.CanvasTheme.Custom:
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
                m_CanvasThemeMenu.text = GetEditorThemeText(BuilderDocument.CanvasTheme.Default);
                return;
            }

            m_CanvasThemeMenu.tooltip = document.fileSettings.editorExtensionMode ?
                BuilderConstants.ToolbarCanvasThemeMenuEditorTooltip :
                BuilderConstants.ToolbarCanvasThemeMenuTooltip;

            foreach (var item in m_CanvasThemeMenu.menu.MenuItems())
            {
                var action = item as DropdownMenuAction;

                // Skip separators
                if (action == null)
                {
                    continue;
                }

                action.UpdateActionStatus(null);

                var theme = document.currentCanvasTheme;

                if (action.status == DropdownMenuAction.Status.Checked)
                {
                    if (theme == BuilderDocument.CanvasTheme.Custom)
                    {
                        if (document.currentCanvasThemeStyleSheet == m_ThemeManager.builtInDefaultRuntimeTheme)
                        {
                            m_CanvasThemeMenu.text = BuilderConstants.ToolbarBuiltInDefaultRuntimeThemeName;
                        }
                        else
                        {
                            var assetPath = AssetDatabase.GetAssetPath(document.currentCanvasThemeStyleSheet);
                            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(assetPath);
                            var themeName = ObjectNames.NicifyVariableName(fileNameWithoutExtension);
                            m_CanvasThemeMenu.text = themeName;
                        }
                    }
                    else
                    {
                        m_CanvasThemeMenu.text = GetEditorThemeText(theme);
                    }
                }
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

            if (Unsupported.IsDeveloperMode())
            {
                m_SettingsMenu.menu.AppendAction(
                    "Always Use UxmlTraits Attribute fields",
                a =>
                {
                    BuilderUxmlAttributesView.alwaysUseUxmlTraits = !BuilderUxmlAttributesView.alwaysUseUxmlTraits;
                    builder.inspector.RefreshUI();
                },
                a => BuilderUxmlAttributesView.alwaysUseUxmlTraits ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }

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
        SearchFilter m_SearchFilter;
        BuilderToolbar m_ToolBar;
        List<string> m_ThemeFiles;

        internal ThemeStyleSheet builtInDefaultRuntimeTheme
        {
            get;
        }

        public List<string> themeFiles
        {
            get
            {
                if (m_ThemeFiles == null)
                {
                    m_ThemeFiles = new List<string>();
                    RefreshThemeFiles();
                }

                return m_ThemeFiles;
            }
        }

        BuilderDocument document => m_ToolBar.document;
        public BuilderSelection selection { get; set; }

        public event Action themeFilesChanged;

        public ThemeStyleSheetManager(BuilderToolbar toolbar)
        {
            m_ToolBar = toolbar;
            m_SearchFilter = new SearchFilter
            {
                searchArea = SearchFilter.SearchArea.AllAssets,
                classNames = new[] { nameof(ThemeStyleSheet) }
            };

            builtInDefaultRuntimeTheme = EditorGUIUtility.Load(ThemeRegistry.k_DefaultStyleSheetPath) as ThemeStyleSheet;
        }

        internal ThemeStyleSheet FindProjectDefaultRuntimeThemeAsset()
        {
            if (themeFiles.Count <= 0)
            {
                return null;
            }

            foreach (var themeFilePath in themeFiles)
            {
                if (BuilderAssetUtilities.IsProjectDefaultRuntimeAsset(themeFilePath))
                {
                    return AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themeFilePath);
                }
            }

            return null;
        }

        bool AddThemeFile(string theme)
        {
            if (themeFiles.Contains(theme))
                return false;
            themeFiles.Add(theme);
            return true;
        }

        bool RemoveThemeFile(string theme)
        {
            if (!themeFiles.Contains(theme))
                return false;
            themeFiles.Remove(theme);
            return true;
        }

        void NotifyThemesChanged()
        {
            themeFilesChanged?.Invoke();
        }

        public void RefreshThemeFiles()
        {
            m_ThemeFiles?.Clear();
            var assets = AssetDatabase.FindAllAssets(m_SearchFilter);

            foreach (var asset in assets)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset.instanceID);
                AddThemeFile(assetPath);
            }

            // If we don't have a Default Runtime Theme in the project, we add the built-in one
            if (FindProjectDefaultRuntimeThemeAsset() == null)
            {
                AddThemeFile(ThemeRegistry.k_DefaultStyleSheetPath);
            }

            NotifyThemesChanged();
        }

        public void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool listChanged = false;

            foreach (var movedAssetPath in movedFromAssetPaths)
            {
                if (!movedAssetPath.EndsWith(BuilderConstants.TssExtension))
                    continue;

                listChanged |= RemoveThemeFile(movedAssetPath);
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

                listChanged |= AddThemeFile(assetPath);
            }

            var projectDefaultRuntimeAsset = FindProjectDefaultRuntimeThemeAsset();

            foreach (var assetPath in deletedAssets)
            {
                if (!assetPath.EndsWith(BuilderConstants.TssExtension))
                    continue;

                // Check if the current theme has been removed then revert to the default one
                if (document.currentCanvasTheme == BuilderDocument.CanvasTheme.Custom &&
                    document.currentCanvasThemeStyleSheet == null)
                {
                    if (document.fileSettings.editorExtensionMode)
                    {
                        m_ToolBar.ChangeCanvasTheme(BuilderDocument.CanvasTheme.Default, null);
                    }
                    else
                    {
                        m_ToolBar.ChangeCanvasTheme(BuilderDocument.CanvasTheme.Custom,
                            projectDefaultRuntimeAsset != null
                                ? projectDefaultRuntimeAsset
                                : builtInDefaultRuntimeTheme);
                    }
                }

                listChanged |= RemoveThemeFile(assetPath);
            }

            if (projectDefaultRuntimeAsset == null && !themeFiles.Contains(ThemeRegistry.k_DefaultStyleSheetPath))
            {
                // Project Default Runtime Theme was deleted, so we add the built-in one
                listChanged |= AddThemeFile(ThemeRegistry.k_DefaultStyleSheetPath);
            }
            else if (projectDefaultRuntimeAsset != null && themeFiles.Contains(ThemeRegistry.k_DefaultStyleSheetPath))
            {
                // Project Default Runtime Theme was added, so we remove the built-in one
                listChanged |= RemoveThemeFile(ThemeRegistry.k_DefaultStyleSheetPath);
            }

            if (listChanged)
            {
                NotifyThemesChanged();
            }
        }
    }
}
