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
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;

namespace UnityEditorInternal.VR
{
    internal class PlayerSettingsEditorVR
    {
        static class Styles
        {
            public static readonly GUIContent singlepassAndroidWarning = EditorGUIUtility.TrTextContent("Single Pass stereo rendering requires OpenGL ES 3. Please make sure that it's the first one listed under Graphics APIs.");
            public static readonly GUIContent singlepassAndroidWarning2 = EditorGUIUtility.TrTextContent("Multi Pass will be used on Android devices that don't support Single Pass.");
            public static readonly GUIContent singlePassInstancedWarning = EditorGUIUtility.TrTextContent("Single Pass Instanced is only supported on Windows. Multi Pass will be used on other platforms.");

            public static readonly GUIContent[] kDefaultStereoRenderingPaths =
            {
                EditorGUIUtility.TrTextContent("Multi Pass"),
                EditorGUIUtility.TrTextContent("Single Pass"),
                EditorGUIUtility.TrTextContent("Single Pass Instanced (Preview)")
            };

            public static readonly GUIContent[] kMultiviewStereoRenderingPaths =
            {
                EditorGUIUtility.TrTextContent("Multi Pass"),
                EditorGUIUtility.TrTextContent("Single Pass (Preview)"),
            };

            public static readonly GUIContent xrSettingsTitle = EditorGUIUtility.TrTextContent("XR Settings");

            public static readonly GUIContent supportedCheckbox = EditorGUIUtility.TrTextContent("Virtual Reality Supported");
            public static readonly GUIContent listHeader = EditorGUIUtility.TrTextContent("Virtual Reality SDKs");
            public static readonly GUIContent stereo360CaptureCheckbox = EditorGUIUtility.TrTextContent("360 Stereo Capture");
        }

        private PlayerSettingsEditor m_Settings;

        private Dictionary<BuildTargetGroup, VRDeviceInfoEditor[]> m_AllVRDevicesForBuildTarget = new Dictionary<BuildTargetGroup, VRDeviceInfoEditor[]>();
        private Dictionary<BuildTargetGroup, ReorderableList> m_VRDeviceActiveUI = new Dictionary<BuildTargetGroup, ReorderableList>();

        private Dictionary<string, string> m_MapVRDeviceKeyToUIString = new Dictionary<string, string>();
        private Dictionary<string, string> m_MapVRUIStringToDeviceKey = new Dictionary<string, string>();

        private Dictionary<string, VRCustomOptions> m_CustomOptions = new Dictionary<string, VRCustomOptions>();
        private SerializedProperty m_StereoRenderingPath;

        private SerializedProperty m_AndroidEnableTango;
        private SerializedProperty m_Enable360StereoCapture;

        private bool m_InstallsRequired = false;
        private bool m_VuforiaInstalled = false;

        internal int GUISectionIndex { get; set; }

        public PlayerSettingsEditorVR(PlayerSettingsEditor settingsEditor)
        {
            m_Settings = settingsEditor;
            m_StereoRenderingPath = m_Settings.serializedObject.FindProperty("m_StereoRenderingPath");

            m_AndroidEnableTango = m_Settings.FindPropertyAssert("AndroidEnableTango");

            SerializedProperty property = m_Settings.serializedObject.FindProperty("vrSettings");
            if (property != null)
                m_Enable360StereoCapture = property.FindPropertyRelative("enable360StereoCapture");
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

        internal bool TargetGroupSupportsAugmentedReality(BuildTargetGroup targetGroup)
        {
            return TargetGroupSupportsTango(targetGroup) ||
                TargetGroupSupportsVuforia(targetGroup);
        }

        internal void XRSectionGUI(BuildTargetGroup targetGroup, int sectionIndex)
        {
            GUISectionIndex = sectionIndex;

            if (!TargetGroupSupportsVirtualReality(targetGroup) && !TargetGroupSupportsAugmentedReality(targetGroup))
                return;

            if (VREditor.IsDeviceListDirty(targetGroup))
            {
                VREditor.ClearDeviceListDirty(targetGroup);
                m_VRDeviceActiveUI[targetGroup].list = VREditor.GetVREnabledDevicesOnTargetGroup(targetGroup);
            }

            // Check to see if any devices require an install and need their GUI hidden
            CheckDevicesRequireInstall(targetGroup);

            if (m_Settings.BeginSettingsBox(sectionIndex, Styles.xrSettingsTitle))
            {
                if (EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox("Changing XRSettings in not allowed in play mode.", MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying)) // switching VR flags in play mode is not supported
                {
                    DevicesGUI(targetGroup);

                    ErrorOnVRDeviceIncompatibility(targetGroup);

                    SinglePassStereoGUI(targetGroup, m_StereoRenderingPath);

                    TangoGUI(targetGroup);

                    VuforiaGUI(targetGroup);

                    Stereo360CaptureGUI(targetGroup);

                    ErrorOnARDeviceIncompatibility(targetGroup);
                }

                InstallGUI(targetGroup);
            }
            m_Settings.EndSettingsBox();
        }

        private void DevicesGUI(BuildTargetGroup targetGroup)
        {
            if (!TargetGroupSupportsVirtualReality(targetGroup))
                return;

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

        private void InstallGUI(BuildTargetGroup targetGroup)
        {
            if (!m_InstallsRequired)
                return;

            EditorGUILayout.Space();
            GUILayout.Label("XR Support Installers", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (!m_VuforiaInstalled)
            {
                if (EditorGUILayout.LinkLabel("Vuforia Augmented Reality"))
                {
                    string downloadUrl = BuildPlayerWindow.GetPlaybackEngineDownloadURL("Vuforia-AR");
                    Application.OpenURL(downloadUrl);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void CheckDevicesRequireInstall(BuildTargetGroup targetGroup)
        {
            // Reset
            m_InstallsRequired = false;

            if (!m_VuforiaInstalled)
            {
                VRDeviceInfoEditor[] deviceInfos = VREditor.GetAllVRDeviceInfo(targetGroup);

                for (int i = 0; i < deviceInfos.Length; ++i)
                {
                    if (deviceInfos[i].deviceNameKey.ToLower() == "vuforia")
                    {
                        m_VuforiaInstalled = true;
                        break;
                    }
                }

                if (!m_VuforiaInstalled)
                    m_InstallsRequired = true;
            }
        }

        // Keep these in sync with BuildTargetPlatformSpecific.cpp
        private static bool DoesBuildTargetSupportSinglePassStereoRendering(BuildTargetGroup targetGroup)
        {
            switch (targetGroup)
            {
                case BuildTargetGroup.Standalone:
                case BuildTargetGroup.PS4:
                case BuildTargetGroup.WSA:
                case BuildTargetGroup.Android:
                    return true;
                default:
                    return false;
            }
        }

        // Keep these in sync with BuildTargetPlatformSpecific.cpp
        private static bool DoesBuildTargetSupportStereoInstancingRendering(BuildTargetGroup targetGroup)
        {
            switch (targetGroup)
            {
                case BuildTargetGroup.Standalone:
                case BuildTargetGroup.PS4:
                case BuildTargetGroup.WSA:
                    return true;
                default:
                    return false;
            }
        }

        // Keep these in sync with BuildTargetPlatformSpecific.cpp
        private static bool DoesBuildTargetSupportStereoMultiviewRendering(BuildTargetGroup targetGroup)
        {
            switch (targetGroup)
            {
                case BuildTargetGroup.Android:
                    return true;
                default:
                    return false;
            }
        }

        // Keep these in sync with BuildTargetPlatformSpecific.cpp
        private static bool DoesBuildTargetSupportStereo360Capture(BuildTargetGroup targetGroup)
        {
            switch (targetGroup)
            {
                case BuildTargetGroup.Standalone:
                    return true;
                default:
                    return false;
            }
        }

        private static GUIContent[] GetStereoRenderingPaths(BuildTargetGroup targetGroup)
        {
            return DoesBuildTargetSupportStereoMultiviewRendering(targetGroup) ? Styles.kMultiviewStereoRenderingPaths : Styles.kDefaultStereoRenderingPaths;
        }

        private void SinglePassStereoGUI(BuildTargetGroup targetGroup, SerializedProperty stereoRenderingPath)
        {
            if (!PlayerSettings.virtualRealitySupported)
                return;

            bool supportsSinglePass = DoesBuildTargetSupportSinglePassStereoRendering(targetGroup);
            bool supportsSinglePassInstanced = DoesBuildTargetSupportStereoInstancingRendering(targetGroup);

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

            EditorGUILayout.IntPopup(stereoRenderingPath, validStereoRenderingPaths, validStereoRenderingValues, EditorGUIUtility.TrTextContent("Stereo Rendering Method*"));

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
            else if ((stereoRenderingPath.intValue == (int)StereoRenderingPath.Instancing) && (targetGroup == BuildTargetGroup.Standalone))
            {
                EditorGUILayout.HelpBox(Styles.singlePassInstancedWarning.text, MessageType.Warning);
            }
        }

        private void Stereo360CaptureGUI(BuildTargetGroup targetGroup)
        {
            if (DoesBuildTargetSupportStereo360Capture(targetGroup))
                EditorGUILayout.PropertyField(m_Enable360StereoCapture, Styles.stereo360CaptureCheckbox);
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

        private void ErrorOnVRDeviceIncompatibility(BuildTargetGroup targetGroup)
        {
            if (!PlayerSettings.GetVirtualRealitySupported(targetGroup))
                return;

            if (targetGroup == BuildTargetGroup.Android)
            {
                List<string> enabledDevices = VREditor.GetVREnabledDevicesOnTargetGroup(targetGroup).ToList();
                if (enabledDevices.Contains("Oculus") && enabledDevices.Contains("daydream"))
                {
                    EditorGUILayout.HelpBox("To avoid initialization conflicts on devices which support both Daydream and Oculus based VR, build separate APKs with different package names, targeting only the Daydream or Oculus VR SDK in the respective APK.", MessageType.Warning);
                }
            }
        }

        private void ErrorOnARDeviceIncompatibility(BuildTargetGroup targetGroup)
        {
            if (targetGroup == BuildTargetGroup.Android)
            {
                if (PlayerSettings.Android.androidTangoEnabled && PlayerSettings.GetPlatformVuforiaEnabled(targetGroup))
                {
                    EditorGUILayout.HelpBox("Both ARCore and Vuforia XR Device support cannot be selected at the same time. Please select only one XR Device that will manage the Android device.", MessageType.Error);
                }
            }
        }

        internal bool TargetGroupSupportsTango(BuildTargetGroup targetGroup)
        {
            return targetGroup == BuildTargetGroup.Android;
        }

        internal bool TargetGroupSupportsVuforia(BuildTargetGroup targetGroup)
        {
            return targetGroup == BuildTargetGroup.Standalone ||
                targetGroup == BuildTargetGroup.Android ||
                targetGroup == BuildTargetGroup.iOS ||
                targetGroup == BuildTargetGroup.WSA;
        }

        internal void TangoGUI(BuildTargetGroup targetGroup)
        {
            if (!TargetGroupSupportsTango(targetGroup))
                return;

            // Google Tango settings
            EditorGUILayout.PropertyField(m_AndroidEnableTango, EditorGUIUtility.TrTextContent("ARCore Supported"));

            if (PlayerSettings.Android.androidTangoEnabled)
            {
                EditorGUI.indentLevel++;

                if ((int)PlayerSettings.Android.minSdkVersion < 24)
                {
                    GUIContent tangoAndroidWarning = EditorGUIUtility.TrTextContent("ARCore requires 'Minimum API Level' to be at least Android 7.0");
                    EditorGUILayout.HelpBox(tangoAndroidWarning.text, MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
        }

        internal void VuforiaGUI(BuildTargetGroup targetGroup)
        {
            if (!TargetGroupSupportsVuforia(targetGroup) || !m_VuforiaInstalled)
                return;

            // Disable toggle when Vuforia is in the VRDevice list and VR Supported == true
            bool vuforiaEnabled;
            var shouldDisableScope = VREditor.GetVREnabledOnTargetGroup(targetGroup) && GetVRDeviceElementIsInList(targetGroup, "Vuforia");
            using (new EditorGUI.DisabledScope(shouldDisableScope))
            {
                if (shouldDisableScope && !PlayerSettings.GetPlatformVuforiaEnabled(targetGroup)) // Force Vuforia AR on if Vuforia is in the VRDevice List
                    PlayerSettings.SetPlatformVuforiaEnabled(targetGroup, true);

                vuforiaEnabled = PlayerSettings.GetPlatformVuforiaEnabled(targetGroup);

                EditorGUI.BeginChangeCheck();
                vuforiaEnabled = EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Vuforia Augmented Reality Supported"), vuforiaEnabled);
                if (EditorGUI.EndChangeCheck())
                {
                    PlayerSettings.SetPlatformVuforiaEnabled(targetGroup, vuforiaEnabled);
                }
            }

            if (shouldDisableScope)
            {
                EditorGUILayout.HelpBox("Vuforia Augmented Reality is required when using the Vuforia Virtual Reality SDK.", MessageType.Info);
            }
        }
    }
}
