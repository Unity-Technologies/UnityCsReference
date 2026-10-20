// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.VFX
{
    ///<summary>This class handles the properties that you transmit to a system using a <see cref="VFX.VisualEffect.SendEvent" />.</summary>
    ///<remarks>Lets you alter initial properties from a Spawn context.</remarks>
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/VFX/Public/VFXEventAttribute.h")]
    public sealed class VFXEventAttribute : IDisposable
    {
        private IntPtr m_Ptr;
        private bool m_Owner;
        private VisualEffectAsset m_VfxAsset;
        private VFXEventAttribute(IntPtr ptr, bool owner, VisualEffectAsset vfxAsset)
        {
            m_Ptr = ptr;
            m_Owner = owner;
            m_VfxAsset = vfxAsset;
        }

        private VFXEventAttribute(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        private VFXEventAttribute() : this(IntPtr.Zero, false, null)
        {
        }

        internal static VFXEventAttribute CreateEventAttributeWrapper()
        {
            var eventAttribute = new VFXEventAttribute(IntPtr.Zero, false, null);
            return eventAttribute;
        }

        internal void SetWrapValue(IntPtr ptrToEventAttribute)
        {
            if (m_Owner)
                throw new Exception("VFXSpawnerState : SetWrapValue is reserved to CreateWrapper object");
            m_Ptr = ptrToEventAttribute;

        }

        ///<summary>The copy constructor for the VFXEventAttribute class.</summary>
        ///<remarks>The default constructor is private. Only instantiate this class from the method <see cref="VFX.VisualEffect.CreateVFXEventAttribute" /> or with this copy constructor.</remarks>
        ///<param name="original">The source VFXEventAttribute to copy from.</param>
        public VFXEventAttribute(VFXEventAttribute original)
        {
            if (original == null)
                throw new ArgumentNullException("VFXEventAttribute expect a non null attribute");
            m_Ptr = Internal_Create();
            m_VfxAsset = original.m_VfxAsset;
            Internal_InitFromEventAttribute(original);
        }

        extern static internal IntPtr Internal_Create();

        static internal VFXEventAttribute Internal_InstanciateVFXEventAttribute(VisualEffectAsset vfxAsset)
        {
            var eventAttribute = new VFXEventAttribute(Internal_Create(), true, vfxAsset);
            eventAttribute.Internal_InitFromAsset(vfxAsset);
            return eventAttribute;
        }

        extern internal void Internal_InitFromAsset(VisualEffectAsset vfxAsset);
        extern internal void Internal_InitFromEventAttribute(VFXEventAttribute vfxEventAttribute);

        internal VisualEffectAsset vfxAsset { get { return m_VfxAsset; } }

        private void Release()
        {
            if (m_Owner && m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
            }
            m_Ptr = IntPtr.Zero;
            m_VfxAsset = null;
        }

#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~VFXEventAttribute()
        {
            Release();
        }
#pragma warning restore UA5000

        ///<exclude />
        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        [NativeMethod(IsThreadSafe = true)]
        extern static internal void Internal_Destroy(IntPtr ptr);

        ///<summary>Use this method to check if the VFXEventAttribute stores a bool with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>True if the VFXEventAttribute stores a bool with that name. Otherwise, false.</returns>
        [NativeName("HasValueFromScript<bool>")] extern public bool HasBool(int nameID);
        ///<summary>Use this method to check if the VFXEventAttribute stores a integer with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>True if the VFXEventAttribute stores an integer with that name. Otherwise, false.</returns>
        [NativeName("HasValueFromScript<int>")] extern public bool HasInt(int nameID);
        ///<summary>Use this method to check if the VFXEventAttribute stores a unsigned integer with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>True if the VFXEventAttribute stores an unsigned integer with that name. Otherwise, false.</returns>
        [NativeName("HasValueFromScript<UInt32>")] extern public bool HasUint(int nameID);
        ///<summary>Use this method to check if the VFXEventAttribute stores a float with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>True if the VFXEventAttribute stores a float with that name. Otherwise, false.</returns>
        [NativeName("HasValueFromScript<float>")] extern public bool HasFloat(int nameID);
        ///<summary>Use this method to check if the VFXEventAttribute stores a Vector2 with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>True if the VFXEventAttribute stores a Vector2 with that name. Otherwise, false.</returns>
        [NativeName("HasValueFromScript<Vector2f>")] extern public bool HasVector2(int nameID);
        ///<summary>Use this method to check if the VFXEventAttribute stores a Vector3 with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>True if the VFXEventAttribute stores a Vector3 with that name. Otherwise, false.</returns>
        [NativeName("HasValueFromScript<Vector3f>")] extern public bool HasVector3(int nameID);
        ///<summary>Use this method to check if the VFXEventAttribute stores a Vector4 with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>True if the VFXEventAttribute stores a Vector4 with that name. Otherwise, false.</returns>
        [NativeName("HasValueFromScript<Vector4f>")] extern public bool HasVector4(int nameID);
        ///<summary>Use this method to check if the VFXEventAttribute stores a Matrix4x4 with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>True if the VFXEventAttribute stores a Matrix4x4 with that name. Otherwise, false.</returns>
        [NativeName("HasValueFromScript<Matrix4x4f>")] extern public bool HasMatrix4x4(int nameID);

        ///<summary>Use this method to set the value of a bool with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="b">The new bool value.</param>
        [NativeName("SetValueFromScript<bool>")] extern public void SetBool(int nameID, bool b);
        ///<summary>Use this method to set the value of an integer with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="i">The new integer value.</param>
        [NativeName("SetValueFromScript<int>")] extern public void SetInt(int nameID, int i);
        ///<summary>Use this method to set the value of an unsigned integer with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="i">The new unsigned integer value.</param>
        [NativeName("SetValueFromScript<UInt32>")] extern public void SetUint(int nameID, uint i);
        ///<summary>Use this method to set the value of a float with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="f">The new float value.</param>
        [NativeName("SetValueFromScript<float>")] extern public void SetFloat(int nameID, float f);
        ///<summary>Use this method to set the value of a Vector2 with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="v">The new Vector2 value.</param>
        [NativeName("SetValueFromScript<Vector2f>")] extern public void SetVector2(int nameID, Vector2 v);
        ///<summary>Use this method to set the value of a Vector3 with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="v">The new Vector3 value.</param>
        [NativeName("SetValueFromScript<Vector3f>")] extern public void SetVector3(int nameID, Vector3 v);
        ///<summary>Use this method to set the value of a Vector4 with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="v">The new Vector4 value.</param>
        [NativeName("SetValueFromScript<Vector4f>")] extern public void SetVector4(int nameID, Vector4 v);
        ///<summary>Use this method to set the value of a Matrix4x4 with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="v">The new Matrix4x4 value.</param>
        [NativeName("SetValueFromScript<Matrix4x4f>")] extern public void SetMatrix4x4(int nameID, Matrix4x4 v);

        ///<summary>Use this method to get the value of a named bool property from the VFXEventAttribute.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the bool you specify. Returns <c>false</c> if <see cref="VFX.VFXEventAttribute.HasBool" /> returns <c>false</c>.</returns>
        [NativeName("GetValueFromScript<bool>")] extern public bool GetBool(int nameID);
        ///<summary>Use this method to get the value of a named integer property from the VFXEventAttribute.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the integer you specify. Returns <c>0</c> if <see cref="VFX.VFXEventAttribute.HasInt" /> returns <c>false</c>.</returns>
        [NativeName("GetValueFromScript<int>")] extern public int GetInt(int nameID);
        ///<summary>Use this method to get the value of a named unsigned integer property from the VFXEventAttribute.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the int you specify. Returns <c>0</c> if <see cref="VFX.VFXEventAttribute.HasUint" /> returns <c>false</c>.</returns>
        [NativeName("GetValueFromScript<UInt32>")] extern public uint GetUint(int nameID);
        ///<summary>Use this method to get the value of a named float property from the VFXEventAttribute.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the float you specify. Returns /0.0f/ if <see cref="VFX.VFXEventAttribute.HasFloat" /> returns <c>false</c>.</returns>
        [NativeName("GetValueFromScript<float>")] extern public float GetFloat(int nameID);
        ///<summary>Use this method to get the value of a named Vector2 property from the VFXEventAttribute.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Vector2 you specify. Returns /Vector2.zero/ if <see cref="VFX.VFXEventAttribute.HasVector2" /> returns <c>false</c>.</returns>
        [NativeName("GetValueFromScript<Vector2f>")] extern public Vector2 GetVector2(int nameID);
        ///<summary>Use this method to get the value of a named Vector3 property from the VFXEventAttribute.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Vector3 you specify. Returns /Vector3.zero/ if <see cref="VFX.VFXEventAttribute.HasVector3" /> returns <c>false</c>.</returns>
        [NativeName("GetValueFromScript<Vector3f>")] extern public Vector3 GetVector3(int nameID);
        ///<summary>Use this method to get the value of a named Vector4 property from the VFXEventAttribute.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Vector4 you specify. Returns /Vector4.zero/ if <see cref="VFX.VFXEventAttribute.HasVector4" /> returns <c>false</c>.</returns>
        [NativeName("GetValueFromScript<Vector4f>")] extern public Vector4 GetVector4(int nameID);
        ///<summary>Use this method to get the value of a named Matrix4x4 property from the VFXEventAttribute.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Matrix4x4 you specify. Returns /Matrix4x4.identity/ if <see cref="VFX.VFXEventAttribute.HasMatrix4x4" /> returns <c>false</c>.</returns>
        [NativeName("GetValueFromScript<Matrix4x4f>")] extern public Matrix4x4 GetMatrix4x4(int nameID);

        ///<summary>Use this method to check if the VFXEventAttribute stores a bool with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>True if the VFXEventAttribute stores a bool with that name. Otherwise, false.</returns>
        public bool HasBool(string name)
        {
            return HasBool(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to check if the VFXEventAttribute stores a integer with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>True if the VFXEventAttribute stores an integer with that name. Otherwise, false.</returns>
        public bool HasInt(string name)
        {
            return HasInt(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to check if the VFXEventAttribute stores a unsigned integer with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>True if the VFXEventAttribute stores an unsigned integer with that name. Otherwise, false.</returns>
        public bool HasUint(string name)
        {
            return HasUint(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to check if the VFXEventAttribute stores a float with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>True if the VFXEventAttribute stores a float with that name. Otherwise, false.</returns>
        public bool HasFloat(string name)
        {
            return HasFloat(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to check if the VFXEventAttribute stores a Vector2 with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>True if the VFXEventAttribute stores a Vector2 with that name. Otherwise, false.</returns>
        public bool HasVector2(string name)
        {
            return HasVector2(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to check if the VFXEventAttribute stores a Vector3 with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>True if the VFXEventAttribute stores a Vector3 with that name. Otherwise, false.</returns>
        public bool HasVector3(string name)
        {
            return HasVector3(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to check if the VFXEventAttribute stores a Vector4 with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>True if the VFXEventAttribute stores a Vector4 with that name. Otherwise, false.</returns>
        public bool HasVector4(string name)
        {
            return HasVector4(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to check if the VFXEventAttribute stores a Matrix4x4 with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>True if the VFXEventAttribute stores a Matrix4x4 with that name. Otherwise, false.</returns>
        public bool HasMatrix4x4(string name)
        {
            return HasMatrix4x4(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to set the value of a bool with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<param name="b">The new bool value.</param>
        public void SetBool(string name, bool b)
        {
            SetBool(Shader.PropertyToID(name), b);
        }

        ///<summary>Use this method to set the value of an integer with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<param name="i">The new integer value.</param>
        public void SetInt(string name, int i)
        {
            SetInt(Shader.PropertyToID(name), i);
        }

        ///<summary>Use this method to set the value of an unsigned integer with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<param name="i">The new unsigned integer value.</param>
        public void SetUint(string name, uint i)
        {
            SetUint(Shader.PropertyToID(name), i);
        }

        ///<summary>Use this method to set the value of a float with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<param name="f">The new float value.</param>
        public void SetFloat(string name, float f)
        {
            SetFloat(Shader.PropertyToID(name), f);
        }

        ///<summary>Use this method to set the value of a Vector2 with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<param name="v">The new Vector2 value.</param>
        public void SetVector2(string name, Vector2 v)
        {
            SetVector2(Shader.PropertyToID(name), v);
        }

        ///<summary>Use this method to set the value of a Vector3 with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<param name="v">The new Vector3 value.</param>
        public void SetVector3(string name, Vector3 v)
        {
            SetVector3(Shader.PropertyToID(name), v);
        }

        ///<summary>Use this method to set the value of a Vector4 with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<param name="v">The new Vector4 value.</param>
        public void SetVector4(string name, Vector4 v)
        {
            SetVector4(Shader.PropertyToID(name), v);
        }

        ///<summary>Use this method to set the value of a Matrix4x4 with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<param name="v">The new Matrix4x4 value.</param>
        public void SetMatrix4x4(string name, Matrix4x4 v)
        {
            SetMatrix4x4(Shader.PropertyToID(name), v);
        }

        ///<summary>Use this method to get the value of a named bool property from the VFXEventAttribute.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the bool you specify. Returns <c>false</c> if <see cref="VFX.VFXEventAttribute.HasBool" /> returns <c>false</c>.</returns>
        public bool GetBool(string name)
        {
            return GetBool(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to get the value of a named integer property from the VFXEventAttribute.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the integer you specify. Returns <c>0</c> if <see cref="VFX.VFXEventAttribute.HasInt" /> returns <c>false</c>.</returns>
        public int GetInt(string name)
        {
            return GetInt(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to get the value of a named unsigned integer property from the VFXEventAttribute.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the int you specify. Returns <c>0</c> if <see cref="VFX.VFXEventAttribute.HasUint" /> returns <c>false</c>.</returns>
        public uint GetUint(string name)
        {
            return GetUint(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to get the value of a named float property from the VFXEventAttribute.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the float you specify. Returns /0.0f/ if <see cref="VFX.VFXEventAttribute.HasFloat" /> returns <c>false</c>.</returns>
        public float GetFloat(string name)
        {
            return GetFloat(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to get the value of a named Vector2 property from the VFXEventAttribute.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the Vector2 you specify. Returns /Vector2.zero/ if <see cref="VFX.VFXEventAttribute.HasVector2" /> returns <c>false</c>.</returns>
        public Vector2 GetVector2(string name)
        {
            return GetVector2(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to get the value of a named Vector3 property from the VFXEventAttribute.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the Vector3 you specify. Returns /Vector3.zero/ if <see cref="VFX.VFXEventAttribute.HasVector3" /> returns <c>false</c>.</returns>
        public Vector3 GetVector3(string name)
        {
            return GetVector3(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to get the value of a named Vector4 property from the VFXEventAttribute.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the Vector4 you specify. Returns /Vector4.zero/ if <see cref="VFX.VFXEventAttribute.HasVector4" /> returns <c>false</c>.</returns>
        public Vector4 GetVector4(string name)
        {
            return GetVector4(Shader.PropertyToID(name));
        }

        ///<summary>Use this method to get the value of a named Matrix4x4 property from the VFXEventAttribute.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the Matrix4x4 you specify. Returns /Matrix4x4.identity/ if <see cref="VFX.VFXEventAttribute.HasMatrix4x4" /> returns <c>false</c>.</returns>
        public Matrix4x4 GetMatrix4x4(string name)
        {
            return GetMatrix4x4(Shader.PropertyToID(name));
        }

        ///<summary>Copies the values from a VFXEventAttribute to the one you call this function from.</summary>
        ///<remarks>This function doesn't add any value, it copies values from source event attribute to already existing properties.</remarks>
        ///<param name="eventAttibute">The source event attribute.</param>
        extern public void CopyValuesFrom([NotNull] VFXEventAttribute eventAttibute);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(VFXEventAttribute eventAttibute) => eventAttibute.m_Ptr;
            public static VFXEventAttribute ConvertToManaged(IntPtr ptr) => new VFXEventAttribute(ptr);
        }
    }
}
