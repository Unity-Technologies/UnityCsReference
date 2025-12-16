// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Presets
{
    class AddPresetTypeGUI : AdvancedDropdownGUI
    {
        private static class Styles
        {
            public static GUIStyle itemStyle = "DD LargeItemStyle";
        }

        private Vector2 m_IconSize = new Vector2(16, 16);

        internal override Vector2 iconSize => m_IconSize;
        internal override GUIStyle lineStyle => Styles.itemStyle;

        public AddPresetTypeGUI(AdvancedDropdownDataSource dataSource) : base(dataSource)
        {
        }

        internal override void DrawItem(AdvancedDropdownItem item, string name, Texture2D icon, bool enabled, bool drawArrow, bool selected, bool hasSearch)
        {
            if (hasSearch && item is PresetTypeDropdownItem)
            {
                name = ((PresetTypeDropdownItem)item).searchableName;
            }
            base.DrawItem(item, name, icon, enabled, drawArrow, selected, hasSearch);
        }
    }
}
