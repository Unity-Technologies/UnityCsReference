// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEditorInternal.VR;
using UnityEngine;
using UnityEngine.Experimental;

namespace UnityEditor.Experimental.XR
{
    internal static class VRDeviceTransition
    {
        class AssetCallbacks : AssetPostprocessor
        {
            static bool s_EditorUpdateCalled = false;

            static AssetCallbacks()
            {
                if (!s_EditorUpdateCalled)
                {
                    EditorApplication.update += OnEditorUpdate;
                }
                EditorApplication.projectChanged += OnProjectChanged;
            }

            static void OnEditorUpdate()
            {
                s_EditorUpdateCalled = true;
                EditorApplication.update -= OnEditorUpdate;
                HandleVRDeviceTransition();
            }

            static void OnProjectChanged()
            {
                HandleVRDeviceTransition();
            }
        }


        static VRDeviceTransition()
        {
            SubsystemManager.reloadSubsytemsStarted += SubsystemReloadStarted;
            SubsystemManager.reloadSubsytemsCompleted += SubsystemReloadCompleted;
        }

        static void SubsystemReloadStarted()
        {
        }

        static void SubsystemReloadCompleted()
        {
            HandleVRDeviceTransition();
        }

        static void HandleVRDeviceTransition()
        {
            if (BuildUtilities.ShouldDisableLegacyVR())
            {
                DisableVRSettings();
            }
            else
            {
                EnableVRSettings();
            }
        }

        static void EnableVRSettings()
        {
            string storedGroupsTransitioned = "";
            List<string> groupsTransitioned = new List<string>();

            XRProjectSettings.SetBool(XRProjectSettings.KnownSettings.k_VRDeviceDisabled, false);
            XRProjectSettings.SetBool(XRProjectSettings.KnownSettings.k_VRDeviceDidAlertUser, false);

            if (XRProjectSettings.HasSetting(XRProjectSettings.KnownSettings.k_VRDeviceTransitionGroups))
            {
                storedGroupsTransitioned = XRProjectSettings.GetString(XRProjectSettings.KnownSettings.k_VRDeviceTransitionGroups);
                groupsTransitioned.AddRange(storedGroupsTransitioned.Split(new char[] { ',' }));

                foreach (var tg in groupsTransitioned)
                {
                    BuildTargetGroup targetGroup;

                    try
                    {
                        targetGroup = (BuildTargetGroup)Enum.Parse(typeof(BuildTargetGroup), tg);
                    }
                    catch (Exception ex)
                    {
                        String logMsg = String.Format("Error converting build target group names {0}.\n", tg);
                        logMsg += ex.Message;

                        Debug.LogError(logMsg);
                        continue;
                    }

                    Debug.LogFormat("No XR SDK Provider detected in project. Re-enabling VR Device settings for {0}", targetGroup);
                    VREditor.SetVREnabledOnTargetGroup(targetGroup, true);
                }

                XRProjectSettings.RemoveSetting(XRProjectSettings.KnownSettings.k_VRDeviceTransitionGroups);
            }
        }

        static void DisableVRSettings()
        {
            bool didTransitionVRDevice = false;
            string storedGroupsTransitioned = "";
            List<string> groupsTransitioned = new List<string>();

            if (XRProjectSettings.HasSetting(XRProjectSettings.KnownSettings.k_VRDeviceTransitionGroups))
            {
                XRProjectSettings.GetString(XRProjectSettings.KnownSettings.k_VRDeviceTransitionGroups, storedGroupsTransitioned);
                groupsTransitioned.AddRange(storedGroupsTransitioned.Split(new char[] { ',' }));
            }

            foreach (BuildTargetGroup targetGroup in (BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup)))
            {
                string targetGroupString = targetGroup.ToString();
                if (VREditor.GetVREnabledOnTargetGroup(targetGroup))
                {
                    Debug.LogFormat("XR SDK Provider detected in project. Disabling VR Device settings for {0}", targetGroup);
                    VREditor.SetVREnabledOnTargetGroup(targetGroup, false);
                    didTransitionVRDevice = true;
                    if (!groupsTransitioned.Contains(targetGroupString))
                    {
                        groupsTransitioned.Add(targetGroupString);
                    }
                }
            }

            if (didTransitionVRDevice)
            {
                storedGroupsTransitioned = String.Join(",", groupsTransitioned.ToArray());
                XRProjectSettings.SetString(XRProjectSettings.KnownSettings.k_VRDeviceTransitionGroups, storedGroupsTransitioned);
            }

            XRProjectSettings.SetBool(XRProjectSettings.KnownSettings.k_VRDeviceDisabled, true);
        }
    }
}
