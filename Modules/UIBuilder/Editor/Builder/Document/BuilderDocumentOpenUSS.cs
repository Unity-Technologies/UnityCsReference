// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;

namespace Unity.UI.Builder
{
    [Serializable]
    class BuilderDocumentOpenUSS
    {
        [SerializeField]
        StyleSheet m_StyleSheet;

        [SerializeField]
        private int m_ContentHash;

        [SerializeField]
        private string m_UssPreview;

        // Used to restore in-memory StyleSheet asset if closing without saving.
        StyleSheet m_BackupStyleSheet;

        // This is for automatic style path fixing after a uss file name change.
        [SerializeField]
        string m_OldPath;

        // Used during saving to reload USS asset from disk after AssetDatabase.Refresh().
        string m_NewPath;

        public StyleSheet styleSheet
        {
            get => m_StyleSheet;
            set
            {
                m_StyleSheet = value;
                m_ContentHash = m_StyleSheet.contentHash;
            }
        }

        public StyleSheet backupStyleSheet
        {
            get => m_BackupStyleSheet;
        }

        public int contentHash => m_ContentHash;

        public string assetPath => AssetDatabase.GetAssetPath(m_StyleSheet);

        public string oldPath => m_OldPath;

        public string ussPreview => m_UssPreview;

        public void Set(StyleSheet styleSheet, string ussPath, bool updateBackup = true)
        {
            if (string.IsNullOrEmpty(ussPath))
                ussPath = AssetDatabase.GetAssetPath(styleSheet);

            this.styleSheet = styleSheet;
            if (updateBackup) {
                m_BackupStyleSheet = styleSheet.DeepCopy();
            }
            m_OldPath = ussPath;
            m_NewPath = null;
            m_UssPreview = m_StyleSheet.GenerateUSS();
        }

        public void Clear()
        {
            RestoreFromBackup();

            ClearBackup();
            m_StyleSheet = null;
            m_UssPreview = string.Empty;
            m_ContentHash = 0;

            // Note: Leaving here as a warning to NOT do this in the
            // future. The problem is that the lines below will force
            // a re-import of the current UXML as well as this USS. This
            // causes the UXML to re-import still referencing the USS being
            // removed. It also causes the Library to start pointing at a
            // deleted asset in memory (because of the force-reimport).
            // I replaced this with a `RestoreFromBackup()` above for now.
            //
            // Restore from file system in case of unsaved changes.
            //if (!string.IsNullOrEmpty(path))
            //AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        public void FixRuleReferences()
        {
            m_StyleSheet.FixRuleReferences();
        }

        public void GeneratePreview()
        {
            m_UssPreview = m_StyleSheet.GenerateUSS();
        }

        public bool SaveToDisk(VisualTreeAsset visualTreeAsset)
        {
            var newUSSPath = assetPath;

            // There should not be a way to have an unsaved USS. The newUSSPath should always be non-empty.

            m_NewPath = newUSSPath;
            visualTreeAsset.ReplaceStyleSheetPaths(m_OldPath, newUSSPath);

            if (ussPreview == null)
                return false;

            bool shouldSave = m_OldPath != newUSSPath;
            if (!shouldSave && m_BackupStyleSheet)
            {
                var backUss = m_BackupStyleSheet.GenerateUSS();
                shouldSave = backUss != ussPreview;
            }

            if (!shouldSave)
            {
                return false;
            }

            // Need to save a backup before the AssetDatabase.Refresh().
            if (m_BackupStyleSheet == null)
                m_BackupStyleSheet = m_StyleSheet.DeepCopy();
            else
                m_StyleSheet.DeepOverwrite(m_BackupStyleSheet);

            return BuilderAssetUtilities.WriteTextFileToDisk(newUSSPath, ussPreview);
        }

        public bool PostSaveToDiskChecksAndFixes()
        {
            m_StyleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(m_NewPath);
            var needsFullRefresh = m_StyleSheet != m_BackupStyleSheet;

            m_ContentHash = m_StyleSheet.contentHash;

            // Get back selection markers from backup:
            if (needsFullRefresh)
            {
                m_BackupStyleSheet.DeepOverwrite(m_StyleSheet);
            }

            m_NewPath = null;
            return needsFullRefresh;
        }

        public bool CheckPostProcessAssetIfFileChanged(string assetPath)
        {
            if (assetPath != m_OldPath)
                return false;

            styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(assetPath);
            return true;
        }

        public void RestoreUnsavedChanges()
        {
            ClearUndo();
            ClearBackup();

            var restoredStyleSheet = StyleSheetUtilities.CreateInstance();
            var ussImporter = new BuilderStyleSheetImporter();
            ussImporter.Import(restoredStyleSheet, ussPreview);
            m_BackupStyleSheet = styleSheet.DeepCopy();
            restoredStyleSheet.DeepOverwrite(styleSheet);
        }

        public void RestoreFromBackup()
        {
            if (m_BackupStyleSheet == null || m_StyleSheet == null)
                return;

            m_BackupStyleSheet.DeepOverwrite(m_StyleSheet);
            m_ContentHash = m_StyleSheet.contentHash;
        }

        public void ClearBackup()
        {
            if (m_BackupStyleSheet == null)
                return;

            m_BackupStyleSheet.Destroy();
            m_BackupStyleSheet = null;
        }

        public void ClearUndo()
        {
            m_StyleSheet.ClearUndo();
        }

        public int GetComplexSelectorsCount()
        {
            if (m_StyleSheet == null || m_StyleSheet.complexSelectors == null)
                return 0;

            var nbComplexSelectorsCount = 0;
            for (var complexSelectorIndex = 0;
                 complexSelectorIndex < m_StyleSheet.complexSelectors.Length;
                 ++complexSelectorIndex)
            {
                var complexSelector = m_StyleSheet.complexSelectors[complexSelectorIndex];

                // Omit special selection rule.
                if (complexSelector.selectors.Length > 0 &&
                    complexSelector.selectors[0].parts.Length > 0 &&
                    (complexSelector.selectors[0].parts[0].value == BuilderConstants.SelectedStyleSheetSelectorName
                     || complexSelector.selectors[0].parts[0].value
                         .StartsWith(BuilderConstants.StyleSelectorElementName)))
                    continue;

                nbComplexSelectorsCount++;
            }

            return nbComplexSelectorsCount;
        }
    }
}
