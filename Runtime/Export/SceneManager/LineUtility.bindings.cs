// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Graphics/LineUtility.bindings.h")]
    public sealed partial class LineUtility
    {
        [FreeFunction("LineUtility_Bindings::GeneratePointsToKeep3D", IsThreadSafe = true)]
        extern internal static void GeneratePointsToKeep3D(ReadOnlySpan<Vector3> points, float tolerance, List<int> pointsToKeepList);

        [FreeFunction("LineUtility_Bindings::GeneratePointsToKeep2D", IsThreadSafe = true)]
        extern internal static void GeneratePointsToKeep2D(ReadOnlySpan<Vector2> points, float tolerance, List<int> pointsToKeepList);

        [FreeFunction("LineUtility_Bindings::GenerateSimplifiedPoints3D", IsThreadSafe = true)]
        extern internal static void GenerateSimplifiedPoints3D(ReadOnlySpan<Vector3> points, float tolerance, List<Vector3> simplifiedPoints);

        [FreeFunction("LineUtility_Bindings::GenerateSimplifiedPoints2D", IsThreadSafe = true)]
        extern internal static void GenerateSimplifiedPoints2D(ReadOnlySpan<Vector2> points, float tolerance, List<Vector2> simplifiedPoints);
    }
}
