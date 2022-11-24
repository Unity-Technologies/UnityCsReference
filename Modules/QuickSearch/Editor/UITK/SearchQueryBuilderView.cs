// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchQueryBuilderView : SearchElement, ISearchField, INotifyValueChanged<string>
    {
        public static readonly string ussClassName = "search-query-builder";

        private QueryBuilder m_QueryBuilder;
        private TextField m_TextField;
        private readonly SearchFieldBase<TextField, string> m_SearchField;
        private Action m_RefreshBuilderOff;
        private readonly Button m_CancelButton;
        private Button m_SearchFieldCancelButton;

        int ISearchField.controlID => (int)m_SearchField.controlid;
        int ISearchField.cursorIndex => m_TextField?.cursorIndex ?? -1;
        string ISearchField.text => m_TextField?.text ?? context.searchText;

        string INotifyValueChanged<string>.value
        {
            get => m_QueryBuilder?.searchText;
            set
            {
                if (m_QueryBuilder == null || (m_QueryBuilder.hasOwnText && string.Equals(value, m_QueryBuilder.searchText, StringComparison.Ordinal)))
                    return;

                if (panel != null)
                {
                    var previousValue = m_QueryBuilder.searchText;
                    ((INotifyValueChanged<string>)this).SetValueWithoutNotify(value);

                    using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(previousValue, m_QueryBuilder.searchText))
                    {
                        evt.target = this;
                        SendEvent(evt);
                    }
                }
                else
                {
                    ((INotifyValueChanged<string>)this).SetValueWithoutNotify(value);
                }
            }
        }

        internal QueryBuilder builder => m_QueryBuilder;

        public SearchQueryBuilderView(string name, ISearchView viewModel, SearchFieldBase<TextField, string> searchField)
            : base(name, viewModel, ussClassName)
        {
            m_SearchField = searchField;

            m_CancelButton = new Button(() => {}) { name = "query-builder-unity-cancel" };
            m_CancelButton.AddToClassList(SearchFieldBase<TextField, string>.cancelButtonUssClassName);
            m_CancelButton.AddToClassList(SearchFieldBase<TextField, string>.cancelButtonOffVariantUssClassName);

            m_CancelButton.clickable.clicked += OnCancelButtonClick;
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterGlobalEventHandler<KeyDownEvent>(GlobalEventHandler, 0);
            On(SearchEvent.RefreshBuilder, RefreshBuilder);
            On(SearchEvent.SearchContextChanged, Rebuild);
            On(SearchEvent.SearchTextChanged, UpdateCancelButton);

            m_TextField = m_SearchField.Q<TextField>();
            m_TextField.RemoveFromHierarchy();

            RefreshBuilder();

            m_SearchField.value = m_QueryBuilder.wordText;

            m_TextField.RegisterCallback<ChangeEvent<string>>(OnQueryChanged);

            m_SearchFieldCancelButton = m_SearchField.Q<Button>(null, SearchFieldBase<TextField, string>.cancelButtonUssClassName);
            if (m_SearchFieldCancelButton != null)
                HideElements(m_SearchFieldCancelButton);

            m_SearchField.Add(m_CancelButton);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_SearchField.Remove(m_CancelButton);

            if (m_SearchFieldCancelButton != null)
                ShowElements(m_SearchFieldCancelButton);

            m_TextField?.UnregisterCallback<ChangeEvent<string>>(OnQueryChanged);
            m_TextField = null;

            m_RefreshBuilderOff?.Invoke();

            UnregisterCallback<KeyDownEvent>(OnKeyDown);
            UnregisterGlobalEventHandler<KeyDownEvent>(GlobalEventHandler);
            Off(SearchEvent.RefreshBuilder, RefreshBuilder);
            Off(SearchEvent.SearchContextChanged, Rebuild);
            Off(SearchEvent.SearchTextChanged, UpdateCancelButton);

            base.OnDetachFromPanel(evt);
        }

        private void Rebuild(ISearchEvent evt)
        {
            m_QueryBuilder = null;
            DeferRefreshBuilder();
        }

        private void OnQueryChanged(ChangeEvent<string> evt)
        {
            if (m_QueryBuilder != null)
                m_QueryBuilder.wordText = evt.newValue;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.imguiEvent != null && (m_QueryBuilder?.HandleKeyEvent(evt.imguiEvent) ?? false))
            {
                evt.StopImmediatePropagation();
                evt.PreventDefault();
            }
        }

        private bool GlobalEventHandler(KeyDownEvent evt)
        {
            var isHandled = m_QueryBuilder?.HandleGlobalKeyDown(evt) ?? false;
            if (isHandled)
                Focus();
            return isHandled;
        }

        private void DeferRefreshBuilder()
        {
            m_RefreshBuilderOff?.Invoke();
            m_RefreshBuilderOff = Utils.CallDelayed(RefreshBuilder, 0.1f);
        }

        private void RefreshBuilder(ISearchEvent evt)
        {
            DeferRefreshBuilder();
        }

        private void RefreshBuilder()
        {
            Clear();

            if (m_QueryBuilder != null)
                m_QueryBuilder.Build();
            else
                m_QueryBuilder = new QueryBuilder(context, this);

            foreach (var b in m_QueryBuilder.EnumerateBlocks())
                Add(b.CreateGUI());

            if (context.options.HasAny(SearchFlags.Debug))
                Debug.LogWarning($"Refresh query builder {m_QueryBuilder.searchText} ({m_QueryBuilder.blocks.Count})");

            m_TextField?.SetValueWithoutNotify(m_QueryBuilder.wordText);

            UpdateCancelButton();

            Emit(SearchEvent.BuilderRefreshed);
        }

        void ISearchField.Focus()
        {
            m_SearchField.Focus();
        }

        VisualElement ISearchField.GetTextElement()
        {
            return m_TextField;
        }

        void INotifyValueChanged<string>.SetValueWithoutNotify(string newValue)
        {
            if (m_QueryBuilder == null || (m_QueryBuilder.hasOwnText && string.Equals(newValue, m_QueryBuilder.searchText, StringComparison.Ordinal)))
                return;
            m_QueryBuilder.searchText = newValue;
            DeferRefreshBuilder();
        }

        private void OnCancelButtonClick()
        {
            ((INotifyValueChanged<string>) this).value = string.Empty;
            UpdateCancelButton();
        }

        private void UpdateCancelButton(ISearchEvent evt)
        {
            UpdateCancelButton();
        }

        private void UpdateCancelButton()
        {
            m_CancelButton.EnableInClassList(SearchFieldBase<TextField, string>.cancelButtonOffVariantUssClassName, string.IsNullOrEmpty(m_QueryBuilder.searchText));
        }
    }
}
