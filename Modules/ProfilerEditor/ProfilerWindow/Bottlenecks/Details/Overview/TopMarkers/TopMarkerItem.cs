// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;
using static Unity.Profiling.Editor.UI.TopMarkersModel;

namespace Unity.Profiling.Editor.UI
{
    class TopMarkerItem : VisualElement
    {
        readonly VisualElement m_Bar;
        readonly Label m_Label;
        readonly Button m_Button;

        Marker m_Marker;
        System.Action<Marker> m_OnActionButtonPressed;

        public TopMarkerItem()
        {
            name = "top-marker-item";

            var container = new VisualElement()
            {
                name = "top-marker-item__container"
            };
            Add(container);

            m_Bar = new VisualElement()
            {
                name = "top-marker-item__bar"
            };
            container.Add(m_Bar);

            m_Label = new Label()
            {
                name = "top-marker-item__label",
                displayTooltipWhenElided = false,
            };
            container.Add(m_Label);

            m_Button = new Button()
            {
                name = "top-marker-item__button"
            };
            Add(m_Button);
            m_Button.clickable.clicked += OnButtonClicked;

            ApplyStyleSheet();
        }

        public void Configure(
            Marker marker,
            float markerTimeNormalized,
            string title,
            System.Action<Marker> onActionButtonPressed)
        {
            m_Label.text = $"{marker.Name} ({marker.FormatValue()})";
            var numberOfInstances = marker.NumberOfInstances;
            m_Label.tooltip = $"{marker.Name} ({marker.FormatValue()})\n\n{numberOfInstances} occurrence{((numberOfInstances > 1) ? "s" : string.Empty)}.";
            m_Button.text = title;
            m_Bar.style.width = new StyleLength(new Length(markerTimeNormalized * 100f, LengthUnit.Percent));

            m_Marker = marker;
            m_OnActionButtonPressed = onActionButtonPressed;
        }

        void OnButtonClicked()
        {
            m_OnActionButtonPressed?.Invoke(m_Marker);
        }

        void ApplyStyleSheet()
        {
            var uss = EditorGUIUtility.Load("TopMarkerItem.uss") as StyleSheet;
            styleSheets.Add(uss);

            const string k_UssClass_Dark = "top-marker-item__dark";
            const string k_UssClass_Light = "top-marker-item__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            AddToClassList(themeUssClass);
        }
    }
}
