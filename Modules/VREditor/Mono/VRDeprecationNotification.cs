// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal.VR;

namespace UnityEditorInternal.VR
{
    [InitializeOnLoad]
    class VRDeprecationNotification
    {
        static VRDeprecationNotification()
        {
            AssemblyReloadEvents.afterAssemblyReload += AssemblyReloadEvents_afterAssemblyReload;
        }

        private static bool IsEditorInPlayMode()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isPlaying ||
                EditorApplication.isPaused;
        }

        private static void AssemblyReloadEvents_afterAssemblyReload()
        {
            if (IsEditorInPlayMode() || Application.isBatchMode)
                return;

            bool virtualRealityEnabled = false;
            foreach (var buildTargetGroup in (BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup)))
            {
#pragma warning disable CS0618
                virtualRealityEnabled |= PlayerSettings.GetVirtualRealitySupported(buildTargetGroup);
#pragma warning restore CS0618
            }

            if (virtualRealityEnabled)
            {
                if (EditorUtility.DisplayDialog("Use XR Plug-in Management", "XR has been disabled in your project as you are using the deprecated XR integration (previously configured in Player/XR Settings). Please use \"XR Plug-in Management\" in \"Project Settings\" to enable XR.", "Ok"))
                {
                    foreach (var buildTargetGroup in (BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup)))
                    {
#pragma warning disable CS0618
                        if (PlayerSettings.GetVirtualRealitySupported(buildTargetGroup))
                        {
                            PlayerSettings.SetVirtualRealitySupported(buildTargetGroup, false);
                            UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(buildTargetGroup, new string[] {});
                        }
#pragma warning restore CS0618
                    }
                }
            }
        }
    }
}
