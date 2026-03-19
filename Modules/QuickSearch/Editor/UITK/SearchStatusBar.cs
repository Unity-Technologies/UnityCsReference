// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchStatusBar : SearchElement
    {
        private readonly Label m_StatusMessage;
        private readonly Slider m_ItemSizeSlider;
        private readonly List<Button> m_DisplayModeButtons;
        private readonly Button m_PrefButton;
        private readonly Button m_SpinnerButton;
        private VisualElement m_ButtonContainer;

        public static readonly string prefButtonTooltip = L10n.Tr("Open search preferences...");

        public static readonly string ussClassName = "search-statusbar";
        public static readonly string modeButtonClassName = ussClassName + "-mode-button";
        public static readonly string messageLabelClassName = ussClassName.WithUssElement("message");
        public static readonly string itemSizeSliderClassName = ussClassName.WithUssElement("item-size-slider");
        public static readonly string hiddenClassName = ussClassName.WithUssModifier("hidden");
        public static readonly string preferencesButtonClassName = ussClassName.WithUssElement("preferences-button");

        public SearchStatusBar(string name, ISearchView viewModel)
            : base(name, viewModel, ussClassName)
        {
            m_StatusMessage = Create<Label>("SearchStatusMessage", messageLabelClassName);
            m_DisplayModeButtons = new();
            m_ButtonContainer = new VisualElement();
            m_ButtonContainer.style.flexDirection = FlexDirection.Row;

            CreateDisplayModeButtons(out var minSlider, out var maxSlider);
            m_ItemSizeSlider = new Slider(minSlider, maxSlider, SliderDirection.Horizontal, 0f);
            m_ItemSizeSlider.name = "SearchItemSizeSlider";
            m_ItemSizeSlider.AddToClassList(itemSizeSliderClassName);

            UpdateItemSizeSlider(viewModel.itemIconSize);

            Add(m_StatusMessage);
            Add(m_ItemSizeSlider);

            Add(m_ButtonContainer);

            foreach (var button in m_DisplayModeButtons)
                m_ButtonContainer.Add(button);

            if (!m_ViewModel.IsPicker())
            {
                m_PrefButton = CreateButton("SearchPreferencesButton", prefButtonTooltip, OpenPreferences, baseIconButtonClassName, preferencesButtonClassName);
                Add(m_PrefButton);
            }

            m_SpinnerButton = CreateButton("SearchInProgressButton", GUIContent.none , () => {}, baseIconLabelClassName);
            m_SpinnerButton.binding = new SearchProgressBinding(m_ViewModel, m_SpinnerButton);
            Add(m_SpinnerButton);

            UpdateSelectedItemSizeButton();
        }

        void CreateDisplayModeButtons(out float minSlider, out float maxSlider)
        {
            minSlider = float.MaxValue;
            maxSlider = float.MinValue;
            foreach (var desc in m_ViewModel.state.resultViewDescriptorList)
            {
                var btn = CreateButton(desc.Id, desc.Description, () => SetResultView(desc), baseIconButtonClassName, modeButtonClassName, desc.ButtonClassName);
                btn.style.backgroundImage = desc.FetchIcon();
                
                btn.userData = desc.Id;
                m_DisplayModeButtons.Add(btn);

                if (desc.SupportsSizeSlider)
                {
                    if (desc.SizeMin < minSlider)
                    {
                        minSlider = desc.SizeMin;
                    }
                    if (desc.SizeMax > maxSlider)
                    {
                        maxSlider = desc.SizeMax;
                    }
                }
                m_ButtonContainer.Add(btn);
            }
            if (minSlider == float.MaxValue)
                minSlider = 0f;
            if (maxSlider == float.MinValue)
                maxSlider = 100f;
            if (maxSlider < minSlider)
            {
                var temp = minSlider;
                minSlider = maxSlider;
                maxSlider = temp;
            }
        }

        bool NeedUpdateDisplayModeButtons()
        {
            if (m_DisplayModeButtons.Count != m_ViewModel.state.resultViewDescriptorList.Count)
                return true;

            for(var i = 0; i < m_DisplayModeButtons.Count; ++i)
            {
                var desc = m_ViewModel.state.resultViewDescriptorList[i];
                if ((string)m_DisplayModeButtons[i].userData != desc.Id)
                    return true;
            }

            return false;
        }

        internal void UpdateDisplayModeButtons()
        {
            if (!NeedUpdateDisplayModeButtons())
                return;

            m_ButtonContainer.Clear();
            m_DisplayModeButtons.Clear();

            CreateDisplayModeButtons(out var minSlider, out var maxSlider);
            m_ItemSizeSlider.lowValue = minSlider;
            m_ItemSizeSlider.highValue = maxSlider;
        }

        private void UpdateSelectedItemSizeButton()
        {
            var viewId = m_ViewModel.state.resultViewDescriptorList.CurrentViewId;
            foreach (var btn in m_DisplayModeButtons)
            {
                var isCurrent = (string)btn.userData == viewId;
                btn.SetCheckedPseudoState(isCurrent);
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

        private void SetItemSize(float itemSize)
        {
            m_ViewModel.itemIconSize = itemSize;
            SearchSettings.itemIconSize = itemSize;
            UpdateItemSizeSlider(itemSize);
            UpdateSelectedItemSizeButton();
        }

        private void SetResultView(SearchResultViewDescriptor desc)
        {
            m_ViewModel.currentResultViewId = desc.Id;
            SearchSettings.itemIconSize = m_ViewModel.itemIconSize;
            UpdateItemSizeSlider(m_ViewModel.itemIconSize);
            UpdateSelectedItemSizeButton();
        }

        private void UpdateItemSizeSlider(float itemSize)
        {
            if (m_ItemSizeSlider == null)
                return;

            if (itemSize <= (float)DisplayMode.Limit)
                m_ItemSizeSlider.SetValueWithoutNotify(itemSize);

            var currentViewDesc = m_ViewModel.state.resultViewDescriptorList.Current;
            m_ItemSizeSlider.EnableInClassList(hiddenClassName, !currentViewDesc.SupportsSizeSlider);
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
            UpdateItemSizeSlider(viewState.itemIconSize);
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (!ignoreErrors && m_ViewModel.GetAllVisibleErrors().FirstOrDefault(e => alwaysPrintError || e.provider.type == m_ViewModel.currentGroup) is SearchQueryError err)
#pragma warning restore UA2001
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
        private Func<bool> m_KeepSpinning;

        public SearchProgressBinding(ISearchView viewModel, VisualElement target)
            : this(viewModel, target, () => viewModel.searchInProgress)
        {
        }

        public SearchProgressBinding(ISearchView viewModel, VisualElement target, Func<bool> keepSpinning)
        {
            m_KeepSpinning = keepSpinning;
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

        public void Spin()
        {
            int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 5, 11.99f);
            m_SpinnerImage.image = m_Wheels[frame].image;
            m_SpinnerImage.tooltip = m_Wheels[frame].tooltip;
        }

        public void Update()
        {
            var keepSpinning = m_KeepSpinning();
            if (keepSpinning && m_SpinnerImage != null)
            {
                Spin();
            }
            m_Target.EnableInClassList(SearchStatusBar.hiddenClassName, !keepSpinning);
        }
    }
}
