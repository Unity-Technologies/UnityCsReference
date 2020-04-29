// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;

namespace UnityEditor.BugReporting
{
    // Keep in sync with "Editor/Platform/Interface/BugReportingTools.h"
    internal enum BugReportMode { ManualOpen, CrashBug, FatalError, CocoaExceptionOrAssertion, ManualSimple }

    [NativeHeader("Editor/Mono/BugReportingTools.bindings.h")]
    internal sealed class BugReportingTools
    {
        [StaticAccessor("BugReportingToolsBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("LaunchBugReportingTool")]
        public static extern void LaunchBugReporter(BugReportMode mode, string[] additionalArguments);
    }
}
