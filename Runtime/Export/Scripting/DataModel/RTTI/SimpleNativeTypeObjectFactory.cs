// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.DataModel;

internal sealed class ConstructedSimpleNativeTypeObjectGroup
{
    internal SimpleNativeTypeObjectRtti rtti;
    internal UdmObjectId[] objectIds;
    internal ISimpleNativeType[] instances;
    internal IntPtr[] objectPtrs;
}

internal struct ConstructedSimpleNativeTypeObjectSet
{
    internal List<ConstructedSimpleNativeTypeObjectGroup> objectGroups;

    public ConstructedSimpleNativeTypeObjectSet()
    {
        objectGroups = new List<ConstructedSimpleNativeTypeObjectGroup>();
    }
}

internal sealed class SimpleNativeTypeObjectFactory
{
    private ConstructedSimpleNativeTypeObjectSet simpleNativeTypeObjects = new ConstructedSimpleNativeTypeObjectSet();

    internal static ISimpleNativeType[] Instantiate(SimpleNativeTypeObjectRtti rtti, int count)
    {
        var objects = new ISimpleNativeType[count];
        for (int objectIndex = 0; objectIndex < objects.Length; objectIndex++)
        {
            objects[objectIndex] = (ISimpleNativeType)RuntimeHelpers.GetUninitializedObject(rtti.Type);

            // We need to call the constructor for simple native types
            if (rtti.Constructor != null)
            {
                rtti.Constructor.Invoke(objects[objectIndex], null);
            }
        }
        return objects;
    }

    internal void AddObjectsToBatch(SimpleNativeTypeObjectRtti rtti, UdmObjectId[] objectIds)
    {
        var objectGroup = new ConstructedSimpleNativeTypeObjectGroup {
            rtti = rtti,
            objectIds = objectIds,
            instances = Instantiate(rtti, objectIds.Length),
            objectPtrs = Array.Empty<IntPtr>()
        };
        simpleNativeTypeObjects.objectGroups.Add(objectGroup);
    }

    internal ConstructedSimpleNativeTypeObjectSet FlushBatch()
    {
        return simpleNativeTypeObjects;
    }
}
