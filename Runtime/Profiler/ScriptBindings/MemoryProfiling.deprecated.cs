// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// This file is only used by the ScriptUpdater
// This file was needed because the MovedTo Attribute couldn't be applied as the field names of the
// DebugScreenCapture class and MetaData struct needed to be adjusted to the convention in the Unity namespace
using System;
using Unity.Collections;
using UnityEngine.Profiling.Experimental;

namespace UnityEngine.Profiling.Memory.Experimental
{
    [Obsolete("UnityEngine.Profiling.Memory.Experimental.MetaData has been deprecated use Unity.Profiling.Memory.MemorySnapshotMetadata instead (UnityUpgradable) -> Unity.Profiling.Memory.MemorySnapshotMetadata", true)]
    public class MetaData
    {
        [Obsolete("MetaData.content has been deprecated use Unity.Profiling.Memory.MemorySnapshotMetadata.Description instead (UnityUpgradable) -> Unity.Profiling.Memory.MemorySnapshotMetadata.Description", true)]
        [NonSerialized]
        public string    content;
        [Obsolete("MetaData.platform has been deprecated as it is now part of the snapshots binary data.")]
        [NonSerialized]
        public string    platform;
    }

    [Obsolete("UnityEngine.Profiling.Memory.Experimental.CaptureFlags has been deprecated use Unity.Profiling.Memory.CaptureFlags instead (UnityUpgradable) -> Unity.Profiling.Memory.CaptureFlags", true)]
    [Flags]
    public enum CaptureFlags : uint
    {
        ManagedObjects        = 1 << 0,
        NativeObjects         = 1 << 1,
        NativeAllocations     = 1 << 2,
        NativeAllocationSites = 1 << 3,
        NativeStackTraces     = 1 << 4,
    }

    [Obsolete("UnityEngine.Profiling.Memory.Experimental.MemoryProfiler has been deprecated use Unity.Profiling.Memory.MemoryProfiler instead (UnityUpgradable) -> Unity.Profiling.Memory.MemoryProfiler", true)]
    public static class MemoryProfiler
    {
#pragma warning disable CS0067 // Yes the event is never used. No warning needed
        [Obsolete("UnityEngine.Profiling.Memory.Experimental.MemoryProfiler.createMetaData has been deprecated use Unity.Profiling.Memory.MemoryProfiler.CreatingMetadata instead (UnityUpgradable) -> Unity.Profiling.Memory.MemoryProfiler.CreatingMetadata", true)]
        public static event Action<MetaData>     createMetaData;
#pragma warning restore CS0067

        [Obsolete("UnityEngine.Profiling.Memory.Experimental.MemoryProfiler.TakeSnapshot has been deprecated use Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot instead (UnityUpgradable) -> Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot(*)", true)]
        public static void TakeSnapshot(string path, Action<string, bool> finishCallback, CaptureFlags captureFlags = CaptureFlags.NativeObjects | CaptureFlags.ManagedObjects) {}
        [Obsolete("UnityEngine.Profiling.Memory.Experimental.MemoryProfiler.TakeSnapshot has been deprecated use Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot instead (UnityUpgradable) -> Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot(*)", true)]
        public static void TakeSnapshot(string path, Action<string, bool> finishCallback, Action<string, bool, DebugScreenCapture> screenshotCallback, CaptureFlags captureFlags = CaptureFlags.NativeObjects | CaptureFlags.ManagedObjects) {}
        [Obsolete("UnityEngine.Profiling.Memory.Experimental.MemoryProfiler.TakeTempSnapshot has been deprecated use Unity.Profiling.Memory.MemoryProfiler.TakeTempSnapshot instead (UnityUpgradable) -> Unity.Profiling.Memory.MemoryProfiler.TakeTempSnapshot(*)", true)]
        public static void TakeTempSnapshot(Action<string, bool>  finishCallback, CaptureFlags captureFlags = CaptureFlags.NativeObjects | CaptureFlags.ManagedObjects) {}
    }
}

namespace UnityEngine.Profiling.Experimental
{
    [Obsolete("UnityEngine.Profiling.Experimental.DebugScreenCapture has been deprecated use Unity.Profiling.DebugScreenCapture instead (UnityUpgradable) -> Unity.Profiling.DebugScreenCapture", true)]
    public struct DebugScreenCapture
    {
        [Obsolete("DebugScreenCapture.rawImageDataReference has been deprecated use Unity.Profiling.DebugScreenCapture.RawImageDataReference instead (UnityUpgradable) -> Unity.Profiling.DebugScreenCapture.RawImageDataReference", true)]
        public NativeArray<byte> rawImageDataReference { get; set; }
        [Obsolete("DebugScreenCapture.imageFormat has been deprecated use Unity.Profiling.DebugScreenCapture.ImageFormat instead (UnityUpgradable) -> Unity.Profiling.DebugScreenCapture.ImageFormat", true)]
        public TextureFormat imageFormat { get; set; }
        [Obsolete("DebugScreenCapture.width has been deprecated use Unity.Profiling.DebugScreenCapture.Width instead (UnityUpgradable) -> Unity.Profiling.DebugScreenCapture.Width", true)]
        public int width { get; set; }
        [Obsolete("DebugScreenCapture.height has been deprecated use Unity.Profiling.DebugScreenCapture.Height instead (UnityUpgradable) -> Unity.Profiling.DebugScreenCapture.Height", true)]
        public int height { get; set; }
    }
}
