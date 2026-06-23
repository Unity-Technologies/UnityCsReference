// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using CanvasTheme = UnityEditor.UIElements.CanvasTheme;

namespace Unity.UI.Builder
{
    class BuilderDocument : ScriptableObject, IBuilderSelectionNotifier, ISerializationCallbackReceiver, IBuilderPerFileAssetPostprocessor
    {
        // Serialized Data
        [SerializeField]
        CanvasTheme m_CurrentCanvasTheme;

        [SerializeField]
        LazyLoadReference<ThemeStyleSheet> m_CurrentCanvasThemeStyleSheetReference;

        [SerializeField]
        bool m_CodePreviewVisible = true;

        [SerializeField]
        List<BuilderDocumentOpenUXML> m_OpenUXMLFiles = new List<BuilderDocumentOpenUXML>();

        [SerializeField]
        int m_ActiveOpenUXMLFileIndex = 0;

        // Unserialized Data
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
        public bool isLoadQueued => activeOpenUXMLFile.isLoadQueued;

        //
        // Getter/Setters
        //

        public CanvasTheme currentCanvasTheme => m_CurrentCanvasTheme;
        public ThemeStyleSheet currentCanvasThemeStyleSheet => m_CurrentCanvasThemeStyleSheetReference.asset;

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

        public void ChangeDocumentTheme(VisualElement documentElement, CanvasTheme canvasTheme, ThemeStyleSheet themeSheet, bool saveOverride)
        {
            m_CurrentCanvasTheme = canvasTheme;
            m_CurrentCanvasThemeStyleSheetReference= themeSheet;

            // Save the theme preference as a user override
            if (saveOverride)
            {
                if (fileSettings.editorExtensionMode)
                    ThemeUtility.SetEditorThemeOverride(canvasTheme, themeSheet);
                else
                    ThemeUtility.SetRuntimeThemeOverride(canvasTheme, themeSheet);
            }

            if (themeSheet)
            {
                themeSheet.isDefaultStyleSheet = true;
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

        public BuilderDocument()
        {
            hasUnsavedChanges = false;
            activeOpenUXMLFile.Clear();
        }

        void OnEnable()
        {
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
            ThemeUtility.LoadThemeOverrides();
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

        public void AddStyleSheetToDocument(StyleSheet styleSheet, string ussPath, int index = -1)
            => activeOpenUXMLFile.AddStyleSheetToDocument(styleSheet, ussPath, index);

        public void RemoveStyleSheetFromDocument(int ussIndex)
            => activeOpenUXMLFile.RemoveStyleSheetFromDocument(ussIndex);

        public void AddStyleSheetsToAllRootElements(string newUssPath = null, int newUssIndex = 0)
            => activeOpenUXMLFile.AddStyleSheetsToAllRootElements(newUssPath, newUssIndex);

        public void UpdateActiveStyleSheet(BuilderSelection selection, StyleSheet styleSheet, IBuilderSelectionNotifier source)
            => activeOpenUXMLFile.UpdateActiveStyleSheet(selection, styleSheet, source);

        public bool IsStyleSheetInDocument(StyleSheet styleSheet)
            => activeOpenUXMLFile.GetUssFileFromSheet(styleSheet) != null;

        //
        // Save / Load
        //

        public bool SaveUnsavedChanges(string manualUxmlPath = null, bool isSaveAs = false)
        {
            var documentRootElement = primaryViewportWindow.documentRootElement;
            return activeOpenUXMLFile.SaveNewDocument(documentRootElement, isSaveAs, out var needsFullRefresh,
                manualUxmlPath);
        }

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

            ClearUsingDeprecatedAPINotification();
        }

        internal bool SaveNewTemplateFileFromHierarchy(string newTemplatePath, string uxml)
            => activeOpenUXMLFile.SaveNewTemplateFileFromHierarchy(newTemplatePath, uxml);

        public void LoadDocument(VisualTreeAsset visualTreeAsset, VisualElement documentElement)
        {
            activeOpenUXMLFile.LoadDocument(visualTreeAsset, documentElement);
            SaveToDisk();
            ClearUsingDeprecatedAPINotification();
            CheckForUsingDeprecatedAPI(documentElement);
        }

        void OnDoNotShowAgainButtonPressed()
        {
            BuilderProjectSettings.BlockNotification(BuilderConstants.UsingDeprecatedAPINotificationKey);
            ClearUsingDeprecatedAPINotification();
        }

        void ClearUsingDeprecatedAPINotification()
        {
            primaryViewportWindow?.viewport.notifications.ClearNotifications(BuilderConstants.UsingDeprecatedAPINotificationKey);
        }

        public bool CheckForUsingDeprecatedAPI(VisualElement element)
        {
            bool usesDeprecatedAPI = false;

            // We do not use the VisualTreeAsset because we want to check in template instances as well.
            // We might check for other deprecated API usage in the future.
            usesDeprecatedAPI |= CheckForUsingDeprecatedAPIUxmlTraits(element);

            if (usesDeprecatedAPI)
            {
                if (!primaryViewportWindow.viewport.notifications.HasNotification(BuilderConstants.UsingDeprecatedAPINotificationKey))
                {
                    var notificationData = new BuilderNotifications.NotificationData
                    {
                        key = BuilderConstants.UsingDeprecatedAPINotificationKey,
                        message = BuilderConstants.UsingDeprecatedAPINotification,
                        actionButtonText = BuilderConstants.DoNotShowAgainNotificationButtonText,
                        onActionButtonClicked = OnDoNotShowAgainButtonPressed,
                        showDismissButton = true,
                        notificationType = BuilderNotifications.NotificationType.Warning,
                    };
                    primaryViewportWindow.viewport.notifications.AddNotification(notificationData);
                }
            }
            return usesDeprecatedAPI;
        }

        static bool CheckForUsingDeprecatedAPIUxmlTraits(VisualElement element)
        {
            using var _ = HashSetPool<string>.Get(out var testedTypes);
            return CheckForUsingDeprecatedAPIUxmlTraitsRecursively(element, testedTypes);
        }

        static bool CheckForUsingDeprecatedAPIUxmlTraitsRecursively(VisualElement element, HashSet<string> testedTypes)
        {
            var fullTypeName = element.fullTypeName;

            // If we already tested this type or if the element is not part of an asset file then skip it
            if (!testedTypes.Contains(fullTypeName) && (element.GetVisualElementAsset() != null || element.GetVisualElementAssetInTemplate() != null))
            {
                testedTypes.Add(fullTypeName);
                var uxmlSerializedDataDesc = UxmlSerializedDataRegistry.GetDescription(fullTypeName);

                // If null then it is using the old UxmlTraits system, which means it is using deprecated API.
                if (uxmlSerializedDataDesc == null)
                    return true;
            }

            foreach (var child in element.Children())
            {
                if (CheckForUsingDeprecatedAPIUxmlTraitsRecursively(child, testedTypes))
                    return true;
            }

            return false;
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

        public void AddSubDocumentInIsolation()
        {
            var newUXMLFile = new BuilderDocumentOpenUXML();
            newUXMLFile.openSubDocumentParentIndex = m_ActiveOpenUXMLFileIndex;

            m_OpenUXMLFiles.Add(newUXMLFile);
            m_ActiveOpenUXMLFileIndex = m_OpenUXMLFiles.Count - 1;
        }

        public void AddSubDocumentInContext(TemplateAsset templateAsset, int templateAssetIndex)
        {
            var newUXMLFile = new BuilderDocumentOpenUXML();
            newUXMLFile.openSubDocumentParentIndex = m_ActiveOpenUXMLFileIndex;
            newUXMLFile.templateAsset = templateAsset;
            newUXMLFile.templateAssetIndex = templateAssetIndex;

            m_OpenUXMLFiles.Add(newUXMLFile);
            m_ActiveOpenUXMLFileIndex = m_OpenUXMLFiles.Count - 1;
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

        public int GetTemplateAssetIndex(TemplateAsset templateAsset)
        {
            var allTemplates = activeOpenUXMLFile.visualTreeAsset.DepthFirstTraversalOfType<TemplateAsset>();
            var index = 0;
            foreach (var template in allTemplates)
            {
                if (template == templateAsset)
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        //
        // Asset Change Detection
        //

        public void OnPostProcessAsset(string assetPath)
        {
            activeOpenUXMLFile.OnPostProcessAsset(assetPath);

            for (int i = 0; i < m_OpenUXMLFiles.Count; i++)
            {
                var openUXMLFile = m_OpenUXMLFiles[i];
                if (openUXMLFile == activeOpenUXMLFile)
                    continue;
                if (openUXMLFile.uxmlOldPath == assetPath || openUXMLFile.ussPaths.Contains(assetPath))
                    openUXMLFile.ResyncBackupToCurrentAsset();
            }

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

            // Very important we convert asset references to paths here after a restore.
            foreach (var openUXMLFile in m_OpenUXMLFiles)
                openUXMLFile.OnAfterLoadFromDisk();
        }

        public void SaveToDisk()
        {
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
