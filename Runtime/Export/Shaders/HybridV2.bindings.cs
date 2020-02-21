// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Rendering.HybridV2
{
    // Keep in sync with the DOTSInstancingCbuffer struct in Shader.h
    [StructLayout(LayoutKind.Sequential)]
    public struct DOTSInstancingCbuffer
    {
        public int NameID;       // The FastPropertyName / Shader.PropertyToID of this cbuffer.
        public int CbufferIndex; // Index in the array returned by GetDOTSInstancingCbuffers() of the cbuffer that contains this constant
        public int SizeBytes;    // How many bytes must be allocated for the cbuffer. Can potentially be shorter than the cbuffer source declaration.
    }

    // Keep in sync with with DOTSInstancingPropertyType in Shader.h
    public enum DOTSInstancingPropertyType
    {
        Unknown,
        Float,
        Half,
        Int,
        Short,
        Uint,
        Bool,
        Struct,
    };

    // Keep in sync with the DOTSInstancingCbuffer struct in Shader.h
    [StructLayout(LayoutKind.Sequential)]
    public struct DOTSInstancingProperty
    {
        public int MetadataNameID;  // The FastPropertyName / Shader.PropertyToID of the uint constant that holds the metadata for this property
        public int ConstantNameID;  // The FastPropertyName / Shader.PropertyToID of the real material property
        public int CbufferIndex;    // Index in the array returned by GetDOTSInstancingCbuffers() of the cbuffer that contains the metadata constant
        public int MetadataOffset;  // Byte offset of the metadata uint in the corresponding DOTSInstancingCbuffer
        public int SizeBytes;       // How many bytes the actual value of the constant (i.e. not the metadata uint) takes. E.g. 16 for float4.
        // The base type of the constant (regardless of vector/matrix). Would be Float for float, float4 and float4x4.
        public DOTSInstancingPropertyType ConstantType;
        // If the constant is a vector, matrix, or array, gives the amount of elements on each row.
        // Will be 0 for Unknown, and 1 for scalars.
        public int Cols;
        // If the constant is a matrix, gives the amount of rows in the matrix.
        // Will be 0 for Unknown, and 1 otherwise.
        public int Rows;
    }

    public class HybridV2ShaderReflection
    {
        [FreeFunction("ShaderScripting::GetDOTSInstancingCbuffersPointer")]
        extern private static unsafe IntPtr GetDOTSInstancingCbuffersPointer([NotNull] Shader shader, ref int cbufferCount);
        [FreeFunction("ShaderScripting::GetDOTSInstancingPropertiesPointer")]
        extern private static unsafe IntPtr GetDOTSInstancingPropertiesPointer([NotNull] Shader shader, ref int propertyCount);
        [FreeFunction("Shader::GetDOTSReflectionVersionNumber")]
        extern public static unsafe uint GetDOTSReflectionVersionNumber();

        public static unsafe NativeArray<DOTSInstancingCbuffer> GetDOTSInstancingCbuffers(Shader shader)
        {
            if (shader == null)
                return new NativeArray<DOTSInstancingCbuffer>();

            int cbCount = 0;
            IntPtr p = GetDOTSInstancingCbuffersPointer(shader, ref cbCount);

            if (p == IntPtr.Zero)
                return new NativeArray<DOTSInstancingCbuffer>();

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<DOTSInstancingCbuffer>(
                (void *)p, cbCount, Allocator.Temp);

            AtomicSafetyHandle safety = AtomicSafetyHandle.GetTempMemoryHandle();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);

            return array;
        }

        public static unsafe NativeArray<DOTSInstancingProperty> GetDOTSInstancingProperties(Shader shader)
        {
            if (shader == null)
                return new NativeArray<DOTSInstancingProperty>();

            int propertyCount = 0;
            IntPtr p = GetDOTSInstancingPropertiesPointer(shader, ref propertyCount);

            if (p == IntPtr.Zero)
                return new NativeArray<DOTSInstancingProperty>();

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<DOTSInstancingProperty>(
                (void *)p, propertyCount, Allocator.Temp);

            AtomicSafetyHandle safety = AtomicSafetyHandle.GetTempMemoryHandle();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);

            return array;
        }
    }
}
