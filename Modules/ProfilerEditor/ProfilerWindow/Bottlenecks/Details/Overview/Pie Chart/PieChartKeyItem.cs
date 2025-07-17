// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class PieChartKeyItem : VisualElement
    {
        readonly VisualElement m_Color;
        readonly Label m_Label;

        public PieChartKeyItem()
        {
            name = "pie-chart-key-item";

            m_Color = new VisualElement()
            {
                name = "pie-chart-key-item__color"
            };
            Add(m_Color);

            m_Label = new Label()
            {
                name = "pie-chart-key-item__label",
                displayTooltipWhenElided = false,
            };
            Add(m_Label);

            ApplyStyleSheet();
        }

        public void Configure(PieChartModel.Segment segment)
        {
            m_Color.style.backgroundColor = segment.Color;
            m_Label.text = $"{segment.Name} ({segment.Percentage}%)";
        }

        void ApplyStyleSheet()
        {
            var uss = EditorGUIUtility.Load("PieChartKeyItem.uss") as StyleSheet;
            styleSheets.Add(uss);

            const string k_UssClass_Dark = "pie-chart-key-item__dark";
            const string k_UssClass_Light = "pie-chart-key-item__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            AddToClassList(themeUssClass);
        }
    }
}
