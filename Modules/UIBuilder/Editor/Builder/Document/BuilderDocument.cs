// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.IO;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    class BuilderDocument : ScriptableObject, IBuilderSelectionNotifier, ISerializationCallbackReceiver, IBuilderPerFileAssetPostprocessor
    {
        //
        // Types
        //

        public enum CanvasTheme
        {
            Default,
            Dark,
            Light,
            Runtime, // obsolete but leave it for compatibility
            Custom
        }

        [Serializable]
        public struct UxmlURIToThemeStyleSheetURIEntry
        {
            public string UxmlURI;
            public string ThemeStyleSheetURI;
            public CanvasTheme CanvasTheme;
        }

        [SerializeField]
        List<UxmlURIToThemeStyleSheetURIEntry> m_SavedBuilderUxmlToThemeStyleSheetList = new List<UxmlURIToThemeStyleSheetURIEntry>();

        //
        // Serialized Data
        //

        [SerializeField]
        CanvasTheme m_CurrentCanvasTheme;

        //
        // Serialized Data
        //

        [SerializeField]
        LazyLoadReference<ThemeStyleSheet> m_CurrentCanvasThemeStyleSheetReference;

        [SerializeField]
        bool m_CodePreviewVisible = true;

        [SerializeField]
        List<BuilderDocumentOpenUXML> m_OpenUXMLFiles = new List<BuilderDocumentOpenUXML>();

        [SerializeField]
        int m_ActiveOpenUXMLFileIndex = 0;

        //
        // Unserialized Data
        //

        readonly WeakReference m_PrimaryViewportWindow = new WeakReference(null);

        readonly List<BuilderPaneWindow> m_RegisteredWindows = new List<BuilderPaneWindow>();

        //
        // Getters
        //

        public BuilderDocumentOpenUXML activeOpenUXMLFile
        {
            get
            {
                // We should always have one open UXML, even if unsaved.
                if (m_OpenUXMLFiles.Count == 0)
                {
                    m_OpenUXMLFiles.Add(new BuilderDocumentOpenUXML());
                    m_ActiveOpenUXMLFileIndex = 0;
                }

                if (m_ActiveOpenUXMLFileIndex < 0)
                    m_ActiveOpenUXMLFileIndex = 0;

                return m_OpenUXMLFiles[m_ActiveOpenUXMLFileIndex];
            }
        }

        public StyleSheet activeStyleSheet => activeOpenUXMLFile.activeStyleSheet;
        public BuilderDocumentSettings settings => activeOpenUXMLFile.settings;
        public string uxmlFileName => activeOpenUXMLFile.uxmlFileName;
        public string uxmlOldPath => activeOpenUXMLFile.uxmlOldPath;
        public string uxmlPath => activeOpenUXMLFile.uxmlPath;
        public List<string> ussPaths => activeOpenUXMLFile.ussPaths;
        public VisualTreeAsset visualTreeAsset => activeOpenUXMLFile.visualTreeAsset;
        public StyleSheet firstStyleSheet => activeOpenUXMLFile.firstStyleSheet;
        public List<BuilderDocumentOpenUSS> openUSSFiles => activeOpenUXMLFile.openUSSFiles;
        public List<BuilderDocumentOpenUXML> openUXMLFiles => m_OpenUXMLFiles;

        //
        // Getter/Setters
        //

        public IBuilderViewportWindow primaryViewportWindow
        {
            get
            {
                if (m_PrimaryViewportWindow == null)
                    return null;

                IBuilderViewportWindow window = m_PrimaryViewportWindow.Target as IBuilderViewportWindow;
                bool isReferenceValid = window != null;
                if (!isReferenceValid)
                    return null;

                return window;
            }
            private set
            {
                m_PrimaryViewportWindow.Target = value;
            }
        }

        public bool hasUnsavedChanges
        {
            get
            {
                bool hasUnsavedChanges = false;
                foreach (var openUXMLFile in m_OpenUXMLFiles)
                {
                    hasUnsavedChanges |= openUXMLFile.hasUnsavedChanges;
                    if (hasUnsavedChanges)
                        break;
                }
                return hasUnsavedChanges;
            }
            set
            {
                foreach (var openUXMLFile in m_OpenUXMLFiles)
                    openUXMLFile.hasUnsavedChanges = value;
            }
        }

        public CanvasTheme currentCanvasTheme => m_CurrentCanvasTheme;
        public ThemeStyleSheet currentCanvasThemeStyleSheet => m_CurrentCanvasThemeStyleSheetReference.asset;

        private ThemeStyleSheetManager m_ThemeManager;

        public void ChangeDocumentTheme(VisualElement documentElement, CanvasTheme canvasTheme, ThemeStyleSheet themeSheet, ThemeStyleSheetManager themeStyleSheetManager, bool isInit = false)
        {
            if (themeStyleSheetManager != null && m_ThemeManager != themeStyleSheetManager)
                m_ThemeManager = themeStyleSheetManager;
            m_CurrentCanvasTheme = canvasTheme;
            m_CurrentCanvasThemeStyleSheetReference = themeSheet;

            if (themeSheet)
            {
                themeSheet.isDefaultStyleSheet = true;
            }

            // Only replace the entry on Load, not on Builder Init
            if (!string.IsNullOrEmpty(activeOpenUXMLFile.uxmlPath) && !isInit)
            {
                var existingEntry =
                    m_SavedBuilderUxmlToThemeStyleSheetList.Find(entry =>
                    {
                        var assetPath = Path.GetFileNameWithoutExtension(entry.UxmlURI);
                        var response = URIHelpers.ValidateAssetURL(assetPath, entry.UxmlURI);
                        if (response.resolvedQueryAsset is VisualTreeAsset vta)
                        {
                            return vta == activeOpenUXMLFile.visualTreeAsset;
                        }

                        return false;
                    });

                if (!string.IsNullOrEmpty(existingEntry.UxmlURI))
                {
                    m_SavedBuilderUxmlToThemeStyleSheetList.Remove(existingEntry);
                }

                var newEntry = new UxmlURIToThemeStyleSheetURIEntry
                {
                    UxmlURI = URIHelpers.MakeAssetUri(activeOpenUXMLFile.visualTreeAsset),
                    ThemeStyleSheetURI = URIHelpers.MakeAssetUri(themeSheet),
                    CanvasTheme = m_CurrentCanvasTheme
                };

                // Do not save entry if it is using the default theme
                var projectDefaultTssAsset = m_ThemeManager.FindProjectDefaultRuntimeThemeAsset();

                // If we find a Default Runtime Theme in the project, use that as the default theme
                // Otherwise we use the built-in Default Runtime Theme
                var defaultTssAsset = projectDefaultTssAsset == null ? m_ThemeManager.builtInDefaultRuntimeTheme : projectDefaultTssAsset;
                if (themeSheet != defaultTssAsset)
                    m_SavedBuilderUxmlToThemeStyleSheetList.Add(newEntry);
            }

            RefreshStyle(documentElement);
        }

        public bool codePreviewVisible
        {
            get { return m_CodePreviewVisible; }
            set { m_CodePreviewVisible = value; }
        }

        public float viewportZoomScale
        {
            get => activeOpenUXMLFile.viewportZoomScale;
            set { activeOpenUXMLFile.viewportZoomScale = value; }
        }

        public Vector2 viewportContentOffset
        {
            get => activeOpenUXMLFile.viewportContentOffset;
            set { activeOpenUXMLFile.viewportContentOffset = value; }
        }

        //
        // Initialize / Construct / Enable / Clear
        //

        bool UnityWantsToQuit() => CheckForUnsavedChanges();

        void OnEnable()
        {
            hasUnsavedChanges = false;
            activeOpenUXMLFile.Clear();
            EditorApplication.wantsToQuit += UnityWantsToQuit;
            BuilderAssetPostprocessor.Register(this);
        }

        void OnDisable()
        {
            EditorApplication.wantsToQuit -= UnityWantsToQuit;
            BuilderAssetPostprocessor.Unregister(this);
        }

        public static BuilderDocument CreateInstance()
        {
            var newDoc = ScriptableObject.CreateInstance<BuilderDocument>();
            newDoc.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
            newDoc.name = "BuilderDocument";
            newDoc.LoadFromDisk();
            return newDoc;
        }

        //
        // Window Registrations
        //

        public void RegisterWindow(BuilderPaneWindow window)
        {
            if (window == null || m_RegisteredWindows.Contains(window))
                return;

            m_RegisteredWindows.Add(window);

            if (window is IBuilderViewportWindow)
            {
                primaryViewportWindow = window as IBuilderViewportWindow;
                BroadcastChange();
            }
        }

        public void UnregisterWindow(BuilderPaneWindow window)
        {
            if (window == null)
                return;

            var removed = m_RegisteredWindows.Remove(window);
            if (!removed)
                return;

            if (window is IBuilderViewportWindow && primaryViewportWindow == window as IBuilderViewportWindow)
            {
                primaryViewportWindow = null;
                BroadcastChange();
            }
        }

        public void BroadcastChange()
        {
            foreach (var window in m_RegisteredWindows)
                window.PrimaryViewportWindowChanged();
        }

        //
        // Styles
        //

        public void RefreshStyle(VisualElement documentElement)
            => activeOpenUXMLFile.RefreshStyle(documentElement);

        public void MarkStyleSheetsDirty()
            => activeOpenUXMLFile.MarkStyleSheetsDirty();

        public void AddStyleSheetToDocument(StyleSheet styleSheet, string ussPath)
            => activeOpenUXMLFile.AddStyleSheetToDocument(styleSheet, ussPath);

        public void RemoveStyleSheetFromDocument(int ussIndex)
            => activeOpenUXMLFile.RemoveStyleSheetFromDocument(ussIndex);

        public void AddStyleSheetsToAllRootElements(string newUssPath = null, int newUssIndex = 0)
            => activeOpenUXMLFile.AddStyleSheetsToAllRootElements(newUssPath, newUssIndex);

        public void UpdateActiveStyleSheet(BuilderSelection selection, StyleSheet styleSheet, IBuilderSelectionNotifier source)
            => activeOpenUXMLFile.UpdateActiveStyleSheet(selection, styleSheet, source);

        //
        // Save / Load
        //

        public bool SaveUnsavedChanges(string manualUxmlPath = null, bool isSaveAs = false)
            => activeOpenUXMLFile.SaveNewDocument(null, isSaveAs, out var needsFullRefresh, manualUxmlPath);

        public bool SaveNewDocument(
            VisualElement documentRootElement, bool isSaveAs,
            out bool needsFullRefresh,
            string manualUxmlPath = null)
        {
            var result = activeOpenUXMLFile.SaveNewDocument(
                documentRootElement, isSaveAs,
                out needsFullRefresh,
                manualUxmlPath);

            SaveToDisk();

            return result;
        }

        public void RestoreAssetsFromBackup() => activeOpenUXMLFile.RestoreAssetsFromBackup();

        public bool CheckForUnsavedChanges(bool assetModifiedExternally = false)
            => activeOpenUXMLFile.CheckForUnsavedChanges(assetModifiedExternally);

        public void NewDocument(VisualElement documentRootElement)
        {
            activeOpenUXMLFile.NewDocument(documentRootElement);
            SaveToDisk();
        }

        internal bool SaveNewTemplateFileFromHierarchy(string newTemplatePath, string uxml)
            => activeOpenUXMLFile.SaveNewTemplateFileFromHierarchy(newTemplatePath, uxml);

        public void LoadDocument(VisualTreeAsset visualTreeAsset, VisualElement documentElement, ThemeStyleSheetManager themeStyleSheetManager)
        {
            activeOpenUXMLFile.LoadDocument(visualTreeAsset, documentElement);
            if (themeStyleSheetManager != null && m_ThemeManager != themeStyleSheetManager)
                m_ThemeManager = themeStyleSheetManager;
            ForceUpdateDocumentTheme();
            SaveToDisk();
        }

        private void ForceUpdateDocumentTheme()
        {
            if (m_ThemeManager == null)
                return;

            // Find the entry. If it exists and is custom, use it to set the theme. Otherwise, use the editor themes or the default runtime theme.
            var forceToRuntimeTheme = false;
            var existingEntry =
                m_SavedBuilderUxmlToThemeStyleSheetList.Find(entry =>
                {
                    var assetPath = Path.GetFileNameWithoutExtension(entry.UxmlURI);
                    var response = URIHelpers.ValidateAssetURL(assetPath, entry.UxmlURI);
                    if (response.resolvedQueryAsset is VisualTreeAsset vta)
                    {
                        return vta == activeOpenUXMLFile.visualTreeAsset;
                    }

                    return false;
                });
            if (!string.IsNullOrEmpty(existingEntry.UxmlURI) && existingEntry.CanvasTheme == CanvasTheme.Custom)
            {
                m_CurrentCanvasTheme = existingEntry.CanvasTheme;
                var themeSheetAssetPath = Path.GetFileNameWithoutExtension(existingEntry.ThemeStyleSheetURI);
                var response = URIHelpers.ValidateAssetURL(themeSheetAssetPath, existingEntry.ThemeStyleSheetURI);
                if (response.resolvedQueryAsset is ThemeStyleSheet themeStyleSheet)
                    m_CurrentCanvasThemeStyleSheetReference =
                        themeStyleSheet;
                else
                    forceToRuntimeTheme = true;
            }
            else if (existingEntry.CanvasTheme == CanvasTheme.Custom)
                forceToRuntimeTheme = true;

            if (!forceToRuntimeTheme)
                return;

            m_CurrentCanvasTheme = CanvasTheme.Custom;
            var projectDefaultTssAsset = m_ThemeManager.FindProjectDefaultRuntimeThemeAsset();
            var defaultTssAsset = projectDefaultTssAsset == null ? m_ThemeManager.builtInDefaultRuntimeTheme : projectDefaultTssAsset;
            m_CurrentCanvasThemeStyleSheetReference = defaultTssAsset;
        }

        //
        // Circular Dependencies
        //

        public bool WillCauseCircularDependency(VisualTreeAsset vtaCheck)
        {
            if (string.IsNullOrEmpty(activeOpenUXMLFile.uxmlPath) || vtaCheck == null)
            {
                // Active document is a new file, so it won't cause a circular dependency
                return false;
            }

            // Perform check on current active document
            var activeVTA = activeOpenUXMLFile.visualTreeAsset;

            if (activeVTA.name == vtaCheck.name || activeVTA.TemplateExists(vtaCheck))
                return true;

            // Crawl up hierarchy if there are open subdocuments
            int parentInd = activeOpenUXMLFile.openSubDocumentParentIndex;
            while (parentInd > -1)
            {
                var parentuxml = openUXMLFiles[parentInd];
                var parentvta = parentuxml.visualTreeAsset;
                if (parentvta.TemplateExists(vtaCheck))
                    return true;
                parentInd = parentuxml.openSubDocumentParentIndex;
            }
            return false;
        }

        //
        // Sub Document
        //

        public void AddSubDocument(TemplateAsset vea = null)
        {
            var newUXMLFile = new BuilderDocumentOpenUXML();
            var templateAssetIndex = activeOpenUXMLFile.visualTreeAsset.templateAssets.IndexOf(vea);

            newUXMLFile.openSubDocumentParentSourceTemplateAssetIndex = templateAssetIndex;
            newUXMLFile.openSubDocumentParentIndex = m_ActiveOpenUXMLFileIndex;

            m_OpenUXMLFiles.Add(newUXMLFile);
            int newIndex = m_OpenUXMLFiles.Count - 1;
            m_ActiveOpenUXMLFileIndex = newIndex;
        }

        void GoToSubdocument(BuilderDocumentOpenUXML targetDocument)
        {
            while (activeOpenUXMLFile != targetDocument)
            {
                int scrapIndex = m_ActiveOpenUXMLFileIndex;
                m_ActiveOpenUXMLFileIndex = activeOpenUXMLFile.openSubDocumentParentIndex;
                m_OpenUXMLFiles.RemoveAt(scrapIndex);
            }
        }

        public void GoToSubdocument(VisualElement documentRootElement, BuilderPaneWindow paneWindow, BuilderDocumentOpenUXML targetDocument, bool skipUnsavedChangesCheck = false)
        {
            if (targetDocument == activeOpenUXMLFile)
                return;

            if (!skipUnsavedChangesCheck && !CheckForUnsavedChanges())
                return;

            NewDocument(documentRootElement);
            documentRootElement.SetProperty(BuilderConstants.ElementLinkedVisualTreeAssetVEPropertyName, null);
            GoToSubdocument(targetDocument);
            paneWindow.OnEnableAfterAllSerialization();
        }

        public void GoToRootDocument(VisualElement documentRootElement, BuilderPaneWindow paneWindow, bool skipUnsavedChangesCheck = false)
        {
            var parentDoc = paneWindow.document.openUXMLFiles[0];
            GoToSubdocument(documentRootElement, paneWindow, parentDoc, skipUnsavedChangesCheck);
        }

        //
        // Asset Change Detection
        //

        public void OnPostProcessAsset(string assetPath)
        {
            activeOpenUXMLFile.OnPostProcessAsset(assetPath);
            var builderWindow = Builder.ActiveWindow;
            if (builderWindow != null)
                builderWindow.toolbar?.InitCanvasTheme();
        }

        //
        // Selection
        //

        public void SelectionChanged()
        {
            // Selection changes don't affect the document.
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
            => activeOpenUXMLFile.HierarchyChanged(element);

        public void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            if (changeType == BuilderStylingChangeType.Default)
            {
                activeOpenUXMLFile.StylingChanged();
            }
        }

        //
        // Serialization
        //

        public void OnAfterBuilderDeserialize(VisualElement documentRootElement)
            => activeOpenUXMLFile.OnAfterBuilderDeserialize(documentRootElement);

        public void OnBeforeSerialize()
        {
            // Do nothing.
        }

        public void OnAfterDeserialize()
            => activeOpenUXMLFile.OnAfterDeserialize();

        // internal because it's used in tests
        internal void LoadFromDisk()
        {
            var path = BuilderConstants.builderDocumentDiskJsonFileAbsolutePath;

            if (!File.Exists(path))
                return;

            var json = File.ReadAllText(path);
            EditorJsonUtility.FromJsonOverwrite(json, this);

            if (currentCanvasThemeStyleSheet == null && m_CurrentCanvasTheme == CanvasTheme.Custom)
            {
                m_CurrentCanvasTheme = CanvasTheme.Default;
            }

            // Very important we convert asset references to paths here after a restore.
            foreach (var openUXMLFile in m_OpenUXMLFiles)
                openUXMLFile.OnAfterLoadFromDisk();
        }

        public void SaveToDisk()
        {
            // Before saving, clear entries with deleted UXMLs from ThemeStyleSheetList
            var BuilderUxmlToThemeStyleSheetListCopy = new List<UxmlURIToThemeStyleSheetURIEntry>(m_SavedBuilderUxmlToThemeStyleSheetList);
            foreach (var entry in BuilderUxmlToThemeStyleSheetListCopy)
            {
                var assetPath = Path.GetFileNameWithoutExtension(entry.UxmlURI);
                var response = URIHelpers.ValidateAssetURL(assetPath, entry.UxmlURI);
                if (response.resolvedQueryAsset is not VisualTreeAsset)
                {
                    m_SavedBuilderUxmlToThemeStyleSheetList.Remove(entry);
                }
            }

            var json = EditorJsonUtility.ToJson(this, true);

            var folderPath = BuilderConstants.builderDocumentDiskJsonFolderAbsolutePath;
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            File.WriteAllText(BuilderConstants.builderDocumentDiskJsonFileAbsolutePath, json);
        }

        public void SaveSettingsToDisk() => activeOpenUXMLFile.settings.SaveSettingsToDisk();

        public BuilderUXMLFileSettings fileSettings => activeOpenUXMLFile.fileSettings;
    }
}
