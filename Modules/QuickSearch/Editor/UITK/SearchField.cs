// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    [Flags]
    enum SearchQueryBuilderViewFlags
    {
        None = 0,
        UseSearchGlobalEventHandler = 1 << 0,
        BlocksSupportExclude = 1 << 1,
        Default = UseSearchGlobalEventHandler | BlocksSupportExclude
    }

    class SearchFieldElement : SearchElement
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

        public static readonly string pressToFilterTooltip = L10n.Tr("Press Tab \u21B9 to filter");

        protected INotifyValueChanged<string> m_SearchTextInput;
        private readonly UndoManager m_UndoManager;
        private readonly Label m_SearchPlaceholder;
        private readonly Label m_PressTabPlaceholder;
        private readonly SearchQueryBuilderViewFlags m_BuilderViewFlags;
        private Button m_CancelButton;

        const float k_PlaceholdersTouchThreshold = 2f;

        public ToolbarSearchField searchField { get; private set; }
        internal QueryBuilder queryBuilder => (m_SearchTextInput as SearchQueryBuilderView)?.builder;
        internal string queryString => viewState.queryBuilderEnabled ? queryBuilder.searchText : searchField.value;
        internal Texture2D addNewBlockIcon { get; set; }

        List<VisualMessage> m_VisualMessages = new();
        IVisualElementScheduledItem m_UpdateQueryErrorScheduledItem;

        internal List<VisualMessage> visualMessages => m_VisualMessages;
        internal IVisualElementScheduledItem queryErrorsScheduleItem => m_UpdateQueryErrorScheduledItem;
        internal INotifyValueChanged<string> searchTextInput => m_SearchTextInput;

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

        public void SetValueWithoutNotify(string newValue)
        {
            searchTextInput.SetValueWithoutNotify(newValue);
            UpdateCancelButton();
            UpdatePlaceholders();
            UpdateQueryErrors();
            UpdateMultiline();
        }

        public bool supportsMultiline => !viewState.queryBuilderEnabled;

        public static readonly string searchFieldClassName = "search-field";
        public static readonly string searchFieldPlaceholderClassName = "search-field-placeholder";
        public static readonly string searchFieldMultilineClassName = "search-field-multiline";

        public SearchFieldElement(string name, ISearchView viewModel, SearchQueryBuilderViewFlags builderViewFlags)
            : base(name, viewModel)
        {
            this.name = name;
            m_BuilderViewFlags = builderViewFlags;

            m_SearchPlaceholder = new Label($"Search {viewState.title}")
            {
                style = { unityTextGenerator = TextGeneratorType.Standard }
            };
            m_SearchPlaceholder.AddToClassList(searchFieldPlaceholderClassName);
            m_SearchPlaceholder.style.paddingLeft = 4f;
            m_SearchPlaceholder.focusable = false;
            m_SearchPlaceholder.pickingMode = PickingMode.Ignore;

            m_PressTabPlaceholder = new Label(pressToFilterTooltip)
            {
                style = { unityTextGenerator = TextGeneratorType.Standard }
            };
            m_PressTabPlaceholder.AddToClassList(searchFieldPlaceholderClassName);
            m_PressTabPlaceholder.style.paddingBottom = 3f;
            m_PressTabPlaceholder.focusable = false;
            m_PressTabPlaceholder.pickingMode = PickingMode.Ignore;
            if (!context.empty)
                m_PressTabPlaceholder.style.right = 12f;
            else
                m_PressTabPlaceholder.style.right = 2f;

            // Search field
            CreateSearchField(this);

            m_UndoManager = new UndoManager(context.searchText);
            Utils.CallDelayed(SaveUndoManager, 2.0d);

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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            searchField.Children().First().style.display = DisplayStyle.None;
#pragma warning restore UA2001
            textField.tripleClickSelectsLine = true;
            textField.selectAllOnFocus = false;

            // The advanced text generator (ATG) requires elements to be in a panel for text
            // measurement. Set the standard generator directly on the TextElement to
            // avoid errors when text is measured before panel attachment. Inline styles update computedStyle immediately even
            // off-panel, but style inheritance does not cascade without a panel.
            var searchTextElement = searchField.Q<TextElement>();
            if (searchTextElement != null)
                searchTextElement.style.unityTextGenerator = TextGeneratorType.Standard;

            m_CancelButton = new Button(() => { }) { name = "query-builder-unity-cancel" };
            m_CancelButton.AddToClassList(SearchFieldBase<TextField, string>.cancelButtonUssClassName);
            m_CancelButton.AddToClassList(SearchFieldBase<TextField, string>.cancelButtonOffVariantUssClassName);

            m_CancelButton.clickable.clicked += OnCancelButtonClick;

            if (viewState.queryBuilderEnabled)
            {
                var queryBuilderView = new SearchQueryBuilderView("SearchQueryBuilder", m_ViewModel, searchField, m_BuilderViewFlags);
                searchField.Insert(1, queryBuilderView);
                searchField.RegisterCallback<ChangeEvent<string>>(OnQueryChanged);
                m_SearchTextInput = queryBuilderView;
            }
            else
            {
                searchField.value = context.searchText;
                searchField.Add(m_SearchPlaceholder);
                searchField.Add(m_PressTabPlaceholder);
                searchField.RegisterCallback<ChangeEvent<string>>(OnQueryChanged);
                searchField.RegisterCallback<GeometryChangedEvent>(evt => UpdatePlaceholders());

                m_SearchTextInput = textField;
            }

            searchField.RegisterCallback<AttachToPanelEvent>(OnSearchFieldAttachToPanel);
            searchField.RegisterCallback<DetachFromPanelEvent>(OnSearchFieldDetachFromPanel);
            searchFieldContainer.Add(searchField);

            if (textElement != null && textElement.isSelectable)
            {
                textElement.selection.OnCursorIndexChange += OnCursorChanged;
                textElement.selection.OnSelectIndexChange += OnCursorChanged;
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
            m_SearchPlaceholder.style.visibility = !context.empty ? Visibility.Hidden : Visibility.Visible;
            if (m_SearchPlaceholder.worldBound.xMax + k_PlaceholdersTouchThreshold >= searchField.worldBound.xMax)
                m_SearchPlaceholder.style.visibility = Visibility.Hidden;

            var oldRight = m_PressTabPlaceholder.style.right.value.value;
            if (!context.empty)
                m_PressTabPlaceholder.style.right = 12f;
            else
                m_PressTabPlaceholder.style.right = 2f;
            var offset = m_PressTabPlaceholder.style.right.value.value - oldRight;

            var te = searchField.Q<TextElement>();
            if (te != null)
            {
                var textSize = te.MeasureTextSize(searchField.value, resolvedStyle.width, MeasureMode.AtMost, resolvedStyle.height, MeasureMode.Exactly);
                if ((textSize.x + te.worldBound.x) >= (m_PressTabPlaceholder.worldBound.xMin - 60f))
                    m_PressTabPlaceholder.style.visibility = Visibility.Hidden;
                else
                    m_PressTabPlaceholder.style.visibility = Visibility.Visible;

                if (m_PressTabPlaceholder.style.visibility == Visibility.Visible && (m_PressTabPlaceholder.worldBound.xMin - offset) <= m_SearchPlaceholder.worldBound.xMax + k_PlaceholdersTouchThreshold)
                    m_PressTabPlaceholder.style.visibility = Visibility.Hidden;
            }
            else
                m_PressTabPlaceholder.style.display = DisplayStyle.None;
        }

        private void OnQueryChanged(ChangeEvent<string> evt)
        {
            UpdateCancelButton();
            if (m_SearchTextInput != evt.target)
                return;

            m_ViewModel.SetSearchText(evt.newValue, TextCursorPlacement.None);
            m_ViewModel.SetSelection();
            UpdatePlaceholders();
            UpdateQueryErrors();
            UpdateMultiline();
        }

        private void OnSearchFieldAttachToPanel(AttachToPanelEvent evt)
        {
            FocusSearchField();
            searchField.RegisterCallback<GeometryChangedEvent>(OnSearchFieldReady);

            var cancelButton = searchField.Q<Button>(null, SearchFieldBase<TextField, string>.cancelButtonUssClassName);
            if (cancelButton != null)
                HideElements(cancelButton);
            searchField.Add(m_CancelButton);
        }

        private void OnSearchFieldDetachFromPanel(DetachFromPanelEvent evt)
        {
            Dispatcher.Off(SearchEvent.SearchTextChanged, OnSearchTextChanged);
            Dispatcher.Off(SearchEvent.SearchContextChanged, OnSearchContextChanged);
            Dispatcher.Off(SearchEvent.RefreshBuilder, OnQueryBuilderStateChanged);
            Dispatcher.Off(SearchEvent.RefreshContent, OnRefreshContent);

            UnregisterCallback<KeyDownEvent>(OnSearchToolbarKeyDown);
            searchField.UnregisterCallback<KeyDownEvent>(OnSearchFieldKeyDown);

            if (m_BuilderViewFlags.HasFlag(SearchQueryBuilderViewFlags.UseSearchGlobalEventHandler))
                viewState.globalEventManager.UnregisterGlobalEventHandler<KeyDownEvent>(OnGlobalKeyInput);

            ClearOnCursorChangedHandlers();

            searchField.Remove(m_CancelButton);
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

            if (m_BuilderViewFlags.HasFlag(SearchQueryBuilderViewFlags.UseSearchGlobalEventHandler))
                viewState.globalEventManager.RegisterGlobalEventHandler<KeyDownEvent>(OnGlobalKeyInput, int.MaxValue);

            UpdateQueryErrors();
            UpdateMultiline();
            UpdateCancelButton();
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
            te.editingManipulator.HandleEventBubbleUp(evt);
            return evt.isPropagationStopped;
        }

        private void OnSearchToolbarKeyDown(KeyDownEvent evt)
        {
            if (supportsMultiline && IsLineBreak(evt))
            {
                var currentCursor = textField.cursorIndex;
                searchField.value = searchField.value.Insert(currentCursor, "\n");
                textElement.selection.MoveForward();
                evt.StopPropagation();
            }
        }

        private void OnSearchFieldKeyDown(KeyDownEvent evt)
        {
            if (m_UndoManager.HandleEvent(evt, out var undoText, out var cursorPos, out _))
            {
                UpdateSearchText(undoText, cursorPos);
                evt.StopImmediatePropagation();
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
            UpdateCancelButton();
        }

        private void OnSearchContextChanged(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;

            CreateSearchField(this);
            UpdatePlaceholders();
            FocusSearchField();
            UpdateCancelButton();
        }

        private void OnQueryBuilderStateChanged(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;

            ToggleQueryBuilder();
        }

        public void ToggleQueryBuilder()
        {
            CreateSearchField(this);
            FocusSearchField(FocusType.MoveEnd);
        }

        void OnRefreshContent(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;

            UpdateQueryErrors();
        }

        private void SaveUndoManager()
        {
            if (m_ViewModel is not ISearchWindow)
                return;

            var time = EditorApplication.timeSinceStartup;
            m_UndoManager.Save(time, context.searchText, textField);
            Utils.CallDelayed(SaveUndoManager, 2.0d);
        }

        public void MoveCursor(TextCursorPlacement moveCursor, int cursorInsertPosition)
        {
            if (textField == null)
                return;
            if (cursorInsertPosition >= 0)
            {
                textField.selectIndex = textField.cursorIndex = cursorInsertPosition;
            }
            else
            {
                if (textField.textSelection is not TextElement te)
                    return;

                switch (moveCursor)
                {
                    case TextCursorPlacement.MoveLineEnd: textElement.selection.MoveToParagraphEnd(); break;
                    case TextCursorPlacement.MoveLineStart: textElement.selection.MoveToParagraphStart(); break;
                    case TextCursorPlacement.MoveToEndOfPreviousWord: textElement.selection.MoveToEndOfPreviousWord(); break;
                    case TextCursorPlacement.MoveToStartOfNextWord: textElement.selection.MoveToStartOfNextWord(); break;
                    case TextCursorPlacement.MoveWordLeft: textElement.selection.MoveWordBackward(); break;
                    case TextCursorPlacement.MoveWordRight: textElement.selection.MoveWordForward(); break;
                    case TextCursorPlacement.MoveAutoComplete: MoveAutoComplete(textField, textElement.selection); break;
                }
            }
        }

        static void MoveAutoComplete(TextField te, ITextSelection selection)
        {
            while (te.cursorIndex < te.text.Length && !char.IsWhiteSpace(te.text[te.cursorIndex]))
                selection.MoveForward();

            // If there is a space at the end of the text, move through it.
            if (te.cursorIndex == te.text.Length - 1 && char.IsWhiteSpace(te.text[te.cursorIndex]))
                selection.MoveForward();
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

            if (m_ViewModel.results == null || (!context.options.HasAny(SearchFlags.ShowErrorsWithResults) && m_ViewModel.results.Count > 0))
                return;

            List<SearchQueryError> errors;
            if (m_ViewModel.currentGroup == (m_ViewModel as IGroup)?.id)
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                errors = m_ViewModel.GetAllVisibleErrors().ToList();
#pragma warning restore UA2001
            else
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                errors = context.GetErrorsByProvider(m_ViewModel.currentGroup).ToList();
#pragma warning restore UA2001

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
                if (alreadyShownErrors.Exists(e => e.Overlaps(searchQueryError)))
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
            var start = textElement.selection.GetCursorPositionFromStringIndex(startIndex);
            var end = textElement.selection.GetCursorPositionFromStringIndex(endIndex);

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
            if (textElement != null && textElement.isSelectable)
            {
                textElement.selection.OnCursorIndexChange -= OnCursorChanged;
                textElement.selection.OnSelectIndexChange -= OnCursorChanged;
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

        void OnCancelButtonClick()
        {
            var cancelValue = viewState.isPicker ? viewState.initialQuery : string.Empty;
            m_SearchTextInput.value = cancelValue;
            UpdateCancelButton();
        }

        void UpdateCancelButton()
        {
            if (viewState.queryBuilderEnabled && queryBuilder == null)
                return;
            // Override the behavior of the ToolbarSearchField that only removes the X if the field is empty.
            var hideCancelBtn = string.IsNullOrEmpty(queryString);
            if (m_ViewModel.IsPicker())
            {
                var simplifiedQueryString = queryString;
                var simplifiedInitialQuery = viewState.initialQuery;
                if (viewState.queryBuilderEnabled)
                {
                    simplifiedQueryString = Utils.Simplify(queryString).ToLower();
                    simplifiedInitialQuery = Utils.Simplify(viewState.initialQuery.Trim()).ToLower();
                }
                hideCancelBtn = string.CompareOrdinal(simplifiedQueryString, simplifiedInitialQuery) == 0;
            }
            m_CancelButton.EnableInClassList(SearchFieldBase<TextField, string>.cancelButtonOffVariantUssClassName, hideCancelBtn);
            m_CancelButton.style.display = hideCancelBtn ? DisplayStyle.None : DisplayStyle.Flex;
        }

        internal void UpdateInternalTextData()
        {
            textElement.uitkTextHandle.ComputeSettingsAndUpdate();
        }
    }
}
