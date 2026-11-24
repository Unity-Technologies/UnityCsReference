// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary>
    /// Provides an interface to safely perform a subset of <see cref="EntityManager"/> operations from a single worker thread, by temporarily
    /// giving that thread exclusive access to a <see cref="World"/>'s <see cref="EntityManager"/>.
    /// </summary>
    /// <remarks>
    /// The intended use case for this interface is to let a worker thread stream Entity data into a staging <see cref="World"/>
    /// and perform any necessary post-processing structural changes, prior to moving the final data into the main simulation
    /// world using <see cref="EntityManager.MoveEntitiesFrom"/>. This lets the main thread continue to safely process
    /// the primary world while the new data is loading in, and not require a main-thread sync point until the new
    /// Entities are fully loaded and ready to be injected.
    ///
    /// **Warning:** Only the <see cref="EntityManager"/> operations exposed by this struct are supported within the context
    /// of an exclusive entity transaction. For more information, refer to [Safety in Entities](xref:concepts-safety).
    /// </remarks>
    /// <seealso cref="EntityManager.BeginExclusiveEntityTransaction"/>
    /// <seealso cref="EntityManager.EndExclusiveEntityTransaction"/>
    [NativeContainer]
    public unsafe struct ExclusiveEntityTransaction
    {
        [NativeDisableUnsafePtrRestriction]
        EntityDataAccess* m_Access;
        // The World's handle for this transaction, which lets the job safety system track the job that holds it
        AtomicSafetyHandle m_Safety;
        // Numbers this transaction among the World's, so a holder of an ended one is refused
        uint m_Transaction;

        /// <summary>
        /// Return the entity manager this transaction operates upon.
        /// </summary>
        /// <remarks>
        /// Do not use this property to invoke arbitrary methods from an ExclusiveEntityTransaction because it is undefined
        /// behavior. Some EntityManager methods involve operations that are not
        /// safe to perform from worker threads, or which might work in certain contexts but fail in others.
        /// Only the operations exposed by the <see cref="ExclusiveEntityTransaction"/> struct are guaranteed
        /// to function correctly.
        /// </remarks>
        public EntityManager EntityManager
        {
            get
            {
                return EntityManager.ExclusiveTransactionHolder(m_Access, m_Safety);
            }
        }

        internal ExclusiveEntityTransaction(EntityDataAccess* access)
        {
            m_Safety = access->DependencyManager->Safety.ExclusiveTransactionSafety;
            m_Access = access;
            m_Transaction = access->DependencyManager->ExclusiveTransaction;
        }

        // The native ExclusiveEntityTransaction (Modules/Entities/Public/ExclusiveEntityTransaction.h) has the same layout
        internal static int GetNativeLayoutForStructMatchCheck(Span<int> values)
        {
            ExclusiveEntityTransaction transaction = default;
            var start = (byte*)&transaction;
            var i = 0;
            values[i++] = sizeof(ExclusiveEntityTransaction);
            values[i++] = (int)((byte*)&transaction.m_Access - start);
            values[i++] = (int)((byte*)&transaction.m_Safety - start);
            values[i++] = (int)((byte*)&transaction.m_Transaction - start);
            return i;
        }

        internal EntityDataAccess* GetCheckedEntityDataAccess()
        {
            CheckIsActive();
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            return m_Access;
        }

        EntityDataAccess* GetCheckedEntityDataAccess(Entity entity)
        {
            var access = GetCheckedEntityDataAccess();
            access->EntityComponentStore->AssertEntityBelongsToThisWorld(entity);
            return access;
        }

        EntityDataAccess* GetCheckedEntityDataAccess(Entity* entities, int count)
        {
            var access = GetCheckedEntityDataAccess();
            access->EntityComponentStore->AssertEntitiesBelongToThisWorld(entities, count);
            return access;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckIsActive()
        {
            // Ending the transaction releases its handle, so a released one means m_Access may already be freed
            if (Hint.Unlikely(!AtomicSafetyHandle.IsHandleValid(m_Safety)))
                throw new InvalidOperationException("The exclusive entity transaction has already ended.");
            if (Hint.Unlikely(m_Access == null || !m_Access->IsInExclusiveTransaction || m_Access->DependencyManager->ExclusiveTransaction != m_Transaction))
                throw new InvalidOperationException("The exclusive entity transaction has already ended.");
        }

        bool HasComponent(Entity entity, TypeIndex typeIndex)
        {
            var archetype = GetCheckedEntityDataAccess()->EntityComponentStore->GetArchetypeInThisWorldOrNull(entity, out _);
            return archetype != null && ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex) != -1;
        }

        void DestroyEntity(Entity* entities, int count)
        {
            var access = GetCheckedEntityDataAccess(entities, count);
            var changes = access->BeginStructuralChanges();
            access->DestroyEntityInternalDuringStructuralChange(entities, count);
            access->EndStructuralChanges(ref changes);
        }

        internal EntityArchetype CreateArchetype(ComponentType* types, int count)
        {
            var access = GetCheckedEntityDataAccess();
            access->PrepareForAdditiveStructuralChanges();
            var changes = access->BeginAdditiveStructuralChanges();
            var result = access->CreateArchetype(types, count, true);
            access->CheckIsAdditiveArchetypeStructuralChangePossible(result.Archetype);
            access->EndStructuralChanges(ref changes);
            return result;
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.CreateArchetype(ComponentType[])"/>
        public EntityArchetype CreateArchetype(params ComponentType[] types)
        {
            if (types == null)
                throw new NullReferenceException(nameof(types));

            fixed (ComponentType* typesPtr = types)
                return CreateArchetype(typesPtr, types.Length);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.CreateEntity(EntityArchetype)"/>
        public Entity CreateEntity(EntityArchetype archetype)
        {
            var access = GetCheckedEntityDataAccess();
            access->PrepareForAdditiveStructuralChanges(archetype.Archetype);
            var changes = access->BeginAdditiveStructuralChanges();
            var result = access->CreateEntityDuringStructuralChange(archetype);
            access->EndStructuralChanges(ref changes);
            return result;
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.CreateEntity(EntityArchetype,NativeArray{Entity})"/>
        public void CreateEntity(EntityArchetype archetype, NativeArray<Entity> entities)
        {
            var access = GetCheckedEntityDataAccess();
            access->PrepareForAdditiveStructuralChanges(archetype.Archetype);
            var changes = access->BeginAdditiveStructuralChanges();
            access->CreateEntityDuringStructuralChange(archetype, entities);
            access->EndStructuralChanges(ref changes);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.CreateEntity(ComponentType[])"/>
        public Entity CreateEntity(params ComponentType[] types)
        {
            return CreateEntity(CreateArchetype(types));
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.Instantiate(Entity)"/>
        public Entity Instantiate(Entity srcEntity)
        {
            Entity entity;
            var access = GetCheckedEntityDataAccess(srcEntity);
            var archetype = access->EntityComponentStore->GetArchetype(srcEntity);
            access->PrepareForAdditiveStructuralChanges(archetype->InstantiateArchetype);
            var changes = access->BeginAdditiveStructuralChanges();
            access->InstantiateInternalDuringStructuralChange(srcEntity, &entity, 1);
            access->EndStructuralChanges(ref changes);
            return entity;
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.Instantiate(Entity,NativeArray{Entity})"/>
        public void Instantiate(Entity srcEntity, NativeArray<Entity> outputEntities)
        {
            var access = GetCheckedEntityDataAccess(srcEntity);
            var archetype = access->EntityComponentStore->GetArchetype(srcEntity);
            access->PrepareForAdditiveStructuralChanges(archetype->InstantiateArchetype);
            var changes = access->BeginAdditiveStructuralChanges();
            access->InstantiateInternalDuringStructuralChange(srcEntity, (Entity*)outputEntities.GetUnsafePtr(), outputEntities.Length);
            access->EndStructuralChanges(ref changes);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.DestroyEntity(NativeArray{Entity})"/>
        public void DestroyEntity(NativeArray<Entity> entities)
        {
            DestroyEntity((Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.DestroyEntity(NativeSlice{Entity})"/>
        public void DestroyEntity(NativeSlice<Entity> entities)
        {
            DestroyEntity((Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.DestroyEntity(Entity)"/>
        public void DestroyEntity(Entity entity)
        {
            DestroyEntity(&entity, 1);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.AddComponent(Entity,ComponentType)"/>
        public void AddComponent(Entity entity, ComponentType componentType)
        {
            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            access->AddComponentDuringStructuralChange(entity, componentType);
            access->EndStructuralChanges(ref changes);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.AddBuffer{T}(Entity)"/>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        public DynamicBuffer<T> AddBuffer<T>(Entity entity) where T : unmanaged, IBufferElementData
        {
            AddComponent(entity, ComponentType.ReadWrite<T>());
            return GetBuffer<T>(entity);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.HasBuffer{T}(Entity)"/>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        public bool HasBuffer<T>(Entity entity) where T : struct, IBufferElementData
        {
            return HasComponent(entity, TypeManager.GetTypeIndex<T>());
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.RemoveComponent(Entity,ComponentType)"/>
        public void RemoveComponent(Entity entity, ComponentType type)
        {
            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            access->RemoveComponentDuringStructuralChange(entity, type);
            access->EndStructuralChanges(ref changes);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.Exists(Entity)"/>
        public bool Exists(Entity entity)
        {
            return GetCheckedEntityDataAccess()->EntityComponentStore->ExistsInThisWorld(entity);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.HasComponent(Entity,ComponentType)"/>
        public bool HasComponent(Entity entity, ComponentType type)
        {
            return HasComponent(entity, type.TypeIndex);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.GetComponentData{T}(Entity)"/>
        public T GetComponentData<T>(Entity entity) where T : unmanaged, IComponentData
        {
            return GetCheckedEntityDataAccess(entity)->GetComponentData<T>(entity);
        }

        /// <summary>
        /// Sets the value of a component of an entity.
        /// </summary>
        /// <remarks>
        /// The target entity must exist, and it must have the target component.
        /// </remarks>
        /// <example>
        /// <code lang="csharp" source="../DocCodeSamples.Tests/GeneralComponentExamples.cs" region="set-component-single-entity" title="SetComponentData Example"/>
        /// </example>
        /// <param name="entity">The entity whose <typeparamref name="T"/> component should be modified.</param>
        /// <param name="componentData">The value to assign to <paramref name="entity"/>'s <typeparamref name="T"/> component.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        public void SetComponentData<T>(Entity entity, T componentData) where T : unmanaged, IComponentData
        {
            GetCheckedEntityDataAccess(entity)->SetComponentData(entity, componentData);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.GetSharedComponent{T}(Entity)"/>
        public T GetSharedComponent<T>(Entity entity) where T : unmanaged, ISharedComponentData
        {
            return GetCheckedEntityDataAccess(entity)->GetSharedComponentData_Unmanaged<T>(entity);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.SetSharedComponent{T}(Entity,T)"/>
        public void SetSharedComponent<T>(Entity entity, T componentData) where T : unmanaged, ISharedComponentData
        {
            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            var defaultValue = default(T);
            access->SetSharedComponentData_Unmanaged(entity, TypeManager.GetTypeIndex<T>(), UnsafeUtility.AddressOf(ref componentData), UnsafeUtility.AddressOf(ref defaultValue));
            access->EndStructuralChanges(ref changes);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.SetSharedComponent{T}(NativeArray{Entity},T)"/>
        public void SetSharedComponent<T>(NativeArray<Entity> entities, T componentData) where T : unmanaged, ISharedComponentData
        {
            var access = GetCheckedEntityDataAccess((Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length);
            var changes = access->BeginStructuralChanges();
            var defaultValue = default(T);
            access->SetSharedComponentData_Unmanaged(entities, TypeManager.GetTypeIndex<T>(), UnsafeUtility.AddressOf(ref componentData), UnsafeUtility.AddressOf(ref defaultValue));
            access->EndStructuralChanges(ref changes);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.AddSharedComponent{T}(NativeArray{ArchetypeChunk},T)"/>
        internal void AddSharedComponent<T>(NativeArray<ArchetypeChunk> chunks, T componentData)
            where T : unmanaged, ISharedComponentData
        {
            var access = GetCheckedEntityDataAccess();
            if (StructuralChangesProfiler.Enabled)
            {
                access->StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in access->m_WorldUnmanaged);
                access->StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.SetSharedComponent, in access->m_WorldUnmanaged);
            }
            var changes = access->BeginStructuralChanges();
            int sharedComponentIndex = access->InsertSharedComponent_Unmanaged(componentData);
            access->AddSharedComponentDataDuringStructuralChange(chunks, sharedComponentIndex, ComponentType.ReadWrite<T>());
            access->EndStructuralChanges(ref changes);
            if (StructuralChangesProfiler.Enabled)
            {
                access->StructuralChangesRecorder.End(); // SetSharedComponent
                access->StructuralChangesRecorder.End(); // AddComponent
            }
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.AddSharedComponent{T}(Entity,T)"/>
        public bool AddSharedComponent<T>(Entity entity, T componentData)  where T : unmanaged, ISharedComponentData
        {
            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            var defaultValue = default(T);
            var result = access->AddSharedComponentDataDuringStructuralChange_Unmanaged(entity, ComponentType.ReadWrite<T>(), UnsafeUtility.AddressOf(ref componentData), UnsafeUtility.AddressOf(ref defaultValue));
            access->EndStructuralChanges(ref changes);
            return result;
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.AddSharedComponent{T}(NativeArray{Entity},T)"/>
        public void AddSharedComponent<T>(NativeArray<Entity> entities, T componentData)  where T : unmanaged, ISharedComponentData
        {
            var access = GetCheckedEntityDataAccess((Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length);
            var changes = access->BeginStructuralChanges();
            var defaultValue = default(T);
            access->AddSharedComponentDataDuringStructuralChange_Unmanaged(entities, ComponentType.ReadWrite<T>(), UnsafeUtility.AddressOf(ref componentData), UnsafeUtility.AddressOf(ref defaultValue));
            access->EndStructuralChanges(ref changes);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.GetBuffer{T}(Entity,bool)"/>
        public DynamicBuffer<T> GetBuffer<T>(Entity entity, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return EntityManager.GetBufferInternal<T>(GetCheckedEntityDataAccess(entity), entity, isReadOnly);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.SwapComponents(ArchetypeChunk,int,ArchetypeChunk,int)"/>
        public void SwapComponents(ArchetypeChunk leftChunk, int leftIndex, ArchetypeChunk rightChunk, int rightIndex)
        {
            GetCheckedEntityDataAccess()->SwapComponents(leftChunk, leftIndex, rightChunk, rightIndex);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.SetComponentEnabled(Entity,ComponentType,bool)"/>
        public void SetComponentEnabled(Entity entity, ComponentType componentType, bool value)
        {
            GetCheckedEntityDataAccess(entity)->SetComponentEnabled(entity, componentType.TypeIndex, value);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.SetComponentEnabled{T}(Entity,bool)"/>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleEnableableComponent) })]
        public void SetComponentEnabled<T>(Entity entity, bool value)  where T : unmanaged, IEnableableComponent
        {
            GetCheckedEntityDataAccess(entity)->SetComponentEnabled(entity, TypeManager.GetTypeIndex<T>(), value);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.IsComponentEnabled(Entity,ComponentType)"/>
        public bool IsComponentEnabled(Entity entity, ComponentType componentType)
        {
            return GetCheckedEntityDataAccess(entity)->IsComponentEnabled(entity, componentType.TypeIndex);
        }

        /// <inheritdoc cref="Unity.Entities.EntityManager.IsComponentEnabled{T}(Entity)"/>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleEnableableComponent) })]
        public bool IsComponentEnabled<T>(Entity entity)where T : unmanaged, IEnableableComponent
        {
            return GetCheckedEntityDataAccess(entity)->IsComponentEnabled(entity, TypeManager.GetTypeIndex<T>());
        }


        // The untyped forms of GlobalEntityManager's methods that the native ExclusiveEntityTransaction needs

        internal void* GetComponentDataPtrRO(Entity entity, ComponentType type)
        {
            GlobalEntityManager.AssertCanPointToComponentData(type.TypeIndex);
            var store = GetCheckedEntityDataAccess(entity)->EntityComponentStore;
            store->AssertEntityHasComponent(entity, type.TypeIndex);
            return store->GetComponentDataWithTypeRO(entity, type.TypeIndex);
        }

        internal void* GetComponentDataPtrRW(Entity entity, ComponentType type)
        {
            GlobalEntityManager.AssertCanPointToComponentData(type.TypeIndex);
            var store = GetCheckedEntityDataAccess(entity)->EntityComponentStore;
            store->AssertEntityHasComponent(entity, type.TypeIndex);
            return store->GetComponentDataWithTypeRW(entity, type.TypeIndex, store->GlobalSystemVersion);
        }

        internal UnsafeUntypedDynamicBuffer GetBuffer(Entity entity, ComponentType type, bool isReadOnly) =>
            GlobalEntityManager.GetBuffer(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex, isReadOnly);

        internal ComponentObjectState GetComponentObjectState(Entity entity, ComponentType type) =>
            GlobalEntityManager.GetCheckedStore(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex)->GetComponentObjectState(entity, type.TypeIndex);

        internal void SetComponentObjectState(Entity entity, ComponentType type, in ComponentObjectState state) =>
            GlobalEntityManager.GetCheckedStore(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex)->SetComponentObjectState(entity, type.TypeIndex, state);

        internal bool HasComponentObjectState(Entity entity, ComponentType type) =>
            GlobalEntityManager.GetCheckedStore(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex)->HasComponentObjectState(entity, type.TypeIndex);

        // Temporary until GameObject entities are allocated in their World when the GameObject is created. Gives an engine
        // id without a chunk one in this World. Returns false when the id is unknown or already has a chunk in any World.
        internal bool AttachGameObjectEntity(Entity entity) =>
            GameObjectComponentStoreBridge.AttachGameObjectEntity(GetCheckedEntityDataAccess(), entity);

        internal NativeArray<ComponentType> GetComponentTypes(Entity entity, AllocatorManager.AllocatorHandle allocator)
        {
            var store = GetCheckedEntityDataAccess(entity)->EntityComponentStore;
            store->AssertEntitiesExist(&entity, 1);
            return new EntityArchetype { Archetype = store->GetArchetype(entity) }.GetComponentTypes(allocator);
        }
    }
}
