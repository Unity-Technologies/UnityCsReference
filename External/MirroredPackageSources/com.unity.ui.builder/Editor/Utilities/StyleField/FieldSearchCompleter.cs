using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class FieldSearchCompleter<TData>
    {
        const int k_ItemHeight = 20;

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

        // List view properties
        int m_ItemHeight;
        Func<VisualElement> m_MakeItem;
        Action<VisualElement, int> m_BindItem;

        // UI parts
        FieldSearchCompleterPopup m_Popup;
        VisualElement m_DetailsContent;
        TextField m_TextField;

        // External references
        Builder m_Builder;

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

        public bool alwaysVisible { get; set; } = false;

        public List<TData> results
        {
            get => m_Results;
            private set
            {
                // Refresh every time set is called even when it has not changed
                m_Results = value;
                popup.listView.itemsSource = m_Results;
                popup.Refresh();
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

        public event Action<TData> onSelectionChange;

        public int itemHeight
        {
            get => m_ItemHeight;
            set
            {
                m_ItemHeight = value;
                UpdatePopup();
            }
        }

        public Func<VisualElement> makeItem
        {
            get => m_MakeItem;
            set
            {
                m_MakeItem = value;
                UpdatePopup();
            }
        }

        public Action<VisualElement, int> bindItem
        {
            get => m_BindItem;
            set
            {
                m_BindItem = value;
                UpdatePopup();
            }
        }

        void UpdatePopup()
        {
            if (m_Popup != null)
            {
                m_Popup.listView.fixedItemHeight = m_ItemHeight;
                m_Popup.listView.makeItem = m_MakeItem;
                m_Popup.listView.bindItem = m_BindItem;
                m_Popup.anchoredControl = textField;
            }
        }

        public Func<string, string> getFilterFromTextCallback
        {
            get => m_GetFilterFromTextCallback;
            set => m_GetFilterFromTextCallback = value;
        }

        public Func<TData, string> getTextFromDataCallback
        {
            get => m_GetTextFromDataCallback;
            set => m_GetTextFromDataCallback = value;
        }

        public FieldSearchCompleterPopup popup
        {
            get
            {
                if (m_Popup == null)
                {
                    var inspector = m_TextField.GetFirstAncestorOfType<BuilderInspector>();
                    if (inspector != null)
                    {
                        m_Builder = inspector.paneWindow as Builder;
                    }
                    else
                    {
                        m_Builder = null;
                    }

                    if (m_Builder != null)
                    {
                        m_Popup = new FieldSearchCompleterPopup();
                        if (detailsContent != null)
                            m_Popup.Add(detailsContent);
                        m_Builder.rootVisualElement.Add(m_Popup);
                        m_Popup.Hide();
                        m_Popup.onElementChosen += (index) =>
                        {
                            textField.value = GetTextFromData(results[index]);
                            textField.Q(TextField.textInputUssName).Blur();
                        };
                        m_Popup.onSelectionChange += (index) => onSelectionChange(index != -1 ? results[index] : default(TData));
                        UpdatePopup();
                    }
                }
                return m_Popup;
            }
        }

        public VisualElement detailsContent
        {
            get => m_DetailsContent;
            set => m_DetailsContent = value;
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
                UpdatePopup();
            }
        }

        void ConnectToField()
        {
            if (m_TextField != null && enabled)
            {
                m_TextField.Q(TextField.textInputUssName).RegisterCallback<FocusInEvent>(OnFocusIn);
                m_TextField.Q(TextField.textInputUssName).RegisterCallback<BlurEvent>(OnBlur);
                m_TextField.Q(TextField.textInputUssName).RegisterCallback<KeyDownEvent>(OnKeyDown);
            }
        }

        void DisconnectFromField()
        {
            if (m_TextField != null)
            {
                m_TextField.Q(TextField.textInputUssName).UnregisterCallback<FocusInEvent>(OnFocusIn);
                m_TextField.Q(TextField.textInputUssName).UnregisterCallback<BlurEvent>(OnBlur);
                m_TextField.Q(TextField.textInputUssName).UnregisterCallback<KeyDownEvent>(OnKeyDown);
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
                (e as Label).text = GetTextFromData(results[i]);
            };
        }

        static bool DefaultMatcher(string filter, TData data)
        {
            var text = data.ToString();
            return string.IsNullOrEmpty(text) ? false : text.Contains(filter);
        }

        void OnTextChanged()
        {
            if (m_ScheduledFilterUpdate == null)
            {
                m_ScheduledFilterUpdate = popup?.schedule.Execute(a => UpdateFilter(m_TextField.text));
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

        string GetFilterFromText(string text)
        {
            return m_GetFilterFromTextCallback != null ? m_GetFilterFromTextCallback(text) : text;
        }

        string GetTextFromData(TData data)
        {
            return m_GetTextFromDataCallback != null ? m_GetTextFromDataCallback(data) : data.ToString();
        }

        void SetFilter(string filter)
        {
            if (dataSource == null || popup == null || matcherCallback == null)
                return;

            var matchingDataList = new List<TData>();

            foreach (var data in dataSource)
            {
                if (string.IsNullOrEmpty(filter) || matcherCallback(filter, data))
                    matchingDataList.Add(data);
            }

            results = matchingDataList;

            if (popup.resolvedStyle.display != DisplayStyle.Flex)
            {
                popup.Show();

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
            m_DataSourceDirty = true;

            if (alwaysVisible)
            {
                if (textField.isReadOnly)
                    return;

                OnTextChanged();
            }
        }

        void OnBlur(BlurEvent e)
        {
            m_ScheduledFilterUpdate?.Pause();
            m_Popup?.Hide();
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

            // If the users presses DownArrow key but the popup is not visible then search using the current text
            if (e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Return || e.character == 3 || e.character == '\n')
            {
                var selectedIndex = popup.listView.selectedIndex;

                if (selectedIndex != -1)
                {
                    m_TextField.value = GetTextFromData(m_Results[selectedIndex]);
                    return;
                }
            }

            if (e.keyCode == KeyCode.DownArrow && popup.resolvedStyle.display != DisplayStyle.Flex)
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
                        evt.target = popup.listView.scrollView.contentContainer;
                        popup.listView.SendEvent(evt);
                    }
                }

                e.StopImmediatePropagation();
                e.PreventDefault();
            }
            else if (!IsNavigationEvent(e))
            {
                OnTextChanged();
            }
        }
    }

    class FieldSearchCompleterPopup : StyleFieldPopup
    {
        static readonly string s_UssClassName = "unity-field-search-completer-popup";
        static readonly string s_ResultLabelUssClassName = "unity-field-search-completer-popup__result-label";

        Label m_ResultLabel;

        public Action<int> onElementChosen;
        public Action<int> onSelectionChange;

        public ListView listView { get; private set; }

        public FieldSearchCompleterPopup()
        {
            AddToClassList(s_UssClassName);

            listView = new ListView();
            listView.Q<ScrollView>().horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            listView.onItemsChosen += (obj) =>
            {
                onElementChosen?.Invoke(listView.selectedIndex);
            };

            listView.onSelectionChange += (obj) =>
            {
                onSelectionChange?.Invoke(listView.selectedIndex);
            };

            Add(listView);
            Add(m_ResultLabel = new Label());
            m_ResultLabel.AddToClassList(s_ResultLabelUssClassName);

            // Avoid focus change when clicking on the popup
            listView.Q<ScrollView>().contentContainer.focusable = true;
            listView.focusable = false;
            listView.delegatesFocus = false;
            this.RegisterCallback<PointerDownEvent>(e =>
            {
                e.StopImmediatePropagation();
                e.PreventDefault();
            });
            style.minHeight = 0;
        }

        public override void AdjustGeometry()
        {
            const int minListViewHeight = 160;

            base.AdjustGeometry();
            listView.style.minHeight = Math.Min(minListViewHeight, listView.fixedItemHeight * (listView.itemsSource != null ? listView.itemsSource.Count : 0));
        }

        public void Refresh()
        {
            listView.Rebuild();

            listView.ClearSelection();
            m_ResultLabel.text = (listView.itemsSource != null ? listView.itemsSource.Count : 0) + " found";
        }
    }
}
