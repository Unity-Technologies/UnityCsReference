// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class AddPackageByNameDropdown : DropdownContent
    {
        internal static readonly string k_NonCompliantDialogTitle = L10n.Tr("Restricted Package");

        private static readonly Vector2 k_DefaultWindowSize = new(320, 72);
        private static readonly Vector2 k_WindowSizeWithError = new(320, 114);
        public override Vector2 windowSize => string.IsNullOrEmpty(errorInfoBox.text) ? k_DefaultWindowSize : k_WindowSizeWithError;

        private IResourceLoader m_ResourceLoader;
        private IUpmClient m_UpmClient;
        private IPackageDatabase m_PackageDatabase;
        private IPageManager m_PageManager;
        private IPackageOperationDispatcher m_OperationDispatcher;
        private ICustomDisplayDialog m_CustomDisplayDialog;

        // We save the initial values and only set the field values when `OnDropdownShown` is called because
        // if we set it too early before the VisualElement is visible, the placeholder text will not show up correctly.
        public string packageNameInitialValue { get; set; }
        public string packageVersionInitialValue { get; set; }

        private void ResolveDependencies(IResourceLoader resourceLoader, IUpmClient upmClient, IPackageDatabase packageDatabase, IPageManager packageManager, IPackageOperationDispatcher packageOperationDispatcher, ICustomDisplayDialog displayDialogCustom)
        {
            m_ResourceLoader = resourceLoader;
            m_UpmClient = upmClient;
            m_PackageDatabase = packageDatabase;
            m_PageManager = packageManager;
            m_OperationDispatcher = packageOperationDispatcher;
            m_CustomDisplayDialog = displayDialogCustom;
        }

        public AddPackageByNameDropdown(IResourceLoader resourceLoader, IUpmClient upmClient, IPackageDatabase packageDatabase, IPageManager packageManager, IPackageOperationDispatcher packageOperationDispatcher, ICustomDisplayDialog displayDialogCustom)
        {
            ResolveDependencies(resourceLoader, upmClient, packageDatabase, packageManager, packageOperationDispatcher, displayDialogCustom);

            styleSheets.Add(m_ResourceLoader.inputDropdownStyleSheet);

            var root = m_ResourceLoader.GetTemplate("AddPackageByNameDropdown.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            packageNameField.textEdition.placeholder = L10n.Tr("Name");
            packageVersionField.textEdition.placeholder = L10n.Tr("Version (optional)");

            submitButton.clickable.clicked += SubmitClicked;
        }

        public override void OnDropdownShown()
        {
            packageNameField.RegisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            packageNameField.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);
            packageVersionField.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);

            inputForm.SetEnabled(true);
            packageNameField.value = packageNameInitialValue ?? string.Empty;
            packageVersionField.value = packageVersionInitialValue ?? string.Empty;
            if (string.IsNullOrEmpty(errorInfoBox.text) || packageNameField.ClassListContains("error"))
                packageNameField.Focus();
            else
                packageVersionField.Focus();
            submitButton.SetEnabled(!string.IsNullOrWhiteSpace(packageNameField.value));
        }

        public override void OnDropdownClosed()
        {
            packageNameField.UnregisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            packageNameField.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);
            packageVersionField.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);
        }

        private void SetError(bool isNameError = false, bool isVersionError = false)
        {
            packageVersionField.RemoveFromClassList("error");
            packageNameField.RemoveFromClassList("error");

            AddToClassList("inputError");
            if (isNameError)
            {
                errorInfoBox.text = L10n.Tr("Unable to find the package with the specified name.\nPlease check the name and try again.");
                packageNameField.AddToClassList("error");
            }
            if (isVersionError)
            {
                errorInfoBox.text = L10n.Tr("Unable to find the package with the specified version.\nPlease check the version and try again.");
                packageVersionField.AddToClassList("error");
            }
            ShowWithNewWindowSize();
        }

        internal void SubmitClicked()
        {
            var packageName = packageNameField.value.Trim();
            if (string.IsNullOrEmpty(packageName))
                return;
            var version = packageVersionField.value.Trim();

            var packageNameParts = packageName.Split('@').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            var packageNameIsolated = packageNameParts.FirstOrDefault();
            var packageVersionIsolated = string.IsNullOrEmpty(version) ? packageNameParts.Length > 1 ? packageNameParts.Last() : null : version;

            if (packageNameParts.Length > 1 && !string.IsNullOrEmpty(version))
            {
                SetError(isNameError:true);
                return;
            }

            var package = m_PackageDatabase.GetPackage(packageNameIsolated);
            if (package != null && (string.IsNullOrEmpty(packageVersionIsolated) || package.versions.Any(v => v.versionString == packageVersionIsolated)))
            {
                CheckComplianceAndInstallPackage(package.compliance, packageNameIsolated, package.displayName,
                    packageVersionIsolated, package.product?.id.ToString());
                return;
            }

            m_UpmClient.ExtraFetchPackageInfo(packageNameIsolated,
                successCallback: packageInfo =>
                {
                    if (packageInfo != null)
                    {
                        if (string.IsNullOrEmpty(packageVersionIsolated) || packageInfo.versions.all.Contains(packageVersionIsolated))
                            CheckComplianceAndInstallPackage(packageInfo.compliance, packageNameIsolated,
                                packageInfo.displayName, packageVersionIsolated, packageInfo.assetStore?.productId);
                        else
                        {
                            // As of the time of writing, users may specify a version that is not included in the version list but is still returned by UPM.
                            // An example is com.unity.a@2, which is not present in the list, but UPM returns a package for it.
                            m_UpmClient.ExtraFetchPackageInfo($"{packageNameIsolated}@{packageVersionIsolated}", successCallback: morePackageInfo =>
                                {
                                    if (morePackageInfo != null)
                                    {
                                        CheckComplianceAndInstallPackage(morePackageInfo.compliance,
                                            packageNameIsolated, morePackageInfo.displayName, packageVersionIsolated,
                                            packageInfo.assetStore?.productId);
                                    }
                                }, errorCallback: error => SetError(isVersionError: true));
                        }
                    }
                    else
                        SetError(isNameError: true);
                },
                errorCallback: error => SetError(isNameError: true));

            inputForm.SetEnabled(false);
        }

        private bool ShouldBlockDueToComplianceViolation(PackageCompliance compliance)
        {
            return compliance != null && compliance.status != PackageComplianceStatus.Compliant;
        }


        private void CheckComplianceAndInstallPackage(PackageCompliance compliance, string packageName,
            string packageDisplayName, string packageVersion, string productId)
        {
            if (ShouldBlockDueToComplianceViolation(compliance))
            {
                var displayDialogArgs = new CustomDisplayDialogArgs(k_NonCompliantDialogTitle, idForAnalytics: "addByNameNonCompliantPackage", L10n.Tr("OK"))
                {
                    headerIcon = Icon.PackageErrorLarge,
                    headerMainText = packageDisplayName,
                    headerSubText = packageName,
                    headerInfoBoxIcon = Icon.Error,
                    headerInfoBoxText = k_NonCompliantDialogTitle,
                    bodyText = compliance.violation.message,
                    readMoreUrl = compliance.violation.readMoreLink,
                    readMoreClickedAnalyticsId = "restricted-package-read-more-clicked",
                    headerColor = HeaderColor.Red
                };
                m_CustomDisplayDialog.Show(displayDialogArgs);
                Close();
                return;
            }

            var packageId = string.IsNullOrEmpty(packageVersion) ? packageName : $"{packageName}@{packageVersion}";

            if (!m_OperationDispatcher.Install(packageId))
            {
                Close();
                return;
            }

            PackageManagerWindowAnalytics.SendEvent("addByNameAndVersion", packageId);

            Close();

            var packageUniqueId = string.IsNullOrEmpty(productId) ? packageName : productId;
            var package = m_PackageDatabase.GetPackage(packageUniqueId);
            if (package == null)
                return;

            var page = m_PageManager.FindPage(package);
            if (page != null)
            {
                m_PageManager.activePage = page;
                page.SetNewSelection(package);
            }
        }

        private void OnTextFieldChange(ChangeEvent<string> evt)
        {
            submitButton.SetEnabled(!string.IsNullOrWhiteSpace(packageNameField.value));
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    Close();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    SubmitClicked();
                    break;
            }
        }

        private VisualElementCache cache { get; }
        private VisualElement inputForm => cache.Get<VisualElement>("inputForm");
        internal TextField packageNameField => cache.Get<TextField>("packageName");
        internal TextField packageVersionField => cache.Get<TextField>("packageVersion");
        private HelpBox errorInfoBox => cache.Get<HelpBox>("errorInfoBox");
        private Button submitButton => cache.Get<Button>("submitButton");
    }
}
