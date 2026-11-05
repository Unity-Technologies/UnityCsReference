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
            new LeakAllowListEntry("", "Unity.Services.Core.Editor.Environments.EnvironmentInfo[]", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("", "UnityEngine.Formats.Alembic.Sdk.aeSubmeshData[]", "Alembic Importer", "SCP-2272"),
            new LeakAllowListEntry("Assembly-CSharp-Editor-testable", "FileSystemProvider+<>c", "Search", "SCP-2273"),
            new LeakAllowListEntry("Assembly-CSharp-Editor-testable", "HierarchicalTypeProvider+<>c", "Search", "SCP-2273"),
            new LeakAllowListEntry("Assembly-CSharp-Editor-testable", "ItemGeneratorSearchProvider", "Search", "SCP-2273"),
            new LeakAllowListEntry("Assembly-CSharp-Editor-testable", "UnityEditor.Search.Providers.LightingDataSearchProvider", "Search", "SCP-2273"),
            new LeakAllowListEntry("Assembly-CSharp-Editor-testable", "UnityEditor.Search.Providers.LightingDataSearchProvider+<>c", "Search", "SCP-2273"),
            new LeakAllowListEntry("Assembly-CSharp-Editor-testable", "UnityEngine.UIElements.PerformanceTests.EnableDisableRendering+PanelOwner", "UI Toolkit Framework", "SCP-2268"),
            new LeakAllowListEntry("Grpc.Core", "Grpc.Core.GrpcEnvironment+ShutdownHooks+<>c", "ML Agents", "SCP-2265"),
            new LeakAllowListEntry("Moq", "Moq.It+<>c__1<Unity.Services.Core.Editor.Settings.EnvironmentSettings>", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("Moq", "Moq.Match<Unity.Services.Core.Editor.Settings.EnvironmentSettings>", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("Moq", "Moq.MatchExpression", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("Moq", "Moq.Mock<Unity.Services.Core.Editor.Environments.Analytics.IEnvironmentAnalytics>", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("Moq", "Moq.Mock<Unity.Services.Core.Editor.Environments.IEnvironmentFetcher>", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("Moq", "Moq.Mock<Unity.Services.Core.Editor.Environments.IEnvironmentProvider>", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("RefEmit_InMemoryManifestModule", "Castle.Proxies.IEnvironmentAnalyticsProxy", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("RefEmit_InMemoryManifestModule", "Castle.Proxies.IEnvironmentFetcherProxy", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("RefEmit_InMemoryManifestModule", "Castle.Proxies.IEnvironmentProviderProxy", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("System.Private.CoreLib", "System.Reflection.LoaderAllocator", "Scripting", "COPT-3929"),
            new LeakAllowListEntry("Unity.Modules.AI.Tests.Editor", "NavMeshBuildAsyncInterruption", "AI Navigation", "SCP-2264"),
            new LeakAllowListEntry("Unity.Modules.Audio.Tests.Playmode", "ScriptableProcessors.RootOutputTests+StateCounters", "Audio", "SCP-2269"),
            new LeakAllowListEntry("Unity.Modules.LocalizationRuntimeEditor.Tests.Editor", "Unity.Localization.Editor.Tests.TableCollectionServiceTests+StubService", "Localization", "SCP-2260"),
            new LeakAllowListEntry("Unity.Services.Core.Editor", "Unity.Services.Core.Editor.Shared.EditorUtils.EditorValueTracker<System.String>", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("Unity.Services.Core.Editor", "Unity.Services.Core.Editor.Shared.EditorUtils.Sync+<>c__DisplayClass0_0", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("Unity.Services.Core.EditorTests.Environments", "Unity.Services.Core.Editor.Tests.Environment.EnvironmentServiceTests+<>c__DisplayClass9_0", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("Unity.Services.Core.Environments.Editor", "Unity.Services.Core.Editor.Environments.EnvironmentService", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("Unity.Services.Core.Environments.Editor", "Unity.Services.Core.Editor.Environments.EnvironmentService+<>c", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("Unity.Services.Core.Environments.Editor", "Unity.Services.Core.Editor.Settings.EnvironmentSettings", "Services Core", "SCP-2262"),
            new LeakAllowListEntry("Unity.Services.Wire.Internal", "Unity.Services.Wire.Internal.Client+<>c__DisplayClass54_0", "UGS Wire", "SCP-2270"),
            new LeakAllowListEntry("Unity.Services.Wire.Internal", "Unity.Services.Wire.Internal.ConnectionFailedException", "UGS Wire", "SCP-2270"),
            new LeakAllowListEntry("Unity.Services.Wire.Tests", "Unity.Services.Wire.Tests.ActionScheduler", "UGS Wire", "SCP-2270"),
            new LeakAllowListEntry("Unity.Services.Wire.Tests", "Unity.Services.Wire.Tests.ActionScheduler+<>c__DisplayClass5_0", "UGS Wire", "SCP-2270"),
            new LeakAllowListEntry("Unity.Timeline", "UnityEngine.Timeline.AnimatorBindingCache", "Timeline", "SCP-2263"),
            new LeakAllowListEntry("Unity.Timeline", "UnityEngine.Timeline.AnimatorBindingCache+AnimatorEntryComparer", "Timeline", "SCP-2263"),
            new LeakAllowListEntry("com.unity.quicksearch.samples", "DynamicProvider+<>c", "Search", "SCP-2273"),
            new LeakAllowListEntry("com.unity.quicksearch.samples", "SpatialProvider+<>c", "Search", "SCP-2273"),
            new LeakAllowListEntry("com.unity.quicksearch.samples", "TypeProvider+<>c__DisplayClass8_0", "Search", "SCP-2273"),
        };
    }
}

