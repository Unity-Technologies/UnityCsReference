// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Profiling;
using UnityEngine.Scripting;


namespace UnityEngine.AdaptivePerformance
{
    [NativeHeader("Modules/AdaptivePerformance/Profiler/AdaptivePerformanceProfilerNative.bindings.h")]
    internal static partial class AdaptivePerformanceProfilerNative
    {
        internal static unsafe void EmitFrameMetaData<T>(Guid id, int tag, List<T> data) where T : struct
        {
            if (!Profiler.enabled) return;
            if (data == null)
                throw new ArgumentNullException("data");

            var elementType = typeof(T);
            if (!UnsafeUtility.IsBlittable(typeof(T)))
                throw new ArgumentException(string.Format("{0} type must be blittable", elementType));

            EmitGlobalMetaData_Span(&id, 16, tag, UnsafeUtility.GetByteSpanFromList(data), data.Count, UnsafeUtility.SizeOf(elementType), true);
        }

        [NativeMethod(Name = "ProfilerBindings::Internal_EmitGlobalMetaData_Span", IsFreeFunction = true, IsThreadSafe = true)]
        internal static extern unsafe void EmitGlobalMetaData_Span(void* id, int idLen, int tag, Span<byte> data, int count, int elementSize, bool frameData);
    }
}
