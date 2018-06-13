// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.AdvancedDropdown
{
    internal class SimpleDataSource : AdvancedDropdownDataSource
    {
        private GUIContent[] m_DisplayedOptions;
        public GUIContent[] displayedOptions {set { m_DisplayedOptions = value; }}

        private static int m_SelectedIndex;
        public int selectedIndex { set { m_SelectedIndex = value; } }

        protected override AdvancedDropdownItem FetchData()
        {
            selectedIds.Clear();
            var rootGroup = new AdvancedDropdownItem("", -1);

            for (int i = 0; i < m_DisplayedOptions.Length; i++)
            {
                var option = m_DisplayedOptions[i];

                var element = new AdvancedDropdownItem(option, i);
                element.SetParent(rootGroup);
                rootGroup.AddChild(element);

                if (i == m_SelectedIndex)
                {
                    selectedIds.Add(element.id);
                    rootGroup.selectedItem = i;
                }
            }
            return rootGroup;
        }
    }
}
