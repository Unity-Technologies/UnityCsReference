// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

using Unity.EntitiesLike;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.DataModel;

// It is important that the value in this enum match with the ones in TransferInstructionFlags in native
[Flags]
internal enum SerializeInstructionFlags : ulong
{
    NoSerializeInstructionFlags = 0,
    ReadWriteFromSerializedFile = 1UL << 0, // Are we reading or writing a serialized file
    AssetMetaDataOnly = 1UL << 1, // Only serialize data needed for .meta files
    IsCloningObject = 1UL << 7,
    ResolveStreamedResourceSources = 1UL << 10,
    SerializeForPrefabSystem = 1UL << 14,
    SerializeEditorMinimalScene = 1UL << 21,
    AllowTextSerialization = 1UL << 31,
    IgnoreSerializeReferenceMissingTypes = 1ul << 32
}

[RequiredByNativeCode]
internal static class SerializeHelper
{
    private static Dictionary<UdmObjectId, EntityId> AllocateNonPersistentInstanceIDs(HashSet<ConstObjectModel> objectModels)
    {
        List<UdmObjectId> nativeObjectIds = new();
        foreach (var referencedObjectModel in objectModels)
        {
            var refRtti = RttiResolver.GetRTTI(referencedObjectModel.GetSchema().GetTypeId());
            if (refRtti == null || refRtti is not UnityObjectRtti || refRtti.RttiGroup != RttiGroup.UnityObject)
                continue;
            nativeObjectIds.Add(referencedObjectModel.GetObjectID());
        }

        var length = nativeObjectIds.Count;        
        var nativeObjectInstanceIds = new EntityId[length];
        for (int i = 0; i < length; ++i)
            nativeObjectInstanceIds[i] = EntityId.AllocateNextLowestEntityId();

        var instanceIdDictionary = new Dictionary<UdmObjectId, EntityId>(nativeObjectIds.Count);
        for (int i = 0; i < nativeObjectIds.Count; i++)
            instanceIdDictionary[nativeObjectIds[i]] = nativeObjectInstanceIds[i];

        return instanceIdDictionary;
    }

    // These functions are meant only to be used in very particular systems (Undo,...)
    // If you need to use any of the functions in this region/group, reach out to the UDM team first.
    #region Only_For_Internal_Usage
    [RequiredByNativeCode]
    internal static void ReadFromObjectModelWithUnityObject(UnityEngine.Object obj, DocumentModel document, ulong objectId, ulong options, IntPtr resolverPtr)
    {
        if (obj == null) return;

        var objectModel = document.GetConstObjectModel(objectId);
        if (!objectModel.IsValid()) return;

        var rtti = (UnityObjectRtti)RttiResolver.GetRTTI(objectModel.GetSchema());
        if (rtti == null) return;

        UDMRefResolver? resolver = null;
        if (resolverPtr != IntPtr.Zero)
        {
            resolver = new UDMRefResolver(resolverPtr);
        }

        SerializeInstructionFlags serializeFlags = (SerializeInstructionFlags)options;
        DeserializeContext deserializationContext = new DeserializeContext(document, new EntityManager(), serializeFlags, resolver);

        if (rtti is UnityHybridObjectRtti hybridRtti)
        {
            // Get managed reference objects
            var referencedObjectModels = SerializeHelper.GetReferencedObjects(deserializationContext.DocumentModel, objectModel, SerializeHelper.IsNotUnityObject);

            // Construct managed objects
            // Use ConstructAndMergeObjects so it populates the ConstructedUnityObjectSet in order for
            // DeserializeObjects to populate the ManagedReferencesRegistry
            /*
            referencedObjectModels.Add(objectModel);
            var objectsBySchema = DocumentInstanceFactory.GroupObjectModelsBySchema(referencedObjectModels);
#if UNITY_EDITOR
            obj.GetInstanceIdFast(out var instanceID); // GetInstanceIdFast() can be called on any thread
            var instanceIds = new Dictionary<UdmObjectId, EntityId>() { { objectId, instanceID } };
#else
            var instanceIds = new Dictionary<UdmObjectId, EntityId>() { { objectId, obj.GetEntityId() } };
#endif
            var existingObjects = new HashSet<UdmObjectId>() { objectId };
            var constructedObjectSets = DocumentInstanceFactory.ConstructAndMergeObjectsInternal(LoadMode.Merge, objectsBySchema, instanceIds, existingObjects, deserializationContext.EntityManager, false);
            */

            // TODO we can't use ConstructAndMergeObjects because it uses InstanceIDToObject to resolve instances
            // causing recursive loading, it should use the reference resolver from the DeserializeContext
            // populate the ConstructedUnityObjectSet manually
            var constructedObjectSets = DocumentInstanceFactory.ConstructObjects(referencedObjectModels, default, deserializationContext.EntityManager);
            constructedObjectSets.unityObjects.schemaObjectCounts = new SchemaObjectCount[] { new SchemaObjectCount {
                Schema = objectModel.GetSchema(),
                ObjectCount = 1,
                FirstExistingObjectIndex = 0
            }};
            constructedObjectSets.unityObjects.allRtti = new UnityObjectRtti[] { rtti };
            obj.GetInstanceIdFast(out var instanceID); // GetInstanceIdFast() can be called on any thread
            constructedObjectSets.unityObjects.allInstanceIds = new EntityId[] { instanceID };
            constructedObjectSets.unityObjects.allObjectIds = new UdmObjectId[] { objectId };
            constructedObjectSets.unityObjects.allInstances = new UnityEngine.Object[] { obj };

            // Register native and managed objects
            LoadUdm.RegisterInstances(deserializationContext, constructedObjectSets);

            // Deserialize native and managed objects
            DocumentInstanceFactory.DeserializeObjects(deserializationContext, constructedObjectSets);
        }
        else
        {
            // Register native object
            var reference = new Reference();
            reference.DocumentId = default;
            reference.UdmObjectId = objectId;
            deserializationContext.SetInstance(reference, obj);

            // Deserialize native object
            UnityObjectSerializer.DeserializeObject(rtti, deserializationContext, objectModel, obj);
        }
    }

    internal static UnityEngine.Object[] ReadFromDocument(DocumentModel document)
    {
        var objects = LoadUdm.CreateLiveObjectsFromDocumentInternal(document, ActiveGameObjectWorld.World.EntityManager, SerializeInstructionFlags.ReadWriteFromSerializedFile, false).unityObjects.allInstances;
        return objects;
    }

    private static void CloneObjectsInternal(ref NativeArray<EntityId> sourceObjects, ref NativeArray<EntityId> cloneObjects, int count)
    {
        var serializeOptions = SerializeInstructionFlags.SerializeForPrefabSystem | SerializeInstructionFlags.IsCloningObject | SerializeInstructionFlags.IgnoreSerializeReferenceMissingTypes;

        using var udmObjectIDGenerator = new NoCollisionUDMObjectIDGenerator();

        UdmWriteData[] writeData = new UdmWriteData[count];
        UdmObjectId[] udmObjectIds = new UdmObjectId[count];
        for (int i = 0; i < count; i++)
        {
            var objectId = udmObjectIDGenerator.NextUDMObjectID();
            udmObjectIds[i] = objectId;
            writeData[i] = new UdmWriteData { objectId = objectId, instanceId = sourceObjects[i], isStrippedObject = 0 };
        }

        // Serialize. Missing references to non-persistent objects outside of the document are preserved.
        using var document = SaveUdm.CreateDocumentFromLiveObjectsInternal(writeData, [], serializeOptions, true, true);

        // Deserialize. The preserved missing references are resolved. Objects are registered so they can be accessed and modified in native code before they are awoken.
        var constructedObjects = LoadUdm.CreateLiveObjectsFromDocumentInternal(document, ActiveGameObjectWorld.World != null ? ActiveGameObjectWorld.World.EntityManager : default, serializeOptions, true, true).unityObjects;

        // Deserialization does not retain the order of objects, so we need to sort them manually.
        for (int i = 0; i < count; ++i)
        {
            var cloneIndex = Array.FindIndex(constructedObjects.allObjectIds, x => x == udmObjectIds[i]);
            if (cloneIndex == -1)
            {
                EngineHelper.LogWarning("Failed to clone object with instance ID " + sourceObjects[i]);
                continue;
            }

            cloneObjects[i] = constructedObjects.allInstanceIds[cloneIndex];
        }
    }

    [RequiredByNativeCode]
    internal static unsafe void CloneObjects(IntPtr sourceInstanceIds, IntPtr outCloneInstanceIds, int count)
    {
        var sourceObjects = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(sourceInstanceIds.ToPointer(), count, Allocator.Invalid);
        var cloneObjects = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(outCloneInstanceIds.ToPointer(), count, Allocator.Invalid);

        var safetyHandleSource = AtomicSafetyHandle.Create();
        var safetyHandleClones = AtomicSafetyHandle.Create();
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref sourceObjects, safetyHandleSource);
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref cloneObjects, safetyHandleClones);
        CloneObjectsInternal(ref sourceObjects, ref cloneObjects, count);

        AtomicSafetyHandle.Release(safetyHandleSource);
        AtomicSafetyHandle.Release(safetyHandleClones);
    }
    #endregion

    internal static T ReadPureManaged<T>(ConstObjectModel dataObjectModel, DocumentModel document, EntityManager entityManager = default)
    {
        return ReadPureManaged<T>(dataObjectModel, new DeserializeContext(document, entityManager));
    }

    internal static T ReadPureManaged<T>(ConstAccessor accessor, DocumentModel document, EntityManager entityManager = default)
    {
        return ReadPureManaged<T>(accessor, new DeserializeContext(document, entityManager));
    }


    internal static HashSet<ConstObjectModel> GetReferencedObjects(
        DocumentModel documentModel,
        ConstObjectModel objectModel,
        Func<ConstObjectModel, bool>? predicate = null)
    {
        var referencedObjects = new HashSet<ConstObjectModel> { objectModel };
        GetReferencedObjectsInternal(documentModel, objectModel.GetAccessor(), referencedObjects, predicate);
        referencedObjects.Remove(objectModel);
        return referencedObjects;
    }

    internal static HashSet<ConstObjectModel> GetReferencedObjects(
        DocumentModel documentModel,
        ConstAccessor accessor,
        Func<ConstObjectModel, bool>? predicate = null)
    {
        var referencedObjects = new HashSet<ConstObjectModel>();
        GetReferencedObjectsInternal(documentModel, accessor, referencedObjects, predicate);
        return referencedObjects;
    }

    internal static void GetReferencedObjectsInternal(
        DocumentModel documentModel,
        ConstAccessor accessor,
        HashSet<ConstObjectModel> referencedObjects,
        Func<ConstObjectModel, bool>? predicate = null)
    {
        Traversal.TraverseReferences(ref accessor, currentAccessor =>
        {
            var reference = currentAccessor.GetReferenceValue();
            if (reference.IsInternal())
            {
                var fieldObject = documentModel.GetConstObjectModel(reference.UdmObjectId);
                if (fieldObject.IsValid())
                {
                    if (predicate == null || predicate(fieldObject))
                    {
                        if (referencedObjects.Add(fieldObject))
                        {
                            GetReferencedObjectsInternal(documentModel, fieldObject.GetAccessor(), referencedObjects, predicate);
                        }
                    }
                }
            }
        });
    }

    // TODO determine this from the schema?
    internal static bool IsPureManaged(ConstObjectModel objectModel)
    {
        var rtti = RttiResolver.GetRTTI(objectModel.GetSchema());
        return rtti.RttiGroup == RttiGroup.PureManaged || rtti.RttiGroup == RttiGroup.ImmutablePureManaged;
    }

    internal static bool IsNotUnityObject(ConstObjectModel objectModel)
    {
        var rtti = RttiResolver.GetRTTI(objectModel.GetSchema());
        return rtti.RttiGroup != RttiGroup.UnityObject;
    }

    internal static T ReadPureManaged<T>(ConstObjectModel dataObjectModel, DeserializeContext deserializationContext)
    {
        return ReadPureManaged<T>(dataObjectModel.GetAccessor(), dataObjectModel.GetObjectID(), deserializationContext);
    }

    internal static T ReadPureManaged<T>(ConstAccessor accessor, DeserializeContext deserializationContext)
    {
        return ReadPureManaged<T>(accessor, default, deserializationContext);
    }

    private static T ReadPureManaged<T>(ConstAccessor accessor, UdmObjectId rootObjectId, DeserializeContext deserializationContext)
    {
        accessor.ThrowIfInvalid();

        if (!deserializationContext.DocumentModel.IsValid())
            throw new InvalidOperationException("There needs to be a valid document in the deserialization context");

        var mainSchema = accessor.GetSchema();
        if (mainSchema.IsReference())
        {
            // We are pointing to a field and we should actually deserialize the object that is being pointed at
            var reference = accessor.GetReferenceValue();
            if (!reference.IsValid())
                throw new InvalidOperationException("Accessor is an invalid reference");
            if (reference.IsExternal())
                throw new InvalidOperationException("Accessor is a reference to an external object. This function doesn't resolve external references.");
            var referencedObjectModel = deserializationContext.DocumentModel.GetConstObjectModel(reference.UdmObjectId);
            return ReadPureManaged<T>(referencedObjectModel.GetAccessor(), deserializationContext);
        }

        // Try resolve the RTTI from the schema first as T may be a base class or interface
        var mainRtti = RttiResolver.GetRTTI(mainSchema);
        if (mainRtti == null)
            throw new InvalidOperationException($"There is no RTTI available for type {typeof(T)}");

        var runtimeType = mainRtti.Type;
        if (!typeof(T).IsAssignableFrom(runtimeType))
            throw new InvalidOperationException($"Can't read accessor ('{runtimeType.FullName}') as type '{typeof(T).FullName}'");

        if (mainRtti.RttiGroup != RttiGroup.PureManaged && mainRtti.RttiGroup != RttiGroup.ImmutablePureManaged)
            throw new Exception("Only pure managed types can be deserialised in this way. Objects dependent on UnityObjects, Native types or Entities will fail.");

        // Instantiate managed reference objects
        var referencedObjects = GetReferencedObjects(deserializationContext.DocumentModel, accessor, IsPureManaged);

        // There could be a circular reference to the root object and we want to deserialize it individually
        // For that reason we delete the root from referencedObjects
        if (rootObjectId != default)
        {
            ConstObjectModel potentialRoot = new ConstObjectModel { Accessor = accessor, ObjectId = rootObjectId };
            referencedObjects.Remove(potentialRoot);
        }

        var constructedObjectSets = DocumentInstanceFactory.ConstructObjects(referencedObjects, default, deserializationContext.EntityManager);

        // Don't resolve external references
        deserializationContext.AllowResolveExternalReferences = false;
        LoadUdm.RegisterInstances(deserializationContext, constructedObjectSets);

        // Instantiate main object
        object returnValue;
        switch (mainRtti.RttiGroup)
        {
            case RttiGroup.ImmutablePureManaged:
                returnValue = ImmutablePureManagedObjectSerializer.InstantiateFromModel((ImmutablePureManagedObjectRtti)mainRtti, accessor, deserializationContext);
                break;

            case RttiGroup.PureManaged:
                returnValue = PureManagedObjectFactory.Instantiate((PureManagedObjectRtti)mainRtti, 1)[0];
                // Register root object, in case some other object refers to it
                if (rootObjectId != default)
                {
                    var reference = new Reference { DocumentId = default, UdmObjectId = rootObjectId };
                    deserializationContext.SetInstance(reference, returnValue);
                }
                break;

            default:
                throw new InvalidOperationException($"Unsupported RTTI group '{mainRtti.RttiGroup}' for type '{typeof(T).FullName}'. Only PureManaged and ImmutablePureManaged are supported.");
        }

        // Deserialize managed reference objects
        DocumentInstanceFactory.DeserializeObjects(deserializationContext, constructedObjectSets);

        // Deserialize main object
        if (mainRtti.RttiGroup == RttiGroup.PureManaged)
        {
            UdmManagedSerialization.ReadFromAccessor(((PureManagedObjectRtti)mainRtti).TransferData, accessor, returnValue, deserializationContext);
        }
        return (T)returnValue;
    }

    internal static UdmObjectId WriteObjectToDocument(object data, SerializeContext context)
    {
        UdmObjectId objectId = context.ObjectIDGenerator.NextUDMObjectID();

        context.AddObjectForSerialization(data, objectId);
        context.Serialize();
        return objectId;
    }

    internal static UdmObjectId WriteToDocument<T>(ref T data, SerializeContext context)
        where T : unmanaged
    {
        // Let's do a simple implementation here (it does boxing for value types) and revisit it later
        return WriteObjectToDocument((object)data, context);
    }
}
