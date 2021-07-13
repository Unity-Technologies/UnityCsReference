// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using System;
using System.Runtime.CompilerServices;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;

namespace Unity.Profiling
{
    internal struct ProfilerMarkerWithStringData
    {
        const MethodImplOptions AggressiveInlining = (MethodImplOptions)256; // MethodImplOptions.AggressiveInlining introduced in .net 4.x
        private IntPtr _marker;
        public static ProfilerMarkerWithStringData Create(string name, string parameterName)
        {
            var marker = ProfilerUnsafeUtility.CreateMarker(name, ProfilerUnsafeUtility.CategoryOther, MarkerFlags.Default, 1);
            ProfilerUnsafeUtility.SetMarkerMetadata(marker, 0, parameterName, (byte)ProfilerMarkerDataType.String16, 0);
            return new ProfilerMarkerWithStringData
            {
                _marker = marker
            };
        }

        public struct AutoScope : IDisposable
        {
            private IntPtr _marker;

            [MethodImpl(AggressiveInlining)]
            internal AutoScope(IntPtr marker)
            {
                _marker = marker;
            }

            [MethodImpl(AggressiveInlining)]
            [Pure]
            public void Dispose()
            {
                if (_marker != IntPtr.Zero)
                {
                    ProfilerUnsafeUtility.EndSample(_marker);
                }
            }
        }

        [MethodImpl(AggressiveInlining)]
        [Pure]
        public AutoScope Auto(bool enabled, Func<string> parameterValue)
        {
            if (enabled)
            {
                return Auto(parameterValue());
            }
            return new AutoScope(IntPtr.Zero);
        }

        [MethodImpl(AggressiveInlining)]
        [Pure]
        public AutoScope Auto(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            unsafe
            {
                fixed(char* charPointer = value)
                {
                    ProfilerMarkerData data = new ProfilerMarkerData
                    {
                        Type = (byte)ProfilerMarkerDataType.String16,
                        Size = (uint)value.Length * 2 + 2
                    };
                    data.Ptr = charPointer;
                    ProfilerUnsafeUtility.BeginSampleWithMetadata(_marker, 1, &data);
                }
            }
            return new AutoScope(_marker);
        }
    }
}
