// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// Generated file. Entries are produced by LeakDetectorAllowListGenerator from the
// Logs/ALCLeaks*.json reports a run writes, normally a set downloaded from CI job artifacts:
// most of these leaks do not reproduce on a single machine in a single project.
//
//   Unity.exe -projectPath <any project> -batchmode -quit \
//     -executeMethod UnityEditor.Scripting.CodeReload.LeakDetectorAllowListGenerator.Regenerate \
//     -alc-leak-reports <folder of downloaded reports>
//
// That unions what the reports observed with what is already here. Pass
// -alc-leak-allowlist-replace to rebaseline from a complete artifact set instead. New entries
// are emitted with TODO placeholders - fill in the owning team and tracking ticket.
//
// Deleting entries by hand is the expected way to retire them: when a leak is fixed, remove its
// entries as part of that fix rather than regenerating the whole file. Only use the generator to
// add entries, so their identities come from a measured run rather than from guesswork.
//
// TEMPORARY TECHNICAL DEBT. Every entry below is a known, pre-existing AssemblyLoadContext leak
// (COPT-3929) that the ALC leak gate is told to tolerate so the gate can be enabled at all. An
// allow-listed leak still leaks - the ALC does not unload.
//
// This whole file and the filter that reads it are deleted by TASK-24 of the UnityALC
// series (epic SCP-1987) once the list is empty. Do not build anything on top of it.
//
// Adding an entry requires a named reviewer: add @christianb to the pull request. Removing an entry
// needs no review - it is what we are working towards.


using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Scripting.CodeReload
{
    internal static class LeakDetectorAllowListEntries
    {
        [NoAutoStaticsCleanup]
        internal static readonly IReadOnlyList<LeakAllowListEntry> Entries = new LeakAllowListEntry[]
        {
            new LeakAllowListEntry("System.Private.CoreLib", "System.Reflection.LoaderAllocator", "Scripting", "COPT-3929"),
            new LeakAllowListEntry("Unity.VisualStudio.Editor", "Microsoft.Unity.VisualStudio.Editor.Messaging.Messager", "IDE Integration", "SCP-2157"),
            new LeakAllowListEntry("Unity.VisualStudio.Editor", "Microsoft.Unity.VisualStudio.Editor.Messaging.UdpSocket", "IDE Integration", "SCP-2157"),
            new LeakAllowListEntry("Unity.VisualStudio.Editor", "Microsoft.Unity.VisualStudio.Editor.VisualStudioIntegration+<>c__DisplayClass8_0", "IDE Integration", "SCP-2157"),
        };
    }
}

