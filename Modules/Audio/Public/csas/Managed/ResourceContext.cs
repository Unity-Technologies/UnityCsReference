// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio.Tests")]

namespace Unity.Experimental.Audio
{
    internal unsafe partial struct ResourceContext
    {
        public NativeArray<T> AllocateArray<T>(int length) where T : struct
        {
            var size = UnsafeUtility.SizeOf<T>() * length;

            var memory = Internal_AllocateArray(m_DSPNodePtr, size);
            var nBuffer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(memory, length, Allocator.Invalid);

            return nBuffer;
        }

        public void FreeArray<T>(ref NativeArray<T> array) where T : struct
        {
            var memory = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(array);
            Internal_FreeArray(m_DSPNodePtr, memory);

            array = new NativeArray<T>();
        }

        internal void* m_DSPNodePtr;
    }
}

