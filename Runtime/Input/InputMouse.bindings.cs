// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    [NativeHeader("Runtime/Camera/Camera.h")]
    // This is needed to internally call Physics/Physics2D using an interface if available, so we don't have a hard dependency on Physics.
    internal class CameraRaycastHelper
    {
        [FreeFunction("CameraScripting::RaycastTry")]   extern internal static GameObject RaycastTry(Camera cam, Ray ray, float distance, int layerMask);
        [FreeFunction("CameraScripting::RaycastTry2D")] extern internal static GameObject RaycastTry2D(Camera cam, Ray ray, float distance, int layerMask);
    }
}
