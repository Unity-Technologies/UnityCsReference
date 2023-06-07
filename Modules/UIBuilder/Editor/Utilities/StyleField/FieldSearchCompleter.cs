// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal interface IFieldSearchCompleter
    {
        public FieldSearchCompleterPopup popup { get; }

        /// <summary>
        /// Refreshes the result.
        /// </summary>
        public void Refresh();
    }

    class FieldSearchCompleter<TData> : IFieldSearchCompleter
    {
        const int k_ItemHeight = 20;
        const int k_PauseDelay = 250;

        static readonly List<TData> s_EmptyMatchingDataList = new List<TData>();

        // Properties
        bool m_Enabled = true;
        IEnumerable<TData> m_DataSource;
        List<TData> m_Results;

        // Internal state
        bool m_DataSourceDirty;
        IVisualElementScheduledItem m_ScheduledFilterUpdate;

        // Callbacks
        Func<IEnumerable<TData>> m_DataSourceCallback;
        Func<string, TData, bool> m_MatcherCallback;
        Func<string, string> m_GetFilterFromTextCallback;
        Func<TData, string> m_GetTextFromDataCallback;
        private Func<int, string> m_GetResultCountTextCallback;

        // List view properties
        int m_ItemHeight;
        Func<VisualElement> m_MakeItem;
        Action<VisualElement> m_DestroyItem;
        Action<VisualElement, int> m_BindItem;

        // UI parts
        FieldSearchCompleterPopup m_Popup;
        VisualElement m_DetailsContent;
        VisualElement m_Footer;
        TextField m_TextField;

        private bool m_TemporarilyDontShowPopup;

        private bool m_IsEvaluatingFocus;
        private bool m_HasPendingValueChanged;
        private string m_PendingPreviousValue;

        /// <summary>
        /// Indicates whether the popup is a native popup window rather than a overlay VisualElement.
        /// The default value is false.
        /// </summary>
        public bool usesNativePopupWindow { get; set; }

        public bool enabled
        {
            get => m_Enabled;
            set
            {
                if (m_Enabled == value)
                    return;

                m_Enabled = value;

                if (!m_Enabled)
                {
                    results = s_EmptyMatchingDataList;
                    DisconnectFromField();
                    m_Popup?.Hide();
                }
                else
                {
                    ConnectToField();
                }
            }
        }

        public bool alwaysVisible { get; set; }

        public List<TData> results
        {
            get => m_Results;
            private set
            {
                // Refresh every time set is called even when it has not changed
                m_Results = value;
                if (m_Popup != null)
                {
                    popup.listView.itemsSource = m_Results;
                    popup.resultLabel.text = GetResultCountText(m_Results?.Count ?? 0);
                    popup.Refresh();
                }
            }
        }

        public IEnumerable<TData> dataSource
        {
            get
            {
                if (m_DataSourceDirty)
                {
                    if (m_DataSourceCallback != null)
                    {
                        m_DataSource = m_DataSourceCallback();
                    }
                    else
                    {
                        m_DataSource = null;
                    }
                    m_DataSourceDirty = false;
                }
                return m_DataSource;
            }
        }

        public Func<IEnumerable<TData>> dataSourceCallback
        {
            get => m_DataSourceCallback;
            set => m_DataSourceCallback = value;
        }

        public Func<string, TData, bool> matcherCallback
        {
            get
            {
                if (m_MatcherCallback == null)
                {
                    m_MatcherCallback = DefaultMatcher;
                }
                return m_MatcherCallback;
            }
            set => m_MatcherCallback = value;
        }

        public TData selectedData
        {
            get
            {
                if (m_Popup != null)
                {
                    var index = m_Popup.listView.selectedIndex;

                    return index != -1 ? results[index] : default(TData);
                }
                return default(TData);
            }
        }

        public event Action<TData> hoveredItemChanged;
        public event Action<TData> selectionChanged;
        public Action<int> itemChosen;

        public int itemHeight
        {
            get => m_ItemHeight;
            set
            {
                m_ItemHeight = value;
                if (m_Popup != null)
                    m_Popup.listView.fixedItemHeight = m_ItemHeight;
            }
        }

        public Func<VisualElement> makeItem
        {
            get => m_MakeItem;
            set
            {
                m_MakeItem = value;
                if (m_Popup != null)
                    m_Popup.listView.makeItem = m_MakeItem;
            }
        }

        public Action<VisualElement> destroyItem
        {
            get => m_DestroyItem;
            set
            {
                m_DestroyItem = value;
                if (m_Popup != null)
                    m_Popup.listView.destroyItem = m_DestroyItem;
            }
        }

        public Action<VisualElement, int> bindItem
        {
            get => m_BindItem;
            set
            {
                m_BindItem = value;
                if (m_Popup != null)
                    m_Popup.listView.bindItem = m_BindItem;
            }
        }

        public Func<string, string> getFilterFromTextCallback
        {
            get => m_GetFilterFromTextCallback;
            set => m_GetFilterFromTextCallback = value;
        }

        public Func<int, string> getResultCountTextCallback
        {
            get => m_GetResultCountTextCallback;
            set => m_GetResultCountTextCallback = value;
        }

        public Func<TData, string> getTextFromDataCallback
        {
            get => m_GetTextFromDataCallback;
            set => m_GetTextFromDataCallback = value;
        }

        public FieldSearchCompleterPopup popup => m_Popup;

        public VisualElement detailsContent
        {
            get => m_DetailsContent;
            set
            {
                m_DetailsContent?.RemoveFromClassList(FieldSearchCompleterPopup.s_DetailsViewUssClassName);
                m_DetailsContent = value;
                m_DetailsContent?.AddToClassList(FieldSearchCompleterPopup.s_DetailsViewUssClassName);
            }
        }

        public VisualElement footer
        {
            get => m_Footer;
            set
            {
                m_Footer?.RemoveFromClassList(FieldSearchCompleterPopup.s_FooterUssClassName);
                m_Footer = value;
                m_Footer?.AddToClassList(FieldSearchCompleterPopup.s_FooterUssClassName);
            }
        }

        public TextField textField
        {
            get => m_TextField;
            set
            {
                if (m_TextField == value)
                    return;

                DisconnectFromField();

                m_TextField = value;

                ConnectToField();
                if (m_Popup != null)
                    m_Popup.anchoredControl = m_TextField.visualInput;
            }
        }

        void EnsurePopupIsCreated()
        {
            if (m_Popup == null)
            {
                VisualElement rootElement;

                var inspector = m_TextField.GetFirstAncestorOfType<BuilderInspector>();
                if (inspector != null)
                {
                    var builder = inspector.paneWindow as Builder;
                    rootElement = builder.rootVisualElement;
                }
                else
                {
                    rootElement = m_TextField.GetRootVisualContainer();
                }

                if (rootElement != null)
                {
                    m_Popup = CreatePopup();
                    m_Popup.usesNativeWindow = usesNativePopupWindow;

                    if (footer != null)
                        m_Popup.Add(footer);

                    if (detailsContent != null)
                    {
                        m_Popup.Add(detailsContent);
                    }

                    if (!usesNativePopupWindow)
                    {
                        rootElement.Add(m_Popup);
                    }

                    m_Popup.elementChosen += (index) =>
                    {
                        CancelEvaluateFocus();
                        textField.value = GetTextFromData(results[index]);
                        m_TemporarilyDontShowPopup = true;
                        textField.schedule.Execute((e) =>
                        {
                            textField.Blur();
                            m_TemporarilyDontShowPopup = false;
                        }).ExecuteLater(k_PauseDelay);
                        if (usesNativePopupWindow)
                        {
                            m_Popup?.Hide();
                        }

                        itemChosen?.Invoke(index);
                    };
                    m_Popup.hoveredItemChanged += (index) => hoveredItemChanged?.Invoke(index != -1 ? results[index] : default(TData));
                    m_Popup.selectionChanged += (index) => selectionChanged?.Invoke(index != -1 ? results[index] : default(TData));
                    m_Popup.onHide += OnPopupWindowClose;
                    UpdatePopup();
                }
            }
        }

        void ConnectToField()
        {
            if (m_TextField != null && enabled)
            {
                m_TextField.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
                m_TextField.RegisterCallback<FocusOutEvent>(OnFocusOut, TrickleDown.TrickleDown);
                m_TextField.RegisterValueChangedCallback(OnTextValueChange);
                m_TextField.Q(TextField.textInputUssName).RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                m_TextField.RegisterCallback<DetachFromPanelEvent>(OnTextFieldDetached);
            }
        }

        void DisconnectFromField()
        {
            if (m_TextField != null)
            {
                m_TextField.UnregisterCallback<DetachFromPanelEvent>(OnTextFieldDetached);
                m_TextField.UnregisterValueChangedCallback(OnTextValueChange);
                m_TextField.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
                m_TextField.UnregisterCallback<FocusOutEvent>(OnFocusOut, TrickleDown.TrickleDown);
                m_TextField.Q(TextField.textInputUssName).UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            }
        }

        void OnTextFieldDetached(DetachFromPanelEvent evt)
        {
            if (m_Popup != null && m_Popup.isOpened)
            {
                m_Popup.Hide();
            }
        }

        public FieldSearchCompleter() : this(null)
        {
        }

        public FieldSearchCompleter(TextField field)
        {
            textField = field;
            itemHeight = k_ItemHeight;
            makeItem = () =>
            {
                var item = new Label();
                return item;
            };
            bindItem = (e, i) =>
            {
                if (e is Label label)
                    label.text = GetTextFromData(results[i]);
            };
        }

        void UpdatePopup()
        {
            if (m_Popup != null)
            {
                m_Popup.listView.fixedItemHeight = m_ItemHeight;
                m_Popup.listView.makeItem = m_MakeItem;
                m_Popup.listView.destroyItem = m_DestroyItem;
                m_Popup.listView.bindItem = m_BindItem;
                m_Popup.anchoredControl = textField.visualInput;
            }
        }

        // This has to be called during an event to ensure that GUIUtility.GUIToScreenRect() uses the window containing the attached text field.
        void UpdateAnchoredControlScreenPosition()
        {
            var field = textField.visualInput;

            if (field != null)
            {
                field.SetProperty(BuilderConstants.CompleterAnchoredControlScreenRectVEPropertyName, GUIUtility.GUIToScreenRect(field.worldBound));
            }
        }

        protected virtual FieldSearchCompleterPopup CreatePopup() => new FieldSearchCompleterPopup();

        static bool DefaultMatcher(string filter, TData data)
        {
            var text = data.ToString();
            return !string.IsNullOrEmpty(text) && text.Contains(filter);
        }

        void OnTextChanged()
        {
            UpdateFilter(m_TextField.text);
        }

        void ScheduleTextChange()
        {
            if (m_ScheduledFilterUpdate == null)
            {
                m_ScheduledFilterUpdate = m_TextField?.schedule.Execute(a => UpdateFilter(m_TextField.text));
            }
            else
            {
                m_ScheduledFilterUpdate.ExecuteLater(0);
            }
        }

        protected virtual bool IsValidText(string text)
        {
            return true;
        }

        /// <summary>
        /// Refreshes the result.
        /// </summary>
        public void Refresh()
        {
            m_DataSourceDirty = true;
            if (m_Popup != null)
            {
                OnTextChanged();
            }
        }

        void UpdateFilter(string text)
        {
            if (!IsValidText(text))
            {
                results = s_EmptyMatchingDataList;
                popup?.Hide();
                return;
            }
            SetFilter(GetFilterFromText(text));
        }

        private string GetFilterFromText(string text)
        {
            return m_GetFilterFromTextCallback != null ? m_GetFilterFromTextCallback(text) : text;
        }

        protected virtual string GetTextFromData(TData data)
        {
            return m_GetTextFromDataCallback != null ? m_GetTextFromDataCallback(data) : data.ToString();
        }

        protected virtual string GetResultCountText(int count)
        {
            return m_GetResultCountTextCallback?.Invoke(count) ?? $"{count} found";
        }

        protected virtual bool MatchFilter(string filter, in TData data)
        {
            return string.IsNullOrEmpty(filter) || (matcherCallback?.Invoke(filter, data) ?? false);
        }

        private void SetFilter(string filter)
        {
            if (dataSource == null || matcherCallback == null)
                return;

            var matchingDataList = new List<TData>();

            foreach (var data in dataSource)
            {
                if (MatchFilter(filter, in data))
                    matchingDataList.Add(data);
            }

            results = matchingDataList;

            if (!popup.isOpened && !m_TemporarilyDontShowPopup)
            {
                popup.Show();
                popup.listView.RefreshItems();

                int selectedIndex = -1;
                int i = 0;

                // Select the element with the exact match
                foreach (var data in matchingDataList)
                {
                    if (!string.IsNullOrEmpty(filter) && filter == GetFilterFromText(GetTextFromData(data)))
                    {
                        selectedIndex = i;
                        break;
                    }
                    i++;
                }
                if (selectedIndex != -1)
                {
                    popup.listView.selectedIndex = selectedIndex;
                    popup.listView.ScrollToItem(selectedIndex);
                }
            }
            else
            {
                popup.AdjustGeometry();
            }
        }

        void OnFocusIn(FocusInEvent e)
        {
            if (m_TemporarilyDontShowPopup)
                return;

            m_DataSourceDirty = true;
            EnsurePopupIsCreated();

            if (alwaysVisible)
            {
                if (textField.isReadOnly)
                    return;
                UpdateAnchoredControlScreenPosition();
                OnTextChanged();
            }
        }

        void OnFocusOut(FocusOutEvent e)
        {
            m_ScheduledFilterUpdate?.Pause();
            if (usesNativePopupWindow)
            {
                ScheduleEvaluateFocus();
            }
            else
            {
                m_Popup?.Hide();
            }
        }

        void OnPopupWindowClose()
        {
            ScheduleEvaluateFocus();
        }

        void OnTextValueChange(ChangeEvent<string> e)
        {
            // Do not submit the value change until we evaluate the current focus. See EvaluateFocus.
            if (m_IsEvaluatingFocus)
            {
                PendValueChange(e);
            }
            else
            {
                ClearPendingValueChange();
            }
        }

        /*
         * Schedule the check of the current focus on the next frame
         */
        void ScheduleEvaluateFocus()
        {
            if (m_IsEvaluatingFocus)
                return;
            
            m_IsEvaluatingFocus = true;
            m_TextField.schedule.Execute(EvaluateFocus);
        }

        void CancelEvaluateFocus()
        {
            ClearPendingValueChange();
            m_IsEvaluatingFocus = false;
        }

        /*
         * When the target text field loses focus, we do not submit (by blocking ChangeEvent notification) the new value right away because we want to ensure that
         * the text field did not lose focus because the user click on the completer popup window (e.g user scrolls the results using the scrollbar).
         * If this is what happened then we will submit the pending change only when the popup window will also lose focus unless the focus is given back to the text field or if the user choses a result.
        */
        void EvaluateFocus()
        {
            if (!m_IsEvaluatingFocus)
                return;

            m_IsEvaluatingFocus = false;

            if (m_TextField.hasFocus || (m_Popup?.nativeWindow != null && m_Popup?.nativeWindow == EditorWindow.focusedWindow))
                return;

            SubmitPendingValueChange();
        }

        void PendValueChange(ChangeEvent<string> e)
        {
            m_HasPendingValueChanged = true;
            m_PendingPreviousValue = e.previousValue;
            e.StopImmediatePropagation();
        }

        void ClearPendingValueChange()
        {
            m_HasPendingValueChanged = false;
            m_PendingPreviousValue = null;
        }

        void SubmitPendingValueChange()
        {
            if (!m_HasPendingValueChanged)
                return;

            try
            {
                using var evt = ChangeEvent<string>.GetPooled(m_PendingPreviousValue, m_TextField.value);
                evt.elementTarget = m_TextField;
                m_TextField.SendEvent(evt);
            }
            finally
            {
                ClearPendingValueChange();
            }
        }

        bool IsNavigationEvent(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Tab:
                case KeyCode.LeftArrow:
                case KeyCode.RightArrow:
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                case KeyCode.PageUp:
                case KeyCode.PageDown:
                case KeyCode.Home:
                case KeyCode.End:
                    return true;
                default:
                    return false;
            }
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (textField.isReadOnly)
                return;

            EnsurePopupIsCreated();
            UpdateAnchoredControlScreenPosition();

            // If the users presses DownArrow key but the popup is not visible then search using the current text
            if (e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Return || e.character == 3 || e.character == '\n')
            {
                var selectedIndex = popup.listView.selectedIndex;

                if (selectedIndex != -1)
                {
                    m_TextField.value = GetTextFromData(m_Results[selectedIndex]);
                    textField.Blur();
                    m_TemporarilyDontShowPopup = true;
                    textField.schedule.Execute((e) => m_TemporarilyDontShowPopup = false).ExecuteLater(k_PauseDelay);
                    if (usesNativePopupWindow)
                    {
                        m_Popup?.Hide();
                    }
                    itemChosen?.Invoke(selectedIndex);
                }
            }

            if (e.keyCode == KeyCode.DownArrow && m_Popup is not {isOpened: true})
            {
                OnTextChanged();
            }
            // Forward the navigation key event to the list view as it does not have focus
            else if (m_Results != null && m_Results.Count > 0 && (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow))
            {
                if (e.keyCode == KeyCode.UpArrow && popup.listView.selectedIndex == 0)
                {
                    popup.listView.ClearSelection();
                }
                else if (e.keyCode == KeyCode.DownArrow && popup.listView.selectedIndex == -1)
                {
                    popup.listView.selectedIndex = 0;
                }
                else
                {
                    using (var evt = KeyDownEvent.GetPooled(e.character, e.keyCode, e.modifiers))
                    {
                        evt.elementTarget = popup.listView.scrollView.contentContainer;
                        popup.listView.SendEvent(evt);
                    }
                }

                e.StopImmediatePropagation();
            }
            else if (!IsNavigationEvent(e))
            {
                ScheduleTextChange();
            }
        }
    }

    class FieldSearchCompleterPopup : StyleFieldPopup
    {
        static readonly string s_UssClassName = "unity-field-search-completer-popup";
        public static readonly string s_DetailsViewUssClassName = s_UssClassName + "__details-view";
        public static readonly string s_FooterUssClassName = s_UssClassName + "__footer";
        public static readonly string s_ResultLabelUssClassName = s_UssClassName + "__result-label";

        int m_HoveredIndex = -1;

        public Action<int> elementChosen;
        public Action<int> selectionChanged;
        public event Action<int> hoveredItemChanged;

        public int hoveredIndex
        {
            get => m_HoveredIndex;
            set
            {
                if (m_HoveredIndex == value)
                    return;
                m_HoveredIndex = value;
                hoveredItemChanged?.Invoke(value);
            }
        }

        public ListView listView { get; }

        public Label resultLabel { get;  }

        public FieldSearchCompleterPopup()
        {
            AddToClassList(s_UssClassName);

            listView = new ListView();
            var sv = listView.Q<ScrollView>();
            sv.style.flexGrow = 0;
            sv.style.flexShrink = 1;
            listView.Q<ScrollView>().horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            listView.itemsChosen += (obj) =>
            {
                elementChosen?.Invoke(listView.selectedIndex);
            };

            listView.selectionChanged += (obj) =>
            {
                selectionChanged?.Invoke(listView.selectedIndex);
            };

            Add(listView);
            Add(resultLabel = new Label());
            resultLabel.AddToClassList(s_ResultLabelUssClassName);

            // Avoid focus change when clicking on the popup
            listView.Q<ScrollView>().contentContainer.focusable = true;
            listView.Query<Scroller>().ForEach(s =>
            {
                s.focusable = false;
            });
            listView.focusable = false;
            listView.delegatesFocus = false;
            RegisterCallback<PointerDownEvent>(e =>
            {
                e.StopImmediatePropagation();
            });
            listView.scrollView.contentContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            listView.scrollView.contentContainer.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);
            style.minHeight = 0;
        }


        void OnPointerMove(PointerMoveEvent evt)
        {
            var index = listView.virtualizationController.GetIndexFromPosition(evt.localPosition);

            if (index > listView.viewController.itemsSource.Count - 1)
            {
                hoveredIndex = -1;
            }
            else
            {
                hoveredIndex = index;
            }
        }

        void OnPointerLeaveEvent(PointerLeaveEvent evt)
        {
            hoveredIndex = -1;
        }

        public override void AdjustGeometry()
        {
            const int minListViewHeight = 160;

            base.AdjustGeometry();
            listView.style.minHeight = Math.Min(minListViewHeight, listView.fixedItemHeight * (listView.itemsSource != null ? listView.itemsSource.Count : 0));
        }

        public void Refresh()
        {
            listView.RefreshItems();
            listView.ClearSelection();
        }
    }
}
