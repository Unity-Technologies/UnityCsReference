// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Internal;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Hmi/HmiPlatform.bindings.h")]
    public class HmiPlatform
    {
        /// <summary>
        /// Log to console including walltime/cpu time from unity start.
        /// e.g. '[TIMING::STARTUP] {tag}: {walltime} | {cpu time} ms
        /// Requires EmbeddedLinux.
        /// Can be guarded with UNITY_EMBEDDED_LINUX_API
        /// </summary>
        /// <param name="tag">The tag to appear in the log.</param>
        [ExcludeFromDocs]
        extern public static void LogStartupTiming(string tag);
    }
}
