// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    class EditorToolbarCycleButton : ToolbarButton
    {
        public Action<int> valueChanged;

        int m_Value;
        GUIContent[] m_Content = new GUIContent[0];
        string[] m_IconNames = new string[0];
        readonly VisualElement m_IconElement;
        readonly TextElement m_TextElement;

        public string[] iconNames
        {
            get => m_IconNames;
            set
            {
                if (value == null)
                {
                    m_Content = new GUIContent[0];
                    return;
                }

                m_IconNames = value;
                this.value = this.value; //Ensure that value is still within new bounds
                UpdateContent();
            }
        }

        public GUIContent[] content
        {
            get => m_Content;
            set
            {
                if (value == null)
                {
                    m_Content = new GUIContent[0];
                    return;
                }

                m_Content = value;
                this.value = this.value; //Ensure that value is still within new bounds
                UpdateContent();
            }
        }

        public int count => Mathf.Max(m_Content.Length, m_IconNames.Length);

        public int value
        {
            get => m_Value;
            set
            {
                var v = Mathf.Clamp(value, 0, Mathf.Max(0, count - 1));
                if (v == m_Value)
                    return;

                m_Value = v;
                UpdateContent();
                valueChanged?.Invoke(v);
            }
        }

        public EditorToolbarCycleButton()
        {
            m_IconElement = EditorToolbarUtility.AddIconElement(this);
            m_TextElement = EditorToolbarUtility.AddTextElement(this);
            clicked += Cycle;
        }

        public void Cycle()
        {
            int next = value + 1;
            if (next >= count)
                next = 0;

            value = next;
        }

        void UpdateContent()
        {
            if (m_IconNames.Length - 1 >= value)
            {
                m_IconElement.name = m_IconNames[value];
            }

            if (m_Content.Length - 1 >= value)
            {
                m_TextElement.text = m_Content[value].text;
                tooltip = m_Content[value].tooltip;
            }
        }
    }
}
