// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.EntitiesLike;
using UnityEngine;
using UnityEngine.Assertions;
using Unsafe = Unity.DataModel.UnsafeHelper;

namespace Unity.DataModel;

internal static class EcsComponentSerializer
{
    internal static unsafe void SerializeEcsComponent(PureManagedObjectRtti rtti, SerializeContext context, Accessor componentAccessor, EntityId entityId, ComponentType componentType)
    {
        unsafe
        {
            if (!componentType.IsZeroSized)
            {
                byte* componentDataRawRO = (byte*)context.EntityManager.GetComponentDataRawRO(entityId, componentType.TypeIndex);
                ref byte objectModelBasePtr = ref Unsafe.AsRef<byte>(componentAccessor.Data.ToPointer());
                ref byte runtimeComponentPtr = ref Unsafe.AsRef<byte>(componentDataRawRO);

                UdmManagedSerialization.FromLiveToObjModel(ref runtimeComponentPtr, ref objectModelBasePtr, rtti.TransferData, context);
            }
        }
    }

    internal static unsafe void SerializeDynamicBuffer(PureManagedObjectRtti elementRtti, SerializeContext context,
        Accessor componentAccessor, EntityId entityId, ComponentType componentType)
    {
        if (componentType.IsZeroSized)
            return;

        ulong bufferLength = (ulong)context.EntityManager.GetBufferLength(entityId, componentType.TypeIndex);

        if (bufferLength == 0)
            return;

        byte* componentDataRawRO = (byte*)context.EntityManager.GetBufferRawRO(entityId, componentType.TypeIndex);
        var modelVector = new Vector
        {
            ElementSchema = elementRtti.Schema,
            Field = (IntPtr)componentAccessor.Data.ToPointer(),
            DocumentModel = context.Document
        };

        UdmManagedSerialization.ConvertBufferFromLiveToObjModel(componentDataRawRO, modelVector, bufferLength, elementRtti.Type, context);
    }

    internal static int SerializePureEntity(
        PureEntityRtti pureEntityRtti,
        EntityId entityId,
        UdmObjectId entityObjectModelId,
        SerializeContext context)
    {
        ObjectModel objectModel = context.Document.CreateObjectModel(pureEntityRtti.Schema, entityObjectModelId);
        if (!objectModel.IsValid())
        {
            throw new InvalidOperationException($"Unable to create object model for pure entity with objectId {entityId}");
        }

        return SerializeEntityComponents(entityId, entityObjectModelId, context);
    }

    internal static int SerializeEntityComponents(
        EntityId entityId,
        UdmObjectId entityObjectModelId,
        SerializeContext context)
    {
        int serializedCount = 0;
        if (context.EntityManager.GetComponentCount(entityId) > 0)
        {
            using var componentTypes = context.EntityManager.GetComponentTypes(entityId);
            foreach (var componentType in componentTypes)
            {
                var runtimeType = TypeManager.GetType(componentType.TypeIndex);
                if (runtimeType == typeof(Simulate) || runtimeType == typeof(EntityAllocator.SkipGlobalEntityDeallocation) || runtimeType == typeof(TransformRef))
                    continue;

                Rtti rtti = RttiResolver.GetOrAddRTTI(runtimeType); // Note: If this is a buffer, this returns the rtti for the element
                if (rtti != null)
                {
                    if (componentType.IsBuffer)
                    {
                        var elementTypeId = rtti.Schema.GetTypeId();
                        unsafe
                        {
                            // use the element to get the schema for the buffer
                            var dynamicBufferTypeId = SchemaBuilder.GetVectorTypeId(elementTypeId, VectorType.DynamicBuffer);
                            var dynamicBufferSchema = Schema.GetSchemaByType(dynamicBufferTypeId, 1);

                            // Add the buffer to the object
                            Accessor componentAccessor = context.Document.AddEcsComponent(entityObjectModelId, dynamicBufferSchema);
                            // Copy runtime buffer data to the document component data
                            SerializeDynamicBuffer((PureManagedObjectRtti)rtti, context, componentAccessor, entityId, componentType);
                        }
                    }
                    else
                    {
                        // Add component to the object
                        Accessor componentAccessor = context.Document.AddEcsComponent(entityObjectModelId, rtti.Schema);
                        // Copy runtime component data to the document component data
                        SerializeEcsComponent((PureManagedObjectRtti)rtti, context, componentAccessor, entityId, componentType);
                    }
                    ++serializedCount;
                }
                else
                {
                    Debug.LogWarning($"Couldn't find RTTI info for {runtimeType.Name}");
                }
            }
        }
        return serializedCount;
    }

    internal static unsafe void DeserializeEcsComponent(PureManagedObjectRtti rtti, DeserializeContext context, ConstAccessor constAccessor, byte* componentDataPtr)
    {
        ref var componentObjectModelBasePtr = ref Unsafe.AsRef<byte>(constAccessor.Data.ToPointer());
        ref byte runtimeComponentPtr = ref Unsafe.AsRef<byte>(componentDataPtr);

        UdmManagedSerialization.FromObjModelToLive(ref runtimeComponentPtr, ref componentObjectModelBasePtr, rtti.TransferData, context);
    }

    internal static unsafe void DeserializeDynamicBuffer(PureManagedObjectRtti elementRtti, DeserializeContext context, ConstAccessor constAccessor, BufferHeader* bufferHeaderPtr, int typeIndex)
    {
        var modelVector = constAccessor.GetVectorValue();
        var vectorSize = (int) modelVector.GetLength();

        // Resize the DynamicBuffer through the header
        var typeInfo = TypeManager.GetTypeInfo(typeIndex);
        BufferHeader.SetCapacity(bufferHeaderPtr, vectorSize, typeInfo.TypeSize, typeInfo.AlignmentInBytes, BufferHeader.TrashMode.TrashOldData, false, 0);
        bufferHeaderPtr->Length = vectorSize;

        UdmManagedSerialization.ConvertBufferFromModelToLive(bufferHeaderPtr->Pointer, modelVector, vectorSize, elementRtti.Schema, elementRtti.Type, context);
    }

    internal static void DeserializeEntityComponentsForInstanceID(EntityId entityId, UdmObjectId objectModelId, DeserializeContext context)
    {
        var components = context.DocumentModel.GetEcsComponents(objectModelId);
        foreach (var componentAccessor in components)
        {
            // This component is a DynamicBuffer
            if (componentAccessor.GetSchema().GetFlags().HasFlag(SchemaFlags.IsVector))
            {
                var elementSchema = componentAccessor.GetSchema().GetVectorElementSchema();
                var elementRtti = (PureManagedObjectRtti)RttiResolver.GetRTTI(elementSchema);

                var typeIndex = TypeManager.GetTypeIndex(elementRtti.Type);
                context.EntityManager.AddComponent(entityId, typeIndex);

                unsafe
                {
                    var bufferHeader = context.EntityManager.GetBufferHeaderRawRO(entityId, typeIndex);
                    DeserializeDynamicBuffer(elementRtti, context, componentAccessor, bufferHeader, typeIndex);
                }
            }
            else
            {

                var entityRtti = (PureManagedObjectRtti)RttiResolver.GetRTTI(componentAccessor.GetSchema());

                var runtimeType = entityRtti.Type;
                if (runtimeType == typeof(Parent))
                {
                    Reference parentReference = componentAccessor.GetReferenceFieldValue("Value");
                    var parentEntityId = context.ResolveInstanceID(parentReference);
                    context.EntityManager.SetParent(entityId, parentEntityId, false);
                    return;
                }

                var typeIndex = TypeManager.GetTypeIndex(entityRtti.Type);
                context.EntityManager.AddComponent(entityId, typeIndex);


                // Ignore Tag Components, as they do not need to copy any data
                if (!typeIndex.IsZeroSized)
                {
                    unsafe
                    {
                        var componentDataPtr = (byte*)context.EntityManager.GetComponentDataRawRW(entityId, typeIndex);
                        DeserializeEcsComponent(entityRtti, context, componentAccessor, componentDataPtr);
                    }
                }
            }
        }
    }

    internal static void DeserializeEntities(DeserializeContext context, ConstructedObjectSets constructedObjectSets)
    {
        // Collect all the instance ids for non pure entities from the calculated data
        Type gameObjectType = typeof(GameObject);
        int offset = 0;
        for (int rttiIndex = 0; rttiIndex < constructedObjectSets.unityObjects.allRtti.Length; ++rttiIndex)
        {
            var rtti = constructedObjectSets.unityObjects.allRtti[rttiIndex];
            int objectCount = constructedObjectSets.unityObjects.schemaObjectCounts[rttiIndex].ObjectCount;
            if (rtti is UnityObjectRtti unityObjectRtti && unityObjectRtti.Type == gameObjectType)
            {
                // Deserialize all the components for the selected rtti
                int endOfRange = offset + objectCount;
                for (int currentObjIndex = offset; currentObjIndex < endOfRange; ++currentObjIndex)
                {
                    // Deserialize all the components for the current object
                    var entityId = constructedObjectSets.unityObjects.allInstanceIds[currentObjIndex];
                    var objectModelId = constructedObjectSets.unityObjects.allObjectIds[currentObjIndex];

                    DeserializeEntityComponentsForInstanceID(entityId, objectModelId, context);
                }
            }
            offset += objectCount;
        }

        // Deserialize Pure Entities
        if (constructedObjectSets.pureEntities.pureEntityInstanceIds != null)
        {
            var pureInstanceIds = constructedObjectSets.pureEntities.pureEntityInstanceIds;
            var pureObjectIds = constructedObjectSets.pureEntities.pureEntityObjectIds;

            // TODO: In AllocateAndAssignChunksToExistingEntities it says that this code only supports one entity at a time. Review this.
            // InitializePureEntityChunks(pureInstanceIds, context.EntityManager);
            for (int index = 0; index < pureInstanceIds.Length; ++index)
            {
                // Initialize chunk for entity. Replace this by the batched version when possible.
                InitializePureEntityChunks(pureInstanceIds[index], context.EntityManager);
                DeserializeEntityComponentsForInstanceID(pureInstanceIds[index], pureObjectIds[index], context);
            }
        }
    }

    internal unsafe static void InitializePureEntityChunks(EntityId instanceID, EntityManager entityManager)
    {
        // TODO: In AllocateAndAssignChunksToExistingEntities it says that this code only supports one entity at a time. Review this.
        Entity entity = instanceID;
        Assert.IsTrue(EntityComponentStore.s_entityStore.Data.Exists(entity));
        var chunkAndIndex = EntityComponentStore.s_entityStore.Data.GetEntityInChunk(entity);
        if (!chunkAndIndex.Equals(EntityInChunk.Null))
            return; // This means the entity already has a chunk assigned; don't allocate a new one

        // Allocate the entities in the current world (ensure code like `new GameObject("My Object")` gets assigned to a world)
        var access = entityManager.GetCheckedEntityDataAccess();

        access->PrepareForAdditiveStructuralChanges();
        var changes = access->BeginAdditiveStructuralChanges();
        var archetype = access->GetGameObjectEntityArchetype();
        entityManager.GetCheckedEntityDataAccess()->AllocateAndAssignChunksToExistingEntities(archetype, &entity, 1);
        access->EndStructuralChanges(ref changes);
    }

    internal unsafe static void InitializePureEntityChunks(EntityId[] instanceIDs, EntityManager entityManager)
    {
        // Allocate the entities in the current world (ensure code like `new GameObject("My Object")` gets assigned to a world)
        var access = entityManager.GetCheckedEntityDataAccess();

        access->PrepareForAdditiveStructuralChanges();
        var changes = access->BeginAdditiveStructuralChanges();
        var archetype = access->GetGameObjectEntityArchetype();
        fixed (EntityId* entities = instanceIDs)
        {
            entityManager.GetCheckedEntityDataAccess()->AllocateAndAssignChunksToExistingEntities(archetype, (Entity*)entities, instanceIDs.Length);
        }
        access->EndStructuralChanges(ref changes);
    }

    internal unsafe static void GetECSComponentDocument(EntityId gameObjectInstanceID, ulong serializationOptions, int typeIndexInt, out IntPtr documentModelPtr)
    {
        TypeIndex typeIndex = typeIndexInt;
        var type = TypeManager.GetType(typeIndex);
        if (EntityManager.TryGetWorldUnmanaged(gameObjectInstanceID, out var worldUnmanaged))
        {
            var entityManager = worldUnmanaged.EntityManager;
            var documentModel = DocumentModel.CreateNew();
            var udmObjectIDGenerator = new NoCollisionUDMObjectIDGenerator();
            var serializeContext = new SerializeContext(documentModel, (SerializeInstructionFlags)serializationOptions, udmObjectIDGenerator, entityManager);

            PureManagedObjectRtti rtti = (PureManagedObjectRtti)RttiResolver.GetOrAddRTTI(type);

            var objectModel = serializeContext.Document.CreateObjectModel(rtti.Schema, gameObjectInstanceID.GetRawData());
            if (!typeIndex.IsZeroSized)
            {
                var componentDataRawRO = (byte*)serializeContext.EntityManager.GetComponentDataRawRO(gameObjectInstanceID, typeIndex);

                ref byte runtimeComponentPtr = ref Unsafe.AsRef<byte>(componentDataRawRO);
                ref byte objectModelBasePtr = ref Unsafe.AsRef<byte>(objectModel.GetAccessor().Data.ToPointer());
                UdmManagedSerialization.FromLiveToObjModel(ref runtimeComponentPtr, ref objectModelBasePtr, rtti.TransferData, serializeContext);
            }

            documentModelPtr = documentModel.DocumentPtr;
        }
        else
        {
            documentModelPtr = IntPtr.Zero;
            Debug.LogError($"Failed to retrieve the Ecs component of type {type.FullName} to game object with type index {typeIndex}. The Ecs world has already been disposed");
            return;
        }
    }

    internal static unsafe void SetECSComponentDocument(EntityId gameObjectInstanceID, int typeIndexInt, IntPtr documentModelPtr, EntityId dataEntityId)
    {
        if (EntityManager.TryGetWorldUnmanaged(gameObjectInstanceID, out var worldUnmanaged))
        {
            var entityManager = worldUnmanaged.EntityManager;
            var document = (DocumentModel)documentModelPtr;
            if (document.GetObjectCount() != 1)
            {
                var type = TypeManager.GetType(typeIndexInt);
                Debug.LogError($"Failed to set the Ecs component of type {type.FullName} to game object with type index {typeIndexInt}. More than one object was serialized.");
                return;
            }

            var deserializeContext = new DeserializeContext(document, entityManager);
            var objectModel = document.GetConstObjectModel(dataEntityId.GetRawData());

            var rtti = (PureManagedObjectRtti)RttiResolver.GetRTTI(objectModel.GetSchema());
            if (rtti == null)
            {
                var type = TypeManager.GetType(typeIndexInt);
                Debug.LogError($"Failed to set the Ecs component of type {type.FullName} to game object with type index {typeIndexInt}. Rtti was invalid.");
                return;
            }

            TypeIndex otherTypeIndex = TypeManager.GetTypeIndex(rtti.Type);

            if (rtti.Type == typeof(Parent))
            {
               var componentAccessor = objectModel.GetAccessor();
               Reference parentReference = componentAccessor.GetReferenceFieldValue("Value");
               var parentEntityId = deserializeContext.ResolveInstanceID(parentReference);
               deserializeContext.EntityManager.SetParent(gameObjectInstanceID, parentEntityId, false);
               return;
            }
            else if (rtti.Type == typeof(Child))
            {
                // We don't support IBufferElementData yet in UDM documents and SetParent should update the children dynamic buffer of the parent
                // So no need to Add the child component to the parent
                return;
            }

            if (!deserializeContext.EntityManager.HasComponent(gameObjectInstanceID, otherTypeIndex))
                deserializeContext.EntityManager.AddComponent(gameObjectInstanceID, otherTypeIndex);

            // We need to skip tags, as we can't call GetComponentDataRawRW on them
            if (!otherTypeIndex.IsZeroSized)
            {
                var componentDataPtr = (byte*)deserializeContext.EntityManager.GetComponentDataRawRW(gameObjectInstanceID, otherTypeIndex);
                EcsComponentSerializer.DeserializeEcsComponent(rtti, deserializeContext, objectModel.GetAccessor(), componentDataPtr);
            }
        }
        else
        {
            var type = TypeManager.GetType(typeIndexInt);
            Debug.LogError($"Failed to set the Ecs component of type {type.FullName} to game object with type index {typeIndexInt}. The Ecs world has already been disposed");
        }
    }
}
