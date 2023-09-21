// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [Overlay(typeof(SceneView), k_OverlayID, "Lighting Visualization", defaultDockZone = DockZone.RightColumn)]
    [Icon("Icons/LightingVisualization.png")]
    class SceneViewLighting : Overlay, ITransientOverlay
    {
        internal static class Styles
        {
            internal const string k_UnityHiddenClass = "unity-pbr-validation-hidden";
            internal const string k_UnityBaseFieldClass = "unity-base-field";
            internal const string k_SliderRowClass = "unity-pbr-slider-row";
            internal const string k_SliderClass = "unity-pbr-slider";
            internal const string k_SliderFloatFieldClass = "unity-pbr-slider-float-field";
            internal const string k_SwatchColorContent = "color-content";
            internal const string k_ToggleClass = "unity-lighting-visualization-toggle";
            internal const string k_ToggleLabelClass = "unity-lighting-visualization-toggle-label";
        }

        const string k_OverlayID = "Scene View/Light Settings";

        bool m_ShouldDisplay;
        public bool visible => m_ShouldDisplay;

        SceneView m_SceneView;
        SceneView sceneView => m_SceneView;

        VisualElement m_ContentRoot;

        // Interactive preview toggle
        VisualElement m_InteractiveBakingContent;

        // Draw lightmap resolution
        VisualElement m_LightmapResolutionContent;

        // Draw backface highlights
        VisualElement m_BackfaceHighlightsContent;

        // Light exposure
        VisualElement m_LightExposureContent;
        Slider m_ExposureSlider;
        FloatField m_ExposureField;
        const float k_ExposureSliderAbsoluteMax = 23.0f;
        float m_ExposureMax = 16f;
        static Texture2D s_ExposureTexture, s_EmptyExposureTexture;

        public SceneViewLighting()
        {
            collapsedChanged += OnCollapsedChanged;
        }

        public override void OnCreated()
        {
            if (!(containerWindow is SceneView view))
                throw new Exception("Lighting Visualization Overlay is only valid in the Scene View");

            m_ShouldDisplay = false;
            m_SceneView = view;
            m_SceneView.onCameraModeChanged += OnCameraModeChanged;
            m_SceneView.debugDrawModesUseInteractiveLightBakingDataChanged += OnUseInteractiveLightBakingDataChanged;
            Lightmapping.bakeStarted += OnBakeStarted;
            Lightmapping.bakeCompleted += OnBakeCompleted;
            Lightmapping.bakeCancelled += OnBakeCompleted;
        }

        public override void OnWillBeDestroyed()
        {
            m_SceneView.onCameraModeChanged -= OnCameraModeChanged;
            m_SceneView.debugDrawModesUseInteractiveLightBakingDataChanged -= OnUseInteractiveLightBakingDataChanged;
            Lightmapping.bakeStarted -= OnBakeStarted;
            Lightmapping.bakeCompleted -= OnBakeCompleted;
            Lightmapping.bakeCancelled -= OnBakeCompleted;
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
                return new Label("Lighting Visualization Overlay is only valid in the Scene View");

            m_SceneView = view;

            m_ContentRoot = new VisualElement();
            m_ContentRoot.Add(m_InteractiveBakingContent = CreateInteractiveBakingContent());
            m_ContentRoot.Add(m_LightmapResolutionContent = CreateLightmapResolutionContent());
            m_ContentRoot.Add(m_BackfaceHighlightsContent = CreateBackfaceHighlightsContent());
            m_ContentRoot.Add(m_LightExposureContent = CreateLightExposureContent());

            OnCameraModeChanged(sceneView.cameraMode);

            return m_ContentRoot;
        }

        void OnCameraModeChanged(SceneView.CameraMode mode)
        {
            // If the rootVisualElement hasn't yet been created, early out
            if (m_ContentRoot == null)
                CreatePanelContent();

            m_InteractiveBakingContent.EnableInClassList(Styles.k_UnityHiddenClass, !sceneView.currentDrawModeMayUseInteractiveLightBakingData);
            m_LightmapResolutionContent.EnableInClassList(Styles.k_UnityHiddenClass, !sceneView.showLightmapResolutionToggle);
            m_BackfaceHighlightsContent.EnableInClassList(Styles.k_UnityHiddenClass, !sceneView.showBackfaceHighlightsToggle);
            m_LightExposureContent.EnableInClassList(Styles.k_UnityHiddenClass, !sceneView.showExposureSettings);

            if (sceneView.showLightingVisualizationPanel)
            {
                m_SceneView.SetOverlayVisible(k_OverlayID, true);
                m_ShouldDisplay = true;
            }
            else
            {
                m_ShouldDisplay = false;
            }
        }

        void OnUseInteractiveLightBakingDataChanged(bool useInteractiveLightBakingDataChanged)
        {
            m_InteractiveBakingContent?.Q<EnumField>()?.SetValueWithoutNotify(useInteractiveLightBakingDataChanged ? LightingDataSource.Preview : LightingDataSource.Baked);
        }

        void OnBakeStarted()
        {
            m_InteractiveBakingContent?.Q<EnumField>()?.SetEnabled(false);
        }

        void OnBakeCompleted()
        {
            m_InteractiveBakingContent?.Q<EnumField>()?.SetEnabled(true);
        }

        enum LightingDataSource
        {
            Baked,
            Preview
        }

        VisualElement CreateInteractiveBakingContent()
        {
            var useInteractiveLightBakingDataChanged = SceneView.lastActiveSceneView?.debugDrawModesUseInteractiveLightBakingData ?? false;

            var root = new VisualElement();

            var dropdown = new EnumField("Lighting Data", LightingDataSource.Baked);
            dropdown.tooltip = "Select which lighting data is shown in Debug Draw Modes.\n\nBaked displays the most recent lighting data generated from the Lighting Window.\n\nPreview displays an interactive preview which updates in relation to Scene changes.";
            dropdown.RegisterValueChangedCallback((evt) =>
            {
                bool isInteractive = (LightingDataSource)evt.newValue == LightingDataSource.Preview;
                SceneView.lastActiveSceneView.debugDrawModesUseInteractiveLightBakingData = isInteractive;
            });

            dropdown.labelElement.AddToClassList(Styles.k_ToggleLabelClass);
            dropdown.visualInput.AddToClassList(Styles.k_SliderFloatFieldClass);

            dropdown.SetEnabled(!Lightmapping.isRunning);

            dropdown.SetValueWithoutNotify(useInteractiveLightBakingDataChanged ? LightingDataSource.Preview : LightingDataSource.Baked);

            root.Add(dropdown);
            return root;
        }

        VisualElement CreateLightmapResolutionContent()
        {
            var showResolution = LightmapVisualization.showResolution;

            var root = new VisualElement();
            root.tooltip = "Draw lightmap texel density as an overlay in this Scene View Mode.";

            var toggle = new Toggle { label = "Show Lightmap Resolution", name = "lightmap-resolution-toggle" };
            toggle.labelElement.AddToClassList(Styles.k_ToggleLabelClass);
            toggle.visualInput.AddToClassList(Styles.k_ToggleClass);
            toggle.RegisterValueChangedCallback((ChangeEvent<bool> evt) => LightmapVisualization.showResolution = evt.newValue);
            toggle.SetValueWithoutNotify(LightmapVisualization.showResolution);

            root.Add(toggle);
            return root;
        }

        VisualElement CreateBackfaceHighlightsContent()
        {
            var backfaceHighlights = SceneView.GetDrawBackfaceHighlights();

            var root = new VisualElement();
            root.tooltip = "Highlight back-facing geometry using debug colors.";

            var toggle = new Toggle { label = "Highlight Back-Facing Geometry", name = "backface-highlights-toggle" };
            toggle.labelElement.AddToClassList(Styles.k_ToggleLabelClass);
            toggle.visualInput.AddToClassList(Styles.k_ToggleClass);
            toggle.RegisterValueChangedCallback((ChangeEvent<bool> evt) => SceneView.SetDrawBackfaceHighlights(evt.newValue));
            toggle.SetValueWithoutNotify(backfaceHighlights);

            root.Add(toggle);
            return root;
        }

        VisualElement CreateLightExposureContent()
        {
            var exposure = SceneView.s_DrawModeExposure;

            var root = new VisualElement();
            root.tooltip = "Adjust Lightmap Exposure";
            root.AddToClassList(Styles.k_SliderRowClass);

            var icon = new Image() { name = "exposure-label" };
            m_ExposureSlider = new Slider(-m_ExposureMax, m_ExposureMax) { name = "exposure-slider" };
            m_ExposureSlider.AddToClassList(Styles.k_SliderClass);
            m_ExposureSlider.RegisterValueChangedCallback(SetBakedExposure);
            m_ExposureSlider.SetValueWithoutNotify(exposure);

            m_ExposureField = new FloatField() { name = "exposure-float-field" };
            m_ExposureField.AddToClassList(Styles.k_SliderFloatFieldClass);
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

        void UpdateGizmoExposure(SceneView view)
        {
            if (m_SceneView != view)
                return;

            if (m_SceneView.showExposureSettings || m_SceneView.cameraMode.drawMode == DrawCameraMode.LitClustering)
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

    [Overlay(typeof(SceneView), k_OverlayID, "PBR Validation Settings", defaultDockZone = DockZone.RightColumn)]
    [Icon("Icons/Exposure.png")]
    class SceneViewLightingPBRValidation : Overlay, ITransientOverlay
    {
        const string k_OverlayID = "Scene View/PBR Validation Settings";

        bool m_ShouldDisplay;
        public bool visible => m_ShouldDisplay;

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

        public SceneViewLightingPBRValidation()
        {
            CreateAlbedoSwatchData();
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

        public override VisualElement CreatePanelContent()
        {
            if (!(containerWindow is SceneView view))
                return new Label("PBR Validation Overlay is only valid in the Scene View");

            m_SceneView = view;

            m_ContentRoot = new VisualElement();
            m_ContentRoot.Add(m_AlbedoContent = CreateAlbedoContent());

            OnCameraModeChanged(sceneView.cameraMode);

            return m_ContentRoot;
        }

        void OnCameraModeChanged(SceneView.CameraMode mode)
        {
            // If the rootVisualElement hasn't yet been created, early out
            if (m_ContentRoot == null)
                CreatePanelContent();

            bool showValidateAlbedo = mode.drawMode == DrawCameraMode.ValidateAlbedo;
            bool showPanel = showValidateAlbedo || mode.drawMode == DrawCameraMode.ValidateMetalSpecular;

            m_AlbedoContent.EnableInClassList(SceneViewLighting.Styles.k_UnityHiddenClass, !showPanel);
            m_AlbedoContent.Q("Albedo").EnableInClassList(SceneViewLighting.Styles.k_UnityHiddenClass, !showValidateAlbedo);
            m_ShouldDisplay = showPanel;

            if (m_ShouldDisplay)
            {
                m_AlbedoContent.Q<HelpBox>()?.EnableInClassList(SceneViewLighting.Styles.k_UnityHiddenClass, PlayerSettings.colorSpace != ColorSpace.Gamma);
                m_SceneView.SetOverlayVisible(k_OverlayID, true);
            }

            UpdateAlbedoMetalValidation();
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
            albedoSpecificContent.Add(m_AlbedoContent = SceneViewLightingColors.CreateColorSwatch("magenta", null));

            var hue = EditorGUIUtility.TrTextContent("Hue Tolerance:", "Check that the hue of the albedo value of a " +
                "material is within the tolerance of the hue of the albedo swatch being validated against");
            var sat = EditorGUIUtility.TrTextContent("Saturation Tolerance:", "Check that the saturation of the albedo " +
                "value of a material is within the tolerance of the saturation of the albedo swatch being validated against");

            m_AlbedoHueTolerance = CreateSliderWithField(hue, m_AlbedoSwatchHueTolerance, k_AlbedoHueToleranceMin, k_AlbedoHueToleranceMax, SetAlbedoHueTolerance);
            albedoSpecificContent.Add(m_AlbedoHueTolerance);

            m_AlbedoSaturationTolerance = CreateSliderWithField(sat, m_AlbedoSwatchSaturationTolerance, k_AlbedoHueToleranceMin, k_AlbedoHueToleranceMax, SetSaturationTolerance);
            albedoSpecificContent.Add(m_AlbedoSaturationTolerance);

            root.Add(albedoSpecificContent);

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

        VisualElement CreateSliderWithField(GUIContent label, float value, float min, float max, EventCallback<ChangeEvent<float>> callback)
        {
            var root = new VisualElement() { name = "Slider Float Field" };
            root.AddToClassList(SceneViewLighting.Styles.k_SliderRowClass);

            var slider = new Slider(min, max);
            slider.AddToClassList(SceneViewLighting.Styles.k_SliderClass);
            slider.RegisterValueChangedCallback(callback);
            slider.SetValueWithoutNotify(value);

            if (label != null && !string.IsNullOrEmpty(label.text))
            {
                slider.label = label.text;
                slider.tooltip = label.tooltip;
            }

            var field = new FloatField();
            field.AddToClassList(SceneViewLighting.Styles.k_SliderFloatFieldClass);
            field.RegisterValueChangedCallback(callback);
            field.SetValueWithoutNotify(value);

            root.Add(slider);
            root.Add(field);

            return root;
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
            SelectedAlbedoSwatchChanged();
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
            var color = m_AlbedoContent.Q(SceneViewLighting.Styles.k_SwatchColorContent);
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
    }

    [Overlay(typeof(SceneView), k_OverlayID, "Lighting Visualization Colors", defaultDockZone = DockZone.LeftColumn)]
    [Icon("Icons/LightingVisualizationColors.png")]
    class SceneViewLightingColors : Overlay, ITransientOverlay
    {
        const string k_OverlayID = "Scene View/Lighting Visualization Colors";

        static readonly PrefColor kSceneViewMaterialValidateLow = new PrefColor("Scene/Material Validator Value Too Low", 255.0f / 255.0f, 0.0f, 0.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialValidateHigh = new PrefColor("Scene/Material Validator Value Too High", 0.0f, 0.0f, 255.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialValidatePureMetal = new PrefColor("Scene/Material Validator Pure Metal", 255.0f / 255.0f, 255.0f / 255.0f, 0.0f, 1.0f);

        static readonly PrefColor kSceneViewMaterialNoContributeGI = new PrefColor("Scene/Receive GI Only (Light Probes)", 230.0f / 255.0f, 99.0f / 255.0f, 25.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialReceiveGILightmaps = new PrefColor("Scene/Receive + Contribute GI (Lightmaps)", 47.0f / 255.0f, 153.0f / 255.0f, 41.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialReceiveGILightProbes = new PrefColor("Scene/Receive + Contribute GI (Light Probes)", 35.0f / 255.0f, 15.0f / 255.0f, 153.0f / 255.0f, 1.0f);

        static readonly PrefColor kSceneViewHighlightBackfaces = new PrefColor("Scene/Back-Facing Geometry", 126.0f / 255.0f, 46.0f / 255.0f, 217.0f / 255.0f, 1.0f);

        // Should match colors in BakePipelineBuildVisualization.cpp!
        static readonly Color kSceneViewInvalidTexel = new Color(247.0f / 255.0f, 79.0f / 255.0f, 65.0f / 255.0f, 255.0f / 255.0f);
        static readonly Color kSceneViewValidTexel = new Color(12.0f / 255.0f, 99.0f / 255.0f, 8.0f / 255.0f, 255.0f / 255.0f);
        static readonly Color kSceneViewOverlappingTexel = new Color(247.0f / 255.0f, 79.0f / 255.0f, 65.0f / 255.0f, 255.0f / 255.0f);
        static readonly Color kSceneViewNonOverlappingTexel = new Color(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);

        // material validation, contribute GI, and albedo swatches are represented in this array. it is used to update
        // the colors when preferences are changed.
        Dictionary<PrefColor, VisualElement> m_PrefColorSwatches = new Dictionary<PrefColor, VisualElement>();

        bool m_ShouldDisplay;
        public bool visible => m_ShouldDisplay;

        SceneView m_SceneView;
        SceneView sceneView => m_SceneView;

        VisualElement m_ContentRoot;

        // Backface highlights
        VisualElement m_BackfaceHighlightsContent;

        // Validate albedo / metals
        VisualElement m_AlbedoContent;
        ColorSpace m_LastKnownColorSpace = ColorSpace.Uninitialized;

        // Light exposure
        VisualElement m_GlobalIlluminationContent;

        // Debug view modes
        VisualElement m_TexelValidityContent;
        VisualElement m_UVOverlapContent;

        public SceneViewLightingColors()
        {
            PrefSettings.settingChanged += UpdatePrefColors;
        }

        public override void OnCreated()
        {
            if (!(containerWindow is SceneView view))
                throw new Exception("Lighting Visualization Colors Overlay is only valid in the Scene View");

            m_ShouldDisplay = false;
            m_SceneView = view;
            m_SceneView.onCameraModeChanged += OnCameraModeChanged;
            SceneView.onDrawBackfaceHighlightsChanged += OnDrawBackfaceHighlightsChanged;
        }

        public override void OnWillBeDestroyed()
        {
            m_SceneView.onCameraModeChanged -= OnCameraModeChanged;
            SceneView.onDrawBackfaceHighlightsChanged -= OnDrawBackfaceHighlightsChanged;
        }

        public override VisualElement CreatePanelContent()
        {
            if (!(containerWindow is SceneView view))
                return new Label("Lighting Visualization Colors Overlay is only valid in the Scene View");

            m_SceneView = view;

            m_ContentRoot = new VisualElement();
            m_ContentRoot.Add(m_BackfaceHighlightsContent = CreateBackfaceHighlightsContent());
            m_ContentRoot.Add(m_AlbedoContent = CreateAlbedoContent());
            m_ContentRoot.Add(m_GlobalIlluminationContent = CreateGIContent());
            m_ContentRoot.Add(m_TexelValidityContent = CreateTexelValidityContent());
            m_ContentRoot.Add(m_UVOverlapContent = CreateUVOverlapContent());

            OnCameraModeChanged(sceneView.cameraMode);

            return m_ContentRoot;
        }

        void OnDrawBackfaceHighlightsChanged(bool value)
        {
            OnCameraModeChanged(sceneView.cameraMode);
        }

        void OnCameraModeChanged(SceneView.CameraMode mode)
        {
            // If the rootVisualElement hasn't yet been created, early out
            if (m_ContentRoot == null)
                CreatePanelContent();

            bool showValidationLegend = mode.drawMode == DrawCameraMode.ValidateAlbedo || mode.drawMode == DrawCameraMode.ValidateMetalSpecular;
            bool showGIContributorsReceiversLegend = mode.drawMode == DrawCameraMode.GIContributorsReceivers;
            bool showTexelValidityLegend = mode.drawMode == DrawCameraMode.BakedTexelValidity;
            bool showUVOverlapLegend = mode.drawMode == DrawCameraMode.BakedUVOverlap;
            bool showBackfaceHighlightsLegend = sceneView.showBackfaceHighlightsToggle && SceneView.GetDrawBackfaceHighlights();
            bool showPanel = showValidationLegend || showGIContributorsReceiversLegend || showBackfaceHighlightsLegend || showTexelValidityLegend || showUVOverlapLegend;

            m_BackfaceHighlightsContent.EnableInClassList(SceneViewLighting.Styles.k_UnityHiddenClass, !showBackfaceHighlightsLegend);
            m_AlbedoContent.EnableInClassList(SceneViewLighting.Styles.k_UnityHiddenClass, !showValidationLegend);
            m_GlobalIlluminationContent.EnableInClassList(SceneViewLighting.Styles.k_UnityHiddenClass, !showGIContributorsReceiversLegend);
            m_TexelValidityContent.EnableInClassList(SceneViewLighting.Styles.k_UnityHiddenClass, !showTexelValidityLegend);
            m_UVOverlapContent.EnableInClassList(SceneViewLighting.Styles.k_UnityHiddenClass, !showUVOverlapLegend);

            if (showPanel)
            {
                m_SceneView.SetOverlayVisible(k_OverlayID, true);
                m_ShouldDisplay = true;
            }
            else
            {
                m_ShouldDisplay = false;
            }

            if (PlayerSettings.colorSpace != m_LastKnownColorSpace)
                UpdateColorLegend();
        }

        VisualElement CreateBackfaceHighlightsContent()
        {
            var root = new VisualElement() { name = "Back-Facing Geometry Content" };
            
            root.Add(m_PrefColorSwatches[kSceneViewHighlightBackfaces] = CreateColorSwatch("Back-Facing Geometry", kSceneViewHighlightBackfaces));

            return root;
        }

        VisualElement CreateGIContent()
        {
            var root = new VisualElement() { name = "GI Content" };

            string contributeGIOff = "Receive GI Only (Light Probes)";
            string receiveGILightmaps = "Receive + Contribute GI (Lightmaps)";
            string receiveGILightProbes = "Receive + Contribute GI (Light Probes)";

            root.Add(m_PrefColorSwatches[kSceneViewMaterialNoContributeGI] = CreateColorSwatch(contributeGIOff, kSceneViewMaterialNoContributeGI));
            root.Add(m_PrefColorSwatches[kSceneViewMaterialReceiveGILightmaps] = CreateColorSwatch(receiveGILightmaps, kSceneViewMaterialReceiveGILightmaps));
            root.Add(m_PrefColorSwatches[kSceneViewMaterialReceiveGILightProbes] = CreateColorSwatch(receiveGILightProbes, kSceneViewMaterialReceiveGILightProbes));

            return root;
        }

        VisualElement CreateAlbedoContent()
        {
            var root = new VisualElement() { name = "Albedo Content" };

            string modeString = m_SceneView.cameraMode.drawMode == DrawCameraMode.ValidateAlbedo
                ? "Luminance"
                : "Specular";

            root.Add(m_PrefColorSwatches[kSceneViewMaterialValidateLow] = CreateColorSwatch($"Below Minimum {modeString} Value", kSceneViewMaterialValidateLow));
            root.Add(m_PrefColorSwatches[kSceneViewMaterialValidateHigh] = CreateColorSwatch($"Above Maximum {modeString} Value", kSceneViewMaterialValidateHigh));
            root.Add(m_PrefColorSwatches[kSceneViewMaterialValidatePureMetal] = CreateColorSwatch($"Not A Pure Metal", kSceneViewMaterialValidatePureMetal));

            return root;
        }

        VisualElement CreateTexelValidityContent()
        {
            var root = new VisualElement() { name = "Texel Validity Content" };

            root.Add(CreateColorSwatch("Invalid Texels", null, kSceneViewInvalidTexel));
            root.Add(CreateColorSwatch("Valid Texels", null, kSceneViewValidTexel));

            return root;
        }

        VisualElement CreateUVOverlapContent()
        {
            var root = new VisualElement() { name = "UV Overlap Content" };

            root.Add(CreateColorSwatch("Overlapping Texels", null, kSceneViewOverlappingTexel));
            root.Add(CreateColorSwatch("Non-Overlapping Texels", null, kSceneViewNonOverlappingTexel));

            return root;
        }

        internal static VisualElement CreateColorSwatch(string label, PrefColor prefColor) => CreateColorSwatch(label, prefColor, Color.magenta);

        internal static VisualElement CreateColorSwatch(string label, PrefColor prefColor, Color fallbackColor)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginLeft = 2 } };
            row.AddToClassList(SceneViewLighting.Styles.k_UnityBaseFieldClass);

            var swatchContainer = new VisualElement();
            swatchContainer.AddToClassList("unity-base-field__label");
            swatchContainer.AddToClassList("unity-pbr-validation-color-swatch");

            var colorContent = new VisualElement() { name = SceneViewLighting.Styles.k_SwatchColorContent };
            if (prefColor == null)
            {
                colorContent.style.backgroundColor = new StyleColor(fallbackColor);
            }
            else
            {
                colorContent.style.backgroundColor = new StyleColor(prefColor);
                colorContent.AddManipulator(new Clickable(() =>
                {
                    ColorPicker.Show((c) =>
                    {
                        prefColor.Color = c;
                        PrefSettings.Set(prefColor.Name, prefColor);
                    },
                    prefColor.Color, false, false);
                }));
            }
            swatchContainer.Add(colorContent);
            row.Add(swatchContainer);

            var colorLabel = new Label(label) { name = "color-label" };
            colorLabel.AddToClassList("unity-base-field__label");
            row.Add(colorLabel);
            return row;
        }

        void UpdateColorLegend()
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                Shader.SetGlobalColor("unity_MaterialValidateLowColor", kSceneViewMaterialValidateLow.Color.linear);
                Shader.SetGlobalColor("unity_MaterialValidateHighColor", kSceneViewMaterialValidateHigh.Color.linear);
                Shader.SetGlobalColor("unity_MaterialValidatePureMetalColor", kSceneViewMaterialValidatePureMetal.Color.linear);
                Shader.SetGlobalColor("unity_BackfaceHighlightsColor", kSceneViewHighlightBackfaces.Color.linear);
            }
            else
            {
                Shader.SetGlobalColor("unity_MaterialValidateLowColor", kSceneViewMaterialValidateLow.Color);
                Shader.SetGlobalColor("unity_MaterialValidateHighColor", kSceneViewMaterialValidateHigh.Color);
                Shader.SetGlobalColor("unity_MaterialValidatePureMetalColor", kSceneViewMaterialValidatePureMetal.Color);
                Shader.SetGlobalColor("unity_BackfaceHighlightsColor", kSceneViewHighlightBackfaces.Color);
            }

            Handles.SetSceneViewModeGIContributorsReceiversColors(
                kSceneViewMaterialNoContributeGI.Color,
                kSceneViewMaterialReceiveGILightmaps.Color,
                kSceneViewMaterialReceiveGILightProbes.Color);

            m_LastKnownColorSpace = PlayerSettings.colorSpace;
        }

        void UpdatePrefColors(string key, Type prefType)
        {
            if (m_ContentRoot == null || prefType != typeof(PrefColor))
                return;

            foreach (var color in m_PrefColorSwatches)
            {
                if (color.Key.Name == key)
                {
                    var swatch = color.Value.Q(SceneViewLighting.Styles.k_SwatchColorContent);
                    if (swatch != null)
                        swatch.style.backgroundColor = new StyleColor(color.Key.Color);
                    break;
                }
            }

            UpdateColorLegend();
        }
    }
}
