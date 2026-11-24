// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// Resolves and caches <see cref="ProfilerMarker"/> instances for block execution.
    /// Markers are keyed by (block concrete type, hook interface type) and created once,
    /// matching the static-dictionary caching convention used in <see cref="Block"/>.
    /// </summary>
    internal static partial class BlockProfiling
    {
        // Plain (lock-free) dictionaries: block dispatch is single-threaded per process
        // (one import at a time per asset import worker / scripted importer), matching the
        // static-cache convention in Block.cs.
        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<(Type blockType, Type interfaceType), ProfilerMarker> s_Markers
            = new Dictionary<(Type, Type), ProfilerMarker>();

        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<Type, string> s_HookMethodNames
            = new Dictionary<Type, string>();

        /// <summary>
        /// Returns the cached <see cref="ProfilerMarker"/> for the given (block type, interface type) pair.
        /// Creates and caches a new marker on the first call for each pair.
        /// </summary>
        internal static ProfilerMarker GetMarker(Type blockType, Type interfaceType)
        {
            var key = (blockType, interfaceType);
            if (!s_Markers.TryGetValue(key, out var marker))
            {
                marker = new ProfilerMarker(ProfilerCategory.Loading, GetMarkerName(blockType, interfaceType));
                s_Markers[key] = marker;
            }
            return marker;
        }

        /// <summary>
        /// Returns the marker name for the given block and interface types.
        /// Format: <c>ImportBlocks.{blockTypeFullName}.{hookMethodName}</c>.
        /// Uses the namespace-qualified FullName so two same-named blocks in different namespaces/assemblies get
        /// distinct profiler markers instead of aggregating under one sample.
        /// Factored out so it is unit-testable without constructing a <see cref="ProfilerMarker"/>.
        /// </summary>
        internal static string GetMarkerName(Type blockType, Type interfaceType)
        {
            return $"ImportBlocks.{blockType.FullName}.{HookMethodName(interfaceType)}";
        }

        /// <summary>
        /// True when <paramref name="interfaceType"/> is a dispatchable hook — an <see cref="IBlock"/>
        /// interface that declares exactly one method — so a marker resolves to a clean
        /// <c>ImportBlocks.{Block}.{HookMethod}</c> name. Family/marker interfaces such as
        /// <see cref="IBlock"/> itself declare no single method, so they
        /// are not markable; dispatch over them yields blocks without markers.
        /// </summary>
        internal static bool IsHookInterface(Type interfaceType)
        {
            return interfaceType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Length == 1;
        }

        /// <summary>
        /// Returns the hook method name for the given interface type, cached after the first call.
        /// Uses <see cref="BindingFlags.DeclaredOnly"/> so inherited <see cref="IBlock"/> members
        /// (e.g. <c>get_Name</c>, <c>get_Description</c>, <c>GetChildBlocks</c>) are excluded.
        /// Falls back to the interface name itself when no declared methods are found (marker interface).
        /// </summary>
        static string HookMethodName(Type interfaceType)
        {
            if (s_HookMethodNames.TryGetValue(interfaceType, out var cached))
                return cached;

            var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            // Hook interfaces are expected to declare exactly one method. A grouping/marker
            // interface (0 methods) or a multi-method interface yields an inaccurate marker
            // name; assert in development so misuse is caught at the call site.
            UnityEngine.Debug.Assert(methods.Length == 1,
                $"BlockProfiling: hook interface '{interfaceType.Name}' should declare exactly one method (found {methods.Length}); profiler marker name may be inaccurate.");
            var name = methods.Length > 0 ? methods[0].Name : interfaceType.Name;
            s_HookMethodNames[interfaceType] = name;
            return name;
        }

        /// <summary>
        /// The number of entries in the marker cache. Exposed for unit tests.
        /// </summary>
        internal static int MarkerCacheCount => s_Markers.Count;
    }
}
