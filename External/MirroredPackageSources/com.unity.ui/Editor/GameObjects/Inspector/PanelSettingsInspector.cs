using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Inspector
{
    [CustomEditor(typeof(PanelSettings))]
    internal class PanelSettingsInspector : Editor
    {
        const string k_DefaultStyleSheetPath = "UIPackageResources/StyleSheets/Inspector/PanelSettingsInspector.uss";
        const string k_InspectorVisualTreeAssetPath = "UIPackageResources/UXML/Inspector/PanelSettingsInspector.uxml";

        private static StyleSheet k_DefaultStyleSheet = null;

        private VisualElement m_RootVisualElement;

        private VisualTreeAsset m_InspectorUxml;

        private ObjectField m_ThemeStyleSheetField;
        private ObjectField m_UITKTextSettings;
        private ObjectField m_TargetTextureField;

        private EnumField m_ScaleModeField;
        private EnumField m_ScreenMatchModeField;

        private VisualElement m_ScaleModeConstantPixelSizeGroup;
        private VisualElement m_ScaleModeScaleWithScreenSizeGroup;
        private VisualElement m_ScaleModeContantPhysicalSizeGroup;

        private VisualElement m_ScreenMatchModeMatchWidthOrHeightGroup;

        private PropertyField m_ClearColorField;
        private PropertyField m_ColorClearValueField;

        private FloatField m_SortOrderField;

        private void ConfigureFields()
        {
            // Using MandatoryQ instead of just Q to make sure modifications of the UXML file don't make the
            // necessary elements disappear unintentionally.
            m_ThemeStyleSheetField = m_RootVisualElement.MandatoryQ<ObjectField>("themeStyleSheet");
            m_ThemeStyleSheetField.objectType = typeof(ThemeStyleSheet);

            m_UITKTextSettings = m_RootVisualElement.MandatoryQ<ObjectField>("textSettings");
            m_UITKTextSettings.objectType = typeof(PanelTextSettings);

            m_TargetTextureField = m_RootVisualElement.MandatoryQ<ObjectField>("targetTexture");
            m_TargetTextureField.objectType = typeof(RenderTexture);

            m_ScaleModeField = m_RootVisualElement.MandatoryQ<EnumField>("scaleMode");
            m_ScreenMatchModeField = m_RootVisualElement.MandatoryQ<EnumField>("screenMatchMode");

            m_ScaleModeConstantPixelSizeGroup = m_RootVisualElement.MandatoryQ("scaleModeConstantPixelSize");
            m_ScaleModeScaleWithScreenSizeGroup = m_RootVisualElement.MandatoryQ("scaleModeScaleWithScreenSize");
            m_ScaleModeContantPhysicalSizeGroup = m_RootVisualElement.MandatoryQ("scaleModeConstantPhysicalSize");

            m_ScreenMatchModeMatchWidthOrHeightGroup =
                m_RootVisualElement.MandatoryQ("screenMatchModeMatchWidthOrHeight");

            m_ClearColorField = m_RootVisualElement.MandatoryQ<PropertyField>("clearColor");
            m_ColorClearValueField = m_RootVisualElement.MandatoryQ<PropertyField>("colorClearValue");

            var choices = new List<int> {0, 1, 2, 3, 4, 5, 6, 7};
            var targetDisplayField = new PopupField<int>("Target Display", choices, 0, i => $"Display {i + 1}", i => $"Display {i + 1}");
            targetDisplayField.bindingPath = "m_TargetDisplay";

            m_TargetTextureField.parent.Add(targetDisplayField);
            targetDisplayField.PlaceInFront(m_TargetTextureField);

            m_SortOrderField = m_RootVisualElement.MandatoryQ<FloatField>("sortingOrder");
            m_SortOrderField.isDelayed = true;
        }

        private void BindFields()
        {
            m_ScaleModeField.RegisterCallback<ChangeEvent<Enum>>(evt =>
                UpdateScaleModeValues((PanelScaleMode)evt.newValue));
            m_ScreenMatchModeField.RegisterCallback<ChangeEvent<Enum>>(evt =>
                UpdateScreenMatchModeValues((PanelScreenMatchMode)evt.newValue));
            m_ClearColorField.RegisterCallback<ChangeEvent<bool>>(evt =>
                UpdateColorClearValue(evt.newValue));
            m_SortOrderField.RegisterCallback<ChangeEvent<float>>(evt => SetSortOrder(evt));
        }

        private void UpdateScaleModeValues(PanelScaleMode scaleMode)
        {
            switch (scaleMode)
            {
                case PanelScaleMode.ConstantPixelSize:
                    m_ScaleModeConstantPixelSizeGroup.style.display = DisplayStyle.Flex;
                    m_ScaleModeScaleWithScreenSizeGroup.style.display = DisplayStyle.None;
                    m_ScaleModeContantPhysicalSizeGroup.style.display = DisplayStyle.None;
                    break;
                case PanelScaleMode.ScaleWithScreenSize:
                    m_ScaleModeConstantPixelSizeGroup.style.display = DisplayStyle.None;
                    m_ScaleModeScaleWithScreenSizeGroup.style.display = DisplayStyle.Flex;
                    m_ScaleModeContantPhysicalSizeGroup.style.display = DisplayStyle.None;
                    break;
                case PanelScaleMode.ConstantPhysicalSize:
                    m_ScaleModeConstantPixelSizeGroup.style.display = DisplayStyle.None;
                    m_ScaleModeScaleWithScreenSizeGroup.style.display = DisplayStyle.None;
                    m_ScaleModeContantPhysicalSizeGroup.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        private void UpdateScreenMatchModeValues(PanelScreenMatchMode screenMatchMode)
        {
            switch (screenMatchMode)
            {
                case PanelScreenMatchMode.MatchWidthOrHeight:
                    m_ScreenMatchModeMatchWidthOrHeightGroup.style.display = DisplayStyle.Flex;
                    break;
                default:
                    m_ScreenMatchModeMatchWidthOrHeightGroup.style.display = DisplayStyle.None;
                    break;
            }
        }

        void UpdateColorClearValue(bool newClearColor)
        {
            m_ColorClearValueField.SetEnabled(newClearColor);
        }

        private void SetSortOrder(ChangeEvent<float> evt)
        {
            PanelSettings panelSettings = (PanelSettings)target;

            if (panelSettings.sortingOrder != evt.newValue)
            {
                m_SortOrderField.schedule.Execute(() => panelSettings.ApplySortingOrder());
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (m_RootVisualElement == null)
            {
                m_RootVisualElement = new VisualElement();
            }
            else
            {
                m_RootVisualElement.Clear();
            }

            if (m_InspectorUxml == null)
            {
                m_InspectorUxml = EditorGUIUtility.Load(k_InspectorVisualTreeAssetPath) as VisualTreeAsset;
            }

            if (k_DefaultStyleSheet == null)
            {
                k_DefaultStyleSheet = EditorGUIUtility.Load(k_DefaultStyleSheetPath) as StyleSheet;
            }
            m_RootVisualElement.styleSheets.Add(k_DefaultStyleSheet);

            m_InspectorUxml.CloneTree(m_RootVisualElement);
            ConfigureFields();
            BindFields();

            PanelSettings panelSettings = (PanelSettings)target;
            UpdateScaleModeValues(panelSettings.scaleMode);
            UpdateScreenMatchModeValues(panelSettings.screenMatchMode);
            UpdateColorClearValue(panelSettings.clearColor);

            return m_RootVisualElement;
        }
    }
}
