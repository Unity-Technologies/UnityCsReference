// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(Light))]
    [CanEditMultipleObjects]
    public class LightEditor : Editor
    {
        SerializedProperty m_Type;
        SerializedProperty m_Range;
        SerializedProperty m_SpotAngle;
        SerializedProperty m_CookieSize;
        SerializedProperty m_Color;
        SerializedProperty m_Intensity;
        SerializedProperty m_BounceIntensity;
        SerializedProperty m_ColorTemperature;
        SerializedProperty m_UseColorTemperature;
        SerializedProperty m_Cookie;
        SerializedProperty m_ShadowsType;
        SerializedProperty m_ShadowsStrength;
        SerializedProperty m_ShadowsResolution;
        SerializedProperty m_ShadowsBias;
        SerializedProperty m_ShadowsNormalBias;
        SerializedProperty m_ShadowsNearPlane;
        SerializedProperty m_Halo;
        SerializedProperty m_Flare;
        SerializedProperty m_RenderMode;
        SerializedProperty m_CullingMask;
        SerializedProperty m_Lightmapping;
        SerializedProperty m_AreaSizeX;
        SerializedProperty m_AreaSizeY;
        SerializedProperty m_BakedShadowRadius;
        SerializedProperty m_BakedShadowAngle;

        Texture2D m_KelvinGradientTexture;
        const float kMinKelvin = 1000f;
        const float kMaxKelvin = 20000f;
        const float kSliderPower = 2f;

        AnimBool m_AnimShowSpotOptions = new AnimBool();
        AnimBool m_AnimShowPointOptions = new AnimBool();
        AnimBool m_AnimShowDirOptions = new AnimBool();
        AnimBool m_AnimShowAreaOptions = new AnimBool();
        AnimBool m_AnimShowRuntimeOptions = new AnimBool();
        AnimBool m_AnimShowShadowOptions = new AnimBool();
        AnimBool m_AnimBakedShadowAngleOptions = new AnimBool();
        AnimBool m_AnimBakedShadowRadiusOptions = new AnimBool();
        AnimBool m_AnimShowLightBounceIntensity = new AnimBool();

        private bool m_CommandBuffersShown = true;

        class Styles
        {
            public static GUIStyle sliderBox = new GUIStyle("ColorPickerBox");
            public static GUIStyle sliderThumb = new GUIStyle("ColorPickerHorizThumb");

            static Styles()
            {
                sliderBox.overflow = new RectOffset(0, 0, -4, -4);
                sliderBox.padding = new RectOffset(0, 0, 1, 1);
            }

            public readonly GUIContent Type = EditorGUIUtility.TextContent("Type|Specifies the current type of light. Possible types are Directional, Spot, Point, and Area lights.");
            public readonly GUIContent Range = EditorGUIUtility.TextContent("Range|Controls how far the light is emitted from the center of the object.");
            public readonly GUIContent SpotAngle = EditorGUIUtility.TextContent("Spot Angle|Controls the angle in degrees at the base of a Spot light's cone.");
            public readonly GUIContent Color = EditorGUIUtility.TextContent("Color|Controls the color being emitted by the light.");
            public readonly GUIContent UseColorTemperature = EditorGUIUtility.TextContent("Use color temperature mode|Cho0se between RGB and temperature mode for light's color.");
            public readonly GUIContent ColorFilter = EditorGUIUtility.TextContent("Filter|A colored gel can be put in front of the light source to tint the light.");
            public readonly GUIContent ColorTemperature = EditorGUIUtility.TextContent("Temperature|Also known as CCT (Correlated color temperature). The color temperature of the electromagnetic radiation emitted from an ideal black body is defined as its surface temperature in Kelvin. White is 6500K");
            public readonly GUIContent Intensity = EditorGUIUtility.TextContent("Intensity|Controls the brightness of the light. Light color is multiplied by this value.");
            public readonly GUIContent LightmappingMode = EditorGUIUtility.TextContent("Mode|Specifies the light mode used to determine if and how a light will be baked. Possible modes are Baked, Mixed, and Realtime.");
            public readonly GUIContent LightBounceIntensity = EditorGUIUtility.TextContent("Indirect Multiplier|Controls the intensity of indirect light being contributed to the scene. A value of 0 will cause Realtime lights to be removed from realtime global illumination and Baked and Mixed lights to no longer emit indirect lighting. Has no effect when both Realtime and Baked Global Illumination are disabled.");
            public readonly GUIContent ShadowType = EditorGUIUtility.TextContent("Shadow Type|Specifies whether Hard Shadows, Soft Shadows, or No Shadows will be cast by the light.");
            //realtime
            public readonly GUIContent ShadowRealtimeSettings = EditorGUIUtility.TextContent("Realtime Shadows|Settings for realtime direct shadows.");
            public readonly GUIContent ShadowStrength = EditorGUIUtility.TextContent("Strength|Controls how dark the shadows cast by the light will be.");
            public readonly GUIContent ShadowResolution = EditorGUIUtility.TextContent("Resolution|Controls the rendered resolution of the shadow maps. A higher resolution will increase the fidelity of shadows at the cost of GPU performance and memory usage.");
            public readonly GUIContent ShadowBias = EditorGUIUtility.TextContent("Bias|Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent ShadowNormalBias = EditorGUIUtility.TextContent("Normal Bias|Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent ShadowNearPlane = EditorGUIUtility.TextContent("Near Plane|Controls the value for the near clip plane when rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.");
            //baked
            public readonly GUIContent BakedShadowRadius = EditorGUIUtility.TextContent("Baked Shadow Radius|Controls the amount of artificial softening applied to the edges of shadows cast by the Point or Spot light.");
            public readonly GUIContent BakedShadowAngle = EditorGUIUtility.TextContent("Baked Shadow Angle|Controls the amount of artificial softening applied to the edges of shadows cast by directional lights.");

            public readonly GUIContent Cookie = EditorGUIUtility.TextContent("Cookie|Specifies the Texture mask to cast shadows, create silhouettes, or patterned illumination for the light.");
            public readonly GUIContent CookieSize = EditorGUIUtility.TextContent("Cookie Size|Controls the size of the cookie mask currently assigned to the light.");
            public readonly GUIContent DrawHalo = EditorGUIUtility.TextContent("Draw Halo|When enabled, draws a spherical halo of light with a radius equal to the lights range value.");
            public readonly GUIContent Flare = EditorGUIUtility.TextContent("Flare|Specifies the flare object to be used by the light to render lens flares in the scene.");
            public readonly GUIContent RenderMode = EditorGUIUtility.TextContent("Render Mode|Specifies the importance of the light which impacts lighting fidelity and performance. Options are Auto, Important, and Not Important. This only affects Forward Rendering");
            public readonly GUIContent CullingMask = EditorGUIUtility.TextContent("Culling Mask|Specifies which layers will be affected or excluded from the light's effect on objects in the scene.");

            public readonly GUIContent iconRemove = EditorGUIUtility.IconContent("Toolbar Minus", "Remove command buffer");
            public readonly GUIStyle invisibleButton = "InvisibleButton";

            public readonly GUIContent AreaWidth = EditorGUIUtility.TextContent("Width|Controls the width in units of the area light.");
            public readonly GUIContent AreaHeight = EditorGUIUtility.TextContent("Height|Controls the height in units of the area light.");

            public readonly GUIContent BakingWarning = EditorGUIUtility.TextContent("Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.");
            public readonly GUIContent IndirectBounceShadowWarning = EditorGUIUtility.TextContent("Realtime indirect bounce shadowing is not supported for Spot and Point lights.");
            public readonly GUIContent CookieWarning = EditorGUIUtility.TextContent("Cookie textures for spot lights should be set to clamp, not repeat, to avoid artifacts.");
            public readonly GUIContent DisabledLightWarning = EditorGUIUtility.TextContent("Lighting has been disabled in at least one Scene view. Any changes applied to lights in the Scene will not be updated in these views until Lighting has been enabled again.");

            public readonly GUIContent[] LightmapBakeTypeTitles = { new GUIContent("Realtime"), new GUIContent("Mixed"), new GUIContent("Baked") };
            public readonly int[] LightmapBakeTypeValues = { (int)LightmapBakeType.Realtime, (int)LightmapBakeType.Mixed, (int)LightmapBakeType.Baked };
        }

        static Styles s_Styles;

        // Should match same colors in GizmoDrawers.cpp!
        internal static Color kGizmoLight = new Color(254 / 255f, 253 / 255f, 136 / 255f, 128 / 255f);
        internal static Color kGizmoDisabledLight = new Color(135 / 255f, 116 / 255f, 50 / 255f, 128 / 255f);

        private bool typeIsSame             { get { return !m_Type.hasMultipleDifferentValues; } }
        private bool shadowTypeIsSame       { get { return !m_ShadowsType.hasMultipleDifferentValues; } }
        private bool lightmappingTypeIsSame { get { return !m_Lightmapping.hasMultipleDifferentValues; } }
        private Light light                 { get { return target as Light; } }
        private bool isRealtime             { get { return m_Lightmapping.intValue == 4; } }
        private bool isCompletelyBaked      { get { return m_Lightmapping.intValue == 2; } }
        private bool isBakedOrMixed         { get { return !isRealtime; } }
        private Texture cookie              { get { return m_Cookie.objectReferenceValue as Texture; } }

        private bool spotOptionsValue       { get { return typeIsSame && light.type == LightType.Spot; } }
        private bool pointOptionsValue      { get { return typeIsSame && light.type == LightType.Point; } }
        private bool dirOptionsValue        { get { return typeIsSame && light.type == LightType.Directional; } }
        private bool areaOptionsValue       { get { return typeIsSame && light.type == LightType.Area; } }
        private bool runtimeOptionsValue    { get { return typeIsSame && (light.type != LightType.Area && !isCompletelyBaked); } }
        private bool bakedShadowRadius      { get { return typeIsSame && (light.type == LightType.Point || light.type == LightType.Spot) && isBakedOrMixed; } }
        private bool bakedShadowAngle       { get { return typeIsSame && light.type == LightType.Directional && isBakedOrMixed; } }
        private bool shadowOptionsValue     { get { return shadowTypeIsSame && light.shadows != LightShadows.None; } }
        private bool bounceWarningValue
        {
            get
            {
                return typeIsSame && (light.type == LightType.Point || light.type == LightType.Spot) &&
                    lightmappingTypeIsSame && isRealtime && !m_BounceIntensity.hasMultipleDifferentValues && m_BounceIntensity.floatValue > 0.0F;
            }
        }
        private bool bakingWarningValue     { get { return !Lightmapping.bakedGI && lightmappingTypeIsSame && isBakedOrMixed; } }
        private bool showLightBounceIntensity   { get { return true; } }
        private bool cookieWarningValue
        {
            get
            {
                return typeIsSame && light.type == LightType.Spot &&
                    !m_Cookie.hasMultipleDifferentValues && cookie && cookie.wrapMode != TextureWrapMode.Clamp;
            }
        }
        private bool isPrefab               { get { PrefabType type = PrefabUtility.GetPrefabType(target); return (type == PrefabType.Prefab || type == PrefabType.ModelPrefab); } }


        private void SetOptions(AnimBool animBool, bool initialize, bool targetValue)
        {
            if (initialize)
            {
                animBool.value = targetValue;
                animBool.valueChanged.AddListener(Repaint);
            }
            else
            {
                animBool.target = targetValue;
            }
        }

        private void UpdateShowOptions(bool initialize)
        {
            SetOptions(m_AnimShowSpotOptions, initialize, spotOptionsValue);
            SetOptions(m_AnimShowPointOptions, initialize, pointOptionsValue);
            SetOptions(m_AnimShowDirOptions, initialize, dirOptionsValue);
            SetOptions(m_AnimShowAreaOptions, initialize, areaOptionsValue);
            SetOptions(m_AnimShowShadowOptions, initialize, shadowOptionsValue);
            SetOptions(m_AnimShowRuntimeOptions, initialize, runtimeOptionsValue);
            SetOptions(m_AnimBakedShadowAngleOptions, initialize, bakedShadowAngle);
            SetOptions(m_AnimBakedShadowRadiusOptions, initialize, bakedShadowRadius);
            SetOptions(m_AnimShowLightBounceIntensity, initialize, showLightBounceIntensity);
        }

        void LightUsageGUI()
        {
            // Baking type
            if (EditorGUILayout.BeginFadeGroup(1.0F - m_AnimShowAreaOptions.faded))
            {
                EditorGUILayout.IntPopup(m_Lightmapping, s_Styles.LightmapBakeTypeTitles, s_Styles.LightmapBakeTypeValues, s_Styles.LightmappingMode);

                // Warning if GI Baking disabled and m_Lightmapping isn't realtime
                if (bakingWarningValue)
                {
                    EditorGUILayout.HelpBox(s_Styles.BakingWarning.text, MessageType.Info);
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        void OnEnable()
        {
            m_Type = serializedObject.FindProperty("m_Type");
            m_Range = serializedObject.FindProperty("m_Range");
            m_SpotAngle = serializedObject.FindProperty("m_SpotAngle");
            m_CookieSize = serializedObject.FindProperty("m_CookieSize");
            m_Color = serializedObject.FindProperty("m_Color");
            m_Intensity = serializedObject.FindProperty("m_Intensity");
            m_BounceIntensity = serializedObject.FindProperty("m_BounceIntensity");
            m_ColorTemperature = serializedObject.FindProperty("m_ColorTemperature");
            m_UseColorTemperature = serializedObject.FindProperty("m_UseColorTemperature");
            m_Cookie = serializedObject.FindProperty("m_Cookie");
            m_ShadowsType = serializedObject.FindProperty("m_Shadows.m_Type");
            m_ShadowsStrength = serializedObject.FindProperty("m_Shadows.m_Strength");
            m_ShadowsResolution = serializedObject.FindProperty("m_Shadows.m_Resolution");
            m_ShadowsBias = serializedObject.FindProperty("m_Shadows.m_Bias");
            m_ShadowsNormalBias = serializedObject.FindProperty("m_Shadows.m_NormalBias");
            m_ShadowsNearPlane = serializedObject.FindProperty("m_Shadows.m_NearPlane");
            m_Halo = serializedObject.FindProperty("m_DrawHalo");
            m_Flare = serializedObject.FindProperty("m_Flare");
            m_RenderMode = serializedObject.FindProperty("m_RenderMode");
            m_CullingMask = serializedObject.FindProperty("m_CullingMask");
            m_Lightmapping = serializedObject.FindProperty("m_Lightmapping");
            m_AreaSizeX = serializedObject.FindProperty("m_AreaSize.x");
            m_AreaSizeY = serializedObject.FindProperty("m_AreaSize.y");
            m_BakedShadowRadius = serializedObject.FindProperty("m_ShadowRadius");
            m_BakedShadowAngle = serializedObject.FindProperty("m_ShadowAngle");

            UpdateShowOptions(true);

            if (m_KelvinGradientTexture == null)
                m_KelvinGradientTexture = CreateKelvinGradientTexture("KelvinGradientTexture", 300, 16, kMinKelvin, kMaxKelvin);
        }

        void OnDestroy()
        {
            if (m_KelvinGradientTexture != null)
                DestroyImmediate(m_KelvinGradientTexture);
        }

        private void CommandBufferGUI()
        {
            // Command buffers are not serialized data, so can't get to them through
            // serialized property (hence no multi-edit).
            if (targets.Length != 1)
                return;
            var light = target as Light;
            if (light == null)
                return;
            int count = light.commandBufferCount;
            if (count == 0)
                return;

            m_CommandBuffersShown = GUILayout.Toggle(m_CommandBuffersShown, GUIContent.Temp(count + " command buffers"), EditorStyles.foldout);
            if (!m_CommandBuffersShown)
                return;
            EditorGUI.indentLevel++;
            foreach (LightEvent le in (LightEvent[])System.Enum.GetValues(typeof(LightEvent)))
            {
                CommandBuffer[] cbs = light.GetCommandBuffers(le);
                foreach (CommandBuffer cb in cbs)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        // row with event & command buffer information label
                        Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.miniLabel);
                        rowRect.xMin += EditorGUI.indent;
                        Rect minusRect = GetRemoveButtonRect(rowRect);
                        rowRect.xMax = minusRect.x;
                        GUI.Label(rowRect, string.Format("{0}: {1} ({2})", le, cb.name, EditorUtility.FormatBytes(cb.sizeInBytes)), EditorStyles.miniLabel);
                        // and a button to remove it
                        if (GUI.Button(minusRect, s_Styles.iconRemove, s_Styles.invisibleButton))
                        {
                            light.RemoveCommandBuffer(le, cb);
                            SceneView.RepaintAll();
                            GameView.RepaintAll();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
            // "remove all" button
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove all", EditorStyles.miniButton))
                {
                    light.RemoveAllCommandBuffers();
                    SceneView.RepaintAll();
                    GameView.RepaintAll();
                }
            }
            EditorGUI.indentLevel--;
        }

        static Rect GetRemoveButtonRect(Rect r)
        {
            var buttonSize = s_Styles.invisibleButton.CalcSize(s_Styles.iconRemove);
            return new Rect(r.xMax - buttonSize.x, r.y + (int)(r.height / 2 - buttonSize.y / 2), buttonSize.x, buttonSize.y);
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            serializedObject.Update();

            UpdateShowOptions(false);

            // Light type (shape and usage)
            EditorGUILayout.PropertyField(m_Type, s_Styles.Type);

            EditorGUILayout.Space();

            // When we are switching between two light types that don't show the range (directional and area lights)
            // we want the fade group to stay hidden.
            //bool keepRangeHidden = m_ShowDirOptions.isAnimating && m_ShowDirOptions.target;
            //float fadeRange = keepRangeHidden ? 0.0f : 1.0f - m_ShowDirOptions.faded;
            float fadeRange = 1.0f - m_AnimShowDirOptions.faded;

            // Light Range
            if (EditorGUILayout.BeginFadeGroup(fadeRange))
            {
                // If the light is an area light, the range is determined by other parameters.
                // Therefore, disable area light's range for editing, but just update the editr field.
                if (m_AnimShowAreaOptions.target)
                {
                    GUI.enabled = false;
                    string areaLightToolTip = "For area lights " + m_Range.displayName + " is computed from Width, Height and Intensity";
                    GUIContent areaRangeWithToolTip = new GUIContent(m_Range.displayName, areaLightToolTip);
                    EditorGUILayout.FloatField(areaRangeWithToolTip, light.range);
                    GUI.enabled = true;
                }
                else
                    EditorGUILayout.PropertyField(m_Range, s_Styles.Range);
            }
            EditorGUILayout.EndFadeGroup();

            // Spot angle
            if (EditorGUILayout.BeginFadeGroup(m_AnimShowSpotOptions.faded))
                EditorGUILayout.Slider(m_SpotAngle, 1f, 179f, s_Styles.SpotAngle);
            EditorGUILayout.EndFadeGroup();

            // Area width & height
            if (EditorGUILayout.BeginFadeGroup(m_AnimShowAreaOptions.faded))
            {
                EditorGUILayout.PropertyField(m_AreaSizeX, s_Styles.AreaWidth);
                EditorGUILayout.PropertyField(m_AreaSizeY, s_Styles.AreaHeight);
            }
            EditorGUILayout.EndFadeGroup();

            if (UnityEngine.Rendering.GraphicsSettings.lightsUseLinearIntensity && UnityEngine.Rendering.GraphicsSettings.lightsUseColorTemperature)
            {
                EditorGUILayout.PropertyField(m_UseColorTemperature, s_Styles.UseColorTemperature);
                if (m_UseColorTemperature.boolValue)
                {
                    EditorGUILayout.LabelField(s_Styles.Color);
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.PropertyField(m_Color, s_Styles.ColorFilter);
                    EditorGUILayout.SliderWithTexture(s_Styles.ColorTemperature, m_ColorTemperature, kMinKelvin, kMaxKelvin, kSliderPower, Styles.sliderBox, Styles.sliderThumb, m_KelvinGradientTexture);
                    EditorGUI.indentLevel -= 1;
                }
                else
                    EditorGUILayout.PropertyField(m_Color, s_Styles.Color);
            }
            else
                EditorGUILayout.PropertyField(m_Color, s_Styles.Color);

            EditorGUILayout.Space();
            LightUsageGUI();

            EditorGUILayout.PropertyField(m_Intensity, s_Styles.Intensity);

            if (EditorGUILayout.BeginFadeGroup(m_AnimShowLightBounceIntensity.faded))
                EditorGUILayout.PropertyField(m_BounceIntensity, s_Styles.LightBounceIntensity);

            EditorGUILayout.EndFadeGroup();

            // Indirect shadows warning (Should be removed when we support realtime indirect shadows)
            if (bounceWarningValue)
            {
                EditorGUILayout.HelpBox(s_Styles.IndirectBounceShadowWarning.text, MessageType.Info);
            }

            ShadowsGUI();

            if (EditorGUILayout.BeginFadeGroup(m_AnimShowRuntimeOptions.faded))
            {
                EditorGUILayout.PropertyField(m_Cookie, s_Styles.Cookie);

                if (cookieWarningValue)
                {
                    // warn on spotlights if the cookie is set to repeat
                    EditorGUILayout.HelpBox(s_Styles.CookieWarning.text, MessageType.Warning);
                }
            }
            EditorGUILayout.EndFadeGroup();

            // Cookie size also requires directional light
            if (EditorGUILayout.BeginFadeGroup(m_AnimShowRuntimeOptions.faded * m_AnimShowDirOptions.faded))
                EditorGUILayout.PropertyField(m_CookieSize, s_Styles.CookieSize);

            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.PropertyField(m_Halo, s_Styles.DrawHalo);
            EditorGUILayout.PropertyField(m_Flare, s_Styles.Flare);
            EditorGUILayout.PropertyField(m_RenderMode, s_Styles.RenderMode);
            EditorGUILayout.PropertyField(m_CullingMask, s_Styles.CullingMask);

            EditorGUILayout.Space();
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.m_SceneLighting == false)
            {
                EditorGUILayout.HelpBox(s_Styles.DisabledLightWarning.text, MessageType.Warning);
            }

            CommandBufferGUI();

            serializedObject.ApplyModifiedProperties();
        }

        void ShadowsGUI()
        {
            //NOTE: FadeGroup's dont support nesting. Thus we just multiply the fade values here.

            // Shadows drop-down. Area lights can only be baked and always have shadows.
            float show = 1 - m_AnimShowAreaOptions.faded;
            if (EditorGUILayout.BeginFadeGroup(show))
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_ShadowsType, s_Styles.ShadowType);
            }
            EditorGUILayout.EndFadeGroup();
            show *= m_AnimShowShadowOptions.faded;

            EditorGUI.indentLevel += 1;
            // Baked Shadow radius
            if (EditorGUILayout.BeginFadeGroup(show * m_AnimBakedShadowRadiusOptions.faded))
            {
                using (new EditorGUI.DisabledScope(m_ShadowsType.intValue != (int)LightShadows.Soft))
                {
                    EditorGUILayout.PropertyField(m_BakedShadowRadius, s_Styles.BakedShadowRadius);
                }
            }
            EditorGUILayout.EndFadeGroup();

            // Baked Shadow angle
            if (EditorGUILayout.BeginFadeGroup(show * m_AnimBakedShadowAngleOptions.faded))
            {
                using (new EditorGUI.DisabledScope(m_ShadowsType.intValue != (int)LightShadows.Soft))
                {
                    EditorGUILayout.Slider(m_BakedShadowAngle, 0.0F, 90.0F, s_Styles.BakedShadowAngle);
                }
            }
            EditorGUILayout.EndFadeGroup();

            // Runtime shadows - shadow strength, resolution, bias
            if (EditorGUILayout.BeginFadeGroup(show * m_AnimShowRuntimeOptions.faded))
            {
                EditorGUILayout.LabelField(s_Styles.ShadowRealtimeSettings);
                EditorGUI.indentLevel += 1;
                EditorGUILayout.Slider(m_ShadowsStrength, 0f, 1f, s_Styles.ShadowStrength);
                EditorGUILayout.PropertyField(m_ShadowsResolution, s_Styles.ShadowResolution);
                EditorGUILayout.Slider(m_ShadowsBias, 0.0f, 2.0f, s_Styles.ShadowBias);
                EditorGUILayout.Slider(m_ShadowsNormalBias, 0.0f, 3.0f, s_Styles.ShadowNormalBias);

                // this min bound should match the calculation in SharedLightData::GetNearPlaneMinBound()
                float nearPlaneMinBound = Mathf.Min(0.01f * m_Range.floatValue, 0.1f);
                EditorGUILayout.Slider(m_ShadowsNearPlane, nearPlaneMinBound, 10.0f, s_Styles.ShadowNearPlane);
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUI.indentLevel -= 1;

            EditorGUILayout.Space();
        }

        void OnSceneGUI()
        {
            Light t = (Light)target;

            Color temp = Handles.color;
            if (t.enabled)
                Handles.color = kGizmoLight;
            else
                Handles.color = kGizmoDisabledLight;

            float thisRange = t.range;
            switch (t.type)
            {
                case LightType.Point:
                    thisRange = Handles.RadiusHandle(Quaternion.identity, t.transform.position, thisRange, true);

                    if (GUI.changed)
                    {
                        Undo.RecordObject(t, "Adjust Point Light");
                        t.range = thisRange;
                    }

                    break;

                case LightType.Spot:
                    // Give handles twice the alpha of the lines
                    Color col = Handles.color;
                    col.a = Mathf.Clamp01(temp.a * 2);
                    Handles.color = col;

                    Vector2 angleAndRange = new Vector2(t.spotAngle, t.range);
                    angleAndRange = Handles.ConeHandle(t.transform.rotation, t.transform.position, angleAndRange, 1.0f, 1.0f, true);
                    if (GUI.changed)
                    {
                        Undo.RecordObject(t, "Adjust Spot Light");
                        t.spotAngle = angleAndRange.x;
                        t.range = Mathf.Max(angleAndRange.y, 0.01F);
                    }
                    break;
                case LightType.Area:
                    EditorGUI.BeginChangeCheck();
                    Vector2 size = Handles.DoRectHandles(t.transform.rotation, t.transform.position, t.areaSize);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(t, "Adjust Area Light");
                        t.areaSize = size;
                    }
                    break;
                default:
                    break;
            }
            Handles.color = temp;
        }

        static Texture2D CreateKelvinGradientTexture(string name, int width, int height, float minKelvin, float maxKelvin)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.name = name;
            texture.hideFlags = HideFlags.HideAndDontSave;
            var pixels = new Color32[width * height];

            float mappedMax = Mathf.Pow(maxKelvin, 1f / kSliderPower);
            float mappedMin = Mathf.Pow(minKelvin, 1f / kSliderPower);

            for (int i = 0; i < width; i++)
            {
                float pixelfrac = i / (float)(width - 1);
                float mappedValue = (mappedMax - mappedMin) * pixelfrac + mappedMin;
                float kelvin = Mathf.Pow(mappedValue, kSliderPower);
                Color kelvinColor = Mathf.CorrelatedColorTemperatureToRGB(kelvin);
                for (int j = 0; j < height; j++)
                    pixels[j * width + i] = kelvinColor.gamma;
            }

            texture.SetPixels32(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }
    }
}
