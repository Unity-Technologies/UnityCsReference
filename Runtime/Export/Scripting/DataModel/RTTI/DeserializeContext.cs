// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.EntitiesLike;

namespace Unity.DataModel;

internal sealed class DeserializeContext
{
    internal DeserializeContext(
        DocumentModel documentModel = default,
        EntityManager entityManager = default,
        SerializeInstructionFlags options = SerializeInstructionFlags.NoSerializeInstructionFlags,
        UDMRefResolver resolver = default)
    {
        EntityManager = entityManager;
        m_Resolver = resolver;
        DocumentModel = documentModel;
        Options = options;
    }

    internal EntityId ResolveInstanceID(Reference reference)
    {
        if (!reference.IsValid())
            return EntityId.None;

        if (InstanceRegistry.TryGetInstance(reference, out var instance))
        {
            if (instance is UnityEngine.Object unityObj)
            {
                unityObj.GetInstanceIdFast(out var instanceID); // GetInstanceIdFast() can be called on any thread
                return instanceID;
            }

            return EntityId.None;
        }

        if (reference.IsExternal() && !AllowResolveExternalReferences)
        {
            return EntityId.None;
        }
        return EntityId.None;
    }

    internal object ResolveInstance(Reference reference)
    {
        if (!reference.IsValid())
            return null;

        object obj = null;

        if (InstanceRegistry.TryGetInstance(reference, out obj))
            return obj;

        if (reference.IsExternal() && !AllowResolveExternalReferences)
        {
            return null;
        }

        return obj;
    }

    internal void SetInstance(Reference reference, object obj)
    {
        InstanceRegistry.SetInstance(reference, obj);
    }

    internal readonly DocumentModel DocumentModel;
    internal InstanceRegistry InstanceRegistry { get; private set; } = new();
    internal readonly SerializeInstructionFlags Options;
    internal bool AllowResolveExternalReferences { get; set; } = true;
    internal NullObjectData NullObjectData;

    internal readonly EntityManager EntityManager;
    UDMRefResolver m_Resolver;
    internal UDMRefResolver Resolver
    {
        get
        {
            if (m_Resolver == null)
            {
                var objectIds =  Array.Empty<UdmObjectId>();
                var instanceIds = Array.Empty<EntityId>();
                var nativeObjects = Array.Empty<UnityEngine.Object>();
                
            }
            return m_Resolver;
        }
    }
}
