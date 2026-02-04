// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsSampleItem
    {
        private readonly IPackageVersion m_Version;
        private Sample m_Sample;

        private readonly IApplicationProxy m_Application;
        private readonly IIOProxy m_IOProxy;

        public PackageDetailsSampleItem(IPackageVersion version, Sample sample, IApplicationProxy application, IIOProxy iOProxy)
        {
            m_Application = application;
            m_IOProxy = iOProxy;

            m_Version = version;
            m_Sample = sample;
            nameLabel.text = sample.displayName;
            nameLabel.tooltip = sample.displayName; // add tooltip for when the label text is cut off
            sizeLabel.text = sample.size;
            descriptionLabel.text = sample.description;
            RefreshActionButtons();
            importButton.clickable.clicked += OnImportButtonClicked;
            locateButton.clickable.clicked += OnLocateButtonClicked;
        }

        private void OnLocateButtonClicked()
        {
            PingSampleInProjectBrowser();
            PackageManagerWindowAnalytics.SendEvent("locateSample", m_Version.uniqueId);
        }

        private void OnImportButtonClicked()
        {
            var previousImports = m_Sample.previousImports;
            if (previousImports.Count > 0)
            {
                var previousImportPathsStringBuilder = new StringBuilder();
                foreach (var path in previousImports)
                {
                    previousImportPathsStringBuilder.Append(path.Replace(@"\", "/").Replace(Application.dataPath, "Assets"));
                    previousImportPathsStringBuilder.Append('\n');
                }

                string warningMessage;
                if (previousImports.Count > 1)
                {
                    warningMessage = L10n.Tr("Different versions of the sample are already imported at") + "\n\n"
                        + previousImportPathsStringBuilder + "\n" + L10n.Tr("They will be deleted when you update.");
                }
                else
                {
                    if (m_Sample.isImported)
                    {
                        warningMessage = L10n.Tr("The sample is already imported at") + "\n\n"
                            + previousImportPathsStringBuilder + "\n" + L10n.Tr("Importing again will override all changes you have made to it.");
                    }
                    else
                    {
                        warningMessage = L10n.Tr("A different version of the sample is already imported at") + "\n\n"
                            + previousImportPathsStringBuilder + "\n" + L10n.Tr("It will be deleted when you update.");
                    }
                }

                if (!m_Application.DisplayDialog("importPackageSample",
                        L10n.Tr("Importing package sample"),
                        warningMessage + L10n.Tr(" Are you sure you want to continue?"),
                        L10n.Tr("Yes"), L10n.Tr("No")))
                    return;
            }

            var eventName = previousImports.Count == 0 ? "importSample" : "reimportSample";
            PackageManagerWindowAnalytics.SendEvent(eventName, m_Version.uniqueId);

            if (m_Sample.Import(Sample.ImportOptions.OverridePreviousImports))
            {
                RefreshActionButtons();
                PingSampleInProjectBrowser();
            }
        }

        private void RefreshActionButtons()
        {
            UIUtils.SetElementDisplay(locateButton, true);
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
                UIUtils.SetElementDisplay(locateButton, false);
                importStatus.RemoveFromClassList("imported");
                importButton.text = L10n.Tr("Import");
            }
        }

        private void PingSampleInProjectBrowser()
        {
            if (m_Application.PingObjectInProjectBrowser(GetRelativePath(m_Sample.importPath)))
                return;
            if (m_Sample.previousImports?.Count > 0)
                m_Application.PingObjectInProjectBrowser(GetRelativePath(m_Sample.previousImports[^1]));
        }

        private string GetRelativePath(string path)
        {
            return path?.Replace(m_IOProxy.CurrentDirectory + Path.DirectorySeparatorChar, "");
        }

        private Label m_ImportStatus;
        public Label importStatus => m_ImportStatus ??= new Label { classList = { "importStatus" } };
        private Label m_NameLabel;
        public Label nameLabel => m_NameLabel ??= new Label { classList = { "nameLabel" } };
        private Label m_SizeLabel;
        public Label sizeLabel => m_SizeLabel ??= new Label { classList = { "sizeLabel" } };
        private SelectableLabel m_DescriptionLabel;
        public SelectableLabel descriptionLabel => m_DescriptionLabel ??= new SelectableLabel { classList = { "descriptionLabel" } };
        private Button m_ImportButton;
        public Button importButton => m_ImportButton ??= new Button { classList = { "actionButton" } };
        private Button m_LocateButton;
        public Button locateButton => m_LocateButton ??= new Button { text = L10n.Tr("Locate"), classList = { "actionButton" } };
    }
}
