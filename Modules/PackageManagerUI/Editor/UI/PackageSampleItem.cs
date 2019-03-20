// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageSampleItem
    {
        private Sample sample;

        public PackageSampleItem(Sample sample)
        {
            this.sample = sample;
            NameLabel.text = sample.displayName;
            SizeLabel.text = sample.Size;
            RefreshImportStatus();
            ImportButton.clickable.clicked += OnImportButtonClicked;
        }

        private void OnImportButtonClicked()
        {
            var previousImports = sample.PreviousImports;
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
                if (sample.isImported)
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

            if (sample.Import(Sample.ImportOptions.OverridePreviousImports))
            {
                RefreshImportStatus();
                if (sample.isImported)
                {
                    // Highlight import path
                    var importRelativePath = sample.importPath.Replace(Application.dataPath, "Assets");
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(importRelativePath, typeof(UnityEngine.Object));
                    UnityEditor.Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }
        }

        private void RefreshImportStatus()
        {
            if (sample.isImported)
            {
                ImportStatus.AddToClassList("imported");
                ImportButton.text = "Import again";
            }
            else if (sample.PreviousImports.Count != 0)
            {
                ImportStatus.AddToClassList("imported");
                ImportButton.text = "Update";
            }
            else
            {
                ImportStatus.RemoveFromClassList("imported");
                ImportButton.text = "Import in project";
            }
        }

        private Label _importStatus;
        internal Label ImportStatus { get { return _importStatus ?? (_importStatus = new Label()); } }
        private Label _nameLabel;
        internal Label NameLabel { get { return _nameLabel ?? (_nameLabel = new Label()); } }
        private Label _sizeLabel;
        internal Label SizeLabel { get { return _sizeLabel ?? (_sizeLabel = new Label()); } }
        private Button _importButton;
        internal Button ImportButton { get { return _importButton ?? (_importButton = new Button()); } }
    }
}
