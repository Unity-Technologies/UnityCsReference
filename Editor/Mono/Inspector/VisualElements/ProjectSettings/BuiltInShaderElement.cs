// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.ProjectSettings
{
    [UxmlElement]
    internal partial class BuiltInShaderElement : VisualElement
    {
        internal enum BuiltinShaderMode
        {
            None,
            Builtin,
            Custom
        }

        PropertyField m_ShaderModeField;
        ObjectField m_CustomShaderObjectField;

        string m_ShaderMode;

        [UxmlAttribute]
        public string shaderMode
        {
            get => m_ShaderMode;
            set
            {
                if (string.Equals(value, m_ShaderMode, StringComparison.Ordinal))
                    return;
                m_ShaderMode = value;
                m_ShaderModeField.bindingPath = value;
            }
        }

        string m_CustomShader;

        [UxmlAttribute]
        public string customShader
        {
            get => m_CustomShader;
            set
            {
                if (string.Equals(value, m_CustomShader, StringComparison.Ordinal))
                    return;
                m_CustomShader = value;
                m_CustomShaderObjectField.bindingPath = value;
            }
        }

        string m_ShaderModeLabel;

        [UxmlAttribute]
        public string shaderModeLabel
        {
            get => m_ShaderModeLabel;
            set
            {
                if (string.Equals(value, m_ShaderModeLabel, StringComparison.Ordinal))
                    return;
                m_ShaderModeLabel = value;
                m_ShaderModeField.label = value;
            }
        }

        string m_CustomShaderLabel;

        [UxmlAttribute]
        public string customShaderLabel
        {
            get => m_CustomShaderLabel;
            set
            {
                if (string.Equals(value, m_CustomShaderLabel, StringComparison.Ordinal))
                    return;
                m_CustomShaderLabel = value;
                m_CustomShaderObjectField.label = value;
            }
        }

        public BuiltInShaderElement()
        {
            m_ShaderModeField = new PropertyField();
            m_CustomShaderObjectField = new ObjectField();
            m_CustomShaderObjectField.SetObjectTypeWithoutDisplayUpdate(typeof(Shader));

            m_ShaderModeField.RegisterValueChangeCallback(evt =>
            {
                m_CustomShaderObjectField.style.display = evt.changedProperty.intValue == (int)BuiltinShaderMode.Custom ? DisplayStyle.Flex : DisplayStyle.None;
            });

            Add(m_ShaderModeField);
            Add(m_CustomShaderObjectField);
            RegisterCallback<TooltipEvent>(evt => ApplyTooltips());
            m_ShaderModeField.RegisterValueChangeCallback(evt => ApplyTooltips());
        }

        public void ApplyTooltips()
        {
            m_ShaderModeField.tooltip = tooltip;
            m_CustomShaderObjectField.tooltip = tooltip;
            m_ShaderModeField.Query<VisualElement>().ForEach(ve => ve.tooltip = tooltip);
        }
    }
}
