// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    sealed class SearchViewItemButtonWithContext : Button
    {
        public delegate void SearchViewItemButtonClickedEventHandler(SearchViewItemButtonWithContext button, SearchItem searchItem);

        readonly SearchViewItemButtonClickedEventHandler m_Handler;

        public SearchItem BoundItem { get; set; }

        public SearchViewItemButtonWithContext(string name, string text, string tooltip, SearchViewItemButtonClickedEventHandler handler, params string[] classNames)
        {
            this.name = name;
            this.text = text;
            this.tooltip = tooltip;
            m_Handler = handler;
            foreach (var n in classNames)
                AddToClassList(n);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            clicked += OnClick;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            clicked -= OnClick;
        }

        void OnClick()
        {
            m_Handler?.Invoke(this, BoundItem);
        }
    }
}
