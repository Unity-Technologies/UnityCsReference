// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    internal class SimpleDataSource : AdvancedDropdownDataSource
    {
        private GUIContent[] m_DisplayedOptions;
        internal GUIContent[] displayedOptions
        {
            set { m_DisplayedOptions = value; }
        }

        private static int m_SelectedIndex;
        private AdvancedDropdownState m_State;

        internal int selectedIndex
        {
            set { m_SelectedIndex = value; }
        }

        internal SimpleDataSource()
        {
        }

        public SimpleDataSource(GUIContent[] displayOptions)
        {
            m_DisplayedOptions = displayOptions;
        }

        public SimpleDataSource(string[] displayOptions)
        {
            m_DisplayedOptions = displayOptions.Select(a => new GUIContent(a)).ToArray();
        }

        protected override AdvancedDropdownItem FetchData()
        {
            selectedIDs.Clear();
            var rootGroup = new AdvancedDropdownItem("");

            for (int i = 0; i < m_DisplayedOptions.Length; i++)
            {
                var element = new AdvancedDropdownItem(m_DisplayedOptions[i].text)
                {
                    icon = (Texture2D)m_DisplayedOptions[i].image
                };
                element.elementIndex = i;
                rootGroup.AddChild(element);
                if (i == m_SelectedIndex)
                {
                    selectedIDs.Add(element.id);
                    if (m_State != null)
                        m_State.SetSelectedIndex(rootGroup, i);
                }
            }
            return rootGroup;
        }
    }
}
