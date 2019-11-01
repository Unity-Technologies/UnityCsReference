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
using VRAttributes = UnityEditor.BuildTargetDiscovery.VRAttributes;

namespace UnityEditorInternal.VR
{
    internal class PlayerSettingsEditorVR
    {
        static class Styles
        {
            public static readonly GUIContent singlepassAndroidWarning = EditorGUIUtility.TrTextContent("Single Pass stereo rendering requires OpenGL ES 3. Please make sure that it's the first one listed under Graphics APIs.");
            public static readonly GUIContent singlepassAndroidWarning2 = EditorGUIUtility.TrTextContent("Multi Pass will be used on Android devices that don't support Single Pass.");
            public static readonly GUIContent singlepassAndroidWarning3 = EditorGUIUtility.TrTextContent("When using a Scriptable Render Pipeline, Single Pass Double Wide will be used on Android devices that don't support Single Pass Instancing or Multi-view.");
            public static readonly GUIContent singlePassInstancedWarning = EditorGUIUtility.TrTextContent("Single Pass Instanced is only supported on Windows. Multi Pass will be used on other platforms.");
            public static readonly GUIContent multiPassNotSupportedWithSRP = EditorGUIUtility.TrTextContent("Multi Pass is only supported using the legacy render pipelines. Stereo Rendering Mode is set to the fallback mode of Single Pass.");

            public static readonly GUIContent[] kDefaultStereoRenderingPaths =
            {
                EditorGUIUtility.TrTextContent("Multi Pass"),
                EditorGUIUtility.TrTextContent("Single Pass"),
                EditorGUIUtility.TrTextContent("Single Pass Instanced")
            };

            public static readonly GUIContent[] kMultiviewStereoRenderingPaths =
            {
                EditorGUIUtility.TrTextContent("Multi Pass"),
                EditorGUIUtility.TrTextContent("Single Pass")
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

        private bool m_ShowMultiPassSRPInfoBox = false;
        private bool m_SharedSettingShown = false;

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
                    customOptions.IsExpanded = true;
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
            return BuildTargetDiscovery.PlatformGroupHasVRFlag(targetGroup, VRAttributes.SupportTango);
        }

        internal void XRSectionGUI(BuildTargetGroup targetGroup, int sectionIndex)
        {
            GUISectionIndex = sectionIndex;

            if (!TargetGroupSupportsVirtualReality(targetGroup) && !TargetGroupSupportsAugmentedReality(targetGroup))
                return;

            if (m_VRDeviceActiveUI.ContainsKey(targetGroup) && VREditor.IsDeviceListDirty(targetGroup))
            {
                VREditor.ClearDeviceListDirty(targetGroup);
                if (m_VRDeviceActiveUI.ContainsKey(targetGroup))
                {
                    m_VRDeviceActiveUI[targetGroup].list = VREditor.GetVREnabledDevicesOnTargetGroup(targetGroup);
                }
            }

            if (m_Settings.BeginSettingsBox(sectionIndex, Styles.xrSettingsTitle))
            {
                m_SharedSettingShown = false;

                if (EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox("Changing XR Settings is not allowed in play mode.", MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying)) // switching VR flags in play mode is not supported
                {
                    bool shouldVRDeviceSettingsBeDisabled = XRProjectSettings.GetBool(XRProjectSettings.KnownSettings.k_VRDeviceDisabled, false);
                    using (new EditorGUI.DisabledGroupScope(shouldVRDeviceSettingsBeDisabled))
                    {
                        if (shouldVRDeviceSettingsBeDisabled)
                        {
                            EditorGUILayout.HelpBox("Legacy XR is currently disabled. Unity has detected that you have one or more XR SDK Provider packages installed. Legacy XR is incompatible with XR SDK. Remove all XR SDK Packages from your project to re-enable legacy XR", MessageType.Warning);

                            if (!XRProjectSettings.GetBool(XRProjectSettings.KnownSettings.k_VRDeviceDidAlertUser))
                            {
                                EditorUtility.DisplayDialog("Legacy XR Disabled", "Unity has detected that you have one or more XR SDK Provider packages installed. Legacy XR is incompatible with XR SDK. Remove all XR SDK Packages from your project to re-enable legacy XR", "Ok");
                                XRProjectSettings.SetBool(XRProjectSettings.KnownSettings.k_VRDeviceDidAlertUser, true);
                            }
                        }
                        DevicesGUI(targetGroup);

                        ErrorOnVRDeviceIncompatibility(targetGroup);
                    }

                    SinglePassStereoGUI(targetGroup, m_StereoRenderingPath);

                    TangoGUI(targetGroup);

                    RemotingWSAHolographicGUI(targetGroup);

                    Stereo360CaptureGUI(targetGroup);

                    WarnOnGraphicsAPIIncompatibility(targetGroup);
                }

                if (m_SharedSettingShown)
                {
                    EditorGUILayout.Space();
                    m_Settings.ShowSharedNote();
                }
            }
            m_Settings.EndSettingsBox();
        }

        internal bool TargetGroupSupportsWSAHolographicRemoting(BuildTargetGroup targetGroup)
        {
            return targetGroup == BuildTargetGroup.WSA;
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

        private static GUIContent[] GetStereoRenderingPaths(BuildTargetGroup targetGroup)
        {
            if (BuildTargetDiscovery.PlatformGroupHasVRFlag(targetGroup, VRAttributes.SupportStereoMultiviewRendering))
                return Styles.kMultiviewStereoRenderingPaths;
            return Styles.kDefaultStereoRenderingPaths;
        }

        private bool IsStereoRenderingModeSupported(BuildTargetGroup targetGroup, StereoRenderingPath stereoRenderingPath)
        {
            switch (stereoRenderingPath)
            {
                case StereoRenderingPath.MultiPass:
                    return true;

                case StereoRenderingPath.SinglePass:
                    return BuildTargetDiscovery.PlatformGroupHasVRFlag(targetGroup, VRAttributes.SupportSinglePassStereoRendering);

                case StereoRenderingPath.Instancing:
                    return BuildTargetDiscovery.PlatformGroupHasVRFlag(targetGroup, VRAttributes.SupportStereoInstancingRendering);
            }

            return false;
        }

        void OnStereoModeSelected(SerializedProperty stereoRenderingPath, object userData)
        {
            stereoRenderingPath.intValue = (int)userData;
            m_ShowMultiPassSRPInfoBox = false;

            m_Settings.serializedObject.ApplyModifiedProperties();
        }

        private void SinglePassStereoGUI(BuildTargetGroup targetGroup, SerializedProperty stereoRenderingPath)
        {
            if (!PlayerSettings.virtualRealitySupported)
                return;

            bool supportsMultiPass = IsStereoRenderingModeSupported(targetGroup, StereoRenderingPath.MultiPass);
            bool supportsSinglePass = IsStereoRenderingModeSupported(targetGroup, StereoRenderingPath.SinglePass);
            bool supportsSinglePassInstanced = IsStereoRenderingModeSupported(targetGroup, StereoRenderingPath.Instancing);

            // populate the dropdown with the valid options based on target platform.
            int multiPassAndSinglePass = 2;
            int validStereoRenderingOptionsCount = multiPassAndSinglePass + (supportsSinglePassInstanced ? 1 : 0);
            var validStereoRenderingPaths = new GUIContent[validStereoRenderingOptionsCount];
            var validStereoRenderingValues = new int[validStereoRenderingOptionsCount];

            GUIContent[] stereoRenderingPaths = GetStereoRenderingPaths(targetGroup);

            int addedStereoRenderingOptionsCount = 0;
            validStereoRenderingPaths[addedStereoRenderingOptionsCount] = stereoRenderingPaths[(int)StereoRenderingPath.MultiPass];
            validStereoRenderingValues[addedStereoRenderingOptionsCount++] = (int)StereoRenderingPath.MultiPass;

            validStereoRenderingPaths[addedStereoRenderingOptionsCount] = stereoRenderingPaths[(int)StereoRenderingPath.SinglePass];
            validStereoRenderingValues[addedStereoRenderingOptionsCount++] = (int)StereoRenderingPath.SinglePass;

            if (supportsSinglePassInstanced)
            {
                validStereoRenderingPaths[addedStereoRenderingOptionsCount] = stereoRenderingPaths[(int)StereoRenderingPath.Instancing];
                validStereoRenderingValues[addedStereoRenderingOptionsCount++] = (int)StereoRenderingPath.Instancing;
            }

            // setup fallbacks
            if (!supportsMultiPass && (stereoRenderingPath.intValue == (int)StereoRenderingPath.MultiPass))
            {
                stereoRenderingPath.intValue = (int)StereoRenderingPath.SinglePass;
                m_ShowMultiPassSRPInfoBox = true;
            }

            if (!supportsSinglePassInstanced && (stereoRenderingPath.intValue == (int)StereoRenderingPath.Instancing))
                stereoRenderingPath.intValue = (int)StereoRenderingPath.SinglePass;

            if (!supportsSinglePass && (stereoRenderingPath.intValue == (int)StereoRenderingPath.SinglePass))
                stereoRenderingPath.intValue = (int)StereoRenderingPath.MultiPass;

            if (m_ShowMultiPassSRPInfoBox)
                EditorGUILayout.HelpBox(Styles.multiPassNotSupportedWithSRP.text, MessageType.Info);

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, EditorGUIUtility.TrTextContent("Stereo Rendering Mode*"), stereoRenderingPath);
            rect = EditorGUI.PrefixLabel(rect, EditorGUIUtility.TrTextContent("Stereo Rendering Mode*"));

            int index = Math.Max(0, Array.IndexOf(validStereoRenderingValues, stereoRenderingPath.intValue));
            if (EditorGUI.DropdownButton(rect, validStereoRenderingPaths[index], FocusType.Passive))
            {
                var menu = new GenericMenu();
                for (int i = 0; i < validStereoRenderingValues.Length; i++)
                {
                    int value = validStereoRenderingValues[i];
                    bool selected = (value == stereoRenderingPath.intValue);

                    if (!IsStereoRenderingModeSupported(targetGroup, (StereoRenderingPath)value))
                        menu.AddDisabledItem(validStereoRenderingPaths[i], selected);
                    else
                        menu.AddItem(validStereoRenderingPaths[i], selected, (object userData) => { OnStereoModeSelected(stereoRenderingPath, userData); }, value);
                }
                menu.DropDown(rect);
            }
            EditorGUI.EndProperty();

            if ((stereoRenderingPath.intValue == (int)StereoRenderingPath.SinglePass) && (targetGroup == BuildTargetGroup.Android))
            {
                var apisAndroid = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                if ((apisAndroid.Length > 0) && (apisAndroid[0] == GraphicsDeviceType.OpenGLES3))
                {
                    if (supportsMultiPass)
                    {
                        EditorGUILayout.HelpBox(Styles.singlepassAndroidWarning2.text, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(Styles.singlepassAndroidWarning3.text, MessageType.Info);
                    }
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

            m_Settings.serializedObject.ApplyModifiedProperties();
        }

        private void Stereo360CaptureGUI(BuildTargetGroup targetGroup)
        {
            if (BuildTargetDiscovery.PlatformGroupHasVRFlag(targetGroup, VRAttributes.SupportStereo360Capture))
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

            var names = allDevices.Select(d => L10n.Tr(d.deviceNameUI)).ToArray();
            var enabled = allDevices.Select(d => !enabledDevices.Any(enabledDeviceName => d.deviceNameKey == enabledDeviceName)).ToArray();

            EditorUtility.DisplayCustomMenu(rect, names, enabled, null, AddVRDeviceMenuSelected, target);
        }

        private void RemoveVRDeviceElement(BuildTargetGroup target, ReorderableList list)
        {
            var devices = VREditor.GetVREnabledDevicesOnTargetGroup(target).ToList();
            var device = devices[list.index];
            devices.RemoveAt(list.index);

            VRCustomOptions customOptions;
            if (m_CustomOptions.TryGetValue(device, out customOptions))
            {
                customOptions.IsExpanded = true;
            }


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
            var VRDeviceUIList = m_VRDeviceActiveUI[target];
            string name = (string)VRDeviceUIList.list[index];
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, VRDeviceUIList.elementHeight);

            string uiName;
            if (!m_MapVRDeviceKeyToUIString.TryGetValue(name, out uiName))
                uiName = name + " (missing from build)";

            VRCustomOptions customOptions;
            if (m_CustomOptions.TryGetValue(name, out customOptions))
            {
                // Draw the foldout if we have extra options
                if (!(customOptions is VRCustomOptionsNone))
                {
                    Rect foldoutRect = new Rect(headerRect.x, headerRect.y + (headerRect.height - EditorStyles.foldout.border.top) / 2,
                        EditorStyles.foldout.border.left, EditorStyles.foldout.border.top);

                    foldoutRect = EditorStyles.foldout.margin.Add(foldoutRect);
                    bool oldHierarchyMode = EditorGUIUtility.hierarchyMode;
                    EditorGUIUtility.hierarchyMode = false;
                    customOptions.IsExpanded = EditorGUI.Foldout(foldoutRect, customOptions.IsExpanded, "", false, EditorStyles.foldout);
                    EditorGUIUtility.hierarchyMode = oldHierarchyMode;
                }
            }

            // Draw
            headerRect.xMin += EditorStyles.foldout.border.left;
            rect.xMin += EditorStyles.foldout.border.left;
            GUI.Label(headerRect, uiName, EditorStyles.label);
            rect.y += VRDeviceUIList.elementHeight + EditorGUI.kControlVerticalSpacing;

            if (customOptions != null && customOptions.IsExpanded)
            {
                customOptions.Draw(target, rect);
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
                customOptionsHeight = customOptions.IsExpanded ? customOptions.GetHeight(target) + EditorGUI.kControlVerticalSpacing : 0.0f;
            }

            return list.elementHeight + customOptionsHeight;
        }

        private bool GetVRDeviceElementIsInList(BuildTargetGroup target, string deviceName)
        {
            var enabledDevices = VREditor.GetVREnabledDevicesOnTargetGroup(target);

            if (enabledDevices.Contains(deviceName))
                return true;

            return false;
        }

        private void DragVRDeviceElement(BuildTargetGroup target, ReorderableList list)
        {
            string name = (string)m_VRDeviceActiveUI[target].list[list.index];
            VRCustomOptions customOptions;
            if (m_CustomOptions.TryGetValue(name, out customOptions))
            {
                customOptions.IsExpanded = false;
            }
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
                rlist.onMouseDragCallback = (list) => DragVRDeviceElement(targetGroup, list);
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

        private void WarnOnGraphicsAPIIncompatibility(BuildTargetGroup targetGroup)
        {
            if (targetGroup == BuildTargetGroup.Android && PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Contains(UnityEngine.Rendering.GraphicsDeviceType.Vulkan))
            {
                if (PlayerSettings.Android.ARCoreEnabled || PlayerSettings.virtualRealitySupported)
                {
                    EditorGUILayout.HelpBox("XR is currently not supported when using the Vulkan Graphics API.\nPlease go to 'Other Settings' and remove 'Vulkan' from the list of Graphics APIs.", MessageType.Warning);
                }
            }
        }

        internal void TangoGUI(BuildTargetGroup targetGroup)
        {
            if (!BuildTargetDiscovery.PlatformGroupHasVRFlag(targetGroup, VRAttributes.SupportTango))
                return;

            // Google Tango settings
            EditorGUILayout.PropertyField(m_AndroidEnableTango, EditorGUIUtility.TrTextContent("ARCore Supported"));
        }

        internal void RemotingWSAHolographicGUI(BuildTargetGroup targetGroup)
        {
            if (!TargetGroupSupportsWSAHolographicRemoting(targetGroup))
                return;

            var shouldEnableScope = VREditor.GetVREnabledOnTargetGroup(targetGroup) && GetVRDeviceElementIsInList(targetGroup, "WindowsMR");
            using (new EditorGUI.DisabledScope(!shouldEnableScope))
            {
                var remotingEnabled = PlayerSettings.GetWsaHolographicRemotingEnabled();
                EditorGUI.BeginChangeCheck();
                remotingEnabled = EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("WSA Holographic Remoting Supported"), remotingEnabled);
                if (EditorGUI.EndChangeCheck())
                {
                    PlayerSettings.SetWsaHolographicRemotingEnabled(remotingEnabled);
                }
            }
            if (shouldEnableScope)
            {
                EditorGUILayout.HelpBox("WindowsMR is required when using WSA Holographic Remoting.", MessageType.Info);
            }
        }
    }
}
