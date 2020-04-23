// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Rendering;
using UnityEditor;

namespace UnityEngine.Rendering
{
    namespace VirtualTexturingEditor
    {
        [NativeHeader("Modules/VirtualTexturingEditor/ScriptBindings/VirtualTexturingEditor.bindings.h")]
        [StaticAccessor("VirtualTexturingEditor::Building", StaticAccessorType.DoubleColon)]
        internal static class Building
        {
            extern internal static bool IsPlatformSupportedForPlayer(BuildTarget platform);
            extern internal static bool IsRenderAPISupported(GraphicsDeviceType type, BuildTarget platform, bool checkEditor);
        }
    }
}
