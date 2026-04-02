// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal sealed class PackageSearchBar : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance()
            {
                var container = ServicesContainer.instance;
                return new PackageSearchBar(
                    container.Resolve<IUnityConnectProxy>(),
                    container.Resolve<IPageManager>());
            }
        }

        private SearchFieldDelayArgs m_SearchFieldDelayArgs;

        private const long k_SearchFieldDelayTicks = TimeSpan.TicksPerSecond / 3;
        private const int k_SearchFieldTextLimit = 500;
        public static readonly string k_SearchPlaceholderText = L10n.Tr("Search {0}");

        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IPageManager m_PageManager;

        public PackageSearchBar(IUnityConnectProxy unityConnect, IPageManager pageManager)
        {
            m_UnityConnect = unityConnect;
            m_PageManager = pageManager;

            m_SearchField = new ToolbarSearchField();
            m_SearchField.textInputField.maxLength = k_SearchFieldTextLimit;
            Add(m_SearchField);

            focusable = true;
            m_SearchFieldDelayArgs = null;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var root = evt.destinationPanel.visualTree;
            root.RegisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            root.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent);

            OnActivePageChanged(m_PageManager.activePage);
            m_SearchField.RegisterValueChangedCallback(OnSearchFieldChanged);

            m_PageManager.onActivePageChanged += OnActivePageChanged;
            m_PageManager.onTrimmedSearchTextChanged += OnTrimmedSearchTextChanged;

            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var root = evt.originPanel.visualTree;
            root.UnregisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            root.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent);

            m_SearchField.UnregisterValueChangedCallback(OnSearchFieldChanged);

            m_PageManager.onActivePageChanged -= OnActivePageChanged;
            m_PageManager.onTrimmedSearchTextChanged -= OnTrimmedSearchTextChanged;

            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        private void OnValidateCommandEvent(ValidateCommandEvent evt)
        {
            if (evt.commandName == EventCommandNames.Find)
                evt.StopPropagation();
        }

        private void OnExecuteCommandEvent(ExecuteCommandEvent evt)
        {
            if (evt.commandName == EventCommandNames.Find)
            {
                FocusOnSearchField();
                evt.StopPropagation();
            }
        }

        private void OnTrimmedSearchTextChanged(IPage page)
        {
            if (!page.isActive)
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

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
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

        private readonly ToolbarSearchField m_SearchField;
        public TextField searchTextField => m_SearchField.Q<TextField>();
    }

    internal class SearchFieldDelayArgs
    {
        public IPage page;
        public long timestampInTicks;
        public string searchText;
    }
}
