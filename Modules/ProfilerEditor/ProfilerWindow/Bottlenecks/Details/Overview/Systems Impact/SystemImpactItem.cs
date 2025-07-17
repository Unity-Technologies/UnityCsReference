// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class SystemImpactItem : VisualElement
    {
        readonly VisualElement m_Bar;
        readonly Label m_Label;

        public SystemImpactItem()
        {
            name = "system-impact-item";

            m_Bar = new VisualElement()
            {
                name = "system-impact-item__bar"
            };
            Add(m_Bar);

            m_Label = new Label()
            {
                name = "system-impact-item__label"
            };
            Add(m_Label);

            ApplyStyleSheet();
        }

        public void Configure(SystemsImpactModel.SystemImpact systemImpact, float normalizedDuration)
        {
            m_Label.text = $"{systemImpact.Name} ({TimeFormatterUtility.FormatTimeNsToMs(systemImpact.DurationNs)})";
            m_Bar.style.width = new StyleLength(new Length(normalizedDuration * 100f, LengthUnit.Percent));

            var color = (UserAccessiblitySettings.colorBlindCondition == ColorBlindCondition.Default)
                ? systemImpact.Color : systemImpact.ColorBlindColor;
            m_Bar.style.backgroundColor = color;
        }

        void ApplyStyleSheet()
        {
            var uss = EditorGUIUtility.Load("SystemImpactItem.uss") as StyleSheet;
            styleSheets.Add(uss);

            const string k_UssClass_Dark = "system-impact-item__dark";
            const string k_UssClass_Light = "system-impact-item__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            AddToClassList(themeUssClass);
        }
    }
}
