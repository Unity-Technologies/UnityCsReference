// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEditor.Rendering;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [Overlay(typeof(SceneView), k_OverlayID, "Light Settings")]
    [Icon("Icons/Exposure.png")]
    class SceneViewLighting : Overlay, ITransientOverlay
    {
        const string k_OverlayID = "Scene View/Light Settings";

        const string k_UnityHiddenClass = "unity-pbr-validation-hidden";
        const string k_UnityBaseFieldClass = "unity-base-field";
        const string k_SliderRowClass = "unity-pbr-slider-row";
        const string k_SliderClass = "unity-pbr-slider";
        const string k_SliderFloatFieldClass = "unity-pbr-slider-float-field";

        static readonly PrefColor kSceneViewMaterialValidateLow = new PrefColor("Scene/Material Validator Value Too Low", 255.0f / 255.0f, 0.0f, 0.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialValidateHigh = new PrefColor("Scene/Material Validator Value Too High", 0.0f, 0.0f, 255.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialValidatePureMetal = new PrefColor("Scene/Material Validator Pure Metal", 255.0f / 255.0f, 255.0f / 255.0f, 0.0f, 1.0f);

        static readonly PrefColor kSceneViewMaterialNoContributeGI = new PrefColor("Scene/Contribute GI: Off / Receive GI: Light Probes", 229.0f / 255.0f, 203.0f / 255.0f, 132.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialReceiveGILightmaps = new PrefColor("Scene/Contribute GI: On / Receive GI: Lightmaps", 89.0f / 255.0f, 148.0f / 255.0f, 161.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialReceiveGILightProbes = new PrefColor("Scene/Contribute GI: On / Receive GI: Light Probes", 221.0f / 255.0f, 115.0f / 255.0f, 91.0f / 255.0f, 1.0f);

        SceneView m_SceneView;
        SceneView sceneView => m_SceneView;

        List<AlbedoSwatchInfo> m_AlbedoSwatchInfos;
        AlbedoSwatchInfo m_SelectedAlbedoSwatch;

        VisualElement m_ContentRoot;

        // Validate albedo / metals
        const float k_AlbedoHueToleranceMin = 0f;
        const float k_AlbedoHueToleranceMax = .5f;
        VisualElement m_AlbedoContent;
        VisualElement m_AlbedoHueTolerance;
        VisualElement m_AlbedoSaturationTolerance;
        PopupField<AlbedoSwatchInfo> m_SelectedAlbedoPopup;
        float m_AlbedoSwatchHueTolerance = .1f;
        float m_AlbedoSwatchSaturationTolerance = .2f;
        ColorSpace m_LastKnownColorSpace = ColorSpace.Uninitialized;

        // Light exposure
        VisualElement m_GlobalIlluminationContent;
        VisualElement m_LightExposureContent;
        Slider m_ExposureSlider;
        FloatField m_ExposureField;
        const float k_ExposureSliderAbsoluteMax = 23.0f;
        float m_ExposureMax = 16f;
        static Texture2D s_ExposureTexture, s_EmptyExposureTexture;

        bool m_ShouldDisplay;

        public bool visible => m_ShouldDisplay;

        public SceneViewLighting()
        {
            CreateAlbedoSwatchData();
            collapsedChanged += OnCollapsedChanged;
        }

        public override void OnCreated()
        {
            if (!(containerWindow is SceneView view))
                throw new Exception("PBR Validation Overlay is only valid in the Scene View");

            m_ShouldDisplay = false;
            m_SceneView = view;
            m_SceneView.onCameraModeChanged += OnCameraModeChanged;
        }

        public override void OnWillBeDestroyed()
        {
            m_SceneView.onCameraModeChanged -= OnCameraModeChanged;
        }

        void OnCollapsedChanged(bool collapsed)
        {
            if (collapsed)
                SceneView.duringSceneGui -= UpdateGizmoExposure;
            else
                SceneView.duringSceneGui += UpdateGizmoExposure;
        }

        public override VisualElement CreatePanelContent()
        {
            if (!(containerWindow is SceneView view))
                return new Label("PBR Validation Overlay is only valid in the Scene View");

            m_SceneView = view;

            m_ContentRoot = new VisualElement();
            m_ContentRoot.Add(m_AlbedoContent = CreateAlbedoContent());
            m_ContentRoot.Add(m_GlobalIlluminationContent = CreateGIContent());
            m_ContentRoot.Add(m_LightExposureContent = CreateLightExposureContent());

            OnCameraModeChanged(sceneView.cameraMode);

            return m_ContentRoot;
        }

        void OnCameraModeChanged(SceneView.CameraMode mode)
        {
            // If the rootVisualElement hasn't yet been created, early out
            if (m_ContentRoot == null)
                CreatePanelContent();

            m_AlbedoContent.EnableInClassList(k_UnityHiddenClass, mode.drawMode != DrawCameraMode.ValidateAlbedo && mode.drawMode != DrawCameraMode.ValidateMetalSpecular);
            m_AlbedoContent.Q("Albedo").EnableInClassList(k_UnityHiddenClass, mode.drawMode != DrawCameraMode.ValidateAlbedo);
            m_LightExposureContent.EnableInClassList(k_UnityHiddenClass, !sceneView.showExposureSettings);
            m_GlobalIlluminationContent.EnableInClassList(k_UnityHiddenClass, mode.drawMode != DrawCameraMode.GIContributorsReceivers);
            m_ShouldDisplay = true;

            switch (mode.drawMode)
            {
                case DrawCameraMode.ValidateAlbedo:
                case DrawCameraMode.ValidateMetalSpecular:
                    displayName = "PBR Validation Settings";
                    m_AlbedoContent.Q<HelpBox>().EnableInClassList(k_UnityHiddenClass, PlayerSettings.colorSpace != ColorSpace.Gamma);
                    m_SceneView.SetOverlayVisible(k_OverlayID, true);
                    break;

                case DrawCameraMode.BakedEmissive:
                case DrawCameraMode.BakedLightmap:
                case DrawCameraMode.RealtimeEmissive:
                case DrawCameraMode.RealtimeIndirect:
                    displayName = "Lightmap Exposure";
                    m_SceneView.SetOverlayVisible(k_OverlayID, true);
                    break;

                case DrawCameraMode.GIContributorsReceivers:
                    displayName = "GI Contributors / Receivers";
                    break;

                default:
                    m_ShouldDisplay = false;
                    break;
            }

            UpdateAlbedoMetalValidation();
            Handles.SetSceneViewModeGIContributorsReceiversColors(
                kSceneViewMaterialNoContributeGI.Color,
                kSceneViewMaterialReceiveGILightmaps.Color,
                kSceneViewMaterialReceiveGILightProbes.Color);
        }

        VisualElement CreateAlbedoContent()
        {
            var root = new VisualElement() { name = "Albedo and Metals" };

            var validateTrueMetals = new Toggle("Check Pure Metals");
            validateTrueMetals.SetValueWithoutNotify(sceneView.validateTrueMetals);
            validateTrueMetals.RegisterValueChangedCallback(ValidateTrueMetalsChanged);
            validateTrueMetals.tooltip = "Check if albedo is black for materials with an average specular color above 0.45";
            root.Add(validateTrueMetals);

            var albedoSpecificContent = new VisualElement() { name = "Albedo" };

            if (PlayerSettings.colorSpace == ColorSpace.Gamma)
                albedoSpecificContent.Add(new HelpBox("Albedo Validation doesn't work when Color Space is set to gamma space",
                    HelpBoxMessageType.Warning));

            m_SelectedAlbedoSwatch = m_AlbedoSwatchInfos[0];
            m_SelectedAlbedoPopup = new PopupField<AlbedoSwatchInfo>("Luminance Validation", m_AlbedoSwatchInfos, m_SelectedAlbedoSwatch);
            m_SelectedAlbedoPopup.tooltip = "Select default luminance validation or validate against a configured albedo swatch";
            m_SelectedAlbedoPopup.formatListItemCallback = swatch => swatch.name;
            m_SelectedAlbedoPopup.formatSelectedValueCallback = swatch => swatch.name;
            m_SelectedAlbedoPopup.RegisterValueChangedCallback(SetSelectedAlbedoSwatch);

            albedoSpecificContent.Add(m_SelectedAlbedoPopup);
            albedoSpecificContent.Add(m_AlbedoContent = CreateColorSwatch("magenta", Color.magenta));

            var hue = EditorGUIUtility.TrTextContent("Hue Tolerance:", "Check that the hue of the albedo value of a " +
                "material is within the tolerance of the hue of the albedo swatch being validated against");
            var sat = EditorGUIUtility.TrTextContent("Saturation Tolerance:", "Check that the saturation of the albedo " +
                "value of a material is within the tolerance of the saturation of the albedo swatch being validated against");

            m_AlbedoHueTolerance = CreateSliderWithField(hue, m_AlbedoSwatchHueTolerance, k_AlbedoHueToleranceMin, k_AlbedoHueToleranceMax, SetAlbedoHueTolerance);
            albedoSpecificContent.Add(m_AlbedoHueTolerance);

            m_AlbedoSaturationTolerance = CreateSliderWithField(sat, m_AlbedoSwatchSaturationTolerance, k_AlbedoHueToleranceMin, k_AlbedoHueToleranceMax, SetSaturationTolerance);
            albedoSpecificContent.Add(m_AlbedoSaturationTolerance);

            root.Add(albedoSpecificContent);
            root.Add(CreateColorLegend());

            return root;
        }

        void SetAlbedoHueTolerance(ChangeEvent<float> evt)
        {
            m_AlbedoSwatchHueTolerance = Mathf.Clamp(evt.newValue, k_AlbedoHueToleranceMin, k_AlbedoHueToleranceMax);
            var slider = m_AlbedoHueTolerance.Q<Slider>();
            var field = m_AlbedoHueTolerance.Q<FloatField>();
            slider.SetValueWithoutNotify(m_AlbedoSwatchHueTolerance);
            if (evt.target != field || evt.newValue != 0)
                field.SetValueWithoutNotify(m_AlbedoSwatchHueTolerance);
            UpdateAlbedoSwatch();
        }

        void SetSaturationTolerance(ChangeEvent<float> evt)
        {
            m_AlbedoSwatchSaturationTolerance = Mathf.Clamp(evt.newValue, k_AlbedoHueToleranceMin, k_AlbedoHueToleranceMax);
            var slider = m_AlbedoSaturationTolerance.Q<Slider>();
            var field = m_AlbedoSaturationTolerance.Q<FloatField>();
            slider.SetValueWithoutNotify(m_AlbedoSwatchSaturationTolerance);
            if (evt.target != field || evt.newValue != 0)
                field.SetValueWithoutNotify(m_AlbedoSwatchSaturationTolerance);
            UpdateAlbedoSwatch();
        }

        VisualElement CreateGIContent()
        {
            var root = new VisualElement() { name = "GI Content" };

            string contributeGIOff = "Contribute GI: Off / Receive GI: Light Probes";
            string receiveGILightmaps = "Contribute GI: On / Receive GI: Lightmaps";
            string receiveGILightProbes = "Contribute GI: On / Receive GI: Light Probes";

            // string contributeGIOff = L10r.Tr("Contribute GI: Off / Receive GI: Light Probes");
            // string receiveGILightmaps = L10r.Tr("Contribute GI: On / Receive GI: Lightmaps");
            // string receiveGILightProbes = L10r.Tr("Contribute GI: On / Receive GI: Light Probes");

            root.Add(CreateColorSwatch(contributeGIOff, kSceneViewMaterialNoContributeGI));
            root.Add(CreateColorSwatch(receiveGILightmaps, kSceneViewMaterialReceiveGILightmaps));
            root.Add(CreateColorSwatch(receiveGILightProbes, kSceneViewMaterialReceiveGILightProbes));

            return root;
        }

        VisualElement CreateLightExposureContent()
        {
            var exposure = SceneView.s_DrawModeExposure;

            var root = new VisualElement();
            root.AddToClassList(k_SliderRowClass);

            var icon = new Image() { name = "exposure-label" };
            m_ExposureSlider = new Slider(-m_ExposureMax, m_ExposureMax) { name = "exposure-slider" };
            m_ExposureSlider.AddToClassList(k_SliderClass);
            m_ExposureSlider.RegisterValueChangedCallback(SetBakedExposure);
            m_ExposureSlider.SetValueWithoutNotify(exposure);

            m_ExposureField = new FloatField() { name = "exposure-float-field" };
            m_ExposureField.AddToClassList(k_SliderFloatFieldClass);
            m_ExposureField.RegisterValueChangedCallback(SetBakedExposure);
            m_ExposureField.SetValueWithoutNotify(exposure);

            root.Add(icon);
            root.Add(m_ExposureSlider);
            root.Add(m_ExposureField);
            return root;
        }

        void SetBakedExposure(ChangeEvent<float> evt)
        {
            var value = Mathf.Clamp(evt.newValue, -k_ExposureSliderAbsoluteMax, k_ExposureSliderAbsoluteMax);

            // This will allow the user to set a new max value for the current session
            if (Mathf.Abs(value) > m_ExposureMax)
            {
                m_ExposureMax = Mathf.Min(k_ExposureSliderAbsoluteMax, Mathf.Abs(value));
                m_ExposureSlider.lowValue = -m_ExposureMax;
                m_ExposureSlider.highValue = m_ExposureMax;
            }

            m_ExposureSlider.SetValueWithoutNotify(value);
            if (evt.target != m_ExposureField || evt.newValue != 0)
                m_ExposureField.SetValueWithoutNotify(value);

            SceneView.s_DrawModeExposure.value = value;
        }

        VisualElement CreateSliderWithField(GUIContent label, float value, float min, float max, EventCallback<ChangeEvent<float>> callback)
        {
            var root = new VisualElement() { name = "Slider Float Field" };
            root.AddToClassList(k_SliderRowClass);

            var slider = new Slider(min, max);
            slider.AddToClassList(k_SliderClass);
            slider.RegisterValueChangedCallback(callback);
            slider.SetValueWithoutNotify(value);

            if (label != null && !string.IsNullOrEmpty(label.text))
            {
                slider.label = label.text;
                slider.tooltip = label.tooltip;
            }

            var field = new FloatField();
            field.AddToClassList(k_SliderFloatFieldClass);
            field.RegisterValueChangedCallback(callback);
            field.SetValueWithoutNotify(value);

            root.Add(slider);
            root.Add(field);

            return root;
        }

        VisualElement CreateColorLegend()
        {
            var root = new VisualElement() { name = "Color Legend" };

            root.Add(new Label("Color Legend"));

            string modeString = m_SceneView.cameraMode.drawMode == DrawCameraMode.ValidateAlbedo
                ? "Luminance"
                : "Specular";

            root.Add(CreateColorSwatch($"Below Minimum {modeString} Value", kSceneViewMaterialValidateLow.Color));
            root.Add(CreateColorSwatch($"Above Maximum {modeString} Value", kSceneViewMaterialValidateHigh.Color));
            root.Add(CreateColorSwatch($"Not A Pure Metal", kSceneViewMaterialValidatePureMetal.Color));

            return root;
        }

        VisualElement CreateColorSwatch(string label, Color color)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            row.AddToClassList(k_UnityBaseFieldClass);

            var swatchContainer = new VisualElement();
            swatchContainer.AddToClassList("unity-base-field__label");
            swatchContainer.AddToClassList("unity-pbr-validation-color-swatch");

            var colorContent = new VisualElement() { name = "color-content" };
            colorContent.style.backgroundColor = new StyleColor(color);
            swatchContainer.Add(colorContent);
            row.Add(swatchContainer);

            var colorLabel = new Label(label) { name = "color-label" };
            colorLabel.AddToClassList("unity-base-field__label");
            row.Add(colorLabel);
            return row;
        }

        void CreateAlbedoSwatchData()
        {
            AlbedoSwatchInfo[] graphicsSettingsSwatches = EditorGraphicsSettings.albedoSwatches;

            if (graphicsSettingsSwatches.Length != 0)
            {
                m_AlbedoSwatchInfos = new List<AlbedoSwatchInfo>(graphicsSettingsSwatches);
            }
            else
            {
                m_AlbedoSwatchInfos = new List<AlbedoSwatchInfo>()
                {
                    // colors taken from http://www.babelcolor.com/index_htm_files/ColorChecker_RGB_and_spectra.xls
                    new AlbedoSwatchInfo()
                    {
                        name = "Black Acrylic Paint",
                        color = new Color(56f / 255f, 56f / 255f, 56f / 255f),
                        minLuminance = 0.03f,
                        maxLuminance = 0.07f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Dark Soil",
                        color = new Color(85f / 255f, 61f / 255f, 49f / 255f),
                        minLuminance = 0.05f,
                        maxLuminance = 0.14f
                    },

                    new AlbedoSwatchInfo()
                    {
                        name = "Worn Asphalt",
                        color = new Color(91f / 255f, 91f / 255f, 91f / 255f),
                        minLuminance = 0.10f,
                        maxLuminance = 0.15f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Dry Clay Soil",
                        color = new Color(137f / 255f, 120f / 255f, 102f / 255f),
                        minLuminance = 0.15f,
                        maxLuminance = 0.35f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Green Grass",
                        color = new Color(123f / 255f, 131f / 255f, 74f / 255f),
                        minLuminance = 0.16f,
                        maxLuminance = 0.26f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Old Concrete",
                        color = new Color(135f / 255f, 136f / 255f, 131f / 255f),
                        minLuminance = 0.17f,
                        maxLuminance = 0.30f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Red Clay Tile",
                        color = new Color(197f / 255f, 125f / 255f, 100f / 255f),
                        minLuminance = 0.23f,
                        maxLuminance = 0.33f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Dry Sand",
                        color = new Color(177f / 255f, 167f / 255f, 132f / 255f),
                        minLuminance = 0.20f,
                        maxLuminance = 0.45f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "New Concrete",
                        color = new Color(185f / 255f, 182f / 255f, 175f / 255f),
                        minLuminance = 0.32f,
                        maxLuminance = 0.55f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "White Acrylic Paint",
                        color = new Color(227f / 255f, 227f / 255f, 227f / 255f),
                        minLuminance = 0.75f,
                        maxLuminance = 0.85f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Fresh Snow",
                        color = new Color(243f / 255f, 243f / 255f, 243f / 255f),
                        minLuminance = 0.85f,
                        maxLuminance = 0.95f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Blue Sky",
                        color = new Color(93f / 255f, 123f / 255f, 157f / 255f),
                        minLuminance = new Color(93f / 255f, 123f / 255f, 157f / 255f).linear.maxColorComponent - 0.05f,
                        maxLuminance = new Color(93f / 255f, 123f / 255f, 157f / 255f).linear.maxColorComponent + 0.05f
                    },
                    new AlbedoSwatchInfo()
                    {
                        name = "Foliage",
                        color = new Color(91f / 255f, 108f / 255f, 65f / 255f),
                        minLuminance = new Color(91f / 255f, 108f / 255f, 65f / 255f).linear.maxColorComponent - 0.05f,
                        maxLuminance = new Color(91f / 255f, 108f / 255f, 65f / 255f).linear.maxColorComponent + 0.05f
                    },
                };
            }

            m_AlbedoSwatchInfos.Insert(0, new AlbedoSwatchInfo()
            {
                name = "Default Luminance",
                color = Color.gray,
                minLuminance = 0.012f,
                maxLuminance = 0.9f
            });
        }

        void UpdateAlbedoMetalValidation()
        {
            if (PlayerSettings.colorSpace != m_LastKnownColorSpace)
                UpdatePBRColorLegend();
            UpdateAlbedoSwatch();
            SelectedAlbedoSwatchChanged();
        }

        void UpdatePBRColorLegend()
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                Shader.SetGlobalColor("unity_MaterialValidateLowColor", kSceneViewMaterialValidateLow.Color.linear);
                Shader.SetGlobalColor("unity_MaterialValidateHighColor", kSceneViewMaterialValidateHigh.Color.linear);
                Shader.SetGlobalColor("unity_MaterialValidatePureMetalColor", kSceneViewMaterialValidatePureMetal.Color.linear);
            }
            else
            {
                Shader.SetGlobalColor("unity_MaterialValidateLowColor", kSceneViewMaterialValidateLow.Color);
                Shader.SetGlobalColor("unity_MaterialValidateHighColor", kSceneViewMaterialValidateHigh.Color);
                Shader.SetGlobalColor("unity_MaterialValidatePureMetalColor", kSceneViewMaterialValidatePureMetal.Color);
            }

            m_LastKnownColorSpace = PlayerSettings.colorSpace;
        }

        void UpdateAlbedoSwatch()
        {
            var color = m_SelectedAlbedoSwatch.color;
            Shader.SetGlobalFloat("_AlbedoMinLuminance", m_SelectedAlbedoSwatch.minLuminance);
            Shader.SetGlobalFloat("_AlbedoMaxLuminance", m_SelectedAlbedoSwatch.maxLuminance);
            Shader.SetGlobalFloat("_AlbedoHueTolerance", m_AlbedoSwatchHueTolerance);
            Shader.SetGlobalFloat("_AlbedoSaturationTolerance", m_AlbedoSwatchSaturationTolerance);

            Shader.SetGlobalColor("_AlbedoCompareColor", color.linear);
            Shader.SetGlobalFloat("_CheckAlbedo", (m_SelectedAlbedoPopup.index != 0) ? 1.0f : 0.0f);
            Shader.SetGlobalFloat("_CheckPureMetal", m_SceneView.validateTrueMetals ? 1.0f : 0.0f);
        }

        void SelectedAlbedoSwatchChanged()
        {
            var color = m_AlbedoContent.Q("color-content");
            var label = m_AlbedoContent.Q<Label>("color-label");

            bool colorCorrect = PlayerSettings.colorSpace == ColorSpace.Linear;
            color.style.backgroundColor = colorCorrect
                ? m_SelectedAlbedoSwatch.color.gamma
                : m_SelectedAlbedoSwatch.color;
            float minLum = m_SelectedAlbedoSwatch.minLuminance;
            float maxLum = m_SelectedAlbedoSwatch.maxLuminance;
            label.text = $"Luminance ({minLum:F2} - {maxLum:F2})";
            UpdateAlbedoSwatch();
        }

        void SetSelectedAlbedoSwatch(ChangeEvent<AlbedoSwatchInfo> evt)
        {
            m_SelectedAlbedoSwatch = evt.newValue;
            SelectedAlbedoSwatchChanged();
        }

        void ValidateTrueMetalsChanged(ChangeEvent<bool> evt)
        {
            m_SceneView.validateTrueMetals = evt.newValue;
        }

        void UpdateGizmoExposure(SceneView view)
        {
            if (m_SceneView != view)
                return;

            if (m_SceneView.showExposureSettings)
            {
                if (s_ExposureTexture == null)
                {
                    s_ExposureTexture = new Texture2D(1, 1, GraphicsFormat.R32G32_SFloat, TextureCreationFlags.None);
                    s_ExposureTexture.hideFlags = HideFlags.HideAndDontSave;
                }

                s_ExposureTexture.SetPixel(0, 0, new Color(Mathf.Pow(2.0f, SceneView.s_DrawModeExposure), 0.0f, 0.0f));
                s_ExposureTexture.Apply();

                Gizmos.exposure = s_ExposureTexture;
            }
            else
            {
                if (s_EmptyExposureTexture == null)
                {
                    s_EmptyExposureTexture = new Texture2D(1, 1, GraphicsFormat.R32G32_SFloat, TextureCreationFlags.None);
                    s_EmptyExposureTexture.hideFlags = HideFlags.HideAndDontSave;
                    s_EmptyExposureTexture.SetPixel(0, 0, new Color(1.0f, 0.0f, 0.0f));
                    s_EmptyExposureTexture.Apply();
                }

                Gizmos.exposure = s_EmptyExposureTexture;
            }
        }
    }
}
