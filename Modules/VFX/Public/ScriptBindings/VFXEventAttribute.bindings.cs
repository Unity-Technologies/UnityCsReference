// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.VFX
{
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(Header = "Modules/VFX/Public/VFXEventAttribute.h")]
    public sealed class VFXEventAttribute : IDisposable
    {
        private IntPtr m_Ptr;
        private bool m_Owner;
        private VFXEventAttribute(IntPtr ptr, bool owner)
        {
            m_Ptr = ptr;
            m_Owner = owner;
        }

        private VFXEventAttribute() : this(IntPtr.Zero, false)
        {
        }

        extern static internal IntPtr Internal_Create();

        static internal VFXEventAttribute Internal_InstanciateVFXEventAttribute(VisualEffectAsset vfxAsset)
        {
            var eventAttribute = new VFXEventAttribute(Internal_Create(), true);
            eventAttribute.Internal_Init(vfxAsset);
            return eventAttribute;
        }

        extern private void Internal_Init(VisualEffectAsset vfxAsset);

        private void Release()
        {
            if (m_Owner && m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
            }
            m_Ptr = IntPtr.Zero;
        }

        ~VFXEventAttribute()
        {
            Release();
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        [NativeMethod(IsThreadSafe = true)]
        extern static internal void Internal_Destroy(IntPtr ptr);

        [NativeName("HasValueFromScript<bool>")] extern public bool HasBool(int name);
        [NativeName("HasValueFromScript<int>")] extern public bool HasInt(int name);
        [NativeName("HasValueFromScript<UInt32>")] extern public bool HasUint(int name);
        [NativeName("HasValueFromScript<float>")] extern public bool HasFloat(int name);
        [NativeName("HasValueFromScript<Vector2f>")] extern public bool HasVector2(int name);
        [NativeName("HasValueFromScript<Vector3f>")] extern public bool HasVector3(int name);
        [NativeName("HasValueFromScript<Vector4f>")] extern public bool HasVector4(int name);
        [NativeName("HasValueFromScript<Matrix4x4f>")] extern public bool HasMatrix4x4(int name);

        [NativeName("SetValueFromScript<bool>")] extern public void SetBool(int name, bool b);
        [NativeName("SetValueFromScript<int>")] extern public void SetInt(int name, int i);
        [NativeName("SetValueFromScript<UInt32>")] extern public void SetUint(int name, uint i);
        [NativeName("SetValueFromScript<float>")] extern public void SetFloat(int name, float f);
        [NativeName("SetValueFromScript<Vector2f>")] extern public void SetVector2(int name, Vector2 v);
        [NativeName("SetValueFromScript<Vector3f>")] extern public void SetVector3(int name, Vector3 v);
        [NativeName("SetValueFromScript<Vector4f>")] extern public void SetVector4(int name, Vector4 v);
        [NativeName("SetValueFromScript<Matrix4x4f>")] extern public void SetMatrix4x4(int name, Matrix4x4 v);

        [NativeName("GetValueFromScript<bool>")] extern public bool GetBool(int name);
        [NativeName("GetValueFromScript<int>")] extern public int GetInt(int name);
        [NativeName("GetValueFromScript<UInt32>")] extern public uint GetUint(int name);
        [NativeName("GetValueFromScript<float>")] extern public float GetFloat(int name);
        [NativeName("GetValueFromScript<Vector2f>")] extern public Vector2 GetVector2(int name);
        [NativeName("GetValueFromScript<Vector3f>")] extern public Vector3 GetVector3(int name);
        [NativeName("GetValueFromScript<Vector4f>")] extern public Vector4 GetVector4(int name);
        [NativeName("GetValueFromScript<Matrix4x4f>")] extern public Matrix4x4 GetMatrix4x4(int name);

        public bool HasBool(string name)
        {
            return HasBool(Shader.PropertyToID(name));
        }

        public bool HasInt(string name)
        {
            return HasInt(Shader.PropertyToID(name));
        }

        public bool HasUint(string name)
        {
            return HasUint(Shader.PropertyToID(name));
        }

        public bool HasFloat(string name)
        {
            return HasFloat(Shader.PropertyToID(name));
        }

        public bool HasVector2(string name)
        {
            return HasVector2(Shader.PropertyToID(name));
        }

        public bool HasVector3(string name)
        {
            return HasVector3(Shader.PropertyToID(name));
        }

        public bool HasVector4(string name)
        {
            return HasVector4(Shader.PropertyToID(name));
        }

        public bool HasMatrix4x4(string name)
        {
            return HasMatrix4x4(Shader.PropertyToID(name));
        }

        public void SetBool(string name, bool b)
        {
            SetBool(Shader.PropertyToID(name), b);
        }

        public void SetInt(string name, int i)
        {
            SetInt(Shader.PropertyToID(name), i);
        }

        public void SetUint(string name, uint i)
        {
            SetUint(Shader.PropertyToID(name), i);
        }

        public void SetFloat(string name, float f)
        {
            SetFloat(Shader.PropertyToID(name), f);
        }

        public void SetVector2(string name, Vector2 v)
        {
            SetVector2(Shader.PropertyToID(name), v);
        }

        public void SetVector3(string name, Vector3 v)
        {
            SetVector3(Shader.PropertyToID(name), v);
        }

        public void SetVector4(string name, Vector4 v)
        {
            SetVector4(Shader.PropertyToID(name), v);
        }

        public void SetMatrix4x4(string name, Matrix4x4 v)
        {
            SetMatrix4x4(Shader.PropertyToID(name), v);
        }

        public bool GetBool(string name)
        {
            return GetBool(Shader.PropertyToID(name));
        }

        public int GetInt(string name)
        {
            return GetInt(Shader.PropertyToID(name));
        }

        public uint GetUint(string name)
        {
            return GetUint(Shader.PropertyToID(name));
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
    }
}
