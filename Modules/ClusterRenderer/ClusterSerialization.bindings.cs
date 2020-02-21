// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [Internal.ExcludeFromDocs]
    [NativeHeader("Modules/ClusterRenderer/ClusterSerialization.h")]
    public static class ClusterSerialization
    {
        public static int SaveTimeManagerState(NativeArray<byte> buffer)
        {
            unsafe
            {
                return SaveTimeManagerStateInternal((byte*)buffer.GetUnsafePtr(), buffer.Length);
            }
        }

        public static bool RestoreTimeManagerState(NativeArray<byte> buffer)
        {
            unsafe
            {
                return RestoreTimeManagerStateInternal((byte*)buffer.GetUnsafePtr(), buffer.Length);
            }
        }

        public static int SaveInputManagerState(NativeArray<byte> buffer)
        {
            unsafe
            {
                return SaveInputManagerStateInternal((byte*)buffer.GetUnsafePtr(), buffer.Length);
            }
        }

        public static bool RestoreInputManagerState(NativeArray<byte> buffer)
        {
            unsafe
            {
                return RestoreInputManagerStateInternal((byte*)buffer.GetUnsafePtr(), buffer.Length);
            }
        }

        public static int SaveClusterInputState(NativeArray<byte> buffer)
        {
            unsafe
            {
                return SaveClusterInputStateInternal((byte*)buffer.GetUnsafePtr(), buffer.Length);
            }
        }

        public static bool RestoreClusterInputState(NativeArray<byte> buffer)
        {
            unsafe
            {
                return RestoreClusterInputStateInternal((byte*)buffer.GetUnsafePtr(), buffer.Length);
            }
        }

        [FreeFunction("ClusterSerialization::SaveTimeManagerState", ThrowsException = true)]
        private static extern unsafe int SaveTimeManagerStateInternal(void* intBuffer, int bufferSize);

        [FreeFunction("ClusterSerialization::RestoreTimeManagerState", ThrowsException = true)]
        private static extern unsafe bool RestoreTimeManagerStateInternal(void* buffer, int bufferSize);

        [FreeFunction("ClusterSerialization::SaveInputManagerState", ThrowsException = true)]
        private static extern unsafe int SaveInputManagerStateInternal(void* intBuffer, int bufferSize);

        [FreeFunction("ClusterSerialization::RestoreInputManagerState", ThrowsException = true)]
        private static extern unsafe bool RestoreInputManagerStateInternal(void* buffer, int bufferSize);

        [FreeFunction("ClusterSerialization::SaveClusterInputState", ThrowsException = true)]
        private static extern unsafe int SaveClusterInputStateInternal(void* intBuffer, int bufferSize);

        [FreeFunction("ClusterSerialization::RestoreClusterInputState", ThrowsException = true)]
        private static extern unsafe bool RestoreClusterInputStateInternal(void* buffer, int bufferSize);
    }
}
