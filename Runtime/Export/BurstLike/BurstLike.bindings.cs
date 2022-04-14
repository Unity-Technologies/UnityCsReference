// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Scripting;
using Unity.Burst;
using Unity.Burst.LowLevel;

namespace Unity.Collections.LowLevel.Unsafe
{
    [NativeHeader("Runtime/Export/BurstLike/BurstLike.bindings.h")]
    [StaticAccessor("BurstLike", StaticAccessorType.DoubleColon)]
    internal static partial class BurstLike
    {
        [ThreadSafe(ThrowsException = false)]
        [BurstAuthorizedExternalMethod]
        internal static extern int NativeFunctionCall_Int_IntPtr_IntPtr(IntPtr function, IntPtr p0, IntPtr p1, out int error);

        // NOTE this is an inferior and internal-only placeholder for SharedStatic
        // We expect to remove the BurstLike functionality once burst can be used in the Unity Editor build pipeline.
        internal readonly unsafe struct SharedStatic<T> where T : unmanaged
        {
            private readonly void* _buffer;

            private SharedStatic(void* buffer) => _buffer = buffer;

            public ref T Data => ref UnsafeUtility.AsRef<T>(_buffer);

            public void* UnsafeDataPointer => _buffer;

            public static SharedStatic<T> GetOrCreate<TContext>(uint alignment = 0) =>
                new SharedStatic<T>(SharedStatic.GetOrCreateSharedStaticInternal(
                    BurstRuntime.GetHashCode64<TContext>(),
                    0,
                    (uint)UnsafeUtility.SizeOf<T>(),
                    alignment));

            public static SharedStatic<T> GetOrCreate<TContext, TSubContext>(uint alignment = 0) =>
                new SharedStatic<T>(SharedStatic.GetOrCreateSharedStaticInternal(
                    BurstRuntime.GetHashCode64<TContext>(),
                    BurstRuntime.GetHashCode64<TSubContext>(),
                    (uint)UnsafeUtility.SizeOf<T>(),
                    alignment));
        }

        internal static class SharedStatic
        {
            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private static void CheckSizeOf(uint sizeOf)
            {
                if (sizeOf == 0) throw new ArgumentException("sizeOf must be > 0", nameof(sizeOf));
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private static unsafe void CheckResult(void* result)
            {
                if (result == null)
                    throw new InvalidOperationException("Unable to create a SharedStatic for this key. This is most likely due to the size of the struct inside of the SharedStatic having changed or the same key being reused for differently sized values. To fix this the editor needs to be restarted.");
            }

            [RequiredMember]
            public static unsafe void* GetOrCreateSharedStaticInternal(long getHashCode64, long getSubHashCode64, uint sizeOf, uint alignment)
            {
                CheckSizeOf(sizeOf);
                var hash128 = new Hash128((ulong)getHashCode64, (ulong)getSubHashCode64);
                var result = BurstCompilerService.GetOrCreateSharedMemory(ref hash128, sizeOf, alignment == 0 ? 4 : alignment);
                CheckResult(result);
                return result;
            }
        }
    }

    // ** Must be in this namespace for burst intrinsic support **
    internal static class BurstRuntime
    {
        // ** Must be in this namespace and class **
        // This is considered an intrinsic in burst, as long as it is in this exact namespace and class name.
        // That makes it burst compilable even though the implementation uses typeof(T)
        public static long GetHashCode64<T>()
        {
            return HashCode64<T>.Value;
        }

        private struct HashCode64<T>
        {
            public static readonly long Value = HashStringWithFNV1A64(typeof(T).AssemblyQualifiedName);
        }

        // ** Must match implementation in Burst **
        internal static long HashStringWithFNV1A64(string text)
        {
            // Using http://www.isthe.com/chongo/tech/comp/fnv/index.html#FNV-1a
            // with basis and prime:
            const ulong offsetBasis = 14695981039346656037;
            const ulong prime = 1099511628211;

            ulong result = offsetBasis;

            foreach (var c in text)
            {
                result = prime * (result ^ (byte)(c & 255));
                result = prime * (result ^ (byte)(c >> 8));
            }

            return (long)result;
        }
    }
}
