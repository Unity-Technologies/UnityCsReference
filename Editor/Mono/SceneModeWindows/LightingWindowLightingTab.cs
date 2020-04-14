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
    internal class LightingWindowLightingTab
    {
        class Styles
        {
            public static readonly float buttonWidth = 200;

            public static readonly GUIContent newLightingSettings = EditorGUIUtility.TrTextContent("New Lighting Settings");

            public static readonly GUIContent workflowSettings = EditorGUIUtility.TrTextContent("Workflow Settings");
            public static readonly GUIContent lightProbeVisualization = EditorGUIUtility.TrTextContent("Light Probe Visualization");
            public static readonly GUIContent displayWeights = EditorGUIUtility.TrTextContent("Display Weights");
            public static readonly GUIContent displayOcclusion = EditorGUIUtility.TrTextContent("Display Occlusion");
            public static readonly GUIContent highlightInvalidCells = EditorGUIUtility.TrTextContent("Highlight Invalid Cells", "Highlight the invalid cells that cannot be used for probe interpolation.");
            public static readonly GUIContent progressiveGPUBakingDevice = EditorGUIUtility.TrTextContent("GPU Baking Device", "Will list all available GPU devices.");
            public static readonly GUIContent progressiveGPUChangeWarning = EditorGUIUtility.TrTextContent("Changing the compute device used by the Progressive GPU Lightmapper requires the editor to be relaunched. Do you want to change device and restart?");
            public static readonly GUIContent concurrentJobs = EditorGUIUtility.TrTextContent("Concurrent Jobs", "The amount of simultaneously scheduled jobs.");
            public static readonly GUIContent progressiveGPUUnknownDeviceInfo = EditorGUIUtility.TrTextContent("No devices found. Please start an initial bake to make this information available.");

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
        }

        SavedBool m_ShowWorkflowSettings;
        SavedBool m_ShowProbeDebugSettings;
        Vector2 m_ScrollPosition = Vector2.zero;

        LightingWindowBakeSettings m_BakeSettings;

        SerializedObject m_LightmapSettings;
        SerializedProperty m_LightingSettingsAsset;

        int m_LightmapDeviceAndPlatform;

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

            m_ShowWorkflowSettings = new SavedBool("LightingWindow.ShowWorkflowSettings", true);
            m_ShowProbeDebugSettings = new SavedBool("LightingWindow.ShowProbeDebugSettings", false);

            string configDeviceAndPlatform = EditorUserSettings.GetConfigValue("lightmappingDeviceAndPlatform");
            if (configDeviceAndPlatform != null)
                m_LightmapDeviceAndPlatform = Int32.Parse(configDeviceAndPlatform);
            else
                EditorUserSettings.SetConfigValue("lightmappingDeviceAndPlatform", "0");
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

            EditorGUILayout.PropertyField(m_LightingSettingsAsset);

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(Styles.newLightingSettings, GUILayout.Width(170)))
            {
                var ls = new LightingSettings();
                ls.name = "New Lighting Settings";
                Undo.RecordObject(m_LightmapSettings.targetObject, "New Lighting Settings");
                Lightmapping.lightingSettingsInternal = ls;
                ProjectWindowUtil.CreateAsset(ls, (ls.name + ".lighting"));
            }

            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            m_BakeSettings.OnGUI();
            WorkflowSettingsGUI();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            lightmapSettings.ApplyModifiedProperties();
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

                        using (new EditorGUI.DisabledScope(devicesAndPlatforms.Length < 2))
                        {
                            m_LightmapDeviceAndPlatform = EditorGUILayout.IntPopup(Styles.progressiveGPUBakingDevice, m_LightmapDeviceAndPlatform, lightmappingDeviceStrings, lightmappingDeviceIndices);
                        }

                        string configDeviceAndPlatform = EditorUserSettings.GetConfigValue("lightmappingDeviceAndPlatform");
                        int oldDeviceAndPlatform = 0;

                        if (configDeviceAndPlatform != null)
                            oldDeviceAndPlatform = Int32.Parse(configDeviceAndPlatform);

                        if (oldDeviceAndPlatform != m_LightmapDeviceAndPlatform)
                        {
                            if (EditorUtility.DisplayDialog("Warning", Styles.progressiveGPUChangeWarning.text, "OK", "Cancel"))
                            {
                                EditorUserSettings.SetConfigValue("lightmappingDeviceAndPlatform", m_LightmapDeviceAndPlatform.ToString());
                                DeviceAndPlatform selectedDeviceAndPlatform = devicesAndPlatforms[m_LightmapDeviceAndPlatform];

                                EditorApplication.CloseAndRelaunch(new string[] { "-OpenCL-PlatformAndDeviceIndices", selectedDeviceAndPlatform.platformId.ToString(), selectedDeviceAndPlatform.deviceId.ToString() });
                            }
                            else
                            {
                                EditorUserSettings.SetConfigValue("lightmappingDeviceAndPlatform", oldDeviceAndPlatform.ToString());
                                m_LightmapDeviceAndPlatform = oldDeviceAndPlatform;
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
