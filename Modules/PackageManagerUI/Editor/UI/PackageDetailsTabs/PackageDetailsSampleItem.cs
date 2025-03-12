// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsSampleItem
    {
        private readonly IPackageVersion m_Version;
        private Sample m_Sample;

        private readonly SelectionProxy m_Selection;
        private readonly AssetDatabaseProxy m_AssetDatabase;
        private readonly ApplicationProxy m_ApplicationProxy;
        private readonly IOProxy m_IOProxy;

        public PackageDetailsSampleItem(IPackageVersion version, Sample sample, SelectionProxy selection, AssetDatabaseProxy assetDatabase, ApplicationProxy application, IOProxy iOProxy)
        {
            m_Selection = selection;
            m_AssetDatabase = assetDatabase;
            m_ApplicationProxy = application;
            m_IOProxy = iOProxy;

            m_Version = version;
            m_Sample = sample;
            nameLabel.text = sample.displayName;
            nameLabel.tooltip = sample.displayName; // add tooltip for when the label text is cut off
            sizeLabel.text = sample.size;
            descriptionLabel.SetValueWithoutNotify(sample.description);
            descriptionLabel.multiline = true;
            RefreshImportStatus();
            importButton.clickable.clicked += OnImportButtonClicked;
        }

        private void OnImportButtonClicked()
        {
            var previousImports = m_Sample.previousImports;
            var previousImportPaths = previousImports.Aggregate(string.Empty,
                (current, next) => current + next.Replace(@"\", "/").Replace(Application.dataPath, "Assets") + "\n");

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
                !m_ApplicationProxy.DisplayDialog("importPackageSample",
                    L10n.Tr("Importing package sample"),
                    warningMessage + L10n.Tr(" Are you sure you want to continue?"),
                    L10n.Tr("Yes"), L10n.Tr("No")))
            {
                return;
            }

            if (previousImports.Count < 1)
                PackageManagerWindowAnalytics.SendEvent("importSample", m_Version.uniqueId);
            else
                PackageManagerWindowAnalytics.SendEvent("reimportSample", m_Version.uniqueId);

            if (m_Sample.Import(Sample.ImportOptions.OverridePreviousImports))
            {
                RefreshImportStatus();
                if (m_Sample.isImported)
                {
                    // Highlight import path
                    var currentPath = m_IOProxy.CurrentDirectory;
                    var importRelativePath = m_Sample.importPath.Replace(currentPath + Path.DirectorySeparatorChar, "");
                    m_ApplicationProxy.PingAtPath(importRelativePath);
                }
            }
        }

        private void RefreshImportStatus()
        {
            if (m_Sample.isImported)
            {
                importStatus.AddToClassList("imported");
                importButton.text = L10n.Tr("Reimport");
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
        internal Label importStatus { get { return m_ImportStatus ??= new Label { classList = { "importStatus" } }; } }
        private Label m_NameLabel;
        internal Label nameLabel { get { return m_NameLabel ??= new Label { classList = { "nameLabel" } }; } }
        private Label m_SizeLabel;
        internal Label sizeLabel { get { return m_SizeLabel ??= new Label { classList = { "sizeLabel" } }; } }
        private SelectableLabel m_DescriptionLabel;
        internal SelectableLabel descriptionLabel { get { return m_DescriptionLabel ??= new SelectableLabel { classList = { "descriptionLabel" } }; } }
        private Button m_ImportButton;
        internal Button importButton { get { return m_ImportButton ??= new Button { classList = { "importButton" } }; } }
    }
}
