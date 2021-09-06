// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityEditor.Search
{
    static class AutoComplete
    {
        private static string s_LastInput;
        private static int s_CurrentSelection = 0;
        private static List<SearchProposition> s_FilteredList = null;
        private static List<float> s_ItemLabelWidths = null;
        private static List<float> s_ItemTooltipWidths = null;

        private static Rect position;
        private static Rect parent { get; set; }
        private static SearchPropositionOptions options { get; set; }
        private static SortedSet<SearchProposition> propositions { get; set; }

        public static bool enabled { get; set; }

        public static SearchProposition? selection
        {
            get
            {
                if (!enabled || s_FilteredList == null)
                    return null;

                if (s_CurrentSelection < 0 || s_CurrentSelection >= s_FilteredList.Count)
                    return null;

                return s_FilteredList[s_CurrentSelection];
            }
        }

        public static int count
        {
            get
            {
                if (!enabled || s_FilteredList == null)
                    return 0;

                return s_FilteredList.Count;
            }
        }

        public static bool Show(SearchContext context, Rect parentRect, SearchField searchField)
        {
            var te = searchField.GetTextEditor();
            if (te.controlID != GUIUtility.keyboardControl)
                return false;

            parent = parentRect;
            options = new SearchPropositionOptions(context, te.cursorIndex);
            propositions = SearchProposition.Fetch(context, options);

            enabled = propositions.Count > 0;
            if (!enabled)
                return false;

            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchAutoCompleteTab, string.Join(",", options.tokens));
            UpdateCompleteList(te, options);
            return true;
        }

        public static void Draw(SearchContext context, ISearchView view, SearchField searchField)
        {
            if (!enabled)
                return;

            var evt = Event.current;
            if (evt.type == EventType.MouseDown && !position.Contains(evt.mousePosition))
            {
                evt.Use();
                Clear();
                return;
            }

            // Check if the cache filtered list should be updated
            if (evt.type == EventType.Repaint && !context.searchText.Equals(s_LastInput, StringComparison.Ordinal))
                UpdateCompleteList(searchField.GetTextEditor());

            if (s_FilteredList == null)
                return;

            var selected = DrawItems(evt, out var proposition);
            if (proposition.valid)
            {
                if (proposition.moveCursor == TextCursorPlacement.MoveLineEnd)
                {
                    view.SetSearchText(proposition.replacement, proposition.moveCursor);
                }
                else if (!options.tokens.All(t => t.StartsWith(proposition.replacement, StringComparison.OrdinalIgnoreCase)))
                {
                    var insertion = ReplaceText(context.searchText, proposition.replacement, options.cursor, out var insertTokenPos);
                    SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchAutoCompleteInsertSuggestion, insertion);
                    view.SetSearchText(insertion, proposition.moveCursor, insertTokenPos);
                }
            }

            if (selected)
            {
                // No more results
                Clear();
            }
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

        public static bool HandleKeyEvent(Event evt)
        {
            if (!enabled || evt.type != EventType.KeyDown)
                return false;

            if (evt.keyCode == KeyCode.DownArrow)
            {
                s_CurrentSelection = Utils.Wrap(s_CurrentSelection + 1, s_FilteredList.Count);
                evt.Use();
                return true;
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                s_CurrentSelection = Utils.Wrap(s_CurrentSelection - 1, s_FilteredList.Count);
                evt.Use();
                return true;
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                Clear();
                evt.Use();
                return true;
            }
            else if (IsKeySelection(evt))
            {
                return enabled;
            }

            return false;
        }

        public static bool IsHovered(in Vector2 mousePosition)
        {
            if (enabled && position.Contains(mousePosition))
                return true;
            return false;
        }

        public static void Clear()
        {
            if (!enabled)
                return;

            enabled = false;
            s_CurrentSelection = 0;
            s_LastInput = null;
            s_FilteredList = null;
            s_ItemTooltipWidths = null;
            s_ItemLabelWidths = null;
        }

        private static void UpdateCompleteList(in TextEditor te, in SearchPropositionOptions baseOptions = null)
        {
            options = baseOptions ?? new SearchPropositionOptions(te.text, te.cursorIndex);
            position = CalcRect(te, parent.width * 0.55f, parent.height * 0.8f);

            var maxVisibleCount = Mathf.FloorToInt(position.height / EditorStyles.toolbarDropDown.fixedHeight);
            BuildCompleteList(options.tokens, maxVisibleCount, 0.4f);

            s_ItemLabelWidths = new List<float>();
            s_ItemTooltipWidths = new List<float>();

            var maxLabelSize = 100f;
            var gc = new GUIContent();
            foreach (var e in s_FilteredList)
            {
                var sf = 5.0f;
                gc.text = e.label;
                Styles.autoCompleteItemLabel.CalcMinMaxWidth(gc, out var minWidth, out var maxWidth);
                s_ItemLabelWidths.Add(maxWidth);
                sf += maxWidth;

                if (!string.IsNullOrEmpty(e.help))
                {
                    gc.text = e.help;
                    Styles.autoCompleteTooltip.CalcMinMaxWidth(gc, out minWidth, out maxWidth);
                    s_ItemTooltipWidths.Add(maxWidth);
                    sf += maxWidth;
                }

                if (sf > maxLabelSize && sf < parent.width)
                    maxLabelSize = sf;
            }

            position.width = maxLabelSize;
            var xOffscreen = parent.width - position.xMax;
            if (xOffscreen < 0)
                position.x += xOffscreen;

            s_LastInput = te.text;
        }

        private static void BuildCompleteList(string[] inputs, int maxCount, float levenshteinDistance)
        {
            var uniqueSrc = new List<SearchProposition>(propositions);
            int srcCnt = uniqueSrc.Count;

            s_FilteredList = new List<SearchProposition>(Math.Min(maxCount, srcCnt));

            // Start with - slow
            SelectPropositions(ref srcCnt, maxCount, uniqueSrc, p => inputs.Any(i => p.label.StartsWith(i, StringComparison.OrdinalIgnoreCase)));

            s_FilteredList.Sort();

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
            if (levenshteinDistance > 0f && inputs.Length > 0 && s_FilteredList.Count < maxCount)
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

            s_CurrentSelection = Math.Max(-1, Math.Min(s_CurrentSelection, s_FilteredList.Count - 1));
        }

        private static string[] FilterInputWords(in IEnumerable<string> words)
        {
            return words.Where(i => i.Length > 3).Select(w => w
                .Replace("<", "")
                .Replace("=", "")
                .Replace(">", "")
                .Replace("#m_", "#")).Distinct().ToArray();
        }

        private static void SelectPropositions(ref int srcCnt, int maxCount, List<SearchProposition> source, Func<SearchProposition, bool> compare)
        {
            for (int i = 0; i < srcCnt && s_FilteredList.Count < maxCount; i++)
            {
                var p = source[i];
                if (!compare(p))
                    continue;

                s_FilteredList.Add(p);
                source.RemoveAt(i);
                srcCnt--;
                i--;
            }
        }

        private static bool DrawItems(Event evt, out SearchProposition result)
        {
            result = default;
            int cnt = s_FilteredList.Count;
            if (cnt == 0)
                return true;

            position = new Rect(position.x, position.y, position.width, cnt * Styles.autoCompleteItemLabel.fixedHeight + 20f);
            GUI.Box(position, GUIContent.none, Styles.autoCompleteBackground);
            using (new GUI.ClipScope(position))
            {
                Rect lineRect = new Rect(1, 10, position.width - 2, Styles.autoCompleteItemLabel.fixedHeight);
                for (int i = 0; i < cnt; i++)
                {
                    if (DrawItem(evt, lineRect, i == s_CurrentSelection, s_FilteredList[i], i))
                    {
                        result = s_FilteredList[i];
                        return true;
                    }
                    lineRect.y += lineRect.height;
                }
            }

            return false;
        }

        private static bool IsKeySelection(Event evt)
        {
            var kc = evt.keyCode;
            return kc == KeyCode.Return || kc == KeyCode.KeypadEnter || kc == KeyCode.Tab;
        }

        private static string HightlightLabel(string label)
        {
            if (string.IsNullOrEmpty(label) || options.tokens.Any(string.IsNullOrEmpty) || label.IndexOf('<') != -1)
                return label;
            foreach (var token in options.tokens)
            {
                var escapedToken = Regex.Escape(token);
                label = Regex.Replace(label, escapedToken, $"<b>{token}</b>", RegexOptions.IgnoreCase);
            }
            return label;
        }

        private static bool DrawItem(Event evt, Rect rect, bool selected, SearchProposition item, int index)
        {
            var itemSelected = selected && evt.type == EventType.KeyDown && IsKeySelection(evt);
            string trimmedLabel;
            if (s_ItemLabelWidths[index] > position.width)
            {
                var width = position.width - s_ItemTooltipWidths[index] - 20f;
                var numCharacters = Styles.autoCompleteItemLabel.GetNumCharactersThatFitWithinWidth(item.label, width);
                trimmedLabel = Utils.TrimText(HightlightLabel(item.label), numCharacters);
            }
            else
            {
                trimmedLabel = Utils.TrimText(HightlightLabel(item.label));
            }

            if (itemSelected || GUI.Button(rect, trimmedLabel, selected ? Styles.autoCompleteSelectedItemLabel : Styles.autoCompleteItemLabel))
            {
                evt.Use();
                GUI.changed = true;
                return true;
            }

            if (!string.IsNullOrEmpty(item.help))
                GUI.Label(rect, Utils.TrimText(item.help), Styles.autoCompleteTooltip);

            return false;
        }

        private static Rect CalcRect(TextEditor te, float maxWidth, float maxHeight)
        {
            return CalcRect(te, new Vector2(maxWidth, maxHeight), true);
        }

        private static Rect CalcRect(TextEditor te, Vector2 popupSize, bool setMinMax = false)
        {
            var itemHeight = Styles.autoCompleteItemLabel.fixedHeight;
            if (setMinMax)
                popupSize = new Vector2(popupSize.x, Mathf.Max(115f, Mathf.Min(propositions.Count * itemHeight, popupSize.y)));
            var popupOffset = new Vector2(te.position.xMin, SearchField.searchFieldSingleLineHeight);
            return new Rect(te.graphicalCursorPos + popupOffset, popupSize);
        }
    }
}
