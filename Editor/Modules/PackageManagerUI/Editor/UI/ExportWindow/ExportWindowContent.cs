// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ExportWindowContent : ModalContent
    {
        private const int k_FixedWindowWidth = 360;
        private const int k_ExportWindowHeight = 180;
        private const int k_SignInWindowHeight = 100;
        private const int k_ClosedNetworkHelpBoxHeight = 55;
        private static readonly string k_ClosedNetworkMessage = L10n.Tr("This package won't be signed because Closed network environment is enabled in Project Settings.", null);
        private OrganizationInfo[] m_OrganizationInfos;

        private readonly IPackageVersion m_VersionToExport;
        private string m_ExportPath;

        private readonly IApplicationProxy m_ApplicationProxy;
        private readonly IUnityConnectProxy m_UnityConnectProxy;
        private readonly IUpmClient m_UpmClient;
        private readonly IIOProxy m_IOProxy;
        private readonly IProjectSettingsProxy m_SettingsProxy;

        public ExportWindowContent(IApplicationProxy applicationProxy, IIOProxy ioProxy, IResourceLoader resourceLoader, IUnityConnectProxy unityConnectProxy, IUpmClient upmClient, IProjectSettingsProxy settingsProxy, IPackageVersion version)
        {
            m_ApplicationProxy = applicationProxy;
            m_IOProxy = ioProxy;
            m_UnityConnectProxy = unityConnectProxy;
            m_UpmClient = upmClient;
            m_SettingsProxy = settingsProxy;

            m_VersionToExport = version;
            windowTitle = L10n.Tr("Export Package", null);

            var root = resourceLoader.GetTemplate("ExportWindow.uxml");
            cache = new VisualElementCache(root);
            Add(root);
            styleSheets.Add(resourceLoader.packageManagerCommonStyleSheet);
            styleSheets.Add(resourceLoader.exportWindowStyleSheet);
            closedNetworkHelpBox.text = k_ClosedNetworkMessage;
            cancelButton.clicked += Close;
            signInOkButton.clicked += Close;
            exportButton.clicked += OnExportButtonClicked;
            packageOrganizationDropdown.RegisterValueChangedCallback( _ => OnOrganizationDropdownChange());
        }

        public void SetOrganizationInfos(OrganizationInfo[] organizationInfos)
        {
            m_OrganizationInfos = organizationInfos;
        }

        public override void OnBeforeShowModal()
        {
            m_UpmClient.onPackOperation += OnPackOperation;

            Refresh();
        }

        public override void OnModalClosed()
        {
            m_UpmClient.onPackOperation -= OnPackOperation;
        }

        private void OnPackOperation(IOperation operation)
        {
            operation.onOperationSuccess += OnPackSuccess;
            operation.onOperationError += OnPackError;
        }

        private void OnPackSuccess(IOperation operation)
        {
            var tarballPath = IOUtils.PathsCombine(m_ExportPath, GetPackageExportFileName());
            Debug.Log(L10n.Tr("[Package Manager Window] Package successfully exported to: ", null) + tarballPath);
            m_ApplicationProxy.RevealInFinder(tarballPath);
        }

        private void OnPackError(IOperation operation, UIError error)
        {
            Debug.LogError(L10n.Tr("[Package Manager Window] Package export failed: ", null) + error.message);
        }

        private void Refresh()
        {
            // Signing in is only needed to sign the package, which is skipped in a closed network environment with a bypass entitlement
            var isSignatureDisabled = m_SettingsProxy.isSignatureDisabled;
            var showSignInContainer = !isSignatureDisabled && m_UnityConnectProxy.isUserInfoReady && !m_UnityConnectProxy.isUserLoggedIn;
            UIUtils.SetElementDisplay(signInContainer, showSignInContainer);
            UIUtils.SetElementDisplay(exportContainer, !showSignInContainer);
            if (showSignInContainer)
            {
                SetWindowSize(k_FixedWindowWidth, k_SignInWindowHeight);
                return;
            }

            UIUtils.SetElementDisplay(closedNetworkHelpBox, isSignatureDisabled);
            SetWindowSize(k_FixedWindowWidth, isSignatureDisabled ? k_ExportWindowHeight + k_ClosedNetworkHelpBoxHeight : k_ExportWindowHeight);

            packageDisplayNameLabel.text = m_VersionToExport.displayName;
            packageVersionLabel.text = m_VersionToExport.version.ToString();
            packageTechnicalNameLabel.text = m_VersionToExport.name;

            var choices = new List<string>();
            var value = L10n.Tr("Select Organization", null);

            if (isSignatureDisabled)
            {
                // The dropdown will be disabled later, we use the default value and choices.
            }
            else if (m_OrganizationInfos.Length == 0)
            {
                value = L10n.Tr("No Organizations Found", null);
            }
            else
            {
                foreach (var orgInfo in m_OrganizationInfos)
                {
                    choices.Add(orgInfo.name);
                }
                if (m_OrganizationInfos.Length == 1)
                {
                    packageOrganizationDropdown.index = 0;
                    value = choices[0];
                }
            }

            packageOrganizationDropdown.choices = choices;
            packageOrganizationDropdown.value = value;
            organizationLabel.SetEnabled(!isSignatureDisabled);
            packageOrganizationDropdown.SetEnabled(m_OrganizationInfos.Length > 0 && !isSignatureDisabled);
            exportButton.SetEnabled(isSignatureDisabled || (m_OrganizationInfos.Length > 0 && packageOrganizationDropdown.index > -1));
        }

        private void SetWindowSize(int width, int height)
        {
            if (container != null)
            {
                var fixedSize = new Vector2(width, height);
                container.minSize = fixedSize;
                container.maxSize = fixedSize;
            }
        }

        private void OnOrganizationDropdownChange()
        {
            exportButton.SetEnabled(packageOrganizationDropdown.index != -1);
        }

        private void OnExportButtonClicked()
        {
            m_ExportPath = m_ApplicationProxy.OpenFolderPanel(L10n.Tr("Export Package", null), IOUtils.GetParentDirectory(m_ApplicationProxy.dataPath));
            if (string.IsNullOrEmpty(m_ExportPath))
                return;

            if (m_IOProxy.GetFileAttributes(m_ExportPath).HasFlag(FileAttributes.ReadOnly))
            {
                m_ApplicationProxy.DisplayAlertDialog("export-package-read-only-error",L10n.Tr("Read-only path", null),
                    L10n.Tr("The selected path is read-only. Please select a different location.", null),
                    L10n.Tr("OK", null));
                return;
            }

            if (m_IOProxy.FileExists(IOUtils.PathsCombine(m_ExportPath, GetPackageExportFileName())))
            {
                if (!m_ApplicationProxy.DisplayDialog("package-export-overwrite",
                        L10n.Tr("Overwrite package?", null),
                        L10n.Tr("A package with the same name already exists at this location. Do you want to overwrite the existing package?", null),
                        L10n.Tr("Overwrite", null), L10n.Tr("Cancel", null)))
                    return;
            }

            var orgKey = m_SettingsProxy.isSignatureDisabled ? null : m_OrganizationInfos[packageOrganizationDropdown.index].foreignKey;

            m_UpmClient.Pack(m_VersionToExport.name, m_VersionToExport.localPath, m_ExportPath, orgKey);
            Close();
        }

        private void Close()
        {
            container?.Close();
        }

        private string GetPackageExportFileName() => $"{m_VersionToExport.name}-{m_VersionToExport.version.ToString()}.tgz";

        private VisualElementCache cache { get; set; }
        private VisualElement signInContainer => cache.Get<VisualElement>("signInContainer");
        private Button signInOkButton => cache.Get<Button>("signInOkButton");
        private VisualElement exportContainer => cache.Get<VisualElement>("exportContainer");
        private Label packageDisplayNameLabel => cache.Get<Label>("packageDisplayNameLabel");
        private Label packageVersionLabel => cache.Get<Label>("packageVersionLabel");
        private ExtendedHelpBox closedNetworkHelpBox => cache.Get<ExtendedHelpBox>("closedNetworkHelpBox");
        private Label organizationLabel => cache.Get<Label>("organizationLabel");
        private DropdownField packageOrganizationDropdown => cache.Get<DropdownField>("packageOrganizationDropdown");
        private Label packageTechnicalNameLabel => cache.Get<Label>("packageTechnicalNameLabel");
        private Button cancelButton => cache.Get<Button>("cancelButton");
        private Button exportButton => cache.Get<Button>("exportButton");
    }
}
