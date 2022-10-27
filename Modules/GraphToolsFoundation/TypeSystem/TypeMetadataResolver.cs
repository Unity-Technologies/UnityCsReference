// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// Base class for type metadata resolvers.
    /// </summary>
    class TypeMetadataResolver
    {
        readonly ConcurrentDictionary<TypeHandle, TypeMetadataBase> m_MetadataCache = new();

        /// <summary>
        /// Gets the <see cref="TypeMetadataBase"/> for a <see cref="TypeHandle"/>.
        /// </summary>
        /// <param name="th">The type handle for which to get the metadata.</param>
        /// <returns>The metadata for the type handle.</returns>
        public virtual TypeMetadataBase Resolve(TypeHandle th)
        {
            if (!m_MetadataCache.TryGetValue(th, out TypeMetadataBase metadata))
            {
                metadata = m_MetadataCache.GetOrAdd(th, t => new TypeMetadata(t, th.Resolve()));
            }
            return metadata;
        }
    }
}
