// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Unity.DataModel;

internal sealed class ConstructedImmutablePureManagedObjectGroup
{
    internal ImmutablePureManagedObjectRtti rtti;
    internal UdmObjectId[] objectIds;
    internal object[] instances;
    internal IntPtr[] objectPtrs;
}

internal struct ConstructedImmutablePureManagedObjectSet
{
    internal List<ConstructedImmutablePureManagedObjectGroup> objectGroups;

    public ConstructedImmutablePureManagedObjectSet()
    {
        objectGroups = new List<ConstructedImmutablePureManagedObjectGroup>();
    }
}

// This object group handles immutable types (strings and arrays) that cannot be initialized with RuntimeHelpers.GetUninitializedObject(type).
// Unlike other factories, we create an instance of the object during deserialization and add it to the InstanceRegistry afterwards.
// Immutable objects must be deserialized first, so that the correct object instances are available in case they are referenced by other objects.
internal sealed class ImmutablePureManagedObjectFactory
{
    private ConstructedImmutablePureManagedObjectSet immutablePureManagedObjects = new ConstructedImmutablePureManagedObjectSet();

    internal void AddObjectsToBatch(ImmutablePureManagedObjectRtti rtti, UdmObjectId[] objectIds)
    {
        var objectGroup = new ConstructedImmutablePureManagedObjectGroup
        {
            rtti = rtti,
            objectIds = objectIds,
            instances = new object[objectIds.Length], // Immutable objects cannot be instantiated uninitialized
            objectPtrs = Array.Empty<IntPtr>()
        };
        immutablePureManagedObjects.objectGroups.Add(objectGroup);
    }

    internal ConstructedImmutablePureManagedObjectSet FlushBatch()
    {
        return immutablePureManagedObjects;
    }
}
