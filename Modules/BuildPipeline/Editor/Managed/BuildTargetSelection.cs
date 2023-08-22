// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor.Build;

namespace UnityEditor.Build.Content
{
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct BuildTargetSelection
    {
        internal BuildTarget platform;
        internal int subTarget;
        internal int extendedPlatform;
        internal int isEditor;

        internal BuildTargetSelection(BuildTarget platform, int subTarget, bool extended = false, bool isEditor = false)
        {
            this.platform = platform;
            this.subTarget = subTarget;
            this.extendedPlatform = extended ? 1 : 0;
            this.isEditor = isEditor ? 1 : 0;
        }
    }
}
