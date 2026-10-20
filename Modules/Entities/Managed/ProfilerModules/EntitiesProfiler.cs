// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Entities
{
    // The recording half of the Entities profiler modules: the entity store and worlds report what they create here.
    // The package's EntitiesProfilerReporting initializes it, sends what it collects to the profiler once a frame, and
    // turns recording on and off with the profiler.
    static partial class EntitiesProfiler
    {
        sealed class SharedStaticData { internal static readonly SharedStatic<StaticData> Ref = SharedStatic<StaticData>.GetOrCreate<SharedStaticData>(); }

        public static Guid Guid => s_Data.Guid;

        static ref StaticData s_Data => ref SharedStaticData.Ref.Data;

        internal static ref StaticData Data => ref s_Data;

        internal static void InitializeRecording()
        {
            s_Data = new StaticData(16, 1024, 1024);
            StructuralChangesProfiler.Initialize();
        }

        internal static void ShutdownRecording()
        {
            StructuralChangesProfiler.Shutdown();
            s_Data.Dispose();
        }

        [ExcludeFromBurstCompatTesting("Takes managed World")]
        public static void OnWorldCreated(World world) => s_Data.AddWorld(world);

        public static void OnSystemCreated(SystemTypeIndex systemType, in SystemHandle systemHandle) => s_Data.AddSystem(systemType, in systemHandle);

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_PROFILER")]
        public static unsafe void ArchetypeAdded(Archetype* archetype) => s_Data.AddArchetype(archetype);

        static unsafe NativeArray<T> AsNativeArray<T>(this UnsafeList<T> list) where T : unmanaged
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(list.Ptr, list.Length, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempMemoryHandle());
            return array;
        }

        static FixedString128Bytes ToFixedString128(this string value)
        {
            return value.Substring(0, math.min(value.Length, FixedString128Bytes.UTF8MaxLengthInBytes));
        }
    }
}
