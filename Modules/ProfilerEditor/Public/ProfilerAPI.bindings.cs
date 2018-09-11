// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
namespace UnityEditorInternal
{
    //@TODO: This should be renamed to LoadedObjectMemoryType like on C++. But should be done on the memory profiler branch.
    public enum MemoryInfoGCReason
    {
        SceneObject = 0,
        BuiltinResource = 1,
        MarkedDontSave = 2,
        AssetMarkedDirtyInEditor = 3,

        SceneAssetReferencedByNativeCodeOnly = 5,
        SceneAssetReferenced = 6,

        AssetReferencedByNativeCodeOnly = 8,
        AssetReferenced = 9,

        NotApplicable = 10
    }

    public enum ProfilerMemoryRecordMode
    {
        None = 0,
        ManagedAllocations,
        AllAllocationsFast,
        AllAllocationsFull
    }

    [Flags]
    public enum InstrumentedAssemblyTypes
    {
        None = 0,
        System = 1,
        Unity = 2,
        Plugins = 4,
        Script = 8,

        All = 0x7FFFFFFF
    }

    public enum ProfilerMemoryView
    {
        Simple = 0,
        Detailed = 1
    }

    public enum ProfilerAudioView
    {
        [Obsolete("This has been made obsolete. Audio stats are now shown on every subpane.", true)] Stats = 0,
        Channels = 1,
        Groups = 2,
        ChannelsAndGroups = 3,
        DSPGraph = 4,
        Clips = 5
    }

    public enum ProfilerCaptureFlags
    {
        None = 0,
        Channels = 1,
        DSPNodes = 2,
        Clips = 4,
        All = 7
    }


    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [System.Serializable]
    public struct EventMarker
    {
        public int objectInstanceId;
        public int nameOffset;
        public int frame;
    }

    [NativeHeader("Modules/ProfilerEditor/Public/ProfilerSession.h")]
    [NativeHeader("Modules/Profiler/Instrumentation/InstrumentationProfiler.h")]
    [NativeHeader("Modules/ProfilerEditor/ProfilerHistory/ProfilerProperty.h")]
    [NativeHeader("Modules/ProfilerEditor/Public/EditorProfilerConnection.h")]
    [NativeHeader("Modules/Profiler/Runtime/CollectProfilerStats.h")]
    [NativeHeader("Runtime/Utilities/MemoryUtilities.h")]
    public static partial class ProfilerDriver
    {
        // The following is a better solution for getting the constant rather than hard coding it
        // Unfortunately, this will not work with our test framework due to it only pulling managed
        // symbols and not native, which causes a runtime exception when the property is accessed
        // [NativeProperty("\"PLAYER_DIRECTCONNECT_PORT\"", true, TargetType.Field, IsThreadSafe = true)]
        // Must stay consistent with PLAYER_DIRECTCONNECT_PORT in GeneralConnection.h

        static public string directConnectionPort = "34999";

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        [NativeMethod("CleanupFrameHistory")]
        static public extern void ClearAllFrames();

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern int GetNextFrameIndex(int frame);

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern int GetPreviousFrameIndex(int frame);

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern int firstFrameIndex {[NativeMethod("GetFirstFrameIndex")] get; }

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern int lastFrameIndex {[NativeMethod("GetLastFrameIndex")] get; }

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern int maxHistoryLength {[NativeMethod("GetMaxFrameHistoryLength")] get; }

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern string selectedPropertyPath
        {
            [NativeMethod("GetSelectedPropertyPath")]
            get;
            [NativeMethod("SetSelectedPropertyPath")]
            set;
        }

        [NativeMethod("SetProfileArea")]
        [StaticAccessor("profiling::GetProfilerSessionPtr()", StaticAccessorType.Arrow)]
        static public extern void SetAreaEnabled(ProfilerArea area, bool enabled);

        [NativeMethod("IsProfilingArea")]
        [StaticAccessor("profiling::GetProfilerSessionPtr()", StaticAccessorType.Arrow)]
        static public extern bool IsAreaEnabled(ProfilerArea area);

        [StaticAccessor("profiling::GetProfilerSessionPtr()", StaticAccessorType.Arrow)]
        static public extern bool enabled
        {
            [NativeMethod("IsProfilingEnabled")]
            get;
            [NativeMethod("SetProfilingEnabled")]
            set;
        }

        static public bool profileGPU
        {
            get
            {
                return IsAreaEnabled(ProfilerArea.GPU);
            }
            set
            {
                SetAreaEnabled(ProfilerArea.GPU, value);
            }
        }

        [StaticAccessor("profiling::GetProfilerSessionPtr()", StaticAccessorType.Arrow)]
        static public extern bool profileEditor
        {
            [NativeMethod("IsEditorProfilingEnabled")]
            get;
            [NativeMethod("SetEditorProfilingEnabled")]
            set;
        }

        [StaticAccessor("profiling::GetProfilerSessionPtr()", StaticAccessorType.Arrow)]
        static public extern bool deepProfiling
        {
            [NativeMethod("IsDeepProfilingEnabled")]
            get;
            [NativeMethod("SetDeepProfilingEnabled")]
            set;
        }

        static public extern ProfilerMemoryRecordMode memoryRecordMode
        {
            [FreeFunction("profiler_get_memory_record_mode")]
            get;
            [FreeFunction("profiler_set_memory_record_mode")]
            set;
        }

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern string GetFormattedStatisticsValue(int frame, int identifier);

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern int GetUISystemEventMarkersCount(int firstFrame, int frameCount);

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern void GetUISystemEventMarkersBatch(int firstFrame, int frameCount, [Out] EventMarker[] buffer, [Out] string[] names);


        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        [NativeMethod("GetStatisticsValuesBatch")]
        static public extern void GetStatisticsValues(int identifier, int firstFrame, float scale, [Out] float[] buffer, out float maxValue);

        [Obsolete("ResetHistory is deprecated, use ClearAllFrames instead.")]
        static public void ResetHistory()
        {
            ClearAllFrames();
        }

        [NativeMethod("SaveToFile")]
        [StaticAccessor("profiling::GetProfilerSessionPtr()", StaticAccessorType.Arrow)]

        static public extern void SaveProfile(string filename);

        [NativeMethod("LoadFromFile")]
        [StaticAccessor("profiling::GetProfilerSessionPtr()", StaticAccessorType.Arrow)]
        static public extern bool LoadProfile(string filename, bool keepExistingData);

        [NativeMethod("GetAllStatisticsProperties")]
        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern string[] GetAllStatisticsProperties();

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern string[] GetGraphStatisticsPropertiesForArea(ProfilerArea area);

        [Obsolete("Use GetStatisticsIdentifierForArea that takes ProfilerArea as first argument", false)]
        static public int GetStatisticsIdentifier(string propertyName)
        {
            return GetStatisticsIdentifierForArea((ProfilerArea)Profiler.areaCount, propertyName);
        }

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern void GetStatisticsAvailable(ProfilerArea profilerArea, int firstFrame, [Out] bool[] buffer);

        [NativeMethod("GetStatisticsIdentifier")]
        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        static public extern int GetStatisticsIdentifierForArea(ProfilerArea profilerArea, string propertyName);

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        [NativeMethod("GetConnectionIdentification")]
        static public extern string GetConnectionIdentifier(int guid);

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        static public extern bool IsIdentifierConnectable(int guid);

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        static internal extern bool IsIdentifierOnLocalhost(int guid);

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        static internal extern bool IsConnectionEditor();

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        static public extern void DirectIPConnect(string IP);

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        static public extern void DirectURLConnect(string IP);

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        static public extern string directConnectionUrl
        {
            [NativeMethod("GetDirectConnectionURL")] get;
        }

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        static public extern int connectedProfiler
        {
            [NativeMethod("GetConnectedProfiler")]
            get;
            [NativeMethod("SetConnectedProfiler")]
            set;
        }

        static public extern string miniMemoryOverview
        {
            [FreeFunction("GetMiniMemoryOverview")] get;
        }

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        [NativeMethod("GetAvailableProfilers")]
        static public extern int[] GetAvailableProfilers();

        [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
        [NativeMethod("GetOverviewTextForProfilerArea")]
        static public extern string GetOverviewText(ProfilerArea profilerArea, int frame);

        static public extern uint usedHeapSize
        {
            [FreeFunction("GetUsedHeapSize")]
            get;
        }

        static public extern uint objectCount
        {
            [StaticAccessor("Object", StaticAccessorType.DoubleColon)]
            [NativeMethod("GetLoadedObjectCount")]
            get;
        }

        static public extern bool isGPUProfilerSupportedByOS
        {
            [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
            [NativeMethod("IsGPUProfilerSupportedByOS")]
            get;
        }

        static public extern bool isGPUProfilerSupported
        {
            [StaticAccessor("profiling::GetProfilerSessionPtr()->GetProfilerHistory()", StaticAccessorType.Arrow)]
            [NativeMethod("IsGPUProfilerSupported")]
            get;
        }

        [Obsolete("Deprecated API, it will always return false")]
        static public bool isGPUProfilerBuggyOnDriver
        {
            get
            {
                return false;
            }
        }

        static public void RequestMemorySnapshot()
        {
            UnityEditor.MemoryProfiler.MemorySnapshot.RequestNewSnapshot();
        }

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        [NativeMethod("SendGetObjectMemoryProfile")]
        static public extern void RequestObjectMemoryInfo(bool gatherObjectReferences);

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        [NativeMethod("SendQueryInstrumentableFunctions")]
        static public extern void QueryInstrumentableFunctions();

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        [NativeMethod("SendQueryFunctionCallees")]
        static public extern void QueryFunctionCallees(string fullname);

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        [NativeMethod("SendSetAutoInstrumentedAssemblies")]

        static public extern void SetAutoInstrumentedAssemblies(InstrumentedAssemblyTypes fullname);

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        [NativeMethod("SendSetAudioCaptureFlags")]
        static public extern void SetAudioCaptureFlags(int flags);

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        [NativeMethod("SendBeginInstrumentFunction")]
        static public extern void BeginInstrumentFunction(string fullName);

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        [NativeMethod("SendEndInstrumentFunction")]
        static public extern void EndInstrumentFunction(string fullName);
    }
}
