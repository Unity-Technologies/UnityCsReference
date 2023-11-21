// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    class UxmlAssetAttributeCache
    {
        readonly Dictionary<string, Type> m_Cache = new();
        string m_CurrentTypeName;

        internal bool GetAssetAttributeType(string fullTypeName, string attributeName, out Type assetType)
        {
            LoadAssetAttributesForType(fullTypeName);
            return m_Cache.TryGetValue(attributeName, out assetType);
        }

        internal Dictionary<string, Type>.KeyCollection GetAssetAttributeNames(string fullTypeName)
        {
            LoadAssetAttributesForType(fullTypeName);
            return m_Cache.Keys;
        }

        static ProfilerMarker s_RegisterMarker = new ProfilerMarker("UxmlAssetAttributeCache.LoadAssetAttributesForType");

        void LoadAssetAttributesForType(string fullTypeName)
        {
            // Avoid reloading attribute info if the type is the same as we loaded last
            if (fullTypeName == m_CurrentTypeName)
                return;

            using var _ = s_RegisterMarker.Auto();

            m_Cache.Clear();
            m_CurrentTypeName = fullTypeName;

            static void CacheEnumerable(IEnumerable<UxmlAttributeDescription> attributes, Dictionary<string, Type> cache)
            {
                foreach (UxmlAttributeDescription description in attributes)
                {
                    if (description != null && description is IUxmlAssetAttributeDescription assetAttributeDescription)
                    {
                        cache[description.name] = assetAttributeDescription.assetType;
                    }
                }
            }

            #pragma warning disable CS0618 // Type or member is obsolete
            if (UxmlObjectFactoryRegistry.factories.TryGetValue(m_CurrentTypeName, out var uxmlObjectFactories))
            {
                foreach (var factory in uxmlObjectFactories)
                {
                    // If the factory has no known attributes, don't bother loading the full definitions
                    if (!UxmlCodeDependencies.instance.HasAnyAssetAttributes(factory))
                        continue;

                    CacheEnumerable(factory.uxmlAttributesDescription, m_Cache);
                }
            }
            #pragma warning restore CS0618 // Type or member is obsolete
            else if (VisualElementFactoryRegistry.factories.TryGetValue(m_CurrentTypeName, out var uxmlFactories))
            {
                foreach (var factory in uxmlFactories)
                {
                    // If the factory has no known attributes, don't bother loading the full definitions
                    if (!UxmlCodeDependencies.instance.HasAnyAssetAttributes(factory))
                        continue;

                    CacheEnumerable(factory.uxmlAttributesDescription, m_Cache);
                }
            }
        }
    }
}
