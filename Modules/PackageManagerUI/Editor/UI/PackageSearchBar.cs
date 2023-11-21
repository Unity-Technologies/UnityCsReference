// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageSearchBar : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PackageSearchBar();
        }

        private SearchFieldDelayArgs m_SearchFieldDelayArgs;

        private const long k_SearchFieldDelayTicks = TimeSpan.TicksPerSecond / 3;
        public static readonly string k_SearchPlaceholderText = L10n.Tr("Search {0}");

        private IUpmRegistryClient m_UpmRegistryClient;
        private IProjectSettingsProxy m_SettingsProxy;
        private IUnityConnectProxy m_UnityConnect;
        private IPageManager m_PageManager;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_UpmRegistryClient = container.Resolve<IUpmRegistryClient>();
            m_SettingsProxy = container.Resolve<IProjectSettingsProxy>();
            m_UnityConnect = container.Resolve<IUnityConnectProxy>();
            m_PageManager = container.Resolve<IPageManager>();
        }

        public PackageSearchBar()
        {
            m_SearchField = new ToolbarSearchField();
            Add(m_SearchField);

            ResolveDependencies();

            focusable = true;
            m_SearchFieldDelayArgs = null;
        }

        public void OnEnable()
        {
            OnActivePageChanged(m_PageManager.activePage);
            m_SearchField.RegisterValueChangedCallback(OnSearchFieldChanged);

            m_PageManager.onActivePageChanged += OnActivePageChanged;
            m_PageManager.onTrimmedSearchTextChanged += OnTrimmedSearchTextChanged;
            m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
        }

        public void OnDisable()
        {
            m_SearchField.UnregisterValueChangedCallback(OnSearchFieldChanged);

            m_PageManager.onActivePageChanged -= OnActivePageChanged;
            m_PageManager.onTrimmedSearchTextChanged -= OnTrimmedSearchTextChanged;
            m_UpmRegistryClient.onRegistriesModified -= OnRegistriesModified;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        private void OnTrimmedSearchTextChanged(IPage page, string trimmedSearchText)
        {
            if (!page.isActivePage)
                return;

            // When the trimmed text changed, we know for sure that the original search text has changed too
            // hence we can safely use the trimmed search text change event to update search field with original search text
            if (page.searchText != m_SearchField.value)
                m_SearchField.SetValueWithoutNotify(page.searchText);
        }

        public void FocusOnSearchField()
        {
            m_SearchField.Focus();
            // Line below is required to make sure focus is in textfield
            m_SearchField.Q<TextField>()?.visualInput?.Focus();
        }

        private void OnSearchFieldChanged(ChangeEvent<string> evt)
        {
            m_SearchFieldDelayArgs = new SearchFieldDelayArgs()
            {
                timestampInTicks = DateTime.Now.Ticks,
                page = m_PageManager.activePage,
                searchText = m_SearchField.value
            };

            EditorApplication.update -= SearchFieldDelayUpdate;
            EditorApplication.update += SearchFieldDelayUpdate;
        }

        private void SearchFieldDelayUpdate()
        {
            if (DateTime.Now.Ticks - m_SearchFieldDelayArgs.timestampInTicks <= k_SearchFieldDelayTicks)
                return;
            SyncTextFromSearchFieldToPage();
        }

        public void OnActivePageChanged(IPage page)
        {
            if (m_SearchFieldDelayArgs != null)
                SyncTextFromSearchFieldToPage();

            searchTextField.textEdition.placeholder = string.Format(k_SearchPlaceholderText, page.displayName);
            m_SearchField.SetValueWithoutNotify(page.searchText);
            RefreshVisibility();
        }

        private void SyncTextFromSearchFieldToPage()
        {
            EditorApplication.update -= SearchFieldDelayUpdate;

            var page = m_SearchFieldDelayArgs.page;
            page.searchText = m_SearchFieldDelayArgs.searchText;
            m_SearchFieldDelayArgs = null;
            if (!string.IsNullOrEmpty(page.trimmedSearchText))
                PackageManagerWindowAnalytics.SendEvent("search");
        }

        private void OnRegistriesModified()
        {
            if (m_SettingsProxy.registries?.Count <= 1)
                m_PageManager.GetPage(MyRegistriesPage.k_Id).searchText = string.Empty;
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            m_PageManager.GetPage(MyAssetsPage.k_Id).searchText = string.Empty;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            var visibility = m_PageManager.activePage.id != MyAssetsPage.k_Id || m_UnityConnect.isUserLoggedIn;
            UIUtils.SetElementDisplay(this, visibility);
            // We use MarkDirtyRepaint as a workaround for the issue of placeholder text refresh when changing pages
            // This should be fixed in UUM-21819 by the UIToolkit team
            searchTextField.Q<TextElement>().MarkDirtyRepaint();
        }

        private ToolbarSearchField m_SearchField;
        public TextField searchTextField => m_SearchField.Children().OfType<TextField>().FirstOrDefault();
    }

    internal class SearchFieldDelayArgs
    {
        public IPage page;
        public long timestampInTicks;
        public string searchText;
    }
}
