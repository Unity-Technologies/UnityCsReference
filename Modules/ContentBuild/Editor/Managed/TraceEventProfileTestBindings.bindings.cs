// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

// Test-only exposure: ContentBuildTests hosts the NUnit test classes and Assembly-CSharp-Editor-testable is the compiled-when-tests-enabled editor assembly they end up in.
[assembly: InternalsVisibleTo("ContentBuildTests")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]

namespace UnityEditor.Build.Content
{
    // Test-only managed access to the TraceEventProfile writer scenarios exposed by TraceEventProfileTestBindings.bindings.cpp/.h. Kept internal so it's only reachable from
    // the ContentBuild tests (via InternalsVisibleTo) and cannot leak into user code.
    [NativeHeader("Modules/ContentBuild/Editor/Public/TraceEventProfileTestBindings.bindings.h")]
    [StaticAccessor("TraceEventProfileTestBindings", StaticAccessorType.DoubleColon)]
    internal static class TraceEventProfileTestBindings
    {
        internal static extern bool CaptureAndSave(string outputFile,
            long anchorUnixUs, long anchorProfilerUs,
            bool remapMainThread, long fakeManagedTid);

        internal static extern bool CaptureAndSaveTwice(string outputFileA, string outputFileB,
            long anchorUnixUs, long anchorProfilerUs);

        internal static extern bool CaptureAndSaveWithTwoAnchors(string outputFileA, string outputFileB,
            long anchorUnixUsA, long anchorProfilerUsA,
            long anchorUnixUsB, long anchorProfilerUsB);

        internal static extern bool ShaderTraceCaptureNoOpsWhenSessionOwnedElsewhere(string outputFile);
    }
}
