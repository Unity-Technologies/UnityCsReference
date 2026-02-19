// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.VFX
{
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

        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<bool>")] extern public bool GetBool(int nameID);
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<int>")] extern public int GetInt(int nameID);
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<UInt32>")] extern public uint GetUInt(int nameID);
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<float>")] extern public float GetFloat(int nameID);
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Vector2f>")] extern public Vector2 GetVector2(int nameID);
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Vector3f>")] extern public Vector3 GetVector3(int nameID);
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Vector4f>")] extern public Vector4 GetVector4(int nameID);
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Matrix4x4f>")] extern public Matrix4x4 GetMatrix4x4(int nameID);
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Texture*>")] extern public Texture GetTexture(int nameID);
        [NativeMethod(ThrowsException = true), NativeName("GetValueFromScript<Mesh*>")] extern public Mesh GetMesh(int nameID);

        public AnimationCurve GetAnimationCurve(int nameID)
        {
            var animationCurve = new AnimationCurve();
            Internal_GetAnimationCurveFromScript(nameID, animationCurve);
            return animationCurve;
        }

        [NativeMethod(ThrowsException = true)]
        extern internal void Internal_GetAnimationCurveFromScript(int nameID, AnimationCurve curve);
        public Gradient GetGradient(int nameID)
        {
            var gradient = new Gradient();
            Internal_GetGradientFromScript(nameID, gradient);
            return gradient;
        }

        [NativeMethod(ThrowsException = true)]
        extern internal void Internal_GetGradientFromScript(int nameID, Gradient gradient);

        public bool GetBool(string name)
        {
            return GetBool(Shader.PropertyToID(name));
        }

        public int GetInt(string name)
        {
            return GetInt(Shader.PropertyToID(name));
        }

        public uint GetUInt(string name)
        {
            return GetUInt(Shader.PropertyToID(name));
        }

        public float GetFloat(string name)
        {
            return GetFloat(Shader.PropertyToID(name));
        }

        public Vector2 GetVector2(string name)
        {
            return GetVector2(Shader.PropertyToID(name));
        }

        public Vector3 GetVector3(string name)
        {
            return GetVector3(Shader.PropertyToID(name));
        }

        public Vector4 GetVector4(string name)
        {
            return GetVector4(Shader.PropertyToID(name));
        }

        public Matrix4x4 GetMatrix4x4(string name)
        {
            return GetMatrix4x4(Shader.PropertyToID(name));
        }

        public Texture GetTexture(string name)
        {
            return GetTexture(Shader.PropertyToID(name));
        }

        public AnimationCurve GetAnimationCurve(string name)
        {
            return GetAnimationCurve(Shader.PropertyToID(name));
        }

        public Gradient GetGradient(string name)
        {
            return GetGradient(Shader.PropertyToID(name));
        }

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
