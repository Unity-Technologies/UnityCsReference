// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    [Serializable]
    class SearchWindowCustomPanelConfig
    {
        [SerializeField] private SearchFunctor<Action<SearchWindowCustomPanelConfig, ISearchWindow, ISearchView, SearchElement>> m_BindPanel;
        [SerializeField] private SearchFunctor<Action<SearchWindowCustomPanelConfig, ISearchWindow, ISearchView, SearchElement>> m_UnbindPanel;
        [SerializeField] private string m_Id;

        public string id => m_Id;
        // User can store contextual data across calls to Bind/Unbind.
        public object bindUserData { get; set; }

        // A locked panel won't be overridden by another query being executed.
        public bool isLocked;
        
        // Should we persist this panel in a search Query saved on disk.
        public bool serializableInQuery;

        public SearchWindowCustomPanelConfig(string id)
        {
            m_Id = id;
        }

        public bool isValid => !string.IsNullOrEmpty(m_Id) && m_BindPanel != null;

        public Action<SearchWindowCustomPanelConfig, ISearchWindow, ISearchView, SearchElement> bindPanel
        {
            get => m_BindPanel?.handler;
            set => m_BindPanel = new SearchFunctor<Action<SearchWindowCustomPanelConfig, ISearchWindow, ISearchView, SearchElement>>(value);
        }

        public Action<SearchWindowCustomPanelConfig, ISearchWindow, ISearchView, SearchElement> unbindPanel
        {
            get => m_UnbindPanel?.handler;
            set => m_UnbindPanel = new SearchFunctor<Action<SearchWindowCustomPanelConfig, ISearchWindow, ISearchView, SearchElement>>(value);
        }
    }

    class SearchWindowCustomPanel : SearchElement
    {
        private SearchWindowCustomPanelConfig m_Config;
        public ISearchWindow window;
        public SearchWindowCustomPanel(ISearchWindow window, ISearchView view)
            : base("SearchWindowCustomPanel", view, "search-custom-panel")
        {
            this.window = window;
            // This panel is hidden by default.
            style.display = DisplayStyle.None;
        }

        public SearchWindowCustomPanelConfig config
        {
            get => m_Config;

            set
            {
                if (m_Config == value)
                    return;
                if (m_Config != null)
                {
                    m_Config.unbindPanel?.Invoke(m_Config, window, m_ViewModel, this);
                    // Be sure to nullify any user data
                    m_Config.bindUserData = null;
                    // Clear out any custom that might be lingering.
                    Clear();
                }
                m_Config = value;
                if (m_Config != null)
                {
                    m_Config.bindPanel?.Invoke(m_Config, window, m_ViewModel, this);
                }

                style.display = childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
