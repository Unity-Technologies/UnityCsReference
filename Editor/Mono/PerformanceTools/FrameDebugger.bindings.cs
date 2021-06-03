// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Mono/PerformanceTools/FrameDebugger.bindings.h")]
    [StaticAccessor("FrameDebugger", StaticAccessorType.DoubleColon)]
    internal sealed class FrameDebuggerUtility
    {
        extern public static void SetEnabled(bool enabled, int remotePlayerGUID);
        public extern static int GetRemotePlayerGUID();
        public extern static bool receivingRemoteFrameEventData { [NativeName("IsReceivingRemoteFrameEventData")] get; }
        public extern static bool locallySupported { [NativeName("IsSupported")] get; }
        [NativeName("FinalDrawCallCount")] public extern static int count { get; }
        [NativeName("DrawCallLimit")] public extern static int limit { get; set; }
        [NativeName("FrameEventsHash")] public extern static int eventsHash { get; }
        [NativeName("FrameEventDataHash")] public extern static uint eventDataHash { get; }
        public extern static void SetRenderTargetDisplayOptions(int rtIndex, Vector4 channels, float blackLevel, float whiteLevel);
        [NativeName("GetProfilerEventName")] public extern static string GetFrameEventInfoName(int index);
        public extern static Object GetFrameEventObject(int index);
        [FreeFunction("FrameDebuggerBindings::GetBatchBreakCauseStrings")] public extern static string[] GetBatchBreakCauseStrings();

        public static FrameDebuggerEvent[] GetFrameEvents() { return (FrameDebuggerEvent[])GetFrameEventsImpl(); }
        [NativeName("GetFrameEvents")] extern private static System.Array GetFrameEventsImpl();

        // Returns false, if frameEventData holds data from previous selected frame
        public static bool GetFrameEventData(int index, FrameDebuggerEventData frameDebuggerEventData)
        {
            // native code will poke and modify the FrameDebuggerEventData object fields
            // directly via a pointer to it
            var handle = GCHandle.Alloc(frameDebuggerEventData, GCHandleType.Pinned);
            GetFrameEventDataImpl(handle.AddrOfPinnedObject());
            handle.Free();
            return frameDebuggerEventData.frameEventIndex == index;
        }

        [FreeFunction("FrameDebuggerBindings::GetFrameEventDataImpl")]
        extern private static void GetFrameEventDataImpl(System.IntPtr frameDebuggerEventData);
    }
}
