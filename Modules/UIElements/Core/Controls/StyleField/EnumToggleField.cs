// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a field for selecting an enum value.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal class EnumToggleField<T> : BaseField<T> where T : struct, Enum, IConvertible
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<T>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<T>.UxmlSerializedData.Register();
            }
        }

        static readonly Dictionary<string, string> k_SpecialEnumNamesCases = new()
        {
            {"nowrap", "no-wrap"},
            {"tabindex", "tab-index"}
        };

        ToggleButtonGroup m_ToggleButtonGroup;

        public ToggleButtonGroup toggleButtonGroup => m_ToggleButtonGroup;

        /// <summary>
        /// Constructor.
        /// </summary>
        public EnumToggleField()
            : this(null) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="useIcon">Whether to use an icon for the buttons or text of the enum value.</param>
        public EnumToggleField(bool useIcon = false)
            : this(null, useIcon) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The Label text.</param>
        /// <param name="useIcon">Whether to use an icon for the buttons or text of the enum value.</param>
        public EnumToggleField(string label, bool useIcon = false)
            : base(label, null)
        {
            m_ToggleButtonGroup = new ToggleButtonGroup();
            m_ToggleButtonGroup.AddToClassList(alignedFieldUssClassName);

            var enumType = typeof(T);
            var kebabCase = k_SpecialEnumNamesCases.GetValueOrDefault(enumType.Name, enumType.Name.ToKebabCase());
            m_ToggleButtonGroup.AddToClassList($"{ToggleButtonGroup.ussClassName}_{kebabCase}-field");

            foreach (Enum item in Enum.GetValues(enumType))
            {
                var enumName = item.ToString();
                var auto = StyleValueKeyword.Auto.ToString();
                var button = new Button();
                if (enumName == auto)
                {
                    button.name = "auto";
                    button.text = auto.ToUpperInvariant();
                }
                else
                {
                    kebabCase = k_SpecialEnumNamesCases.GetValueOrDefault(enumName, enumName.ToKebabCase());
                    button.name = kebabCase;
                    if (!useIcon)
                    {
                        button.text = enumName;
                    }
                }

                button.clicked += () =>
                {
                    value = (T)Enum.Parse(enumType, enumName, true);
                };
                m_ToggleButtonGroup.Add(button);
            }

            m_ToggleButtonGroup.userData = enumType;
            visualInput.Add(m_ToggleButtonGroup);
        }

        public void SetIconForEnumValue(T enumValue, Texture2D icon)
        {
            var index = Array.IndexOf(Enum.GetValues(typeof(T)), enumValue);
            if (index >= 0)
            {
                m_ToggleButtonGroup.GetButton(index).iconImage = Background.FromTexture2D(icon);
            }
        }

        public void SetTextForEnumValue(T enumValue, string text)
        {
            var index = Array.IndexOf(Enum.GetValues(typeof(T)), enumValue);
            if (index >= 0)
            {
                m_ToggleButtonGroup.GetButton(index).text = text;
            }
        }

        public void SetTooltipForEnumValue(T enumValue, string tooltipValue)
        {
            var index = Array.IndexOf(Enum.GetValues(typeof(T)), enumValue);
            if (index >= 0)
            {
                m_ToggleButtonGroup.GetButton(index).tooltip = tooltipValue;
            }
        }

        public override void SetValueWithoutNotify(T newValue)
        {
            base.SetValueWithoutNotify(newValue);
            var state = m_ToggleButtonGroup.value;
            state.ResetAllOptions();

            if (m_ToggleButtonGroup.userData is Type type)
            {
                var index = Array.IndexOf(Enum.GetValues(type), newValue);
                state[index] = true;
            }

            m_ToggleButtonGroup.value = state;
        }
    }
}
