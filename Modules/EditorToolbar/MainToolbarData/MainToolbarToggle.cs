// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;   

namespace UnityEditor.Toolbars
{
    public sealed class MainToolbarToggle : MainToolbarElement
    {
        readonly Action<bool> m_ValueChanged;

        bool m_Value;

        public MainToolbarToggle(MainToolbarContent content, bool value, Action<bool> valueChanged)
        {
            this.content = content;
            m_Value = value;
            m_ValueChanged = valueChanged;
        }

        internal override VisualElement CreateElement()
        {
            var toggle = new EditorToolbarToggle(content.text, content.image, content.image);
            toggle.AddToClassList(EditorToolbar.elementClassName);
            toggle.SetValueWithoutNotify(m_Value);
            toggle.RegisterValueChangedCallback(ValueChanged);
            toggle.tooltip = content.tooltip;
            return toggle;
        }

        void ValueChanged(ChangeEvent<bool> evt)
        {
            var toggle = (EditorToolbarToggle)evt.target;
            m_ValueChanged?.Invoke(evt.newValue);
        }
    }
}
