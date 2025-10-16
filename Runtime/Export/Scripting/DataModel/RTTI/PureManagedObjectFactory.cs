// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Unity.DataModel;

internal sealed class ConstructedPureManagedObjectGroup
{
    internal PureManagedObjectRtti rtti;
    internal UdmObjectId[] objectIds;
    internal object[] instances;
    internal IntPtr[] objectPtrs;
}

internal struct ConstructedPureManagedObjectSet
{
    internal List<ConstructedPureManagedObjectGroup> objectGroups;

    public ConstructedPureManagedObjectSet()
    {
        objectGroups = new List<ConstructedPureManagedObjectGroup>();
    }
}

internal sealed class PureManagedObjectFactory
{
    private ConstructedPureManagedObjectSet pureManagedObjects = new ConstructedPureManagedObjectSet();

    internal static object[] Instantiate(PureManagedObjectRtti rtti, int count)
    {
        var objects = new object[count];
        for (int objectIndex = 0; objectIndex < objects.Length; objectIndex++)
        {
            objects[objectIndex] = RuntimeHelpers.GetUninitializedObject(rtti.Type);

            // We need to call the constructor for pure managed types
            if (rtti.Constructor != null)
            {
                rtti.Constructor.Invoke(objects[objectIndex], null);
            }
        }
        return objects;
    }

    internal void AddObjectsToBatch(PureManagedObjectRtti rtti, UdmObjectId[] objectIds)
    {
        var objectGroup = new ConstructedPureManagedObjectGroup {
            rtti = rtti,
            objectIds = objectIds,
            instances = Instantiate(rtti, objectIds.Length),
            objectPtrs = Array.Empty<IntPtr>()
        };
        pureManagedObjects.objectGroups.Add(objectGroup);
    }

    internal ConstructedPureManagedObjectSet FlushBatch()
    {
        return pureManagedObjects;
    }
}
