// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(Light))]
    [CanEditMultipleObjects]
    public class LightEditor : Editor
    {
        public sealed class Settings
        {
            private SerializedObject m_SerializedObject;

            public SerializedProperty lightType { get; private set; }
            public SerializedProperty range { get; private set; }
            public SerializedProperty spotAngle { get; private set; }
            public SerializedProperty cookieSize { get; private set; }
            public SerializedProperty color { get; private set; }
            public SerializedProperty intensity { get; private set; }
            public SerializedProperty bounceIntensity { get; private set; }
            public SerializedProperty colorTemperature { get; private set; }
            public SerializedProperty useColorTemperature { get; private set; }
            public SerializedProperty cookieProp { get; private set; }
            public SerializedProperty shadowsType { get; private set; }
            public SerializedProperty shadowsStrength { get; private set; }
            public SerializedProperty shadowsResolution { get; private set; }
            public SerializedProperty shadowsBias { get; private set; }
            public SerializedProperty shadowsNormalBias { get; private set; }
            public SerializedProperty shadowsNearPlane { get; private set; }
            public SerializedProperty halo { get; private set; }
            public SerializedProperty flare { get; private set; }
            public SerializedProperty renderMode { get; private set; }
            public SerializedProperty cullingMask { get; private set; }
            public SerializedProperty lightmapping { get; private set; }
            public SerializedProperty areaSizeX { get; private set; }
            public SerializedProperty areaSizeY { get; private set; }
            public SerializedProperty bakedShadowRadiusProp { get; private set; }
            public SerializedProperty bakedShadowAngleProp { get; private set; }


            Texture2D m_KelvinGradientTexture;
            const float kMinKelvin = 1000f;
            const float kMaxKelvin = 20000f;
            const float kSliderPower = 2f;

            public Settings(SerializedObject so)
            {
                m_SerializedObject = so;
            }

            class Styles
            {
                public readonly GUIContent Type = EditorGUIUtility.TrTextContent("Type", "Specifies the current type of light. Possible types are Directional, Spot, Point, and Area lights.");
                public readonly GUIContent Range = EditorGUIUtility.TrTextContent("Range", "Controls how far the light is emitted from the center of the object.");
                public readonly GUIContent SpotAngle = EditorGUIUtility.TrTextContent("Spot Angle", "Controls the angle in degrees at the base of a Spot light's cone.");
                public readonly GUIContent Color = EditorGUIUtility.TrTextContent("Color", "Controls the color being emitted by the light.");
                public readonly GUIContent UseColorTemperature = EditorGUIUtility.TrTextContent("Use color temperature mode", "Choose between RGB and temperature mode for light's color.");
                public readonly GUIContent ColorFilter = EditorGUIUtility.TrTextContent("Filter", "A colored gel can be put in front of the light source to tint the light.");
                public readonly GUIContent ColorTemperature = EditorGUIUtility.TrTextContent("Temperature", "Also known as CCT (Correlated color temperature). The color temperature of the electromagnetic radiation emitted from an ideal black body is defined as its surface temperature in Kelvin. White is 6500K");
                public readonly GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity", "Controls the brightness of the light. Light color is multiplied by this value.");
                public readonly GUIContent LightmappingMode = EditorGUIUtility.TrTextContent("Mode", "Specifies the light mode used to determine if and how a light will be baked. Possible modes are Baked, Mixed, and Realtime.");
                public readonly GUIContent LightBounceIntensity = EditorGUIUtility.TrTextContent("Indirect Multiplier", "Controls the intensity of indirect light being contributed to the scene. A value of 0 will cause Realtime lights to be removed from realtime global illumination and Baked and Mixed lights to no longer emit indirect lighting. Has no effect when both Realtime and Baked Global Illumination are disabled.");
                public readonly GUIContent ShadowType = EditorGUIUtility.TrTextContent("Shadow Type", "Specifies whether Hard Shadows, Soft Shadows, or No Shadows will be cast by the light.");
                //realtime
                public readonly GUIContent ShadowRealtimeSettings = EditorGUIUtility.TrTextContent("Realtime Shadows", "Settings for realtime direct shadows.");
                public readonly GUIContent ShadowStrength = EditorGUIUtility.TrTextContent("Strength", "Controls how dark the shadows cast by the light will be.");
                public readonly GUIContent ShadowResolution = EditorGUIUtility.TrTextContent("Resolution", "Controls the rendered resolution of the shadow maps. A higher resolution will increase the fidelity of shadows at the cost of GPU performance and memory usage.");
                public readonly GUIContent ShadowBias = EditorGUIUtility.TrTextContent("Bias", "Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
                public readonly GUIContent ShadowNormalBias = EditorGUIUtility.TrTextContent("Normal Bias", "Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
                public readonly GUIContent ShadowNearPlane = EditorGUIUtility.TrTextContent("Near Plane", "Controls the value for the near clip plane when rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.");
                //baked
                public readonly GUIContent BakedShadowRadius = EditorGUIUtility.TrTextContent("Baked Shadow Radius", "Controls the amount of artificial softening applied to the edges of shadows cast by the Point or Spot light.");
                public readonly GUIContent BakedShadowAngle = EditorGUIUtility.TrTextContent("Baked Shadow Angle", "Controls the amount of artificial softening applied to the edges of shadows cast by directional lights.");

                public readonly GUIContent Cookie = EditorGUIUtility.TrTextContent("Cookie", "Specifies the Texture mask to cast shadows, create silhouettes, or patterned illumination for the light.");
                public readonly GUIContent CookieSize = EditorGUIUtility.TrTextContent("Cookie Size", "Controls the size of the cookie mask currently assigned to the light.");
                public readonly GUIContent DrawHalo = EditorGUIUtility.TrTextContent("Draw Halo", "When enabled, draws a spherical halo of light with a radius equal to the lights range value.");
                public readonly GUIContent Flare = EditorGUIUtility.TrTextContent("Flare", "Specifies the flare object to be used by the light to render lens flares in the scene.");
                public readonly GUIContent RenderMode = EditorGUIUtility.TrTextContent("Render Mode", "Specifies the importance of the light which impacts lighting fidelity and performance. Options are Auto, Important, and Not Important. This only affects Forward Rendering");
                public readonly GUIContent CullingMask = EditorGUIUtility.TrTextContent("Culling Mask", "Specifies which layers will be affected or excluded from the light's effect on objects in the scene.");

                public readonly GUIContent AreaWidth = EditorGUIUtility.TrTextContent("Width", "Controls the width in units of the area light.");
                public readonly GUIContent AreaHeight = EditorGUIUtility.TrTextContent("Height", "Controls the height in units of the area light.");

                public readonly GUIContent BakingWarning = EditorGUIUtility.TrTextContent("Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.");
                public readonly GUIContent IndirectBounceShadowWarning = EditorGUIUtility.TrTextContent("Realtime indirect bounce shadowing is not supported for Spot and Point lights.");
                public readonly GUIContent CookieWarning = EditorGUIUtility.TrTextContent("Cookie textures for spot lights should be set to clamp, not repeat, to avoid artifacts.");

                public readonly GUIContent[] LightmapBakeTypeTitles = { EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Mixed"), EditorGUIUtility.TrTextContent("Baked") };
                public readonly int[] LightmapBakeTypeValues = { (int)LightmapBakeType.Realtime, (int)LightmapBakeType.Mixed, (int)LightmapBakeType.Baked };
            }

            static Styles s_Styles;

            public bool isRealtime { get { return lightmapping.intValue == 4; } }
            public bool isCompletelyBaked { get { return lightmapping.intValue == 2; } }
            public bool isBakedOrMixed { get { return !isRealtime; } }
            public Texture cookie { get { return cookieProp.objectReferenceValue as Texture; } }

            internal bool typeIsSame { get { return !lightType.hasMultipleDifferentValues; } }
            internal bool shadowTypeIsSame { get { return !shadowsType.hasMultipleDifferentValues; } }
            private bool lightmappingTypeIsSame { get { return !lightmapping.hasMultipleDifferentValues; } }
            public Light light { get { return m_SerializedObject.targetObject as Light; } }

            internal bool bounceWarningValue
            {
                get
                {
                    return typeIsSame && (light.type == LightType.Point || light.type == LightType.Spot) &&
                        lightmappingTypeIsSame && isRealtime && !bounceIntensity.hasMultipleDifferentValues && bounceIntensity.floatValue > 0.0F;
                }
            }
            internal bool bakingWarningValue { get { return !Lightmapping.bakedGI && lightmappingTypeIsSame && isBakedOrMixed; } }

            internal bool cookieWarningValue
            {
                get
                {
                    return typeIsSame && light.type == LightType.Spot &&
                        !cookieProp.hasMultipleDifferentValues && cookie && cookie.wrapMode != TextureWrapMode.Clamp;
                }
            }

            public void OnEnable()
            {
                lightType = m_SerializedObject.FindProperty("m_Type");
                range = m_SerializedObject.FindProperty("m_Range");
                spotAngle = m_SerializedObject.FindProperty("m_SpotAngle");
                cookieSize = m_SerializedObject.FindProperty("m_CookieSize");
                color = m_SerializedObject.FindProperty("m_Color");
                intensity = m_SerializedObject.FindProperty("m_Intensity");
                bounceIntensity = m_SerializedObject.FindProperty("m_BounceIntensity");
                colorTemperature = m_SerializedObject.FindProperty("m_ColorTemperature");
                useColorTemperature = m_SerializedObject.FindProperty("m_UseColorTemperature");
                cookieProp = m_SerializedObject.FindProperty("m_Cookie");
                shadowsType = m_SerializedObject.FindProperty("m_Shadows.m_Type");
                shadowsStrength = m_SerializedObject.FindProperty("m_Shadows.m_Strength");
                shadowsResolution = m_SerializedObject.FindProperty("m_Shadows.m_Resolution");
                shadowsBias = m_SerializedObject.FindProperty("m_Shadows.m_Bias");
                shadowsNormalBias = m_SerializedObject.FindProperty("m_Shadows.m_NormalBias");
                shadowsNearPlane = m_SerializedObject.FindProperty("m_Shadows.m_NearPlane");
                halo = m_SerializedObject.FindProperty("m_DrawHalo");
                flare = m_SerializedObject.FindProperty("m_Flare");
                renderMode = m_SerializedObject.FindProperty("m_RenderMode");
                cullingMask = m_SerializedObject.FindProperty("m_CullingMask");
                lightmapping = m_SerializedObject.FindProperty("m_Lightmapping");
                areaSizeX = m_SerializedObject.FindProperty("m_AreaSize.x");
                areaSizeY = m_SerializedObject.FindProperty("m_AreaSize.y");
                bakedShadowRadiusProp = m_SerializedObject.FindProperty("m_ShadowRadius");
                bakedShadowAngleProp = m_SerializedObject.FindProperty("m_ShadowAngle");

                if (m_KelvinGradientTexture == null)
                    m_KelvinGradientTexture = CreateKelvinGradientTexture("KelvinGradientTexture", 300, 16, kMinKelvin, kMaxKelvin);
            }

            public void OnDestroy()
            {
                if (m_KelvinGradientTexture != null)
                    DestroyImmediate(m_KelvinGradientTexture);
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

            public void Update()
            {
                if (s_Styles == null)
                    s_Styles = new Styles();

                m_SerializedObject.Update();
            }

            public void DrawLightType()
            {
                EditorGUILayout.PropertyField(lightType, s_Styles.Type);
            }

            public void DrawRange(bool showAreaOptions)
            {
                // If the light is an area light, the range is determined by other parameters.
                // Therefore, disable area light's range for editing, but just update the editor field.
                if (showAreaOptions)
                {
                    GUI.enabled = false;
                    string areaLightToolTip = "For area lights " + range.displayName + " is computed from Width, Height and Intensity";
                    GUIContent areaRangeWithToolTip = new GUIContent(range.displayName, areaLightToolTip);
                    EditorGUILayout.FloatField(areaRangeWithToolTip, light.range);
                    GUI.enabled = true;
                }
                else
                    EditorGUILayout.PropertyField(range, s_Styles.Range);
            }

            public void DrawSpotAngle()
            {
                EditorGUILayout.Slider(spotAngle, 1f, 179f, s_Styles.SpotAngle);
            }

            public void DrawArea()
            {
                EditorGUILayout.PropertyField(areaSizeX, s_Styles.AreaWidth);
                EditorGUILayout.PropertyField(areaSizeY, s_Styles.AreaHeight);
            }

            public void DrawColor()
            {
                if (GraphicsSettings.lightsUseLinearIntensity && GraphicsSettings.lightsUseColorTemperature)
                {
                    EditorGUILayout.PropertyField(useColorTemperature, s_Styles.UseColorTemperature);
                    if (useColorTemperature.boolValue)
                    {
                        EditorGUILayout.LabelField(s_Styles.Color);
                        EditorGUI.indentLevel += 1;
                        EditorGUILayout.PropertyField(color, s_Styles.ColorFilter);
                        EditorGUILayout.SliderWithTexture(s_Styles.ColorTemperature, colorTemperature, kMinKelvin, kMaxKelvin, kSliderPower, m_KelvinGradientTexture);
                        EditorGUI.indentLevel -= 1;
                    }
                    else
                        EditorGUILayout.PropertyField(color, s_Styles.Color);
                }
                else
                    EditorGUILayout.PropertyField(color, s_Styles.Color);
            }

            public void DrawLightmapping()
            {
                EditorGUILayout.IntPopup(lightmapping, s_Styles.LightmapBakeTypeTitles, s_Styles.LightmapBakeTypeValues, s_Styles.LightmappingMode);

                // Warning if GI Baking disabled and m_Lightmapping isn't realtime
                if (bakingWarningValue)
                {
                    EditorGUILayout.HelpBox(s_Styles.BakingWarning.text, MessageType.Info);
                }
            }

            public void DrawIntensity()
            {
                EditorGUILayout.PropertyField(intensity, s_Styles.Intensity);
            }

            public void DrawBounceIntensity()
            {
                EditorGUILayout.PropertyField(bounceIntensity, s_Styles.LightBounceIntensity);
                // Indirect shadows warning (Should be removed when we support realtime indirect shadows)
                if (bounceWarningValue)
                {
                    EditorGUILayout.HelpBox(s_Styles.IndirectBounceShadowWarning.text, MessageType.Info);
                }
            }

            public void DrawCookie()
            {
                EditorGUILayout.PropertyField(cookieProp, s_Styles.Cookie);

                if (cookieWarningValue)
                {
                    // warn on spotlights if the cookie is set to repeat
                    EditorGUILayout.HelpBox(s_Styles.CookieWarning.text, MessageType.Warning);
                }
            }

            public void DrawCookieSize()
            {
                EditorGUILayout.PropertyField(cookieSize, s_Styles.CookieSize);
            }

            public void DrawHalo()
            {
                EditorGUILayout.PropertyField(halo, s_Styles.DrawHalo);
            }

            public void DrawFlare()
            {
                EditorGUILayout.PropertyField(flare, s_Styles.Flare);
            }

            public void DrawRenderMode()
            {
                EditorGUILayout.PropertyField(renderMode, s_Styles.RenderMode);
            }

            public void DrawCullingMask()
            {
                EditorGUILayout.PropertyField(cullingMask, s_Styles.CullingMask);
            }

            public void ApplyModifiedProperties()
            {
                m_SerializedObject.ApplyModifiedProperties();
            }

            public void DrawShadowsType()
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(shadowsType, s_Styles.ShadowType);
            }

            public void DrawBakedShadowRadius()
            {
                using (new EditorGUI.DisabledScope(shadowsType.intValue != (int)LightShadows.Soft))
                {
                    EditorGUILayout.PropertyField(bakedShadowRadiusProp, s_Styles.BakedShadowRadius);
                }
            }

            public void DrawBakedShadowAngle()
            {
                using (new EditorGUI.DisabledScope(shadowsType.intValue != (int)LightShadows.Soft))
                {
                    EditorGUILayout.Slider(bakedShadowAngleProp, 0.0F, 90.0F, s_Styles.BakedShadowAngle);
                }
            }

            public void DrawRuntimeShadow()
            {
                EditorGUILayout.LabelField(s_Styles.ShadowRealtimeSettings);
                EditorGUI.indentLevel += 1;
                EditorGUILayout.Slider(shadowsStrength, 0f, 1f, s_Styles.ShadowStrength);
                EditorGUILayout.PropertyField(shadowsResolution, s_Styles.ShadowResolution);
                EditorGUILayout.Slider(shadowsBias, 0.0f, 2.0f, s_Styles.ShadowBias);
                EditorGUILayout.Slider(shadowsNormalBias, 0.0f, 3.0f, s_Styles.ShadowNormalBias);

                // this min bound should match the calculation in SharedLightData::GetNearPlaneMinBound()
                float nearPlaneMinBound = Mathf.Min(0.01f * range.floatValue, 0.1f);
                EditorGUILayout.Slider(shadowsNearPlane, nearPlaneMinBound, 10.0f, s_Styles.ShadowNearPlane);
                EditorGUI.indentLevel -= 1;
            }
        }

        private class Style
        {
            public readonly GUIContent iconRemove = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove command buffer");
            public readonly GUIContent DisabledLightWarning = EditorGUIUtility.TrTextContent("Lighting has been disabled in at least one Scene view. Any changes applied to lights in the Scene will not be updated in these views until Lighting has been enabled again.");
            public readonly GUIStyle invisibleButton = "InvisibleButton";
        }

        private static Style s_Styles;

        private Settings m_Settings;
        protected Settings settings
        {
            get
            {
                if (m_Settings == null)
                    m_Settings = new Settings(serializedObject);
                return m_Settings;
            }
        }

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

        // Should match same colors in GizmoDrawers.cpp!
        internal static Color kGizmoLight = new Color(254 / 255f, 253 / 255f, 136 / 255f, 128 / 255f);
        internal static Color kGizmoDisabledLight = new Color(135 / 255f, 116 / 255f, 50 / 255f, 128 / 255f);

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

        bool spotOptionsValue { get { return settings.typeIsSame && settings.light.type == LightType.Spot; } }
        bool pointOptionsValue { get { return settings.typeIsSame && settings.light.type == LightType.Point; } }
        bool dirOptionsValue { get { return settings.typeIsSame && settings.light.type == LightType.Directional; } }
        bool areaOptionsValue { get { return settings.typeIsSame && settings.light.type == LightType.Area; } }
        bool runtimeOptionsValue { get { return settings.typeIsSame && (settings.light.type != LightType.Area && !settings.isCompletelyBaked); } }
        bool bakedShadowRadius { get { return settings.typeIsSame && (settings.light.type == LightType.Point || settings.light.type == LightType.Spot) && settings.isBakedOrMixed; } }
        bool bakedShadowAngle { get { return settings.typeIsSame && settings.light.type == LightType.Directional && settings.isBakedOrMixed; } }
        bool shadowOptionsValue { get { return settings.shadowTypeIsSame && settings.light.shadows != LightShadows.None; } }


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
            SetOptions(m_AnimShowLightBounceIntensity, initialize, true);
        }

        protected virtual void OnEnable()
        {
            settings.OnEnable();

            UpdateShowOptions(true);
        }

        protected virtual void OnDestroy()
        {
            if (m_Settings != null)
            {
                m_Settings.OnDestroy();
                m_Settings = null;
            }
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
                s_Styles = new Style();

            settings.Update();

            UpdateShowOptions(false);

            // Light type (shape and usage)
            settings.DrawLightType();

            EditorGUILayout.Space();

            // When we are switching between two light types that don't show the range (directional and area lights)
            // we want the fade group to stay hidden.
            //bool keepRangeHidden = m_ShowDirOptions.isAnimating && m_ShowDirOptions.target;
            //float fadeRange = keepRangeHidden ? 0.0f : 1.0f - m_ShowDirOptions.faded;
            // Light Range
            if (EditorGUILayout.BeginFadeGroup(1.0f - m_AnimShowDirOptions.faded))
                settings.DrawRange(m_AnimShowAreaOptions.target);
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_AnimShowSpotOptions.faded))
                settings.DrawSpotAngle();
            EditorGUILayout.EndFadeGroup();

            // Area width & height
            if (EditorGUILayout.BeginFadeGroup(m_AnimShowAreaOptions.faded))
                settings.DrawArea();
            EditorGUILayout.EndFadeGroup();

            settings.DrawColor();

            EditorGUILayout.Space();

            // Baking type
            if (EditorGUILayout.BeginFadeGroup(1.0F - m_AnimShowAreaOptions.faded))
                settings.DrawLightmapping();
            EditorGUILayout.EndFadeGroup();

            settings.DrawIntensity();

            if (EditorGUILayout.BeginFadeGroup(m_AnimShowLightBounceIntensity.faded))
                settings.DrawBounceIntensity();
            EditorGUILayout.EndFadeGroup();

            ShadowsGUI();

            if (EditorGUILayout.BeginFadeGroup(m_AnimShowRuntimeOptions.faded))
                settings.DrawCookie();
            EditorGUILayout.EndFadeGroup();

            // Cookie size also requires directional light
            if (EditorGUILayout.BeginFadeGroup(m_AnimShowRuntimeOptions.faded * m_AnimShowDirOptions.faded))
                settings.DrawCookieSize();
            EditorGUILayout.EndFadeGroup();

            settings.DrawHalo();
            settings.DrawFlare();
            settings.DrawRenderMode();
            settings.DrawCullingMask();

            EditorGUILayout.Space();
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.m_SceneLighting == false)
                EditorGUILayout.HelpBox(s_Styles.DisabledLightWarning.text, MessageType.Warning);

            CommandBufferGUI();

            settings.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
        }

        void ShadowsGUI()
        {
            //NOTE: FadeGroup's dont support nesting. Thus we just multiply the fade values here.

            // Shadows drop-down. Area lights can only be baked and always have shadows.
            float show = 1 - m_AnimShowAreaOptions.faded;

            if (EditorGUILayout.BeginFadeGroup(show))
                settings.DrawShadowsType();
            EditorGUILayout.EndFadeGroup();

            EditorGUI.indentLevel += 1;
            show *= m_AnimShowShadowOptions.faded;

            // Baked Shadow radius
            if (EditorGUILayout.BeginFadeGroup(show * m_AnimBakedShadowRadiusOptions.faded))
                settings.DrawBakedShadowRadius();
            EditorGUILayout.EndFadeGroup();

            // Baked Shadow angle
            if (EditorGUILayout.BeginFadeGroup(show * m_AnimBakedShadowAngleOptions.faded))
                settings.DrawBakedShadowAngle();
            EditorGUILayout.EndFadeGroup();

            // Runtime shadows - shadow strength, resolution, bias
            if (EditorGUILayout.BeginFadeGroup(show * m_AnimShowRuntimeOptions.faded))
                settings.DrawRuntimeShadow();
            EditorGUILayout.EndFadeGroup();

            EditorGUI.indentLevel -= 1;

            EditorGUILayout.Space();
        }

        protected virtual void OnSceneGUI()
        {
            Light t = target as Light;

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
    }
}
