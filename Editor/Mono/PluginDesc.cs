// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    internal struct PluginDesc
    {
        public string pluginPath;
        public CPUArch architecture;
    }

    internal enum CPUArch
    {
        Any,
        x86,
        ARMv7
    }
}
