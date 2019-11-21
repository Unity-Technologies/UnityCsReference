// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor.AnimatedValues;
using System.Linq;

namespace UnityEditor
{
    internal enum DefaultReflectionMode
    {
        FromSkybox = 0,
        Custom
    }

    [CustomEditor(typeof(RenderSettings))]
    internal class LightingEditor : Editor
    {
        internal static class Styles
        {
            static Styles() {}

            public static readonly GUIContent env_top = EditorGUIUtility.TrTextContent("Environment");
            public static readonly GUIContent env_skybox_mat = EditorGUIUtility.TrTextContent("Skybox Material", "Specifies the material that is used to simulate the sky or other distant background in the Scene.");
            public static readonly GUIContent env_skybox_sun = EditorGUIUtility.TrTextContent("Sun Source", "Specifies the directional light that is used to indicate the direction of the sun when a procedural skybox is used. If set to None, the brightest directional light in the Scene is used to represent the sun.");
            public static readonly GUIContent env_amb_top = EditorGUIUtility.TrTextContent("Environment Lighting");
            public static readonly GUIContent env_amb_src = EditorGUIUtility.TrTextContent("Source", "Specifies whether to use a skybox, gradient, or color for ambient light contributed to the Scene.");
            public static readonly GUIContent env_amb_int = EditorGUIUtility.TrTextContent("Intensity Multiplier", "Controls the brightness of the skybox lighting in the Scene.");
            public static readonly GUIContent env_refl_top = EditorGUIUtility.TrTextContent("Environment Reflections");
            public static readonly GUIContent env_refl_src = EditorGUIUtility.TrTextContent("Source", "Specifies whether to use the skybox or a custom cube map for reflection effects in the Scene.");
            public static readonly GUIContent env_refl_res = EditorGUIUtility.TrTextContent("Resolution", "Controls the resolution for the cube map assigned to the skybox material for reflection effects in the Scene.");
            public static readonly GUIContent env_refl_cmp = EditorGUIUtility.TrTextContent("Compression", "Controls how Unity compresses the reflection cube maps. Options are Auto, Compressed, and Uncompressed. Auto compresses the cube maps if the compression format is suitable.");
            public static readonly GUIContent env_refl_int = EditorGUIUtility.TrTextContent("Intensity Multiplier", "Controls how much the skybox or custom cubemap affects reflections in the Scene. A value of 1 produces physically correct results.");
            public static readonly GUIContent env_refl_bnc = EditorGUIUtility.TrTextContent("Bounces", "Controls how many times a reflection includes other reflections. A value of 1 results in the Scene being rendered once so mirrored reflections will be black. A value of 2 results in mirrored reflections being visible in the Scene.");
            public static readonly GUIContent skyboxWarning = EditorGUIUtility.TrTextContent("Shader of this material does not support skybox rendering.");
            public static readonly GUIContent createLight = EditorGUIUtility.TrTextContent("Create Light");
            public static readonly GUIContent ambientUp = EditorGUIUtility.TrTextContent("Sky Color", "Controls the color of light emitted from the sky in the Scene.");
            public static readonly GUIContent ambientMid = EditorGUIUtility.TrTextContent("Equator Color", "Controls the color of light emitted from the sides of the Scene.");
            public static readonly GUIContent ambientDown = EditorGUIUtility.TrTextContent("Ground Color", "Controls the color of light emitted from the ground of the Scene.");
            public static readonly GUIContent ambient = EditorGUIUtility.TrTextContent("Ambient Color", "Controls the color of the ambient light contributed to the Scene.");
            public static readonly GUIContent customReflection = EditorGUIUtility.TrTextContent("Cubemap", "Specifies the custom cube map used for reflection effects in the Scene.");
            public static readonly GUIContent SubtractiveColor = EditorGUIUtility.TrTextContent("Realtime Shadow Color", "The color used for mixing realtime shadows with baked lightmaps in Subtractive lighting mode. The color defines the darkest point of the realtime shadow.");

            public static readonly GUIContent[] kFullAmbientSource =
            {
                EditorGUIUtility.TrTextContent("Skybox"),
                EditorGUIUtility.TrTextContent("Gradient"),
                EditorGUIUtility.TrTextContent("Color"),
            };

            public static readonly int[] kFullAmbientSourceValues = { (int)AmbientMode.Skybox, (int)AmbientMode.Trilight, (int)AmbientMode.Flat };
        }

        protected SerializedProperty m_Sun;
        protected SerializedProperty m_SubtractiveShadowColor;
        protected SerializedProperty m_AmbientSource;
        protected SerializedProperty m_AmbientSkyColor;
        protected SerializedProperty m_AmbientEquatorColor;
        protected SerializedProperty m_AmbientGroundColor;
        protected SerializedProperty m_AmbientIntensity;

        protected SerializedProperty m_ReflectionIntensity;
        protected SerializedProperty m_ReflectionBounces;

        protected SerializedProperty m_SkyboxMaterial;
        protected SerializedProperty m_DefaultReflectionMode;
        protected SerializedProperty m_DefaultReflectionResolution;
        protected SerializedProperty m_CustomReflection;
        protected SerializedProperty m_ReflectionCompression;

        protected SerializedObject m_RenderSettings;
        protected SerializedObject m_LightmapSettings;

        SerializedObject renderSettings
        {
            get
            {
                // if we set a new scene as the active scene, we need to make sure to respond to those changes
                if (m_RenderSettings == null || m_RenderSettings.targetObject == null || m_RenderSettings.targetObject != RenderSettings.GetRenderSettings())
                {
                    m_RenderSettings = new SerializedObject(RenderSettings.GetRenderSettings());

                    m_Sun = m_RenderSettings.FindProperty("m_Sun");
                    m_SubtractiveShadowColor = m_RenderSettings.FindProperty("m_SubtractiveShadowColor");
                    m_AmbientSource = m_RenderSettings.FindProperty("m_AmbientMode");
                    m_AmbientSkyColor = m_RenderSettings.FindProperty("m_AmbientSkyColor");
                    m_AmbientEquatorColor = m_RenderSettings.FindProperty("m_AmbientEquatorColor");
                    m_AmbientGroundColor = m_RenderSettings.FindProperty("m_AmbientGroundColor");
                    m_AmbientIntensity = m_RenderSettings.FindProperty("m_AmbientIntensity");
                    m_ReflectionIntensity = m_RenderSettings.FindProperty("m_ReflectionIntensity");
                    m_ReflectionBounces = m_RenderSettings.FindProperty("m_ReflectionBounces");
                    m_SkyboxMaterial = m_RenderSettings.FindProperty("m_SkyboxMaterial");
                    m_DefaultReflectionMode = m_RenderSettings.FindProperty("m_DefaultReflectionMode");
                    m_DefaultReflectionResolution = m_RenderSettings.FindProperty("m_DefaultReflectionResolution");
                    m_CustomReflection = m_RenderSettings.FindProperty("m_CustomReflection");
                }

                return m_RenderSettings;
            }
        }

        SerializedObject lightmapSettings
        {
            get
            {
                // if we set a new scene as the active scene, we need to make sure to respond to those changes
                if (m_LightmapSettings == null || m_LightmapSettings.targetObject == null || m_LightmapSettings.targetObject != LightmapEditorSettings.GetLightmapSettings())
                {
                    m_LightmapSettings = new SerializedObject(LightmapEditorSettings.GetLightmapSettings());
                    m_ReflectionCompression = m_LightmapSettings.FindProperty("m_LightmapEditorSettings.m_ReflectionCompression");
                }

                return m_LightmapSettings;
            }
        }

        private bool m_bShowEnvironment;
        private const string kShowEnvironment = "ShowEnvironment";

        public virtual void OnEnable()
        {
            m_bShowEnvironment = SessionState.GetBool(kShowEnvironment, true);
        }

        public virtual void OnDisable()
        {
            SessionState.SetBool(kShowEnvironment, m_bShowEnvironment);
        }

        private void DrawGUI()
        {
            Material skyboxMaterial = m_SkyboxMaterial.objectReferenceValue as Material;

            m_bShowEnvironment = EditorGUILayout.FoldoutTitlebar(m_bShowEnvironment, Styles.env_top, true);

            if (m_bShowEnvironment)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_SkyboxMaterial, Styles.env_skybox_mat);
                if (skyboxMaterial && !EditorMaterialUtility.IsBackgroundMaterial(skyboxMaterial))
                {
                    EditorGUILayout.HelpBox(Styles.skyboxWarning.text, MessageType.Warning);
                }

                EditorGUILayout.PropertyField(m_Sun, Styles.env_skybox_sun);
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(m_SubtractiveShadowColor, Styles.SubtractiveColor);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.env_amb_top);
                EditorGUI.indentLevel++;

                EditorGUILayout.IntPopup(m_AmbientSource, Styles.kFullAmbientSource, Styles.kFullAmbientSourceValues, Styles.env_amb_src);
                switch ((AmbientMode)m_AmbientSource.intValue)
                {
                    case AmbientMode.Trilight:
                    {
                        EditorGUI.BeginChangeCheck();
                        Color newValueUp = EditorGUILayout.ColorField(Styles.ambientUp, m_AmbientSkyColor.colorValue, true, false, true);
                        Color newValueMid = EditorGUILayout.ColorField(Styles.ambientMid, m_AmbientEquatorColor.colorValue, true, false, true);
                        Color newValueDown = EditorGUILayout.ColorField(Styles.ambientDown, m_AmbientGroundColor.colorValue, true, false, true);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_AmbientSkyColor.colorValue = newValueUp;
                            m_AmbientEquatorColor.colorValue = newValueMid;
                            m_AmbientGroundColor.colorValue = newValueDown;
                        }
                    }
                    break;

                    case AmbientMode.Flat:
                    {
                        EditorGUI.BeginChangeCheck();
                        Color newValue = EditorGUILayout.ColorField(Styles.ambient, m_AmbientSkyColor.colorValue, true, false, true);
                        if (EditorGUI.EndChangeCheck())
                            m_AmbientSkyColor.colorValue = newValue;
                    }
                    break;

                    case AmbientMode.Skybox:
                        if (skyboxMaterial == null)
                        {
                            EditorGUI.BeginChangeCheck();
                            Color newValue = EditorGUILayout.ColorField(Styles.ambient, m_AmbientSkyColor.colorValue, true, false, true);
                            if (EditorGUI.EndChangeCheck())
                                m_AmbientSkyColor.colorValue = newValue;
                        }
                        else
                        {
                            // Ambient intensity - maximum is kEmissiveRGBMMax
                            EditorGUILayout.Slider(m_AmbientIntensity, 0.0F, 8.0F, Styles.env_amb_int);
                        }
                        break;
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.env_refl_top);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_DefaultReflectionMode, Styles.env_refl_src);

                DefaultReflectionMode defReflectionMode = (DefaultReflectionMode)m_DefaultReflectionMode.intValue;
                switch (defReflectionMode)
                {
                    case DefaultReflectionMode.FromSkybox:
                    {
                        int[] reflectionResolutionValuesArray = null;
                        GUIContent[] reflectionResolutionTextArray = null;
                        ReflectionProbeEditor.GetResolutionArray(ref reflectionResolutionValuesArray, ref reflectionResolutionTextArray);
                        EditorGUILayout.IntPopup(m_DefaultReflectionResolution, reflectionResolutionTextArray, reflectionResolutionValuesArray, Styles.env_refl_res, GUILayout.MinWidth(40));
                    }
                    break;
                    case DefaultReflectionMode.Custom:
                        EditorGUILayout.PropertyField(m_CustomReflection, Styles.customReflection);
                        break;
                }

                EditorGUILayout.PropertyField(m_ReflectionCompression, Styles.env_refl_cmp);
                EditorGUILayout.Slider(m_ReflectionIntensity, 0.0F, 1.0F, Styles.env_refl_int);
                EditorGUILayout.IntSlider(m_ReflectionBounces, 1, 5, Styles.env_refl_bnc);

                EditorGUI.indentLevel--;

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }

        public override void OnInspectorGUI()
        {
            renderSettings.Update();
            lightmapSettings.Update();

            DrawGUI();

            renderSettings.ApplyModifiedProperties();
            lightmapSettings.ApplyModifiedProperties();
        }
    }
}
