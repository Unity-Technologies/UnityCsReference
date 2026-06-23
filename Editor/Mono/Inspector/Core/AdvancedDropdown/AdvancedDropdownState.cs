// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    [Serializable]
    public class AdvancedDropdownState
    {
        [Serializable]
        private class AdvancedDropdownItemState
        {
            public AdvancedDropdownItemState(AdvancedDropdownItem item)
            {
                this.itemId = item.id;
            }

            public int itemId;
            public int selectedIndex = -1;
            public Vector2 scroll;
        }

        [SerializeField]
        private AdvancedDropdownItemState[] states = Array.Empty<AdvancedDropdownItemState>();
        private AdvancedDropdownItemState m_LastSelectedState;

        private AdvancedDropdownItemState GetStateForItem(AdvancedDropdownItem item)
        {
            if (m_LastSelectedState != null && m_LastSelectedState.itemId == item.id)
                return m_LastSelectedState;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].itemId == item.id)
                {
                    m_LastSelectedState = states[i];
                    return m_LastSelectedState;
                }
            }
            Array.Resize(ref states, states.Length + 1);
            states[states.Length - 1] = new AdvancedDropdownItemState(item);
            m_LastSelectedState = states[states.Length - 1];
            return states[states.Length - 1];
        }

        internal void MoveDownSelection(AdvancedDropdownItem item)
        {
            MoveSelection(item, 1);
        }

        internal void MoveUpSelection(AdvancedDropdownItem item)
        {
            MoveSelection(item, -1);
        }

        // Moves the selection to the next selectable child in the given direction
        // (+1 = down, -1 = up), wrapping around the list and skipping decorative rows
        // (separators, help boxes). Does nothing if the level has no selectable child.
        private void MoveSelection(AdvancedDropdownItem item, int direction)
        {
            var count = item.childList.Count;
            if (count == 0)
                return;

            var index = GetStateForItem(item).selectedIndex;

            // Normalize an empty selection (-1) so the first step lands on the natural
            // end of the list: the top when moving down, the bottom when moving up.
            if (index < 0)
                index = direction > 0 ? -1 : count;

            // Scan at most one full loop so the wrap path also skips decorative rows
            // instead of landing on a leading/trailing separator or help box.
            for (var step = 0; step < count; step++)
            {
                index = (index + direction + count) % count;
                if (item.childList[index].IsSelectable())
                {
                    SetSelectionOnItem(item, index);
                    return;
                }
            }
        }

        internal void SetSelectionOnItem(AdvancedDropdownItem item, int selectedIndex)
        {
            var state = GetStateForItem(item);

            if (selectedIndex < 0)
            {
                state.selectedIndex = 0;
            }
            else if (selectedIndex >= item.childList.Count)
            {
                state.selectedIndex = item.childList.Count - 1;
            }
            else
            {
                state.selectedIndex = selectedIndex;
            }
        }

        internal int GetSelectedIndex(AdvancedDropdownItem item)
        {
            return GetStateForItem(item).selectedIndex;
        }

        internal void SetSelectedIndex(AdvancedDropdownItem item, int index)
        {
            GetStateForItem(item).selectedIndex = index;
        }

        internal AdvancedDropdownItem GetSelectedChild(AdvancedDropdownItem item)
        {
            var index = GetSelectedIndex(item);
            if (!item.hasChildren || index < 0 || index >= item.childList.Count)
                return null;
            return item.childList[index];
        }

        internal Vector2 GetScrollState(AdvancedDropdownItem item)
        {
            return GetStateForItem(item).scroll;
        }

        internal void SetScrollState(AdvancedDropdownItem item, Vector2 scrollState)
        {
            GetStateForItem(item).scroll = scrollState;
        }
    }
}
