// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
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
        private AdvancedDropdownItemState[] states = new AdvancedDropdownItemState[0];
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            while (selectedIndex < item.children.Count() && item.children.ElementAt(selectedIndex).IsSeparator());
#pragma warning restore RS0030

#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (selectedIndex >= item.children.Count())
#pragma warning restore RS0030
                selectedIndex = 0;

#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (selectedIndex < item.children.Count())
#pragma warning restore RS0030
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            while (selectedIndex >= 0 && item.children.ElementAt(selectedIndex).IsSeparator());
#pragma warning restore RS0030

            if (selectedIndex < 0)
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                selectedIndex = item.children.Count() - 1;
#pragma warning restore RS0030

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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            else if (selectedIndex >= item.children.Count())
#pragma warning restore RS0030
            {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                state.selectedIndex = item.children.Count() - 1;
#pragma warning restore RS0030
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (!item.children.Any() || index < 0 || index >= item.children.Count())
#pragma warning restore RS0030
                return null;
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return item.children.ElementAt(index);
#pragma warning restore RS0030
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
