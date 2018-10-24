// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Profiling
{
    public enum ProfilerArea
    {
        CPU,
        GPU,
        Rendering,
        Memory,
        Audio,
        Video,
        Physics,
        Physics2D,
        NetworkMessages,
        NetworkOperations,
        UI,
        UIDetails,
        GlobalIllumination,
    }


    [UsedByNativeCode]
    [MovedFrom("UnityEngine")]
    [NativeHeader("Runtime/Allocator/MemoryManager.h")]
    [NativeHeader("Runtime/Profiler/Profiler.h")]
    [NativeHeader("Runtime/Profiler/ScriptBindings/Profiler.bindings.h")]
    [NativeHeader("Runtime/ScriptingBackend/ScriptingApi.h")]
    [NativeHeader("Runtime/Utilities/MemoryUtilities.h")]
    public sealed class Profiler
    {
        // This class can't be explicitly created
        private Profiler() {}

        // *undocumented*
        public extern static bool supported
        {
            [NativeMethod(Name = "profiler_is_available", IsFreeFunction = true)]
            get;
        }

        // Sets profiler output file in built players.
        [StaticAccessor("ProfilerBindings", StaticAccessorType.DoubleColon)]
        public extern static string logFile
        {
            get;
            set;
        }

        // Sets profiler output file in built players.
        public extern static bool enableBinaryLog
        {
            [NativeMethod(Name = "ProfilerBindings::IsBinaryLogEnabled", IsFreeFunction = true)]
            get;
            [NativeMethod(Name = "ProfilerBindings::SetBinaryLogEnabled", IsFreeFunction = true)]
            set;
        }

        public extern static int maxUsedMemory
        {
            [NativeMethod(Name = "ProfilerBindings::GetMaxUsedMemory", IsFreeFunction = true)]
            get;
            [NativeMethod(Name = "ProfilerBindings::SetMaxUsedMemory", IsFreeFunction = true)]
            set;
        }

        // Enables the Profiler.
        public extern static bool enabled
        {
            [NativeConditional("ENABLE_PROFILER")]
            [NativeMethod(Name = "profiler_is_enabled", IsFreeFunction = true)]
            get;

            [NativeMethod(Name = "ProfilerBindings::SetProfilerEnabled", IsFreeFunction = true)]
            set;
        }

        [Conditional("ENABLE_PROFILER")]
        [FreeFunction("profiler_set_area_enabled")]
        public extern static void SetAreaEnabled(ProfilerArea area, bool enabled);

        public static int areaCount
        {
            get
            {
                return Enum.GetNames(typeof(ProfilerArea)).Length;
            }
        }


        [NativeConditional("ENABLE_PROFILER")]
        [FreeFunction("profiler_is_area_enabled")]
        public extern static bool GetAreaEnabled(ProfilerArea area);

        // Displays the recorded profiledata in the profiler.
        [Conditional("UNITY_EDITOR")]
        public static void AddFramesFromFile(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                Debug.LogError("AddFramesFromFile: Invalid or empty path");
                return;
            }

            AddFramesFromFile_Internal(file, true);
        }

        [NativeHeader("Modules/ProfilerEditor/Public/ProfilerSession.h")]
        [NativeConditional("ENABLE_PROFILER && UNITY_EDITOR")]
        [NativeMethod(Name = "LoadFromFile")]
        [StaticAccessor("profiling::GetProfilerSessionPtr()", StaticAccessorType.Arrow)]
        private extern static void AddFramesFromFile_Internal(string file, bool keepExistingFrames);

        [Conditional("ENABLE_PROFILER")]
        public static void BeginThreadProfiling(string threadGroupName, string threadName)
        {
            if (string.IsNullOrEmpty(threadGroupName))
                throw new ArgumentException("Argument should be a valid string", "threadGroupName");
            if (string.IsNullOrEmpty(threadName))
                throw new ArgumentException("Argument should be a valid string", "threadName");

            BeginThreadProfilingInternal(threadGroupName, threadName);
        }

        [NativeConditional("ENABLE_PROFILER")]
        [NativeMethod(Name = "ProfilerBindings::BeginThreadProfiling", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static void BeginThreadProfilingInternal(string threadGroupName, string threadName);

        [NativeConditional("ENABLE_PROFILER")]
        [NativeMethod(Name = "ProfilerBindings::EndThreadProfiling", IsFreeFunction = true, IsThreadSafe = true)]
        public extern static void EndThreadProfiling();

        // Begin profiling a piece of code with a custom label.
        // TODO: make obsolete
        //OBSOLETE warning Profiler.BeginSample method is deprecated. Please use faster CustomSampler.Begin method instead.
        [Conditional("ENABLE_PROFILER")]
        public static void BeginSample(string name)
        {
            BeginSampleImpl(name, null);
        }

        // Begin profiling a piece of code with a custom label.
        // TODO: make obsolete
        //OBSOLETE warning Profiler.BeginSample method is deprecated. Please use faster CustomSampler.Begin method instead.
        [Conditional("ENABLE_PROFILER")]
        public static void BeginSample(string name, Object targetObject)
        {
            BeginSampleImpl(name, targetObject);
        }

        [NativeMethod(Name = "ProfilerBindings::BeginSample", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static void BeginSampleImpl(string name, Object targetObject);

        // End profiling a piece of code with a custom label.
        // TODO: make obsolete
        //OBSOLETE warning Profiler.EndSample method is deprecated. Please use faster CustomSampler.End method instead.
        [Conditional("ENABLE_PROFILER")]
        [NativeMethod(Name = "ProfilerBindings::EndSample", IsFreeFunction = true, IsThreadSafe = true)]
        public extern static void EndSample();

        [Obsolete("maxNumberOfSamplesPerFrame has been depricated. Use maxUsedMemory instead")]
        public static int maxNumberOfSamplesPerFrame
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        [Obsolete("usedHeapSize has been deprecated since it is limited to 4GB. Please use usedHeapSizeLong instead.")]
        public static uint usedHeapSize
        {
            get { return (uint)usedHeapSizeLong; }
        }

        // Heap size used by the program
        public extern static long usedHeapSizeLong
        {
            [NativeMethod(Name = "GetUsedHeapSize", IsFreeFunction = true)]
            get;
        }

        // Returns the runtime memory usage of the resource.

        [Obsolete("GetRuntimeMemorySize has been deprecated since it is limited to 2GB. Please use GetRuntimeMemorySizeLong() instead.")]
        public static int GetRuntimeMemorySize(Object o)
        {
            return (int)GetRuntimeMemorySizeLong(o);
        }

        [NativeMethod(Name = "ProfilerBindings::GetRuntimeMemorySizeLong", IsFreeFunction = true)]
        [NativeConditional("ENABLE_PROFILER")]
        public extern static long GetRuntimeMemorySizeLong(Object o);

        [Obsolete("GetMonoHeapSize has been deprecated since it is limited to 4GB. Please use GetMonoHeapSizeLong() instead.")]
        public static uint GetMonoHeapSize()
        {
            return (uint)GetMonoHeapSizeLong();
        }

        // Returns the size of the mono heap
        [NativeMethod(Name = "scripting_gc_get_heap_size", IsFreeFunction = true)]
        public extern static long GetMonoHeapSizeLong();

        [Obsolete("GetMonoUsedSize has been deprecated since it is limited to 4GB. Please use GetMonoUsedSizeLong() instead.")]
        public static uint GetMonoUsedSize()
        {
            return (uint)GetMonoUsedSizeLong();
        }

        // Returns the used size from mono
        [NativeMethod(Name = "scripting_gc_get_used_size", IsFreeFunction = true)]
        public extern static long GetMonoUsedSizeLong();

        // Sets the size of the MainThread's StackAllocator which is used for temp allocs
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        public extern static bool SetTempAllocatorRequestedSize(uint size);

        // Gets the size of the MainThread's StackAllocator which is used for temp allocs
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        public extern static uint GetTempAllocatorSize();

        [Obsolete("GetTotalAllocatedMemory has been deprecated since it is limited to 4GB. Please use GetTotalAllocatedMemoryLong() instead.")]
        public static uint GetTotalAllocatedMemory()
        {
            return (uint)GetTotalAllocatedMemoryLong();
        }

        [NativeMethod(Name = "GetTotalAllocatedMemory")]
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        public extern static long GetTotalAllocatedMemoryLong();

        [Obsolete("GetTotalUnusedReservedMemory has been deprecated since it is limited to 4GB. Please use GetTotalUnusedReservedMemoryLong() instead.")]
        public static uint GetTotalUnusedReservedMemory()
        {
            return (uint)GetTotalUnusedReservedMemoryLong();
        }

        [NativeMethod(Name = "GetTotalUnusedReservedMemory")]
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        public extern static long GetTotalUnusedReservedMemoryLong();

        [Obsolete("GetTotalReservedMemory has been deprecated since it is limited to 4GB. Please use GetTotalReservedMemoryLong() instead.")]
        public static uint GetTotalReservedMemory()
        {
            return (uint)GetTotalReservedMemoryLong();
        }

        [NativeMethod(Name = "GetTotalReservedMemory")]
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_MEMORY_MANAGER")]
        public extern static long GetTotalReservedMemoryLong();

        [NativeMethod(Name = "GetRegisteredGFXDriverMemory")]
        [StaticAccessor("GetMemoryManager()", StaticAccessorType.Dot)]
        [NativeConditional("ENABLE_PROFILER")]
        public extern static long GetAllocatedMemoryForGraphicsDriver();
    }
}
