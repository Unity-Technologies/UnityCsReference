// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.Search
{
    class SearchAutoCompleteItem : SearchElement
    {
        Label m_Label;
        Label m_Tooltip;
        SearchPropositionOptions m_Options;
        SearchProposition m_Proposition;

        public event Action<SearchProposition> selected;

        public SearchAutoCompleteItem(ISearchView viewModel, SearchPropositionOptions options)
            : base("SearchAutoCompleteItem", viewModel, "search-autocomplete-item")
        {
            m_Options = options;
            m_Label = Create<Label>("SearchAutoCompleteItemLabel", "search-autocomplete-item__label");
            m_Tooltip = Create<Label>("SearchAutoCompleteItemDescription", "search-autocomplete-item__description");

            Add(m_Label);
            Add(m_Tooltip);
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);
            RegisterCallback<ClickEvent>(HandleClickEvent);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<ClickEvent>(HandleClickEvent);
            base.OnDetachFromPanel(evt);
        }

        void HandleClickEvent(ClickEvent evt)
        {
            selected?.Invoke(m_Proposition);
        }

        public void BindItem(SearchProposition proposition)
        {
            m_Proposition = proposition;
            m_Label.text = Utils.TrimText(HightlightLabel(proposition.label));
            if (!string.IsNullOrEmpty(proposition.help))
                m_Tooltip.text = Utils.TrimText(proposition.help);
        }

        string HightlightLabel(string label)
        {
            if (string.IsNullOrEmpty(label) || m_Options.tokens.Any(string.IsNullOrEmpty) || label.IndexOf('<') != -1)
                return label;
            foreach (var token in m_Options.tokens)
            {
                var escapedToken = Regex.Escape(token);
                label = Regex.Replace(label, escapedToken, $"<b>{token}</b>", RegexOptions.IgnoreCase);
            }
            return label;
        }
    }

    class SearchAutoCompleteWindow : SearchElement
    {
        UnityEngine.UIElements.PopupWindow m_PopupWindow;
        UnityEngine.UIElements.ListView m_ListView;
        VisualElement m_RootWindowElement;
        SearchToolbar m_SearchToolbar;
        ToolbarSearchField m_ToolbarSearchField;
        TextField m_TextField;
        VisualElement m_AttachedRoot;

        float m_ItemHeight;
        const float k_WindowPaddingTop = 10f;
        const float k_WindowPaddingBottom = 10f;
        const string k_MetricsLineHeightName = "--unity-metrics-single_line-height";

        List<SearchProposition> m_FilteredList = null;
        bool m_Initialized;

        SearchPropositionOptions options { get; set; }
        SortedSet<SearchProposition> propositions { get; set; }

        public float x
        {
            get => style.left.value.value;
            set => style.left = value;
        }

        public float y
        {
            get => style.top.value.value;
            set => style.top = value;
        }

        public StyleLength width
        {
            get => style.width;
            set => style.width = value;
        }

        public StyleLength height
        {
            get => style.height;
            set => style.height = value;
        }

        public float maxWidth { get; private set; } = -1f;
        public float maxHeight { get; private set; } = -1f;

        public bool enabled { get; private set; }

        public UnityEngine.UIElements.ListView listView => m_ListView;

        public SearchProposition? selection
        {
            get
            {
                if (!enabled || m_FilteredList == null || m_ListView == null)
                    return null;

                if (m_ListView.selectedIndex < 0 || m_ListView.selectedIndex >= m_FilteredList.Count)
                    return null;

                return m_FilteredList[m_ListView.selectedIndex];
            }
        }

        public int count
        {
            get
            {
                if (!enabled || m_FilteredList == null)
                    return 0;

                return m_FilteredList.Count;
            }
        }

        public SearchAutoCompleteWindow(ISearchView viewModel, VisualElement rootWindowElement)
            : base("SearchAutoCompleteWindow", viewModel)
        {
            style.position = Position.Absolute;
            width = new StyleLength(StyleKeyword.Auto);
            m_RootWindowElement = rootWindowElement;
        }

        void DestroyItem(VisualElement element)
        {
            if (element is not SearchAutoCompleteItem item)
                return;
            item.selected -= HandleSelection;
        }

        void BindItem(VisualElement element, int index)
        {
            var item = element as SearchAutoCompleteItem;
            if (item == null)
                return;
            item.BindItem(m_FilteredList[index]);
        }

        VisualElement MakeItem()
        {
            var item = new SearchAutoCompleteItem(m_ViewModel, options);
            item.selected += HandleSelection;
            return item;
        }

        void Init()
        {
            if (m_Initialized)
                return;

            m_PopupWindow = Create<UnityEngine.UIElements.PopupWindow>("SearchPopupWindow", "search-autocomplete-window");
            m_PopupWindow.style.paddingTop = k_WindowPaddingTop;
            m_PopupWindow.style.paddingBottom = k_WindowPaddingBottom;
            m_ListView = new UnityEngine.UIElements.ListView();
            m_ListView.fixedItemHeight = m_ItemHeight;
            m_ListView.makeItem = MakeItem;
            m_ListView.bindItem = BindItem;
            m_ListView.destroyItem = DestroyItem;
            m_ListView.scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            m_ListView.scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            m_PopupWindow.Add(m_ListView);
            Add(m_PopupWindow);

            m_Initialized = true;
        }

        void InitializeSearchField()
        {
            ClearSearchField();
            m_ToolbarSearchField = m_SearchToolbar.searchField.searchField;
            m_ToolbarSearchField.RegisterValueChangedCallback(HandleTextFieldChanged);
            m_TextField = m_ToolbarSearchField?.Q<TextField>();
        }

        void ClearSearchField()
        {
            m_ToolbarSearchField?.UnregisterValueChangedCallback(HandleTextFieldChanged);
            m_ToolbarSearchField = null;
        }

        public void Show(SearchToolbar searchToolbar)
        {
            m_SearchToolbar = searchToolbar;

            // Attach the empty window to the root element to trigger an attach to panel
            m_RootWindowElement.Add(this);
        }

        void Show()
        {
            options = new SearchPropositionOptions(context, m_TextField.cursorIndex, m_TextField.text);
            propositions = SearchProposition.Fetch(context, options);

            enabled = propositions.Count > 0;
            if (!enabled)
            {
                Close();
                return;
            }

            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchAutoCompleteTab, string.Join(",", options.tokens));

            if (!UpdateCompleteList(m_TextField, options))
            {
                Close();
                return;
            }

            // Select the first item so we don't have to press down once to select it.
            m_ListView.selectedIndex = 0;
        }

        void HandleRootCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            m_AttachedRoot?.UnregisterCallback<CustomStyleResolvedEvent>(HandleRootCustomStyleResolved);
            InitializeCustomStyleValues();

            Init();
            Show();
        }

        bool InitializeCustomStyleValues()
        {
            if (!TryReadStyleDimension(m_AttachedRoot?.computedStyle.customProperties, k_MetricsLineHeightName, out m_ItemHeight, EditorGUIUtility.singleLineHeight))
                return false;

            return true;
        }

        static bool TryReadStyleDimension(Dictionary<string, StylePropertyValue> customProperties, string propertyName, out float value, float defaultValue = 0.0f)
        {
            value = defaultValue;
            if (customProperties != null &&
                customProperties.TryGetValue(propertyName, out var customProp))
            {
                if (customProp.sheet.TryReadDimension(customProp.handle, out var dimension))
                {
                    value = dimension.value;
                    return true;
                }
            }

            return false;
        }

        public void Close()
        {
            ClearAllCallbacks();
            RemoveFromHierarchy();
            Clear();
            enabled = false;
            m_Initialized = false;
            m_TextField = null;
            m_ToolbarSearchField = null;
            m_SearchToolbar = null;
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            m_RootWindowElement.RegisterCallback<GeometryChangedEvent>(HandleRootElementGeometryChanged);
            InitializeSearchField();
            RegisterCallback<GeometryChangedEvent>(HandlePopupGeometryChanged);
            On(SearchEvent.RefreshBuilder, HandleBuilderRefreshed);
            RegisterGlobalEventHandler<KeyDownEvent>(HandleGlobalKeyDownEvent, 10);

            m_AttachedRoot = GetRootVisualContainer();
            if (InitializeCustomStyleValues())
            {
                Init();
                Show();
            }
            else
                m_AttachedRoot.RegisterCallback<CustomStyleResolvedEvent>(HandleRootCustomStyleResolved);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ClearAllCallbacks();
            base.OnDetachFromPanel(evt);
        }

        void ClearAllCallbacks()
        {
            Off(SearchEvent.RefreshBuilder, HandleBuilderRefreshed);
            m_AttachedRoot?.UnregisterCallback<CustomStyleResolvedEvent>(HandleRootCustomStyleResolved);
            UnregisterGlobalEventHandler<KeyDownEvent>(HandleGlobalKeyDownEvent);
            UnregisterCallback<GeometryChangedEvent>(HandlePopupGeometryChanged);
            m_RootWindowElement?.UnregisterCallback<GeometryChangedEvent>(HandleRootElementGeometryChanged);
            ClearSearchField();
        }

        void HandleBuilderRefreshed(ISearchEvent evt)
        {
            InitializeSearchField();

            var textElement = m_TextField.Q<TextElement>();
            textElement.generateVisualContent += HandleTextElementGenerateVisualContent;
        }

        void HandleTextElementGenerateVisualContent(MeshGenerationContext obj)
        {
            var textElement = m_TextField.Q<TextElement>();
            textElement.generateVisualContent -= HandleTextElementGenerateVisualContent;
            UpdateWindowSizeAndPosition();
        }

        bool HandleGlobalKeyDownEvent(KeyDownEvent evt)
        {
            if (!enabled)
                return false;

            if (IsKeySelection(evt))
            {
                HandleSelection();
                return true;
            }

            if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                return true;
            }

            if ((evt.keyCode != KeyCode.UpArrow && evt.keyCode != KeyCode.DownArrow) || evt.modifiers.HasAny(EventModifiers.Alt | EventModifiers.Control | EventModifiers.Shift))
                return false;

            var newIndex = m_ListView.selectedIndex;
            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                    --newIndex;
                    break;
                case KeyCode.DownArrow:
                    ++newIndex;
                    break;
            }

            m_ListView.selectedIndex = WrapAround(newIndex, m_FilteredList.Count);
            return true;
        }

        static int WrapAround(int value, int mod)
        {
            var r = value % mod;
            return r < 0 ? r + mod : r;
        }

        void HandleSelection()
        {
            var selectedIndex = m_ListView.selectedIndex;
            if (m_FilteredList == null || selectedIndex < 0 || selectedIndex >= m_FilteredList.Count)
                return;
            HandleSelection(m_FilteredList[selectedIndex]);
        }

        void HandleSelection(SearchProposition proposition)
        {
            if (proposition.valid)
            {
                if (proposition.moveCursor == TextCursorPlacement.MoveLineEnd)
                {
                    m_TextField.value = proposition.replacement;
                    m_SearchToolbar.searchField.MoveCursor(TextCursorPlacement.MoveLineEnd, -1);
                }
                else if (!options.tokens.All(t => t.StartsWith(proposition.replacement, StringComparison.OrdinalIgnoreCase)))
                {
                    var insertion = ReplaceText(m_TextField.text, proposition.replacement, options.cursor, out var insertTokenPos);
                    SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchAutoCompleteInsertSuggestion, insertion);

                    m_TextField.value = insertion;
                    m_SearchToolbar.searchField.MoveCursor(proposition.moveCursor, insertTokenPos);
                }
            }

            Close();
        }

        void HandlePopupGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateWindowSizeAndPosition();
        }

        void HandleRootElementGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.layoutPass == 0)
                UpdateWindowSizeAndPosition();
        }

        void HandleTextFieldChanged(ChangeEvent<string> evt)
        {
            options = new SearchPropositionOptions(context, m_TextField.cursorIndex, m_TextField.text);
            propositions = SearchProposition.Fetch(context, options);
            if (propositions.Count <= 0 || !UpdateCompleteList(m_TextField, options))
                Close();
        }

        void UpdateWindowSizeAndPosition()
        {
            var position = CalcRect(m_TextField, rect.width, maxHeight < 0f ? m_RootWindowElement.rect.height * 0.8f : maxHeight);
            UpdateWindowSizeAndPosition(position);
        }

        void UpdateWindowSizeAndPosition(Rect position)
        {
            position = FitToRect(position, m_RootWindowElement.rect);

            // Positions were computed in world space. Remove the parent translation to get the parent local space.
            x = position.x - m_RootWindowElement.worldBound.x;
            y = position.y - m_RootWindowElement.worldBound.y;
            height = position.height;
            style.maxWidth = m_RootWindowElement.rect.width;
        }

        Rect CalcRect(TextField tf, float maxWidth, float maxHeight)
        {
            return CalcRect(tf, new Vector2(maxWidth, maxHeight), true);
        }

        Rect CalcRect(TextField tf, Vector2 popupSize, bool setMinMax = false)
        {
            if (setMinMax)
                popupSize = new Vector2(popupSize.x, Mathf.Max(115f, Mathf.Min(propositions.Count * m_ItemHeight, popupSize.y)));
            var popupOffset = new Vector2(tf.worldBound.xMin, tf.worldBound.yMax);
            var cursorPos = new Vector2(tf.cursorPosition.x, 0f);
            return new Rect(cursorPos + popupOffset, popupSize);
        }

        static Rect FitToRect(Rect rect, Rect boundingRect)
        {
            var newRect = new Rect(rect);
            var xOffscreen = boundingRect.width - rect.xMax;
            if (xOffscreen < 0)
                newRect.x += xOffscreen;
            if (newRect.x < 0)
            {
                newRect.width += newRect.x;
                newRect.x = 0;
            }

            var yOffScreen = boundingRect.height - rect.yMax;
            if (yOffScreen < 0)
                newRect.height += yOffScreen;

            return newRect;
        }

        bool UpdateCompleteList(in TextField tf, in SearchPropositionOptions baseOptions = null)
        {
            options = baseOptions ?? new SearchPropositionOptions(tf.text, tf.cursorIndex);
            var position = CalcRect(tf, rect.width, m_RootWindowElement.rect.height * 0.8f);
            UpdateWindowSizeAndPosition(position);

            var maxVisibleCount = Mathf.FloorToInt((position.height - k_WindowPaddingTop - k_WindowPaddingBottom) / m_ItemHeight);
            BuildCompleteList(options.tokens, maxVisibleCount, 0.4f);

            m_ListView.itemsSource = m_FilteredList;
            m_ListView.Rebuild();

            return m_FilteredList.Count > 0;
        }

        void BuildCompleteList(string[] inputs, int maxCount, float levenshteinDistance)
        {
            var uniqueSrc = new List<SearchProposition>(propositions);
            int srcCnt = uniqueSrc.Count;

            m_FilteredList = new List<SearchProposition>(Math.Min(maxCount, srcCnt));

            // Start with - slow
            SelectPropositions(ref srcCnt, maxCount, uniqueSrc, p => inputs.Any(i => p.label.StartsWith(i, StringComparison.OrdinalIgnoreCase)));

            m_FilteredList.Sort();

            // Contains - very slow
            inputs = FilterInputWords(inputs);
            SelectPropositions(ref srcCnt, maxCount, uniqueSrc, (p) =>
            {
                if (inputs.Any(i => p.label.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1))
                    return true;
                if (p.help != null && inputs.Any(i => p.help.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1))
                    return true;
                return false;
            });

            // Levenshtein Distance - very very slow.
            if (levenshteinDistance > 0f && inputs.Length > 0 && m_FilteredList.Count < maxCount)
            {
                levenshteinDistance = Mathf.Clamp01(levenshteinDistance);
                SelectPropositions(ref srcCnt, maxCount, uniqueSrc, p =>
                {
                    return inputs.Any(levenshteinInput =>
                    {
                        int distance = Utils.LevenshteinDistance(p.label, levenshteinInput, caseSensitive: false);
                        return (int)(levenshteinDistance * p.label.Length) >= distance;
                    });
                });
            }
        }

        static string[] FilterInputWords(in IEnumerable<string> words)
        {
            return words.Where(i => i.Length > 3).Select(w => w
                .Replace("<", "")
                .Replace("=", "")
                .Replace(">", "")
                .Replace("#m_", "#")).Distinct().ToArray();
        }

        void SelectPropositions(ref int srcCnt, int maxCount, List<SearchProposition> source, Func<SearchProposition, bool> compare)
        {
            for (int i = 0; i < srcCnt && m_FilteredList.Count < maxCount; i++)
            {
                var p = source[i];
                if (!compare(p))
                    continue;

                m_FilteredList.Add(p);
                source.RemoveAt(i);
                srcCnt--;
                i--;
            }
        }

        public static bool IsKeySelection(IKeyboardEvent evt)
        {
            var kc = evt.keyCode;
            return kc == KeyCode.Return || kc == KeyCode.KeypadEnter || kc == KeyCode.Tab;
        }

        private static int IndexOfDelimiter(string self, int startIndex)
        {
            for (int index = startIndex; index < self.Length; ++index)
            {
                if (SearchPropositionOptions.IsDelimiter(self[index]))
                    return index;
            }
            return -1;
        }

        public static string ReplaceText(string searchText, string replacement, int cursorPos, out int insertTokenPos)
        {
            var replaceFrom = cursorPos - 1;
            while (replaceFrom >= 0 && !SearchPropositionOptions.IsDelimiter(searchText[replaceFrom]))
                replaceFrom--;
            if (replaceFrom == -1)
                replaceFrom = 0;
            else
                replaceFrom++;

            var activeProviders = SearchService.GetActiveProviders();
            foreach (var provider in activeProviders)
            {
                if (replaceFrom + provider.filterId.Length > searchText.Length || provider.filterId.Length == 1)
                    continue;

                var stringViewTest = new StringView(searchText, replaceFrom, replaceFrom + provider.filterId.Length);
                if (stringViewTest == provider.filterId)
                {
                    replaceFrom += provider.filterId.Length;
                    break;
                }
            }

            var replaceTo = IndexOfDelimiter(searchText, cursorPos);
            if (replaceTo == -1)
                replaceTo = searchText.Length;

            if (searchText.Substring(replaceFrom, replaceTo - replaceFrom).StartsWith(replacement, StringComparison.OrdinalIgnoreCase))
            {
                insertTokenPos = cursorPos;
                return searchText;
            }

            var sb = new StringBuilder(searchText);
            sb.Remove(replaceFrom, replaceTo - replaceFrom);
            sb.Insert(replaceFrom, replacement);

            var insertion = sb.ToString();
            insertTokenPos = insertion.LastIndexOf('\t');
            if (insertTokenPos != -1)
                insertion = insertion.Remove(insertTokenPos, 1);
            return insertion;
        }
    }
}
