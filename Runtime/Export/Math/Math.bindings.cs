// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct FrustumPlanes
    {
        public float left;
        public float right;
        public float bottom;
        public float top;
        public float zNear;
        public float zFar;
    }

    [NativeType(Header = "Runtime/Math/Matrix4x4.h")]
    [NativeHeader("Runtime/Math/MathScripting.h")]
    public partial struct Matrix4x4
    {
        [ThreadSafe] extern private readonly Quaternion    GetRotation();
        [ThreadSafe] extern private readonly Vector3       GetLossyScale();
        [ThreadSafe] extern private readonly bool          IsIdentity();
        [ThreadSafe] extern private readonly float         GetDeterminant();
        [ThreadSafe] extern private readonly FrustumPlanes DecomposeProjection();

        public readonly Quaternion rotation               { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => GetRotation(); }
        public readonly Vector3 lossyScale                { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => GetLossyScale(); }
        public readonly bool isIdentity                   { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => IsIdentity(); }
        public readonly float determinant                 { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => GetDeterminant(); }
        public readonly FrustumPlanes decomposeProjection { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => DecomposeProjection(); }

        [ThreadSafe] extern public readonly bool ValidTRS();

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Determinant(Matrix4x4 m) => m.determinant;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Determinant(in Matrix4x4 m) => m.determinant;

        [FreeFunction("MatrixScripting::TRS", IsThreadSafe = true)] extern private static Matrix4x4 Internal_TRS(in Vector3 pos, in Quaternion q, in Vector3 s);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s) => Matrix4x4.Internal_TRS(in pos, in q, in s);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Matrix4x4 TRS(in Vector3 pos, in Quaternion q, in Vector3 s) => Matrix4x4.Internal_TRS(in pos, in q, in s);

        [FreeFunction("MatrixScripting::SetTRS", IsThreadSafe = true)] extern private static void Internal_SetTRS(ref Matrix4x4 m, in Vector3 pos, in Quaternion q, in Vector3 s);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetTRS(Vector3 pos, Quaternion q, Vector3 s) => Matrix4x4.Internal_SetTRS(ref this, in pos, in q, in s);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetTRS(in Vector3 pos, in Quaternion q, in Vector3 s) => Matrix4x4.Internal_SetTRS(ref this, in pos, in q, in s);

        [FreeFunction("MatrixScripting::Inverse3DAffine", IsThreadSafe = true)] extern private static bool Internal_Inverse3DAffine(in Matrix4x4 input, ref Matrix4x4 result);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool Inverse3DAffine(Matrix4x4 input, ref Matrix4x4 result) => Matrix4x4.Internal_Inverse3DAffine(in input, ref result);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool Inverse3DAffine(in Matrix4x4 input, ref Matrix4x4 result) => Matrix4x4.Internal_Inverse3DAffine(in input, ref result);

        [FreeFunction("MatrixScripting::Inverse", IsThreadSafe = true)] extern private static Matrix4x4 Internal_Inverse(in Matrix4x4 m);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Matrix4x4 Inverse(Matrix4x4 m) => Matrix4x4.Internal_Inverse(in m);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Matrix4x4 Inverse(in Matrix4x4 m) => Matrix4x4.Internal_Inverse(in m);
        public readonly Matrix4x4 inverse { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => Matrix4x4.Internal_Inverse(in this); }

        [FreeFunction("MatrixScripting::Transpose", IsThreadSafe = true)] extern private static Matrix4x4 Internal_Transpose(in Matrix4x4 m);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Matrix4x4 Transpose(Matrix4x4 m) => Matrix4x4.Internal_Transpose(in m);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Matrix4x4 Transpose(in Matrix4x4 m) => Matrix4x4.Internal_Transpose(in m);
        public readonly Matrix4x4 transpose { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => Matrix4x4.Internal_Transpose(in this); }

        [FreeFunction("MatrixScripting::Ortho", IsThreadSafe = true)] extern public static Matrix4x4 Ortho(float left, float right, float bottom, float top, float zNear, float zFar);
        [FreeFunction("MatrixScripting::Perspective", IsThreadSafe = true)] extern public static Matrix4x4 Perspective(float fov, float aspect, float zNear, float zFar);

        [FreeFunction("MatrixScripting::LookAt", IsThreadSafe = true)] extern private static Matrix4x4 Internal_LookAt(in Vector3 from, in Vector3 to, in Vector3 up);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Matrix4x4 LookAt(Vector3 from, Vector3 to, Vector3 up) => Matrix4x4.Internal_LookAt(in from, in to, in up);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Matrix4x4 LookAt(in Vector3 from, in Vector3 to, in Vector3 up) => Matrix4x4.Internal_LookAt(in from, in to, in up);

        [FreeFunction("MatrixScripting::Frustum", IsThreadSafe = true)] extern public static Matrix4x4 Frustum(float left, float right, float bottom, float top, float zNear, float zFar);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Matrix4x4 Frustum(FrustumPlanes fp) => Matrix4x4.Frustum(fp.left, fp.right, fp.bottom, fp.top, fp.zNear, fp.zFar);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Matrix4x4 Frustum(in FrustumPlanes fp) => Matrix4x4.Frustum(fp.left, fp.right, fp.bottom, fp.top, fp.zNear, fp.zFar);

        [FreeFunction("MatrixScripting::Internal_CompareApproximately", IsThreadSafe = true)] extern private static bool Internal_CompareApproximately(in Matrix4x4 a, in Matrix4x4 b, float threshold);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static bool CompareApproximately(Matrix4x4 a, Matrix4x4 b, float threshold) => Matrix4x4.Internal_CompareApproximately(in a, in b, threshold);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static bool CompareApproximately(in Matrix4x4 a, in Matrix4x4 b, float threshold) => Matrix4x4.Internal_CompareApproximately(in a, in b, threshold);
    }

    [NativeType(Header = "Runtime/Math/Vector3.h")]
    [NativeHeader("Runtime/Math/MathScripting.h")]
    public partial struct Vector3
    {
        [FreeFunction("VectorScripting::Slerp", IsThreadSafe = true)] extern private static Vector3 Internal_Slerp(in Vector3 a, in Vector3 b, float t);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t) => Vector3.Internal_Slerp(in a, in b, t);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 Slerp(in Vector3 a, in Vector3 b, float t) => Vector3.Internal_Slerp(in a, in b, t);

        [FreeFunction("VectorScripting::SlerpUnclamped", IsThreadSafe = true)] extern private static Vector3 Internal_SlerpUnclamped(in Vector3 a, in Vector3 b, float t);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 SlerpUnclamped(Vector3 a, Vector3 b, float t) => Vector3.Internal_SlerpUnclamped(in a, in b, t);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 SlerpUnclamped(in Vector3 a, in Vector3 b, float t) => Vector3.Internal_SlerpUnclamped(in a, in b, t);

        [FreeFunction("VectorScripting::OrthoNormalize", IsThreadSafe = true)] extern private static void OrthoNormalize2(ref Vector3 a, ref Vector3 b);
        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent) => OrthoNormalize2(ref normal, ref tangent);
        [FreeFunction("VectorScripting::OrthoNormalize", IsThreadSafe = true)] extern private static void OrthoNormalize3(ref Vector3 a, ref Vector3 b, ref Vector3 c);
        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent, ref Vector3 binormal) => OrthoNormalize3(ref normal, ref tangent, ref binormal);

        [FreeFunction("VectorScripting::RotateTowards", IsThreadSafe = true)] extern private static Vector3 Internal_RotateTowards(in Vector3 current, in Vector3 target, float maxRadiansDelta, float maxMagnitudeDelta);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 RotateTowards(Vector3 current, Vector3 target, float maxRadiansDelta, float maxMagnitudeDelta) => Vector3.Internal_RotateTowards(in current, in target, maxRadiansDelta, maxMagnitudeDelta);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 RotateTowards(in Vector3 current, in Vector3 target, float maxRadiansDelta, float maxMagnitudeDelta) => Vector3.Internal_RotateTowards(in current, in target, maxRadiansDelta, maxMagnitudeDelta);
    }

    [NativeType(Header = "Runtime/Math/Quaternion.h")]
    [NativeHeader("Runtime/Math/MathScripting.h")]
    [UsedByNativeCode]
    public partial struct Quaternion
    {
        [FreeFunction("FromToQuaternionSafe", IsThreadSafe = true)] extern private static Quaternion Internal_FromToRotation(in Vector3 fromDirection, in Vector3 toDirection);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion FromToRotation(Vector3 fromDirection, Vector3 toDirection) => Quaternion.Internal_FromToRotation(in fromDirection, in toDirection);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion FromToRotation(in Vector3 fromDirection, in Vector3 toDirection) => Quaternion.Internal_FromToRotation(in fromDirection, in toDirection);

        [FreeFunction("QuaternionScripting::Inverse", IsThreadSafe = true)] extern private static Quaternion Internal_Inverse(in Quaternion rotation);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion Inverse(Quaternion rotation) => Quaternion.Internal_Inverse(in rotation);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion Inverse(in Quaternion rotation) => Quaternion.Internal_Inverse(in rotation);

        [FreeFunction("QuaternionScripting::Slerp", IsThreadSafe = true)]          extern private static Quaternion Internal_Slerp(in Quaternion a, in Quaternion b, float t);
        [FreeFunction("QuaternionScripting::SlerpUnclamped", IsThreadSafe = true)] extern private static Quaternion Internal_SlerpUnclamped(in Quaternion a, in Quaternion b, float t);
        [FreeFunction("QuaternionScripting::Lerp", IsThreadSafe = true)]           extern private static Quaternion Internal_Lerp(in Quaternion a, in Quaternion b, float t);
        [FreeFunction("QuaternionScripting::LerpUnclamped", IsThreadSafe = true)]  extern private static Quaternion Internal_LerpUnclamped(in Quaternion a, in Quaternion b, float t);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => Quaternion.Internal_Slerp(in a, in b, t); 
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t) => Quaternion.Internal_SlerpUnclamped(in a, in b, t); 
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) => Quaternion.Internal_Lerp(in a, in b, t); 
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion LerpUnclamped(Quaternion a, Quaternion b, float t) => Quaternion.Internal_LerpUnclamped(in a, in b, t); 

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion Slerp(in Quaternion a, in Quaternion b, float t) => Quaternion.Internal_Slerp(in a, in b, t);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion SlerpUnclamped(in Quaternion a, in Quaternion b, float t) => Quaternion.Internal_SlerpUnclamped(in a, in b, t);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion Lerp(in Quaternion a, in Quaternion b, float t) => Quaternion.Internal_Lerp(in a, in b, t);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion LerpUnclamped(in Quaternion a, in Quaternion b, float t) => Quaternion.Internal_LerpUnclamped(in a, in b, t);

        [FreeFunction("EulerToQuaternion", IsThreadSafe = true)] extern private static Quaternion Internal_FromEulerRad(in Vector3 euler);
        [FreeFunction("QuaternionScripting::ToEuler", IsThreadSafe = true)] extern private static Vector3 Internal_ToEulerRad(in Quaternion rotation);
        [FreeFunction("QuaternionScripting::ToAxisAngle", IsThreadSafe = true)] extern private static void Internal_ToAxisAngleRad(in Quaternion q, out Vector3 axis, out float angle);

        [FreeFunction("QuaternionScripting::AngleAxis", IsThreadSafe = true)] extern private static Quaternion Internal_AngleAxis(float angle, in Vector3 axis);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion AngleAxis(float angle, Vector3 axis) => Quaternion.Internal_AngleAxis(angle, in axis);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion AngleAxis(float angle, in Vector3 axis) => Quaternion.Internal_AngleAxis(angle, in axis);

        [FreeFunction("QuaternionScripting::LookRotation", IsThreadSafe = true)] extern private static Quaternion Internal_LookRotation(in Vector3 forward, [uei.DefaultValue("Vector3.up")] in Vector3 upwards);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion LookRotation(Vector3 forward, [uei.DefaultValue("Vector3.up")] Vector3 upwards) => Quaternion.Internal_LookRotation(in forward, in upwards);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Quaternion LookRotation(in Vector3 forward, [uei.DefaultValue("Vector3.up")] in Vector3 upwards) => Quaternion.Internal_LookRotation(in forward, in upwards);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        [uei.ExcludeFromDocs] public static Quaternion LookRotation(Vector3 forward) => Quaternion.Internal_LookRotation(in forward, Vector3.up);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        [uei.ExcludeFromDocs] public static Quaternion LookRotation(in Vector3 forward) => Quaternion.Internal_LookRotation(in forward, Vector3.up);
    }

    [NativeType(Header = "Runtime/Geometry/AABB.h")]
    [NativeHeader("Runtime/Math/MathScripting.h")]
    [NativeHeader("Runtime/Geometry/Ray.h")]
    [NativeHeader("Runtime/Geometry/Intersection.h")]
    public partial struct Bounds
    {
        [NativeMethod("IsInside", IsThreadSafe = true)] extern private readonly bool Internal_Contains(in Vector3 point);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(Vector3 point) => Internal_Contains(in point);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(in Vector3 point) => Internal_Contains(in point);

        [FreeFunction("BoundsScripting::SqrDistance", HasExplicitThis = true, IsThreadSafe = true)] extern private readonly float Internal_SqrDistance(in Vector3 point);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly float SqrDistance(Vector3 point) => Internal_SqrDistance(in point);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly float SqrDistance(in Vector3 point) => Internal_SqrDistance(in point);

        [FreeFunction("IntersectRayAABB", IsThreadSafe = true)] extern static private bool IntersectRayAABB(in Ray ray, in Bounds bounds, out float dist);

        [FreeFunction("BoundsScripting::ClosestPoint", HasExplicitThis = true, IsThreadSafe = true)] extern private readonly Vector3 Internal_ClosestPoint(in Vector3 point);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly Vector3 ClosestPoint(Vector3 point) => Internal_ClosestPoint(in point);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly Vector3 ClosestPoint(in Vector3 point) => Internal_ClosestPoint(in point);
    }

    [NativeHeader("Runtime/Math/ColorSpaceConversion.h")]
    [NativeHeader("NativeKernel/Math/FloatConversion.h")]
    [NativeHeader("Runtime/Math/PerlinNoise.h")]
    public partial struct Mathf
    {
        [FreeFunction(IsThreadSafe = true)] extern public static float GammaToLinearSpace(float value);
        [FreeFunction(IsThreadSafe = true)] extern public static float LinearToGammaSpace(float value);
        [FreeFunction(IsThreadSafe = true)] extern public static Color CorrelatedColorTemperatureToRGB(float kelvin);

        [FreeFunction(IsThreadSafe = true)] extern public static ushort FloatToHalf(float val);
        [FreeFunction(IsThreadSafe = true)] extern public static float HalfToFloat(ushort val);

        [FreeFunction("PerlinNoise::NoiseNormalized", IsThreadSafe = true)] extern public static float PerlinNoise(float x, float y);
        [FreeFunction("PerlinNoise::NoiseNormalized", IsThreadSafe = true)] extern public static float PerlinNoise1D(float x);
    }
}
