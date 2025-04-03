// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Inspector
{
    [CustomEditor(typeof(PanelSettings))]
    internal class PanelSettingsInspector : Editor
    {
        const string k_DefaultStyleSheetPath = "UIPackageResources/StyleSheets/Inspector/PanelSettingsInspector.uss";
        const string k_InspectorVisualTreeAssetPath = "UIPackageResources/UXML/Inspector/PanelSettingsInspector.uxml";
        private const string k_StyleClassThemeMissing = "unity-panel-settings-inspector--theme-warning--hidden";

        private static StyleSheet k_DefaultStyleSheet = null;

        private VisualElement m_RootVisualElement;

        private VisualTreeAsset m_InspectorUxml;

        private ObjectField m_ThemeStyleSheetField;
        private ObjectField m_UITKTextSettings;
        private EnumField m_RenderModeField;
        private EnumField m_WorldInputModeField;
        private LayerField m_WorldSpaceLayerField;
        private ObjectField m_TargetTextureField;
        private FloatField m_SortingOrderField;

        private PopupField<int> m_TargetDisplayField;

        private EnumField m_ScaleModeField;
        private EnumField m_ScreenMatchModeField;
        private EnumField m_DataBindingLogLevelField;

        private VisualElement m_ScaleModeConstantPixelSizeGroup;
        private VisualElement m_ScaleModeScaleWithScreenSizeGroup;
        private VisualElement m_ScaleModeContantPhysicalSizeGroup;
        private VisualElement m_ScaleModeWorldSpace;

        private VisualElement m_ReferencePixelsPerUnit;

        private VisualElement m_ScreenMatchModeMatchWidthOrHeightGroup;

        private Foldout m_ClearSettingsFoldout;
        private PropertyField m_ClearColorField;
        private PropertyField m_ColorClearValueField;

        private PropertyField m_ForceGammaOutputField;
        private HelpBox m_ForceGammaOutputHelpBox;

        private PropertyField m_VertexBudgetField;

        private HelpBox m_MissingThemeHelpBox;

        private void ConfigureFields()
        {
            // Using MandatoryQ instead of just Q to make sure modifications of the UXML file don't make the
            // necessary elements disappear unintentionally.
            m_MissingThemeHelpBox = m_RootVisualElement.MandatoryQ<HelpBox>("missing-theme-warning");

            m_ThemeStyleSheetField = m_RootVisualElement.MandatoryQ<ObjectField>("theme-style-sheet");
            m_ThemeStyleSheetField.objectType = typeof(ThemeStyleSheet);

            m_UITKTextSettings = m_RootVisualElement.MandatoryQ<ObjectField>("text-settings");
            m_UITKTextSettings.objectType = typeof(PanelTextSettings);

            m_RenderModeField = m_RootVisualElement.MandatoryQ<EnumField>("render-mode");
            m_WorldInputModeField = m_RootVisualElement.MandatoryQ<EnumField>("worldinput-mode");
            m_WorldSpaceLayerField = m_RootVisualElement.MandatoryQ<LayerField>("worldspace-layer");

            m_TargetTextureField = m_RootVisualElement.MandatoryQ<ObjectField>("target-texture");
            m_TargetTextureField.objectType = typeof(RenderTexture);

            m_ForceGammaOutputField = m_RootVisualElement.MandatoryQ<PropertyField>("force-gamma-rendering");
            m_ForceGammaOutputHelpBox = m_RootVisualElement.MandatoryQ<HelpBox>("force-gamma-rendering-help");

            m_SortingOrderField = m_RootVisualElement.MandatoryQ<FloatField>("sorting-order");

            m_ScaleModeField = m_RootVisualElement.MandatoryQ<EnumField>("scale-mode");
            m_ScreenMatchModeField = m_RootVisualElement.MandatoryQ<EnumField>("screen-match-mode");
            m_DataBindingLogLevelField = m_RootVisualElement.MandatoryQ<EnumField>("log-level");

            m_ScaleModeConstantPixelSizeGroup = m_RootVisualElement.MandatoryQ("scale-mode-constant-pixel-size");
            m_ScaleModeScaleWithScreenSizeGroup = m_RootVisualElement.MandatoryQ("scale-mode-scale-with-screen-size");
            m_ScaleModeContantPhysicalSizeGroup = m_RootVisualElement.MandatoryQ("scale-mode-constant-physical-size");
            m_ScaleModeWorldSpace = m_RootVisualElement.MandatoryQ("scale-mode-world-space");

            m_ReferencePixelsPerUnit = m_RootVisualElement.MandatoryQ("reference-pixels-per-unit");

            m_ScreenMatchModeMatchWidthOrHeightGroup =
                m_RootVisualElement.MandatoryQ("screen-match-mode-match-width-or-height");

            m_ClearSettingsFoldout = m_RootVisualElement.MandatoryQ<Foldout>("clear-settings-foldout");
            m_ClearColorField = m_RootVisualElement.MandatoryQ<PropertyField>("clear-color");
            m_ColorClearValueField = m_RootVisualElement.MandatoryQ<PropertyField>("color-clear-value");

            m_VertexBudgetField = m_RootVisualElement.MandatoryQ<PropertyField>("vertex-budget");

            var choices = new List<int> {0, 1, 2, 3, 4, 5, 6, 7};
            m_TargetDisplayField = new PopupField<int>("Target Display", choices, 0, i => $"Display {i + 1}", i => $"Display {i + 1}");
            m_TargetDisplayField.bindingPath = "m_TargetDisplay";
            m_TargetDisplayField.AddToClassList("unity-base-field__aligned");

            m_TargetTextureField.parent.Add(m_TargetDisplayField);
            m_TargetDisplayField.PlaceInFront(m_TargetTextureField);
        }

        private void BindFields()
        {
            m_ScaleModeField.RegisterCallback<ChangeEvent<Enum>>(evt =>
                UpdateScaleModeValues((PanelScaleMode)evt.newValue));
            m_ScreenMatchModeField.RegisterCallback<ChangeEvent<Enum>>(evt =>
                UpdateScreenMatchModeValues((PanelScreenMatchMode)evt.newValue));
            m_ClearColorField.RegisterCallback<ChangeEvent<bool>>(evt =>
                UpdateColorClearValue(evt.newValue));
            m_RenderModeField.RegisterCallback<ChangeEvent<Enum>>(evt =>
                UpdateRenderMode((PanelRenderMode)evt.newValue));
            m_DataBindingLogLevelField.RegisterCallback<ChangeEvent<Enum>>(evt =>
                ((PanelSettings)target).bindingLogLevel = (BindingLogLevel)evt.newValue);

            m_ThemeStyleSheetField.RegisterValueChangedCallback(evt => UpdateHelpBoxDisplay());

            m_ForceGammaOutputField.RegisterCallback<ChangeEvent<bool>>(_ => UpdateForceGammaOutput());
            m_TargetTextureField.RegisterValueChangedCallback(_ => UpdateForceGammaOutput());
        }

        private void UpdateScaleModeValues(PanelScaleMode scaleMode)
        {
            var ps = target as PanelSettings;
            if (ps != null && ps.renderMode == PanelRenderMode.WorldSpace)
                return; // Don't update the visibility of "overlay" scale values when in world space render mode

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

        void UpdateHelpBoxDisplay()
        {
            PanelSettings panelSettings = (PanelSettings)target;
            bool displayHelpBox = panelSettings.themeStyleSheet == null;

            m_MissingThemeHelpBox.EnableInClassList(k_StyleClassThemeMissing, !displayHelpBox);
        }

        void UpdateForceGammaOutput()
        {
            var panelSettings = (PanelSettings)target;

            if (panelSettings.renderMode == PanelRenderMode.WorldSpace || QualitySettings.activeColorSpace == ColorSpace.Gamma)
            {
                m_ForceGammaOutputField.style.display = DisplayStyle.None;
                m_ForceGammaOutputHelpBox.style.display = DisplayStyle.None;
                return;
            }

            bool hasMessage = false;

            if (panelSettings.forceGammaRendering)
            {
                var rt = panelSettings.targetTexture;
                if (rt != null)
                {
                    if (GraphicsFormatUtility.IsSRGBFormat(rt.graphicsFormat))
                    {
                        m_ForceGammaOutputHelpBox.messageType = HelpBoxMessageType.Warning;
                        m_ForceGammaOutputHelpBox.text = "You should not use a target texture with an SRGB format to store values that are already sRGB-encoded.";
                        hasMessage = true;
                    }
                }
                else
                {
                    // At some point, if it becomes possible to have a UNORM swapchain image when the project is linear,
                    // we could remove this warning.
                    m_ForceGammaOutputHelpBox.messageType = HelpBoxMessageType.Warning;
                    m_ForceGammaOutputHelpBox.text = "Gamma rendering requires the use of a target texture.";
                    hasMessage = true;
                }
            }

            m_ForceGammaOutputField.style.display = DisplayStyle.Flex;
            m_ForceGammaOutputHelpBox.style.display = hasMessage ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void UpdateRenderMode(PanelRenderMode newRenderMode)
        {
            DisplayStyle displayStyleForWorldProperties = DisplayStyle.None;
            DisplayStyle displayStyleForOverlayProperties = DisplayStyle.Flex;

            if (newRenderMode == PanelRenderMode.WorldSpace)
            {
                displayStyleForWorldProperties = DisplayStyle.Flex;
                displayStyleForOverlayProperties = DisplayStyle.None;
            }

            m_WorldSpaceLayerField.style.display = displayStyleForWorldProperties;
            m_WorldInputModeField.style.display = displayStyleForWorldProperties;

            m_TargetTextureField.style.display = displayStyleForOverlayProperties;
            m_ScaleModeField.style.display = displayStyleForOverlayProperties;
            m_ClearSettingsFoldout.style.display = displayStyleForOverlayProperties;
            m_TargetTextureField.style.display = displayStyleForOverlayProperties;
            m_SortingOrderField.style.display = displayStyleForOverlayProperties;
            m_TargetDisplayField.style.display = displayStyleForOverlayProperties;

            if (newRenderMode == PanelRenderMode.WorldSpace)
            {
                m_ScaleModeConstantPixelSizeGroup.style.display = DisplayStyle.None;
                m_ScaleModeScaleWithScreenSizeGroup.style.display = DisplayStyle.None;
                m_ScaleModeContantPhysicalSizeGroup.style.display = DisplayStyle.None;
                m_ScaleModeWorldSpace.style.display = DisplayStyle.Flex;
            }
            else
            {
                PanelScaleMode scaleMode = PanelScaleMode.ConstantPixelSize;

                if (m_ScaleModeField.value != null)
                {
                    scaleMode = (PanelScaleMode)m_ScaleModeField.value;
                    UpdateScaleModeValues(scaleMode);
                }

                m_ScaleModeWorldSpace.style.display = DisplayStyle.None;
            }

            UpdateForceGammaOutput();
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
            UpdateHelpBoxDisplay();
            UpdateRenderMode(panelSettings.renderMode);
            return m_RootVisualElement;
        }
    }
}
