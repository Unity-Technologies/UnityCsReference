// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define DEBUG_UNDO_REDO

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

            public TextUndoInfo(in string text, in TextField te)
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

        [Serializable]
        internal class UndoQueue<T> : IEnumerable<T> where T : struct
        {
            public int capacity => m_Buffer.Length;

            [SerializeField] T[] m_Buffer;
            [SerializeField] int m_HeadIndex;
            [SerializeField] int m_TailIndex;
            [SerializeField] int m_CurrentIndex;

            public int currentIndex => m_CurrentIndex;

            public UndoQueue(int capacity)
            {
                if (capacity <= 0)
                    throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity should be higher than 0!");
                m_Buffer = new T[capacity];
                m_HeadIndex = 0;
                m_TailIndex = -1;
                m_CurrentIndex = -1;
            }

            public void Push(T data)
            {
                // Empty queue
                if (m_TailIndex == -1)
                {
                    m_Buffer[m_HeadIndex] = data;
                    m_TailIndex = m_CurrentIndex = 0;
                    m_HeadIndex = NextIndex(0);
                    return;
                }

                // Current is on top, i.e. not in the middle of the stack doing undo/redo operations
                if (NextIndex(m_CurrentIndex) == m_HeadIndex)
                {
                    m_Buffer[m_HeadIndex] = data;

                    if (m_HeadIndex == m_TailIndex)
                        m_TailIndex = NextIndex(m_TailIndex);

                    m_CurrentIndex = NextIndex(m_CurrentIndex);
                    m_HeadIndex = NextIndex(m_HeadIndex);
                    return;
                }

                m_CurrentIndex = NextIndex(m_CurrentIndex);
                m_Buffer[m_CurrentIndex] = data;
                m_HeadIndex = NextIndex(m_CurrentIndex);
            }

            public bool Undo(out T data)
            {
                data = default;
                if (!CanUndo())
                    return false;

                m_CurrentIndex = PreviousIndex(m_CurrentIndex);
                data = m_Buffer[m_CurrentIndex];
                return true;
            }

            public bool Redo(out T data)
            {
                data = default;
                if (!CanRedo())
                    return false;

                m_CurrentIndex = NextIndex(m_CurrentIndex);
                data = m_Buffer[m_CurrentIndex];
                return true;
            }

            public bool CanUndo()
            {
                return m_TailIndex != -1 && m_CurrentIndex != m_TailIndex;
            }

            public bool CanRedo()
            {
                return m_TailIndex != -1 && NextIndex(m_CurrentIndex) != m_HeadIndex;
            }

            public IEnumerator<T> GetEnumerator()
            {
                if (m_TailIndex == -1)
                    yield break;
                var index = m_TailIndex;
                do
                {
                    yield return m_Buffer[index];
                    index = NextIndex(index);
                } while (index != m_HeadIndex);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            int NextIndex(int index)
            {
                return Utils.Wrap(index + 1, capacity);
            }

            int PreviousIndex(int index)
            {
                return Utils.Wrap(index - 1, capacity);
            }
        }

        [SerializeField] private UndoQueue<TextUndoInfo> m_UndoStack;
        [SerializeField] private double m_UndoLastTime;
        [SerializeField] private int m_RecentSearchIndex;
        [SerializeField] private string m_CurrentText;

        public UndoManager(string text)
        {
            m_UndoLastTime = 0d;
            m_UndoStack = new(20);
            m_RecentSearchIndex = -1;

            Save(text, null);
        }

        public void Save(in double time, string text, TextField textField)
        {
            if (time - m_UndoLastTime < 0.25d)
                return;

            m_UndoLastTime = time;
            Save(text, textField, out var ld);
            if (ld > 5 && textField != null)
            {
                m_RecentSearchIndex = 0;
                SearchSettings.AddRecentSearch(text);
            }
        }

        public void Save(string text, TextField textField)
        {
            Save(text, textField, out _);
        }

        private void Save(string text, TextField textField, out int distance)
        {
            distance = 0;

            if (m_CurrentText != null && string.CompareOrdinal(text ?? string.Empty, m_CurrentText) == 0)
                return;

            distance = m_CurrentText != null ? Utils.LevenshteinDistance(m_CurrentText, text) : int.MaxValue;
            if (distance <= 1)
                return;

            m_UndoStack.Push(new TextUndoInfo(text, textField));


            m_CurrentText = text ?? string.Empty;
        }


        public bool HandleEvent(in KeyDownEvent evt, out string searchText, out int cursorPos, out int selectPos)
        {
            searchText = null;
            cursorPos = -1;
            selectPos = -1;


            if (evt.modifiers.HasAny(EventModifiers.Alt))
            {
                if (evt.keyCode == KeyCode.DownArrow)
                    searchText = CyclePreviousSearch(-1);
                else if (evt.keyCode == KeyCode.UpArrow)
                    searchText = CyclePreviousSearch(+1);
            }
            else if (m_UndoStack.CanUndo() && evt.keyCode == KeyCode.Z && (evt.commandKey || evt.ctrlKey))
            {
                if (!m_UndoStack.Undo(out var undoData))
                    return false;
                searchText = undoData.text;
                cursorPos = undoData.cursorPos;
                selectPos = undoData.selectPos;
            }
            else if (m_UndoStack.CanRedo() && evt.keyCode == KeyCode.Y && (evt.commandKey || evt.ctrlKey))
            {
                if (!m_UndoStack.Redo(out var redoData))
                    return false;
                searchText = redoData.text;
                cursorPos = redoData.cursorPos;
                selectPos = redoData.selectPos;
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
    }
}
