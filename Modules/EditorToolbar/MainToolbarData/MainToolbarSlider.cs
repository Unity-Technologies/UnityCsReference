// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public sealed class MainToolbarSlider : MainToolbarElement
    {
        readonly Action<float> m_ValueChanged;
        readonly float m_MinValue, m_MaxValue, m_Value;
        readonly bool m_Rounded;
        EditorToolbarSlider m_Slider;

        public MainToolbarSlider(MainToolbarContent content, float value, float minValue, float maxValue, Action<float> valueChanged)
            : this(content, value, minValue, maxValue, valueChanged, false) {}

        public MainToolbarSlider(MainToolbarContent content, float value, float minValue, float maxValue, Action<float> valueChanged, bool rounded)
        {
            this.content = content;
            m_MinValue = minValue;
            m_MaxValue = maxValue;
            m_Value = value;
            m_ValueChanged = valueChanged;
            m_Rounded = rounded;
        }

        internal override Action<DropdownMenu> populateContextMenuInternal => PopulateContextMenu;

        internal override VisualElement CreateElement()
        {
            m_Slider = new EditorToolbarSlider(content.text, new EditorToolbarIcon(content.image), m_MinValue, m_MaxValue);
            m_Slider.rounded = m_Rounded;
            m_Slider.SetValueWithoutNotify(m_Value);
            m_Slider.RegisterValueChangedCallback((evt) => m_ValueChanged.Invoke(evt.newValue));
            return m_Slider;
        }

        void PopulateContextMenu(DropdownMenu menu)
        {
            if (m_Slider == null)
                return;

            m_Slider.PopulateContextMenu(menu);
        }
    }
}
