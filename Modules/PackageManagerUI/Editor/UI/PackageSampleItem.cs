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

        private SelectionProxy m_Selection;
        private AssetDatabaseProxy m_AssetDatabase;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Selection = container.Resolve<SelectionProxy>();
            m_AssetDatabase = container.Resolve<AssetDatabaseProxy>();
        }

        public PackageSampleItem(IPackageVersion version, Sample sample)
        {
            ResolveDependencies();

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
                warningMessage = L10n.Tr("Different versions of the sample are already imported at") + "\n\n"
                    + previousImportPaths + "\n" + L10n.Tr("They will be deleted when you update.");
            }
            else if (previousImports.Count == 1)
            {
                if (m_Sample.isImported)
                {
                    warningMessage = L10n.Tr("The sample is already imported at") + "\n\n"
                        + previousImportPaths + "\n" + L10n.Tr("Importing again will override all changes you have made to it.");
                }
                else
                {
                    warningMessage = L10n.Tr("A different version of the sample is already imported at") + "\n\n"
                        + previousImportPaths + "\n" + L10n.Tr("It will be deleted when you update.");
                }
            }

            if (!string.IsNullOrEmpty(warningMessage) &&
                EditorUtility.DisplayDialog(L10n.Tr("Unity Package Manager"),
                    warningMessage + L10n.Tr(" Are you sure you want to continue?"),
                    L10n.Tr("No"), L10n.Tr("Yes")))
            {
                return;
            }

            PackageManagerWindowAnalytics.SendEvent("importSample", m_Version.uniqueId);

            if (m_Sample.Import(Sample.ImportOptions.OverridePreviousImports))
            {
                RefreshImportStatus();
                if (m_Sample.isImported)
                {
                    // Highlight import path
                    var importRelativePath = m_Sample.importPath.Replace(Application.dataPath, "Assets");
                    Object obj = m_AssetDatabase.LoadMainAssetAtPath(importRelativePath);
                    m_Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }
        }

        private void RefreshImportStatus()
        {
            if (m_Sample.isImported)
            {
                importStatus.AddToClassList("imported");
                importButton.text = L10n.Tr("Import again");
            }
            else if (m_Sample.previousImports.Count != 0)
            {
                importStatus.AddToClassList("imported");
                importButton.text = L10n.Tr("Update");
            }
            else
            {
                importStatus.RemoveFromClassList("imported");
                importButton.text = L10n.Tr("Import");
            }
        }

        private Label m_ImportStatus;
        internal Label importStatus { get { return m_ImportStatus ?? (m_ImportStatus = new Label() { classList = { "importStatus" } }); } }
        private Label m_NameLabel;
        internal Label nameLabel { get { return m_NameLabel ?? (m_NameLabel = new Label() { classList = { "nameLabel" } }); } }
        private Label m_SizeLabel;
        internal Label sizeLabel { get { return m_SizeLabel ?? (m_SizeLabel = new Label() { classList = { "sizeLabel" } }); } }
        private Button m_ImportButton;
        internal Button importButton { get { return m_ImportButton ?? (m_ImportButton = new Button() { classList = { "importButton" } }); } }
    }
}
