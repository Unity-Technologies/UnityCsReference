// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;

using System.Runtime.CompilerServices;
using Unity.EntitiesLike;
using UnityEngine;
using UnityEngine.Serialization;
using RefId = System.Int64;

namespace Unity.DataModel;

sealed class ConstructedObjectSets
{
    internal ConstructedPureManagedObjectSet pureManagedObjects;
    internal ConstructedImmutablePureManagedObjectSet immutablePureManagedObjects;
    internal ConstructedUnityObjectSet unityObjects;
    internal PureEntitySet pureEntities;
    internal ConstructedSimpleNativeTypeObjectSet simpleNativeTypeObjects;
}

static class DocumentInstanceFactory
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

    internal static Dictionary<Schema, UdmObjectId[]> GroupObjectModelsBySchema(IReadOnlyCollection<ConstObjectModel> objectModels)
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
            r[kvp.Key] = [.. kvp.Value];

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

        Dictionary<UdmObjectId, (UnityEngine.Object, RefId, object)> existingManagedObjects = new();

        bool hasExistingObjects = loadMode == LoadMode.Merge && existingObjects.Count > 0;
        if (hasExistingObjects)
        {
            foreach (var existingObject in existingObjects)
            {
                var instanceId = instanceIDs[existingObject];
                var hostObj = Resources.EntityIdToObject(instanceId);

                var rtti = RttiResolver.GetRTTI(hostObj);
                if (rtti is UnityHybridObjectRtti hybridRtti)
                {
                    var managedReferenceIds = ManagedReferenceUtility.GetManagedReferenceIds(hostObj);
                    foreach (var managedReferenceId in managedReferenceIds)
                    {
                        UdmObjectId managedObjectId = existingObject.Id ^ (ulong)managedReferenceId;
                        var obj = ManagedReferenceUtility.GetManagedReference(hostObj, managedReferenceId);
                        existingManagedObjects.Add(managedObjectId, (hostObj, managedReferenceId, obj));
                    }
                }
            }
        }

        PureManagedObjectFactory pureManagedObjectFactory = new(existingManagedObjects, loadMode);

        ImmutablePureManagedObjectFactory immutablePureManagedObjectFactory = new();
        UnityObjectFactory unityObjectFactory = new(instanceIDs, existingObjects, entityManager, loadMode, registerObjects);
        PureEntityFactory pureEntityFactory = new();
        SimpleNativeTypeObjectFactory simpleNativeTypeObjectFactory = new();

        foreach (var schemaEntry in objectsBySchema)
        {
            var schema = schemaEntry.Key;
            var objectIds = schemaEntry.Value;

            var rtti = RttiResolver.GetRTTI(schema);
            if (hasExistingObjects && rtti.RttiGroup != RttiGroup.UnityObject)
            {
                foreach (var objectId in objectIds)
                {
                    if (existingManagedObjects.TryGetValue(objectId, out var entry))
                    {
                        var (hostObj, managedReferenceId, obj) = entry;
                        if (obj.GetType() != rtti.GetType())
                        {
                            existingManagedObjects.Remove(objectId);
                        }
                    }
                }
            }

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

        // Set managed references in the hosts registry
        var reference = new Reference();
        reference.DocumentId = default;

        var allObjectsIndex = 0;

        for (int i = 0; i < constructedObjectSets.unityObjects.schemaObjectCounts.Length; i++)
        {
            var rtti = constructedObjectSets.unityObjects.allRtti[i];
            var schemaObjectCount = constructedObjectSets.unityObjects.schemaObjectCounts[i];

            if (rtti is UnityHybridObjectRtti hybridRtti)
            {
                unsafe
                {
                    var objectModel = new ConstObjectModel() {
                        ObjectId = default,
                        Accessor = new ConstAccessor()
                        {
                            Schema = rtti.Schema,
                            Data = default,
                            References = (IntPtr)context.DocumentModel.GetReferences()
                        }
                    };

                    for (int j = 0; j < schemaObjectCount.ObjectCount; j++)
                    {
                        var hostObj = constructedObjectSets.unityObjects.allInstances[allObjectsIndex];
                        objectModel.ObjectId = constructedObjectSets.unityObjects.allObjectIds[allObjectsIndex];
                        objectModel.Accessor.Data = constructedObjectSets.unityObjects.allObjectPtrs[allObjectsIndex];

                        var referencedObjectModels = SerializeHelper.GetReferencedObjects(context.DocumentModel, objectModel, SerializeHelper.IsNotUnityObject);
                        foreach (var referencedObjectModel in referencedObjectModels)
                        {
                            reference.UdmObjectId = referencedObjectModel.ObjectId;
                            bool found = context.InstanceRegistry.TryGetInstance(reference, out var obj);
                            EngineHelper.AssertIsTrue(found);

                            var refId = (objectModel.ObjectId.Id ^ reference.UdmObjectId.Id) & 0x7FFFFFFFFFFFFFFF;
                            ManagedReferenceUtility.SetManagedReferenceIdForObject(hostObj, obj, (long)refId);
                        }

                        allObjectsIndex++;
                    }
                }
            }
            else
            {
                allObjectsIndex += schemaObjectCount.ObjectCount;
            }
        }

        UnityObjectSerializer.DeserializeObjects(context, constructedObjectSets.unityObjects);

        EcsComponentSerializer.DeserializeEntities(context, constructedObjectSets);

        SimpleNativeTypeObjectSerializer.DeserializeObjects(context, constructedObjectSets.simpleNativeTypeObjects);
    }
}
