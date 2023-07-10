// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // In order to correctly support UxmlAssetAttributeDescription<T>, Unity must know how to reimport
    // UXML files when such attributes change (e.g. when code is changed).
    // To achieve this we use the Custom Dependency feature of the Asset Database
    // 1. If a type has any relevant attribute, we will register this as custom dependency name (using fully qualified name)
    // 2. The "version" is Hash128 created by sequentially combining attribute name and object type.
    // 3. The UXML importer declares a dependency using the name as previously described
    // All names are preceded by a prefix, which is left as a parameter here to make testing easier
    class UxmlCodeDependencies
    {
        const string k_DefaultDependencyPrefix = "UxmlFactory/";

        internal static UxmlCodeDependencies instance { get; } = Build(k_DefaultDependencyPrefix);

        string m_DependencyPrefix;
        HashSet<string> m_Set;

        UxmlCodeDependencies(string customDependencyPrefix)
        {
            m_DependencyPrefix = customDependencyPrefix;
            m_Set = new();
        }

        internal static UxmlCodeDependencies Build(string customDependencyPrefix)
        {
            // Since we will rebuild dependencies, clear any existing values registered with Asset Database first
            AssetDatabase.UnregisterCustomDependencyPrefixFilter(customDependencyPrefix);
            return new UxmlCodeDependencies(customDependencyPrefix);
        }

        internal string FormatDependencyKeyName(string uxmlQualifiedName) => m_DependencyPrefix + uxmlQualifiedName;

        static ProfilerMarker s_RegisterMarker = new("UxmlCodeDependencies.RegisterAssetAttributeDependencies");

        internal void RegisterAssetAttributeDependencies(IBaseUxmlFactory factory)
        {
            var uxmlAttributesDescription = factory.uxmlAttributesDescription;
            if (uxmlAttributesDescription == null)
                return;

            using var _ = s_RegisterMarker.Auto();

            var dependencyKeyName = FormatDependencyKeyName(factory.uxmlQualifiedName);
            var valueHash = new Hash128();
            uint count = 0;

            foreach (var description in uxmlAttributesDescription)
            {
                if (description != null && description is IUxmlAssetAttributeDescription assetAttributeDescription)
                {
                    valueHash.Append(description.name);
                    valueHash.Append(assetAttributeDescription.assetType.AssemblyQualifiedName);

                    ++count;
                }
            }

            if (count > 0)
            {
                // This actually will cause a reimport if the value hash has changed since last import of UXML
                AssetDatabase.RegisterCustomDependency(dependencyKeyName, valueHash);
                m_Set.Add(dependencyKeyName);
            }
            else
            {
                UnregisterDependencies(factory);
            }
        }

        internal void UnregisterDependencies(IBaseUxmlFactory factory)
        {
            var dependencyKeyName = FormatDependencyKeyName(factory.uxmlQualifiedName);
            if (m_Set.Contains(dependencyKeyName))
            {
                AssetDatabase.UnregisterCustomDependencyPrefixFilter(dependencyKeyName);
                m_Set.Remove(dependencyKeyName);
            }
        }

        internal bool HasAnyAssetAttributes(IBaseUxmlFactory factory)
        {
            var dependencyKeyName = FormatDependencyKeyName(factory.uxmlQualifiedName);
            return m_Set.Contains(dependencyKeyName);
        }
    }
}
