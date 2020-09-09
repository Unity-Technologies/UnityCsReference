// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    public enum ProfileEventType
    {
        Begin = 0,
        End = 1,
        Info = 2
    }

    public enum ProfileCaptureOptions
    {
        None = 0,
        IgnoreShortEvents = 1
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ContentBuildProfileEvent
    {
        public UInt64 TimeMicroseconds;
        public string Name;
        public string Metadata;
        public ProfileEventType Type;
    };
}
