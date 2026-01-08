// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Mathematics;

#pragma warning disable 0660, 0661

namespace UnityEngine
{
    public partial struct Vector2
    {
        /// <summary>
        /// Converts a float2 to Vector2.
        /// </summary>
        /// <param name="v">float2 to convert.</param>
        /// <returns>The converted Vector2.</returns>
        public static implicit operator Vector2(float2 v)     { return new Vector2(v.x, v.y); }

        /// <summary>
        /// Converts a Vector2 to float2.
        /// </summary>
        /// <param name="v">Vector2 to convert.</param>
        /// <returns>The converted float2.</returns>
        public static implicit operator float2(Vector2 v)     { return new float2(v.x, v.y); }
    }

    public partial struct Vector3
    {
        /// <summary>
        /// Converts a float3 to Vector3.
        /// </summary>
        /// <param name="v">float3 to convert.</param>
        /// <returns>The converted Vector3.</returns>
        public static implicit operator Vector3(float3 v)     { return new Vector3(v.x, v.y, v.z); }

        /// <summary>
        /// Converts a Vector3 to float3.
        /// </summary>
        /// <param name="v">Vector3 to convert.</param>
        /// <returns>The converted float3.</returns>
        public static implicit operator float3(Vector3 v)     { return new float3(v.x, v.y, v.z); }
    }

    public partial struct Vector4
    {
        /// <summary>
        /// Converts a Vector4 to float4.
        /// </summary>
        /// <param name="v">Vector4 to convert.</param>
        /// <returns>The converted float4.</returns>
        public static implicit operator float4(Vector4 v)     { return new float4(v.x, v.y, v.z, v.w); }

        /// <summary>
        /// Converts a float4 to Vector4.
        /// </summary>
        /// <param name="v">float4 to convert.</param>
        /// <returns>The converted Vector4.</returns>
        public static implicit operator Vector4(float4 v)     { return new Vector4(v.x, v.y, v.z, v.w); }
    }

    public partial struct Quaternion
    {
        /// <summary>
        /// Converts a quaternion to Quaternion.
        /// </summary>
        /// <param name="q">quaternion to convert.</param>
        /// <returns>The converted Quaternion.</returns>
        public static implicit operator Quaternion(quaternion q)  { return new Quaternion(q.value.x, q.value.y, q.value.z, q.value.w); }

        /// <summary>
        /// Converts a Quaternion to quaternion.
        /// </summary>
        /// <param name="q">Quaternion to convert.</param>
        /// <returns>The converted quaternion.</returns>
        public static implicit operator quaternion(Quaternion q)  { return new quaternion(q.x, q.y, q.z, q.w); }
    }

    public partial struct Matrix4x4
    {
        /// <summary>
        /// Converts a Matrix4x4 to float4x4.
        /// </summary>
        /// <param name="m">Matrix4x4 to convert.</param>
        /// <returns>The converted float4x4.</returns>
        public static implicit operator float4x4(Matrix4x4 m) { return new float4x4(m.GetColumn(0), m.GetColumn(1), m.GetColumn(2), m.GetColumn(3)); }

        /// <summary>
        /// Converts a float4x4 to Matrix4x4.
        /// </summary>
        /// <param name="m">float4x4 to convert.</param>
        /// <returns>The converted Matrix4x4.</returns>
        public static implicit operator Matrix4x4(float4x4 m) { return new Matrix4x4(m.c0, m.c1, m.c2, m.c3); }
    }
}
