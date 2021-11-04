// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_UNDO_REDO

using System;
using UnityEngine;

namespace UnityEditor.Search
{
    [Serializable]
    class UndoManager
    {
        [Serializable]
        struct TextUndoInfo
        {
            public string text;
            public int cursorPos;
            public int selectPos;

            public TextUndoInfo(in string text, in TextEditor te)
            {
                this.text = text ?? te?.text ?? string.Empty;
                this.cursorPos = te?.cursorIndex ?? this.text.Length;
                this.selectPos = te?.selectIndex ?? this.cursorPos;
            }

            public TextUndoInfo(string text, int cursorPos, int selectPos)
            {
                this.text = text;
                this.cursorPos = cursorPos;
                this.selectPos = selectPos;
            }

            public override string ToString()
            {
                return $"{text} ({cursorPos}/{selectPos})";
            }
        }

        private int m_UndoIndex;
        private TextUndoInfo[] m_UndoStack;
        private double m_UndoLastTime;
        private int m_RecentSearchIndex;
        private string m_CurrentText;

        public UndoManager(string text)
        {
            m_UndoLastTime = 0d;
            m_UndoIndex = -1;
            m_UndoStack = new TextUndoInfo[20];
            m_RecentSearchIndex = -1;

            Save(text, null);
        }

        public void Save(in double time, string text, TextEditor textEditor)
        {
            if (time - m_UndoLastTime < 0.25d)
                return;

            m_UndoLastTime = time;
            Save(text, textEditor, out var ld);
            if (ld > 5 && textEditor != null)
            {
                m_RecentSearchIndex = 0;
                SearchSettings.AddRecentSearch(text);
            }
        }

        public void Save(string text, TextEditor textEditor)
        {
            Save(text, textEditor, out _);
        }

        private void Save(string text, TextEditor textEditor, out int distance)
        {
            distance = 0;
            if (string.IsNullOrEmpty(text))
                return;

            if (string.CompareOrdinal(text, m_CurrentText) == 0)
                return;

            distance = Utils.LevenshteinDistance(m_CurrentText ?? string.Empty, text);
            if (distance <= 1)
                return;

            m_UndoIndex = Utils.Wrap(m_UndoIndex + 1, m_UndoStack.Length);
            m_UndoStack[m_UndoIndex] = new TextUndoInfo(text, textEditor);


            m_CurrentText = text;
        }


        public bool HandleEvent(in Event evt, out string searchText, out int cursorPos, out int selectPos)
        {
            searchText = null;
            cursorPos = -1;
            selectPos = -1;

            if (evt.type != EventType.KeyDown)
                return false;

            if (evt.modifiers.HasAny(EventModifiers.Alt))
            {
                if (evt.keyCode == KeyCode.DownArrow)
                    searchText = CyclePreviousSearch(-1);
                else if (evt.keyCode == KeyCode.UpArrow)
                    searchText = CyclePreviousSearch(+1);
            }
            else if (m_UndoIndex != -1 && evt.keyCode == KeyCode.Z && (evt.command || evt.control))
            {
                if (!UndoEdit(1, out searchText, out cursorPos, out selectPos))
                    return false;
            }
            else if (m_UndoIndex != -1 && evt.keyCode == KeyCode.Y && (evt.command || evt.control))
            {
                if (!UndoEdit(-1, out searchText, out cursorPos, out selectPos))
                    return false;
            }

            if (searchText != null && string.CompareOrdinal(searchText, m_CurrentText) != 0)
            {
                m_CurrentText = searchText;
                return true;
            }

            return false;
        }

        private string CyclePreviousSearch(int shift)
        {
            if (SearchSettings.recentSearches.Count == 0)
                return m_CurrentText;

            m_RecentSearchIndex = Utils.Wrap(m_RecentSearchIndex + shift, SearchSettings.recentSearches.Count);
            return SearchSettings.recentSearches[m_RecentSearchIndex];
        }

        private bool UndoEdit(int direction, out string text, out int cursorPos, out int selectPos)
        {
            text = null;
            cursorPos = -1;
            selectPos = -1;

            var nextIndex = Utils.Wrap(m_UndoIndex - direction, m_UndoStack.Length);
            if (string.IsNullOrEmpty(m_UndoStack[nextIndex].text))
                return false;

            m_UndoIndex = nextIndex;
            text = m_UndoStack[m_UndoIndex].text;
            selectPos = m_UndoStack[m_UndoIndex].selectPos;
            cursorPos = m_UndoStack[m_UndoIndex].cursorPos;
            return true;
        }
    }
}
