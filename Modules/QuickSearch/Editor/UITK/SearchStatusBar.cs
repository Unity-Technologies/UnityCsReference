// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchStatusBar : SearchElement
    {
        private readonly Label m_StatusMessage;
        private readonly Slider m_ItemSizeSlider;
        private readonly Button m_ListButton;
        private readonly Button m_GridButton;
        private readonly Button m_TableButton;
        private readonly Button m_PrefButton;
        private readonly Button m_SpinnerButton;

        public static readonly string prefButtonTooltip = L10n.Tr("Open search preferences...");
        public static readonly string listModeTooltip = L10n.Tr("List View");
        public static readonly string gridModeTooltip = string.Format(L10n.Tr("Grid View ({0}x{0})"), (int)DisplayMode.Grid);
        public static readonly string tableModeTooltip = L10n.Tr("Table View");

        public static readonly string ussClassName = "search-statusbar";
        public static readonly string messageLabelClassName = ussClassName.WithUssElement("message");
        public static readonly string itemSizeSliderClassName = ussClassName.WithUssElement("item-size-slider");
        public static readonly string hiddenClassName = ussClassName.WithUssModifier("hidden");
        public static readonly string preferencesButtonClassName = ussClassName.WithUssElement("preferences-button");
        public static readonly string listModeButtonClassName = ussClassName.WithUssElement("list-mode-button");
        public static readonly string gridModeButtonClassName = ussClassName.WithUssElement("grid-mode-button");
        public static readonly string tableModeButtonClassName = ussClassName.WithUssElement("table-mode-button");


        public SearchStatusBar(string name, ISearchView viewModel)
            : base(name, viewModel, ussClassName)
        {
            m_StatusMessage = Create<Label>("SearchStatusMessage", messageLabelClassName);

            m_ItemSizeSlider = new Slider(0f, (float)DisplayMode.Limit, SliderDirection.Horizontal, 0f);
            m_ItemSizeSlider.name = "SearchItemSizeSlider";
            m_ItemSizeSlider.AddToClassList(itemSizeSliderClassName);
            UpdateItemSizeSlider(viewModel.itemIconSize);

            m_ListButton = CreateButton("SearchListButton", listModeTooltip, SelectListMode, baseIconButtonClassName, listModeButtonClassName);
            m_GridButton = CreateButton("SearchGridButton", gridModeTooltip, SelectGridMode, baseIconButtonClassName, gridModeButtonClassName);
            m_TableButton = CreateButton("SearchTableButton", tableModeTooltip, SelectTableMode, baseIconButtonClassName, tableModeButtonClassName);

            Add(m_StatusMessage);
            Add(m_ItemSizeSlider);
            Add(m_ListButton);
            Add(m_GridButton);
            Add(m_TableButton);

            if (!m_ViewModel.IsPicker())
            {
                m_PrefButton = CreateButton("SearchPreferencesButton", prefButtonTooltip, OpenPreferences, baseIconButtonClassName, preferencesButtonClassName);
                Add(m_PrefButton);
            }

            m_SpinnerButton = CreateButton("SearchInProgressButton", GUIContent.none , () => {}, baseIconButtonClassName);
            m_SpinnerButton.binding = new SearchProgressBinding(m_ViewModel, m_SpinnerButton);
            Add(m_SpinnerButton);

            UpdateSelectedItemSizeButton();
        }

        private void UpdateSelectedItemSizeButton()
        {
            switch (m_ViewModel.displayMode)
            {
                case DisplayMode.Compact:
                case DisplayMode.List:
                    m_ListButton.pseudoStates |= PseudoStates.Checked;
                    m_GridButton.pseudoStates &= ~PseudoStates.Checked;
                    m_TableButton.pseudoStates &= ~PseudoStates.Checked;
                    break;

                case DisplayMode.Grid:
                    m_ListButton.pseudoStates &= ~PseudoStates.Checked;
                    m_GridButton.pseudoStates |= PseudoStates.Checked;
                    m_TableButton.pseudoStates &= ~PseudoStates.Checked;
                    break;

                case DisplayMode.Table:
                    m_ListButton.pseudoStates &= ~PseudoStates.Checked;
                    m_GridButton.pseudoStates &= ~PseudoStates.Checked;
                    m_TableButton.pseudoStates |= PseudoStates.Checked;
                    break;

                default:
                    m_ListButton.pseudoStates &= ~PseudoStates.Checked;
                    m_GridButton.pseudoStates &= ~PseudoStates.Checked;
                    m_TableButton.pseudoStates &= ~PseudoStates.Checked;
                    break;
            }
        }

        private void OnItemSizeChanged(ChangeEvent<float> evt)
        {
            SetItemSize(evt.newValue);
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            On(SearchEvent.RefreshContent, OnRefreshed);
            On(SearchEvent.ViewStateUpdated, OnRefreshed);
            m_StatusMessage.RegisterCallback<PointerDownEvent>(HandleStatusMessageClicked);
            m_ItemSizeSlider.RegisterCallback<ChangeEvent<float>>(OnItemSizeChanged);
            base.OnAttachToPanel(evt);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Off(SearchEvent.RefreshContent, OnRefreshed);
            Off(SearchEvent.ViewStateUpdated, OnRefreshed);
            m_StatusMessage.UnregisterCallback<PointerDownEvent>(HandleStatusMessageClicked);
            m_ItemSizeSlider.UnregisterCallback<ChangeEvent<float>>(OnItemSizeChanged);
            base.OnDetachFromPanel(evt);
        }

        private void SelectListMode() => SetItemSize(DisplayMode.List);
        private void SelectGridMode() => SetItemSize(DisplayMode.Grid);
        private void SelectTableMode() => SetItemSize(DisplayMode.Table);

        private void SetItemSize(DisplayMode displayMode)
        {
            if ((float)displayMode == m_ViewModel.itemIconSize)
                return;

            SetItemSize((float)displayMode);
            SearchAnalytics.SendEvent(viewState.sessionId,
                SearchAnalytics.GenericEventType.QuickSearchSizeRadioButton,
                displayMode.ToString());
        }

        private void SetItemSize(float itemSize)
        {
            m_ViewModel.itemIconSize = itemSize;
            if (!viewState.forceViewMode)
                SearchSettings.itemIconSize = itemSize;
            UpdateItemSizeSlider(itemSize);
            UpdateSelectedItemSizeButton();
        }

        private void UpdateItemSizeSlider(float itemSize)
        {
            if (m_ItemSizeSlider == null)
                return;

            if (itemSize <= (float)DisplayMode.Limit)
                m_ItemSizeSlider.SetValueWithoutNotify(itemSize);
            m_ItemSizeSlider.EnableInClassList(hiddenClassName, m_ViewModel.displayMode == DisplayMode.Table);
        }

        private void OpenPreferences()
        {
            if (!m_ViewModel.IsPicker())
                SearchUtils.OpenPreferences();
        }

        private void OnRefreshed(ISearchEvent evt)
        {
            var width = resolvedStyle.width;
            if (TryGetCurrentError(out var err))
            {
                var firstLineReason = err.reason;
                var firstLineIndex = string.IsNullOrEmpty(err.reason) ? -1 : err.reason.IndexOf("\n");
                if (firstLineIndex >= 0)
                    firstLineReason = err.reason.Substring(0, firstLineIndex);
                var msg = $"[in {err.provider.name}] {Utils.TrimText(firstLineReason)}";
                m_StatusMessage.text = msg;
                m_StatusMessage.tooltip = $"({err.provider.name}) {err.reason}";
                m_StatusMessage.style.color = Color.red;
            }
            else if (SearchSettings.showStatusBar && width >= 340f)
            {
                var status = SearchUtils.FormatStatusMessage(context, m_ViewModel?.totalCount ?? 0);
                m_StatusMessage.text = status;
                m_StatusMessage.tooltip = status;
                m_StatusMessage.style.color = new StyleColor(StyleKeyword.Null);
            }
            else if (!string.IsNullOrEmpty(m_StatusMessage.text))
            {
                // Clear the status
                m_StatusMessage.text = null;
                m_StatusMessage.tooltip = null;
                m_StatusMessage.style.color = new StyleColor(StyleKeyword.Null);
            }

            UpdateSelectedItemSizeButton();
            UpdateItemSizeSlider(viewState.itemSize);
        }

        private bool TryGetCurrentError(out SearchQueryError error)
        {
            error = null;
            var currentGroup = m_ViewModel.currentGroup;
            var hasProgress = context.searchInProgress;
            var ignoreErrors = m_ViewModel.results.Count > 0 || hasProgress;
            var alwaysPrintError = currentGroup == null ||
                !string.IsNullOrEmpty(context.filterId) ||
                (m_ViewModel.totalCount == 0 && string.Equals(GroupedSearchList.allGroupId, currentGroup, StringComparison.Ordinal));
            if (!ignoreErrors && m_ViewModel.GetAllVisibleErrors().FirstOrDefault(e => alwaysPrintError || e.provider.type == m_ViewModel.currentGroup) is SearchQueryError err)
            {
                error = err;
                return true;
            }

            return false;
        }

        void HandleStatusMessageClicked(PointerDownEvent evt)
        {
            if (!TryGetCurrentError(out var error))
                return;
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, error.reason);
        }
    }

    class SearchProgressBinding : IBinding
    {
        private readonly ISearchView m_ViewModel;
        private readonly VisualElement m_Target;
        private readonly Image m_SpinnerImage;
        private readonly GUIContent[] m_Wheels;

        public SearchProgressBinding(ISearchView viewModel, VisualElement target)
        {
            m_ViewModel = viewModel;
            m_Target = target;
            m_SpinnerImage = target.Query<Image>();

            m_Wheels = new GUIContent[12];
            for (int i = 0; i < 12; i++)
                m_Wheels[i] = EditorGUIUtility.IconContent("WaitSpin" + i.ToString("00"));

            Update();
        }

        void IBinding.PreUpdate() { }
        void IBinding.Release() { }

        public void Update()
        {
            var searchInProgress = m_ViewModel.searchInProgress;
            if (searchInProgress && m_SpinnerImage != null)
            {
                int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 5, 11.99f);
                m_SpinnerImage.image = m_Wheels[frame].image;
                m_SpinnerImage.tooltip = m_Wheels[frame].tooltip;
            }

            m_Target.EnableInClassList(SearchStatusBar.hiddenClassName, !searchInProgress);
        }
    }
}
