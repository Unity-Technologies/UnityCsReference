// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal.VR;

namespace UnityEditor
{
    partial class PlayerSettings
    {
        // TODO: This needs to be removed once we stop XR Plug-in Management from auto upgrading/checking.
        [Obsolete("This API is deprecated and will be removed prior to shipping 2020.2", false)]
        public static bool GetVirtualRealitySupported(BuildTargetGroup targetGroup)
        {
            return false;
        }

        // TODO: This needs to be removed once we stop XR Plug-in Management from auto upgrading/checking.
        [Obsolete("This API is deprecated and will be removed prior to shipping 2020.2", false)]
        public static void SetVirtualRealitySupported(BuildTargetGroup targetGroup, bool value)
        {
        }
    }
}
