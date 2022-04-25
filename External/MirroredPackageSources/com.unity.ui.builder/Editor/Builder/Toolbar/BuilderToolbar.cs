using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.UIElements;
using UnityEditor.UIElements.StyleSheets;
using Toolbar = UnityEditor.UIElements.Toolbar;

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
        ToolbarBreadcrumbs m_Breadcrumbs;

        ThemeStyleSheetManager m_ThemeManager;
        ThemeStyleSheet m_LastCustomTheme;

        string m_LastSavePath = "Assets";

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

            // Fit canvas
            m_FitCanvasButton = this.Q<ToolbarButton>(FitCanvasButtonName);
            m_FitCanvasButton.clickable.clicked += () => m_Viewport.FitCanvas();

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
            var currentTheme = document.currentCanvasTheme;
            var currentThemeSheet = document.currentCanvasThemeStyleSheet;

            // If canvas theme is editor-only without Editor Extensions mode enabled, treat this as a Runtime theme
            if (!document.fileSettings.editorExtensionMode && IsEditorCanvasTheme(currentTheme))
                currentTheme = BuilderDocument.CanvasTheme.Runtime;

            // If canvas theme is equal to the obsolete Runtime enum, search for the Unity default runtime theme
            // in the current project.  If that can't be found, try one of the custom themes, otherwise
            // fallback to default Editor theme
            if (currentTheme == BuilderDocument.CanvasTheme.Runtime)
            {
                var defaultTssAsset = EditorGUIUtility.Load(ThemeRegistry.kUnityRuntimeThemePath) as ThemeStyleSheet;
                if (defaultTssAsset == null)
                {
                    // Try to load or create the default runtime asset for the user
                    var pathName = $"Assets/{ThemeRegistry.kUnityRuntimeThemeFileName}";
                    defaultTssAsset = EditorGUIUtility.Load(pathName) as ThemeStyleSheet;

                    if (defaultTssAsset == null)
                    {
                        var action = ScriptableObject.CreateInstance<DoCreateAssetWithContent>();
                        action.filecontent = "@import url(\"" + ThemeRegistry.kThemeScheme +
                                             "://default\");\nVisualElement {}";

                        var instanceId = ProjectBrowser.kAssetCreationInstanceID_ForNonExistingAssets;
                        action.Action(instanceId, pathName, null);
                        action.CleanUp();

                        defaultTssAsset = EditorGUIUtility.Load(pathName) as ThemeStyleSheet;
                    }
                }

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

            ChangeCanvasTheme(currentTheme, currentThemeSheet);
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

        void NewTestDocument()
        {
            if (!document.CheckForUnsavedChanges())
                return;

            var testAsset =
                BuilderConstants.UIBuilderPackagePath +
                "/SampleDocument/BuilderSampleCanvas.uxml";
            var originalAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(testAsset);
            LoadDocument(originalAsset, testAsset);
        }

        void NewTestVariablesDocument()
        {
            if (!document.CheckForUnsavedChanges())
                return;

            var testAsset =
                BuilderConstants.UIBuilderPackagePath +
                "/SampleDocument/BuilderVariableSampleCanvas.uxml";
            var originalAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(testAsset);
            LoadDocument(originalAsset, testAsset);
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
            ChangeCanvasTheme(document.currentCanvasTheme, document.currentCanvasThemeStyleSheet);
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
            return $"{scale*100f}%";
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
                    var themeName = ObjectNames.NicifyVariableName(Path.GetFileNameWithoutExtension(themeFile));

                    m_CanvasThemeMenu.menu.AppendAction(themeName, a =>
                    {
                        var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themeFile);
                        ChangeCanvasTheme(BuilderDocument.CanvasTheme.Custom, theme);
                        UpdateCanvasThemeMenuStatus();
                    },
                        a => document.currentCanvasThemeStyleSheet != null && AssetDatabase.GetAssetPath(document.currentCanvasThemeStyleSheet) == themeFile
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);
                }
            }
        }

        bool IsEditorCanvasTheme(BuilderDocument.CanvasTheme theme)
        {
            return theme is BuilderDocument.CanvasTheme.Default or BuilderDocument.CanvasTheme.Dark or BuilderDocument.CanvasTheme.Light;
        }

        public void ChangeCanvasTheme(BuilderDocument.CanvasTheme theme, ThemeStyleSheet themeStyleSheet = null)
        {
            ApplyCanvasTheme(m_Viewport.sharedStylesAndDocumentElement, theme, themeStyleSheet);
            ApplyCanvasTheme(m_Viewport.documentRootElement, theme, themeStyleSheet);
            ApplyCanvasBackground(m_Viewport.canvas.defaultBackgroundElement, theme, themeStyleSheet);
            ApplyCanvasTheme(m_TooltipPreview, theme, themeStyleSheet);
            ApplyCanvasBackground(m_TooltipPreview, theme, themeStyleSheet);

            document.ChangeDocumentTheme(m_Viewport.documentRootElement, theme, themeStyleSheet);
            m_Inspector?.selection.NotifyOfStylingChange(null, null, BuilderStylingChangeType.RefreshOnly);
        }

        void ApplyCanvasTheme(VisualElement element, BuilderDocument.CanvasTheme theme, ThemeStyleSheet customThemeSheet)
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

            // Should remove the previous custom theme stylesheet
            if (m_LastCustomTheme)
            {
                element.styleSheets.Remove(m_LastCustomTheme);
                m_LastCustomTheme = null;
            }
            // We verify whether the styles are loaded beforehand because calling GetCommonXXXStyleSheet() will load them unecessarily in this case
            if (UIElementsEditorUtility.IsCommonDarkStyleSheetLoaded())
                element.styleSheets.Remove(UIElementsEditorUtility.GetCommonDarkStyleSheet());
            if (UIElementsEditorUtility.IsCommonLightStyleSheetLoaded())
                element.styleSheets.Remove(UIElementsEditorUtility.GetCommonLightStyleSheet());
            m_Viewport.canvas.defaultBackgroundElement.style.display = DisplayStyle.Flex;

            StyleSheet themeStyleSheet = null;
            m_Viewport.canvas.checkerboardBackgroundElement.style.display = DisplayStyle.None;

            switch (theme)
            {
                case BuilderDocument.CanvasTheme.Dark:
                    themeStyleSheet = UIElementsEditorUtility.GetCommonDarkStyleSheet();
                    break;
                case BuilderDocument.CanvasTheme.Light:
                    themeStyleSheet = UIElementsEditorUtility.GetCommonLightStyleSheet();
                    break;
                case BuilderDocument.CanvasTheme.Default:
                    themeStyleSheet = EditorGUIUtility.isProSkin ? UIElementsEditorUtility.GetCommonDarkStyleSheet() : UIElementsEditorUtility.GetCommonLightStyleSheet();
                    break;
                case BuilderDocument.CanvasTheme.Custom:
                    m_Viewport.canvas.defaultBackgroundElement.style.display = DisplayStyle.None;
                    m_Viewport.canvas.checkerboardBackgroundElement.style.display = DisplayStyle.Flex;
                    m_LastCustomTheme = customThemeSheet;
                    themeStyleSheet = customThemeSheet;
                    break;
                default:
                    break;
            }

            if (themeStyleSheet != null)
                element.styleSheets.Add(themeStyleSheet);

            element.SetProperty(BuilderConstants.ElementLinkedActiveThemeStyleSheetVEPropertyName, themeStyleSheet);
        }

        void ApplyCanvasBackground(VisualElement element, BuilderDocument.CanvasTheme theme, ThemeStyleSheet themeStyleSheet)
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
                        var themeName = ObjectNames.NicifyVariableName(Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(document.currentCanvasThemeStyleSheet)));
                        m_CanvasThemeMenu.text = themeName;
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
                classNames = new[] { "ThemeStyleSheet" }
            };
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

            foreach (var assetPath in deletedAssets)
            {
                if (!assetPath.EndsWith(BuilderConstants.TssExtension))
                    continue;

                // Check if the current theme has been removed then revert to the default one
                if (document.currentCanvasTheme == BuilderDocument.CanvasTheme.Custom &&
                    document.currentCanvasThemeStyleSheet == null)
                {
                    m_ToolBar.ChangeCanvasTheme(BuilderDocument.CanvasTheme.Default, null);
                }

                listChanged |= RemoveThemeFile(assetPath);
            }

            if (listChanged)
            {
                NotifyThemesChanged();
            }
        }
    }
}
