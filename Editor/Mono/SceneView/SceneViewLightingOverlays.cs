// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UObject = UnityEngine.Object;

namespace UnityEditor
{
    class SceneViewLightingOverlays : IDisposable
    {
        static readonly PrefColor kSceneViewMaterialValidateLow = new PrefColor("Scene/Material Validator Value Too Low", 255.0f / 255.0f, 0.0f, 0.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialValidateHigh = new PrefColor("Scene/Material Validator Value Too High", 0.0f, 0.0f, 255.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialValidatePureMetal = new PrefColor("Scene/Material Validator Pure Metal", 255.0f / 255.0f, 255.0f / 255.0f, 0.0f, 1.0f);

        static readonly PrefColor kSceneViewMaterialNoContributeGI = new PrefColor("Scene/Contribute GI: Off / Receive GI: Light Probes", 229.0f / 255.0f, 203.0f / 255.0f, 132.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialReceiveGILightmaps = new PrefColor("Scene/Contribute GI: On / Receive GI: Lightmaps", 89.0f / 255.0f, 148.0f / 255.0f, 161.0f / 255.0f, 1.0f);
        static readonly PrefColor kSceneViewMaterialReceiveGILightProbes = new PrefColor("Scene/Contribute GI: On / Receive GI: Light Probes", 221.0f / 255.0f, 115.0f / 255.0f, 91.0f / 255.0f, 1.0f);

        SceneView m_SceneView;

        static class Styles
        {
            public static GUIContent contributeGIOff = EditorGUIUtility.TrTextContent("Contribute GI: Off / Receive GI: Light Probes");
            public static GUIContent receiveGILightmaps = EditorGUIUtility.TrTextContent("Contribute GI: On / Receive GI: Lightmaps");
            public static GUIContent receiveGILightProbes = EditorGUIUtility.TrTextContent("Contribute GI: On / Receive GI: Light Probes");
            public static GUIStyle colorSwatch = new GUIStyle() { normal = new GUIStyleState() { background = Texture2D.whiteTexture } };
        }

        class AlbedoSwatchGUI
        {
            public Color color;
            public GUIContent content;
            public string luminance;
        }

        string[] m_AlbedoSwatchPopup;
        AlbedoSwatchGUI[] m_AlbedoSwatches;
        AlbedoSwatchInfo[] m_AlbedoSwatchInfos;

        int m_SelectedAlbedoSwatchIndex = 0;
        float m_AlbedoSwatchHueTolerance = 0.1f;
        float m_AlbedoSwatchSaturationTolerance = 0.2f;
        ColorSpace m_LastKnownColorSpace = ColorSpace.Uninitialized;

        // this value can be altered by the user
        float m_ExposureSliderMax = 16f;

        Texture2D m_ExposureTexture = null;
        Texture2D m_EmptyExposureTexture = null;

        OverlayWindow m_PBRSettingsOverlayWindow;
        OverlayWindow m_LightingExposureSettingsOverlayWindow;
        OverlayWindow m_GIContributorsReceiversOverlayWindow;

        public SceneViewLightingOverlays(SceneView sceneView)
        {
            m_SceneView = sceneView;
            m_PBRSettingsOverlayWindow = new OverlayWindow(EditorGUIUtility.TrTextContent("PBR Validation Settings"), PBRValidationGUI, (int)SceneViewOverlay.Ordering.PhysicsDebug, sceneView, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
            m_LightingExposureSettingsOverlayWindow = new OverlayWindow(EditorGUIUtility.TrTextContent("Lighting Exposure"), LightingExposureGUI, (int)SceneViewOverlay.Ordering.PhysicsDebug, sceneView, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
            m_GIContributorsReceiversOverlayWindow = new OverlayWindow(EditorGUIUtility.TrTextContent("GI Contributors / Receivers"), GIContributorsReceiversGUI, (int)SceneViewOverlay.Ordering.PhysicsDebug, sceneView, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);

            //Ensure lightmap exposure is initialize to the right value
            Unsupported.SetSceneViewDebugModeExposureNoDirty(m_SceneView.bakedLightmapExposure);
        }

        public void Dispose()
        {
            if (m_ExposureTexture)
                UObject.DestroyImmediate(m_ExposureTexture, true);
            if (m_EmptyExposureTexture)
                UObject.DestroyImmediate(m_EmptyExposureTexture, true);
        }

        void PBRValidationGUI(UObject target, SceneView sceneView)
        {
            DrawTrueMetalCheckbox();
            DrawPBRSettingsForScene();
        }

        void LightingExposureGUI(UObject target, SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();
            m_SceneView.bakedLightmapExposure = EditorGUIInternal.ExposureSlider(m_SceneView.bakedLightmapExposure, ref m_ExposureSliderMax, EditorStyles.toolbarSlider);
            if (EditorGUI.EndChangeCheck())
                Unsupported.SetSceneViewDebugModeExposureNoDirty(m_SceneView.bakedLightmapExposure);
        }

        void GIContributorsReceiversGUI(UObject target, SceneView sceneView)
        {
            DrawGIContributorsReceiversSettings();
        }

        string CreateSwatchDescriptionForName(float minLum, float maxLum)
        {
            return "Luminance (" + minLum.ToString("F2", CultureInfo.InvariantCulture.NumberFormat) + " - " + maxLum.ToString("F2", CultureInfo.InvariantCulture.NumberFormat) + ")";
        }

        void CreateAlbedoSwatchData()
        {
            AlbedoSwatchInfo[] graphicsSettingsSwatches = EditorGraphicsSettings.albedoSwatches;

            if (graphicsSettingsSwatches.Length != 0)
            {
                m_AlbedoSwatchInfos = graphicsSettingsSwatches;
            }
            else
            {
                m_AlbedoSwatchInfos = new AlbedoSwatchInfo[]
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
            UpdateAlbedoSwatchGUI();
        }

        void UpdateAlbedoSwatchGUI()
        {
            m_LastKnownColorSpace = PlayerSettings.colorSpace;
            m_AlbedoSwatches = new AlbedoSwatchGUI[m_AlbedoSwatchInfos.Length + 1];
            bool colorCorrect = PlayerSettings.colorSpace == ColorSpace.Linear;

            m_AlbedoSwatches[0] = new AlbedoSwatchGUI()
            {
                color = colorCorrect ? Color.gray.gamma : Color.gray,
                content = new GUIContent("Default Luminance"),
                luminance = CreateSwatchDescriptionForName(0.012f, 0.9f)
            };

            for (int i = 0; i < m_AlbedoSwatchInfos.Length; i++)
            {
                m_AlbedoSwatches[i + 1] = new AlbedoSwatchGUI()
                {
                    color = colorCorrect ? m_AlbedoSwatchInfos[i].color.gamma : m_AlbedoSwatchInfos[i].color,
                    content = new GUIContent(m_AlbedoSwatchInfos[i].name),
                    luminance = CreateSwatchDescriptionForName(m_AlbedoSwatchInfos[i].minLuminance, m_AlbedoSwatchInfos[i].maxLuminance)
                };
            }

            m_AlbedoSwatchPopup = m_AlbedoSwatches.Select(x => x.content.text).ToArray();
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
        }

        void UpdateAlbedoSwatch()
        {
            Color color = Color.gray;
            if (m_SelectedAlbedoSwatchIndex != 0)
            {
                color = m_AlbedoSwatchInfos[m_SelectedAlbedoSwatchIndex - 1].color;
                Shader.SetGlobalFloat("_AlbedoMinLuminance", m_AlbedoSwatchInfos[m_SelectedAlbedoSwatchIndex - 1].minLuminance);
                Shader.SetGlobalFloat("_AlbedoMaxLuminance", m_AlbedoSwatchInfos[m_SelectedAlbedoSwatchIndex - 1].maxLuminance);
                Shader.SetGlobalFloat("_AlbedoHueTolerance", m_AlbedoSwatchHueTolerance);
                Shader.SetGlobalFloat("_AlbedoSaturationTolerance", m_AlbedoSwatchSaturationTolerance);
            }
            Shader.SetGlobalColor("_AlbedoCompareColor", color.linear);
            Shader.SetGlobalFloat("_CheckAlbedo", (m_SelectedAlbedoSwatchIndex != 0) ? 1.0f : 0.0f);
            Shader.SetGlobalFloat("_CheckPureMetal", m_SceneView.validateTrueMetals ? 1.0f : 0.0f);
        }

        internal void DrawTrueMetalCheckbox()
        {
            m_SceneView.validateTrueMetals = EditorGUILayout.ToggleLeft(EditorGUIUtility.TrTextContent("Check Pure Metals", "Check if albedo is black for materials with an average specular color above 0.45"), m_SceneView.validateTrueMetals);
        }

        internal void DrawPBRSettingsForScene()
        {
            if (m_SceneView.cameraMode.drawMode == DrawCameraMode.ValidateAlbedo)
            {
                if (PlayerSettings.colorSpace == ColorSpace.Gamma)
                {
                    EditorGUILayout.HelpBox("Albedo Validation doesn't work when Color Space is set to gamma space", MessageType.Warning);
                }

                EditorGUIUtility.labelWidth = 140;

                m_SelectedAlbedoSwatchIndex = EditorGUILayout.Popup(EditorGUIUtility.TrTextContent("Luminance Validation:", "Select default luminance validation or validate against a configured albedo swatch"), m_SelectedAlbedoSwatchIndex, m_AlbedoSwatchPopup);

                EditorGUI.indentLevel++;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUIUtility.labelWidth = 5;
                    GUI.color = m_AlbedoSwatches[m_SelectedAlbedoSwatchIndex].color;
                    EditorGUILayout.LabelField(" ", Styles.colorSwatch);
                    GUI.color = Color.white;
                    EditorGUIUtility.labelWidth = 140;
                    EditorGUILayout.LabelField(m_AlbedoSwatches[m_SelectedAlbedoSwatchIndex].luminance);
                }

                UpdateAlbedoSwatch();

                EditorGUI.indentLevel--;
                using (new EditorGUI.DisabledScope(m_SelectedAlbedoSwatchIndex == 0))
                {
                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUI.DisabledScope(m_SelectedAlbedoSwatchIndex == 0))
                    {
                        m_AlbedoSwatchHueTolerance = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Hue Tolerance:", "Check that the hue of the albedo value of a material is within the tolerance of the hue of the albedo swatch being validated against"), m_AlbedoSwatchHueTolerance, 0f, 0.5f);
                        m_AlbedoSwatchSaturationTolerance = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Saturation Tolerance:", "Check that the saturation of the albedo value of a material is within the tolerance of the saturation of the albedo swatch being validated against"), m_AlbedoSwatchSaturationTolerance, 0f, 0.5f);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateAlbedoSwatch();
                    }
                }
            }

            EditorGUILayout.LabelField("Color Legend:");
            EditorGUI.indentLevel++;
            string modeString;

            if (m_SceneView.cameraMode.drawMode == DrawCameraMode.ValidateAlbedo)
            {
                modeString = "Luminance";
            }
            else
            {
                modeString = "Specular";
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                GUI.color = kSceneViewMaterialValidateLow.Color;
                EditorGUILayout.LabelField("", Styles.colorSwatch);
                GUI.color = Color.white;
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField("Below Minimum " + modeString + " Value");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                GUI.color = kSceneViewMaterialValidateHigh.Color;
                EditorGUILayout.LabelField("", Styles.colorSwatch);
                GUI.color = Color.white;
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField("Above Maximum " + modeString + " Value");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                GUI.color = kSceneViewMaterialValidatePureMetal.Color;
                EditorGUILayout.LabelField("", Styles.colorSwatch);
                GUI.color = Color.white;
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField("Not A Pure Metal");
            }
        }

        void PrepareValidationUI()
        {
            if (m_AlbedoSwatchInfos == null)
            {
                CreateAlbedoSwatchData();
                UpdatePBRColorLegend();
            }

            if (PlayerSettings.colorSpace != m_LastKnownColorSpace)
            {
                UpdateAlbedoSwatchGUI();
                UpdateAlbedoSwatch();
                UpdatePBRColorLegend();
            }
        }

        void UpdateGIContributorsReceiversColors()
        {
            Handles.SetSceneViewModeGIContributorsReceiversColors(kSceneViewMaterialNoContributeGI.Color, kSceneViewMaterialReceiveGILightmaps.Color, kSceneViewMaterialReceiveGILightProbes.Color);
        }

        internal void DrawGIContributorsReceiversSettings()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                GUI.color = kSceneViewMaterialNoContributeGI.Color;
                EditorGUILayout.LabelField("", Styles.colorSwatch);
                GUI.color = Color.white;
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField(Styles.contributeGIOff);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                GUI.color = kSceneViewMaterialReceiveGILightmaps.Color;
                EditorGUILayout.LabelField("", Styles.colorSwatch);
                GUI.color = Color.white;
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField(Styles.receiveGILightmaps);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 2;
                GUI.color = kSceneViewMaterialReceiveGILightProbes.Color;
                EditorGUILayout.LabelField("", Styles.colorSwatch);
                GUI.color = Color.white;
                EditorGUIUtility.labelWidth = 200;
                EditorGUILayout.LabelField(Styles.receiveGILightProbes);
            }
        }

        public void OnGUI()
        {
            DrawSceneViewSwatch();
            UpdateGizmoExposure();
        }

        internal void DrawSceneViewSwatch()
        {
            if (m_SceneView.cameraMode.drawMode == DrawCameraMode.ValidateAlbedo || m_SceneView.cameraMode.drawMode == DrawCameraMode.ValidateMetalSpecular)
            {
                PrepareValidationUI();
                SceneViewOverlay.ShowWindow(m_PBRSettingsOverlayWindow);
            }

            if (m_SceneView.showExposureSettings)
            {
                SceneViewOverlay.ShowWindow(m_LightingExposureSettingsOverlayWindow);
            }

            if (m_SceneView.cameraMode.drawMode == DrawCameraMode.GIContributorsReceivers)
            {
                UpdateGIContributorsReceiversColors();
                SceneViewOverlay.ShowWindow(m_GIContributorsReceiversOverlayWindow);
            }
        }

        internal void UpdateGizmoExposure()
        {
            if (m_SceneView.showExposureSettings)
            {
                if (m_ExposureTexture == null)
                {
                    m_ExposureTexture = new Texture2D(1, 1, GraphicsFormat.R32G32_SFloat, TextureCreationFlags.None);
                    m_ExposureTexture.hideFlags = HideFlags.HideAndDontSave;
                }

                m_ExposureTexture.SetPixel(0, 0, new Color(Mathf.Pow(2.0f, m_SceneView.bakedLightmapExposure), 0.0f, 0.0f));
                m_ExposureTexture.Apply();

                Gizmos.exposure = m_ExposureTexture;
            }
            else
            {
                if (m_EmptyExposureTexture == null)
                {
                    m_EmptyExposureTexture = new Texture2D(1, 1, GraphicsFormat.R32G32_SFloat, TextureCreationFlags.None);
                    m_EmptyExposureTexture.hideFlags = HideFlags.HideAndDontSave;
                    m_EmptyExposureTexture.SetPixel(0, 0, new Color(1.0f, 0.0f, 0.0f));
                    m_EmptyExposureTexture.Apply();
                }

                Gizmos.exposure = m_EmptyExposureTexture;
            }
        }
    }
}
