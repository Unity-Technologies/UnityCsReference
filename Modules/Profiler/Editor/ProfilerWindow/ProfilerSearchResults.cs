// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditorInternal
{
    internal partial class ProfilerHierarchyGUI
    {
        internal class SearchResults
        {
            struct SearchResult
            {
                public string propertyPath;
                public string[] columnValues;
            }

            SearchResult[] m_SearchResults;
            int m_NumResultsUsed;
            ProfilerColumn[] m_ColumnsToShow;
            int m_SelectedSearchIndex;
            bool m_FoundAllResults;
            string m_LastSearchString;
            int m_LastFrameIndex;
            ProfilerColumn m_LastSortType;

            public int numRows
            {
                get
                {
                    return m_NumResultsUsed + (m_FoundAllResults ? 0 : 1);
                }
            }

            public int selectedSearchIndex
            {
                get { return m_SelectedSearchIndex; }
                set
                {
                    if (value < m_NumResultsUsed)
                        m_SelectedSearchIndex = value;
                    else
                        m_SelectedSearchIndex = -1;

                    if (m_SelectedSearchIndex >= 0)
                    {
                        string propertyPath = m_SearchResults[m_SelectedSearchIndex].propertyPath;
                        if (propertyPath != ProfilerDriver.selectedPropertyPath)
                            ProfilerDriver.selectedPropertyPath = propertyPath;
                    }
                }
            }

            public void Init(int maxNumberSearchResults)
            {
                m_SearchResults = new SearchResult[maxNumberSearchResults];
                m_NumResultsUsed = 0;
                m_LastSearchString = "";
                m_LastFrameIndex = -1;
                m_FoundAllResults = false;
                m_ColumnsToShow = null;
                m_SelectedSearchIndex = -1;
            }

            public void Filter(ProfilerProperty property, ProfilerColumn[] columns, string searchString, int frameIndex, ProfilerColumn sortType)
            {
                if (searchString == m_LastSearchString &&
                    frameIndex == m_LastFrameIndex &&
                    sortType == m_LastSortType)
                    return;

                m_LastSearchString = searchString;
                m_LastFrameIndex = frameIndex;
                m_LastSortType = sortType;
                IterateProfilingData(property, columns, searchString);
            }

            void IterateProfilingData(ProfilerProperty property, ProfilerColumn[] columns, string searchString)
            {
                // Reset
                m_NumResultsUsed = 0;
                m_ColumnsToShow = columns;
                m_FoundAllResults = true;
                m_SelectedSearchIndex = -1;

                // Reuse array
                int row = 0;
                string selectedPropertyPath = ProfilerDriver.selectedPropertyPath;
                while (property.Next(true))
                {
                    if (row >= m_SearchResults.Length)
                    {
                        m_FoundAllResults = false;
                        break;
                    }

                    string propertyPath = property.propertyPath;

                    // Ensure we only search the last part of the path: the property name
                    int startPos = Mathf.Max(propertyPath.LastIndexOf('/'), 0);
                    if (propertyPath.IndexOf(searchString, startPos, StringComparison.CurrentCultureIgnoreCase) > -1)
                    {
                        var values = new string[m_ColumnsToShow.Length];
                        for (var i = 0; i < m_ColumnsToShow.Length; i++)
                        {
                            values[i] = property.GetColumn(m_ColumnsToShow[i]);
                        }

                        m_SearchResults[row].propertyPath = propertyPath;
                        m_SearchResults[row].columnValues = values;

                        if (propertyPath == selectedPropertyPath)
                            m_SelectedSearchIndex = row;

                        ++row;
                    }
                }
                m_NumResultsUsed = row;
            }

            public void Draw(ProfilerHierarchyGUI gui, int controlID)
            {
                HandleCommandEvents(gui);

                Event evt = Event.current;
                string selectedPropertyPath = ProfilerDriver.selectedPropertyPath;

                // Only draw visible rows
                int firstRowVisible, lastRowVisible;
                GetFirstAndLastRowVisible(m_NumResultsUsed, kRowHeight, gui.m_TextScroll.y, gui.m_ScrollViewHeight, out firstRowVisible, out lastRowVisible);

                for (int row = firstRowVisible; row <= lastRowVisible; ++row)
                {
                    bool selected = selectedPropertyPath == m_SearchResults[row].propertyPath;
                    var r = gui.GetRowRect(row);
                    var backgroundStyle = gui.GetRowBackgroundStyle(row);

                    // Selection
                    if (evt.type == EventType.MouseDown && r.Contains(evt.mousePosition))
                    {
                        m_SelectedSearchIndex = row;
                        gui.RowMouseDown(m_SearchResults[row].propertyPath);
                        GUIUtility.keyboardControl = controlID;
                        evt.Use();
                    }

                    // Rendering
                    if (evt.type == EventType.Repaint)
                    {
                        // Background
                        backgroundStyle.Draw(r, GUIContent.none, false, false, selected, GUIUtility.keyboardControl == controlID);

                        // Tooltip
                        if (r.Contains(evt.mousePosition))
                        {
                            string tooltip = m_SearchResults[row].propertyPath.Replace("/", "/\n");
                            if (m_SelectedSearchIndex >= 0)
                                tooltip += "\n\n(Press 'F' to frame selection)";
                            GUI.Label(r, GUIContent.Temp(string.Empty, tooltip));
                        }

                        // Text
                        gui.DrawTextColumn(ref r, m_SearchResults[row].columnValues[0], 0, kSmallMargin, selected);
                        styles.numberLabel.alignment = TextAnchor.MiddleRight;
                        int sizerIndex = 1;
                        for (var i = 1; i < gui.m_VisibleColumns.Length; i++)
                        {
                            if (!gui.ColIsVisible(i))
                                continue;

                            r.x += gui.m_Splitter.realSizes[sizerIndex - 1];
                            r.width = gui.m_Splitter.realSizes[sizerIndex] - kSmallMargin;
                            sizerIndex++;

                            styles.numberLabel.Draw(r, m_SearchResults[row].columnValues[i], false, false, false, selected);
                        }
                        styles.numberLabel.alignment = TextAnchor.MiddleLeft;
                    }
                }

                if (!m_FoundAllResults && evt.type == EventType.Repaint)
                {
                    int lastRow = m_NumResultsUsed;
                    var r = new Rect(1, kRowHeight * lastRow, GUIClip.visibleRect.width, kRowHeight);
                    var backgroundStyle = (lastRow % 2 == 0 ? styles.entryEven : styles.entryOdd);
                    GUI.Label(r, GUIContent.Temp(string.Empty, styles.notShowingAllResults.tooltip), GUIStyle.none);

                    backgroundStyle.Draw(r, GUIContent.none, false, false, false, false);
                    gui.DrawTextColumn(ref r, styles.notShowingAllResults.text, 0, kSmallMargin, false);
                }
            }

            static void GetFirstAndLastRowVisible(int numRows, float rowHeight, float scrollBarY, float scrollAreaHeight, out int firstRowVisible, out int lastRowVisible)
            {
                firstRowVisible = (int)Mathf.Floor(scrollBarY / rowHeight);
                lastRowVisible = firstRowVisible + (int)Mathf.Ceil(scrollAreaHeight / rowHeight);

                firstRowVisible = Mathf.Max(firstRowVisible, 0);
                lastRowVisible = Mathf.Min(lastRowVisible, numRows - 1);
            }

            public void MoveSelection(int steps, ProfilerHierarchyGUI gui)
            {
                int newIndex = Mathf.Clamp(m_SelectedSearchIndex + steps, 0, m_NumResultsUsed - 1);
                if (newIndex != m_SelectedSearchIndex)
                {
                    m_SelectedSearchIndex = newIndex;
                    gui.m_Window.SetSelectedPropertyPath(m_SearchResults[newIndex].propertyPath);
                }
            }

            void HandleCommandEvents(ProfilerHierarchyGUI gui)
            {
                Event evt = Event.current;
                EventType eventType = evt.type;
                if (eventType == EventType.ExecuteCommand || eventType == EventType.ValidateCommand)
                {
                    bool execute = eventType == EventType.ExecuteCommand;
                    if (Event.current.commandName == "FrameSelected")
                    {
                        if (execute)
                            gui.FrameSelection();
                        evt.Use();
                    }
                }
            }
        } // class SearchResults
    } // partial class ProfilerHierarchyGUI
} // namespace
