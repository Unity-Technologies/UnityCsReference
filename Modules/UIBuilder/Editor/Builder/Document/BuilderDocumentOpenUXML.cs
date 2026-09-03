// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using Unity.UIToolkit.Editor;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    [Serializable]
    [ExtensionOfNativeClass]
    partial class BuilderDocumentOpenUXML
    {
        //
        // Serialized Data
        //

        [SerializeField]
        List<BuilderDocumentOpenUSS> m_OpenUSSFiles = new List<BuilderDocumentOpenUSS>();

        [SerializeField]
        string m_OpenendVisualTreeAssetOldPath;

        [SerializeField]
        private string m_UxmlPreview;

        [SerializeField]
        private int m_ContentHash;

        [SerializeField, FormerlySerializedAs("m_VisualTreeAsset")]
        LazyLoadReference<VisualTreeAsset> m_VisualTreeAssetRef;

        [SerializeField]
        int m_TemplateAssetId;

        [SerializeField]
        int m_TemplateAssetIndex = -1;

        [SerializeField]
        StyleSheet m_ActiveStyleSheet;

        [SerializeField]
        BuilderDocumentSettings m_Settings;

        [SerializeField]
        int m_OpenSubDocumentParentIndex = -1;

        //
        // Unserialized Data
        //

        bool m_HasUnsavedChanges;
        bool m_DocumentBeingSavedExplicitly;
        // Stylesheet reimports we told the registry we would drive ourselves, so we can hand it the matching
        // reload notifications once our own reload has settled.
        List<StyleSheet> m_ClaimedExternalStyleSheets;
        // The assets we last reported open to the registry, so we can release assets that were dropped.
        // Not serialized on purpose: the registry's own open set does not survive a domain reload either.
        List<Object> m_ReportedAssets;
        bool m_ExternalReimportInProgress;
        BuilderUXMLFileSettings m_FileSettings;
        BuilderDocument m_Document;
        VisualTreeAsset m_VisualTreeAsset;
        VisualTreeAsset m_VisualTreeAssetBackup;
        VisualElement m_CurrentDocumentRootElement;

        //
        // Getters
        //

        // Used in tests
        internal bool isBackupSet => m_VisualTreeAssetBackup != null;

        [AutoStaticsCleanupOnCodeReload]
        internal static Action<string, string> s_WriteToDiskCallback = WriteToDisk;
        [AutoStaticsCleanupOnCodeReload]
        internal static Func<string, string, string> s_SaveFileDialogCallback = DisplaySaveFileDialogForTempFile;
        [NoAutoStaticsCleanup] // Monotonic temp-file name counter; persisting it just continues the sequence, never reset, safe across reload.
        internal static int s_UxmlTempFileCounter = 1;
        [NoAutoStaticsCleanup] // Monotonic temp-file name counter; persisting it just continues the sequence, never reset, safe across reload.
        internal static int s_UssTempFileCounter = 1;

        public BuilderUXMLFileSettings fileSettings => m_FileSettings ?? (m_FileSettings = new BuilderUXMLFileSettings(visualTreeAsset, document));

        // The owning document assigns itself on creation, deserialization and load; the fallback only covers a
        // file that was never attached to a document, which matches the old find-the-singleton behavior.
        internal BuilderDocument document
        {
            get
            {
                if (m_Document == null)
                    m_Document = BuilderDocument.FindOrCreateDefaultInstance();

                return m_Document;
            }
            set => m_Document = value;
        }

        internal List<BuilderDocumentOpenUXML> openUXMLFiles => document.openUXMLFiles;

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
                // If this uxml is being edited in place then use the parent document's settings
                if (isChildSubDocument && templateAsset != null)
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

        public TemplateAsset templateAsset
        {
            get
            {
                if (m_TemplateAssetId == 0)
                    return null;

                if (!isChildSubDocument)
                    return null;

                var parentDocument = openSubDocumentParent;
                var templateAssets = parentDocument.visualTreeAsset.DepthFirstTraversalOfType<TemplateAsset>();
                var idx = 0;

                foreach (var template in templateAssets)
                {
                    if (template.id == m_TemplateAssetId)
                    {
                        m_TemplateAssetIndex = idx;
                        return template;
                    }

                    // If there was a change during reload, the template id might be out of sync. Therefore, we will
                    // rely on the active index.
                    if (idx == m_TemplateAssetIndex)
                    {
                        m_TemplateAssetId = template.id;
                        m_TemplateAssetIndex = -1;
                        return template;
                    }
                    idx++;
                }

                return null;
            }
            set => m_TemplateAssetId = value?.id ?? 0;
        }

        public int templateAssetIndex
        {
            get => m_TemplateAssetIndex;
            internal set => m_TemplateAssetIndex = value;
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
            get { return AssetDatabase.GetAssetPath(visualTreeAsset); }
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

        public string uxmlPreview => m_UxmlPreview;

        public VisualTreeAsset visualTreeAsset
        {
            get
            {
                if (m_VisualTreeAsset != null)
                    return m_VisualTreeAsset;

                if (m_VisualTreeAssetRef.isSet && m_VisualTreeAssetRef.asset != null)
                    m_VisualTreeAsset = m_VisualTreeAssetRef.asset;
                else
                    m_VisualTreeAsset = VisualTreeAssetUtilities.CreateInstanceWithHideFlags();

                m_ContentHash = m_VisualTreeAsset.contentHash;
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

        bool m_IsLoadQueued;

        public bool isLoadQueued { get => m_IsLoadQueued; internal set => m_IsLoadQueued = value; }

        //
        // Initialize / Construct / Enable / Clear
        //

        public void Clear()
        {
            var sharedWithAnotherEditor =
                UIAssetRegistry.LiveInstance?.IsOpenForWritingByOther(m_VisualTreeAsset, this) ?? false;

            // Using LiveInstance here intentionally because Clear() runs from the BuilderDocument constructor,
            // where accessing ScriptableSingleton.instance (which loads a file) is illegal. If the registry isn't alive
            // yet there is nothing tracked to release anyway.
            UIAssetRegistry.LiveInstance?.CloseAll(this);
            m_ReportedAssets?.Clear();

            ClearUndo();

            if (!sharedWithAnotherEditor)
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
                m_VisualTreeAssetRef = null;
            }

            foreach (var openUSSFile in m_OpenUSSFiles)
            {
                openUSSFile.Clear();
            }

            m_OpenUSSFiles.Clear();
            m_UxmlPreview = string.Empty;
            m_ContentHash = 0;
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
            foreach (var openUSS in m_OpenUSSFiles)
            {
                openUSS.styleSheet.RequestRebuild(StyleSheet.RebuildOptions.Synchronous);
            }
            m_CurrentDocumentRootElement.IncrementVersion(VersionChangeTypeUtility.StylingChanged());
        }

        public void MarkStyleSheetsDirty()
        {
            foreach (var openUSS in m_OpenUSSFiles)
                EditorUtility.SetDirty(openUSS.styleSheet);
        }

        public void AddStyleSheetToDocument(StyleSheet styleSheet, string ussPath, int index = -1)
        {
            var newOpenUssFile = new BuilderDocumentOpenUSS();
            newOpenUssFile.Set(styleSheet, ussPath);

            if (index == -1)
                m_OpenUSSFiles.Add(newOpenUssFile);
            else
                m_OpenUSSFiles.Insert(index, newOpenUssFile);

            AddStyleSheetsToAllRootElements();

            if (styleSheet != null)
                UIAssetRegistry.instance.Open(styleSheet, this, UIAssetAccess.ReadWrite);

            hasUnsavedChanges = true;
        }

        public void RemoveStyleSheetFromDocument(int ussIndex)
        {
            var removedStyleSheet = ussIndex >= 0 && ussIndex < m_OpenUSSFiles.Count
                ? m_OpenUSSFiles[ussIndex].styleSheet
                : null;

            RemoveStyleSheetFromLists(ussIndex);

            AddStyleSheetsToAllRootElements();

            if (removedStyleSheet != null)
                UIAssetRegistry.instance.Close(removedStyleSheet, this);

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
            }
        }

        public void AddStyleSheetsToAllRootElements(string newUssPath = null, int newUssIndex = 0)
        {
            var rootVEA = visualTreeAsset.visualTree;
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

            var savedContext = new VisualTreeAssetEditingContext(visualTreeAsset);

            // Flag the owner while the save (and its pre/post commands) runs, so it can tell its own save from
            // one arriving out of another Builder window.
            var ownerDocument = document;
            ownerDocument.isSavingOwnDocument = true;

            // Starts false so a save that throws before writing anything reports as failed to PostSaveCommand
            // in the finally (a "successful" report would rebaseline the registry over unsaved edits).
            var succeeded = false;
            try
            {
                PreSaveCommand.Execute(CommandSources.Builder, savedContext);
                ClearUndo();

                succeeded = true;

                var startTime = DateTime.UtcNow;
                using var pool = ListPool<BuilderDocumentOpenUSS>.Get(out var savedUSSFiles);

                // Save USS files.
                foreach (var openUSSFile in m_OpenUSSFiles)
                {
                    if (openUSSFile.SaveToDisk(visualTreeAsset, out var ussFailed))
                    {
                        savedUSSFiles.Add(openUSSFile);
                    }
                    succeeded &= !ussFailed;
                }

                var oldUxmlTest = m_VisualTreeAssetBackup ? m_VisualTreeAssetBackup.GenerateUXML() : string.Empty;

                // Save UXML files
                // Saving all open UXML files to ensure references correct upon changes in child documents.
                foreach (var openUXMLFile in openUXMLFiles)
                    openUXMLFile.PreSaveSyncBackup();

                bool shouldSave = m_OpenendVisualTreeAssetOldPath != newUxmlPath;
                var uxmlText = visualTreeAsset.GenerateUXML();

                if (uxmlText != null)
                {
                    if (!shouldSave && m_VisualTreeAssetBackup)
                    {
                        shouldSave = oldUxmlTest != uxmlText;
                    }

                    if (shouldSave)
                    {
                        succeeded &= BuilderAssetUtilities.WriteTextFileToDisk(newUxmlPath, uxmlText);
                    }
                }
                else
                {
                    // A null (not empty) export means the document could not be serialized at all.
                    succeeded = false;
                }

                needsFullRefresh |= ReloadDocument(documentRootElement, newUxmlPath, savedUSSFiles);

                // ReloadDocument clears the unsaved flag as part of its normal post-save bookkeeping. If a write
                // failed the changes are still only in memory, so put the "*" back.
                if (!succeeded)
                    hasUnsavedChanges = true;

                var assetSize = uxmlText?.Length ?? 0;
                BuilderAnalyticsUtility.SendSaveEvent(startTime, this, newUxmlPath, assetSize);
            }
            finally
            {
                try
                {
                    PostSaveCommand.Execute(CommandSources.Builder, savedContext, succeeded);
                }
                finally
                {
                    ownerDocument.isSavingOwnDocument = false;
                }
            }

            return succeeded;
        }

        bool ReloadDocument(VisualElement documentRootElement, string newUxmlPath, List<BuilderDocumentOpenUSS> savedUSSFiles)
        {
            var needsFullRefresh = false;

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
                visualTreeAsset.name = Path.GetFileNameWithoutExtension(newUxmlPath);
                m_OpenendVisualTreeAssetOldPath = newUxmlPath;
            }

            if (documentRootElement != null)
                ReloadDocumentToCanvas(documentRootElement);

            ClearVisualTreeAssetDirtyFlags();
            foreach (var openUSSFile in m_OpenUSSFiles)
            {
                EditorUtility.ClearDirty(openUSSFile.styleSheet);
            }

            hasUnsavedChanges = false;
            return needsFullRefresh;
        }

        private void SetInlineStyleRecursively(VisualElement ve)
        {
            if (ve == null)
            {
                return;
            }

            var inlineSheet = visualTreeAsset.GetOrCreateInlineStyleSheet();
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
                isReplacingFileInHierarchy = replacedVTA.TemplateExists(visualTreeAsset);

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
                // The reimport may not have actually changed the exported content (e.g. a touch/no-op import);
                // in that case keep our unsaved edits silently rather than resolving a non-existent conflict.
                var ussWasModified = false;
                foreach (var openUssFile in m_OpenUSSFiles)
                {
                    var h = new Hash128();
                    byte[] b = Encoding.UTF8.GetBytes(openUssFile.ussPreview);
                    if (b.Length > 0)
                    {
                        HashUtilities.ComputeHash128(b, ref h);
                    }

                    if (openUssFile.contentHash != h.GetHashCode())
                    {
                        ussWasModified = true;
                        break;
                    }
                }

                if (m_ContentHash == visualTreeAsset.contentHash && !ussWasModified)
                {
                    RestoreUnsavedChanges();
                    return false;
                }

                // A genuine external change conflicts with our unsaved edits. The UIAssetRegistry is the single
                // resolver: ask it for the decision instead of prompting ourselves, then apply the result.
                switch (UIAssetRegistry.instance.ResolveExternalChange(visualTreeAsset))
                {
                    case UIAssetConflictChoice.Keep:
                        RestoreUnsavedChanges();
                        return false;
                    case UIAssetConflictChoice.UseImported:
                        return true;
                    case UIAssetConflictChoice.SaveBackupAndUseImported:
                        WritePreviewToDiskAndUseExternalChanges();
                        return true;
                }
            }
            else
            {
                var option = BuilderDialogsUtility.DisplayDialogComplex(
                    BuilderConstants.SaveDialogSaveChangesPromptTitle,
                    BuilderConstants.SaveDialogSaveChangesPromptMessage,
                    BuilderConstants.DialogSaveActionOption,
                    BuilderConstants.DialogCancelOption,
                    BuilderConstants.DialogDiscardChangesActionOption);

                switch (option)
                {
                    // Save
                    case 0:
                        return SaveUnsavedChanges();
                    // Cancel
                    case 1:
                        return false;
                    case 2:
                        UIAssetRegistry.instance.DiscardAsset(visualTreeAsset, this);
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

            // LazyLoadReference rejects non-persistent targets; such as an unsaved/in-memory document. (UUM-142891)
            m_VisualTreeAssetRef = EditorUtility.IsPersistent(visualTreeAsset) ? visualTreeAsset : null;

            m_ContentHash = m_VisualTreeAsset.contentHash;

            PostLoadDocumentStyleSheetCleanup();

            hasUnsavedChanges = false;

            m_OpenendVisualTreeAssetOldPath = AssetDatabase.GetAssetPath(visualTreeAsset);

            m_Settings = BuilderDocumentSettings.CreateOrLoadSettingsObject(m_Settings, uxmlPath);

            ReloadDocumentToCanvas(documentElement);
            GenerateUxmlPreview();
            GenerateUssPreview();

            // The asset may already have unsaved changes from another tool (e.g. it was edited in the UI Stage
            // before being opened here). LoadDocument forces a clean state, so reflect the registry's truth
            // instead of showing no "*".
            if (ReportOpenAssetsToRegistry())
                hasUnsavedChanges = true;
        }

        /// <summary>
        /// Reports the currently open document (and its stylesheets) to the unified asset registry as read-write,
        /// so dirtiness/save/discard stay coordinated with the other authoring tools. Returns whether any of them
        /// already had unsaved changes.
        /// </summary>
        internal bool ReportOpenAssetsToRegistry()
        {
            var registry = UIAssetRegistry.instance;

            using var _ = ListPool<Object>.Get(out var current);
            // A never-saved document has no file, so no other tool can share it and the registry has nothing to
            // coordinate — and a save of it would fail the registry's "no path" check.
            if (visualTreeAsset != null && EditorUtility.IsPersistent(visualTreeAsset))
                current.Add(visualTreeAsset);
            foreach (var openUss in m_OpenUSSFiles)
                if (openUss?.styleSheet != null)
                    current.Add(openUss.styleSheet);

            var anyAlreadyDirty = false;
            foreach (var asset in current)
            {
                registry.Open(asset, this, UIAssetAccess.ReadWrite);
                anyAlreadyDirty |= registry.IsDirty(asset);
            }

            // Release only what we actually dropped. Closing everything first and re-opening would momentarily
            // release the last read-write reference to a dirty asset, which the registry reads as "this needs
            // saving" — a spurious save request on every re-report (an undo, a discard, a domain reload).
            if (m_ReportedAssets != null)
            {
                foreach (var previous in m_ReportedAssets)
                    if (!ReferenceEquals(previous, null) && !current.Contains(previous))
                        registry.Close(previous, this);
            }

            m_ReportedAssets ??= new List<Object>();
            m_ReportedAssets.Clear();
            m_ReportedAssets.AddRange(current);

            return anyAlreadyDirty;
        }

        public void PostLoadDocumentStyleSheetCleanup()
        {
            visualTreeAsset.UpdateUsingEntries();

            m_OpenUSSFiles.Clear();

            // Load styles.
            var styleSheetsUsed = visualTreeAsset.GetAllReferencedStyleSheets();
            for (int i = 0; i < styleSheetsUsed.Count; ++i)
                AddStyleSheetToDocument(styleSheetsUsed[i], null);

            hasUnsavedChanges = false;
        }

        private void LoadVisualTreeAsset(VisualTreeAsset newVisualTreeAsset)
        {
            // Reload into the window that owns this document, never another Builder instance. If the owning
            // window is gone there is no canvas to reload; external holders are notified separately.
            var builderWindow = document.primaryViewportWindow as Builder;
            if (builderWindow == null || builderWindow.document != document)
                return;

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
            if (m_DocumentBeingSavedExplicitly || m_ExternalReimportInProgress)
                return;

            // Only participate when this document's own Builder window is open to drive the reload. Otherwise
            // leave the external change entirely to the asset registry's own push handling (which the claim below
            // would suppress) — so we never claim a reimport we won't reload, orphaning it.
            var builderOpen = document.primaryViewportWindow is Builder;

            var newVisualTreeAsset = visualTreeAsset;
            var isCurrentDocumentBeingProcessed = assetPath == uxmlOldPath;
            var wasCurrentDocumentRenamed = uxmlPath == assetPath && assetPath != uxmlOldPath;

            if (isCurrentDocumentBeingProcessed || wasCurrentDocumentRenamed)
            {
                // Claim this reimport so the UIAssetRegistry defers its own external-change handling.
                if (isCurrentDocumentBeingProcessed && builderOpen)
                    UIAssetRegistry.instance.ClaimExternalChange(visualTreeAsset);

                newVisualTreeAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(assetPath);
            }
            else
            {
                bool found = false;
                StyleSheet trackedSheet = null;
                foreach (var openUSSFile in m_OpenUSSFiles)
                {
                    // Capture the currently-held (registry-tracked) instance before CheckPostProcessAssetIfFileChanged
                    // swaps in the freshly reimported one.
                    var previousSheet = openUSSFile.styleSheet;
                    if (found = openUSSFile.CheckPostProcessAssetIfFileChanged(assetPath))
                    {
                        trackedSheet = previousSheet;
                        break;
                    }
                }

                if (!found)
                    return;

                // Claim the stylesheet reimport using the instance the registry actually tracks (the one we held
                // before the reload). A reimport that replaces the managed object gives it a new entity id the
                // registry has not rebound to yet, so claiming the freshly loaded instance would silently no-op.
                if (builderOpen && trackedSheet != null)
                {
                    UIAssetRegistry.instance.ClaimExternalChange(trackedSheet);
                    // Claiming makes the registry skip its own push for this asset, so we owe it the reload
                    // notification once we are done — otherwise every other holder (the UI Stage) keeps
                    // rendering the pre-import styles.
                    m_ClaimedExternalStyleSheets ??= new List<StyleSheet>();
                    if (!m_ClaimedExternalStyleSheets.Contains(trackedSheet))
                        m_ClaimedExternalStyleSheets.Add(trackedSheet);
                }
            }

            // LoadDocument() will call Clear() which will try to restore from Backup().
            // If we don't clear the Backups here, they will overwrite the newly post-processed
            // and re-imported asset we detected here.
            ClearBackups();

            if (builderOpen)
            {
                // LoadVisualTreeAsset needs to be delayed to ensure that this is called later, while in a correct state.
                // If there isn't already a call to LoadVisualTreeAsset in the queue, we add one.
                if (!m_IsLoadQueued)
                {
                    EditorApplication.delayCall += () =>
                    {
                        m_IsLoadQueued = false;
                        LoadVisualTreeAsset(newVisualTreeAsset);

                        // Now that the reimport/keep existing change has been resolved, notify external tools/windows
                        // to reload as well — for every asset whose reimport we claimed, not just the document.
                        UIAssetRegistry.instance.NotifyExternalReload(visualTreeAsset);
                        if (m_ClaimedExternalStyleSheets != null)
                        {
                            foreach (var claimed in m_ClaimedExternalStyleSheets)
                                UIAssetRegistry.instance.NotifyExternalReload(claimed);
                            m_ClaimedExternalStyleSheets.Clear();
                        }
                    };
                    m_IsLoadQueued = true;
                }
            }
        }

        //
        // Selection
        //

        public void HierarchyChanged(VisualElement element)
        {
            hasUnsavedChanges = true;
            RefreshUnsavedSnapshots();
        }

        public void StylingChanged()
        {
            hasUnsavedChanges = true;
            RefreshUnsavedSnapshots();

            // Make sure active stylesheet is still in the document.
            ValidateActiveStyleSheet();
        }

        // Re-exports the document and its stylesheets so the registry's "unsaved snapshot" of each matches what
        // we currently have in memory.
        //
        // This has to happen eagerly on every edit. The snapshot is what "Keep My Changes" restores after an
        // external reimport, and a reimport replaces the in-memory asset before anyone gets a chance to react —
        // so a snapshot captured later than the edit is already too late. Regenerating it only when the UXML
        // code-preview pane happens to refresh (which ignores several change types, inline styles among them)
        // silently answered "keep" with pre-edit content.
        void RefreshUnsavedSnapshots()
        {
            if (visualTreeAsset == null)
                return;

            GenerateUxmlPreview();
            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile?.GeneratePreview();
        }

        //
        // Serialization
        //

        internal void GenerateUxmlPreview()
        {
            m_UxmlPreview = visualTreeAsset.GenerateUXML(); // Set this to false to see the special selection elements and attributes.
            UIAssetRegistry.instance.SetUnsavedSnapshot(visualTreeAsset, m_UxmlPreview);
        }

        void GenerateUssPreview()
        {
            foreach (var openUSSFile in m_OpenUSSFiles)
            {
                openUSSFile.GeneratePreview();
            }
        }

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

        public void OnAfterBuilderDeserialize(VisualElement documentRootElement, bool restoringUnsavedChanges = false)
        {
            // Refresh StyleSheets.
            var styleSheetsUsed = visualTreeAsset.GetAllReferencedStyleSheets();
            while (m_OpenUSSFiles.Count < styleSheetsUsed.Count)
                m_OpenUSSFiles.Add(new BuilderDocumentOpenUSS());

            for (int i = 0; i < styleSheetsUsed.Count; ++i)
            {
                if (m_OpenUSSFiles[i].styleSheet == styleSheetsUsed[i] && m_OpenUSSFiles[i].backupStyleSheet != null)
                {
                    continue;
                }

                m_OpenUSSFiles[i].Set(styleSheetsUsed[i], null, !restoringUnsavedChanges);
            }

            while (m_OpenUSSFiles.Count > styleSheetsUsed.Count)
            {
                var lastIndex = m_OpenUSSFiles.Count - 1;
                RemoveStyleSheetFromLists(lastIndex);
            }

            // Make sure active stylesheet is still in the document.
            ValidateActiveStyleSheet();

            // The loops above add, replace and drop entries in m_OpenUSSFiles directly (an undo/redo, a discard,
            // a domain reload), so the registry's view of what we hold is now stale: a dropped slot would leak a
            // read-write reference that is never released, and a re-adopted one would be edited untracked — where
            // the registry reports it clean and the next external save clears our "*" over real unsaved edits.
            ReportOpenAssetsToRegistry();

            ReloadDocumentToCanvas(documentRootElement);
        }

        public void OnAfterDeserialize()
        {
        }

        internal void ReopenAsset(VisualTreeAsset asset)
        {
            m_VisualTreeAssetRef = asset;
            m_OpenendVisualTreeAssetOldPath = AssetDatabase.GetAssetPath(asset);
            OnAfterLoadFromDisk();
        }

        public void OnAfterLoadFromDisk()
        {
            if (m_VisualTreeAssetRef.isSet && m_VisualTreeAssetRef.asset != null)
            {
                // Very important we convert asset references to paths here after a restore.
                m_VisualTreeAssetRef.asset.UpdateUsingEntries();

                // Make sure we have a backup after loading from disk
                m_VisualTreeAssetBackup = m_VisualTreeAssetRef.asset.DeepCopy();
            }
        }

        //
        // Private Utilities
        //

        static void WriteToDisk(string path, string content)
        {
            BuilderAssetUtilities.WriteTextFileToDisk(path, content);
        }

        public void RestoreAssetsFromBackup()
        {
            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.RestoreFromBackup();

            if (m_VisualTreeAsset != null && m_VisualTreeAssetBackup != null)
            {
                m_VisualTreeAssetBackup.DeepOverwrite(m_VisualTreeAsset);
                m_ContentHash = m_VisualTreeAsset.contentHash;

                // Restore the VTA name.
                if (!string.IsNullOrEmpty(uxmlOldPath))
                    m_VisualTreeAsset.name = Path.GetFileNameWithoutExtension(uxmlOldPath);

                if (hasUnsavedChanges && !isAnonymousDocument)
                {
                    UIElementsUtility.MarkVisualTreeAssetAsChanged(m_VisualTreeAsset);
                    UIElementsUtility.MarkVisualTreeAssetAsChanged(m_VisualTreeAssetBackup);
                }

                ClearVisualTreeAssetDirtyFlags();
            }

            hasUnsavedChanges = false;
        }

        internal void NotifyRegistryOfDiscard()
        {
            if (visualTreeAsset != null)
                UIAssetRegistry.instance.NotifyReverted(visualTreeAsset);
        }

        public void RestoreUnsavedChanges()
        {
            ClearUndo();
            ClearBackups();

            // Save USS files.
            foreach (var openUSSFile in m_OpenUSSFiles)
            {
                // Reimport with the uss preview from the UI Builder.
                openUSSFile.RestoreUnsavedChanges();
            }

            // Reimport the uxml preview from the UI Builder.
            var uxmlImporter = new BuilderVisualTreeAssetImporter();
            uxmlImporter.ImportXmlFromString(uxmlPreview, out var restoredVisualTreeAsset);
            restoredVisualTreeAsset.name = visualTreeAsset.name;
            restoredVisualTreeAsset.DeepOverwrite(visualTreeAsset);

            OnAfterBuilderDeserialize(m_CurrentDocumentRootElement, true);

            hasUnsavedChanges = true;
        }

        void WritePreviewToDiskAndUseExternalChanges()
        {
            // Write the current preview to disk in a TEMP file using timestamp to avoid conflicts.
            foreach (var openUSSFile in m_OpenUSSFiles)
            {
                // Ask user for path
                var indexOfLastSlash = openUSSFile.assetPath.LastIndexOf("/");
                var ussFileName = openUSSFile.assetPath.Substring(indexOfLastSlash + 1, openUSSFile.assetPath.LastIndexOf(".") - indexOfLastSlash - 1);
                var ussPath = s_SaveFileDialogCallback($"{ussFileName} ({s_UssTempFileCounter++}).backup", "uss");
                if (ussPath == null)
                    continue;
                s_WriteToDiskCallback.Invoke(ussPath, openUSSFile.ussPreview);
            }

            var uxmlPath = s_SaveFileDialogCallback($"{visualTreeAsset.name} ({s_UxmlTempFileCounter++}).backup", "uxml");
            if (uxmlPath == null)
                return;
            s_WriteToDiskCallback.Invoke(uxmlPath, uxmlPreview);
        }

        static string DisplaySaveFileDialogForTempFile(string defaultFileName, string extension)
        {
            var title = $"Save Temporary {extension.ToUpper()} File with UI Builder Changes";
            var path = BuilderDialogsUtility.DisplaySaveFileDialog(
                title, null, defaultFileName, extension);
            return path;
        }

        // internal because it's used in tests
        internal void ClearBackups()
        {
            if (m_VisualTreeAssetBackup != null)
            {
                m_VisualTreeAssetBackup.Destroy();
                m_VisualTreeAssetBackup = null;
            }

            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.ClearBackup();
        }

        void ClearUndo()
        {
            // Destroy temp serialized data needed for undo/redo
            if (m_CurrentDocumentRootElement != null)
            {
                var elements = m_CurrentDocumentRootElement.Query<VisualElement>();
                elements.ForEach(x =>
                {
                    var tempSerializedData = x.GetProperty(BuilderConstants.InspectorTempSerializedDataPropertyName) as Object;
                    if (tempSerializedData == null)
                    {
                        return;
                    }

                    Object.DestroyImmediate(tempSerializedData);
                });
            }

            m_VisualTreeAsset.ClearUndo();

            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.ClearUndo();
        }

        void ClearVisualTreeAssetDirtyFlags()
        {
            EditorUtility.ClearDirty(visualTreeAsset);
            EditorUtility.ClearDirty(visualTreeAsset.inlineSheet);
        }

        bool WriteUXMLToFile(string uxmlPath)
        {
            var uxmlText = visualTreeAsset.GenerateUXML();

            // This will only be null (not empty) if the UXML is invalid in some way.
            if (uxmlText == null)
                return false;

            return BuilderAssetUtilities.WriteTextFileToDisk(uxmlPath, uxmlText);
        }

        VisualElement ReloadChildToCanvas(BuilderDocumentOpenUXML childOpenUXML, VisualElement rootElement)
        {
            var childRootElement = rootElement;
            if (childOpenUXML.templateAsset != null)
            {
                var parentOpenUXML = openUXMLFiles[childOpenUXML.openSubDocumentParentIndex];
                rootElement = ReloadChildToCanvas(parentOpenUXML, rootElement);

                var targetTemplateAsset = childOpenUXML.templateAsset;
                var templateContainerQuery = rootElement.Query<TemplateContainer>().Where(container =>
                    container.GetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName) as TemplateAsset == targetTemplateAsset);
                var foundTemplateContainer = templateContainerQuery.First();
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

            if (documentRootElement is TemplateContainer rootTemplate)
            {
                // If the contentContainer was set then
                if (documentRootElement.contentContainer != documentRootElement)
                {
                    // Clear the hierarchy
                    documentRootElement.hierarchy.Clear();

                    // Reset the content container
                    rootTemplate.SetContentContainer(rootTemplate);
                }
            }

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

            // Do not display styles from parent documents if this is a subdocument open in isolation.
            bool isIsolationMode = isChildSubDocument && templateAsset == null;

            while (parentIndex > -1 && !isIsolationMode)
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
                m_VisualTreeAssetBackup = visualTreeAsset.DeepCopy();
            else
                visualTreeAsset.DeepOverwrite(m_VisualTreeAssetBackup);
        }

        internal void ResyncBackupToCurrentAsset()
        {
            foreach (var openUSSFile in m_OpenUSSFiles)
                openUSSFile.ResyncBackupToCurrentAsset();

            if (string.IsNullOrEmpty(uxmlOldPath))
                return;

            var fresh = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(uxmlOldPath);
            if (fresh == null)
                return;

            m_VisualTreeAsset = fresh;
            m_VisualTreeAssetRef = fresh;

            if (m_VisualTreeAssetBackup == null)
            {
                m_VisualTreeAssetBackup = fresh.DeepCopy();
                return;
            }

            // Preserve selection markers across the sync so the canvas selection survives the dependent
            // reimport (UUM-141060). Prefer the fresh asset's markers — the backup's index-based snapshot can
            // map onto a stale element order after a sibling's reorder — falling back to the backup only when
            // the reimport stripped them.
            var markers = CaptureSelectionMarkers(fresh);
            if (markers.isEmpty)
                markers = CaptureSelectionMarkers(m_VisualTreeAssetBackup);
            fresh.DeepOverwrite(m_VisualTreeAssetBackup);
            ApplySelectionMarkers(m_VisualTreeAssetBackup, markers);
            ApplySelectionMarkers(fresh, markers);
        }

        bool PostSaveToDiskChecksAndFixes(string newUxmlPath, bool needsFullRefresh)
        {
            var oldVTAReference = visualTreeAsset;
            var oldUxmlPath = uxmlPath;
            var hasNewUxmlPath = !string.IsNullOrEmpty(newUxmlPath) && newUxmlPath != oldUxmlPath;
            var localUxmlPath = !string.IsNullOrEmpty(newUxmlPath) ? newUxmlPath : oldUxmlPath;

            m_VisualTreeAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(localUxmlPath);
            m_VisualTreeAssetRef = m_VisualTreeAsset;
            var newIsDifferentFromOld = m_VisualTreeAsset != oldVTAReference;
            m_ContentHash = m_VisualTreeAsset.contentHash;

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
                m_VisualTreeAsset.SetupReferences();

                // Update hash. Otherwise we end up with the old overwritten contentHash
                var hash = UXMLImporterImpl.GenerateHash(localUxmlPath);
                m_VisualTreeAsset.contentHash = hash.GetHashCode();
                m_ContentHash = m_VisualTreeAsset.contentHash;
            }

            // Sync the backup to the freshly imported asset so a later RestoreAssetsFromBackup
            // doesn't revert ids referenced by ancestor serializedDataOverrides (UUM-141060),
            // capturing selection markers across the sync so the editor-only state survives.
            // Prefer markers already on the live asset over the backup's index-based snapshot; see
            // ResyncBackupToCurrentAsset.
            if (m_VisualTreeAssetBackup != null && m_VisualTreeAsset != null)
            {
                var markers = CaptureSelectionMarkers(m_VisualTreeAsset);
                if (markers.isEmpty)
                    markers = CaptureSelectionMarkers(m_VisualTreeAssetBackup);
                m_VisualTreeAsset.DeepOverwrite(m_VisualTreeAssetBackup);
                ApplySelectionMarkers(m_VisualTreeAssetBackup, markers);
                ApplySelectionMarkers(m_VisualTreeAsset, markers);
            }

            // Reset file settings
            fileSettings.SetRootElementAsset(m_VisualTreeAsset);

            return needsFullRefresh;
        }

        struct SelectionMarkerSnapshot
        {
            public List<int> selectedVeaIndices;
            public bool hasRootMarker;

            public bool isEmpty => (selectedVeaIndices == null || selectedVeaIndices.Count == 0) && !hasRootMarker;
        }

        static SelectionMarkerSnapshot CaptureSelectionMarkers(VisualTreeAsset vta)
        {
            var snapshot = new SelectionMarkerSnapshot
            {
                selectedVeaIndices = new List<int>(),
                hasRootMarker = false,
            };
            if (vta == null)
                return snapshot;

            var idx = 0;
            foreach (var ua in vta.DepthFirstTraversal())
            {
                if (ua.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                {
                    snapshot.hasRootMarker = true;
                    continue;
                }
                if (ua is VisualElementAsset vea)
                {
                    var value = vea.GetAttributeValue(BuilderConstants.SelectedVisualElementAssetAttributeName);
                    if (value == BuilderConstants.SelectedVisualElementAssetAttributeValue)
                        snapshot.selectedVeaIndices.Add(idx);
                }
                idx++;
            }
            return snapshot;
        }

        static void ApplySelectionMarkers(VisualTreeAsset vta, SelectionMarkerSnapshot snapshot)
        {
            if (vta == null)
                return;

            if (snapshot.selectedVeaIndices != null && snapshot.selectedVeaIndices.Count > 0)
            {
                var lookup = new HashSet<int>(snapshot.selectedVeaIndices);
                var idx = 0;
                foreach (var ua in vta.DepthFirstTraversal())
                {
                    if (ua.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                        continue;
                    if (lookup.Contains(idx) && ua is VisualElementAsset vea)
                    {
                        vea.SetAttribute(
                            BuilderConstants.SelectedVisualElementAssetAttributeName,
                            BuilderConstants.SelectedVisualElementAssetAttributeValue);
                    }
                    idx++;
                }
            }

            if (snapshot.hasRootMarker
                && vta.FindElementByType(BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName) == null)
            {
                SelectionUtility.AddToSelection(vta);
            }
        }

        public BuilderDocumentOpenUSS GetUssFileFromSheet(StyleSheet styleSheet)
        {
            if (styleSheet == null)
                return null;

            foreach (var openUSSFile in m_OpenUSSFiles)
            {
                if (openUSSFile?.styleSheet == styleSheet)
                    return openUSSFile;
            }

            // If this is a subdocument, check the parent document for the style sheet.
            return openSubDocumentParent?.GetUssFileFromSheet(styleSheet);
        }

        public void IgnoreExternalChanges(bool ignore)
        {
            m_ExternalReimportInProgress = ignore;
        }
    }
}
