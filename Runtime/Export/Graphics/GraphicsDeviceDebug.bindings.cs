// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Internal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngineInternal
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GraphicsDeviceDebugSettings
    {
        public float sleepAtStartOfGraphicsJobs;
        public float sleepBeforeTextureUpload;
    }

    [NativeHeader("Runtime/Export/Graphics/GraphicsDeviceDebug.bindings.h")]
    [StaticAccessor("GraphicsDeviceDebug", StaticAccessorType.DoubleColon)]
    internal static class GraphicsDeviceDebug
    {
        extern internal static GraphicsDeviceDebugSettings settings { get; set; }
    }
}
