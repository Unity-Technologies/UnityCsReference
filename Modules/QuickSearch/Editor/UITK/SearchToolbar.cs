// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchToolbar : UIElements.Toolbar, ISearchElement
    {
        enum FocusType
        {
            None,
            MoveEnd,
            SelectAll
        }

        internal class VisualMessage : VisualElement
        {
            string m_Text;
            Color m_Color;

            public int startIndex { get; private set; }
            public int endIndex { get; private set; }

            public VisualMessage(int startIndex, int endIndex, string text, in Color color, in Rect position)
            {
                this.startIndex = startIndex;
                this.endIndex = endIndex;
                m_Text = text;
                m_Color = color;

                style.position = Position.Absolute;
                UpdatePosition(position);

                this.generateVisualContent += GenerateVisualContent;
                this.tooltip = m_Text;

                RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            void OnDetachFromPanel(DetachFromPanelEvent evt)
            {
                this.generateVisualContent -= GenerateVisualContent;
            }

            void GenerateVisualContent(MeshGenerationContext mgc)
            {
                var halfPoint = localBound.height / 2;
                mgc.painter2D.BeginPath();
                mgc.painter2D.lineWidth = 2f;
                mgc.painter2D.strokeColor = m_Color;
                mgc.painter2D.MoveTo(new Vector2(0, halfPoint));
                mgc.painter2D.LineTo(new Vector2(localBound.width, halfPoint));
                mgc.painter2D.Stroke();
            }

            public void UpdatePosition(in Rect newPosition)
            {
                style.left = newPosition.xMin;
                style.width = newPosition.width;
                style.top = newPosition.yMin;
                style.height = newPosition.height;
            }
        }

        private static readonly string openSaveSearchesIconTooltip = L10n.Tr("Open Saved Searches Panel (F3)");

        public static readonly string queryBuilderIconTooltip = L10n.Tr("Toggle Query Builder Mode (F1)");

        public static readonly string previewInspectorButtonTooltip = L10n.Tr("Open Inspector (F4)");

        public static readonly string saveQueryButtonTooltip = L10n.Tr("Save search query as an asset.");

        public static readonly string pressToFilterTooltip = L10n.Tr("Press Tab \u21B9 to filter");

        protected readonly ISearchView m_ViewModel;
        protected readonly Toggle m_SearchQueryToggle;
        protected readonly Toggle m_QueryBuilderToggle;
        protected readonly Toggle m_InspectorToggle;
        protected readonly VisualElement m_SearchFieldContainer;
        protected INotifyValueChanged<string> m_SearchTextInput;
        private readonly UndoManager m_UndoManager;
        private readonly Label m_SearchPlaceholder;
        private readonly Label m_PressTabPlaceholder;
        private readonly ToolbarMenu s_SaveQueryDropdown;

        public virtual SearchContext context => m_ViewModel.context;
        public virtual SearchViewState viewState => m_ViewModel.state;

        public ToolbarSearchField searchField { get; private set; }
        internal QueryBuilder queryBuilder => (m_SearchTextInput as SearchQueryBuilderView)?.builder;

        List<VisualMessage> m_VisualMessages = new();
        IVisualElementScheduledItem m_UpdateQueryErrorScheduledItem;

        internal List<VisualMessage> visualMessages => m_VisualMessages;
        internal IVisualElementScheduledItem queryErrorsScheduleItem => m_UpdateQueryErrorScheduledItem;

        public TextElement textElement
        {
            get
            {
                if (textField?.Q<TextElement>() is not TextElement te)
                    return null;

                return te;
            }
        }

        internal TextField textField
        {
            get
            {
                if (viewState.queryBuilderEnabled && queryBuilder != null)
                    return queryBuilder?.textBlock?.GetSearchField()?.GetTextElement() as TextField;
                if (searchField?.Q<TextField>() is not TextField tf)
                    return null;
                return tf;
            }
        }

        TextSelectingUtilities selectingUtilities => textElement?.selectingManipulator?.m_SelectingUtilities;

        public bool supportsMultiline => !viewState.queryBuilderEnabled;

        public new static readonly string ussClassName = "search-toolbar";
        public static readonly string buttonClassName = ussClassName.WithUssElement("button");
        public static readonly string dropdownClassName = ussClassName.WithUssElement("dropdown");
        public static readonly string savedSearchesButtonClassName = ussClassName.WithUssElement("saved-searches-button");
        public static readonly string queryBuilderButtonClassName = ussClassName.WithUssElement("query-builder-button");
        public static readonly string inspectorButtonClassName = ussClassName.WithUssElement("inspector-button");
        public static readonly string saveQueryButtonClassName = ussClassName.WithUssElement("save-query-button");

        public static readonly string searchFieldClassName = "search-field";
        public static readonly string searchFieldPlaceholderClassName = "search-field-placeholder";
        public static readonly string searchFieldMultilineClassName = "search-field-multiline";

        public SearchToolbar(string name, ISearchView viewModel)
        {
            this.name = name;
            m_ViewModel = viewModel;

            AddToClassList(ussClassName);

            var window = viewModel as SearchWindow;

            // Search query panel toggle
            if (window?.IsSavedSearchQueryEnabled() ?? false)
            {
                m_SearchQueryToggle = SearchElement.CreateToolbarToggle("SearchQueryPanelToggle", openSaveSearchesIconTooltip, viewState.flags.HasAny(SearchViewFlags.OpenLeftSidePanel), OnToggleSearchQueryPanel, buttonClassName, savedSearchesButtonClassName);
                Add(m_SearchQueryToggle);
            }

            // Query builder toggle
            if (!viewState.flags.HasAny(SearchViewFlags.DisableBuilderModeToggle))
            {
                m_QueryBuilderToggle = SearchElement.CreateToolbarToggle("SearchQueryBuilderToggle", queryBuilderIconTooltip, viewState.queryBuilderEnabled, OnToggleQueryBuilder, buttonClassName, queryBuilderButtonClassName);
                Add(m_QueryBuilderToggle);
            }

            m_SearchPlaceholder = new Label($"Search {viewState.title}");
            m_SearchPlaceholder.AddToClassList(searchFieldPlaceholderClassName);
            m_SearchPlaceholder.style.paddingLeft = 4f;
            m_SearchPlaceholder.focusable = false;
            m_SearchPlaceholder.pickingMode = PickingMode.Ignore;

            m_PressTabPlaceholder = new Label(pressToFilterTooltip);
            m_PressTabPlaceholder.AddToClassList(searchFieldPlaceholderClassName);
            m_PressTabPlaceholder.style.paddingBottom = 3f;
            m_PressTabPlaceholder.focusable = false;
            m_PressTabPlaceholder.pickingMode = PickingMode.Ignore;

            // Search field
            m_SearchFieldContainer = new VisualElement();
            CreateSearchField(m_SearchFieldContainer);
            Add(m_SearchFieldContainer);

            // Save query dropdown
            if (window?.IsSavedSearchQueryEnabled() ?? false)
            {
                s_SaveQueryDropdown = SearchElement.Create<ToolbarMenu, MouseUpEvent>("SearchSaveQueryMenu", OnSaveQueryDropdown, buttonClassName, dropdownClassName, saveQueryButtonClassName);
                Add(s_SaveQueryDropdown);
                UpdateSaveQueryButton();
            }

            // Details view panel toggle.
            if (viewState.flags.HasNone(SearchViewFlags.DisableInspectorPreview))
            {
                m_InspectorToggle = SearchElement.CreateToolbarToggle("SearchInspectorToggle", previewInspectorButtonTooltip, viewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview), OnToggleInspector, buttonClassName, inspectorButtonClassName);
                Add(m_InspectorToggle);
            }

            m_UndoManager = new UndoManager(context.searchText);
            Utils.CallDelayed(SaveUndoManager, 2.0d);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void CreateSearchField(VisualElement searchFieldContainer)
        {
            m_SearchTextInput = null;
            m_VisualMessages.Clear();
            searchFieldContainer.Clear();
            searchFieldContainer.style.flexDirection = FlexDirection.Row;
            searchFieldContainer.style.flexGrow = 1f;

            ClearOnCursorChangedHandlers();

            searchField = new ToolbarSearchField() { name = "SearchField" };
            searchField.AddToClassList(searchFieldClassName);
            searchField.style.flexGrow = 1f;
            searchField.Children().First().style.display = DisplayStyle.None;
            textField.tripleClickSelectsLine = true;
            textField.selectAllOnFocus = false;

            if (viewState.queryBuilderEnabled)
            {
                var queryBuilderView = new SearchQueryBuilderView("SearchQueryBuilder", m_ViewModel, searchField);
                queryBuilderView.RegisterCallback<ChangeEvent<string>>(OnQueryChanged);
                searchField.Insert(1, queryBuilderView);
                m_SearchTextInput = queryBuilderView;
            }
            else
            {
                searchField.value = context.searchText;
                searchField.Add(m_SearchPlaceholder);
                searchField.Add(m_PressTabPlaceholder);
                searchField.RegisterCallback<ChangeEvent<string>>(OnQueryChanged);
                searchField.RegisterCallback<GeometryChangedEvent>(evt => UpdatePlaceholders());

                m_SearchTextInput = searchField;
            }

            searchField.RegisterCallback<AttachToPanelEvent>(OnSearchFieldAttachToPanel);
            searchField.RegisterCallback<DetachFromPanelEvent>(OnSearchFieldDetachFromPanel);
            searchFieldContainer.Add(searchField);

            if (selectingUtilities != null)
            {
                selectingUtilities.OnCursorIndexChange += OnCursorChanged;
                selectingUtilities.OnSelectIndexChange += OnCursorChanged;
            }
        }

        public void FocusSearchField()
        {
            FocusSearchField(FocusType.SelectAll);
        }

        void FocusSearchField(FocusType focusType)
        {
            if (!(textElement?.GetSearchHostWindow()?.HasFocus() ?? false))
                return;

            if (textElement != null && textElement.selection != null)
            {
                textElement.Focus();
                Utils.CallDelayed(() => // Call delayed is needed because TextSelectingUtilities.OnFocus is overriding the cursor setting
                {
                    Dispatcher.Emit(SearchEvent.SearchFieldFocused, new SearchEventPayload(this));

                    // the text element might have been detached by the time the delayed callback fires
                    if (textElement?.selection == null)
                        return;

                    if (focusType != FocusType.None && m_ViewModel.IsPicker())
                        focusType = FocusType.MoveEnd;

                    switch (focusType)
                    {
                        case FocusType.None: break;
                        case FocusType.MoveEnd:
                            textElement.selection.MoveTextEnd();
                            break;
                        case FocusType.SelectAll:
                            textElement.selection.SelectAll();
                            break;
                    }
                });
            }
            else
                Dispatcher.Emit(SearchEvent.SearchFieldFocused, new SearchEventPayload(this));
        }

        private void UpdatePlaceholders()
        {
            m_SearchPlaceholder.style.display = !context.empty ? DisplayStyle.None : DisplayStyle.Flex;
            if (!context.empty)
                m_PressTabPlaceholder.style.right = 12f;
            else
                m_PressTabPlaceholder.style.right = 2f;

            var te = searchField.Q<TextElement>();
            if (te != null)
            {
                var textSize = te.MeasureTextSize(searchField.value, resolvedStyle.width, MeasureMode.AtMost, resolvedStyle.height, MeasureMode.Exactly);
                if ((textSize.x + te.worldBound.x) >= (m_PressTabPlaceholder.worldBound.xMin - 60f))
                    m_PressTabPlaceholder.style.visibility = Visibility.Hidden;
                else
                    m_PressTabPlaceholder.style.visibility = Visibility.Visible;
            }
            else
                m_PressTabPlaceholder.style.display = DisplayStyle.None;
        }

        private void OnToggleQueryBuilder(ChangeEvent<bool> evt)
        {
            if (m_ViewModel is SearchWindow window)
                window.ToggleQueryBuilder();
            CreateSearchField(m_SearchFieldContainer);
            FocusSearchField(FocusType.MoveEnd);
        }

        private void OnToggleSearchQueryPanel(ChangeEvent<bool> evt)
        {
            if (m_ViewModel is SearchWindow window)
                window.TogglePanelView(SearchViewFlags.OpenLeftSidePanel);
        }

        private void OnToggleInspector(ChangeEvent<bool> evt)
        {
            if (m_ViewModel is SearchWindow window)
                window.TogglePanelView(SearchViewFlags.OpenInspectorPreview);
        }

        private void OnQueryChanged(ChangeEvent<string> evt)
        {
            if (evt.target != m_SearchTextInput)
                return;

            m_ViewModel.SetSearchText(evt.newValue, TextCursorPlacement.None);
            m_ViewModel.SetSelection();
            UpdatePlaceholders();
            UpdateQueryErrors();
            UpdateMultiline();
        }

        private void OnSaveQuery(SearchWindow window)
        {
            var saveQueryMenu = new GenericMenu();
            if (m_ViewModel.state.activeQuery != null)
            {
                saveQueryMenu.AddItem(new GUIContent($"Save {m_ViewModel.state.activeQuery.displayName}"), false, window.SaveActiveSearchQuery);
                saveQueryMenu.AddSeparator("");
            }

            AddSaveQueryMenuItems(window, saveQueryMenu);
            saveQueryMenu.ShowAsContext();
        }

        private void OnSaveQueryDropdown(MouseUpEvent evt)
        {
            if (m_ViewModel is SearchWindow window)
                OnSaveQuery(window);
        }

        private void AddSaveQueryMenuItems(ISearchQueryView window, GenericMenu saveQueryMenu)
        {
            saveQueryMenu.AddItem(new GUIContent("Save User"), false, window.SaveUserSearchQuery);
            saveQueryMenu.AddItem(new GUIContent("Save Project..."), false, window.SaveProjectSearchQuery);
            if (!string.IsNullOrEmpty(context.searchText))
            {
                saveQueryMenu.AddSeparator("");
                saveQueryMenu.AddItem(new GUIContent("Clipboard"), false, () => SaveQueryToClipboard(context.searchText));
            }
        }

        private void SaveQueryToClipboard(in string query)
        {
            var trimmedQuery = Utils.TrimText(query);
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, trimmedQuery);
            EditorGUIUtility.systemCopyBuffer = Utils.TrimText(trimmedQuery);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Dispatcher.On(SearchEvent.ViewStateUpdated, OnViewFlagsUpdated);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Dispatcher.Off(SearchEvent.ViewStateUpdated, OnViewFlagsUpdated);
        }

        private void OnSearchFieldAttachToPanel(AttachToPanelEvent evt)
        {
            FocusSearchField();
            searchField.RegisterCallback<GeometryChangedEvent>(OnSearchFieldReady);
        }

        private void OnSearchFieldDetachFromPanel(DetachFromPanelEvent evt)
        {
            Dispatcher.Off(SearchEvent.SearchTextChanged, OnSearchTextChanged);
            Dispatcher.Off(SearchEvent.SearchContextChanged, OnSearchContextChanged);
            Dispatcher.Off(SearchEvent.RefreshBuilder, OnQueryBuilderStateChanged);
            Dispatcher.Off(SearchEvent.RefreshContent, OnRefreshContent);

            UnregisterCallback<KeyDownEvent>(OnSearchToolbarKeyDown);
            searchField.UnregisterCallback<KeyDownEvent>(OnSearchFieldKeyDown);

            viewState.globalEventManager.UnregisterGlobalEventHandler<KeyDownEvent>(OnGlobalKeyInput);

            ClearOnCursorChangedHandlers();
        }

        void OnSearchFieldReady(GeometryChangedEvent evt)
        {
            searchField.UnregisterCallback<GeometryChangedEvent>(OnSearchFieldReady);

            Dispatcher.On(SearchEvent.SearchTextChanged, OnSearchTextChanged);
            Dispatcher.On(SearchEvent.SearchContextChanged, OnSearchContextChanged);
            Dispatcher.On(SearchEvent.RefreshBuilder, OnQueryBuilderStateChanged);
            Dispatcher.On(SearchEvent.RefreshContent, OnRefreshContent);
            RegisterCallback<KeyDownEvent>(OnSearchToolbarKeyDown);
            searchField.RegisterCallback<KeyDownEvent>(OnSearchFieldKeyDown);

            viewState.globalEventManager.RegisterGlobalEventHandler<KeyDownEvent>(OnGlobalKeyInput, int.MaxValue);

            UpdateQueryErrors();
            UpdateMultiline();
        }

        private static bool IgnoreKey(IKeyboardEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape || evt.keyCode == KeyCode.Tab || evt.keyCode == KeyCode.Return || evt.shiftKey || evt.ctrlKey)
                return true;

            if ((evt.modifiers & EventModifiers.FunctionKey) != 0 && (int)evt.keyCode >= 255)
                return true;

            return false;
        }

        private static bool IsLineBreak(IKeyboardEvent evt)
        {
            if ((evt.ctrlKey || evt.shiftKey) && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
                return true;
            return false;
        }

        private bool OnGlobalKeyInput(KeyDownEvent evt)
        {
            if (Contains((VisualElement)evt.target))
                return false;

            if (IgnoreKey(evt))
                return false;

            var te = textElement;
            if (te == null || te.editingManipulator == null)
                return false;

            if (te is not Focusable tef)
                return false;

            te.selection.selectAllOnFocus = false;
            te.focusController.DoFocusChange(tef);
            te.editingManipulator.ExecuteDefaultActionAtTarget(evt);
            return evt.isPropagationStopped;
        }

        private void OnSearchToolbarKeyDown(KeyDownEvent evt)
        {
            if (supportsMultiline && IsLineBreak(evt))
            {
                var currentCursor = textField.cursorIndex;
                searchField.value = searchField.value.Insert(currentCursor, "\n");
                UpdateInternalTextData();
                selectingUtilities.MoveRight();
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        private void OnSearchFieldKeyDown(KeyDownEvent evt)
        {
            if (m_UndoManager.HandleEvent(evt, out var undoText, out var cursorPos, out _))
            {
                UpdateSearchText(undoText, cursorPos);
                evt.StopImmediatePropagation();
                evt.PreventDefault();
            }
        }

        private void UpdateSearchText(string undoText, int cursorPos)
        {
            m_ViewModel.SetSearchText(undoText, TextCursorPlacement.Default, cursorPos);
        }

        private void OnSearchTextChanged(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;

            var moveCursor = evt.GetArgument<TextCursorPlacement>(2);
            var cursorInsertIndex = evt.GetArgument<int>(3);
            if (evt.GetArgument<TextCursorPlacement>(2) != TextCursorPlacement.None)
            {
                m_SearchTextInput?.SetValueWithoutNotify(evt.GetArgument<string>(1));
                MoveCursor(moveCursor, cursorInsertIndex);

                UpdatePlaceholders();
                UpdateQueryErrors();
                UpdateMultiline();
            }

            UpdateSaveQueryButton();
        }

        private void OnSearchContextChanged(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;

            OnViewFlagsUpdated(evt);

            CreateSearchField(m_SearchFieldContainer);
            UpdatePlaceholders();
            FocusSearchField();
            UpdateSaveQueryButton();
        }

        private void OnQueryBuilderStateChanged(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;

            CreateSearchField(m_SearchFieldContainer);
            FocusSearchField(FocusType.MoveEnd);
        }

        void OnRefreshContent(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;

            UpdateQueryErrors();
        }

        private void OnViewFlagsUpdated(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;

            m_QueryBuilderToggle?.SetValueWithoutNotify(evt.sourceViewState.queryBuilderEnabled);
            m_SearchQueryToggle?.SetValueWithoutNotify(evt.sourceViewState.flags.HasAny(SearchViewFlags.OpenLeftSidePanel));
            m_InspectorToggle?.SetValueWithoutNotify(evt.sourceViewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview));
        }

        private void SaveUndoManager()
        {
            if (m_ViewModel is not SearchWindow)
                return;

            var time = EditorApplication.timeSinceStartup;
            m_UndoManager.Save(time, context.searchText, textField);
            Utils.CallDelayed(SaveUndoManager, 2.0d);
        }

        public void MoveCursor(TextCursorPlacement moveCursor, int cursorInsertPosition)
        {
            if (cursorInsertPosition >= 0)
            {
                textField.selectIndex = textField.cursorIndex = cursorInsertPosition;
            }
            else
            {
                if (textField.textSelection is not TextElement te)
                    return;

                if (selectingUtilities == null)
                    return;

                UpdateInternalTextData();

                switch (moveCursor)
                {
                    case TextCursorPlacement.MoveLineEnd: selectingUtilities.MoveLineEnd(); break;
                    case TextCursorPlacement.MoveLineStart: selectingUtilities.MoveLineStart(); break;
                    case TextCursorPlacement.MoveToEndOfPreviousWord: selectingUtilities.MoveToEndOfPreviousWord(); break;
                    case TextCursorPlacement.MoveToStartOfNextWord: selectingUtilities.MoveToStartOfNextWord(); break;
                    case TextCursorPlacement.MoveWordLeft: selectingUtilities.MoveWordLeft(); break;
                    case TextCursorPlacement.MoveWordRight: selectingUtilities.MoveWordRight(); break;
                    case TextCursorPlacement.MoveAutoComplete: MoveAutoComplete(textField, selectingUtilities); break;
                }
            }
        }

        static void MoveAutoComplete(TextField te, TextSelectingUtilities selectingUtilities)
        {
            while (te.cursorIndex < te.text.Length && !char.IsWhiteSpace(te.text[te.cursorIndex]))
                selectingUtilities.MoveRight();

            // If there is a space at the end of the text, move through it.
            if (te.cursorIndex == te.text.Length - 1 && char.IsWhiteSpace(te.text[te.cursorIndex]))
                selectingUtilities.MoveRight();
        }

        void UpdateQueryErrors()
        {
            m_UpdateQueryErrorScheduledItem?.Pause();

            foreach (var visualMessage in m_VisualMessages)
            {
                visualMessage.RemoveFromHierarchy();
            }
            m_VisualMessages.Clear();

            m_UpdateQueryErrorScheduledItem = this.schedule.Execute(DrawQueryErrors).Every(100).StartingIn(100);
        }

        // Only draw errors when you are done typing, to prevent cases where
        // the cursor moved because of changes but we did not clear the errors yet.
        private void DrawQueryErrors()
        {
            if (context.searchInProgress)
                return;

            if (m_UpdateQueryErrorScheduledItem != null && m_UpdateQueryErrorScheduledItem.isActive)
                m_UpdateQueryErrorScheduledItem.Pause();

            if (!context.options.HasAny(SearchFlags.ShowErrorsWithResults) && m_ViewModel.results.Count > 0)
                return;

            List<SearchQueryError> errors;
            if (m_ViewModel.currentGroup == (m_ViewModel as IGroup)?.id)
                errors = m_ViewModel.GetAllVisibleErrors().ToList();
            else
                errors = context.GetErrorsByProvider(m_ViewModel.currentGroup).ToList();

            if (errors.Count == 0 || context.markers?.Length > 0)
                return;

            errors.Sort(SearchQueryError.Compare);
            DrawQueryErrors(errors);
        }

        private void DrawQueryErrors(IEnumerable<SearchQueryError> errors)
        {
            var alreadyShownErrors = new List<SearchQueryError>();
            UpdateInternalTextData();
            foreach (var searchQueryError in errors)
            {
                var queryErrorStart = searchQueryError.index;

                // Do not stack errors on top of each other
                if (alreadyShownErrors.Any(e => e.Overlaps(searchQueryError)))
                    continue;

                alreadyShownErrors.Add(searchQueryError);

                if (searchQueryError.type == SearchQueryErrorType.Error)
                {
                    DrawError(
                        queryErrorStart,
                        searchQueryError.length,
                        searchQueryError.reason);
                }
                else
                {
                    DrawWarning(
                        queryErrorStart,
                        searchQueryError.length,
                        searchQueryError.reason);
                }
            }
        }

        void DrawError(int startIndex, int length, in string reason)
        {
            DrawMessage(startIndex, length, reason, Color.red);
        }

        void DrawWarning(int startIndex, int length, in string reason)
        {
            DrawMessage(startIndex, length, reason, Color.yellow);
        }

        void DrawMessage(int startIndex, int length, in string message, in Color color)
        {
            var position = GetVisualMessagePosition(startIndex, startIndex + length);
            var msgElement = new VisualMessage(startIndex, startIndex + length, message, color, position);
            m_VisualMessages.Add(msgElement);

            textField.Add(msgElement);
            msgElement.MarkDirtyRepaint();

            if (!CanShowVisualMessage(msgElement, textField))
                msgElement.visible = false;
        }

        bool CanShowVisualMessage(VisualMessage visualMessage, TextField te)
        {
            // Do not show error if the cursor is inside the error itself, or if the error intersect with
            // the current token
            if (te.cursorIndex >= visualMessage.startIndex && te.cursorIndex <= visualMessage.endIndex)
                return false;
            SearchPropositionOptions.GetTokenBoundariesAtCursorPosition(context.searchText, te.cursorIndex, out var tokenStartPos, out var tokenEndPos);
            if (visualMessage.startIndex >= tokenStartPos && visualMessage.startIndex <= tokenEndPos)
                return false;
            if (visualMessage.endIndex >= tokenStartPos && visualMessage.endIndex <= tokenEndPos)
                return false;
            return true;
        }

        Rect GetVisualMessagePosition(int startIndex, int endIndex)
        {
            var diff = textElement.worldBound.position - textField.worldBound.position;
            var start = textElement.uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(startIndex);
            var end = textElement.uitkTextHandle.GetCursorPositionFromStringIndexUsingLineHeight(endIndex);

            start += diff;
            end += diff;

            return new Rect(start.x, start.y - 5f, end.x - start.x, 10f);
        }

        Rect GetVisualMessagePosition(VisualMessage visualMessage)
        {
            return GetVisualMessagePosition(visualMessage.startIndex, visualMessage.endIndex);
        }

        void OnCursorChanged()
        {
            foreach (var visualMessage in m_VisualMessages)
            {
                visualMessage.visible = CanShowVisualMessage(visualMessage, textField);
                visualMessage.UpdatePosition(GetVisualMessagePosition(visualMessage));
            }
        }

        void ClearOnCursorChangedHandlers()
        {
            if (selectingUtilities != null)
            {
                selectingUtilities.OnCursorIndexChange -= OnCursorChanged;
                selectingUtilities.OnSelectIndexChange -= OnCursorChanged;
            }
        }

        void UpdateMultiline()
        {
            if (textField == null)
                return;

            var hasLineBreak = supportsMultiline && searchField.value.Contains('\n') || searchField.value.Contains('\r') || searchField.value.Contains("\r\n");
            textField.multiline = hasLineBreak;
            searchField.EnableInClassList(searchFieldMultilineClassName, hasLineBreak);
        }

        internal void UpdateInternalTextData()
        {
            textElement.uitkTextHandle.Update();
        }

        void UpdateSaveQueryButton()
        {
            if (s_SaveQueryDropdown == null)
                return;

            s_SaveQueryDropdown.SetEnabled(!context.empty);
        }
    }
}
