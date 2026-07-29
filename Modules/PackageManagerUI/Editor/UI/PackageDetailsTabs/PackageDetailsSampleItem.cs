// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsSampleItem
    {
        private readonly IPackageVersion m_Version;
        private Sample m_Sample;

        private readonly ISelectionProxy m_Selection;
        private readonly IAssetDatabaseProxy m_AssetDatabase;
        private readonly IApplicationProxy m_Application;
        private readonly IIOProxy m_IOProxy;

        public PackageDetailsSampleItem(IPackageVersion version, Sample sample, ISelectionProxy selection, IAssetDatabaseProxy assetDatabase, IApplicationProxy application, IIOProxy iOProxy)
        {
            m_Selection = selection;
            m_AssetDatabase = assetDatabase;
            m_Application = application;
            m_IOProxy = iOProxy;

            m_Version = version;
            m_Sample = sample;
            nameLabel.text = sample.displayName;
            nameLabel.tooltip = sample.displayName; // add tooltip for when the label text is cut off
            sizeLabel.text = sample.size;
            descriptionLabel.text = sample.description;
            RefreshImportButton();
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
                !m_Application.DisplayDialog("importPackageSample",
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
                RefreshImportButton();
                if (m_Sample.isImported)
                {
                    // Highlight import path
                    var importRelativePath = m_IOProxy.GetProjectRelativePath(m_Sample.importPath);
                    var obj = m_AssetDatabase.LoadMainAssetAtPath(importRelativePath);
                    m_Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }
        }

        private void RefreshImportButton()
        {
            RefreshButtonState();
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

        private void RefreshButtonState()
        {
            var error = m_Version?.errors?.FirstOrDefault(e => !e.HasAttribute(UIError.Attribute.Clearable | UIError.Attribute.HiddenFromUI));
            if (error is { errorCode: UIErrorCode.UpmError_InvalidSourcePath })
            {
                importButton.SetEnabled(false);
                importButton.tooltip = L10n.Tr("The package this sample belongs to is stored in an invalid location.");
            }
            else if (m_Version?.hasEntitlementsError == true)
            {
                importButton.SetEnabled(false);
                importButton.tooltip = L10n.Tr("You need to sign in with a licensed account to perform this action.");
            }
            else if (m_Version?.errors?.Any(i => i.errorCode == UIErrorCode.UpmError_PackageNotLoaded) == true)
            {
                importButton.SetEnabled(false);
                importButton.tooltip = L10n.Tr("The package this sample belongs to isn't loaded in your project.");
            }
            else
            {
                importButton.SetEnabled(true);
                importButton.tooltip = string.Empty;
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
