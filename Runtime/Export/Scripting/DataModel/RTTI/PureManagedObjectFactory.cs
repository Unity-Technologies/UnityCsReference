// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.Serialization;
using RefId = System.Int64;

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
    private readonly Dictionary<UdmObjectId, (UnityEngine.Object, RefId, object)> existingManagedObjects;
    private readonly LoadMode loadMode;
    private ConstructedPureManagedObjectSet pureManagedObjects = new ConstructedPureManagedObjectSet();

    internal PureManagedObjectFactory(Dictionary<UdmObjectId, (UnityEngine.Object, RefId, object)> existingManagedObjects, LoadMode loadMode)
    {
        this.loadMode = loadMode;
        this.existingManagedObjects = existingManagedObjects;
    }

    internal static void Instantiate(PureManagedObjectRtti rtti, object[] objects, int count)
    {
        for (int objectIndex = 0; objectIndex < count; objectIndex++)
        {
            objects[objectIndex] = RuntimeHelpers.GetUninitializedObject(rtti.Type);

            // We need to call the constructor for pure managed types
            if (rtti.Constructor != null)
            {
                rtti.Constructor.Invoke(objects[objectIndex], null);
            }
        }
    }

    internal static object[] Instantiate(PureManagedObjectRtti rtti, int count)
    {
        var objects = new object[count];
        Instantiate(rtti, objects, count);
        return objects;
    }

    internal void AddObjectsToBatch(PureManagedObjectRtti rtti, UdmObjectId[] objectIds)
    {
        int firstExistingObjectIndex = objectIds.Length;

        bool hasExistingObjects = loadMode == LoadMode.Merge && existingManagedObjects.Count > 0;
        if (hasExistingObjects)
        {
            Array.Sort(objectIds, (id1, id2) =>
            {
                bool exists1 = existingManagedObjects.ContainsKey(id1);
                bool exists2 = existingManagedObjects.ContainsKey(id2);
                return exists1.CompareTo(exists2);
            });

            firstExistingObjectIndex = Array.FindIndex(objectIds, id => existingManagedObjects.ContainsKey(id));
            if (firstExistingObjectIndex == -1)
            {
                firstExistingObjectIndex = objectIds.Length;
            }
        }

        var instances = new object[objectIds.Length];
        Instantiate(rtti, instances, firstExistingObjectIndex);

        for (int i = firstExistingObjectIndex; i < objectIds.Length; i++)
        {
            var (hostObj, managedReferenceId, obj) = existingManagedObjects[objectIds[i]];
            instances[i] = obj;
        }

        var objectGroup = new ConstructedPureManagedObjectGroup
        {
            rtti = rtti,
            objectIds = objectIds,
            instances = instances,
            objectPtrs = Array.Empty<IntPtr>()
        };
        pureManagedObjects.objectGroups.Add(objectGroup);
    }

    internal ConstructedPureManagedObjectSet FlushBatch()
    {
        return pureManagedObjects;
    }
}
