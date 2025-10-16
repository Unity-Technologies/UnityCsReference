// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;


using System.Runtime.CompilerServices;
using Unity.EntitiesLike;
using UnityEngine;

namespace Unity.DataModel;

internal sealed class ConstructedObjectSets
{
    internal ConstructedPureManagedObjectSet pureManagedObjects;
    internal ConstructedImmutablePureManagedObjectSet immutablePureManagedObjects;
    internal ConstructedUnityObjectSet unityObjects;
    internal PureEntitySet pureEntities;
    internal ConstructedSimpleNativeTypeObjectSet simpleNativeTypeObjects;
}

internal static class DocumentInstanceFactory
{
    internal static Dictionary<Schema, UdmObjectId[]> GetObjectsBySchema(DocumentModel documentModel)
    {
        Dictionary<Schema, UdmObjectId[]> objectsBySchema = new();

        foreach (ref readonly Schema schema in documentModel.GetSchemas())
        {
            var objectModelsPerSchema = documentModel.GetConstObjectModelsPerSchema(schema);

            unsafe
            {
                var objectModels = new ReadOnlySpan<ObjectModelPairEntry>(objectModelsPerSchema.Entries.ToPointer(), (int)objectModelsPerSchema.EntryCount);
                var objectArray = new UdmObjectId[objectModels.Length];
                for (int i = 0; i < objectModels.Length; i++)
                    objectArray[i] = objectModels[i].ObjectId;
                objectsBySchema[schema] = objectArray;
            }
        }

        return objectsBySchema;
    }

    internal static UdmObjectId[] GetNativeObjectIds(Dictionary<Schema, UdmObjectId[]> objectsBySchema)
    {
        var nativeObjectCount = 0;
        foreach (var kvp in objectsBySchema)
        {
            var rtti = RttiResolver.GetRTTI(kvp.Key);
            if (rtti.RttiGroup != RttiGroup.UnityObject)
                continue;
            nativeObjectCount += kvp.Value.Length;
        }

        int nativeObjectIdsTargetIndex = 0;
        var nativeObjectIds = new UdmObjectId[nativeObjectCount];

        foreach (var kvp in objectsBySchema)
        {
            var rtti = RttiResolver.GetRTTI(kvp.Key);
            if (rtti.RttiGroup != RttiGroup.UnityObject)
                continue;

            kvp.Value.CopyTo(nativeObjectIds, nativeObjectIdsTargetIndex);
            nativeObjectIdsTargetIndex += kvp.Value.Length;
        }

        return nativeObjectIds;
    }

    private static Dictionary<Schema, UdmObjectId[]> GroupObjectModelsBySchema(IReadOnlyCollection<ConstObjectModel> objectModels)
    {
        var objectModelsBySchema = new Dictionary<Schema, List<UdmObjectId>>();

        foreach (var objectModel in objectModels)
        {
            var schema = objectModel.GetSchema();
            if (!objectModelsBySchema.TryGetValue(schema, out var list))
            {
                list = new List<UdmObjectId>();
                objectModelsBySchema[schema] = list;
            }

            list.Add(objectModel.ObjectId);
        }

        Dictionary<Schema, UdmObjectId[]> r = new(objectModelsBySchema.Count);
        foreach (var kvp in objectModelsBySchema)
            r[kvp.Key] = kvp.Value.ToArray();

        return r;
    }

    internal static ConstructedObjectSets ConstructObjects(IReadOnlyCollection<ConstObjectModel> objectModels, Dictionary<UdmObjectId, EntityId> instanceIDs, EntityManager entityManager, bool registerObjects = false)
    {
        var objectsBySchema = GroupObjectModelsBySchema(objectModels);

        return ConstructObjects(objectsBySchema, instanceIDs, entityManager, registerObjects);
    }


    internal static ConstructedObjectSets ConstructObjects(Dictionary<Schema, UdmObjectId[]> objectsBySchema, Dictionary<UdmObjectId, EntityId> instanceIDs, EntityManager entityManager, bool registerObjects = false)
    {
        return ConstructAndMergeObjectsInternal(LoadMode.Normal, objectsBySchema, instanceIDs, null, entityManager, registerObjects);
    }

    internal static ConstructedObjectSets ConstructAndMergeObjectsInternal(LoadMode loadMode, Dictionary<Schema, UdmObjectId[]> objectsBySchema, Dictionary<UdmObjectId, EntityId> instanceIDs, HashSet<UdmObjectId> existingObjects, EntityManager entityManager, bool registerObjects)
    {
        ConstructedObjectSets constructedObjectSets = new();

        PureManagedObjectFactory pureManagedObjectFactory = new();
        ImmutablePureManagedObjectFactory immutablePureManagedObjectFactory = new();
        UnityObjectFactory unityObjectFactory = new(instanceIDs, existingObjects, entityManager, loadMode, registerObjects);
        PureEntityFactory pureEntityFactory = new();
        SimpleNativeTypeObjectFactory simpleNativeTypeObjectFactory = new();

        foreach (var schemaEntry in objectsBySchema)
        {
            var schema = schemaEntry.Key;
            var objectIds = schemaEntry.Value;

            var rtti = RttiResolver.GetRTTI(schema);
            switch (rtti.RttiGroup)
            {
                case RttiGroup.PureManaged:
                {
                    pureManagedObjectFactory.AddObjectsToBatch((PureManagedObjectRtti)rtti, objectIds);
                    break;
                }
                case RttiGroup.ImmutablePureManaged:
                {
                    immutablePureManagedObjectFactory.AddObjectsToBatch((ImmutablePureManagedObjectRtti)rtti, objectIds);
                    break;
                }
                case RttiGroup.UnityObject:
                {
                    unityObjectFactory.AddObjectsToBatch((UnityObjectRtti)rtti, objectIds);
                    break;
                }
                case RttiGroup.PureEntity:
                {
                    pureEntityFactory.AddObjectsToBatch((PureEntityRtti)rtti, objectIds);
                    break;
                }
                case RttiGroup.SimpleNativeType:
                {
                    simpleNativeTypeObjectFactory.AddObjectsToBatch((SimpleNativeTypeObjectRtti)rtti, objectIds);
                    break;
                }
                default:
                    break;
            }
        }

        constructedObjectSets.pureManagedObjects = pureManagedObjectFactory.FlushBatch();
        constructedObjectSets.immutablePureManagedObjects = immutablePureManagedObjectFactory.FlushBatch();
        constructedObjectSets.unityObjects = unityObjectFactory.FlushBatch();
        constructedObjectSets.pureEntities = pureEntityFactory.FlushBatch();
        constructedObjectSets.simpleNativeTypeObjects = simpleNativeTypeObjectFactory.FlushBatch();

        return constructedObjectSets;
    }

    internal static void PatchObjectPtrs(DocumentModel documentModel, ConstructedObjectSets constructedObjectSets)
    {
        Dictionary<UdmObjectId, IntPtr> allObjectPtrs = new((int)documentModel.GetObjectCount());

        foreach (ref readonly Schema schema in documentModel.GetSchemas())
        {
            var objectModelsPerSchema = documentModel.GetConstObjectModelsPerSchema(schema);

            unsafe
            {
                var objectModels = new ReadOnlySpan<ObjectModelPairEntry>(objectModelsPerSchema.Entries.ToPointer(), (int)objectModelsPerSchema.EntryCount);
                foreach (var objectModel in objectModels)
                {
                    allObjectPtrs[objectModel.ObjectId] = objectModel.Data;
                }
            }
        }

        var pureManagedObjectGroups = constructedObjectSets.pureManagedObjects.objectGroups;
        for (int i = 0; i < pureManagedObjectGroups.Count; i++)
        {
            var objectGroup = pureManagedObjectGroups[i];
            var objectIds = objectGroup.objectIds;
            objectGroup.objectPtrs = new IntPtr[objectIds.Length];
            for (int j = 0; j < objectIds.Length; j++)
            {
                objectGroup.objectPtrs[j] = allObjectPtrs[objectIds[j]];
            }
        }

        var immutablePureManagedGroups = constructedObjectSets.immutablePureManagedObjects.objectGroups;
        for (int i = 0; i < immutablePureManagedGroups.Count; i++)
        {
            var objectGroup = immutablePureManagedGroups[i];
            var objectIds = objectGroup.objectIds;
            objectGroup.objectPtrs = new IntPtr[objectIds.Length];
            for (int j = 0; j < objectIds.Length; j++)
            {
                objectGroup.objectPtrs[j] = allObjectPtrs[objectIds[j]];
            }
        }

        var allUnityObjectIds = constructedObjectSets.unityObjects.allObjectIds;
        constructedObjectSets.unityObjects.allObjectPtrs = new IntPtr[allUnityObjectIds.Length];
        for (int i = 0; i < allUnityObjectIds.Length; i++)
        {
            constructedObjectSets.unityObjects.allObjectPtrs[i] = allObjectPtrs[allUnityObjectIds[i]];
        }

        var simpleNativeTypeObjectGroups = constructedObjectSets.simpleNativeTypeObjects.objectGroups;
        for (int i = 0; i < simpleNativeTypeObjectGroups.Count; i++)
        {
            var objectGroup = simpleNativeTypeObjectGroups[i];
            var objectIds = objectGroup.objectIds;
            objectGroup.objectPtrs = new IntPtr[objectIds.Length];
            for (int j = 0; j < objectIds.Length; j++)
            {
                objectGroup.objectPtrs[j] = allObjectPtrs[objectIds[j]];
            }
        }
    }

    internal static void DeserializeObjects(DeserializeContext context, ConstructedObjectSets constructedObjectSets)
    {
        // TODO if we replace the ALF ArtifactHeader with the UDM BinaryHeader we can use
        // ObjectCollectionPerSchema instead of Dictionary<Schema, UdmObjectId[]> in our
        // ConstructObjects API
        // ObjectCollectionPerSchema contains object data offsets so we can avoid having
        // to patch them later
        PatchObjectPtrs(context.DocumentModel, constructedObjectSets);

        // Immutable objects are not available in the InstanceRegistry before deserialiation,
        // so they need to be deserialized before other groups in case they are referenced.
        ImmutablePureManagedObjectSerializer.DeserializeObjects(context, constructedObjectSets.immutablePureManagedObjects);

        PureManagedObjectSerializer.DeserializeObjects(context, constructedObjectSets.pureManagedObjects);

        UnityObjectSerializer.DeserializeObjects(context, constructedObjectSets.unityObjects);

        EcsComponentSerializer.DeserializeEntities(context, constructedObjectSets);

        SimpleNativeTypeObjectSerializer.DeserializeObjects(context, constructedObjectSets.simpleNativeTypeObjects);
    }
}
