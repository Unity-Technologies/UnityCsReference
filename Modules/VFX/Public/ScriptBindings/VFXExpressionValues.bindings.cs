// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.VFX
{
    ///<summary>This class is a wrapper to the set of expression values.</summary>
    ///<remarks>Only used with <see cref="VFX.VFXSpawnerState" />.</remarks>
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/VFX/Public/VFXExpressionValues.h")]
    public class VFXExpressionValues
    {
        internal IntPtr m_Ptr;
        private VFXExpressionValues()
        {
        }

        [RequiredByNativeCode]
        static internal VFXExpressionValues CreateExpressionValuesWrapper(IntPtr ptr)
        {
            var expressionValue = new VFXExpressionValues();
            expressionValue.m_Ptr = ptr;
            return expressionValue;
        }

        ///<summary>Gets the boolean value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The bool value of the bound named expression.</returns>
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<bool>")] extern public bool GetBool(int nameID);
        ///<summary>Gets the integer value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The integer value of the bound named expression.</returns>
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<int>")] extern public int GetInt(int nameID);
        ///<summary>Gets the unsigned integer value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The unsigned integer value of the bound named expression.</returns>
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<UInt32>")] extern public uint GetUInt(int nameID);
        ///<summary>Gets the float value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The float value of the bound named expression.</returns>
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<float>")] extern public float GetFloat(int nameID);
        ///<summary>Gets the Vector2 value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The Vector2 value of the bound named expression.</returns>
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Vector2f>")] extern public Vector2 GetVector2(int nameID);
        ///<summary>Gets the Vector3 value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The Vector3 value of the bound named expression.</returns>
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Vector3f>")] extern public Vector3 GetVector3(int nameID);
        ///<summary>Gets the Vector4 value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The Vector4 value of the bound named expression.</returns>
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Vector4f>")] extern public Vector4 GetVector4(int nameID);
        ///<summary>Gets the Matrix4x4 value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The Matrix4x4 value of the bound named expression.</returns>
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Matrix4x4f>")] extern public Matrix4x4 GetMatrix4x4(int nameID);
        ///<summary>Gets the texture that corresponds to the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The texture that corresponds to the bound named expression.</returns>
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Texture*>")] extern public Texture GetTexture(int nameID);
        ///<summary>Gets the mesh that corresponds to the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The mesh that corresponds to the bound named expression.</returns>
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Mesh*>")] extern public Mesh GetMesh(int nameID);

        ///<summary>Gets the animation curve that corresponds to the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The animation curve that corresponds to the bound named expression.</returns>
        public AnimationCurve GetAnimationCurve(int nameID)
        {
            var animationCurve = new AnimationCurve();
            Internal_GetAnimationCurveFromScript(nameID, animationCurve);
            return animationCurve;
        }

        [NativeMethod(ThrowsException = true)]
        extern internal void Internal_GetAnimationCurveFromScript(int nameID, AnimationCurve curve);
        ///<summary>Gets the gradient that corresponds to the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="nameID">The name ID of the property retrieved by <see cref="Shader.PropertyToID" />.</param>
        ///<returns>The gradient that corresponds to the bound named expression.</returns>
        public Gradient GetGradient(int nameID)
        {
            var gradient = new Gradient();
            Internal_GetGradientFromScript(nameID, gradient);
            return gradient;
        }

        [NativeMethod(ThrowsException = true)]
        extern internal void Internal_GetGradientFromScript(int nameID, Gradient gradient);

        ///<summary>Gets the boolean value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The bool value of the bound named expression.</returns>
        public bool GetBool(string name)
        {
            return GetBool(Shader.PropertyToID(name));
        }

        ///<summary>Gets the integer value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The integer value of the bound named expression.</returns>
        public int GetInt(string name)
        {
            return GetInt(Shader.PropertyToID(name));
        }

        ///<summary>Gets the unsigned integer value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The unsigned integer value of the bound named expression.</returns>
        public uint GetUInt(string name)
        {
            return GetUInt(Shader.PropertyToID(name));
        }

        ///<summary>Gets the float value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The float value of the bound named expression.</returns>
        public float GetFloat(string name)
        {
            return GetFloat(Shader.PropertyToID(name));
        }

        ///<summary>Gets the Vector2 value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The Vector2 value of the bound named expression.</returns>
        public Vector2 GetVector2(string name)
        {
            return GetVector2(Shader.PropertyToID(name));
        }

        ///<summary>Gets the Vector3 value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The Vector3 value of the bound named expression.</returns>
        public Vector3 GetVector3(string name)
        {
            return GetVector3(Shader.PropertyToID(name));
        }

        ///<summary>Gets the Vector4 value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The Vector4 value of the bound named expression.</returns>
        public Vector4 GetVector4(string name)
        {
            return GetVector4(Shader.PropertyToID(name));
        }

        ///<summary>Gets the Matrix4x4 value of the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The Matrix4x4 value of the bound named expression.</returns>
        public Matrix4x4 GetMatrix4x4(string name)
        {
            return GetMatrix4x4(Shader.PropertyToID(name));
        }

        ///<summary>Gets the texture that corresponds to the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The texture that corresponds to the bound named expression.</returns>
        public Texture GetTexture(string name)
        {
            return GetTexture(Shader.PropertyToID(name));
        }

        ///<summary>Gets the animation curve that corresponds to the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The animation curve that corresponds to the bound named expression.</returns>
        public AnimationCurve GetAnimationCurve(string name)
        {
            return GetAnimationCurve(Shader.PropertyToID(name));
        }

        ///<summary>Gets the gradient that corresponds to the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The gradient that corresponds to the bound named expression.</returns>
        public Gradient GetGradient(string name)
        {
            return GetGradient(Shader.PropertyToID(name));
        }

        ///<summary>Gets the mesh that corresponds to the bound named expression.</summary>
        ///<remarks>Throws an <see cref="System.ArgumentException" /> if the entry isn't available or its type doesn't match.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The mesh that corresponds to the bound named expression.</returns>
        public Mesh GetMesh(string name)
        {
            return GetMesh(Shader.PropertyToID(name));
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(VFXExpressionValues vFXExpressionValues) => vFXExpressionValues.m_Ptr;
        }
    }
}
