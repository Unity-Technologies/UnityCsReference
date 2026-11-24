// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace Unity.Entities
{
    [NativeHeader("Modules/Entities/Public/World.bindings.h")]
    public unsafe partial class World
    {
        [FreeFunction("ActiveWorldsStack::RegisterStack", IsThreadSafe = true)]
        static extern void RegisterActiveWorldsStack(void* stack);

        [FreeFunction("ActiveWorldsStack::CheckManagedLayout", IsThreadSafe = true)]
        static extern int CheckActiveWorldsStackLayout(int* values, int count);

        static SharedStatic<FixedList512Bytes<WorldUnmanaged>> CreateActiveWorldsStack()
        {
            var stack = SharedStatic<FixedList512Bytes<WorldUnmanaged>>.GetOrCreate<ActiveWorldsStackIdentifier>();
            RegisterActiveWorldsStack(stack.UnsafeDataPointer);
            return stack;
        }

        // Sends the values in the order ActiveWorldsStack::CheckManagedLayout compares them, and returns the
        // index of the first one that differs, or -1. NativeLayoutTests calls it.
        internal static int FindLayoutMismatch()
        {
            var stack = new FixedList512Bytes<WorldUnmanaged>();
            stack.Add(default);
            Span<int> values = stackalloc int[]
            {
                sizeof(FixedList512Bytes<WorldUnmanaged>),
                (int)((byte*)UnsafeUtility.AddressOf(ref stack.ElementAt(0)) - (byte*)UnsafeUtility.AddressOf(ref stack)),
                stack.Capacity,
                sizeof(WorldUnmanaged),
                WorldUnmanaged.GetImplOffset(),
            };

            fixed (int* pointer = values)
            {
                return CheckActiveWorldsStackLayout(pointer, values.Length);
            }
        }
    }
}
