// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
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
    [ThreadAndSerializationSafe]
    [UsedByNativeCode]
    public partial struct Matrix4x4
    {
        [ThreadSafe] extern private Quaternion    GetRotation();
        [ThreadSafe] extern private Vector3       GetLossyScale();
        [ThreadSafe] extern private bool          IsIdentity();
        [ThreadSafe] extern private float         GetDeterminant();
        [ThreadSafe] extern private FrustumPlanes DecomposeProjection();


        public Quaternion rotation               { get { return GetRotation(); } }
        public Vector3 lossyScale                { get { return GetLossyScale(); } }
        public bool isIdentity                   { get { return IsIdentity(); } }
        public float determinant                 { get { return GetDeterminant(); } }
        public FrustumPlanes decomposeProjection { get { return DecomposeProjection(); } }

        [ThreadSafe] extern public bool ValidTRS();
        public static float Determinant(Matrix4x4 m) { return m.determinant; }

        [FreeFunction("MatrixScripting::TRS", IsThreadSafe = true)] extern public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s);
        public void SetTRS(Vector3 pos, Quaternion q, Vector3 s) { this = Matrix4x4.TRS(pos, q, s); }

        [FreeFunction("MatrixScripting::Inverse", IsThreadSafe = true)] extern public static Matrix4x4 Inverse(Matrix4x4 m);
        public Matrix4x4 inverse { get { return Matrix4x4.Inverse(this); } }

        [FreeFunction("MatrixScripting::Transpose", IsThreadSafe = true)] extern public static Matrix4x4 Transpose(Matrix4x4 m);
        public Matrix4x4 transpose { get { return Matrix4x4.Transpose(this); } }

        [FreeFunction("MatrixScripting::Ortho", IsThreadSafe = true)] extern public static Matrix4x4 Ortho(float left, float right, float bottom, float top, float zNear, float zFar);
        [FreeFunction("MatrixScripting::Perspective", IsThreadSafe = true)] extern public static Matrix4x4 Perspective(float fov, float aspect, float zNear, float zFar);
        [FreeFunction("MatrixScripting::LookAt", IsThreadSafe = true)] extern public static Matrix4x4 LookAt(Vector3 from, Vector3 to, Vector3 up);

        [FreeFunction("MatrixScripting::Frustum", IsThreadSafe = true)] extern public static Matrix4x4 Frustum(float left, float right, float bottom, float top, float zNear, float zFar);
        public static Matrix4x4 Frustum(FrustumPlanes fp) { return Frustum(fp.left, fp.right, fp.bottom, fp.top, fp.zNear, fp.zFar); }
    }

    [NativeType(Header = "Runtime/Math/Vector3.h")]
    [NativeHeader("Runtime/Math/MathScripting.h")]
    [ThreadAndSerializationSafe]
    [UsedByNativeCode]
    public partial struct Vector3
    {
        [FreeFunction("VectorScripting::Slerp", IsThreadSafe = true)] extern public static Vector3 Slerp(Vector3 a, Vector3 b, float t);
        [FreeFunction("VectorScripting::SlerpUnclamped", IsThreadSafe = true)] extern public static Vector3 SlerpUnclamped(Vector3 a, Vector3 b, float t);

        [FreeFunction("VectorScripting::OrthoNormalize", IsThreadSafe = true)] extern private static void OrthoNormalize2(ref Vector3 a, ref Vector3 b);
        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent) { OrthoNormalize2(ref normal, ref tangent); }
        [FreeFunction("VectorScripting::OrthoNormalize", IsThreadSafe = true)] extern private static void OrthoNormalize3(ref Vector3 a, ref Vector3 b, ref Vector3 c);
        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent, ref Vector3 binormal) { OrthoNormalize3(ref normal, ref tangent, ref binormal); }

        [FreeFunction(IsThreadSafe = true)] extern public static Vector3 RotateTowards(Vector3 current, Vector3 target, float maxRadiansDelta, float maxMagnitudeDelta);
    }

    [NativeType(Header = "Runtime/Math/Quaternion.h")]
    [NativeHeader("Runtime/Math/MathScripting.h")]
    [ThreadAndSerializationSafe]
    [UsedByNativeCode]
    public partial struct Quaternion
    {
        [FreeFunction("FromToQuaternionSafe", IsThreadSafe = true)] extern public static Quaternion FromToRotation(Vector3 fromDirection, Vector3 toDirection);
        [FreeFunction(IsThreadSafe = true)] extern public static Quaternion Inverse(Quaternion rotation);

        [FreeFunction("QuaternionScripting::Slerp", IsThreadSafe = true)]          extern public static Quaternion Slerp(Quaternion a, Quaternion b, float t);
        [FreeFunction("QuaternionScripting::SlerpUnclamped", IsThreadSafe = true)] extern public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t);
        [FreeFunction("QuaternionScripting::Lerp", IsThreadSafe = true)]           extern public static Quaternion Lerp(Quaternion a, Quaternion b, float t);
        [FreeFunction("QuaternionScripting::LerpUnclamped", IsThreadSafe = true)]  extern public static Quaternion LerpUnclamped(Quaternion a, Quaternion b, float t);

        [FreeFunction("EulerToQuaternion", IsThreadSafe = true)] extern private static Quaternion Internal_FromEulerRad(Vector3 euler);
        [FreeFunction("QuaternionScripting::ToEuler", IsThreadSafe = true)] extern private static Vector3 Internal_ToEulerRad(Quaternion rotation);
        [FreeFunction("QuaternionScripting::ToAxisAngle", IsThreadSafe = true)] extern private static void Internal_ToAxisAngleRad(Quaternion q, out Vector3 axis, out float angle);
        [FreeFunction("QuaternionScripting::AngleAxis", IsThreadSafe = true)] extern public static Quaternion AngleAxis(float angle, Vector3 axis);

        [FreeFunction("QuaternionScripting::LookRotation", IsThreadSafe = true)] extern public static Quaternion LookRotation(Vector3 forward, [uei.DefaultValue("Vector3.up")] Vector3 upwards);
        [uei.ExcludeFromDocs] public static Quaternion LookRotation(Vector3 forward) { return LookRotation(forward, Vector3.up); }
    }

    [NativeType(Header = "Runtime/Geometry/AABB.h")]
    [NativeHeader("Runtime/Math/MathScripting.h")]
    [NativeHeader("Runtime/Geometry/Ray.h")]
    [NativeHeader("Runtime/Geometry/Intersection.h")]
    [ThreadAndSerializationSafe]
    [UsedByNativeCode]
    public partial struct Bounds
    {
        [NativeMethod("IsInside", IsThreadSafe = true)] extern public bool Contains(Vector3 point);
        [FreeFunction("BoundsScripting::SqrDistance", HasExplicitThis = true, IsThreadSafe = true)] extern public float SqrDistance(Vector3 point);
        [FreeFunction("IntersectRayAABB", IsThreadSafe = true)] extern static private bool IntersectRayAABB(Ray ray, Bounds bounds, out float dist);
        [FreeFunction("BoundsScripting::ClosestPoint", HasExplicitThis = true, IsThreadSafe = true)] extern public Vector3 ClosestPoint(Vector3 point);
    }

    [ThreadAndSerializationSafe]
    [NativeHeader("Runtime/Utilities/BitUtility.h")]
    [NativeHeader("Runtime/Math/ColorSpaceConversion.h")]
    [NativeHeader("Runtime/Math/FloatConversion.h")]
    [NativeHeader("Runtime/Math/PerlinNoise.h")]
    public partial struct Mathf
    {
        [FreeFunction(IsThreadSafe = true)] extern public static int  ClosestPowerOfTwo(int value);
        [FreeFunction(IsThreadSafe = true)] extern public static bool IsPowerOfTwo(int value);
        [FreeFunction(IsThreadSafe = true)] extern public static int  NextPowerOfTwo(int value);

        [FreeFunction(IsThreadSafe = true)] extern public static float GammaToLinearSpace(float value);
        [FreeFunction(IsThreadSafe = true)] extern public static float LinearToGammaSpace(float value);
        [FreeFunction(IsThreadSafe = true)] extern public static Color CorrelatedColorTemperatureToRGB(float kelvin);

        [FreeFunction(IsThreadSafe = true)] extern public static ushort FloatToHalf(float val);
        [FreeFunction(IsThreadSafe = true)] extern public static float HalfToFloat(ushort val);

        [FreeFunction("PerlinNoise::NoiseNormalized", IsThreadSafe = true)] extern public static float PerlinNoise(float x, float y);
    }
}
