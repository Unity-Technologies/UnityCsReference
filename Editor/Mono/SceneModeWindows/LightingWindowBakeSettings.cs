// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class LightingWindowBakeSettings
    {
        // light modes
        LightModeUtil            m_LightModeUtil;
        private bool             m_ShowRealtimeLightsSettings       = true;
        private bool             m_ShowMixedLightsSettings          = true;
        private bool             m_ShowGeneralLightmapSettings      = true;
        private const string     kShowRealtimeLightsSettingsKey     = "ShowRealtimeLightsSettings";
        private const string     kShowMixedLightsSettingsKey        = "ShowMixedLightsSettings";
        private const string     kShowGeneralLightmapSettingsKey    = "ShowGeneralLightmapSettings";

        SerializedObject m_LightmapSettingsSO;
        Object m_LightmapSettings;

        SerializedObject m_RenderSettingsSO;

        //realtime GI
        SerializedProperty m_Resolution;

        //baked
        SerializedProperty m_AlbedoBoost;
        SerializedProperty m_IndirectOutputScale;
        SerializedProperty m_LightmapParameters;
        SerializedProperty m_LightmapDirectionalMode;
        SerializedProperty m_BakeResolution;
        SerializedProperty m_Padding;
        SerializedProperty m_AmbientOcclusion;
        SerializedProperty m_AOMaxDistance;
        SerializedProperty m_CompAOExponent;
        SerializedProperty m_CompAOExponentDirect;
        SerializedProperty m_TextureCompression;
        SerializedProperty m_FinalGather;
        SerializedProperty m_FinalGatherRayCount;
        SerializedProperty m_FinalGatherFiltering;
        SerializedProperty m_LightmapSize;
        SerializedProperty m_SubtractiveShadowColor;
        SerializedProperty m_BakeBackend;
        //SerializedProperty m_PVRSampling; // TODO(PVR): make non-fixed sampling modes work.
        SerializedProperty m_PVRSampleCount;
        SerializedProperty m_PVRDirectSampleCount;
        SerializedProperty m_PVRBounces;
        SerializedProperty m_PVRCulling;
        SerializedProperty m_PVRFilteringMode;
        SerializedProperty m_PVRFilterTypeDirect;
        SerializedProperty m_PVRFilterTypeIndirect;
        SerializedProperty m_PVRFilterTypeAO;
        SerializedProperty m_PVRFilteringGaussRadiusDirect;
        SerializedProperty m_PVRFilteringGaussRadiusIndirect;
        SerializedProperty m_PVRFilteringGaussRadiusAO;
        SerializedProperty m_PVRFilteringAtrousPositionSigmaDirect;
        SerializedProperty m_PVRFilteringAtrousPositionSigmaIndirect;
        SerializedProperty m_PVRFilteringAtrousPositionSigmaAO;

        SerializedProperty  m_BounceScale;
        SerializedProperty  m_UpdateThreshold;

        static bool PlayerHasSM20Support()
        {
            var apis = PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget);
            bool hasSM20Api = apis.Contains(UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2) || apis.Contains(UnityEngine.Rendering.GraphicsDeviceType.N3DS);
            return hasSM20Api;
        }

        public LightingWindowBakeSettings()
        {
            m_LightModeUtil = LightModeUtil.Get();
        }

        private void InitSettings()
        {
            Object renderSettings = RenderSettings.GetRenderSettings();
            SerializedObject rso = m_RenderSettingsSO = new SerializedObject(renderSettings);

            m_SubtractiveShadowColor = rso.FindProperty("m_SubtractiveShadowColor");

            m_LightmapSettings = LightmapEditorSettings.GetLightmapSettings();
            SerializedObject so = m_LightmapSettingsSO = new SerializedObject(m_LightmapSettings);

            //realtime GI
            m_Resolution = so.FindProperty("m_LightmapEditorSettings.m_Resolution");

            //baked
            m_AlbedoBoost = so.FindProperty("m_GISettings.m_AlbedoBoost");
            m_IndirectOutputScale = so.FindProperty("m_GISettings.m_IndirectOutputScale");
            m_LightmapParameters = so.FindProperty("m_LightmapEditorSettings.m_LightmapParameters");
            m_LightmapDirectionalMode = so.FindProperty("m_LightmapEditorSettings.m_LightmapsBakeMode");
            m_BakeResolution = so.FindProperty("m_LightmapEditorSettings.m_BakeResolution");
            m_Padding = so.FindProperty("m_LightmapEditorSettings.m_Padding");
            m_AmbientOcclusion = so.FindProperty("m_LightmapEditorSettings.m_AO");
            m_AOMaxDistance = so.FindProperty("m_LightmapEditorSettings.m_AOMaxDistance");
            m_CompAOExponent = so.FindProperty("m_LightmapEditorSettings.m_CompAOExponent");
            m_CompAOExponentDirect = so.FindProperty("m_LightmapEditorSettings.m_CompAOExponentDirect");
            m_TextureCompression = so.FindProperty("m_LightmapEditorSettings.m_TextureCompression");
            m_FinalGather = so.FindProperty("m_LightmapEditorSettings.m_FinalGather");
            m_FinalGatherRayCount = so.FindProperty("m_LightmapEditorSettings.m_FinalGatherRayCount");
            m_FinalGatherFiltering = so.FindProperty("m_LightmapEditorSettings.m_FinalGatherFiltering");
            m_LightmapSize = so.FindProperty("m_LightmapEditorSettings.m_TextureWidth");
            m_BakeBackend = so.FindProperty("m_LightmapEditorSettings.m_BakeBackend");
            //m_PVRSampling = so.FindProperty("m_LightmapEditorSettings.m_PVRSampling"); // TODO(PVR): make non-fixed sampling modes work.
            m_PVRSampleCount = so.FindProperty("m_LightmapEditorSettings.m_PVRSampleCount");
            m_PVRDirectSampleCount = so.FindProperty("m_LightmapEditorSettings.m_PVRDirectSampleCount");
            m_PVRBounces = so.FindProperty("m_LightmapEditorSettings.m_PVRBounces");
            m_PVRCulling = so.FindProperty("m_LightmapEditorSettings.m_PVRCulling");
            m_PVRFilteringMode = so.FindProperty("m_LightmapEditorSettings.m_PVRFilteringMode");
            m_PVRFilterTypeDirect = so.FindProperty("m_LightmapEditorSettings.m_PVRFilterTypeDirect");
            m_PVRFilterTypeIndirect = so.FindProperty("m_LightmapEditorSettings.m_PVRFilterTypeIndirect");
            m_PVRFilterTypeAO = so.FindProperty("m_LightmapEditorSettings.m_PVRFilterTypeAO");
            m_PVRFilteringGaussRadiusDirect = so.FindProperty("m_LightmapEditorSettings.m_PVRFilteringGaussRadiusDirect");
            m_PVRFilteringGaussRadiusIndirect = so.FindProperty("m_LightmapEditorSettings.m_PVRFilteringGaussRadiusIndirect");
            m_PVRFilteringGaussRadiusAO = so.FindProperty("m_LightmapEditorSettings.m_PVRFilteringGaussRadiusAO");
            m_PVRFilteringAtrousPositionSigmaDirect = so.FindProperty("m_LightmapEditorSettings.m_PVRFilteringAtrousPositionSigmaDirect");
            m_PVRFilteringAtrousPositionSigmaIndirect = so.FindProperty("m_LightmapEditorSettings.m_PVRFilteringAtrousPositionSigmaIndirect");
            m_PVRFilteringAtrousPositionSigmaAO = so.FindProperty("m_LightmapEditorSettings.m_PVRFilteringAtrousPositionSigmaAO");


            //dev debug properties
            m_BounceScale = so.FindProperty("m_GISettings.m_BounceScale");
            m_UpdateThreshold = so.FindProperty("m_GISettings.m_TemporalCoherenceThreshold");
        }

        public void OnEnable()
        {
            InitSettings();

            m_ShowGeneralLightmapSettings = SessionState.GetBool(kShowGeneralLightmapSettingsKey, true);
            m_ShowRealtimeLightsSettings = SessionState.GetBool(kShowRealtimeLightsSettingsKey, true);
            m_ShowMixedLightsSettings = SessionState.GetBool(kShowMixedLightsSettingsKey, true);
        }

        public void OnDisable()
        {
            SessionState.SetBool(kShowGeneralLightmapSettingsKey, m_ShowGeneralLightmapSettings);
            SessionState.SetBool(kShowRealtimeLightsSettingsKey, m_ShowRealtimeLightsSettings);
            SessionState.SetBool(kShowMixedLightsSettingsKey, m_ShowMixedLightsSettings);

            m_LightmapSettingsSO.Dispose();
            m_LightmapSettings = null;
            m_RenderSettingsSO.Dispose();
        }

        void Repaint() { InspectorWindow.RepaintAllInspectors(); }

        static void DrawResolutionField(SerializedProperty resolution, GUIContent label)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(resolution, label);

            GUILayout.Label(" texels per unit", Styles.LabelStyle);
            GUILayout.EndHorizontal();
        }

        static void DrawFilterSettingField(SerializedProperty gaussSetting,
            SerializedProperty atrousSetting,
            GUIContent gaussLabel,
            GUIContent atrousLabel,
            LightmapEditorSettings.FilterType type)
        {
            if (type == LightmapEditorSettings.FilterType.None)
                return;

            GUILayout.BeginHorizontal();

            if (type == LightmapEditorSettings.FilterType.Gaussian)
            {
                EditorGUILayout.IntSlider(gaussSetting, 0, 5, gaussLabel);
                GUILayout.Label(" texels", Styles.LabelStyle);
            }
            else if (type == LightmapEditorSettings.FilterType.ATrous)
            {
                EditorGUILayout.Slider(atrousSetting, 0.0f, 2.0f, atrousLabel);
                GUILayout.Label(" sigma", Styles.LabelStyle);
            }

            GUILayout.EndHorizontal();
        }

        static private bool isBuiltIn(SerializedProperty prop)
        {
            if (prop.objectReferenceValue != null)
            {
                var parameters = prop.objectReferenceValue as LightmapParameters;
                return (parameters.hideFlags == HideFlags.NotEditable);
            }

            return true;
        }

        static private bool LightmapParametersGUI(SerializedProperty prop, GUIContent content)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUIInternal.AssetPopup<LightmapParameters>(prop, content, "giparams", "Default-Medium");

            string label = "Edit...";

            if (isBuiltIn(prop))
                label = "View";

            bool editClicked = false;

            if (prop.objectReferenceValue == null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        Selection.activeObject = null;
                        editClicked = true;
                    }
                }
            }
            else
            {
                if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    Selection.activeObject = prop.objectReferenceValue;
                    editClicked = true;
                }
            }

            EditorGUILayout.EndHorizontal();

            return editClicked;
        }

        void RealtimeLightingGUI()
        {
            m_ShowRealtimeLightsSettings = EditorGUILayout.FoldoutTitlebar(m_ShowRealtimeLightsSettings, Styles.RealtimeLightsLabel, true);

            if (m_ShowRealtimeLightsSettings)
            {
                EditorGUI.indentLevel++;

                int realtimeMode, mixedMode;
                m_LightModeUtil.GetModes(out realtimeMode, out mixedMode);

                bool realtimeGI = (realtimeMode == 0);

                realtimeGI = EditorGUILayout.Toggle(Styles.UseRealtimeGI, realtimeGI);

                if (realtimeGI != (realtimeMode == 0))
                {
                    m_LightModeUtil.Store(realtimeGI ? 0 : 1, mixedMode);
                }

                if (realtimeGI && PlayerHasSM20Support())
                {
                    EditorGUILayout.HelpBox(Styles.NoRealtimeGIInSM2AndGLES2.text, MessageType.Warning);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }

        void MixedLightingGUI()
        {
            m_ShowMixedLightsSettings = EditorGUILayout.FoldoutTitlebar(m_ShowMixedLightsSettings, Styles.MixedLightsLabel, true);

            if (m_ShowMixedLightsSettings)
            {
                EditorGUI.indentLevel++;

                LightModeUtil.Get().DrawBakedGIElement();

                if (!LightModeUtil.Get().AreBakedLightmapsEnabled())
                {
                    EditorGUILayout.HelpBox(Styles.BakedGIDisabledInfo.text, MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(!LightModeUtil.Get().AreBakedLightmapsEnabled()))
                {
                    int realtimeMode, mixedMode;
                    m_LightModeUtil.GetModes(out realtimeMode, out mixedMode);
                    int newMixedMode = EditorGUILayout.IntPopup(Styles.MixedLightMode, mixedMode, Styles.MixedModeStrings, Styles.MixedModeValues);

                    if (LightModeUtil.Get().AreBakedLightmapsEnabled())
                    {
                        EditorGUILayout.HelpBox(Styles.HelpStringsMixed[mixedMode].text, MessageType.Info);
                    }

                    if (newMixedMode != mixedMode)
                    {
                        m_LightModeUtil.Store(realtimeMode, newMixedMode);
                    }

                    if (m_LightModeUtil.IsSubtractiveModeEnabled())
                    {
                        EditorGUILayout.PropertyField(m_SubtractiveShadowColor, Styles.SubtractiveShadowColor);
                        m_RenderSettingsSO.ApplyModifiedProperties();
                        EditorGUILayout.Space();
                    }
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }

        public void DeveloperBuildSettingsGUI()
        {
            if (!Unsupported.IsDeveloperBuild())
                return;

            Lightmapping.concurrentJobsType = (Lightmapping.ConcurrentJobsType)EditorGUILayout.IntPopup(Styles.ConcurrentJobs, (int)Lightmapping.concurrentJobsType, Styles.ConcurrentJobsTypeStrings, Styles.ConcurrentJobsTypeValues);
            Lightmapping.enlightenForceUpdates = EditorGUILayout.Toggle(Styles.ForceUpdates, Lightmapping.enlightenForceUpdates);
            Lightmapping.enlightenForceWhiteAlbedo = EditorGUILayout.Toggle(Styles.ForceWhiteAlbedo, Lightmapping.enlightenForceWhiteAlbedo);
            Lightmapping.filterMode = (FilterMode)EditorGUILayout.EnumPopup(EditorGUIUtility.TempContent("Filter Mode"), Lightmapping.filterMode);

            EditorGUILayout.Slider(m_BounceScale, 0.0f, 10.0f, Styles.BounceScale);
            EditorGUILayout.Slider(m_UpdateThreshold, 0.0f, 1.0f, Styles.UpdateThreshold);

            if (GUILayout.Button("Clear disk cache", GUILayout.Width(LightingWindow.kButtonWidth)))
            {
                Lightmapping.Clear();
                Lightmapping.ClearDiskCache();
            }

            if (GUILayout.Button("Print state to console", GUILayout.Width(LightingWindow.kButtonWidth)))
            {
                Lightmapping.PrintStateToConsole();
            }

            if (GUILayout.Button("Reset albedo/emissive", GUILayout.Width(LightingWindow.kButtonWidth)))
                GIDebugVisualisation.ResetRuntimeInputTextures();

            if (GUILayout.Button("Reset environment", GUILayout.Width(LightingWindow.kButtonWidth)))
                DynamicGI.UpdateEnvironment();
        }

        void GeneralLightmapSettingsGUI()
        {
            m_ShowGeneralLightmapSettings = EditorGUILayout.FoldoutTitlebar(m_ShowGeneralLightmapSettings, Styles.GeneralLightmapLabel, true);
            if (m_ShowGeneralLightmapSettings)
            {
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(!LightModeUtil.Get().IsAnyGIEnabled()))
                {
                    using (new EditorGUI.DisabledScope(!LightModeUtil.Get().AreBakedLightmapsEnabled()))
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(m_BakeBackend, Styles.BakeBackend);
                        if (EditorGUI.EndChangeCheck())
                            InspectorWindow.RepaintAllInspectors(); // We need to repaint other inspectors that might need to update based on the selected backend.
                        if (LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveCPU)
                        {
                            EditorGUI.indentLevel++;

                            EditorGUILayout.PropertyField(m_PVRCulling, Styles.PVRCulling);

                            // Sampling type
                            //EditorGUILayout.PropertyField(m_PvrSampling, Styles.m_PVRSampling); // TODO(PVR): make non-fixed sampling modes work.

                            if (LightmapEditorSettings.sampling != LightmapEditorSettings.Sampling.Auto)
                            {
                                // Update those constants also in LightmapBake.cpp UpdateSamples().
                                const int kMinSamples = 10;
                                const int kMaxSamples = 100000;

                                // Sample count
                                // TODO(PVR): make non-fixed sampling modes work.
                                //EditorGUI.indentLevel++;
                                //if (LightmapEditorSettings.giPathTracerSampling == LightmapEditorSettings.PathTracerSampling.PathTracerSamplingAdaptive)
                                //  EditorGUILayout.PropertyField(m_PVRSampleCount, Styles.PVRSampleCountAdaptive);
                                //else

                                EditorGUILayout.PropertyField(m_PVRDirectSampleCount, Styles.PVRDirectSampleCount);
                                EditorGUILayout.PropertyField(m_PVRSampleCount, Styles.PVRIndirectSampleCount);

                                if (m_PVRSampleCount.intValue < kMinSamples || m_PVRSampleCount.intValue > kMaxSamples)
                                {
                                    m_PVRSampleCount.intValue = Math.Max(Math.Min(m_PVRSampleCount.intValue, kMaxSamples), kMinSamples);
                                }

                                // TODO(PVR): make non-fixed sampling modes work.
                                //EditorGUI.indentLevel--;
                            }

                            EditorGUILayout.IntPopup(m_PVRBounces, Styles.BouncesStrings, Styles.BouncesValues, Styles.PVRBounces);

                            // Filtering
                            EditorGUILayout.PropertyField(m_PVRFilteringMode, Styles.PVRFilteringMode);

                            if (m_PVRFilteringMode.enumValueIndex == (int)LightmapEditorSettings.FilterMode.Advanced)
                            {
                                EditorGUI.indentLevel++;

                                EditorGUILayout.PropertyField(m_PVRFilterTypeDirect, Styles.PVRFilterTypeDirect);
                                DrawFilterSettingField(m_PVRFilteringGaussRadiusDirect, m_PVRFilteringAtrousPositionSigmaDirect,
                                    Styles.PVRFilteringGaussRadiusDirect, Styles.PVRFilteringAtrousPositionSigmaDirect,
                                    LightmapEditorSettings.filterTypeDirect);

                                EditorGUILayout.Space();

                                EditorGUILayout.PropertyField(m_PVRFilterTypeIndirect, Styles.PVRFilterTypeIndirect);
                                DrawFilterSettingField(m_PVRFilteringGaussRadiusIndirect, m_PVRFilteringAtrousPositionSigmaIndirect,
                                    Styles.PVRFilteringGaussRadiusIndirect, Styles.PVRFilteringAtrousPositionSigmaIndirect,
                                    LightmapEditorSettings.filterTypeIndirect);

                                using (new EditorGUI.DisabledScope(!m_AmbientOcclusion.boolValue))
                                {
                                    EditorGUILayout.Space();

                                    EditorGUILayout.PropertyField(m_PVRFilterTypeAO, Styles.PVRFilterTypeAO);
                                    DrawFilterSettingField(m_PVRFilteringGaussRadiusAO, m_PVRFilteringAtrousPositionSigmaAO,
                                        Styles.PVRFilteringGaussRadiusAO, Styles.PVRFilteringAtrousPositionSigmaAO,
                                        LightmapEditorSettings.filterTypeAO);
                                }

                                EditorGUI.indentLevel--;
                            }

                            EditorGUI.indentLevel--;
                            EditorGUILayout.Space();
                        }
                    }

                    using (new EditorGUI.DisabledScope((LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveCPU) && !LightModeUtil.Get().IsRealtimeGIEnabled()))
                    {
                        DrawResolutionField(m_Resolution, Styles.IndirectResolution);
                    }

                    using (new EditorGUI.DisabledScope(!LightModeUtil.Get().AreBakedLightmapsEnabled()))
                    {
                        DrawResolutionField(m_BakeResolution, Styles.LightmapResolution);

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(m_Padding, Styles.Padding);
                        GUILayout.Label(" texels", Styles.LabelStyle);
                        GUILayout.EndHorizontal();

                        EditorGUILayout.IntPopup(m_LightmapSize, Styles.LightmapSizeStrings, Styles.LightmapSizeValues, Styles.LightmapSize);

                        EditorGUILayout.PropertyField(m_TextureCompression, Styles.TextureCompression);

                        EditorGUILayout.PropertyField(m_AmbientOcclusion, Styles.AmbientOcclusion);
                        if (m_AmbientOcclusion.boolValue)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(m_AOMaxDistance, Styles.AOMaxDistance);
                            if (m_AOMaxDistance.floatValue < 0.0f)
                                m_AOMaxDistance.floatValue = 0.0f;
                            EditorGUILayout.Slider(m_CompAOExponent, 0.0f, 10.0f, Styles.AmbientOcclusionContribution);
                            EditorGUILayout.Slider(m_CompAOExponentDirect, 0.0f, 10.0f, Styles.AmbientOcclusionContributionDirect);
                            EditorGUI.indentLevel--;
                        }

                        if (LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.Enlighten)
                        {
                            EditorGUILayout.PropertyField(m_FinalGather, Styles.FinalGather);
                            if (m_FinalGather.boolValue)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.PropertyField(m_FinalGatherRayCount, Styles.FinalGatherRayCount);
                                EditorGUILayout.PropertyField(m_FinalGatherFiltering, Styles.FinalGatherFiltering);
                                EditorGUI.indentLevel--;
                            }
                        }
                    }

                    EditorGUILayout.IntPopup(m_LightmapDirectionalMode, Styles.LightmapDirectionalModeStrings, Styles.LightmapDirectionalModeValues, Styles.LightmapDirectionalMode);

                    if ((int)LightmapsMode.CombinedDirectional == m_LightmapDirectionalMode.intValue && PlayerHasSM20Support())
                        EditorGUILayout.HelpBox(Styles.NoDirectionalInSM2AndGLES2.text, MessageType.Warning);

                    EditorGUILayout.Slider(m_IndirectOutputScale, 0.0f, 5.0f, Styles.IndirectOutputScale);

                    // albedo boost, push the albedo value towards one in order to get more bounce
                    EditorGUILayout.Slider(m_AlbedoBoost, 1.0f, 10.0f, Styles.AlbedoBoost);

                    if (LightmapParametersGUI(m_LightmapParameters, Styles.DefaultLightmapParameters))
                    {
                        EditorWindow.FocusWindowIfItsOpen<InspectorWindow>();
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }
            }
        }

        public void OnGUI()
        {
            if (m_LightmapSettings == null || m_LightmapSettings != LightmapEditorSettings.GetLightmapSettings())
            {
                InitSettings();
            }

            m_LightmapSettingsSO.UpdateIfRequiredOrScript();

            RealtimeLightingGUI();
            MixedLightingGUI();
            GeneralLightmapSettingsGUI();

            m_LightmapSettingsSO.ApplyModifiedProperties();
        }

        static class Styles
        {
            public static readonly int[] LightmapDirectionalModeValues = { (int)LightmapsMode.NonDirectional, (int)LightmapsMode.CombinedDirectional };
            public static readonly GUIContent[] LightmapDirectionalModeStrings =
            {
                new GUIContent("Non-Directional"),
                new GUIContent("Directional"),
            };

            public static readonly int[] LightmapSizeValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
            public static readonly GUIContent[] LightmapSizeStrings = Array.ConvertAll(LightmapSizeValues, (x) => new GUIContent(x.ToString()));

            public static readonly int[] ConcurrentJobsTypeValues = { (int)Lightmapping.ConcurrentJobsType.Min, (int)Lightmapping.ConcurrentJobsType.Low, (int)Lightmapping.ConcurrentJobsType.High };
            public static readonly GUIContent[] ConcurrentJobsTypeStrings =
            {
                new GUIContent("Min"),
                new GUIContent("Low"),
                new GUIContent("High")
            };

            // must match LightmapMixedBakeMode
            public static readonly int[] MixedModeValues = { 0, 2, 1 };
            public static readonly GUIContent[] MixedModeStrings =
            {
                EditorGUIUtility.TextContent("Baked Indirect"),
                EditorGUIUtility.TextContent("Shadowmask"),
                EditorGUIUtility.TextContent("Subtractive")
            };

            public static readonly int[] BouncesValues = { 0, 1, 2, 3, 4 };
            public static readonly GUIContent[] BouncesStrings =
            {
                EditorGUIUtility.TextContent("None"),
                EditorGUIUtility.TextContent("1"),
                EditorGUIUtility.TextContent("2"),
                EditorGUIUtility.TextContent("3"),
                EditorGUIUtility.TextContent("4")
            };

            public static readonly GUIContent[] HelpStringsMixed =
            {
                EditorGUIUtility.TextContent("Mixed lights provide realtime direct lighting while indirect light is baked into lightmaps and light probes."),
                EditorGUIUtility.TextContent("Mixed lights provide baked direct and indirect lighting for static objects. Dynamic objects receive realtime direct lighting and cast shadows on static objects using the main directional light in the scene."),
                EditorGUIUtility.TextContent("Mixed lights provide realtime direct lighting. Indirect lighting gets baked into lightmaps and light probes. Shadowmasks and light probes occlusion get generated for baked shadows. The Shadowmask Mode used at run time can be set in the Quality Settings panel.")
            };

            public static readonly GUIContent BounceScale = EditorGUIUtility.TextContent("Bounce Scale|Multiplier for indirect lighting. Use with care.");
            public static readonly GUIContent UpdateThreshold = EditorGUIUtility.TextContent("Update Threshold|Threshold for updating realtime GI. A lower value causes more frequent updates (default 1.0).");
            public static readonly GUIContent AlbedoBoost = EditorGUIUtility.TextContent("Albedo Boost|Controls the amount of light bounced between surfaces by intensifying the albedo of materials in the scene. Increasing this draws the albedo value towards white for indirect light computation. The default value is physically accurate.");
            public static readonly GUIContent IndirectOutputScale = EditorGUIUtility.TextContent("Indirect Intensity|Controls the brightness of indirect light stored in realtime and baked lightmaps. A value above 1.0 will increase the intensity of indirect light while a value less than 1.0 will reduce indirect light intensity.");
            public static readonly GUIContent LightmapDirectionalMode = EditorGUIUtility.TextContent("Directional Mode|Controls whether baked and realtime lightmaps will store directional lighting information from the lighting environment. Options are Directional and Non-Directional.");
            public static readonly GUIContent DefaultLightmapParameters = EditorGUIUtility.TextContent("Lightmap Parameters|Allows the adjustment of advanced parameters that affect the process of generating a lightmap for an object using global illumination.");
            public static readonly GUIContent RealtimeLightsLabel = EditorGUIUtility.TextContent("Realtime Lighting|Precompute Realtime indirect lighting for realtime lights and static objects. In this mode realtime lights, ambient lighting, materials of static objects (including emission) will generate indirect lighting at runtime. Only static objects are blocking and bouncing light, dynamic objects receive indirect lighting via light probes.");
            public static readonly GUIContent MixedLightsLabel = EditorGUIUtility.TextContent("Mixed Lighting|Bake Global Illumination for mixed lights and static objects. May bake both direct and/or indirect lighting based on settings. Only static objects are blocking and bouncing light, dynamic objects receive baked lighting via light probes.");
            public static readonly GUIContent GeneralLightmapLabel = EditorGUIUtility.TextContent("Lightmapping Settings|Settings that apply to both Global Illumination modes (Precomputed Realtime and Baked).");
            public static readonly GUIContent NoDirectionalInSM2AndGLES2 = EditorGUIUtility.TextContent("Directional lightmaps cannot be decoded on SM2.0 hardware nor when using GLES2.0. They will fallback to Non-Directional lightmaps.");
            public static readonly GUIContent NoRealtimeGIInSM2AndGLES2 = EditorGUIUtility.TextContent("Realtime Global Illumination is not supported on SM2.0 hardware nor when using GLES2.0.");
            public static readonly GUIContent ConcurrentJobs = EditorGUIUtility.TextContent("Concurrent Jobs|The amount of simultaneously scheduled jobs.");
            public static readonly GUIContent ForceWhiteAlbedo = EditorGUIUtility.TextContent("Force White Albedo|Force white albedo during lighting calculations.");
            public static readonly GUIContent ForceUpdates = EditorGUIUtility.TextContent("Force Updates|Force continuous updates of runtime indirect lighting calculations.");
            public static readonly GUIStyle   LabelStyle = EditorStyles.wordWrappedMiniLabel;
            public static readonly GUIContent IndirectResolution = EditorGUIUtility.TextContent("Indirect Resolution|Sets the resolution in texels that are used per unit for objects being lit by indirect lighting. The larger the value, the more significant the impact will be on the time it takes to bake the lighting.");
            public static readonly GUIContent LightmapResolution = EditorGUIUtility.TextContent("Lightmap Resolution|Sets the resolution in texels that are used per unit for objects being lit by baked global illumination. Larger values will result in increased time to calculate the baked lighting.");
            public static readonly GUIContent Padding = EditorGUIUtility.TextContent("Lightmap Padding|Sets the separation in texels between shapes in the baked lightmap.");
            public static readonly GUIContent LightmapSize = EditorGUIUtility.TextContent("Lightmap Size|Sets the resolution of the full lightmap Texture in pixels. Values are squared, so a setting of 1024 will produce a 1024x1024 pixel sized lightmap.");
            public static readonly GUIContent TextureCompression = EditorGUIUtility.TextContent("Compress Lightmaps|Controls whether the baked lightmap is compressed or not. When enabled, baked lightmaps are compressed to reduce required storage space but some artifacting may be present due to compression.");
            public static readonly GUIContent AmbientOcclusion = EditorGUIUtility.TextContent("Ambient Occlusion|Specifies whether to include ambient occlusion or not in the baked lightmap result. Enabling this results in simulating the soft shadows that occur in cracks and crevices of objects when light is reflected onto them.");
            public static readonly GUIContent AmbientOcclusionContribution = EditorGUIUtility.TextContent("Indirect Contribution|Adjusts the contrast of ambient occlusion applied to indirect lighting. The larger the value, the more contrast is applied to the ambient occlusion for indirect lighting.");
            public static readonly GUIContent AmbientOcclusionContributionDirect = EditorGUIUtility.TextContent("Direct Contribution|Adjusts the contrast of ambient occlusion applied to the direct lighting. The larger the value is, the more contrast is applied to the ambient occlusion for direct lighting. This effect is not physically accurate.");
            public static readonly GUIContent AOMaxDistance = EditorGUIUtility.TextContent("Max Distance|Controls how far rays are cast in order to determine if an object is occluded or not. A larger value produces longer rays and contributes more shadows to the lightmap, while a smaller value produces shorter rays that contribute shadows only when objects are very close to one another. A value of 0 casts an infinitely long ray that has no maximum distance.");
            public static readonly GUIContent FinalGather = EditorGUIUtility.TextContent("Final Gather|Specifies whether the final light bounce of the global illumination calculation is calculated at the same resolution as the baked lightmap. When enabled, visual quality is improved at the cost of additional time required to bake the lighting.");
            public static readonly GUIContent FinalGatherRayCount = EditorGUIUtility.TextContent("Ray Count|Controls the number of rays emitted for every final gather point.");
            public static readonly GUIContent FinalGatherFiltering = EditorGUIUtility.TextContent("Denoising|Controls whether a denoising filter is applied to the final gather output.");
            public static readonly GUIContent SubtractiveShadowColor = EditorGUIUtility.TextContent("Realtime Shadow Color|The color used for mixing realtime shadows with baked lightmaps in Subtractive lighting mode. The color defines the darkest point of the realtime shadow.");
            public static readonly GUIContent MixedLightMode = EditorGUIUtility.TextContent("Lighting Mode|Specifies which Scene lighting mode will be used for all Mixed lights in the Scene. Options are Baked Indirect, Shadowmask and Subtractive.");
            public static readonly GUIContent UseRealtimeGI = EditorGUIUtility.TextContent("Realtime Global Illumination|Controls whether Realtime lights in the Scene contribute indirect light. If enabled, Realtime lights contribute both direct and indirect light. If disabled, Realtime lights only contribute direct light. This can be disabled on a per-light basis in the light component Inspector by setting Indirect Multiplier to 0.");
            public static readonly GUIContent BakedGIDisabledInfo = EditorGUIUtility.TextContent("All Baked and Mixed lights in the Scene are currently being overridden to Realtime light modes. Enable Baked Global Illumination to allow the use of Baked and Mixed light modes.");
            public static readonly GUIContent BakeBackend = EditorGUIUtility.TextContent("Lightmapper|Specifies which baking system will be used to generate baked lightmaps.");
            //public static readonly GUIContent PVRSampling = EditorGUIUtility.TextContent("Sampling|How to sample the lightmaps. Auto and adaptive automatically tests for convergence. Auto uses a maximum of 16K samples. Adaptive uses a configurable maximum number of samples. Fixed always uses the set number of samples and does not test for convergence.");
            //public static readonly GUIContent PVRDirectSampleCountAdaptive = EditorGUIUtility.TextContent("Max Direct Samples|Maximum number of samples to use for direct lighting.");
            public static readonly GUIContent PVRDirectSampleCount = EditorGUIUtility.TextContent("Direct Samples|Controls the number of samples the lightmapper will use for direct lighting calculations. Increasing this value may improve the quality of lightmaps but increases the time required for baking to complete.");
            //public static readonly GUIContent PVRSampleCountAdaptive = EditorGUIUtility.TextContent("Max Indirect Samples|Maximum number of samples to use for indirect lighting.");
            public static readonly GUIContent PVRIndirectSampleCount = EditorGUIUtility.TextContent("Indirect Samples|Controls the number of samples the lightmapper will use for indirect lighting calculations. Increasing this value may improve the quality of lightmaps but increases the time required for baking to complete.");
            public static readonly GUIContent PVRBounces = EditorGUIUtility.TextContent("Bounces|Controls the maximum number of bounces the lightmapper will compute for indirect light.");
            public static readonly GUIContent PVRFilteringMode = EditorGUIUtility.TextContent("Filtering|Specifies the method used to reduce noise in baked lightmaps. Options are None, Automatic, or Advanced.");
            public static readonly GUIContent PVRFiltering = EditorGUIUtility.TextContent("Filtering|Specifies the filter kernel used to reduce the amount of noise in baked lightmaps.");
            public static readonly GUIContent PVRFilteringAdvanced = EditorGUIUtility.TextContent("Advanced Filter Settings|Show advanced settings to configure filtering on lightmaps.");
            public static readonly GUIContent PVRFilterTypeDirect = EditorGUIUtility.TextContent("Direct Filter|Specifies the filter kernel applied to the direct light stored in the lightmap. Gaussian will blur the lightmap with some loss of detail. A-Trous will reduce noise based on a threshold while maintaining edge detail.");
            public static readonly GUIContent PVRFilterTypeIndirect = EditorGUIUtility.TextContent("Indirect Filter|Specifies the filter kernel applied to the indirect light stored in the lightmap. Gaussian will blur the lightmap with some loss of detail. A-Trous will reduce noise based on a threshold while maintaining edge detail.");
            public static readonly GUIContent PVRFilterTypeAO = EditorGUIUtility.TextContent("Ambient Occlusion Filter|Specifies the filter kernel applied to the ambient occlusion stored in the lightmap. Gaussian will blur the lightmap with some loss of detail. A-Trous will reduce noise based on a threshold while maintaining edge detail.");
            public static readonly GUIContent PVRFilteringGaussRadiusDirect = EditorGUIUtility.TextContent("Direct Radius|Controls the radius of the filter for direct light stored in the lightmap. A higher value will increase the strength of the blur, reducing noise from direct light in the lightmap.");
            public static readonly GUIContent PVRFilteringGaussRadiusIndirect = EditorGUIUtility.TextContent("Indirect Radius|Controls the radius of the filter for indirect light stored in the lightmap. A higher value will increase the strength of the blur, reducing noise from indirect light in the lightmap.");
            public static readonly GUIContent PVRFilteringGaussRadiusAO = EditorGUIUtility.TextContent("Ambient Occlusion Radius|The radius of the filter for ambient occlusion in the lightmap. A higher radius will increase the blur strength, reducing sampling noise from ambient occlusion in the lightmap.");
            public static readonly GUIContent PVRFilteringAtrousPositionSigmaDirect = EditorGUIUtility.TextContent("Direct Sigma|Controls the threshold of the filter for direct light stored in the lightmap. A higher value will increase the threshold, reducing noise in the direct layer of the lightmap. Too high of a value can cause a loss of detail in the lightmap.");
            public static readonly GUIContent PVRFilteringAtrousPositionSigmaIndirect = EditorGUIUtility.TextContent("Indirect Sigma|Controls the threshold of the filter for indirect light stored in the lightmap. A higher value will increase the threshold, reducing noise in the indirect layer of the lightmap. Too high of a value can cause a loss of detail in the lightmap.");
            public static readonly GUIContent PVRFilteringAtrousPositionSigmaAO = EditorGUIUtility.TextContent("Ambient Occlusion Sigma|Controls the threshold of the filter for ambient occlusion stored in the lightmap. A higher value will increase the threshold, reducing noise in the ambient occlusion layer of the lightmap. Too high of a value can cause a loss of detail in the lightmap.");
            public static readonly GUIContent PVRCulling = EditorGUIUtility.TextContent("Prioritize View|Specifies whether the lightmapper should prioritize baking texels within the scene view. When disabled, objects outside the scene view will have the same priority as those in the scene view.");
        }
    }
} // namespace
