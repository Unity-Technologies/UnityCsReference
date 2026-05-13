// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

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
        const string k_UxmlSerializedDataDependencyPrefix = "UxmlSerializedData/";

        internal static UxmlCodeDependencies instance { get; } = new UxmlCodeDependencies();

        readonly HashSet<string> m_Set = new();

        internal string FormatSerializedDependencyKeyName(string uxmlQualifiedName) => k_UxmlSerializedDataDependencyPrefix + uxmlQualifiedName;

        static ProfilerMarker s_UxmlSerializationRegisterMarker = new(ProfilerCategory.UIToolkit, "UxmlCodeDependencies.UxmlSerialization.RegisterAssetAttributeDependencies");

        internal void RegisterUxmlSerializedDataDependencies(Dictionary<string, Type> serializedDataTypes)
        {
            AssetDatabase.UnregisterCustomDependencyPrefixFilter(k_UxmlSerializedDataDependencyPrefix);

            using var _ = s_UxmlSerializationRegisterMarker.Auto();

            foreach (var typeName in serializedDataTypes.Keys)
            {
                var type = UxmlSerializedDataRegistry.GetDataType(typeName);
                var desc = UxmlDescriptionRegistry.GetDescription(type);
                var dependencyKeyName = FormatSerializedDependencyKeyName(typeName);
                var valueHash = Hash128.Compute(typeName);

                foreach (var attribute in desc.attributeDescriptions)
                {
                    if (typeof(Object).IsAssignableFrom(attribute.fieldType))
                    {
                        valueHash.Append(attribute.uxmlName);
                        valueHash.Append(attribute.fieldType.AssemblyQualifiedName);
                        m_Set.Add(dependencyKeyName);
                    }
                }

                // This actually will cause a reimport if the value hash has changed since last import of UXML
                AssetDatabase.RegisterCustomDependency(dependencyKeyName, valueHash);
            }
        }

        internal bool HasAnyAssetAttributes(UxmlSerializedDataDescription description)
        {
            var dependencyKeyName = FormatSerializedDependencyKeyName(description.uxmlFullName);
            return m_Set.Contains(dependencyKeyName);
        }
    }
}
