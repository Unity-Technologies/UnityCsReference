// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using System.Collections.Generic;

namespace UnityEngine
{
    // Keep this in sync with DiagnosticSwitch::SwitchFlags in C++
    [Flags]
    internal enum DiagnosticSwitchFlags
    {
        None                        = 0,
        CanChangeAfterEngineStart   = (1 << 0)
    }

    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    internal struct DiagnosticSwitch
    {
        public string name;
        public string description;
        public DiagnosticSwitchFlags flags;
        public object value;
        public object minValue;
        public object maxValue;
        public object persistentValue;
        public EnumInfo enumInfo;

        [UsedByNativeCode]
        private static void AppendDiagnosticSwitchToList(List<DiagnosticSwitch> list, string name, string description,
            DiagnosticSwitchFlags flags, object value, object minValue, object maxValue, object persistentValue, EnumInfo enumInfo)
        {
            list.Add(new DiagnosticSwitch
            {
                name = name,
                description = description,
                flags = flags,
                value = value,
                minValue = minValue,
                maxValue = maxValue,
                persistentValue = persistentValue,
                enumInfo = enumInfo
            });
        }
    }
}
