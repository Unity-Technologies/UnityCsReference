// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    static class MathUtils
    {
        static class MethodImplOptionsEx
        {
            public const short AggressiveInlining = 256;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 MultiplyVector2(Matrix4x4 lhs, Vector2 vector)
        {
            Vector2 res;
            res.x = lhs.m00 * vector.x + lhs.m01 * vector.y;
            res.y = lhs.m10 * vector.x + lhs.m11 * vector.y;
            return res;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 Multiply2X3((Vector2, Vector2, Vector2) transform, Vector3 point)
        {
            return new Vector2(
                transform.Item1.x * point.x + transform.Item2.x * point.y + transform.Item3.x * point.z,
                transform.Item1.y * point.x + transform.Item2.y * point.y + transform.Item3.y * point.z
            );
        }
    }
}
