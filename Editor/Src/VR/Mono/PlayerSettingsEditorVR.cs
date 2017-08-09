// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;

namespace UnityEditorInternal.VR
{
    internal class PlayerSettingsEditorVR
    {
        static class Styles
        {
            public static readonly GUIContent singlepassAndroidWarning = EditorGUIUtility.TextContent("Single-pass stereo rendering requires OpenGL ES 3. Please make sure that it's the first one listed under Graphics APIs.");
            public static readonly GUIContent singlepassAndroidWarning2 = EditorGUIUtility.TextContent("Multi-pass stereo rendering will be used on Android devices that don't support single-pass stereo rendering.");
            public static readonly GUIContent singlePassStereoRendering = EditorGUIUtility.TextContent("Single-Pass Stereo Rendering");

            public static readonly GUIContent[] kDefaultStereoRenderingPaths =
            {
                new GUIContent("Multi Pass"),
                new GUIContent("Single Pass"),
                new GUIContent("Single Pass Instanced (Preview)")
            };

            public static readonly GUIContent[] kAndroidStereoRenderingPaths =
            {
                new GUIContent("Multi Pass"),
                new GUIContent("Single Pass (Preview)"),
            };

            public static readonly GUIContent xrSettingsTitle = EditorGUIUtility.TextContent("XR Settings");

            public static readonly GUIContent supportedCheckbox = EditorGUIUtility.TextContent("Virtual Reality Supported");
            public static readonly GUIContent listHeader = EditorGUIUtility.TextContent("Virtual Reality SDKs");
        }

        private PlayerSettingsEditor m_Settings;

        private Dictionary<BuildTargetGroup, VRDeviceInfoEditor[]> m_AllVRDevicesForBuildTarget = new Dictionary<BuildTargetGroup, VRDeviceInfoEditor[]>();
        private Dictionary<BuildTargetGroup, ReorderableList> m_VRDeviceActiveUI = new Dictionary<BuildTargetGroup, ReorderableList>();

        private Dictionary<string, string> m_MapVRDeviceKeyToUIString = new Dictionary<string, string>();
        private Dictionary<string, string> m_MapVRUIStringToDeviceKey = new Dictionary<string, string>();

        private Dictionary<string, VRCustomOptions> m_CustomOptions = new Dictionary<string, VRCustomOptions>();
        private SerializedProperty m_StereoRenderingPath;

        private SerializedProperty m_AndroidEnableTango;
        private SerializedProperty m_AndroidTangoUsesCamera;


        internal int GUISectionIndex { get; set; }

        public PlayerSettingsEditorVR(PlayerSettingsEditor settingsEditor)
        {
            m_Settings = settingsEditor;
            m_StereoRenderingPath = m_Settings.serializedObject.FindProperty("m_StereoRenderingPath");

            m_AndroidEnableTango = m_Settings.FindPropertyAssert("AndroidEnableTango");
            m_AndroidTangoUsesCamera = m_Settings.FindPropertyAssert("AndroidTangoUsesCamera");
        }

        private void RefreshVRDeviceList(BuildTargetGroup targetGroup)
        {
            VRDeviceInfoEditor[] deviceInfos = VREditor.GetAllVRDeviceInfo(targetGroup);
            deviceInfos = deviceInfos.OrderBy(d => d.deviceNameUI).ToArray();
            m_AllVRDevicesForBuildTarget[targetGroup] = deviceInfos;

            for (int i = 0; i < deviceInfos.Length; ++i)
            {
                VRDeviceInfoEditor deviceInfo = deviceInfos[i];
                m_MapVRDeviceKeyToUIString[deviceInfo.deviceNameKey] = deviceInfo.deviceNameUI;
                m_MapVRUIStringToDeviceKey[deviceInfo.deviceNameUI] = deviceInfo.deviceNameKey;

                // Create custom UI options if they exist for this sdk
                VRCustomOptions customOptions;
                if (!m_CustomOptions.TryGetValue(deviceInfo.deviceNameKey, out customOptions))
                {
                    Type optionsType = Type.GetType("UnityEditorInternal.VR.VRCustomOptions" + deviceInfo.deviceNameKey, false, true);
                    if (optionsType != null)
                    {
                        customOptions = (VRCustomOptions)Activator.CreateInstance(optionsType);
                    }
                    else
                    {
                        customOptions = new VRCustomOptionsNone();
                    }
                    customOptions.Initialize(m_Settings.serializedObject);
                    m_CustomOptions.Add(deviceInfo.deviceNameKey, customOptions);
                }
            }
        }

        internal bool TargetGroupSupportsVirtualReality(BuildTargetGroup targetGroup)
        {
            if (!m_AllVRDevicesForBuildTarget.ContainsKey(targetGroup))
            {
                RefreshVRDeviceList(targetGroup);
            }

            VRDeviceInfoEditor[] supportedDevices = m_AllVRDevicesForBuildTarget[targetGroup];

            return supportedDevices.Length > 0;
        }

        internal void XRSectionGUI(BuildTargetGroup targetGroup, int sectionIndex)
        {
            GUISectionIndex = sectionIndex;

            if (!TargetGroupSupportsVirtualReality(targetGroup) && !TargetGroupSupportsVuforia(targetGroup))
                return;

            if (m_Settings.BeginSettingsBox(sectionIndex, Styles.xrSettingsTitle))
            {
                if (EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox("Changing XRSettings in not allowed in play mode.", MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying)) // switching VR flags in play mode is not supported
                {
                    if (TargetGroupSupportsVirtualReality(targetGroup))
                    {
                        DevicesGUI(targetGroup);

                        SinglePassStereoGUI(targetGroup, m_StereoRenderingPath);
                    }

                    if (targetGroup == BuildTargetGroup.Android)
                    {
                        // Google Tango settings
                        EditorGUILayout.PropertyField(m_AndroidEnableTango, EditorGUIUtility.TextContent("Tango Supported"));

                        if (PlayerSettings.Android.androidTangoEnabled)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(m_AndroidTangoUsesCamera, EditorGUIUtility.TextContent("Tango Uses Camera"));

                            if ((int)PlayerSettings.Android.minSdkVersion < 23)
                            {
                                GUIContent tangoAndroidWarning = EditorGUIUtility.TextContent("Tango requires 'Minimum API Level' to be at least Android 6.0");
                                EditorGUILayout.HelpBox(tangoAndroidWarning.text, MessageType.Warning);
                            }
                            EditorGUI.indentLevel--;
                        }
                    }

                    if (TargetGroupSupportsVuforia(targetGroup))
                        VuforiaGUI(targetGroup);
                }
            }
            m_Settings.EndSettingsBox();
        }

        private void DevicesGUI(BuildTargetGroup targetGroup)
        {
            if (TargetGroupSupportsVirtualReality(targetGroup))
            {
                bool vrSupported = VREditor.GetVREnabledOnTargetGroup(targetGroup);

                EditorGUI.BeginChangeCheck();
                vrSupported = EditorGUILayout.Toggle(Styles.supportedCheckbox, vrSupported);
                if (EditorGUI.EndChangeCheck())
                {
                    VREditor.SetVREnabledOnTargetGroup(targetGroup, vrSupported);
                }

                if (vrSupported)
                {
                    VRDevicesGUIOneBuildTarget(targetGroup);
                }
            }
        }

        private static bool TargetSupportsSinglePassStereoRendering(BuildTargetGroup targetGroup)
        {
            switch (targetGroup)
            {
                case BuildTargetGroup.Standalone:
                case BuildTargetGroup.PS4:
                case BuildTargetGroup.Android:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TargetSupportsStereoInstancingRendering(BuildTargetGroup targetGroup)
        {
            switch (targetGroup)
            {
                case BuildTargetGroup.WSA:
                case BuildTargetGroup.Standalone:
                    return true;
                default:
                    return false;
            }
        }

        private static GUIContent[] GetStereoRenderingPaths(BuildTargetGroup targetGroup)
        {
            return (targetGroup == BuildTargetGroup.Android) ? Styles.kAndroidStereoRenderingPaths : Styles.kDefaultStereoRenderingPaths;
        }

        private void SinglePassStereoGUI(BuildTargetGroup targetGroup, SerializedProperty stereoRenderingPath)
        {
            if (!PlayerSettings.virtualRealitySupported)
                return;

            bool supportsSinglePass = TargetSupportsSinglePassStereoRendering(targetGroup);
            bool supportsSinglePassInstanced = TargetSupportsStereoInstancingRendering(targetGroup);

            // populate the dropdown with the valid options based on target platform.
            int validStereoRenderingOptionsCount = 1 + (supportsSinglePass ? 1 : 0) + (supportsSinglePassInstanced ? 1 : 0);
            var validStereoRenderingPaths = new GUIContent[validStereoRenderingOptionsCount];
            var validStereoRenderingValues = new int[validStereoRenderingOptionsCount];

            GUIContent[] stereoRenderingPaths = GetStereoRenderingPaths(targetGroup);

            int addedStereoRenderingOptionsCount = 0;
            validStereoRenderingPaths[addedStereoRenderingOptionsCount] = stereoRenderingPaths[(int)StereoRenderingPath.MultiPass];
            validStereoRenderingValues[addedStereoRenderingOptionsCount++] = (int)StereoRenderingPath.MultiPass;

            if (supportsSinglePass)
            {
                validStereoRenderingPaths[addedStereoRenderingOptionsCount] = stereoRenderingPaths[(int)StereoRenderingPath.SinglePass];
                validStereoRenderingValues[addedStereoRenderingOptionsCount++] = (int)StereoRenderingPath.SinglePass;
            }

            if (supportsSinglePassInstanced)
            {
                validStereoRenderingPaths[addedStereoRenderingOptionsCount] = stereoRenderingPaths[(int)StereoRenderingPath.Instancing];
                validStereoRenderingValues[addedStereoRenderingOptionsCount++] = (int)StereoRenderingPath.Instancing;
            }

            // setup fallbacks
            if (!supportsSinglePassInstanced && (stereoRenderingPath.intValue == (int)StereoRenderingPath.Instancing))
                stereoRenderingPath.intValue = (int)StereoRenderingPath.SinglePass;

            if (!supportsSinglePass && (stereoRenderingPath.intValue == (int)StereoRenderingPath.SinglePass))
                stereoRenderingPath.intValue = (int)StereoRenderingPath.MultiPass;

            EditorGUILayout.IntPopup(stereoRenderingPath, validStereoRenderingPaths, validStereoRenderingValues, EditorGUIUtility.TextContent("Stereo Rendering Method*"));

            if ((stereoRenderingPath.intValue == (int)StereoRenderingPath.SinglePass) && (targetGroup == BuildTargetGroup.Android))
            {
                var apisAndroid = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                if ((apisAndroid.Length > 0) && (apisAndroid[0] == GraphicsDeviceType.OpenGLES3))
                {
                    EditorGUILayout.HelpBox(Styles.singlepassAndroidWarning2.text, MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(Styles.singlepassAndroidWarning.text, MessageType.Warning);
                }
            }
        }

        private void AddVRDeviceMenuSelected(object userData, string[] options, int selected)
        {
            BuildTargetGroup target = (BuildTargetGroup)userData;
            var enabledDevices = VREditor.GetVREnabledDevicesOnTargetGroup(target).ToList();

            string deviceKey;
            if (!m_MapVRUIStringToDeviceKey.TryGetValue(options[selected], out deviceKey))
                deviceKey = options[selected];

            enabledDevices.Add(deviceKey);

            ApplyChangedVRDeviceList(target, enabledDevices.ToArray());
        }

        private void AddVRDeviceElement(BuildTargetGroup target, Rect rect, ReorderableList list)
        {
            VRDeviceInfoEditor[] allDevices = m_AllVRDevicesForBuildTarget[target];

            var enabledDevices = VREditor.GetVREnabledDevicesOnTargetGroup(target).ToList();

            var names = allDevices.Select(d => d.deviceNameUI).ToArray();
            var enabled = allDevices.Select(d => !enabledDevices.Any(enabledDeviceName => d.deviceNameKey == enabledDeviceName)).ToArray();

            EditorUtility.DisplayCustomMenu(rect, names, enabled, null, AddVRDeviceMenuSelected, target);
        }

        private void RemoveVRDeviceElement(BuildTargetGroup target, ReorderableList list)
        {
            var devices = VREditor.GetVREnabledDevicesOnTargetGroup(target).ToList();
            devices.RemoveAt(list.index);
            ApplyChangedVRDeviceList(target, devices.ToArray());
        }

        private void ReorderVRDeviceElement(BuildTargetGroup target, ReorderableList list)
        {
            var devices = list.list.Cast<string>().ToArray();

            ApplyChangedVRDeviceList(target, devices);
        }

        private void ApplyChangedVRDeviceList(BuildTargetGroup target, string[] devices)
        {
            if (!m_VRDeviceActiveUI.ContainsKey(target))
                return;

            if (target == BuildTargetGroup.iOS)
            {
                // Set a sensible default if cardboard is enabled, as it uses that feature and
                // the setting is mandatory on iOS
                if (devices.Contains("cardboard") && PlayerSettings.iOS.cameraUsageDescription == "")
                {
                    PlayerSettings.iOS.cameraUsageDescription = "Used to scan QR codes";
                }
            }

            VREditor.SetVREnabledDevicesOnTargetGroup(target, devices);
            m_VRDeviceActiveUI[target].list = devices;
        }

        private void DrawVRDeviceElement(BuildTargetGroup target, Rect rect, int index, bool selected, bool focused)
        {
            string name = (string)m_VRDeviceActiveUI[target].list[index];

            string uiName;
            if (!m_MapVRDeviceKeyToUIString.TryGetValue(name, out uiName))
                uiName = name + " (missing from build)";

            VRCustomOptions customOptions;
            if (m_CustomOptions.TryGetValue(name, out customOptions))
            {
                // Draw the foldout if we have extra options
                if (!(customOptions is VRCustomOptionsNone))
                {
                    Rect foldoutRect = new Rect(rect);
                    foldoutRect.width = EditorStyles.foldout.border.left;
                    foldoutRect.height = EditorStyles.foldout.border.top;
                    bool oldHierarchyMode = EditorGUIUtility.hierarchyMode;
                    EditorGUIUtility.hierarchyMode = false;
                    customOptions.IsExpanded = EditorGUI.Foldout(foldoutRect, customOptions.IsExpanded, "", false, EditorStyles.foldout);
                    EditorGUIUtility.hierarchyMode = oldHierarchyMode;
                }
            }

            // Draw
            rect.xMin += EditorStyles.foldout.border.left;
            GUI.Label(rect, uiName, EditorStyles.label);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUI.kControlVerticalSpacing;

            if (customOptions != null && customOptions.IsExpanded)
            {
                customOptions.Draw(rect);
            }
        }

        private float GetVRDeviceElementHeight(BuildTargetGroup target, int index)
        {
            ReorderableList list = (ReorderableList)m_VRDeviceActiveUI[target];
            string name = (string)list.list[index];

            float customOptionsHeight = 0.0f;
            VRCustomOptions customOptions;
            if (m_CustomOptions.TryGetValue(name, out customOptions))
            {
                customOptionsHeight = customOptions.IsExpanded ? customOptions.GetHeight() + EditorGUI.kControlVerticalSpacing : 0.0f;
            }

            return list.elementHeight + customOptionsHeight;
        }

        private void SelectVRDeviceElement(BuildTargetGroup target, ReorderableList list)
        {
            string name = (string)m_VRDeviceActiveUI[target].list[list.index];
            VRCustomOptions customOptions;
            if (m_CustomOptions.TryGetValue(name, out customOptions))
            {
                customOptions.IsExpanded = false;
            }
        }

        private bool GetVRDeviceElementIsInList(BuildTargetGroup target, string deviceName)
        {
            var enabledDevices = VREditor.GetVREnabledDevicesOnTargetGroup(target);

            if (enabledDevices.Contains(deviceName))
                return true;

            return false;
        }

        private void VRDevicesGUIOneBuildTarget(BuildTargetGroup targetGroup)
        {
            // create reorderable list for this target if needed
            if (!m_VRDeviceActiveUI.ContainsKey(targetGroup))
            {
                var rlist = new ReorderableList(VREditor.GetVREnabledDevicesOnTargetGroup(targetGroup), typeof(VRDeviceInfoEditor), true, true, true, true);
                rlist.onAddDropdownCallback = (rect, list) => AddVRDeviceElement(targetGroup, rect, list);
                rlist.onRemoveCallback = (list) => RemoveVRDeviceElement(targetGroup, list);
                rlist.onReorderCallback = (list) => ReorderVRDeviceElement(targetGroup, list);
                rlist.drawElementCallback = (rect, index, isActive, isFocused) => DrawVRDeviceElement(targetGroup, rect, index, isActive, isFocused);
                rlist.drawHeaderCallback = (rect) => GUI.Label(rect, Styles.listHeader, EditorStyles.label);
                rlist.elementHeightCallback = (index) => GetVRDeviceElementHeight(targetGroup, index);
                rlist.onSelectCallback = (list) => SelectVRDeviceElement(targetGroup, list);
                m_VRDeviceActiveUI.Add(targetGroup, rlist);
            }

            m_VRDeviceActiveUI[targetGroup].DoLayoutList();

            if (m_VRDeviceActiveUI[targetGroup].list.Count == 0)
            {
                EditorGUILayout.HelpBox("Must add at least one Virtual Reality SDK.", MessageType.Warning);
            }
        }

        internal bool TargetGroupSupportsVuforia(BuildTargetGroup targetGroup)
        {
            return targetGroup == BuildTargetGroup.Standalone ||
                targetGroup == BuildTargetGroup.Android ||
                targetGroup == BuildTargetGroup.iOS ||
                targetGroup == BuildTargetGroup.WSA;
        }

        internal void VuforiaGUI(BuildTargetGroup targetGroup)
        {
            // Disable toggle when Vuforia is in the VRDevice list and VR Supported == true
            var shouldDisableScope = VREditor.GetVREnabledOnTargetGroup(targetGroup) && GetVRDeviceElementIsInList(targetGroup, "Vuforia");
            using (new EditorGUI.DisabledScope(shouldDisableScope))
            {
                if (shouldDisableScope && !PlayerSettings.GetPlatformVuforiaEnabled(targetGroup)) // Force Vuforia AR on if Vuforia is in the VRDevice List
                    PlayerSettings.SetPlatformVuforiaEnabled(targetGroup, true);

                var vuforiaEnabled = PlayerSettings.GetPlatformVuforiaEnabled(targetGroup);

                EditorGUI.BeginChangeCheck();
                vuforiaEnabled = EditorGUILayout.Toggle(EditorGUIUtility.TextContent("Vuforia AR"), vuforiaEnabled);
                if (EditorGUI.EndChangeCheck())
                {
                    PlayerSettings.SetPlatformVuforiaEnabled(targetGroup, vuforiaEnabled);
                }
            }

            if (shouldDisableScope)
            {
                EditorGUILayout.HelpBox("Vuforia AR is required when using the Vuforia Virtual Reality SDK.", MessageType.Info);
            }
        }
    }
}
