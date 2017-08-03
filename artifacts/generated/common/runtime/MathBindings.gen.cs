// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine
{
[UsedByNativeCode]
[NativeType(Header = "Runtime/Math/Vector3.h")]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Vector3
{
    [ThreadAndSerializationSafe ()]
    public static Vector3 Slerp (Vector3 a, Vector3 b, float t) {
        Vector3 result;
        INTERNAL_CALL_Slerp ( ref a, ref b, t, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Slerp (ref Vector3 a, ref Vector3 b, float t, out Vector3 value);
    public static Vector3 SlerpUnclamped (Vector3 a, Vector3 b, float t) {
        Vector3 result;
        INTERNAL_CALL_SlerpUnclamped ( ref a, ref b, t, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SlerpUnclamped (ref Vector3 a, ref Vector3 b, float t, out Vector3 value);
    private static void Internal_OrthoNormalize2 (ref Vector3 a, ref Vector3 b) {
        INTERNAL_CALL_Internal_OrthoNormalize2 ( ref a, ref b );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_OrthoNormalize2 (ref Vector3 a, ref Vector3 b);
    private static void Internal_OrthoNormalize3 (ref Vector3 a, ref Vector3 b, ref Vector3 c) {
        INTERNAL_CALL_Internal_OrthoNormalize3 ( ref a, ref b, ref c );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_OrthoNormalize3 (ref Vector3 a, ref Vector3 b, ref Vector3 c);
    static public void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent) { Internal_OrthoNormalize2(ref normal, ref tangent); }
    static public void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent, ref Vector3 binormal) { Internal_OrthoNormalize3(ref normal, ref tangent, ref binormal); }
    
    
    public static Vector3 RotateTowards (Vector3 current, Vector3 target, float maxRadiansDelta, float maxMagnitudeDelta) {
        Vector3 result;
        INTERNAL_CALL_RotateTowards ( ref current, ref target, maxRadiansDelta, maxMagnitudeDelta, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_RotateTowards (ref Vector3 current, ref Vector3 target, float maxRadiansDelta, float maxMagnitudeDelta, out Vector3 value);
    [System.Obsolete ("Use Vector3.ProjectOnPlane instead.")]
public static Vector3 Exclude(Vector3 excludeThis, Vector3 fromThat)
        {
            return fromThat - Project(fromThat, excludeThis);
        }
    
    
}

[UsedByNativeCode]
[NativeType(Header = "Runtime/Math/Quaternion.h")]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Quaternion
{
    [ThreadAndSerializationSafe ()]
    public static Quaternion AngleAxis (float angle, Vector3 axis) {
        Quaternion result;
        INTERNAL_CALL_AngleAxis ( angle, ref axis, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AngleAxis (float angle, ref Vector3 axis, out Quaternion value);
    public void ToAngleAxis(out float angle, out Vector3 axis) { Internal_ToAxisAngleRad(this, out axis, out angle); angle *= Mathf.Rad2Deg;  }
    
    
    public static Quaternion FromToRotation (Vector3 fromDirection, Vector3 toDirection) {
        Quaternion result;
        INTERNAL_CALL_FromToRotation ( ref fromDirection, ref toDirection, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_FromToRotation (ref Vector3 fromDirection, ref Vector3 toDirection, out Quaternion value);
    public void SetFromToRotation(Vector3 fromDirection, Vector3 toDirection) { this = FromToRotation(fromDirection, toDirection); }
    
    
    public static Quaternion LookRotation (Vector3 forward, [uei.DefaultValue("Vector3.up")]  Vector3 upwards ) {
        Quaternion result;
        INTERNAL_CALL_LookRotation ( ref forward, ref upwards, out result );
        return result;
    }

    [uei.ExcludeFromDocs]
    public static Quaternion LookRotation (Vector3 forward) {
        Vector3 upwards = Vector3.up;
        Quaternion result;
        INTERNAL_CALL_LookRotation ( ref forward, ref upwards, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_LookRotation (ref Vector3 forward, ref Vector3 upwards, out Quaternion value);
    public static Quaternion Slerp (Quaternion a, Quaternion b, float t) {
        Quaternion result;
        INTERNAL_CALL_Slerp ( ref a, ref b, t, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Slerp (ref Quaternion a, ref Quaternion b, float t, out Quaternion value);
    public static Quaternion SlerpUnclamped (Quaternion a, Quaternion b, float t) {
        Quaternion result;
        INTERNAL_CALL_SlerpUnclamped ( ref a, ref b, t, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SlerpUnclamped (ref Quaternion a, ref Quaternion b, float t, out Quaternion value);
    public static Quaternion Lerp (Quaternion a, Quaternion b, float t) {
        Quaternion result;
        INTERNAL_CALL_Lerp ( ref a, ref b, t, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Lerp (ref Quaternion a, ref Quaternion b, float t, out Quaternion value);
    public static Quaternion LerpUnclamped (Quaternion a, Quaternion b, float t) {
        Quaternion result;
        INTERNAL_CALL_LerpUnclamped ( ref a, ref b, t, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_LerpUnclamped (ref Quaternion a, ref Quaternion b, float t, out Quaternion value);
    public static Quaternion RotateTowards(Quaternion from, Quaternion to, float maxDegreesDelta)
        {
            float angle = Quaternion.Angle(from, to);
            if (angle == 0.0f)
                return to;
            float slerpValue = Mathf.Min(1.0f, maxDegreesDelta / angle);
            return SlerpUnclamped(from, to, slerpValue);
        }
    
    
    public static Quaternion Inverse (Quaternion rotation) {
        Quaternion result;
        INTERNAL_CALL_Inverse ( ref rotation, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Inverse (ref Quaternion rotation, out Quaternion value);
    public Vector3 eulerAngles { get { return Internal_MakePositive(Internal_ToEulerRad(this) * Mathf.Rad2Deg); } set { this = Internal_FromEulerRad(value * Mathf.Deg2Rad); } }
    
    
    static public Quaternion Euler(float x, float y, float z) { return Internal_FromEulerRad(new Vector3(x, y, z) * Mathf.Deg2Rad); }
    
    
    static public Quaternion Euler(Vector3 euler) { return Internal_FromEulerRad(euler * Mathf.Deg2Rad); }
    
    
    private static Vector3 Internal_ToEulerRad (Quaternion rotation) {
        Vector3 result;
        INTERNAL_CALL_Internal_ToEulerRad ( ref rotation, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_ToEulerRad (ref Quaternion rotation, out Vector3 value);
    private static Quaternion Internal_FromEulerRad (Vector3 euler) {
        Quaternion result;
        INTERNAL_CALL_Internal_FromEulerRad ( ref euler, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_FromEulerRad (ref Vector3 euler, out Quaternion value);
    private static void Internal_ToAxisAngleRad (Quaternion q, out Vector3 axis, out float angle) {
        INTERNAL_CALL_Internal_ToAxisAngleRad ( ref q, out axis, out angle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_ToAxisAngleRad (ref Quaternion q, out Vector3 axis, out float angle);
    [System.Obsolete ("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees")]
static public Quaternion EulerRotation(float x, float y, float z) { return Internal_FromEulerRad(new Vector3(x, y, z)); }
    [System.Obsolete ("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees")]
public static Quaternion EulerRotation(Vector3 euler) { return Internal_FromEulerRad(euler); }
    [System.Obsolete ("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees")]
public void SetEulerRotation(float x, float y, float z) { this = Internal_FromEulerRad(new Vector3(x, y, z)); }
    [System.Obsolete ("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees")]
public void SetEulerRotation(Vector3 euler) { this = Internal_FromEulerRad(euler); }
    [System.Obsolete ("Use Quaternion.eulerAngles instead. This function was deprecated because it uses radians instead of degrees")]
public Vector3 ToEuler() { return Internal_ToEulerRad(this); }
    
    
    [System.Obsolete ("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees")]
static public Quaternion EulerAngles(float x, float y, float z) { return Internal_FromEulerRad(new Vector3(x, y, z)); }
    [System.Obsolete ("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees")]
public static Quaternion EulerAngles(Vector3 euler) {  return Internal_FromEulerRad(euler); }
    
    
    [System.Obsolete ("Use Quaternion.ToAngleAxis instead. This function was deprecated because it uses radians instead of degrees")]
public void ToAxisAngle(out Vector3 axis, out float angle) { Internal_ToAxisAngleRad(this, out axis, out angle);  }
    
    
    [System.Obsolete ("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees")]
public void SetEulerAngles(float x, float y, float z) { SetEulerRotation(new Vector3(x, y, z)); }
    [System.Obsolete ("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees")]
public void SetEulerAngles(Vector3 euler) { this = EulerRotation(euler); }
    [System.Obsolete ("Use Quaternion.eulerAngles instead. This function was deprecated because it uses radians instead of degrees")]
public static Vector3 ToEulerAngles(Quaternion rotation) { return Quaternion.Internal_ToEulerRad(rotation); }
    [System.Obsolete ("Use Quaternion.eulerAngles instead. This function was deprecated because it uses radians instead of degrees")]
public Vector3 ToEulerAngles() { return Quaternion.Internal_ToEulerRad(this); }
    
    
    [System.Obsolete ("Use Quaternion.AngleAxis instead. This function was deprecated because it uses radians instead of degrees")]
    public static Quaternion AxisAngle (Vector3 axis, float angle) {
        Quaternion result;
        INTERNAL_CALL_AxisAngle ( ref axis, angle, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AxisAngle (ref Vector3 axis, float angle, out Quaternion value);
    [System.Obsolete ("Use Quaternion.AngleAxis instead. This function was deprecated because it uses radians instead of degrees")]
public void SetAxisAngle(Vector3 axis, float angle) { this = AxisAngle(axis, angle); }
    
    
}

[Serializable]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct FrustumPlanes
{
            public float left;
            public float right;
            public float bottom;
            public float top;
            public float zNear;
            public float zFar;
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Matrix4x4
{
    [ThreadAndSerializationSafe ()]
    public static Matrix4x4 Inverse (Matrix4x4 m) {
        Matrix4x4 result;
        INTERNAL_CALL_Inverse ( ref m, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Inverse (ref Matrix4x4 m, out Matrix4x4 value);
    public static Matrix4x4 Transpose (Matrix4x4 m) {
        Matrix4x4 result;
        INTERNAL_CALL_Transpose ( ref m, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Transpose (ref Matrix4x4 m, out Matrix4x4 value);
    internal static bool Invert (Matrix4x4 inMatrix, out Matrix4x4 dest) {
        return INTERNAL_CALL_Invert ( ref inMatrix, out dest );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Invert (ref Matrix4x4 inMatrix, out Matrix4x4 dest);
    public Matrix4x4 inverse { get { return Matrix4x4.Inverse(this); } }
    
    
    public Matrix4x4 transpose { get { return Matrix4x4.Transpose(this); } }
    
    
    public  Quaternion rotation
    {
        get { Quaternion tmp; INTERNAL_get_rotation(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_rotation (out Quaternion value) ;


    public  Vector3 lossyScale
    {
        get { Vector3 tmp; INTERNAL_get_lossyScale(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_lossyScale (out Vector3 value) ;


    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool ValidTRS () ;

    public  bool isIdentity
    {
        get {return default(bool);}
    }

    public static float Determinant (Matrix4x4 m) {
        return INTERNAL_CALL_Determinant ( ref m );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_Determinant (ref Matrix4x4 m);
    public float determinant { get { return Matrix4x4.Determinant(this); } }
    
    
    public void SetTRS(Vector3 pos, Quaternion q, Vector3 s)
        {
            this = Matrix4x4.TRS(pos, q, s);
        }
    
    
    public static Matrix4x4 TRS (Vector3 pos, Quaternion q, Vector3 s) {
        Matrix4x4 result;
        INTERNAL_CALL_TRS ( ref pos, ref q, ref s, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_TRS (ref Vector3 pos, ref Quaternion q, ref Vector3 s, out Matrix4x4 value);
    public static Matrix4x4 Ortho (float left, float right, float bottom, float top, float zNear, float zFar) {
        Matrix4x4 result;
        INTERNAL_CALL_Ortho ( left, right, bottom, top, zNear, zFar, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Ortho (float left, float right, float bottom, float top, float zNear, float zFar, out Matrix4x4 value);
    public static Matrix4x4 Perspective (float fov, float aspect, float zNear, float zFar) {
        Matrix4x4 result;
        INTERNAL_CALL_Perspective ( fov, aspect, zNear, zFar, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Perspective (float fov, float aspect, float zNear, float zFar, out Matrix4x4 value);
    public static Matrix4x4 LookAt (Vector3 from, Vector3 to, Vector3 up) {
        Matrix4x4 result;
        INTERNAL_CALL_LookAt ( ref from, ref to, ref up, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_LookAt (ref Vector3 from, ref Vector3 to, ref Vector3 up, out Matrix4x4 value);
    public  FrustumPlanes decomposeProjection
    {
        get { FrustumPlanes tmp; INTERNAL_get_decomposeProjection(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_decomposeProjection (out FrustumPlanes value) ;


    public static Matrix4x4 Frustum(float left, float right, float bottom, float top, float zNear, float zFar)
        {
            return FrustumInternal(left, right, bottom, top, zNear, zFar);
        }
    
    
    public static Matrix4x4 Frustum(FrustumPlanes frustumPlanes)
        {
            return FrustumInternal(frustumPlanes.left, frustumPlanes.right, frustumPlanes.bottom, frustumPlanes.top, frustumPlanes.zNear, frustumPlanes.zFar);
        }
    
    
    private static Matrix4x4 FrustumInternal (float left, float right, float bottom, float top, float zNear, float zFar) {
        Matrix4x4 result;
        INTERNAL_CALL_FrustumInternal ( left, right, bottom, top, zNear, zFar, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_FrustumInternal (float left, float right, float bottom, float top, float zNear, float zFar, out Matrix4x4 value);
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Bounds
{
    [ThreadAndSerializationSafe ()]
    private static bool Internal_Contains (Bounds m, Vector3 point) {
        return INTERNAL_CALL_Internal_Contains ( ref m, ref point );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_Contains (ref Bounds m, ref Vector3 point);
    public bool Contains(Vector3 point) { return Internal_Contains(this, point); }
    
    
    private static float Internal_SqrDistance (Bounds m, Vector3 point) {
        return INTERNAL_CALL_Internal_SqrDistance ( ref m, ref point );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_Internal_SqrDistance (ref Bounds m, ref Vector3 point);
    public float SqrDistance(Vector3 point) { return Internal_SqrDistance(this, point); }
    
    
    private static bool Internal_IntersectRay (ref Ray ray, ref Bounds bounds, out float distance) {
        return INTERNAL_CALL_Internal_IntersectRay ( ref ray, ref bounds, out distance );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_IntersectRay (ref Ray ray, ref Bounds bounds, out float distance);
    public bool IntersectRay(Ray ray) { float dist; return Internal_IntersectRay(ref ray, ref this, out dist); }
    
    
    public bool IntersectRay(Ray ray, out float distance) { return Internal_IntersectRay(ref ray, ref this, out distance);  }
    
    
    private static Vector3 Internal_GetClosestPoint (ref Bounds bounds, ref Vector3 point) {
        Vector3 result;
        INTERNAL_CALL_Internal_GetClosestPoint ( ref bounds, ref point, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GetClosestPoint (ref Bounds bounds, ref Vector3 point, out Vector3 value);
    public Vector3 ClosestPoint(Vector3 point)
        {
            return Internal_GetClosestPoint(ref this, ref point);
        }
    
    
}


} 

namespace UnityEngine
{


[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Mathf
{
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int ClosestPowerOfTwo (int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GammaToLinearSpace (float value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float LinearToGammaSpace (float value) ;

    public static Color CorrelatedColorTemperatureToRGB (float kelvin) {
        Color result;
        INTERNAL_CALL_CorrelatedColorTemperatureToRGB ( kelvin, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CorrelatedColorTemperatureToRGB (float kelvin, out Color value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsPowerOfTwo (int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int NextPowerOfTwo (int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float PerlinNoise (float x, float y) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  ushort FloatToHalf (float val) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float HalfToFloat (ushort val) ;

}

[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Keyframe
{
    float m_Time;
    float m_Value;
    float m_InTangent;
    float m_OutTangent;
    
    
    
            int m_TangentMode;
    
    
    public Keyframe(float time, float value)
        {
            m_Time = time;
            m_Value = value;
            m_InTangent = 0;
            m_OutTangent = 0;
            m_TangentMode = 0;
        }
    
    
    public Keyframe(float time, float value, float inTangent, float outTangent)
        {
            m_Time = time;
            m_Value = value;
            m_InTangent = inTangent;
            m_OutTangent = outTangent;
            m_TangentMode = 0;
        }
    
    
    public float time { get { return m_Time; } set { m_Time = value; }  }
    
    
    public float value { get { return m_Value; } set { m_Value = value; }  }
    
    
    public float inTangent { get { return m_InTangent; } set { m_InTangent = value; }  }
    
    
    public float outTangent { get { return m_OutTangent; } set { m_OutTangent = value; }  }
    
    
    public int tangentMode
        {
            get
            {
                return m_TangentMode;
            }
            set
            {
                m_TangentMode = value;
            }
        }
    
    
}

public enum WrapMode
{
    
    Once = 1,
    
    Loop = 2,
    
    PingPong = 4,
    
    Default = 0,
    
    ClampForever = 8,
    
    Clamp = 1,
}

#pragma warning disable 414


[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public sealed partial class AnimationCurve
{
    
            internal IntPtr m_Ptr;
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Cleanup () ;

    
            ~AnimationCurve()
        {
            Cleanup();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public float Evaluate (float time) ;

    public Keyframe[]  keys { get { return GetKeys(); } set { SetKeys(value); } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int AddKey (float time, float value) ;

    public int AddKey(Keyframe key) { return AddKey_Internal(key); }
    
    
    private int AddKey_Internal (Keyframe key) {
        return INTERNAL_CALL_AddKey_Internal ( this, ref key );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_AddKey_Internal (AnimationCurve self, ref Keyframe key);
    public int MoveKey (int index, Keyframe key) {
        return INTERNAL_CALL_MoveKey ( this, index, ref key );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_MoveKey (AnimationCurve self, int index, ref Keyframe key);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void RemoveKey (int index) ;

    public Keyframe this[int index]
            {
            get { return GetKey_Internal(index); }
        }
    
    
    public extern  int length
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetKeys (Keyframe[] keys) ;

    private Keyframe GetKey_Internal (int index) {
        Keyframe result;
        INTERNAL_CALL_GetKey_Internal ( this, index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetKey_Internal (AnimationCurve self, int index, out Keyframe value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Keyframe[] GetKeys () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SmoothTangents (int index, float weight) ;

    public static AnimationCurve Constant(float timeStart, float timeEnd, float value)
        {
            return Linear(timeStart, value, timeEnd, value);
        }
    
    
    public static AnimationCurve Linear(float timeStart, float valueStart, float timeEnd, float valueEnd)
        {
            float tangent = (valueEnd - valueStart) / (timeEnd - timeStart);
            Keyframe[] keys = { new Keyframe(timeStart, valueStart, 0.0F, tangent), new Keyframe(timeEnd, valueEnd, tangent, 0.0F) };
            return new AnimationCurve(keys);
        }
    
    
    public static AnimationCurve EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd)
        {
            Keyframe[] keys = { new Keyframe(timeStart, valueStart, 0.0F, 0.0F), new Keyframe(timeEnd, valueEnd, 0.0F, 0.0F) };
            return new AnimationCurve(keys);
        }
    
    
    public extern  WrapMode preWrapMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  WrapMode postWrapMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public AnimationCurve(params Keyframe[] keys) { Init(keys); }
    
    
    
    
    [RequiredByNativeCode]
    public AnimationCurve()  { Init(null); }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Init (Keyframe[] keys) ;

}

#pragma warning restore 414

}
