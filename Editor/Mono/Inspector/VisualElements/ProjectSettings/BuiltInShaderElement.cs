// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.ProjectSettings
{
    internal class BuiltInShaderElement : VisualElement
    {
        internal enum BuiltinShaderMode
        {
            None,
            Builtin,
            Custom
        }

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
#pragma warning disable 649
            [SerializeField, UxmlAttribute("shader-mode")]
            string shaderMode;
            [SerializeField, UxmlAttribute("custom-shader")]
            string customShader;

            [SerializeField, UxmlAttribute("shader-mode-label")]
            string shaderModeLabel;
            [SerializeField, UxmlAttribute("custom-shader-label")]
            string customShaderLabel;
#pragma warning restore 649
            public override object CreateInstance() => new BuiltInShaderElement();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (BuiltInShaderElement)obj;
                e.shaderMode = shaderMode;
                e.customShader = customShader;
                e.shaderModeLabel = shaderModeLabel;
                e.customShaderLabel = customShaderLabel;
            }
        }

        PropertyField m_ShaderModeField;
        ObjectField m_CustomShaderObjectField;

        string m_ShaderMode;

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
