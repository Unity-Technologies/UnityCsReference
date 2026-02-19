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
            var state = GetStateForItem(item);
            var selectedIndex = state.selectedIndex;
            do
            {
                ++selectedIndex;
            }
            while (selectedIndex < item.childList.Count && item.childList[selectedIndex].IsSeparator());

            if (selectedIndex >= item.childList.Count)
                selectedIndex = 0;

            if (selectedIndex < item.childList.Count)
                SetSelectionOnItem(item, selectedIndex);
        }

        internal void MoveUpSelection(AdvancedDropdownItem item)
        {
            var state = GetStateForItem(item);
            var selectedIndex = state.selectedIndex;
            do
            {
                --selectedIndex;
            }
            while (selectedIndex >= 0 && item.childList[selectedIndex].IsSeparator());

            if (selectedIndex < 0)
                selectedIndex = item.childList.Count - 1;

            if (selectedIndex >= 0)
                SetSelectionOnItem(item, selectedIndex);
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
