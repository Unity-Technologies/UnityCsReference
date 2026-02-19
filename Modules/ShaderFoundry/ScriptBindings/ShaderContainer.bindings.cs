// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderContainer.h")]
    [StructLayout(LayoutKind.Sequential)]
    [FoundryAPI]
    internal sealed partial class ShaderContainer : IDisposable
    {
        private IntPtr m_Ptr;
        private List<ShaderContainer> m_Dependencies = new List<ShaderContainer>();
        // Denotes that the container's IntPtr was allocated by managed code and so de-allocation should be triggered
        // by managed as well.
        private readonly bool m_IsOwnedByManaged = false;

        // call native function to implement "constructor"
        public ShaderContainer() : this(Internal_Create())
        {
            m_IsOwnedByManaged = true;
        }

        private ShaderContainer(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        ~ShaderContainer()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero && m_IsOwnedByManaged)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        [NativeMethod(IsThreadSafe = true)] private extern static IntPtr Internal_Create();
        [NativeMethod(IsThreadSafe = true)] private static extern void Internal_Destroy(IntPtr ptr);

        [NativeMethod(Name = "AddDependency", IsThreadSafe = true)]
        private extern void Internal_AddDependency(ShaderContainer other);

        internal void AddDependency(ShaderContainer other)
        {
            if (m_Dependencies.Contains(other))
                return;

            m_Dependencies.Add(other);
            Internal_AddDependency(other);
        }

        internal static class BindingsMarshaller
        {
            // obj is checked for null before this call
            // Only required if the type is passed into native code
            public static IntPtr ConvertToNative(ShaderContainer obj) => obj.m_Ptr;

            // If the natives return NULL the bindings will return null and this method will not be called
            // Only required if the type is returned from native code
            public static ShaderContainer ConvertToManaged(IntPtr ptr) => new ShaderContainer(ptr);
        }

        // native member functions
        [NativeMethod(IsThreadSafe = true)] internal extern FoundryHandle AddString(string s);
        [NativeMethod(IsThreadSafe = true)] internal extern string GetString(FoundryHandle stringHandle);

        [NativeMethod(IsThreadSafe = true)] extern FoundryHandle AllocateStaticSized(DataType dataType, ulong sizeInBytes);
        [NativeMethod(IsThreadSafe = true)] unsafe extern void* GetStaticSizedPointer(FoundryHandle handle, DataType expectedDataType);
        [NativeMethod(IsThreadSafe = true)] unsafe extern void* GetStaticSizedPointerConst(FoundryHandle handle, DataType expectedDataType);
        [NativeMethod(IsThreadSafe = true)] internal extern DataType GetDataTypeFromHandle(FoundryHandle handle);

        internal FoundryHandle Create<T>() where T : struct, IInternalType<T>
        {
            // Strings are special and do not work in this code path. We can fix this later if we want with a runtime check.
            Debug.Assert(typeof(T) != typeof(StringLiteral));
            Debug.Assert(typeof(T) != typeof(ListType));
            return AllocateStaticSized(InternalTypeStatic<T>.dataType, (ulong)InternalTypeStatic<T>.internalTypeSizeInBytes);
        }

        internal T Get<T>(FoundryHandle handle) where T : unmanaged, IInternalType<T>
        {
            // Strings are special and do not work in this code path. We can fix this later if we want with a runtime check.
            Debug.Assert(typeof(T) != typeof(StringLiteral));
            Debug.Assert(typeof(T) != typeof(ListType));
            unsafe
            {
                void* pointer = GetStaticSizedPointerConst(handle, InternalTypeStatic<T>.dataType);
                if (pointer != null)
                {
                    T* typedPointer = (T*)pointer;
                    return *typedPointer;
                }
            }
            return InternalTypeStatic<T>.Invalid;
        }

        internal static void Get<T>(ShaderContainer container, FoundryHandle handle, out T result) where T : unmanaged, IInternalType<T>
        {
            result = container?.Get<T>(handle) ?? InternalTypeStatic<T>.Invalid;
        }

        internal T Get<T>(IPublicType publicType) where T : struct, IPublicType<T>
        {
            var handle = publicType.Handle;
            DataType dataType = GetDataTypeFromHandle(handle);
            if (dataType == PublicTypeStatic<T>.dataType)
            {
                return PublicTypeStatic<T>.ConstructFromHandle(this, handle);
            }
            return PublicTypeStatic<T>.Invalid;
        }

        internal bool Set<T>(FoundryHandle handle, T data) where T : unmanaged, IInternalType<T>
        {
            // Strings are special and do not work in this code path. We can fix this later if we want with a runtime check.
            Debug.Assert(typeof(T) != typeof(StringLiteral));
            Debug.Assert(typeof(T) != typeof(ListType));
            unsafe
            {
                void* pointer = GetStaticSizedPointer(handle, InternalTypeStatic<T>.dataType);
                if (pointer != null)
                {
                    T* typedPointer = (T*)pointer;
                    *typedPointer = data;
                    return true;
                }
            }
            return false;
        }

        internal IPublicType ConstructTypeFromHandle(FoundryHandle handle)
        {
            var dataType = GetDataTypeFromHandle(handle);
            var info = DataTypeStatic.GetInfoFromDataType(dataType);
            return info.ConstructFromHandle(this, handle);
        }

        internal FoundryHandle Add<T>(T data) where T : unmanaged, IInternalType<T>
        {
            // Strings are special and do not work in this code path. We can fix this later if we want with a runtime check.
            Debug.Assert(typeof(T) != typeof(StringLiteral));
            Debug.Assert(typeof(T) != typeof(ListType));
            var handle = Create<T>();
            Set<T>(handle, data);
            return handle;
        }

        [NativeMethod(Name = "CreateArray<ShaderFoundry::FoundryHandle>", IsThreadSafe = true)]
        internal extern FoundryHandle CreateArray(ulong size);
        [NativeMethod(IsThreadSafe = true)]
        internal extern ulong GetArraySize(FoundryHandle arrayHandle);
        [NativeMethod(Name = "GetArrayElement<ShaderFoundry::FoundryHandle>", IsThreadSafe = true)]
        internal extern FoundryHandle GetArrayElement(FoundryHandle arrayHandle, ulong elementIndex);
        [NativeMethod(Name = "SetArrayElement<ShaderFoundry::FoundryHandle>", IsThreadSafe = true)]
        internal extern void SetArrayElement(FoundryHandle arrayHandle, ulong elementIndex, FoundryHandle elementHandle);

        [NativeMethod(IsThreadSafe = true)] internal extern FoundryHandle GetTypeByName(string typeName);

        [NativeMethod(IsThreadSafe = true)] internal extern FoundryHandle[] GetSymbolsFromTree(DataType dataType);
        [NativeMethod(IsThreadSafe = true)] internal extern FoundryHandle[] GetShaderTypesFromTree(ShaderTypeInternal.Kind kind);
        // Used for testing only
        [NativeMethod(IsThreadSafe = true)] internal extern void AddSymbolToTree(FoundryHandle scopeNameHandle, FoundryHandle symbolHandle);
        IEnumerable<T> EnumerateSymbols<T>(FoundryHandle[] handles) where T : struct, IPublicType<T>
        {
            var symbols = new List<T>(handles.Length);
            foreach (var handle in handles)
                symbols.Add(PublicTypeStatic<T>.ConstructFromHandle(this, handle));
            return symbols;
        }
        IEnumerable<T> GetSymbols<T>() where T : struct, IPublicType<T>
        {
            FoundryHandle[] handles = GetSymbolsFromTree(PublicTypeStatic<T>.dataType);
            return EnumerateSymbols<T>(handles);
        }
        IEnumerable<ShaderType> GetShaderTypes(ShaderTypeInternal.Kind kind)
        {
            FoundryHandle[] handles = GetShaderTypesFromTree(kind);
            return EnumerateSymbols<ShaderType>(handles);
        }

        // Symbol discovery: The only symbols directly exposed by the symbol tree are those allocated in SA's
        // symbol allocator pass.
        public IEnumerable<Block> GetLocalBlocks() => GetSymbols<Block>();
        public IEnumerable<BlockSequence> GetLocalBlockSequences() => GetSymbols<BlockSequence>();
        public IEnumerable<BlockShaderInterface> GetLocalBlockShaderInterfaces() => GetSymbols<BlockShaderInterface>();
        public IEnumerable<BlockShader> GetLocalBlockShaders() => GetSymbols<BlockShader>();
        public IEnumerable<CustomizationPoint> GetLocalCustomizationPoints() => GetSymbols<CustomizationPoint>();
        public IEnumerable<ShaderType> GetLocalResources() => GetShaderTypes(ShaderTypeInternal.Kind.Resource);
        public IEnumerable<ShaderType> GetLocalStructs() => GetShaderTypes(ShaderTypeInternal.Kind.Struct);
        public IEnumerable<Template> GetLocalTemplates() => GetSymbols<Template>();
        public IEnumerable<CustomAttributeDefinition> GetLocalCustomAttributeDefinitions() => GetSymbols<CustomAttributeDefinition>();

        public ShaderType GetType(string name) => new ShaderType(this, GetTypeByName(name));

        public extern bool IsReadOnly { [NativeMethod(Name = "IsReadOnly", IsThreadSafe = true)] get; }
        [NativeMethod(IsThreadSafe = true)] public extern void MakeReadOnly();

        // TODO @ SHADERS SHADERS-253: Remove once symbol write API is gone
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal extern void ConstructTypedAttributeManaged(FoundryHandle attributeHandle);

        // cached common types
        ShaderType m_Void;

        ShaderType m_Bool;
        ShaderType m_Bool2;
        ShaderType m_Bool3;
        ShaderType m_Bool4;
        ShaderType m_Int;
        ShaderType m_Int2;
        ShaderType m_Int3;
        ShaderType m_Int4;
        ShaderType m_UInt;
        ShaderType m_UInt2;
        ShaderType m_UInt3;
        ShaderType m_UInt4;
        ShaderType m_Half;
        ShaderType m_Half2;
        ShaderType m_Half3;
        ShaderType m_Half4;
        ShaderType m_Float;
        ShaderType m_Float2;
        ShaderType m_Float3;
        ShaderType m_Float4;

        ShaderType m_Float2x2;
        ShaderType m_Float3x3;
        ShaderType m_Float4x4;

        ShaderType m_Texture1DFloat4;
        ShaderType m_Texture2DFloat4;
        ShaderType m_Texture3DFloat4;
        ShaderType m_TextureCubeFloat4;
        ShaderType m_Texture2DMS4Float4;

        ShaderType m_Texture1DHalf4;
        ShaderType m_Texture2DHalf4;
        ShaderType m_Texture3DHalf4;
        ShaderType m_TextureCubeHalf4;
        ShaderType m_Texture2DMS4Half4;

        ShaderType m_Texture1DArrayFloat4;
        ShaderType m_Texture2DArrayFloat4;
        ShaderType m_TextureCubeArrayFloat4;
        ShaderType m_Texture2DMS4ArrayFloat4;

        ShaderType m_Texture1DArrayHalf4;
        ShaderType m_Texture2DArrayHalf4;
        ShaderType m_TextureCubeArrayHalf4;
        ShaderType m_Texture2DMS4ArrayHalf4;

        ShaderType m_SamplerState;

        public ShaderType Void => m_Void.IsValid ? m_Void : (m_Void = GetType("void"));

        // Scalars and vectors
        public ShaderType Bool => m_Bool.IsValid ? m_Bool : (m_Bool = GetType("bool"));
        public ShaderType Bool2 => m_Bool2.IsValid ? m_Bool2 : (m_Bool2 = GetType("bool2"));
        public ShaderType Bool3 => m_Bool3.IsValid ? m_Bool3 : (m_Bool3 = GetType("bool3"));
        public ShaderType Bool4 => m_Bool4.IsValid ? m_Bool4 : (m_Bool4 = GetType("bool4"));

        public ShaderType Int => m_Int.IsValid ? m_Int : (m_Int = GetType("int"));
        public ShaderType Int2 => m_Int2.IsValid ? m_Int2 : (m_Int2 = GetType("int2"));
        public ShaderType Int3 => m_Int3.IsValid ? m_Int3 : (m_Int3 = GetType("int3"));
        public ShaderType Int4 => m_Int4.IsValid ? m_Int4 : (m_Int4 = GetType("int4"));

        public ShaderType UInt => m_UInt.IsValid ? m_UInt : (m_UInt = GetType("uint"));
        public ShaderType UInt2 => m_UInt2.IsValid ? m_UInt2 : (m_UInt2 = GetType("uint2"));
        public ShaderType UInt3 => m_UInt3.IsValid ? m_UInt3 : (m_UInt3 = GetType("uint3"));
        public ShaderType UInt4 => m_UInt4.IsValid ? m_UInt4 : (m_UInt4 = GetType("uint4"));

        public ShaderType Half => m_Half.IsValid ? m_Half : (m_Half = GetType("half"));
        public ShaderType Half2 => m_Half2.IsValid ? m_Half2 : (m_Half2 = GetType("half2"));
        public ShaderType Half3 => m_Half3.IsValid ? m_Half3 : (m_Half3 = GetType("half3"));
        public ShaderType Half4 => m_Half4.IsValid ? m_Half4 : (m_Half4 = GetType("half4"));

        public ShaderType Float => m_Float.IsValid ? m_Float : (m_Float = GetType("float"));
        public ShaderType Float2 => m_Float2.IsValid ? m_Float2 : (m_Float2 = GetType("float2"));
        public ShaderType Float3 => m_Float3.IsValid ? m_Float3 : (m_Float3 = GetType("float3"));
        public ShaderType Float4 => m_Float4.IsValid ? m_Float4 : (m_Float4 = GetType("float4"));

        // Matrices
        public ShaderType Float2x2 => m_Float2x2.IsValid ? m_Float2x2 : (m_Float2x2 = GetType("float2x2"));
        public ShaderType Float3x3 => m_Float3x3.IsValid ? m_Float3x3 : (m_Float3x3 = GetType("float3x3"));
        public ShaderType Float4x4 => m_Float4x4.IsValid ? m_Float4x4 : (m_Float4x4 = GetType("float4x4"));

        // Textures
        // N.B. for MSAA textures we chose 4x as the most commonly used/supported format here.
        // The user can still manually construct a type with a different number of samples.
        public ShaderType Texture1DFloat4 => m_Texture1DFloat4.IsValid ? m_Texture1DFloat4 : (m_Texture1DFloat4 = GetType("Texture1D<float4>"));
        public ShaderType Texture2DFloat4 => m_Texture2DFloat4.IsValid ? m_Texture2DFloat4 : (m_Texture2DFloat4 = GetType("Texture2D<float4>"));
        public ShaderType Texture3DFloat4 => m_Texture3DFloat4.IsValid ? m_Texture3DFloat4 : (m_Texture3DFloat4 = GetType("Texture3D<float4>"));
        public ShaderType TextureCubeFloat4 => m_TextureCubeFloat4.IsValid ? m_TextureCubeFloat4 : (m_TextureCubeFloat4 = GetType("TextureCube<float4>"));
        public ShaderType Texture2DMS4Float4 => m_Texture2DMS4Float4.IsValid ? m_Texture2DMS4Float4 : (m_Texture2DMS4Float4 = GetType("Texture2DMS<float4, 4>"));

        public ShaderType Texture1DHalf4 => m_Texture1DHalf4.IsValid ? m_Texture1DHalf4 : (m_Texture1DHalf4 = GetType("Texture1D<half4>"));
        public ShaderType Texture2DHalf4 => m_Texture2DHalf4.IsValid ? m_Texture2DHalf4 : (m_Texture2DHalf4 = GetType("Texture2D<half4>"));
        public ShaderType Texture3DHalf4 => m_Texture3DHalf4.IsValid ? m_Texture3DHalf4 : (m_Texture3DHalf4 = GetType("Texture3D<half4>"));
        public ShaderType TextureCubeHalf4 => m_TextureCubeHalf4.IsValid ? m_TextureCubeHalf4 : (m_TextureCubeHalf4 = GetType("TextureCube<half4>"));
        public ShaderType Texture2DMS4Half4 => m_Texture2DMS4Half4.IsValid ? m_Texture2DMS4Half4 : (m_Texture2DMS4Half4 = GetType("Texture2DMS<half4, 4>"));

        public ShaderType Texture1DArrayFloat4 => m_Texture1DArrayFloat4.IsValid ? m_Texture1DArrayFloat4 : (m_Texture1DArrayFloat4 = GetType("Texture1DArray<float4>"));
        public ShaderType Texture2DArrayFloat4 => m_Texture2DArrayFloat4.IsValid ? m_Texture2DArrayFloat4 : (m_Texture2DArrayFloat4 = GetType("Texture2DArray<float4>"));
        public ShaderType TextureCubeArrayFloat4 => m_TextureCubeArrayFloat4.IsValid ? m_TextureCubeArrayFloat4 : (m_TextureCubeArrayFloat4 = GetType("TextureCubeArray<float4>"));
        public ShaderType Texture2DMS4ArrayFloat4 => m_Texture2DMS4ArrayFloat4.IsValid ? m_Texture2DMS4ArrayFloat4 : (m_Texture2DMS4ArrayFloat4 = GetType("Texture2DMSArray<float4, 4>"));

        public ShaderType Texture1DArrayHalf4 => m_Texture1DArrayHalf4.IsValid ? m_Texture1DArrayHalf4 : (m_Texture1DArrayHalf4 = GetType("Texture1DArray<half4>"));
        public ShaderType Texture2DArrayHalf4 => m_Texture2DArrayHalf4.IsValid ? m_Texture2DArrayHalf4 : (m_Texture2DArrayHalf4 = GetType("Texture2DArray<half4>"));
        public ShaderType TextureCubeArrayHalf4 => m_TextureCubeArrayHalf4.IsValid ? m_TextureCubeArrayHalf4 : (m_TextureCubeArrayHalf4 = GetType("TextureCubeArray<half4>"));
        public ShaderType Texture2DMS4ArrayHalf4 => m_Texture2DMS4ArrayHalf4.IsValid ? m_Texture2DMS4ArrayHalf4 : (m_Texture2DMS4ArrayHalf4 = GetType("Texture2DMSArray<half4, 4>"));

        public ShaderType SamplerState => m_SamplerState.IsValid ? m_SamplerState : (m_SamplerState = GetType("SamplerState"));
    }
}
