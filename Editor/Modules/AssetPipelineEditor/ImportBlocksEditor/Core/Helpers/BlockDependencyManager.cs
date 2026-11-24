// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    internal static partial class BlockDependencyManager
    {
        internal const string k_ToggleDependencyKey = "ImportBlocks/Enabled";

        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<Type, ImportBlockAttribute> s_DependencyCache =
            new Dictionary<Type, ImportBlockAttribute>();

        [AutoStaticsCleanupOnCodeReload]
        static readonly List<string> s_PendingDiagnostics = new List<string>();

        internal static IReadOnlyList<string> PendingDiagnostics => s_PendingDiagnostics;

        internal static Hash128 ComputeChainHash(Type concreteType)
        {
            var parts = new List<int>();
            var t = concreteType;
            while (t != null && t != typeof(object))
            {
                var attr = t.GetCustomAttribute<ImportBlockAttribute>(inherit: false);
                if (attr != null) parts.Add(attr.Version);
                t = t.BaseType;
            }

            parts.Reverse();
            return Hash128.Compute(string.Join(":", parts));
        }

        internal static void RegisterToggleDependency()
        {
            AssetDatabase.RegisterCustomDependency(k_ToggleDependencyKey, Hash128.Compute(ImportBlocksToggle.IsEnabled ? "On" : "Off"));
        }

        [InitializeOnLoadMethod]
        static void InitializeAllBlocks()
        {
            s_PendingDiagnostics.Clear();

            RegisterToggleDependency();

            var types = TypeCache.GetTypesDerivedFrom<UnityEditor.Experimental.AssetImporters.ImportBlocks.Block>();

            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<ImportBlockAttribute>(inherit: false);

                if (type.IsAbstract)
                {
                    if (attribute == null)
                        s_PendingDiagnostics.Add(
                            $"[IB0005] Abstract Block '{type.FullName}' is missing [ImportBlock(version)]. " +
                            "Abstract classes in the hierarchy require this for composed dependency hashing and per-class upgrade dispatch.");

                    if (attribute != null)
                        s_DependencyCache[type] = attribute;

                    continue;
                }

                if (!type.IsDefined(typeof(SerializableAttribute), inherit: false))
                    s_PendingDiagnostics.Add($"[IB0001] '{type.FullName}' is missing [Serializable]. " +
                                   "It is needed to serialize blocks.");

                if (type.GetConstructor(Type.EmptyTypes) == null)
                    s_PendingDiagnostics.Add($"[IB0006] '{type.FullName}' has no public parameterless constructor. " +
                                   "[SerializeReference] deserialization and the Add Block menu require one to create the block.");

                if (attribute == null)
                {
                    s_PendingDiagnostics.Add($"[IB0002] '{type.FullName}' is missing [ImportBlock(version)]. " +
                                   "Every Block requires this for dependency tracking and versioning.");
                    continue;
                }

                s_DependencyCache[type] = attribute;

                var dependencyName = ImportBlockAttribute.GetDependencyName(type);
                AssetDatabase.RegisterCustomDependency(dependencyName, ComputeChainHash(type));
            }

            ReportDiagnostics();
        }

        // Surface authoring diagnostics collected above. Without this they were silently buffered, so a block
        // missing [Serializable] would be dropped by [SerializeReference] with no console output. Warnings only —
        // they must not fail imports or tests.
        static void ReportDiagnostics()
        {
            foreach (var diagnostic in s_PendingDiagnostics)
                Debug.LogWarning(diagnostic);
        }

        /// <summary>
        /// Returns the <see cref="ImportBlockAttribute"/> for the given block type, or null if the type has no [ImportBlock] attribute.
        /// </summary>
        public static ImportBlockAttribute GetDependencyAttribute(Type blockType)
        {
            if (s_DependencyCache.TryGetValue(blockType, out var attribute))
            {
                return attribute;
            }

            // inherit:false to match InitializeAllBlocks — Block itself carries [ImportBlock(1)], so an inherited
            // lookup would return non-null for every subclass, masking a concrete block that lacks its own attribute
            // and causing a dependency on a custom-dependency key that was never registered.
            attribute = blockType.GetCustomAttribute<ImportBlockAttribute>(inherit: false);
            if (attribute != null)
            {
                s_DependencyCache[blockType] = attribute;
            }

            return attribute;
        }
    }
}
