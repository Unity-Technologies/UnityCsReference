// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class AddPackageByNameDropdown : DropdownContent
    {
        private static readonly string k_NonCompliantDialogTitle = L10n.Tr("Restricted Package", null);
        private static readonly string k_PartOfNonCompatiblePackageErrorMessage = "compatible with this Unity version";

        private const float k_Width = 320f;
        private const float k_BaseHeight = 72f;
        private const float k_ErrorBoxSpacing = 8f;
        private Vector2 m_CurrentSize = new(k_Width, k_BaseHeight);
        public override Vector2 windowSize => m_CurrentSize;

        // We save the initial values and only set the field values when `OnDropdownShown` is called because
        // if we set it too early before the VisualElement is visible, the placeholder text will not show up correctly.
        public string packageNameInitialValue { get; set; }
        public string packageVersionInitialValue { get; set; }

        private readonly IUpmClient m_UpmClient;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPageManager m_PageManager;
        private readonly IPackageOperationDispatcher m_OperationDispatcher;
        private readonly ICustomDisplayDialog m_CustomDisplayDialog;
        private readonly IApplicationProxy m_ApplicationProxy;
        public AddPackageByNameDropdown(IResourceLoader resourceLoader, IUpmClient upmClient, IPackageDatabase packageDatabase, IPageManager packageManager, IPackageOperationDispatcher packageOperationDispatcher, ICustomDisplayDialog displayDialogCustom, IApplicationProxy applicationProxy)
        {
            m_UpmClient = upmClient;
            m_PackageDatabase = packageDatabase;
            m_PageManager = packageManager;
            m_OperationDispatcher = packageOperationDispatcher;
            m_CustomDisplayDialog = displayDialogCustom;
            m_ApplicationProxy = applicationProxy;

            styleSheets.Add(resourceLoader.inputDropdownStyleSheet);

            var root = resourceLoader.GetTemplate("AddPackageByNameDropdown.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            packageNameField.textEdition.placeholder = L10n.Tr("Technical name ", null) + "(com.org.package)";
            packageVersionField.textEdition.placeholder = L10n.Tr("Version (optional)", null);

            submitButton.clickable.clicked += SubmitClicked;

            errorInfoBox.RegisterCallback<GeometryChangedEvent>(OnErrorInfoBoxGeometryChanged);
        }

        private void OnErrorInfoBoxGeometryChanged(GeometryChangedEvent evt)
        {
            var errorHeight = string.IsNullOrEmpty(errorInfoBox.text)
                ? 0f
                : Mathf.Ceil(evt.newRect.height) + k_ErrorBoxSpacing;
            var newSize = new Vector2(k_Width, k_BaseHeight + errorHeight);
            if (newSize == m_CurrentSize)
                return;

            m_CurrentSize = newSize;
            if (container != null)
            {
                container.minSize = newSize;
                container.maxSize = newSize;
            }
        }

        public override void OnDropdownShown()
        {
            packageNameField.RegisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            packageNameField.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);
            packageVersionField.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);

            inputForm.SetEnabled(true);
            if (!string.IsNullOrEmpty(packageNameInitialValue))
            {
                packageNameField.value = packageNameInitialValue;
                packageNameInitialValue = string.Empty;
            }
            if (!string.IsNullOrEmpty(packageVersionInitialValue))
            {
                packageVersionField.value = packageVersionInitialValue;
                packageVersionInitialValue = string.Empty;
            }
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

        private void SetError(bool isNameError = false, bool isVersionError = false, string customErrorMessage = null)
        {
            packageVersionField.RemoveFromClassList("error");
            packageNameField.RemoveFromClassList("error");

            AddToClassList("inputError");
            if (isNameError)
            {
                errorInfoBox.text = L10n.Tr("Unable to find the package with the specified name.\nPlease check the name and try again.", null);
                packageNameField.AddToClassList("error");
            }
            if (isVersionError)
            {
                errorInfoBox.text = L10n.Tr("Unable to find the package with the specified version.\nPlease check the version and try again.", null);
                packageVersionField.AddToClassList("error");
            }

            if (!string.IsNullOrEmpty(customErrorMessage))
            {
                errorInfoBox.text = customErrorMessage;
                packageNameField.AddToClassList("error");
            }
            ShowWithNewWindowSize();
        }

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal void SubmitClicked()
        {
            var packageName = packageNameField.value.Trim();
            if (string.IsNullOrEmpty(packageName))
                return;
            var version = packageVersionField.value.Trim();

            var packageNameParts = packageName.Split(new []{'@'}, StringSplitOptions.RemoveEmptyEntries);
            var packageNameIsolated = packageNameParts[0];
            var packageVersionIsolated = string.IsNullOrEmpty(version) ? packageNameParts.Length > 1 ? packageNameParts[^1] : null : version;

            if (packageNameParts.Length > 1 && !string.IsNullOrEmpty(version))
            {
                SetError(isNameError:true);
                return;
            }

            var package = m_PackageDatabase.GetPackage(packageNameIsolated);
            if (package != null && (string.IsNullOrEmpty(packageVersionIsolated) || package.versions.AnyMatches(v => v.versionString == packageVersionIsolated)))
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
                        if (string.IsNullOrEmpty(packageVersionIsolated) || packageInfo.versions.all.ContainsMatches(packageVersionIsolated))
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
                errorCallback: error =>
                {
                    if (error.message?.Contains(k_PartOfNonCompatiblePackageErrorMessage) == true)
                    {
                        var message = string.Format(
                                L10n.Tr("Unable to find a version of package [{0}] compatible with this Unity version ({1}).", null),
                                packageName, m_ApplicationProxy.unityVersion);
                        SetError(customErrorMessage: message);
                    }
                    else
                        SetError(isNameError: true);
                });

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
                var displayDialogArgs = new CustomDisplayDialogArgs(k_NonCompliantDialogTitle, idForAnalytics: "addByNameNonCompliantPackage", L10n.Tr("OK", null), new Vector2(340f, 165f))
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

            if (!m_OperationDispatcher.Install(packageId, OperationType.Install))
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
                page.SetNewSelection(package.uniqueId, false);
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
        public TextField packageNameField => cache.Get<TextField>("packageName");
        public TextField packageVersionField => cache.Get<TextField>("packageVersion");
        private HelpBox errorInfoBox => cache.Get<HelpBox>("errorInfoBox");
        private Button submitButton => cache.Get<Button>("submitButton");
    }
}
