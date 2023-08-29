// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class AddPackageByNameDropdown : DropdownContent
    {
        private static readonly Vector2 k_DefaultWindowSize = new(320, 72);
        private static readonly Vector2 k_WindowSizeWithError = new(320, 110);
        internal override Vector2 windowSize => string.IsNullOrEmpty(errorInfoBox.text) ? k_DefaultWindowSize : k_WindowSizeWithError;

        private EditorWindow m_AnchorWindow;

        private TextFieldPlaceholder m_PackageNamePlaceholder;
        private TextFieldPlaceholder m_PackageVersionPlaceholder;

        private IResourceLoader m_ResourceLoader;
        private IUpmClient m_UpmClient;
        private IPackageDatabase m_PackageDatabase;
        private IPageManager m_PageManager;
        private void ResolveDependencies(IResourceLoader resourceLoader, IUpmClient upmClient, IPackageDatabase packageDatabase, IPageManager packageManager)
        {
            m_ResourceLoader = resourceLoader;
            m_UpmClient = upmClient;
            m_PackageDatabase = packageDatabase;
            m_PageManager = packageManager;
        }

        public AddPackageByNameDropdown(IResourceLoader resourceLoader, IUpmClient upmClient, IPackageDatabase packageDatabase, IPageManager packageManager, EditorWindow anchorWindow)
        {
            ResolveDependencies(resourceLoader, upmClient, packageDatabase, packageManager);

            styleSheets.Add(m_ResourceLoader.inputDropdownStyleSheet);

            var root = m_ResourceLoader.GetTemplate("AddPackageByNameDropdown.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            Init(anchorWindow);
        }

        private void Init(EditorWindow anchorWindow)
        {
            m_AnchorWindow = anchorWindow;

            packageNameField.RegisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            packageNameField.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);
            packageVersionField.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);

            m_PackageNamePlaceholder = new TextFieldPlaceholder(packageNameField, L10n.Tr("Name"));
            m_PackageVersionPlaceholder = new TextFieldPlaceholder(packageVersionField, L10n.Tr("Version (optional)"));

            submitButton.clickable.clicked += SubmitClicked;
        }

        internal override void OnDropdownShown()
        {
            inputForm.SetEnabled(true);

            // If we show a DropdownElement (dropdown filled with url values), we don't use the anchor window
            if (container != null)
                m_AnchorWindow?.rootVisualElement?.SetEnabled(false);

            if (string.IsNullOrEmpty(errorInfoBox.text) || packageNameField.ClassListContains("error"))
                packageNameField.Focus();
            else
                packageVersionField.Focus();
            submitButton.SetEnabled(!string.IsNullOrWhiteSpace(packageNameField.value));
        }

        internal override void OnDropdownClosed()
        {
            m_PackageNamePlaceholder.OnDisable();
            m_PackageVersionPlaceholder.OnDisable();

            packageNameField.UnregisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            packageNameField.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);
            packageVersionField.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);

            if (m_AnchorWindow != null)
            {
                m_AnchorWindow.rootVisualElement.SetEnabled(true);
                m_AnchorWindow = null;
            }

            submitButton.clickable.clicked -= SubmitClicked;
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

            var package = m_PackageDatabase.GetPackage(packageName);
            if (package != null)
            {
                if (string.IsNullOrEmpty(version))
                {
                    InstallByNameAndVersion(packageName);
                    return;
                }
                else if (package.versions.Any(v => v.versionString == version))
                {
                    InstallByNameAndVersion(packageName, version);
                    return;
                }
            }

            m_UpmClient.ExtraFetchPackageInfo(packageName,
                successCallback: packageInfo =>
                {
                    var packageVersion = packageVersionField.value;
                    if (packageInfo == null)
                        SetError(isNameError: true);
                    else if (string.IsNullOrEmpty(packageVersion) || packageInfo.versions.all.Contains(packageVersion) == true)
                        InstallByNameAndVersion(packageName, packageVersion);
                    else
                        SetError(isVersionError: true);
                },
                errorCallback: error => SetError(isNameError: true));

            inputForm.SetEnabled(false);
        }

        private void InstallByNameAndVersion(string packageName, string packageVersion = null)
        {
            var packageId = string.IsNullOrEmpty(packageVersion) ? packageName : $"{packageName}@{packageVersion}";
            m_UpmClient.AddById(packageId);

            PackageManagerWindowAnalytics.SendEvent("addByNameAndVersion", packageId);

            Close();

            var package = m_PackageDatabase.GetPackage(packageName);
            if (package == null)
                return;

            var page = m_PageManager.FindPage(package);
            if (page != null)
            {
                m_PageManager.activePage = page;
                page.SetNewSelection(package, package.versions?.FirstOrDefault(v => v.versionString == packageVersion));
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

        private VisualElementCache cache { get; set; }
        private VisualElement inputForm => cache.Get<VisualElement>("inputForm");
        internal TextField packageNameField => cache.Get<TextField>("packageName");
        internal TextField packageVersionField => cache.Get<TextField>("packageVersion");
        private HelpBox errorInfoBox => cache.Get<HelpBox>("errorInfoBox");
        private Button submitButton => cache.Get<Button>("submitButton");
    }
}
