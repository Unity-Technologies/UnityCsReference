// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.EntitiesLike;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.DataModel;

internal struct InstanceIdExistsPair
{
    internal EntityId instanceId;
    internal bool exists;
}

internal struct NativeTypeSchemaCount
{
    internal Int32 PersistentTypeID;
    internal int SchemaCount;
}

internal struct SchemaObjectCount
{
    internal Schema Schema;
    internal int ObjectCount;
    internal int FirstExistingObjectIndex;
}

internal struct ConstructedUnityObjectSet
{
    internal SchemaObjectCount[] schemaObjectCounts;
    internal UnityObjectRtti[] allRtti;
    internal EntityId[] allInstanceIds;
    internal UdmObjectId[] allObjectIds;
    internal UnityEngine.Object[] allInstances;
    internal IntPtr[] allObjectPtrs;
}

internal sealed class UnityObjectFactory
{
    private readonly Dictionary<UdmObjectId, EntityId> instanceIDs;
    private readonly HashSet<UdmObjectId> existingObjects;
    private readonly EntityManager entityManager;
    private readonly bool registerObjects;
    private readonly LoadMode loadMode;
    private int nativeObjectCount = 0;
    private int nativeSchemaCount = 0;
    private Dictionary<Int32, Dictionary<UnityObjectRtti, UdmObjectId[]>> objectsByNativeType = new Dictionary<Int32, Dictionary<UnityObjectRtti, UdmObjectId[]>>();

    internal UnityObjectFactory(Dictionary<UdmObjectId, EntityId> instanceIDs, HashSet<UdmObjectId> existingObjects, EntityManager entityManager, LoadMode loadMode, bool registerObjects = false)
    {
        this.loadMode = loadMode;
        this.instanceIDs = instanceIDs;
        this.existingObjects = existingObjects;
        this.entityManager = entityManager;
        this.registerObjects = registerObjects;
    }

    internal void AddObjectsToBatch(UnityObjectRtti rtti, UdmObjectId[] objectIds)
    {
        var persistentTypeID = rtti.PersistentTypeID;

        if (!objectsByNativeType.TryGetValue(persistentTypeID, out var nativeTypeEntry))
        {
            nativeTypeEntry = new Dictionary<UnityObjectRtti, UdmObjectId[]>();
            objectsByNativeType[persistentTypeID] = nativeTypeEntry;
        }
        nativeTypeEntry[rtti] = objectIds;

        nativeObjectCount += objectIds.Length;
        nativeSchemaCount++;
    }

    internal ConstructedUnityObjectSet FlushBatch()
    {
        bool hasExistingObjects = loadMode == LoadMode.Merge && existingObjects.Count > 0;
        int nativeTypeCount = objectsByNativeType.Count;
        var nativeTypeSchemaCounts = new NativeTypeSchemaCount[nativeTypeCount];
        var schemaObjectCounts = new SchemaObjectCount[nativeSchemaCount];
        var allRtti = new UnityObjectRtti[nativeSchemaCount];
        var allObjectIds = new UdmObjectId[nativeObjectCount];

        int schemaIndex = 0;
        int allObjectsIndex = 0;

        var gameObjectRtti = (UnityObjectRtti)RttiResolver.GetRTTI<GameObject>();
        var gameObjectPersistentTypeID = gameObjectRtti.PersistentTypeID;
        int gameObjectInstanceIdStart = 0;
        int gameObjectInstanceIdEnd = 0;

        var nativeTypeIndex = 0;
        foreach (var nativeTypeEntry in objectsByNativeType)
        {
            var persistentTypeID = nativeTypeEntry.Key;
            var objectsByRtti = nativeTypeEntry.Value;

            foreach (var rttiEntry in objectsByRtti)
            {
                var rtti = rttiEntry.Key;
                var objectIds = rttiEntry.Value;

                int firstExistingObjectIndex = objectIds.Length;
                if (hasExistingObjects)
                {
                    // move existing objects to the end
                    Array.Sort(objectIds, (id1, id2) =>
                    {
                        bool exists1 = existingObjects.Contains(id1);
                        bool exists2 = existingObjects.Contains(id2);
                        return exists1.CompareTo(exists2);
                    });

                    firstExistingObjectIndex = Array.FindIndex(objectIds, id => existingObjects.Contains(id));
                    if (firstExistingObjectIndex == -1)
                    {
                        firstExistingObjectIndex = objectIds.Length;
                    }
                }

                schemaObjectCounts[schemaIndex] = new SchemaObjectCount
                {
                    Schema = rtti.Schema,
                    ObjectCount = objectIds.Length,
                    FirstExistingObjectIndex = firstExistingObjectIndex
                };
                allRtti[schemaIndex] = rtti;

                schemaIndex++;

                var beforeAllObjectsIndex = allObjectsIndex;
                foreach (var objectId in objectIds)
                {
                    allObjectIds[allObjectsIndex] = objectId;
                    allObjectsIndex++;
                }

                if (persistentTypeID == gameObjectPersistentTypeID)
                {
                    gameObjectInstanceIdStart = beforeAllObjectsIndex;
                    gameObjectInstanceIdEnd = gameObjectInstanceIdStart + firstExistingObjectIndex;
                }
            }

            nativeTypeSchemaCounts[nativeTypeIndex] = new NativeTypeSchemaCount
            {
                PersistentTypeID = persistentTypeID,
                SchemaCount = objectsByRtti.Count,
            };

            nativeTypeIndex++;
        }

        var allInstanceIds = new EntityId[allObjectIds.Length];
        for (int i = 0; i != allObjectIds.Length; ++i)
            allInstanceIds[i] = instanceIDs[allObjectIds[i]];

        var allInstances = new UnityEngine.Object[allInstanceIds.Length];

        allObjectsIndex = 0;

        // Instantiate managed object uninitialized
        for (int i = 0; i < schemaObjectCounts.Length; i++)
        {
            var schemaObjectCount = schemaObjectCounts[i];
            var rtti = allRtti[i];

            for (int j = 0; j < schemaObjectCount.ObjectCount; j++)
            {
                if (j < schemaObjectCount.FirstExistingObjectIndex)
                {
                    allInstances[allObjectsIndex] = (UnityEngine.Object)RuntimeHelpers.GetUninitializedObject(rtti.Type);
                }
                else
                {
                    // TODO convert to InstanceIDToObjectList
                    allInstances[allObjectsIndex] = Resources.EntityIdToObject(allInstanceIds[allObjectsIndex]);
                    EngineHelper.AssertIsTrue(allInstances[allObjectsIndex] != null);
                }

                allObjectsIndex++;
            }
        }

        // Instantiate native objects
        // We are only allowed to register objects on the main thread
        allObjectsIndex = 0;

        for (int i = 0; i < schemaObjectCounts.Length; i++)
        {
            var schemaObjectCount = schemaObjectCounts[i];
            var rtti = allRtti[i];

            if (rtti is UnityHybridObjectRtti hybridRtti && hybridRtti.Constructor != null)
            {
                for (int j = 0; j < schemaObjectCount.ObjectCount; j++)
                {
                    // We need to call the constructor for cases like AdditionalCameraData, where we got:
                    //      [NonSerialized] TaaPersistentData m_TaaPersistentData = new TaaPersistentData();
                    // If we don't call the constructor, m_TaaPersistentData will be empty
                    // TODO: We should try to find a better way to do this, instead of using reflection (https://jira.unity3d.com/browse/CPN-862)
                    // Note, we can't use Activator.CreateInstance() here, as it doesn't find all constructors
                    // Example is UnityEngine.UI.Toggle, that has a protected constructor that isn't found
                    hybridRtti.Constructor.Invoke(allInstances[allObjectsIndex], null);

                    allObjectsIndex++;
                }
            }
            else
            {
                allObjectsIndex += schemaObjectCount.ObjectCount;
            }
        }

        // Chunks have been allocated already during object registration
        if (!registerObjects && gameObjectInstanceIdStart != gameObjectInstanceIdEnd)
        {
            // Assets don't require an Entities World for now
            if (entityManager.IsCreated)
            {
                unsafe
                {
                    var access = entityManager.GetCheckedEntityDataAccess();
                    access->PrepareForAdditiveStructuralChanges();
                    var changes = access->BeginAdditiveStructuralChanges();
                    var archetype = access->GetGameObjectEntityArchetype();

                    fixed (EntityId* nativeInstanceIds = allInstanceIds)
                    {
                        for (int index = gameObjectInstanceIdStart; index < gameObjectInstanceIdEnd; ++index)
                        {
                            entityManager.GetCheckedEntityDataAccess()->AllocateAndAssignChunksToExistingEntities(archetype, (Entity*)&nativeInstanceIds[index], 1);
                        }
                    }
                    access->EndStructuralChanges(ref changes);
                }
            }
        }

        return new ConstructedUnityObjectSet
        {
            schemaObjectCounts = schemaObjectCounts,
            allRtti = allRtti,
            allInstanceIds = allInstanceIds,
            allObjectIds = allObjectIds,
            allInstances = allInstances
        };
    }
}
