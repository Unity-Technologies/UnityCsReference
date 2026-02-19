// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchIndexingWarningWindow : SearchElement
    {
        public static readonly string k_IndexingInProgressLabel = L10n.Tr("Indexing is still in progress. Some results may not be available until indexing is complete.");

        private SearchProgressBinding m_SpinnerAnimation;
        private Button m_Spinner;
        private Button m_CloseBtn;
        private bool m_IsDismissed;
        private bool? m_IsDbReady;

        public SearchIndexingWarningWindow(ISearchView viewModel, bool isWarningWindowDismissed)
            : base("SearchIndexingWarningWindow", viewModel)
        {
            m_Spinner = CreateButton("SearchIndexingWarningSpinner", GUIContent.none, () => { }, baseIconLabelClassName, "search-warning-spinner");
            m_SpinnerAnimation = new SearchProgressBinding(m_ViewModel, m_Spinner, () => m_IsDbReady.HasValue ? !m_IsDbReady.Value : false);
            Add(m_Spinner);

            m_IsDismissed = isWarningWindowDismissed;

            var text = new Label(k_IndexingInProgressLabel);
            Add(text);

            m_CloseBtn = CreateButton("SearchIndexingWarningClose", GUIContent.none, Close, "search-warning-close-button");
            m_CloseBtn.style.backgroundImage = Icons.clear;
            Add(m_CloseBtn);

            style.display = DisplayStyle.None;
        }

        public bool CheckIndexing()
        {
            if (m_IsDismissed)
                return m_IsDismissed;

            var dbReady = SearchIndexingService.IsIndexReady();
            if (dbReady != m_IsDbReady)
            {
                m_IsDbReady = dbReady;
                style.display = dbReady ? DisplayStyle.None : DisplayStyle.Flex;
            }
            if (m_IsDbReady.HasValue && !m_IsDbReady.Value)
            {
                m_SpinnerAnimation.Update();
            }
            return m_IsDismissed;
        }

        void Close()
        {
            m_IsDismissed = true;
            style.display = DisplayStyle.None;
        }
    }
}
