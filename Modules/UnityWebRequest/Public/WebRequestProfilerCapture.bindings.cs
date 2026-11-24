// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Networking
{
    // Lets a caller with no UnityWebRequest record into the same capture table and id space as the transport.
    [NativeHeader("Modules/UnityWebRequest/Profiler/WebRequestProfilerCapture.h")]
    [StaticAccessor("WebRequestProfiler", StaticAccessorType.DoubleColon)]
    [VisibleToOtherModules("UnityEditor.NetworkProfilerModule")]
    internal static class WebRequestProfilerCapture
    {
        internal const ulong k_InvalidRequestId = 0;

        [NativeMethod("IsCapturingRequests", IsThreadSafe = true)]
        [VisibleToOtherModules("UnityEditor.NetworkProfilerModule")]
        internal static extern bool IsCapturing();

        [NativeMethod("CaptureRequestStarted", IsThreadSafe = true)]
        internal static extern ulong RequestStarted(string url, string method, byte source);

        [NativeMethod("CaptureRequestTransportAttached", IsThreadSafe = true)]
        internal static extern void TransportAttached(ulong id, byte measurable, bool canReportBodies);

        // Null or empty leaves that field as it was.
        [NativeMethod("CaptureRequestDetails", IsThreadSafe = true)]
        internal static extern void RequestDetails(ulong id, string url, string method);

        [NativeMethod("CaptureRequestConfiguredHeader", IsThreadSafe = true)]
        internal static extern void RequestHeader(ulong id, string name, string value);

        [NativeMethod("CaptureRequestCompletedWithoutTransport", IsThreadSafe = true)]
        internal static extern void RequestCompleted(ulong id, bool succeeded, ushort statusCode);

        [NativeMethod("CaptureRequestAbandoned", IsThreadSafe = true)]
        internal static extern void RequestAbandoned(ulong id);
    }
}
