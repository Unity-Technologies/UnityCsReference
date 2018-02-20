// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // Unwrapping settings.
    [NativeHeader("Editor/Mono/Unwrapping.bindings.h")]
    public struct UnwrapParam
    {
        // maximum allowed angle distortion (0..1)
        public float angleError;
        // maximum allowed area distortion (0..1)
        public float areaError;
        // this angle (or greater) between triangles will cause seam to be created
        public float hardAngle;
        // how much uv-islands will be padded
        public float packMargin;

        internal int recollectVertices;

        // Will set default values for params
        [FreeFunction("ResetUnwrapParam")]
        public static extern void SetDefaults(out UnwrapParam param);
    }

    // This class holds everything you may need in regard to uv-unwrapping.
    [NativeHeader("Editor/Mono/Unwrapping.bindings.h")]
    public static class Unwrapping
    {
        // Will generate per-triangle uv (3 uv pairs for each triangle) with default settings
        public static Vector2[] GeneratePerTriangleUV(Mesh src)
        {
            UnwrapParam settings = new UnwrapParam();
            UnwrapParam.SetDefaults(out settings);

            return GeneratePerTriangleUV(src, settings);
        }

        // Will generate per-triangle uv (3 uv pairs for each triangle) with provided settings
        public static Vector2[] GeneratePerTriangleUV(Mesh src, UnwrapParam settings)
        {
            return GeneratePerTriangleUVImpl(src, settings);
        }

        [NativeThrows]
        internal static extern Vector2[] GeneratePerTriangleUVImpl(Mesh src, UnwrapParam settings);

        // Will auto generate uv2 with default settings for provided mesh, and fill them in
        public static void GenerateSecondaryUVSet(Mesh src)
        {
            MeshUtility.SetPerTriangleUV2(src, GeneratePerTriangleUV(src));
        }

        // Will auto generate uv2 with provided settings for provided mesh, and fill them in
        public static void GenerateSecondaryUVSet(Mesh src, UnwrapParam settings)
        {
            MeshUtility.SetPerTriangleUV2(src, GeneratePerTriangleUV(src, settings));
        }
    }
}
