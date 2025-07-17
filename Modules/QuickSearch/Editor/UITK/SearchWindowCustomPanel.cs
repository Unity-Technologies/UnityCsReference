// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Analytics;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    [Serializable]
    class SearchWindowCustomPanelConfig
    {
        [SerializeField] private SearchFunctor<Action<ISearchWindow, ISearchView, VisualElement>> m_BindPanel;
        [SerializeField] private SearchFunctor<Action<ISearchWindow, ISearchView, VisualElement>> m_UnbindPanel;
        [SerializeField] private string m_Id;

        public string id => m_Id;

        public SearchWindowCustomPanelConfig(string id)
        {
            m_Id = id;
        }

        public bool isValid => !string.IsNullOrEmpty(m_Id) && m_BindPanel != null;

        public Action<ISearchWindow, ISearchView, VisualElement> bindPanel
        {
            get => m_BindPanel.handler;
            set => m_BindPanel = new SearchFunctor<Action<ISearchWindow, ISearchView, VisualElement>>(value);
        }

        public Action<ISearchWindow, ISearchView, VisualElement> unbindPanel
        {
            get => m_UnbindPanel.handler;
            set => m_UnbindPanel = new SearchFunctor<Action<ISearchWindow, ISearchView, VisualElement>>(value);
        }
    }

    class SearchWindowCustomPanel : VisualElement
    {
        private SearchWindowCustomPanelConfig m_Config;
        public ISearchWindow window;
        public ISearchView view;
        public SearchWindowCustomPanel(ISearchWindow window, ISearchView view)
        {
            this.window = window;
            this.view = view;
            AddToClassList("search-custom-panel");
        }

        public SearchWindowCustomPanelConfig config
        {
            get => m_Config;

            set
            {
                if (m_Config?.id == value?.id)
                    return;
                if (m_Config != null)
                {
                    m_Config.unbindPanel?.Invoke(window, view, this);
                    Clear();
                }
                m_Config = value;
                if (m_Config != null)
                {
                    m_Config.bindPanel?.Invoke(window, view, this);
                }

                style.display = childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
