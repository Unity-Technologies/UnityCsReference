// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine.Windows
{
    public static partial class Input
    {
        public static void ForwardRawInput(IntPtr rawInputHeaderIndices, IntPtr rawInputDataIndices, uint indicesCount, IntPtr rawInputData, uint rawInputDataSize)
        {
            unsafe
            {
                ForwardRawInput((uint*)rawInputHeaderIndices, (uint*)rawInputDataIndices, indicesCount, (byte*)rawInputData, rawInputDataSize);
            }
        }

        public unsafe static void ForwardRawInput(uint* rawInputHeaderIndices, uint* rawInputDataIndices, uint indicesCount, byte* rawInputData, uint rawInputDataSize)
        {
            throw new NotSupportedException();
        }
    }
}
