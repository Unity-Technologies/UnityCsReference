// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    [Serializable]
    [ExtensionOfNativeClass]
    class BuilderDocumentOpenUXML
    {
        //
        // Serialized Data
        //

        [SerializeField]
        List<BuilderDocumentOpenUSS> m_OpenUSSFiles = new List<BuilderDocumentOpenUSS>();

        [SerializeField]
        VisualTreeAsset m_VisualTreeAssetBackup;

        [SerializeField]
        string m_OpenendVisualTreeAssetOldPath;

        [SerializeField]
        VisualTreeAsset m_VisualTreeAsset;

        [SerializeField]
        StyleSheet m_ActiveStyleSheet;

        [SerializeField]
        BuilderDocumentSettings m_Settings;

        [SerializeField]
        int m_OpenSubDocumentParentIndex = -1;

        [SerializeField]
        int m_OpenSubDocumentParentSourceTemplateAssetIndex = -1;

        //
        // Unserialized Data
        //

        bool m_HasUnsavedChanges;
        bool m_DocumentBeingSavedExplicitly;
        BuilderUXMLFileSettings m_FileSettings;
        BuilderDocument m_Document;
        VisualElement m_CurrentDocumentRootElement;

        //
        // Getters
        //

        public BuilderUXMLFileSettings fileSettings => m_FileSettings ?? (m_FileSettings = new BuilderUXMLFileSettings(visualTreeAsset));

        internal List<BuilderDocumentOpenUXML> openUXMLFiles
        {
            get
            {
                // Find or create document.
                if (m_Document == null)
                {
                    var allDocuments = Resources.FindObjectsOfTypeAll(typeof(BuilderDocument));
                    if (allDocuments.Length > 1)
                        Debug.LogError("UIBuilder: More than one BuilderDocument was somehow created!");
                    if (allDocuments.Length == 0)
                        m_Document = BuilderDocument.CreateInstance();
                    else
                        m_Document = allDocuments[0] as BuilderDocument;
                }

                if (m_Document == null)
                    return null;

                return m_Document.openUXMLFiles;
            }
        }

        public StyleSheet activeStyleSheet
        {
            get
            {
                if (m_ActiveStyleSheet == null)
                    m_ActiveStyleSheet = firstStyleSheet;

                return m_ActiveStyleSheet;
            }
        }

        public BuilderDocumentSettings settings
        {
            get
            {
                // If this uxmnl is being edited in place then use the parent document's settings
                if (isChildSubDocument && openSubDocumentParentSourceTemplateAssetIndex != -1)
                {
                    m_Settings = openSubDocumentParent.settings;
                }
                else
                {
                    m_Settings = BuilderDocumentSettings.CreateOrLoadSettingsObject(m_Settings, uxmlPath);
                }
                return m_Settings;
            }
        }

        public string uxmlFileName
        {
            get
            {
                var path = uxmlPath;
                if (path == null)
                    return null;

                var fileName = Path.GetFileName(path);
                return fileName;
            }
        }

        public string uxmlOldPath
        {
            get { return m_OpenendVisualTreeAssetOldPath; }
        }

        public string uxmlPath
        {
            get { return AssetDatabase.GetAssetPath(m_VisualTreeAsset); }
        }

        public List<string> ussPaths
        {
            get
            {
                var paths = new List<string>();
                for (int i = 0; i < m_OpenUSSFiles.Count; ++i)
                    paths.Add(m_OpenUSSFiles[i].assetPath);
                return paths;
            }
        }

        public VisualTreeAsset visualTreeAsset
        {
            get
            {
                if (m_VisualTreeAsset == null)
                    m_VisualTreeAsset = VisualTreeAssetUtilities.CreateInstance();

                return m_VisualTreeAsset;
            }
        }

        public StyleSheet firstStyleSheet
        {
            get { return m_OpenUSSFiles.Count > 0 ? m_OpenUSSFiles[0].styleSheet : null; }
        }

        public List<BuilderDocumentOpenUSS> openUSSFiles => m_OpenUSSFiles;

        public bool isChildSubDocument => openSubDocumentParentIndex > -1;

        public BuilderDocumentOpenUXML openSubDocumentParent => isChildSubDocument ? openUXMLFiles[openSubDocumentParentIndex] : null;

        //
        // Getter/Setters
        //

        public bool hasUnsavedChanges
        {
            get { return m_HasUnsavedChanges; }
            set
            {
                if (value == m_HasUnsavedChanges)
                    return;

                m_HasUnsavedChanges = value;
            }
        }

        public bool isAnonymousDocument
        {
            get { return string.IsNullOrEmpty(uxmlPath); }
        }

        public float viewportZoomScale
        {
            get
            {
                return settings.ZoomScale;
            }
            set
            {
                settings.ZoomScale = value;
                settings.SaveSettingsToDisk();
            }
        }

        public Vector2 viewportContentOffset
        {
            get
            {
                return settings.PanOffset;
            }
            set
            {
                settings.PanOffset = value;
                settings.SaveSettingsToDisk();
            }
        }

        public int openSubDocumentParentIndex
        {
            get { return m_OpenSubDocumentParentIndex; }
            set
            {
                m_OpenSubDocumentParentIndex = value;
            }
        }

        public int openSubDocumentParentSourceTemplateAssetIndex
        {
            get { return m_OpenSubDocumentParentSourceTemplateAssetIndex; }
            set
            {
                m_OpenSubDocumentParentSourceTemplateAssetIndex = value;
            }
        }

        //
        // Initialize / Construct / Enable / Clear
        //

        public void Clear()
        {
            ClearUndo();

            RestoreAssetsFromBackup();

            ClearBackups();
            m_OpenendVisualTreeAssetOldPath = string.Empty;
            m_ActiveStyleSheet = null;
            m_FileSettings = null;

            if (m_VisualTreeAsset != null)
            {
                if (!AssetDatabase.Contains(m_VisualTreeAsset))
                    m_VisualTreeAsset.Destroy();

                m_VisualTreeAsset = null;
            }

            m_OpenUSSFiles.Clear();

            m_Settings = null;
        }

        //
        // Styles
        //

        public void RefreshStyle(VisualElement documentRootElement)
        {
            if (m_CurrentDocumentRootElement == null)
                m_CurrentDocumentRootElement = documentRootElement;

            StyleCache.ClearStyleCache();
            UnityEngine.UIElements.StyleSheets.StyleSheetCache.ClearCaches();
            foreach (var openUSS in m_OpenUSSFiles)
                openUSS.FixRuleReferences();
            m_CurrentDocumentRootElement.IncrementVersion((VersionChangeType) (-1));
        }

        public void MarkStyleSheetsDirty()
        {
            foreach (var openUSS in m_OpenUSSFiles)
                EditorUtility.SetDirty(openUSS.styleSheet);
        }

        public void AddStyleSheetToDocument(StyleSheet styleSheet, string ussPath)
        {
            var newOpenUssFile = new BuilderDocumentOpenUSS();
            newOpenUssFile.Set(styleSheet, ussPath);
            m_OpenUSSFiles.Add(newOpenUssFile);

            AddStyleSheetsToAllRootElements();

            hasUnsavedChanges = true;
        }

        public void RemoveStyleSheetFromDocument(int ussIndex)
        {
            RemoveStyleSheetFromLists(ussIndex);

            AddStyleSheetsToAllRootElements();

            hasUnsavedChanges = true;
        }

        void AddStyleSheetsToRootAsset(VisualElementAsset rootAsset, string newUssPath = null, int newUssIndex = 0)
        {
            if (rootAsset.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                return;

            rootAsset.ClearStyleSheets();

            for (int i = 0; i < m_OpenUSSFiles.Count; ++i)
            {
                var localUssPath = m_OpenUSSFiles[i].assetPath;

                if (!string.IsNullOrEmpty(newUssPath) && i == newUssIndex)
                    localUssPath = newUssPath;

                if (string.IsNullOrEmpty(localUssPath))
                    continue;

                rootAsset.AddStyleSheet(m_OpenUSSFiles[i].styleSheet);
                rootAsset.AddStyleSheetPath(localUssPath);
            }
        }

        void RemoveStyleSheetsFromRootAsset(VisualElementAsset rootAsset)
        {
            if (rootAsset.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                return;

            for (int i = 0; i < m_OpenUSSFiles.Count; ++i)
            {
                var localUssPath = m_OpenUSSFiles[i].assetPath;

                if (string.IsNullOrEmpty(localUssPath))
                    continue;

                rootAsset.RemoveStyleSheet(m_OpenUSSFiles[i].styleSheet);
                rootAsset.RemoveStyleSheetPath(localUssPath);
            }
        }

        // For the near to mid term, we have this code that cleans up any
        // existing root element stylesheets.
        void RemoveLegacyStyleSheetsFromRootAssets()
        {
            foreach (var asset in visualTreeAsset.visualElementAssets)
            {
                if (!visualTreeAsset.IsRootElement(asset))
                    continue; // Not a root asset.

                RemoveStyleSheetsFromRootAsset(asset);
            }

            foreach (var asset in visualTreeAsset.templateAssets)
            {
                if (!visualTreeAsset.IsRootElement(asset))
                    continue; // Not a root asset.

                RemoveStyleSheetsFromRootAsset(asset);
            }
        }

        public void AddStyleSheetsToAllRootElements(string newUssPath = null, int newUssIndex = 0)
        {
            var rootVEA = visualTreeAsset.GetRootUXMLElement();
            AddStyleSheetsToRootAsset(rootVEA, newUssPath, newUssIndex);
        }

        void RemoveStyleSheetFromLists(int ussIndex)
        {
            var openUSSFile = m_OpenUSSFiles[ussIndex];
            m_OpenUSSFiles.RemoveAt(ussIndex);
            openUSSFile.Clear();
        }

        public void UpdateActiveStyleSheet(BuilderSelection selection, StyleSheet styleSheet, IBuilderSelectionNotifier source)
        {
            if (m_ActiveStyleSheet == styleSheet)
                return;

            m_ActiveStyleSheet = styleSheet;
            selection.ForceReselection(source);
        }

        public bool SaveUnsavedChanges(string manualUxmlPath = null, bool isSaveAs = false)
        {
            return SaveNewDocument(null, isSaveAs, out var needsFullRefresh, manualUxmlPath);
        }

        public bool SaveNewDocument(
            VisualElement documentRootElement, bool isSaveAs,
            out bool needsFullRefresh,
            string manualUxmlPath = null)
        {
            needsFullRefresh = false;

            // Re-use or ask the user for the UXML path.
            var newUxmlPath = uxmlPath;
            if (string.IsNullOrEmpty(newUxmlPath) || isSaveAs)
            {
                if (!string.IsNullOrEmpty(manualUxmlPath))
                {
                    newUxmlPath = manualUxmlPath;
                }
                else
                {
                    newUxmlPath = BuilderDialogsUtility.DisplaySaveFileDialog("Save UXML", null, null, "uxml");
                    if (newUxmlPath == null) // User cancelled the save dialog.
                        return false;
                }
            }

            ClearUndo();

            var startTime = DateTime.UtcNow;
            var savedUSSFiles = new List<BuilderDocumentOpenUSS>();

            // Save USS files.
            foreach (var openUSSFile in m_OpenUSSFiles)
            {
                if (openUSSFile.SaveToDisk(visualTreeAsset))
                {
                    savedUSSFiles.Add(openUSSFile);
                }
            }

            var oldUxmlTest = m_VisualTreeAssetBackup?.GenerateUXML(m_OpenendVisualTreeAssetOldPath, true);

            // Save UXML files
            // Saving all open UXML files to ensure references correct upon changes in child documents.
            foreach (var openUXMLFile in openUXMLFiles)
                openUXMLFile.PreSaveSyncBackup();

            bool shouldSave = m_OpenendVisualTreeAssetOldPath != newUxmlPath;
            var uxmlText = visualTreeAsset.GenerateUXML(newUxmlPath, true);

            if (uxmlText != null)
            {
                if (!shouldSave && m_VisualTreeAssetBackup)
                {
                    shouldSave = oldUxmlTest != uxmlText;
                }

                if (shouldSave)
                {
                    WriteUXMLToFile(newUxmlPath, uxmlText);
                }
            }

            // Once we wrote all the files to disk, we refresh the DB and reload
            // the files from the AssetDatabase.
            m_DocumentBeingSavedExplicitly = true;
            try
            {
                AssetDatabase.Refresh();
            }
            finally
            {
                m_DocumentBeingSavedExplicitly = false;
            }

            // Reorder document after reimporting
            VisualTreeAssetUtilities.ReOrderDocument(m_VisualTreeAsset);

            // Check if any USS assets have changed reload them.
            foreach (var openUSSFile in savedUSSFiles)
                needsFullRefresh |= openUSSFile.PostSaveToDiskChecksAndFixes();

            // Check if any UXML assets have changed and reload them.
            // Saving all open UXML files to ensure references correct upon changes in child subdocuments.
            foreach (var openUXMLFile in openUXMLFiles)
                needsFullRefresh |= openUXMLFile.PostSaveToDiskChecksAndFixes(this == openUXMLFile ? newUxmlPath : null, needsFullRefresh);

            if (needsFullRefresh)
            {
                // Copy previous document settings.
                if (m_Settings != null)
                {
                    m_Settings.UxmlGuid = AssetDatabase.AssetPathToGUID(newUxmlPath);
                    m_Settings.UxmlPath = newUxmlPath;
                    m_Settings.SaveSettingsToDisk();
                }

                // Reset asset name.
                m_VisualTreeAsset.name = Path.GetFileNameWithoutExtension(newUxmlPath);
                m_OpenendVisualTreeAssetOldPath = newUxmlPath;
            }

            if (documentRootElement != null)
                ReloadDocumentToCanvas(documentRootElement);

            hasUnsavedChanges = false;

            var assetSize = uxmlText?.Length ?? 0;
            BuilderAnalyticsUtility.SendSaveEvent(startTime, this, newUxmlPath, assetSize);

            return true;
        }

        private void SetInlineStyleRecursively(VisualElement ve)
        {
            if (ve == null)
            {
                return;
            }

            var inlineSheet = m_VisualTreeAsset.inlineSheet;
            var vea = ve.GetVisualElementAsset();

            if (vea != null && vea.ruleIndex != -1)
            {
                var rule = inlineSheet.rules[vea.ruleIndex];
                ve.UpdateInlineRule(inlineSheet, rule);
            }

            var children = ve.Children();

            foreach (var child in children)
            {
                SetInlineStyleRecursively(child);
            }
        }

        internal bool SaveNewTemplateFileFromHierarchy(string newTemplatePath, string uxml)
        {
            var isReplacingFile = File.Exists(newTemplatePath);
            bool isReplacingFileInHierarchy = false;

            if (isReplacingFile)
            {
                var replacedVTA = EditorGUIUtility.Load(newTemplatePath) as VisualTreeAsset;
                isReplacingFileInHierarchy = replacedVTA.TemplateExists(m_VisualTreeAsset);

                if (isReplacingFileInHierarchy && hasUnsavedChanges)
                {
                    // If we are replacing an element in the hierarchy and there is unsaved changes,
                    // we need to save to make sure we don't lose any elements
                    var saveUnsavedChanges = BuilderDialogsUtility.DisplayDialog(
                        BuilderConstants.SaveDialogSaveChangesPromptTitle,
                        BuilderConstants.SaveDialogReplaceWithNewTemplateMessage,
                        BuilderConstants.DialogSaveActionOption,
                        BuilderConstants.DialogCancelOption);

                    if (saveUnsavedChanges)
                    {
                        var wasDocumentSaved = SaveUnsavedChanges();

                        if (!wasDocumentSaved)
                        {
                            // Save failed
                            Debug.LogError("Saving the current template failed. New template will not be created.");
                            return false;
                        }
                    }
                    else
                    {
                        // Save cancelled
                        return false;
                    }
                }
            }

            if (isReplacingFileInHierarchy)
            {
                // This is necessary to make sure we don't show the external changes popup
                // since we are creating a new template
                m_DocumentBeingSavedExplicitly = true;
            }

            File.WriteAllText(newTemplatePath, uxml);
            AssetDatabase.Refresh();

            if (isReplacingFileInHierarchy)
            {
                m_DocumentBeingSavedExplicitly = false;
            }

            return true;
        }

        public bool CheckForUnsavedChanges(bool assetModifiedExternally = false)
        {
            if (!hasUnsavedChanges)
                return true;

            if (assetModifiedExternally)
            {
                // TODO: Nothing can be done here yet, other than telling the user
                // what just happened. Adding the ability to save unsaved changes
                // after a file has been modified externally will require some
                // major changes to the document flow.
                var promptTitle = string.Format(BuilderConstants.SaveDialogExternalChangesPromptTitle,
                    m_Document.uxmlPath);
                BuilderDialogsUtility.DisplayDialog(
                    promptTitle,
                    BuilderConstants.SaveDialogExternalChangesPromptMessage);

                return true;
            }
            else
            {
                var option = BuilderDialogsUtility.DisplayDialogComplex(
                    BuilderConstants.SaveDialogSaveChangesPromptTitle,
                    BuilderConstants.SaveDialogSaveChangesPromptMessage,
                    BuilderConstants.DialogSaveActionOption,
                    BuilderConstants.DialogCancelOption,
                    BuilderConstants.DialogDontSaveActionOption);

                switch (option)
                {
                    // Save
                    case 0:
                        return SaveUnsavedChanges();
                    // Cancel
                    case 1:
                        return false;
                    // Don't Save
                    case 2:
                        RestoreAssetsFromBackup();
                        return true;
                }
            }

            return true;
        }

        public void NewDocument(VisualElement documentRootElement)
        {
            Clear();

            ClearCanvasDocumentRootElement(documentRootElement);

            hasUnsavedChanges = false;
        }

        public void LoadDocument(VisualTreeAsset visualTreeAsset, VisualElement documentElement)
        {
            NewDocument(documentElement);

            if (visualTreeAsset == null)
                return;

            m_VisualTreeAssetBackup = visualTreeAsset.DeepCopy();
            m_VisualTreeAsset = visualTreeAsset;

            // Re-stamp orderInDocument values using BuilderConstants.VisualTreeAssetOrderIncrement
            VisualTreeAssetUtilities.ReOrderDocument(m_VisualTreeAsset);

            PostLoadDocumentStyleSheetCleanup();

            hasUnsavedChanges = false;

            m_OpenendVisualTreeAssetOldPath = AssetDatabase.GetAssetPath(m_VisualTreeAsset);

            m_Settings = BuilderDocumentSettings.CreateOrLoadSettingsObject(m_Settings, uxmlPath);

            ReloadDocumentToCanvas(documentElement);
        }

        public void PostLoadDocumentStyleSheetCleanup()
        {
            m_VisualTreeAsset.UpdateUsingEntries();

            m_OpenUSSFiles.Clear();

            // Load styles.
            var styleSheetsUsed = m_VisualTreeAsset.GetAllReferencedStyleSheets();
            for (int i = 0; i < styleSheetsUsed.Count; ++i)
                AddStyleSheetToDocument(styleSheetsUsed[i], null);

            // For the near to mid term, we have this code that cleans up any
            // existing root element stylesheets.
            RemoveLegacyStyleSheetsFromRootAssets();

            hasUnsavedChanges = false;
        }

        private void LoadVisualTreeAsset(VisualTreeAsset newVisualTreeAsset)
        {
            var builderWindow = Builder.ActiveWindow;
            if (builderWindow == null)
                builderWindow = Builder.ShowWindow();

            if (string.IsNullOrEmpty(uxmlPath))
                builderWindow.toolbar.ReloadDocument();
            else
                builderWindow.toolbar.LoadDocument(newVisualTreeAsset, false, true);
        }

        //
        // Asset Change Detection
        //

        public void OnPostProcessAsset(string assetPath)
        {
            if (m_DocumentBeingSavedExplicitly)
                return;

            var newVisualTreeAsset = m_VisualTreeAsset;
            var isCurrentDocumentBeingProcessed = assetPath == uxmlOldPath;
            var wasCurrentDocumentRenamed = uxmlPath == assetPath && assetPath != uxmlOldPath;

            if (isCurrentDocumentBeingProcessed || wasCurrentDocumentRenamed)
            {
                newVisualTreeAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(assetPath);
            }
            else
            {
                bool found = false;
                foreach (var openUSSFile in m_OpenUSSFiles)
                    if (found = openUSSFile.CheckPostProcessAssetIfFileChanged(assetPath))
                        break;

                if (!found)
                    return;
            }

            // LoadDocument() will call Clear() which will try to restore from Backup().
            // If we don't clear the Backups here, they will overwrite the newly post-processed
            // and re-imported asset we detected here.
            ClearBackups();

            if (EditorWindow.HasOpenInstances<Builder>())
            {
                // LoadVisualTreeAsset needs to be delayed to ensure that this is called later, while in a correct state.
                EditorApplication.delayCall += () => LoadVisualTreeAsset(newVisualTreeAsset);
            }
        }

        //
        // Selection
        //

        public void HierarchyChanged(VisualElement element)
        {
            hasUnsavedChanges = true;
        }

        public void StylingChanged()
        {
            hasUnsavedChanges = true;

            // Make sure active stylesheet is still in the document.
            ValidateActiveStyleSheet();
        }

        //
        // Serialization
        //

        void ValidateActiveStyleSheet()
        {
            bool found = false;
            foreach (var openUSSFile in m_OpenUSSFiles)
            {
                if (m_ActiveStyleSheet != openUSSFile.styleSheet)
                    continue;

                found = true;
                break;
            }

            if (!found)
                m_ActiveStyleSheet = firstStyleSheet;
        }

        //
        // Serialization
        //

        public void OnAfterBuilderDeserialize(VisualElement documentRootElement)
        {
            // Refresh StyleSheets.
            var styleSheetsUsed = m_VisualTreeAsset.GetAllReferencedStyleSheets();
            while (m_OpenUSSFiles.Count < styleSheetsUsed.Count)
                m_OpenUSSFiles.Add(new BuilderDocumentOpenUSS());

            // Make sure active stylesheet is still in the document.
            ValidateActiveStyleSheet();

            for (int i = 0; i < styleSheetsUsed.Count; ++i)
            {
                if (m_OpenUSSFiles[i].styleSheet == styleSheetsUsed[i] && m_OpenUSSFiles[i].backupStyleSheet != null)
                {
                    continue;
                }

                m_OpenUSSFiles[i].Set(styleSheetsUsed[i], null);
            }

            while (m_OpenUSSFiles.Count > styleSheetsUsed.Count)
            {
                var lastIndex = m_OpenUSSFiles.Count - 1;
                RemoveStyleSheetFromLists(lastIndex);
            }

            // Fix unserialized rule references in Selectors in StyleSheets.
            // VTA.inlineSheet only has Rules so it does not need this fix.
            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.FixRuleReferences();

            ReloadDocumentToCanvas(documentRootElement);
        }

        public void OnAfterDeserialize()
        {
            // Fix unserialized rule references in Selectors in StyleSheets.
            // VTA.inlineSheet only has Rules so it does not need this fix.
            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.FixRuleReferences();
        }

        public void OnAfterLoadFromDisk()
        {
            // Very important we convert asset references to paths here after a restore.
            if (m_VisualTreeAsset != null)
                m_VisualTreeAsset.UpdateUsingEntries();
        }

        //
        // Private Utilities
        //

        public void RestoreAssetsFromBackup()
        {
            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.RestoreFromBackup();

            if (m_VisualTreeAsset != null && m_VisualTreeAssetBackup != null)
            {
                m_VisualTreeAssetBackup.DeepOverwrite(m_VisualTreeAsset);
                EditorUtility.SetDirty(m_VisualTreeAsset);
                if (hasUnsavedChanges && !isAnonymousDocument)
                {
                    BuilderAssetUtilities.LiveReload(BuilderAssetUtilities.LiveReloadChanges.Hierarchy |
                                                     BuilderAssetUtilities.LiveReloadChanges.Styles);
                }
            }

            hasUnsavedChanges = false;
        }

        void ClearBackups()
        {
            m_VisualTreeAssetBackup.Destroy();
            m_VisualTreeAssetBackup = null;

            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.ClearBackup();
        }

        void ClearUndo()
        {
            m_VisualTreeAsset.ClearUndo();

            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.ClearUndo();
        }

        bool WriteUXMLToFile(string uxmlPath)
        {
            var uxmlText = visualTreeAsset.GenerateUXML(uxmlPath, true);

            // This will only be null (not empty) if the UXML is invalid in some way.
            if (uxmlText == null)
                return false;

            return WriteUXMLToFile(uxmlPath, uxmlText);
        }

        bool WriteUXMLToFile(string uxmlPath, string uxmlText)
        {
            // Make sure the folders exist.
            var uxmlFolder = Path.GetDirectoryName(uxmlPath);
            if (!Directory.Exists(uxmlFolder))
                Directory.CreateDirectory(uxmlFolder);

            return BuilderAssetUtilities.WriteTextFileToDisk(uxmlPath, uxmlText);
        }

        VisualElement ReloadChildToCanvas(BuilderDocumentOpenUXML childOpenUXML, VisualElement rootElement)
        {
            var childRootElement = rootElement;
            if (childOpenUXML.openSubDocumentParentSourceTemplateAssetIndex > -1)
            {
                var parentOpenUXML = openUXMLFiles[childOpenUXML.openSubDocumentParentIndex];
                rootElement = ReloadChildToCanvas(parentOpenUXML, rootElement);

                var targetTemplateAsset = parentOpenUXML.visualTreeAsset.templateAssets[childOpenUXML.openSubDocumentParentSourceTemplateAssetIndex];
                var templateContainerQuery = rootElement.Query<TemplateContainer>().Where(container =>
                    container.GetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName) as TemplateAsset == targetTemplateAsset);
                var foundTemplateContainer = templateContainerQuery.ToList().First();
                childRootElement = foundTemplateContainer;
            }

            ReloadDocumentHierarchyToCanvas(childOpenUXML.visualTreeAsset, childRootElement);
            childOpenUXML.ReloadStyleSheetsToCanvas(childRootElement);
            return childRootElement;
        }

        void ClearCanvasDocumentRootElement(VisualElement documentRootElement)
        {
            if (documentRootElement == null)
                return;

            documentRootElement.Clear();
            ResetCanvasDocumentRootElementStyleSheets(documentRootElement);
            BuilderSharedStyles.ClearContainer(documentRootElement);

            documentRootElement.SetProperty(
                BuilderConstants.ElementLinkedVisualTreeAssetVEPropertyName, visualTreeAsset);
        }

        internal void ResetCanvasDocumentRootElementStyleSheets(VisualElement documentRootElement)
        {
            documentRootElement.styleSheets.Clear();

            // Restore the active theme stylesheet
            if (documentRootElement.HasProperty(BuilderConstants.ElementLinkedActiveThemeStyleSheetVEPropertyName))
            {
                var activeThemeStyleSheet = documentRootElement.GetProperty(BuilderConstants.ElementLinkedActiveThemeStyleSheetVEPropertyName) as StyleSheet;

                if (activeThemeStyleSheet != null)
                {
                    documentRootElement.styleSheets.Add(activeThemeStyleSheet);
                }
            }
        }

        void ReloadDocumentToCanvas(VisualElement documentRootElement)
        {
            if (documentRootElement == null)
                return;

            ClearCanvasDocumentRootElement(documentRootElement);

            if (visualTreeAsset == null)
                return;

            var childRootElement = ReloadChildToCanvas(this, documentRootElement);
            ReloadStyleSheetElements(documentRootElement);

            // Lighten opacity of all sibling documents throughout hierarchy
            var currentRoot = childRootElement;
            while (currentRoot != documentRootElement)
            {
                var currentParent = currentRoot.parent;
                foreach (var sibling in currentParent.Children())
                {
                    if (sibling == currentRoot)
                        continue;
                    sibling.style.opacity = BuilderConstants.OpacityFadeOutFactor;
                }

                currentRoot = currentParent;
            }
        }

        void ReloadDocumentHierarchyToCanvas(VisualTreeAsset vta, VisualElement parentElement)
        {
            if (parentElement == null)
                return;

            parentElement.Clear();
            // Load the asset.
            try
            {
                vta.LinkedCloneTree(parentElement);
            }
            catch (Exception e)
            {
                Debug.LogError("Invalid UXML or USS: " + e.ToString());
                Clear();
            }

            parentElement.SetProperty(
                BuilderConstants.ElementLinkedVisualTreeAssetVEPropertyName, vta);
        }

        void ReloadStyleSheetsToCanvas(VisualElement documentRootElement)
        {
            m_CurrentDocumentRootElement = documentRootElement;

            // Refresh styles.
            RefreshStyle(documentRootElement);
        }

        void ReloadStyleSheetElements(VisualElement documentRootElement)
        {
            // Add shared styles.

            BuilderSharedStyles.ClearContainer(documentRootElement);
            BuilderSharedStyles.AddSelectorElementsFromStyleSheet(documentRootElement, m_OpenUSSFiles);

            var parentIndex = openSubDocumentParentIndex;
            while (parentIndex > -1)
            {
                var parentUXML = openUXMLFiles[parentIndex];
                var parentUSSFiles = parentUXML.openUSSFiles;
                BuilderSharedStyles.AddSelectorElementsFromStyleSheet(documentRootElement, parentUSSFiles, m_OpenUSSFiles.Count, true, parentUXML.uxmlFileName);

                parentIndex = parentUXML.openSubDocumentParentIndex;
            }
        }

        void PreSaveSyncBackup()
        {
            if (m_VisualTreeAssetBackup == null)
                m_VisualTreeAssetBackup = m_VisualTreeAsset.DeepCopy();
            else
                m_VisualTreeAsset.DeepOverwrite(m_VisualTreeAssetBackup);
        }

        bool PostSaveToDiskChecksAndFixes(string newUxmlPath, bool needsFullRefresh)
        {
            var oldVTAReference = m_VisualTreeAsset;
            var oldUxmlPath = uxmlPath;
            var hasNewUxmlPath = !string.IsNullOrEmpty(newUxmlPath) && newUxmlPath != oldUxmlPath;
            var localUxmlPath = !string.IsNullOrEmpty(newUxmlPath) ? newUxmlPath : oldUxmlPath;

            m_VisualTreeAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(localUxmlPath);
            var newIsDifferentFromOld = m_VisualTreeAsset != oldVTAReference;

            // If we have a new uxmlPath, it means we're saving as and we need to reset the
            // original document to stock.
            if (hasNewUxmlPath && newIsDifferentFromOld && !string.IsNullOrEmpty(oldUxmlPath))
            {
                m_DocumentBeingSavedExplicitly = true;
                AssetDatabase.ImportAsset(oldUxmlPath, ImportAssetOptions.ForceUpdate);
                m_DocumentBeingSavedExplicitly = false;
            }

            needsFullRefresh |= newIsDifferentFromOld;

            // Check if the UXML asset has changed and reload it.
            if (needsFullRefresh)
            {
                // To get all the selection markers into the new assets.
                m_VisualTreeAssetBackup.DeepOverwrite(m_VisualTreeAsset);
                m_VisualTreeAsset.UpdateUsingEntries();

                // Update hash. Otherwise we end up with the old overwritten contentHash
                var hash = UXMLImporterImpl.GenerateHash(localUxmlPath);
                m_VisualTreeAsset.contentHash = hash.GetHashCode();
            }

            return needsFullRefresh;
        }
    }
}
