// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageSampleItem
    {
        private IPackageVersion m_Version;
        private Sample m_Sample;

        public PackageSampleItem(IPackageVersion version, Sample sample)
        {
            m_Version = version;
            m_Sample = sample;
            nameLabel.text = sample.displayName;
            nameLabel.tooltip = sample.displayName; // add tooltip for when the label text is cut off
            sizeLabel.text = sample.size;
            RefreshImportStatus();
            importButton.clickable.clicked += OnImportButtonClicked;
        }

        private void OnImportButtonClicked()
        {
            var previousImports = m_Sample.previousImports;
            var previousImportPaths = string.Empty;
            foreach (var v in previousImports)
                previousImportPaths += v.Replace(Application.dataPath, "Assets") + "\n";

            var warningMessage = string.Empty;
            if (previousImports.Count > 1)
            {
                warningMessage = "Different versions of the sample are already imported at\n\n"
                    + previousImportPaths + "\nThey will be deleted when you update.";
            }
            else if (previousImports.Count == 1)
            {
                if (m_Sample.isImported)
                {
                    warningMessage = "The sample is already imported at\n\n" + previousImportPaths
                        + "\nImporting again will override all changes you have made to it.";
                }
                else
                {
                    warningMessage = "A different version of the sample is already imported at\n\n"
                        + previousImportPaths + "\nIt will be deleted when you update.";
                }
            }

            if (!string.IsNullOrEmpty(warningMessage) &&
                EditorUtility.DisplayDialog("Unity Package Manager", warningMessage + " Are you sure you want to continue?", "No", "Yes"))
                return;

            PackageManagerWindowAnalytics.SendEvent("importSample", m_Version.uniqueId);

            if (m_Sample.Import(Sample.ImportOptions.OverridePreviousImports))
            {
                RefreshImportStatus();
                if (m_Sample.isImported)
                {
                    // Highlight import path
                    var importRelativePath = m_Sample.importPath.Replace(Application.dataPath, "Assets");
                    Object obj = AssetDatabase.LoadAssetAtPath(importRelativePath, typeof(Object));
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }
        }

        private void RefreshImportStatus()
        {
            if (m_Sample.isImported)
            {
                importStatus.AddToClassList("imported");
                importButton.text = "Import again";
            }
            else if (m_Sample.previousImports.Count != 0)
            {
                importStatus.AddToClassList("imported");
                importButton.text = "Update";
            }
            else
            {
                importStatus.RemoveFromClassList("imported");
                importButton.text = "Import in project";
            }
        }

        private Label m_ImportStatus;
        internal Label importStatus { get { return m_ImportStatus ?? (m_ImportStatus = new Label()); } }
        private Label m_NameLabel;
        internal Label nameLabel { get { return m_NameLabel ?? (m_NameLabel = new Label()); } }
        private Label m_SizeLabel;
        internal Label sizeLabel { get { return m_SizeLabel ?? (m_SizeLabel = new Label()); } }
        private Button m_ImportButton;
        internal Button importButton { get { return m_ImportButton ?? (m_ImportButton = new Button()); } }
    }
}
