// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine;

using UnityEditor;
using UnityEditorInternal.VR;

namespace UnityEditor.XR
{
    [VRDeviceSettingsTransitionTargetGroup(BuildTargetGroup.WSA)]
    internal class WindowsMRDeviceSettingsTransition : IVRDeviceSettingsTransition
    {
        internal static readonly string k_WsaRemoting = "WSARemoting";

        public void DisableSettings()
        {
            var remotingEnabled = PlayerSettings.GetWsaHolographicRemotingEnabled();
            if (remotingEnabled)
            {
                PlayerSettings.SetWsaHolographicRemotingEnabled(false);
                XRProjectSettings.SetBool(k_WsaRemoting, true);
            }
        }

        public void EnableSettings()
        {
            var remotingEnabled = XRProjectSettings.GetBool(k_WsaRemoting);

            if (remotingEnabled)
            {
                PlayerSettings.SetWsaHolographicRemotingEnabled(remotingEnabled);
                XRProjectSettings.RemoveSetting(k_WsaRemoting);
            }
        }
    }
}
