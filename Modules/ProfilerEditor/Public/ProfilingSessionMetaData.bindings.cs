// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Profiling.Editor
{
    // Must be in sync with UnityProfilingSessionMetaDataEntry in!
    internal enum ProfilingSessionMetaDataEntry
    {
        Version,
        RuntimeSessionId,
        RuntimePlatform,
        GraphicsDeviceType,
        TotalPhysicalMemory,
        TotalGraphicsMemory,
        ScriptingBackend,
        TimeSinceStartup,
        FrameCountSinceStartup,
        UnityVersion,
        ProductName,
    }
}
