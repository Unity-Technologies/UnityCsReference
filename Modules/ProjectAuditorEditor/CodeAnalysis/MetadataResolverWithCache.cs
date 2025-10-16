// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    class MetadataResolverWithCache : IMetadataResolver
    {
        IMetadataResolver m_Impl;
        Dictionary<object, object> m_Cache = new Dictionary<object, object>();

        public MetadataResolverWithCache(IAssemblyResolver assemblyResolver)
        {
            m_Impl = new MetadataResolver(assemblyResolver);
        }

        public TypeDefinition Resolve(TypeReference reference)
        {
            object definition;
            if (m_Cache.TryGetValue(reference, out definition))
                return (TypeDefinition)definition;
            definition = m_Impl.Resolve(reference);
            m_Cache.Add(reference, definition);
            return (TypeDefinition)definition;
        }

        public FieldDefinition Resolve(FieldReference reference)
        {
            object definition;
            if (m_Cache.TryGetValue(reference, out definition))
                return (FieldDefinition)definition;
            definition = m_Impl.Resolve(reference);
            m_Cache.Add(reference, definition);
            return (FieldDefinition)definition;
        }

        public MethodDefinition Resolve(MethodReference reference)
        {
            object definition;
            if (m_Cache.TryGetValue(reference, out definition))
                return (MethodDefinition)definition;
            definition = m_Impl.Resolve(reference);
            m_Cache.Add(reference, definition);
            return (MethodDefinition)definition;
        }
    }
}
