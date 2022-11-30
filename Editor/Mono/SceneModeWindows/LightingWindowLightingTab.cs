// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditorInternal;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using System.Globalization;

namespace UnityEditor
{
    internal class LightingWindowLightingTab : LightingWindow.WindowTab
    {
        class Styles
        {
            public static readonly float buttonWidth = 200;

            public static readonly GUIContent newLightingSettings = EditorGUIUtility.TrTextContent("New", "Create a new Lighting Settings Asset with default settings.");
            public static readonly GUIContent cloneLightingSettings = EditorGUIUtility.TrTextContent("Clone", "Create a new Lighting Settings Asset based on the current settings.");

            public static readonly GUIContent lightingSettings = EditorGUIUtility.TrTextContent("Lighting Settings");
            public static readonly GUIContent workflowSettings = EditorGUIUtility.TrTextContent("Workflow Settings");
            public static readonly GUIContent lightProbeVisualization = EditorGUIUtility.TrTextContent("Light Probe Visualization");
            public static readonly GUIContent displayWeights = EditorGUIUtility.TrTextContent("Display Weights");
            public static readonly GUIContent displayOcclusion = EditorGUIUtility.TrTextContent("Display Occlusion");
            public static readonly GUIContent highlightInvalidCells = EditorGUIUtility.TrTextContent("Highlight Invalid Cells", "Highlight the invalid cells that cannot be used for probe interpolation.");
            public static readonly GUIContent progressiveGPUBakingDevice = EditorGUIUtility.TrTextContent("GPU Baking Device", "Will list all available GPU devices.");
            public static readonly GUIContent gpuBakingProfile = EditorGUIUtility.TrTextContent("GPU Baking Profile", "The profile chosen for trading off between performance and memory usage when baking using the GPU.");
            public static readonly GUIContent progressiveGPUChangeWarning = EditorGUIUtility.TrTextContent("Changing the compute device used by the Progressive GPU Lightmapper requires the editor to be relaunched. Do you want to change device and restart?");
            public static readonly GUIContent concurrentJobs = EditorGUIUtility.TrTextContent("Concurrent Jobs", "The amount of simultaneously scheduled jobs.");
            public static readonly GUIContent progressiveGPUUnknownDeviceInfo = EditorGUIUtility.TrTextContent("No devices found. Please start an initial bake to make this information available.");
            public static readonly GUIContent recalculateEnvironmentLighting = EditorGUIUtility.TrTextContent("Recalculate Environment Lighting", "Whether to automatically generate environment lighting in cases where the Active Scene has not previously been baked. This affects the ambient Light Probe and default cubemap which are both generated from the sky.");

            public static readonly int[] progressiveGPUUnknownDeviceValues = { 0 };
            public static readonly GUIContent[] progressiveGPUUnknownDeviceStrings =
            {
                EditorGUIUtility.TrTextContent("Unknown"),
            };

            public static readonly int[] concurrentJobsTypeValues = { (int)Lightmapping.ConcurrentJobsType.Min, (int)Lightmapping.ConcurrentJobsType.Low, (int)Lightmapping.ConcurrentJobsType.High };
            public static readonly GUIContent[] concurrentJobsTypeStrings =
            {
                EditorGUIUtility.TrTextContent("Min"),
                EditorGUIUtility.TrTextContent("Low"),
                EditorGUIUtility.TrTextContent("High")
            };

            // Keep in sync with BakingProfile.h::BakingProfile
            public static readonly int bakingProfileDefault = 2;
            public static readonly int[] bakingProfileValues = {0, 1, 2, 3, 4};
            public static readonly GUIContent[] bakingProfileStrings =
            {
                EditorGUIUtility.TrTextContent("Highest Performance"),
                EditorGUIUtility.TrTextContent("High Performance"),
                EditorGUIUtility.TrTextContent("Balanced"),
                EditorGUIUtility.TrTextContent("Low Memory Usage"),
                EditorGUIUtility.TrTextContent("Lowest Memory Usage"),
            };
        }

        SavedBool m_ShowLightingSettings;
        SavedBool m_ShowWorkflowSettings;
        SavedBool m_ShowProbeDebugSettings;
        Vector2 m_ScrollPosition = Vector2.zero;
        string m_LightmappingDeviceIndexKey = "lightmappingDeviceIndex";
        string m_BakingProfileKey = "lightmappingBakingProfile";
        LightingWindowBakeSettings m_BakeSettings;

        SerializedObject m_LightmapSettings;
        SerializedProperty m_LightingSettingsAsset;

        SerializedObject lightmapSettings
        {
            get
            {
                // if we set a new scene as the active scene, we need to make sure to respond to those changes
                if (m_LightmapSettings == null || m_LightmapSettings.targetObject != LightmapEditorSettings.GetLightmapSettings())
                {
                    m_LightmapSettings = new SerializedObject(LightmapEditorSettings.GetLightmapSettings());
                    m_LightingSettingsAsset = m_LightmapSettings.FindProperty("m_LightingSettings");
                }

                return m_LightmapSettings;
            }
        }

        public void OnEnable()
        {
            m_BakeSettings = new LightingWindowBakeSettings();
            m_BakeSettings.OnEnable();

            m_ShowLightingSettings = new SavedBool("LightingWindow.ShowLightingSettings", true);
            m_ShowWorkflowSettings = new SavedBool("LightingWindow.ShowWorkflowSettings", true);
            m_ShowProbeDebugSettings = new SavedBool("LightingWindow.ShowProbeDebugSettings", false);
        }

        public void OnDisable()
        {
            m_BakeSettings.OnDisable();
        }

        public void OnGUI()
        {
            EditorGUIUtility.hierarchyMode = true;

            lightmapSettings.Update();

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            LightingSettingsGUI();

            m_BakeSettings.OnGUI();
            WorkflowSettingsGUI();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            lightmapSettings.ApplyModifiedProperties();
        }

        public void OnSummaryGUI()
        {
            LightingWindow.Summary();
        }

        public void OnSelectionChange()
        {
        }

        private void CreateLightingSettings(LightingSettings from = null)
        {
            LightingSettings ls;
            if (from == null)
            {
                ls = new LightingSettings();
                ls.name = "New Lighting Settings";
            }
            else
            {
                ls = Object.Instantiate(from);
                ls.name = from.name;
            }
            Undo.RecordObject(m_LightmapSettings.targetObject, "New Lighting Settings");
            Lightmapping.lightingSettingsInternal = ls;
            ProjectWindowUtil.CreateAsset(ls, (ls.name + ".lighting"));
        }

        void LightingSettingsGUI()
        {
            m_ShowLightingSettings.value = EditorGUILayout.FoldoutTitlebar(m_ShowLightingSettings.value, Styles.lightingSettings, true);

            if (m_ShowLightingSettings.value)
            {
                ++EditorGUI.indentLevel;

                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(m_LightingSettingsAsset, GUIContent.Temp("Lighting Settings Asset"));

                if (GUILayout.Button(Styles.newLightingSettings, EditorStyles.miniButtonLeft, GUILayout.Width(50)))
                    CreateLightingSettings();
                else if (GUILayout.Button(Styles.cloneLightingSettings, EditorStyles.miniButtonRight, GUILayout.Width(50)))
                    CreateLightingSettings(Lightmapping.GetLightingSettingsOrDefaultsFallback());

                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                --EditorGUI.indentLevel;
            }
        }

        void WorkflowSettingsGUI()
        {
            m_ShowWorkflowSettings.value = EditorGUILayout.FoldoutTitlebar(m_ShowWorkflowSettings.value, Styles.workflowSettings, true);

            if (m_ShowWorkflowSettings.value)
            {
                EditorGUI.indentLevel++;

                // GPU lightmapper device selection.
                if (Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapper == LightingSettings.Lightmapper.ProgressiveGPU)
                {
                    DeviceAndPlatform[] devicesAndPlatforms = Lightmapping.GetLightmappingGpuDevices();
                    if (devicesAndPlatforms.Length > 0)
                    {
                        int[] lightmappingDeviceIndices = Enumerable.Range(0, devicesAndPlatforms.Length).ToArray();
                        GUIContent[] lightmappingDeviceStrings = devicesAndPlatforms.Select(x => new GUIContent(x.name)).ToArray();

                        int bakingDeviceAndPlatform = -1;
                        string configDeviceAndPlatform = EditorUserSettings.GetConfigValue(m_LightmappingDeviceIndexKey);
                        if (configDeviceAndPlatform != null)
                        {
                            bakingDeviceAndPlatform = Int32.Parse(configDeviceAndPlatform);
                            bakingDeviceAndPlatform = Mathf.Clamp(bakingDeviceAndPlatform, 0, devicesAndPlatforms.Length - 1); // Removing a GPU and rebooting invalidates the saved value.
                        }
                        else
                            bakingDeviceAndPlatform = Lightmapping.GetLightmapBakeGPUDeviceIndex();

                        Debug.Assert(bakingDeviceAndPlatform != -1);

                        EditorGUI.BeginChangeCheck();
                        using (new EditorGUI.DisabledScope(devicesAndPlatforms.Length < 2))
                        {
                            bakingDeviceAndPlatform = EditorGUILayout.IntPopup(Styles.progressiveGPUBakingDevice, bakingDeviceAndPlatform, lightmappingDeviceStrings, lightmappingDeviceIndices);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (EditorUtility.DisplayDialog("Warning", Styles.progressiveGPUChangeWarning.text, "OK", "Cancel"))
                            {
                                EditorUserSettings.SetConfigValue(m_LightmappingDeviceIndexKey, bakingDeviceAndPlatform.ToString());
                                DeviceAndPlatform selectedDeviceAndPlatform = devicesAndPlatforms[bakingDeviceAndPlatform];
                                EditorApplication.CloseAndRelaunch(new string[] { "-OpenCL-PlatformAndDeviceIndices", selectedDeviceAndPlatform.platformId.ToString(), selectedDeviceAndPlatform.deviceId.ToString() });
                            }
                        }
                    }
                    else
                    {
                        // To show when we are still fetching info, so that the UI doesn't pop around too much for no reason
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.IntPopup(Styles.progressiveGPUBakingDevice, 0, Styles.progressiveGPUUnknownDeviceStrings, Styles.progressiveGPUUnknownDeviceValues);
                        }

                        EditorGUILayout.HelpBox(Styles.progressiveGPUUnknownDeviceInfo.text, MessageType.Info);
                    }

                    // Handle the baking profile setting
                    int bakingProfile = Styles.bakingProfileDefault;
                    string bakingProfileString = EditorUserSettings.GetConfigValue(m_BakingProfileKey);
                    if (bakingProfileString != null)
                    {
                        if (Int32.TryParse(bakingProfileString, out int bakingProfileStoredValue))
                        {
                            if (bakingProfileStoredValue >= 0 && bakingProfileStoredValue <= 4)
                                bakingProfile = bakingProfileStoredValue;
                        }
                    }
                    bakingProfile = EditorGUILayout.IntPopup(Styles.gpuBakingProfile, bakingProfile, Styles.bakingProfileStrings, Styles.bakingProfileValues);
                    EditorUserSettings.SetConfigValue(m_BakingProfileKey, bakingProfile.ToString());
                }

                m_ShowProbeDebugSettings.value = EditorGUILayout.Foldout(m_ShowProbeDebugSettings.value, Styles.lightProbeVisualization, true);

                if (m_ShowProbeDebugSettings.value)
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUI.indentLevel++;
                    LightProbeVisualization.lightProbeVisualizationMode = (LightProbeVisualization.LightProbeVisualizationMode)EditorGUILayout.EnumPopup(LightProbeVisualization.lightProbeVisualizationMode);
                    LightProbeVisualization.showInterpolationWeights = EditorGUILayout.Toggle(Styles.displayWeights, LightProbeVisualization.showInterpolationWeights);
                    LightProbeVisualization.showOcclusions = EditorGUILayout.Toggle(Styles.displayOcclusion, LightProbeVisualization.showOcclusions);
                    LightProbeVisualization.highlightInvalidCells = EditorGUILayout.Toggle(Styles.highlightInvalidCells, LightProbeVisualization.highlightInvalidCells);
                    EditorGUI.indentLevel--;

                    if (EditorGUI.EndChangeCheck())
                        EditorApplication.SetSceneRepaintDirty();
                }

                // If either auto ambient or auto reflection baking is supported, show the SkyManager toggle.
                if (SupportedRenderingFeatures.active.autoAmbientProbeBaking || SupportedRenderingFeatures.active.autoDefaultReflectionProbeBaking)
                {
                    BuiltinSkyManager.enabled = EditorGUILayout.Toggle(Styles.recalculateEnvironmentLighting, BuiltinSkyManager.enabled);
                }

                if (Unsupported.IsDeveloperMode())
                {
                    Lightmapping.concurrentJobsType = (Lightmapping.ConcurrentJobsType)EditorGUILayout.IntPopup(Styles.concurrentJobs, (int)Lightmapping.concurrentJobsType, Styles.concurrentJobsTypeStrings, Styles.concurrentJobsTypeValues);

                    if (GUILayout.Button("Clear disk cache", GUILayout.Width(Styles.buttonWidth)))
                    {
                        Lightmapping.Clear();
                        Lightmapping.ClearDiskCache();
                    }

                    if (GUILayout.Button("Print state to console", GUILayout.Width(Styles.buttonWidth)))
                    {
                        Lightmapping.PrintStateToConsole();
                    }

                    if (GUILayout.Button("Reset albedo/emissive", GUILayout.Width(Styles.buttonWidth)))
                        GIDebugVisualisation.ResetRuntimeInputTextures();

                    if (GUILayout.Button("Reset environment", GUILayout.Width(Styles.buttonWidth)))
                        DynamicGI.UpdateEnvironment();
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }
    }
} // namespace
