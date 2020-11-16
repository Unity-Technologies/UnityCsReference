using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using System.IO;

namespace Unity.UI.Builder
{
    [Serializable]
    class BuilderDocumentOpenUSS
    {
        [SerializeField]
        StyleSheet m_StyleSheet;

        // Used to restore in-memory StyleSheet asset if closing without saving.
        [SerializeField]
        StyleSheet m_Backup;

        // This is for automatic style path fixing after a uss file name change.
        [SerializeField]
        string m_OldPath;

        // Used during saving to reload USS asset from disk after AssetDatabase.Refresh().
        string m_NewPath;

        public StyleSheet styleSheet
        {
            get => m_StyleSheet;
            set => m_StyleSheet = value;
        }

        public string assetPath => AssetDatabase.GetAssetPath(m_StyleSheet);

        public string oldPath => m_OldPath;

        public void Set(StyleSheet styleSheet, string ussPath)
        {
            if (string.IsNullOrEmpty(ussPath))
                ussPath = AssetDatabase.GetAssetPath(styleSheet);

            m_StyleSheet = styleSheet;
            m_Backup = styleSheet.DeepCopy();
            m_OldPath = ussPath;
            m_NewPath = null;
        }

        public void Clear()
        {
            RestoreFromBackup();

            ClearBackup();
            m_StyleSheet = null;

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

        public void SaveToDisk(VisualTreeAsset visualTreeAsset)
        {
            var newUSSPath = assetPath;

            // There should not be a way to have an unsaved USS. The newUSSPath should always be non-empty.

            m_NewPath = newUSSPath;
            visualTreeAsset.ReplaceStyleSheetPaths(m_OldPath, newUSSPath);

            // Need to save a backup before the AssetDatabase.Refresh().
            if (m_Backup == null)
                m_Backup = m_StyleSheet.DeepCopy();
            else
                m_StyleSheet.DeepOverwrite(m_Backup);

            WriteUSSToFile(newUSSPath);
        }

        public bool PostSaveToDiskChecksAndFixes()
        {
            m_StyleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(m_NewPath);
            bool needsFullRefresh = m_StyleSheet != m_Backup;

            // Get back selection markers from backup:
            if (needsFullRefresh)
                m_Backup.DeepOverwrite(m_StyleSheet);

            m_NewPath = null;
            return needsFullRefresh;
        }

        public bool CheckPostProcessAssetIfFileChanged(string assetPath)
        {
            if (assetPath != m_OldPath)
                return false;

            m_StyleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(assetPath);
            return true;
        }

        public void RestoreFromBackup()
        {
            if (m_Backup == null || m_StyleSheet == null)
                return;

            m_Backup.DeepOverwrite(m_StyleSheet);
        }

        public void ClearBackup()
        {
            if (m_Backup == null)
                return;

            m_Backup.Destroy();
            m_Backup = null;
        }

        public void ClearUndo()
        {
            m_StyleSheet.ClearUndo();
        }

        void WriteUSSToFile(string ussPath)
        {
            var ussText = m_StyleSheet.GenerateUSS();

            // This will only be null (not empty) if the UXML is invalid in some way.
            if (ussText == null)
                return;

            // Make sure the folders exist.
            var ussFolder = Path.GetDirectoryName(ussPath);
            if (!Directory.Exists(ussFolder))
                Directory.CreateDirectory(ussFolder);

            File.WriteAllText(ussPath, ussText);
        }
    }
}
