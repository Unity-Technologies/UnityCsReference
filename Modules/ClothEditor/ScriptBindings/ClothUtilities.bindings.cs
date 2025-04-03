// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using System;
using UnityEngine.Bindings;

using UnityEngine;
using UnityEngine.Scripting;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace UnityEditor
{
    [NativeHeader("Modules/ClothEditor/ClothUtilities.h")]
    internal static class ClothUtilities
    {
        [FreeFunction("ClothUtilities::Raycast")]
        internal static extern RaycastHit Raycast([NotNull] Cloth cloth, Ray ray, float maxDistance, ref bool hasHit);
    }
}
