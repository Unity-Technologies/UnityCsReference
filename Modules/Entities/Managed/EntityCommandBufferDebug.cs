// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{

    [DebuggerTypeProxy(typeof(EntityCommandBufferDebugView))]
    public unsafe partial struct EntityCommandBuffer
    {
        internal const int INITIAL_COMMANDS_CAPACITY = 8;

        private struct ECBPlaybackWithTrace
        {
            public static readonly SharedStatic<int> Ref = SharedStatic<int>.GetOrCreate<EntityCommandBuffer, ECBPlaybackWithTrace>();
        }

        static readonly SharedStatic<int> _PLAYBACK_WITH_TRACE = ECBPlaybackWithTrace.Ref;

        /// <summary>
        /// A static field for logging details during EntityCommandBuffer playback.
        /// When set to true, each EntityCommandBuffer will log its commands as they are processed during playback.
        /// </summary>
        public static bool PLAYBACK_WITH_TRACE
        {
            get => _PLAYBACK_WITH_TRACE.Data != 0;
            set => _PLAYBACK_WITH_TRACE.Data = value ? 1 : 0;
        }

        private struct ECBPrePlaybackValidation
        {
            public static readonly SharedStatic<int> Ref = SharedStatic<int>.GetOrCreate<EntityCommandBuffer, ECBPrePlaybackValidation>();
        }

        static readonly SharedStatic<int> _ENABLE_PRE_PLAYBACK_VALIDATION = ECBPrePlaybackValidation.Ref;

        internal static bool ENABLE_PRE_PLAYBACK_VALIDATION
        {
            get => _ENABLE_PRE_PLAYBACK_VALIDATION.Data != 0;
            set => _ENABLE_PRE_PLAYBACK_VALIDATION.Data = value ? 1 : 0;
        }

        internal struct PlaybackWithTraceProcessor : IEcbProcessor
        {
            private const string kMsgUnknownSystem = "unknown system type";

            public PlaybackProcessor playbackProcessor;
            public EntityDataAccess* mgr;
            public SystemHandle originSystem;
            public FixedString128Bytes originSystemDebugName;
            public FixedString128Bytes currentSystemDebugName;

            public void Init(EntityDataAccess* entityDataAccess, EntityCommandBufferData* data, in SystemHandle originSystemHandle)
            {
                playbackProcessor.Init(entityDataAccess, data, originSystemHandle);

                mgr = entityDataAccess;
                originSystem = originSystemHandle;
                var systemState = mgr->m_WorldUnmanaged.ResolveSystemState(originSystem);
                ExtractDebugName(systemState, out originSystemDebugName);
                ExtractDebugName(mgr->m_WorldUnmanaged.ResolveSystemState(mgr->m_WorldUnmanaged.ExecutingSystem), out currentSystemDebugName);

                Debug.Log($"Starting EntityCommandBuffer playback in {currentSystemDebugName}; recorded from {originSystemDebugName}.");
            }

            public void Cleanup()
            {
                playbackProcessor.Cleanup();
                Debug.Log($"Ending EntityCommandBuffer playback in {currentSystemDebugName}; recorded from {originSystemDebugName}.");
            }

            internal static void ExtractDebugName(SystemState* systemState, out FixedString128Bytes fixedString)
            {
                if (systemState != null)
                    fixedString = new FixedString128Bytes(systemState->DebugName);
                else
                    fixedString = kMsgUnknownSystem;
            }

            public void LogEntitiesAndComponentsCommand(Entity* entities, int count, in ComponentTypeSet typeSet,
                in FixedString64Bytes commandAction)
            {
                for (var i = 0; i < count; i++)
                {
                    LogEntityAndComponentsCommand(entities[i], typeSet, commandAction);
                }
            }

            public void LogEntitiesAndComponentCommand(Entity* entities, int count, TypeIndex typeIndex,
                in FixedString64Bytes commandAction)
            {
                for (var i = 0; i < count; i++)
                {
                    LogEntityAndComponentCommand(entities[i], typeIndex, commandAction);
                }
            }

            public void LogEntityAndComponentsCommand(Entity entity, ComponentTypeInArchetype* types, int count,
                in FixedString64Bytes commandAction)
            {
                for (var i = 0; i < count; i++)
                {
                    LogEntityAndComponentCommand(entity, types[i].TypeIndex, commandAction);
                }
            }

            public void LogEntityAndComponentsCommand(Entity entity, in ComponentTypeSet typeSet,
                in FixedString64Bytes commandAction)
            {
                for (var i = 0; i < typeSet.Length; i++)
                {
                    LogEntityAndComponentCommand(entity, typeSet.UnsafeTypesPtrRO[i], commandAction);
                }
            }

            public void LogEntityAndComponentCommand(Entity cmdEntity, TypeIndex typeIndex, in FixedString64Bytes commandAction)
            {
                mgr->GetName(cmdEntity, out var entityName);

                Debug.Log($"{commandAction} component on entity {entityName}({cmdEntity.Index},{cmdEntity.Version}) for component index {typeIndex.ToFixedString()}; recorded from {originSystemDebugName}.");
            }

            public void LogEntitiesOnlyCommand(Entity* entities, int count, in FixedString64Bytes commandAction)
            {
                for (var i = 0; i < count; i++)
                {
                    LogEntityOnlyCommand(entities[i], commandAction);
                }
            }

            public void LogEntityOnlyCommand(Entity cmdEntity, in FixedString64Bytes commandAction)
            {
                mgr->GetName(cmdEntity, out var entityName);

                Debug.Log($"{commandAction} entity {entityName}({cmdEntity.Index},{cmdEntity.Version}); recorded from {originSystemDebugName}.");
            }

            public void LogLinkedEntityGroupCommand(Entity cmdEntity, TypeIndex typeIndex, in EntityQueryMask mask, in FixedString64Bytes commandAction)
            {
                mgr->GetName(cmdEntity, out var entityName);
                var linkedTypeIndex = TypeManager.GetTypeIndex<LinkedEntityGroup>();
                using var linkedEntities = mgr->GetBuffer<LinkedEntityGroup>(cmdEntity
                        , mgr->DependencyManager->Safety.GetSafetyHandle(linkedTypeIndex, false),
                        mgr->DependencyManager->Safety.GetBufferSafetyHandle(linkedTypeIndex)
                    ).Reinterpret<Entity>()
                    .ToNativeArray(mgr->m_WorldUnmanaged.UpdateAllocator.ToAllocator);

                // Filter the linked entities based on the mask
                foreach (var e in linkedEntities)
                {
                    if (mask.MatchesIgnoreFilter(e))
                    {
                        Debug.Log($"{commandAction} component to {entityName}({cmdEntity.Index},{cmdEntity.Version})'s linked entity ({e.Index},{e.Version}) for component index {typeIndex.ToFixedString()}; recorded from {originSystemDebugName}.");
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DestroyEntity(BasicCommand* header)
            {
                var cmd = (EntityCommand*)header;
                FixedString64Bytes commandAction = "Destroying";
                LogEntityOnlyCommand(cmd->Entity, in commandAction);
                playbackProcessor.DestroyEntity(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveComponent(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;
                FixedString64Bytes commandAction = "Removing";
                LogEntityAndComponentCommand(cmd->Header.Entity, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.RemoveComponent(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveMultipleComponents(BasicCommand* header)
            {
                var cmd = (EntityMultipleComponentsCommand*)header;
                FixedString64Bytes commandAction = "Removing";
                LogEntityAndComponentsCommand(cmd->Header.Entity, cmd->TypeSet, in commandAction);
                playbackProcessor.RemoveMultipleComponents(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CreateEntity(ECBPlaybackState* ecbPlaybackState, BasicCommand* header)
            {
                var cmd = (CreateCommand*)header;

                Debug.Log($"Creating {cmd->EntityCount} entity; recorded from {originSystemDebugName}.");
                playbackProcessor.CreateEntity(ecbPlaybackState, header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void InstantiateEntity(BasicCommand* header)
            {
                var cmd = (EntityCommand*)header;
                mgr->GetName(cmd->Entity, out var entityName);

                Debug.Log($"Instantiating {cmd->EntityCount} instance(s) of entity {entityName}({cmd->Entity.Index},{cmd->Entity.Version}); recorded from {originSystemDebugName}.");
                playbackProcessor.InstantiateEntity(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddComponent(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;
                FixedString64Bytes commandAction = "Adding";
                LogEntityAndComponentCommand(cmd->Header.Entity, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.AddComponent(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddMultipleComponents(BasicCommand* header)
            {
                var cmd = (EntityMultipleComponentsCommand*)header;
                FixedString64Bytes commandAction = "Adding";
                LogEntityAndComponentsCommand(cmd->Header.Entity, cmd->TypeSet, in commandAction);
                playbackProcessor.AddMultipleComponents(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetComponent(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;
                FixedString64Bytes commandAction = "Setting";
                LogEntityAndComponentCommand(cmd->Header.Entity, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.SetComponent(header);
            }

            public void SetEnabled(BasicCommand* header)
            {
                var cmd = (EntityEnabledCommand*)header;
                mgr->GetName(cmd->Header.Entity, out var entityName);
                FixedString32Bytes enabled = cmd->IsEnabled == 0 ? "DISABLED" : "ENABLED";

                Debug.Log($"Setting entity {entityName}({cmd->Header.Entity.Index},{cmd->Header.Entity.Version}) to {enabled}; recorded from {originSystemDebugName}.");
                playbackProcessor.SetEnabled(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetComponentEnabled(BasicCommand* header)
            {
                var cmd = (EntityComponentEnabledCommand*)header;
                mgr->GetName(cmd->Header.Header.Entity, out var entityName);
                FixedString32Bytes enabled = cmd->Header.IsEnabled == 0 ? "FALSE" : "TRUE";

                Debug.Log($"Setting component enableable on entity {entityName}({cmd->Header.Header.Entity.Index},{cmd->Header.Header.Entity.Version}) for component index {cmd->ComponentTypeIndex.ToFixedString()} to {enabled}; recorded from {originSystemDebugName}.");
                playbackProcessor.SetComponentEnabled(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetName(BasicCommand* header)
            {
                var cmd = (EntityNameCommand*)header;
                mgr->GetName(cmd->Header.Entity, out var entityName);

                Debug.Log($"Setting name on entity {entityName}({cmd->Header.Entity.Index},{cmd->Header.Entity.Version}) with name {cmd->Name}; recorded from {originSystemDebugName}.");
                playbackProcessor.SetName(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddBuffer(BasicCommand* header)
            {
                var cmd = (EntityBufferCommand*)header;
                FixedString64Bytes commandAction = "Adding dynamic buffer";
                LogEntityAndComponentCommand(cmd->Header.Entity, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.AddBuffer(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetBuffer(BasicCommand* header)
            {
                var cmd = (EntityBufferCommand*)header;
                FixedString64Bytes commandAction = "Setting dynamic buffer";
                LogEntityAndComponentCommand(cmd->Header.Entity, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.SetBuffer(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendToBuffer(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;
                FixedString64Bytes commandAction = "Appending element to dynamic buffer";
                LogEntityAndComponentCommand(cmd->Header.Entity, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.AppendToBuffer(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddComponentForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommand*)header;
                FixedString64Bytes commandAction = "Adding";
                Debug.Log($"{commandAction} component on entity query for component index {cmd->ComponentTypeIndex.ToFixedString()}; recorded from {originSystemDebugName}.");
                playbackProcessor.AddComponentForEntityQuery(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddComponentForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesComponentCommand*)header;
                FixedString64Bytes commandAction = "Adding";
                LogEntitiesAndComponentCommand(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.AddComponentForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveComponentForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommand*)header;
                FixedString64Bytes commandAction = "Removing";
                Debug.Log($"{commandAction} component on entity query for component index {cmd->ComponentTypeIndex.ToFixedString()}; recorded from {originSystemDebugName}.");
                playbackProcessor.RemoveComponentForEntityQuery(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveComponentForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesComponentCommand*)header;
                FixedString64Bytes commandAction = "Removing";
                LogEntitiesAndComponentCommand(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.RemoveComponentForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddMultipleComponentsForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesAndComponentsCommand*)header;
                FixedString64Bytes commandAction = "Adding";
                LogEntitiesAndComponentsCommand(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->TypeSet, in commandAction);
                playbackProcessor.AddMultipleComponentsForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddMultipleComponentsForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentTypeSetCommand*)header;
                FixedString64Bytes commandAction = "Adding";
                Debug.Log($"{commandAction} component on entity query for component set {cmd->TypeSet}; recorded from {originSystemDebugName}.");
                playbackProcessor.AddMultipleComponentsForEntityQuery(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveMultipleComponentsForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesAndComponentsCommand*)header;
                FixedString64Bytes commandAction = "Removing";
                LogEntitiesAndComponentsCommand(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->TypeSet, in commandAction);
                playbackProcessor.RemoveMultipleComponentsForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveMultipleComponentsForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentTypeSetCommand*)header;
                FixedString64Bytes commandAction = "Removing";
                Debug.Log($"{commandAction} component on entity query for component set {cmd->TypeSet}; recorded from {originSystemDebugName}.");
                playbackProcessor.RemoveMultipleComponentsForEntityQuery(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DestroyMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesCommand*)header;
                FixedString64Bytes commandAction = "Destroying";
                LogEntitiesOnlyCommand(cmd->Entities.Ptr, cmd->EntitiesCount, in commandAction);
                playbackProcessor.DestroyMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DestroyForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryCommand*)header;
                Debug.Log($"Destroying entities in EntityQuery recorded from {originSystemDebugName}.");
                playbackProcessor.DestroyMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddComponentLinkedEntityGroup(BasicCommand* header)
            {
                var cmd = (EntityQueryMaskCommand*) header;
                FixedString64Bytes commandAction = "Adding";
                LogLinkedEntityGroupCommand(cmd->Header.Header.Entity, cmd->Header.ComponentTypeIndex, cmd->Mask,
                    commandAction);

                playbackProcessor.AddComponentLinkedEntityGroup(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetComponentLinkedEntityGroup(BasicCommand* header)
            {
                var cmd = (EntityQueryMaskCommand*) header;
                FixedString64Bytes commandAction = "Setting";
                LogLinkedEntityGroupCommand(cmd->Header.Header.Entity, cmd->Header.ComponentTypeIndex, cmd->Mask,
                    commandAction);

                playbackProcessor.SetComponentLinkedEntityGroup(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReplaceComponentLinkedEntityGroup(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*) header;
                FixedString64Bytes commandAction = "Replacing";
                mgr->GetName(cmd->Header.Entity, out var entityName);
                var linkedTypeIndex = TypeManager.GetTypeIndex<LinkedEntityGroup>();
                using var linkedEntities = mgr->GetBuffer<LinkedEntityGroup>(cmd->Header.Entity
                        , mgr->DependencyManager->Safety.GetSafetyHandle(linkedTypeIndex, false),
                        mgr->DependencyManager->Safety.GetBufferSafetyHandle(linkedTypeIndex)
                    ).Reinterpret<Entity>()
                    .ToNativeArray(mgr->m_WorldUnmanaged.UpdateAllocator.ToAllocator);

                // Filter the linked entities based on the mask
                foreach (var e in linkedEntities)
                {
                    if (mgr->HasComponent(e, ComponentType.FromTypeIndex(cmd->ComponentTypeIndex)))
                    {
                        Debug.Log($"{commandAction} component to {entityName}({cmd->Header.Entity.Index},{cmd->Header.Entity.Version})'s linked entity ({e.Index},{e.Version}) for component index {cmd->ComponentTypeIndex.ToFixedString()}; recorded from {originSystemDebugName}.");
                    }
                }

                playbackProcessor.ReplaceComponentLinkedEntityGroup(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddUnmanagedSharedComponentData(BasicCommand* header)
            {
                var cmd = (EntityUnmanagedSharedComponentCommand*)header;
                FixedString64Bytes commandAction = "Adding unmanaged shared";
                LogEntityAndComponentCommand(cmd->Header.Entity, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.AddUnmanagedSharedComponentData(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetUnmanagedSharedComponentData(BasicCommand* header)
            {
                var cmd = (EntityUnmanagedSharedComponentCommand*)header;
                FixedString64Bytes commandAction = "Setting unmanaged shared";
                LogEntityAndComponentCommand(cmd->Header.Entity, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.SetUnmanagedSharedComponentData(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddUnmanagedSharedComponentValueForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesCommand_WithUnmanagedSharedComponent*)header;
                FixedString64Bytes commandAction = "Adding unmanaged shared";
                LogEntitiesAndComponentCommand(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.AddUnmanagedSharedComponentValueForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddUnmanagedSharedComponentValueForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommandWithUnmanagedSharedComponent*)header;
                FixedString64Bytes commandAction = "Adding unmanaged shared";
                Debug.Log($"{commandAction} component on entity query for component index {cmd->Header.ComponentTypeIndex.ToFixedString()}; recorded from {originSystemDebugName}.");
                playbackProcessor.AddUnmanagedSharedComponentValueForEntityQuery(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetUnmanagedSharedComponentValueForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesCommand_WithUnmanagedSharedComponent*)header;
                FixedString64Bytes commandAction = "Setting unmanaged shared";
                LogEntitiesAndComponentCommand(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->ComponentTypeIndex, in commandAction);
                playbackProcessor.SetUnmanagedSharedComponentValueForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetUnmanagedSharedComponentValueForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommandWithUnmanagedSharedComponent*)header;
                FixedString64Bytes commandAction = "Setting unmanaged shared";
                Debug.Log($"{commandAction} component on entity query for component index {cmd->Header.ComponentTypeIndex.ToFixedString()}; recorded from {originSystemDebugName}.");
                playbackProcessor.SetUnmanagedSharedComponentValueForEntityQuery(header);
            }

            public ECBProcessorType ProcessorType => ECBProcessorType.PlaybackWithTraceProcessor;
        }

        internal struct PrePlaybackValidationProcessor : IEcbProcessor
        {
            private const string kMsgUnknownSystem = "unknown system type";

            public PlaybackProcessor playbackProcessor;
            public EntityDataAccess* mgr;

            public void Init(EntityDataAccess* entityDataAccess, EntityCommandBufferData* data, in SystemHandle originSystemHandle)
            {
                playbackProcessor.Init(entityDataAccess, data, originSystemHandle);

                mgr = entityDataAccess;
            }

            public void Cleanup()
            {
                playbackProcessor.Cleanup();
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private void ThrowIfPrefabComponentInArchetype(EntityArchetype archetype)
            {
                //The Prefab component can not be added or removed in an ECB.
                for(int i = 0; i < archetype.TypesCount; i++)
                {
                    if( archetype.Types[i].ToComponentType() == ComponentType.ReadWrite<Prefab>())
                        throw new InvalidOperationException($"Cannot create an entity with a Prefab component in an EntityCommandBuffer.");
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private void ThrowIfPrefabComponent(ComponentType type)
            {
                //The Prefab component can not be added or removed in an ECB.
                if (type == ComponentType.ReadWrite<Prefab>())
                {
                    throw new InvalidOperationException($"Cannot add or remove the Prefab component to an entity within an EntityCommandBuffer.");
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private void ThrowIfPrefabComponentInSet(in ComponentTypeSet typeSet)
            {
                for (var i = 0; i < typeSet.Length; i++)
                {
                    ThrowIfPrefabComponent(typeSet.GetComponentType(i));
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private void ThrowIfPrefab(Entity e)
            {
                //Entities with the Prefab component can not be created or destroyed in an ECB.
                if (mgr->HasComponent(e, ComponentType.ReadWrite<Prefab>()))
                {
                    throw new InvalidOperationException($"Cannot create, destroy, add, or remove components an entity that has the Prefab tag within an EntityCommandBuffer.");
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private void ThrowIfPrefab(Entity* entities, int entityCount)
            {
                for (int len = entityCount, i = 0; i < len; ++i)
                {
                    ThrowIfPrefab(entities[i]);
                }

            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private void ThrowIfPrefabInSetOfEntities(NativeArray<Entity> entities)
            {
                for (var i = 0; i < entities.Length; i++)
                {
                    ThrowIfPrefab(entities[i]);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DestroyEntity(BasicCommand* header)
            {
                var cmd = (EntityCommand*)header;

                ThrowIfPrefab(cmd->Entity);

                playbackProcessor.DestroyEntity(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveComponent(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;

                ThrowIfPrefab(cmd->Header.Entity);
                ThrowIfPrefabComponent(ComponentType.FromTypeIndex(cmd->ComponentTypeIndex));

                playbackProcessor.RemoveComponent(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveMultipleComponents(BasicCommand* header)
            {
                var cmd = (EntityMultipleComponentsCommand*)header;

                var componentTypes = cmd->TypeSet;
                ThrowIfPrefab(cmd->Header.Entity);
                ThrowIfPrefabComponentInSet(in componentTypes);

                playbackProcessor.RemoveMultipleComponents(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CreateEntity(ECBPlaybackState* ecbPlaybackState, BasicCommand* header)
            {
                var cmd = (CreateCommand*)header;

                EntityArchetype at = cmd->Archetype;
                if (!at.Valid)
                    at = mgr->GetEntityAndSimulateArchetype();
                ThrowIfPrefabComponentInArchetype(at);

                playbackProcessor.CreateEntity(ecbPlaybackState, header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void InstantiateEntity(BasicCommand* header)
            {
                var cmd = (EntityCommand*)header;
                playbackProcessor.InstantiateEntity(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddComponent(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;

                var componentType = ComponentType.FromTypeIndex(cmd->ComponentTypeIndex);
                ThrowIfPrefab(cmd->Header.Entity);
                ThrowIfPrefabComponent(componentType);

                playbackProcessor.AddComponent(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddMultipleComponents(BasicCommand* header)
            {
                var cmd = (EntityMultipleComponentsCommand*)header;

                ThrowIfPrefab(cmd->Header.Entity);
                ThrowIfPrefabComponentInSet(in cmd->TypeSet);

                playbackProcessor.AddMultipleComponents(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetComponent(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;

                ThrowIfPrefab(cmd->Header.Entity);

                playbackProcessor.SetComponent(header);
            }

            public void SetEnabled(BasicCommand* header)
            {
                var cmd = (EntityEnabledCommand*)header;

                ThrowIfPrefab(cmd->Header.Entity);

                playbackProcessor.SetEnabled(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetComponentEnabled(BasicCommand* header)
            {
                var cmd = (EntityComponentEnabledCommand*)header;

                ThrowIfPrefab(cmd->Header.Header.Entity);

                playbackProcessor.SetComponentEnabled(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetName(BasicCommand* header)
            {
                var cmd = (EntityNameCommand*)header;

                ThrowIfPrefab(cmd->Header.Entity);

                playbackProcessor.SetName(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddBuffer(BasicCommand* header)
            {
                var cmd = (EntityBufferCommand*)header;

                ThrowIfPrefab(cmd->Header.Entity);

                playbackProcessor.AddBuffer(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetBuffer(BasicCommand* header)
            {
                var cmd = (EntityBufferCommand*)header;

                ThrowIfPrefab(cmd->Header.Entity);

                playbackProcessor.SetBuffer(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendToBuffer(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;

                ThrowIfPrefab(cmd->Header.Entity);

                playbackProcessor.AppendToBuffer(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddComponentForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommand*)header;

                var componentType = ComponentType.FromTypeIndex(cmd->ComponentTypeIndex);
                ThrowIfPrefabComponent(componentType);

                // TODO: throw if any matching archetypes have the Prefab component
                //ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->Header.SkipDeferredEntityLookup != 0);

                playbackProcessor.AddComponentForEntityQuery(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddComponentForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesComponentCommand*)header;

                var componentType = ComponentType.FromTypeIndex(cmd->ComponentTypeIndex);
                ThrowIfPrefabComponent(componentType);

                ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount);

                playbackProcessor.AddComponentForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveComponentForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommand*)header;

                var componentType = ComponentType.FromTypeIndex(cmd->ComponentTypeIndex);
                ThrowIfPrefabComponent(componentType);

                // TODO: throw if any matching archetypes have the Prefab component
                //ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->Header.SkipDeferredEntityLookup != 0);

                playbackProcessor.RemoveComponentForEntityQuery(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveComponentForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesComponentCommand*)header;

                var componentType = ComponentType.FromTypeIndex(cmd->ComponentTypeIndex);
                ThrowIfPrefabComponent(componentType);

                ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount);

                playbackProcessor.RemoveComponentForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddMultipleComponentsForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesAndComponentsCommand*)header;

                ThrowIfPrefabComponentInSet(in cmd->TypeSet);

                ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount);

                playbackProcessor.AddMultipleComponentsForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddMultipleComponentsForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentTypeSetCommand*)header;

                ThrowIfPrefabComponentInSet(in cmd->TypeSet);

                // TODO: throw if any matching archetypes have the Prefab component
                //ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->Header.SkipDeferredEntityLookup != 0);

                playbackProcessor.AddMultipleComponentsForEntityQuery(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveMultipleComponentsForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesAndComponentsCommand*)header;

                ThrowIfPrefabComponentInSet(in cmd->TypeSet);

                ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount);

                playbackProcessor.RemoveMultipleComponentsForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveMultipleComponentsForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentTypeSetCommand*)header;

                ThrowIfPrefabComponentInSet(in cmd->TypeSet);

                // TODO: throw if any matching archetypes have the Prefab component
                //ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->Header.SkipDeferredEntityLookup != 0);

                playbackProcessor.RemoveMultipleComponentsForEntityQuery(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DestroyMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesCommand*)header;

                ThrowIfPrefab(cmd->Entities.Ptr, cmd->EntitiesCount);

                playbackProcessor.DestroyMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DestroyForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryCommand*)header;

                // TODO: throw if any matching archetypes have the Prefab component
                //ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->Header.SkipDeferredEntityLookup != 0);

                playbackProcessor.DestroyForEntityQuery(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddComponentLinkedEntityGroup(BasicCommand* header)
            {
                var cmd = (EntityQueryMaskCommand*) header;

                ThrowIfPrefab(cmd->Header.Header.Entity);

                ThrowIfPrefabComponent(ComponentType.FromTypeIndex(cmd->Header.ComponentTypeIndex));

                playbackProcessor.AddComponentLinkedEntityGroup(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetComponentLinkedEntityGroup(BasicCommand* header)
            {
                var cmd = (EntityQueryMaskCommand*) header;

                ThrowIfPrefab(cmd->Header.Header.Entity);

                playbackProcessor.SetComponentLinkedEntityGroup(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReplaceComponentLinkedEntityGroup(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*) header;

                ThrowIfPrefab(cmd->Header.Entity);

                playbackProcessor.ReplaceComponentLinkedEntityGroup(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddUnmanagedSharedComponentData(BasicCommand* header)
            {
                var cmd = (EntityUnmanagedSharedComponentCommand*)header;

                ThrowIfPrefab(cmd->Header.Entity);

                playbackProcessor.AddUnmanagedSharedComponentData(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetUnmanagedSharedComponentData(BasicCommand* header)
            {
                var cmd = (EntityUnmanagedSharedComponentCommand*)header;

                ThrowIfPrefab(cmd->Header.Entity);

                playbackProcessor.SetUnmanagedSharedComponentData(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddUnmanagedSharedComponentValueForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesCommand_WithUnmanagedSharedComponent*)header;

                ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount);

                playbackProcessor.AddUnmanagedSharedComponentValueForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddUnmanagedSharedComponentValueForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommandWithUnmanagedSharedComponent*)header;

                // TODO: throw if any matching archetypes have the Prefab component
                //ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->Header.SkipDeferredEntityLookup != 0);

                playbackProcessor.AddUnmanagedSharedComponentValueForEntityQuery(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetUnmanagedSharedComponentValueForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesCommand_WithUnmanagedSharedComponent*)header;

                ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount);

                playbackProcessor.SetUnmanagedSharedComponentValueForMultipleEntities(header);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetUnmanagedSharedComponentValueForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommandWithUnmanagedSharedComponent*)header;

                // TODO: throw if any matching archetypes have the Prefab component
                //ThrowIfPrefab(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, cmd->Header.SkipDeferredEntityLookup != 0);

                playbackProcessor.SetUnmanagedSharedComponentValueForEntityQuery(header);
            }

            public ECBProcessorType ProcessorType => ECBProcessorType.PrePlaybackValidationProcessor;
        }

        internal struct DebugViewProcessor : IEcbProcessor
        {
            public UnsafePtrList<BasicCommand> commands;

            public void Init(AllocatorManager.AllocatorHandle allocator)
            {
                commands = new UnsafePtrList<BasicCommand>(INITIAL_COMMANDS_CAPACITY, allocator);
            }

            public void Cleanup()
            {
                commands.Dispose();
            }

            public unsafe void DestroyEntity(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void RemoveComponent(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void RemoveMultipleComponents(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void CreateEntity(ECBPlaybackState *ecbPlaybackState, BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void InstantiateEntity(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void AddComponent(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void AddMultipleComponents(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void SetComponent(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void SetEnabled(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void SetComponentEnabled(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void SetName(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void AddBuffer(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void SetBuffer(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void AppendToBuffer(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void AddComponentForEntityQuery(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void AddComponentForMultipleEntities(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void RemoveComponentForEntityQuery(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void RemoveComponentForMultipleEntities(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void AddMultipleComponentsForMultipleEntities(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void AddMultipleComponentsForEntityQuery(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void RemoveMultipleComponentsForMultipleEntities(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void RemoveMultipleComponentsForEntityQuery(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void DestroyMultipleEntities(BasicCommand* header)
            {
                commands.Add(header);
            }

            public unsafe void DestroyForEntityQuery(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void AddComponentLinkedEntityGroup(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void SetComponentLinkedEntityGroup(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void ReplaceComponentLinkedEntityGroup(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void AddUnmanagedSharedComponentData(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void SetUnmanagedSharedComponentData(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void AddUnmanagedSharedComponentValueForMultipleEntities(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void AddUnmanagedSharedComponentValueForEntityQuery(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void SetUnmanagedSharedComponentValueForMultipleEntities(BasicCommand* header)
            {
                commands.Add(header);
            }

            public void SetUnmanagedSharedComponentValueForEntityQuery(BasicCommand* header)
            {
                commands.Add(header);
            }

            public ECBProcessorType ProcessorType => ECBProcessorType.DebugViewProcessor;
        }

        internal class BasicCommandView
        {
            public ECBCommand CommandType;
            public int SortKey;
            public int TotalSizeInBytes;

            public BasicCommandView()
            {
                CommandType = default;
                SortKey = Int32.MinValue;
                TotalSizeInBytes = 0;
            }

            public BasicCommandView(ECBCommand commandType, int sortKey, int totalSize)
            {
                CommandType = commandType;
                SortKey = sortKey;
                TotalSizeInBytes = totalSize;
            }
        }

        internal class CreateCommandView : BasicCommandView
        {
            public EntityArchetype EntityArchetype;
            public Entity* Entities;
            public int EntityCount;

            public CreateCommandView(CreateCommand* cmd)
            {
                CommandType = cmd->Header.CommandType;
                SortKey = cmd->Header.SortKey;
                TotalSizeInBytes = cmd->Header.TotalSize;
                EntityArchetype = cmd->Archetype;
                Entities = cmd->Entities;
                EntityCount = cmd->EntityCount;
            }

            public override string ToString()
            {
                return "Create Entity";
            }
        }

        internal class EntityCommandView : BasicCommandView
        {
            public Entity Entity;
            public Entity* Entities;
            public int EntityCount;

            public EntityCommandView()
            {
                Entity = Entity.Null;
                EntityCount = 0;
                Entities = null;
            }

            public EntityCommandView(EntityCommand* cmd)
            {
                CommandType = cmd->Header.CommandType;
                SortKey = cmd->Header.SortKey;
                TotalSizeInBytes = cmd->Header.TotalSize;
                Entity = cmd->Entity;
                Entities = cmd->Entities;
                EntityCount = cmd->EntityCount;
            }

            public override string ToString()
            {
                return (CommandType == ECBCommand.InstantiateEntity) ? $"Instantiate Entity (count={EntityCount})" : "Destroy Entity";
            }
        }

        internal class EntityQueryCommandView : BasicCommandView
        {
            public EntityQueryImpl* Query;

            public EntityQueryCommandView()
            {
                CommandType = 0;
                SortKey = 0;
                TotalSizeInBytes = 0;
                Query = null;
            }

            public EntityQueryCommandView(EntityQueryCommand* cmd)
            {
                CommandType = cmd->Header.CommandType;
                SortKey = cmd->Header.SortKey;
                TotalSizeInBytes = cmd->Header.TotalSize;
                Query = cmd->QueryImpl;
            }
        }

        internal class EntityQueryComponentCommandView : EntityQueryCommandView
        {
            public TypeIndex ComponentTypeIndex;

            public EntityQueryComponentCommandView()
            {
                CommandType = 0;
                SortKey = 0;
                TotalSizeInBytes = 0;
                Query = null;
                ComponentTypeIndex = 0;
            }

            public EntityQueryComponentCommandView(EntityQueryComponentCommand* cmd)
            {
                CommandType = cmd->Header.Header.CommandType;
                SortKey = cmd->Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.TotalSize;
                Query = cmd->Header.QueryImpl;
                ComponentTypeIndex = cmd->ComponentTypeIndex;
            }

            public override string ToString()
            {
                var type = TypeManager.GetType(ComponentTypeIndex);
                var typeName = type.Name + " ";
                return (CommandType == ECBCommand.AddComponentForEntityQuery) ? $"Add {typeName}Component to EntityQuery" : $"Remove {typeName}Component from EntityQuery";
            }
        }

        internal class EntityQueryComponentTypeSetCommandView : EntityQueryCommandView
        {
            public ComponentTypeSet TypeSet;

            public EntityQueryComponentTypeSetCommandView(EntityQueryComponentTypeSetCommand* cmd)
            {
                CommandType = cmd->Header.Header.CommandType;
                SortKey = cmd->Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.TotalSize;
                Query = cmd->Header.QueryImpl;
                TypeSet = cmd->TypeSet;
            }

            public override string ToString()
            {
                return (CommandType == ECBCommand.AddComponentForEntityQuery) ? $"Add {TypeSet} to EntityQuery" : $"Remove {TypeSet} from EntityQuery";
            }
        }

        internal class MultipleEntitiesCommandView : BasicCommandView
        {
            public EntityNode Entities;
            public int EntitiesCount;
            public AllocatorManager.AllocatorHandle Allocator;

            public MultipleEntitiesCommandView()
            {
                Entities = new EntityNode();
                EntitiesCount = 0;
                Allocator = Unity.Collections.Allocator.Invalid;
            }

            public MultipleEntitiesCommandView(MultipleEntitiesCommand *cmd)
            {
                CommandType = cmd->Header.CommandType;
                SortKey = cmd->Header.SortKey;
                TotalSizeInBytes = cmd->Header.TotalSize;
                Entities = cmd->Entities;
                EntitiesCount = cmd->EntitiesCount;
                Allocator = cmd->Allocator.ToAllocator;
            }

            public override string ToString()
            {
                return (CommandType == ECBCommand.CreateEntity) ? $"Instantiate {EntitiesCount} Entities" : $"Destroy {EntitiesCount} Entities";
            }
        }

        internal class MultipleEntitiesComponentCommandView : MultipleEntitiesCommandView
        {
            public TypeIndex ComponentTypeIndex;
            public short ComponentSize;
            public object ComponentValue;

            public MultipleEntitiesComponentCommandView(MultipleEntitiesComponentCommand* cmd, byte* componentValue)
            {
                CommandType = cmd->Header.Header.CommandType;
                SortKey = cmd->Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.TotalSize;
                Entities = cmd->Header.Entities;
                EntitiesCount = cmd->Header.EntitiesCount;
                Allocator = cmd->Header.Allocator.ToAllocator;
                ComponentTypeIndex = cmd->ComponentTypeIndex;
                ComponentSize = cmd->ComponentSize;
                if (ComponentSize > 0 && componentValue != null)
                {
                    ComponentValue = TypeManager.ConstructComponentFromBuffer(ComponentTypeIndex, componentValue);
                }
                else
                {
                    ComponentValue = default;
                }
            }

            public override string ToString()
            {
                var type = TypeManager.GetType(ComponentTypeIndex);
                var typeName = type.Name + " ";
                return (CommandType == ECBCommand.AddComponentForMultipleEntities) ? $"Add {typeName}Component to {EntitiesCount} Entities" : $"Remove {typeName}Component from {EntitiesCount} Entities";
            }
        }

        internal class MultipleEntitiesAndComponentsCommandView : MultipleEntitiesCommandView
        {
            public ComponentTypeSet TypeSet;

            public MultipleEntitiesAndComponentsCommandView(MultipleEntitiesAndComponentsCommand* cmd)
            {
                CommandType = cmd->Header.Header.CommandType;
                SortKey = cmd->Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.TotalSize;
                Entities = cmd->Header.Entities;
                EntitiesCount = cmd->Header.EntitiesCount;
                Allocator = cmd->Header.Allocator.ToAllocator;
                TypeSet = cmd->TypeSet;
            }

            public override string ToString()
            {
                return CommandType == ECBCommand.AddMultipleComponentsForMultipleEntities ? $"Add {TypeSet.Length} " +
                    $"Components to {EntitiesCount} Entities": $"Remove {TypeSet.Length} Components from {EntitiesCount} Entities";
            }
        }

        internal class EntityComponentCommandView : EntityCommandView
        {
            public TypeIndex ComponentTypeIndex;
            public short ComponentSize;
            public object ComponentValue;

            public EntityComponentCommandView()
            {
                ComponentTypeIndex = TypeIndex.Null;
                ComponentSize = 0;
                ComponentValue = default;
            }

            public EntityComponentCommandView(EntityComponentCommand* cmd, byte* componentValue)
            {
                CommandType = cmd->Header.Header.CommandType;
                SortKey = cmd->Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.TotalSize;
                Entity = cmd->Header.Entity;
                EntityCount = cmd->Header.EntityCount;
                Entities = cmd->Header.Entities;
                ComponentTypeIndex = cmd->ComponentTypeIndex;
                ComponentSize = cmd->ComponentSize;
                if (ComponentSize > 0 && componentValue != null)
                {
                    ComponentValue = TypeManager.ConstructComponentFromBuffer(ComponentTypeIndex, componentValue);
                }
                else
                {
                    ComponentValue = default;
                }
            }

            public override string ToString()
            {
                var type = TypeManager.GetType(ComponentTypeIndex);
                var typeName = type.Name + " ";
                switch (CommandType)
                {
                    case ECBCommand.RemoveComponent: return $"Remove {typeName}Component";
                    case ECBCommand.AddComponent: return $"Add {typeName}Component";
                    case ECBCommand.SetComponent: return $"Set {typeName}Component";
                    case ECBCommand.AppendToBuffer: return $"Append {typeName}BufferElementData";
                    case ECBCommand.ReplaceComponentLinkedEntityGroup:
                        return $"Replace {typeName}Component for LinkedEntityGroup";
                    default: throw new ArgumentException("Unknown Command");
                }
            }
        }

        internal class EntityQueryMaskCommandView : EntityComponentCommandView
        {
            public EntityQueryMask Mask;

            public EntityQueryMaskCommandView()
            {
                ComponentTypeIndex = TypeIndex.Null;
                ComponentSize = 0;
                ComponentValue = default;
            }

            public EntityQueryMaskCommandView(EntityQueryMaskCommand* cmd, byte* componentValue)
            {
                CommandType = cmd->Header.Header.Header.CommandType;
                SortKey = cmd->Header.Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.Header.TotalSize;
                Entity = cmd->Header.Header.Entity;
                EntityCount = cmd->Header.Header.EntityCount;
                Entities = cmd->Header.Header.Entities;
                Mask = cmd->Mask;
                ComponentTypeIndex = cmd->Header.ComponentTypeIndex;
                ComponentSize = cmd->Header.ComponentSize;
                ComponentValue = (ComponentSize > 0 && componentValue != null)
                    ? TypeManager.ConstructComponentFromBuffer(ComponentTypeIndex, componentValue) : default;
            }

            public override string ToString()
            {
                var type = TypeManager.GetType(ComponentTypeIndex);
                var typeName = type.Name + " ";
                switch (CommandType)
                {
                    case ECBCommand.AddComponentLinkedEntityGroup: return $"Add {typeName}Component for LinkedEntityGroup";
                    case ECBCommand.SetComponentLinkedEntityGroup: return $"Set {typeName}Component for LinkedEntityGroup";
                    default: throw new ArgumentException("Unknown Command");
                }
            }
        }

        internal class EntityEnabledCommandView : EntityCommandView
        {
            public int ComponentTypeIndex;
            public byte IsEnabled;

            public EntityEnabledCommandView(EntityEnabledCommand* cmd)
            {
                CommandType = cmd->Header.Header.CommandType;
                SortKey = cmd->Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.TotalSize;
                Entity = cmd->Header.Entity;
                EntityCount = cmd->Header.EntityCount;
                Entities = cmd->Header.Entities;
                IsEnabled = cmd->IsEnabled;
            }

            public override string ToString()
            {
                return IsEnabled == 1 ? $"Set Entity ({Entity.Index},{Entity.Version}) to Enabled" : $"Set Entity ({Entity.Index},{Entity.Version}) to Disabled";
            }

        }

        internal class EntityComponentEnabledCommandView : EntityCommandView
        {
            public TypeIndex ComponentTypeIndex;
            public byte IsEnabled;

            public EntityComponentEnabledCommandView(EntityComponentEnabledCommand* cmd)
            {
                CommandType = cmd->Header.Header.Header.CommandType;
                SortKey = cmd->Header.Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.Header.TotalSize;
                Entity = cmd->Header.Header.Entity;
                EntityCount = cmd->Header.Header.EntityCount;
                Entities = cmd->Header.Header.Entities;
                IsEnabled = cmd->Header.IsEnabled;
                ComponentTypeIndex = cmd->ComponentTypeIndex;
            }

            public override string ToString()
            {
                var type = TypeManager.GetType(ComponentTypeIndex);
                var typeName = type.Name + " ";
                return IsEnabled != 1 ? $"{typeName}Component Enabled" : $"{typeName}Component Disabled";
            }

        }

        internal class EntityNameCommandView : EntityCommandView
        {
            public FixedString64Bytes Name;

            public EntityNameCommandView(EntityNameCommand* cmd)
            {
                CommandType = cmd->Header.Header.CommandType;
                SortKey = cmd->Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.TotalSize;
                Entity = cmd->Header.Entity;
                EntityCount = cmd->Header.EntityCount;
                Entities = cmd->Header.Entities;
                Name = cmd->Name;
            }

            public override string ToString()
            {
                return $"Set EntityName: {Name.ToString()}";
            }
        }

        internal class EntityMultipleComponentsCommandView : EntityCommandView
        {
            public ComponentTypeSet TypeSet;

            public EntityMultipleComponentsCommandView(EntityMultipleComponentsCommand* cmd)
            {
                CommandType = cmd->Header.Header.CommandType;
                SortKey = cmd->Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.TotalSize;
                Entity = cmd->Header.Entity;
                EntityCount = cmd->Header.EntityCount;
                Entities = cmd->Header.Entities;
                TypeSet = cmd->TypeSet;
            }

            public override string ToString()
            {
                switch (CommandType)
                {
                    case ECBCommand.AddMultipleComponents: return $"Add {TypeSet.Length} Components";
                    default: return $"Remove {TypeSet.Length} Components";
                }
            }
        }

        internal unsafe class EntityBufferCommandView : EntityCommandView
        {
            public TypeIndex ComponentTypeIndex;
            public short ComponentSize;
            // Must point to original buffer node in ECB, so that we can find the buffer data embedded after it.
            public BufferHeaderNode* BufferNode;

            public EntityBufferCommandView(EntityBufferCommand* cmd)
            {
                CommandType = cmd->Header.Header.CommandType;
                SortKey = cmd->Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.TotalSize;
                Entity = cmd->Header.Entity;
                EntityCount = cmd->Header.EntityCount;
                Entities = cmd->Header.Entities;
                ComponentTypeIndex = cmd->ComponentTypeIndex;
                ComponentSize = cmd->ComponentSize;
                BufferNode = &(cmd->BufferNode);
            }

            public override string ToString()
            {
                var type = TypeManager.GetType(ComponentTypeIndex);
                var typeName = " " + type.Name;
                return CommandType == ECBCommand.AddBuffer ? $"Add Entity Buffer{typeName}" : $"Set Entity Buffer{typeName}";
            }
        }

        internal class EntityUnmanagedSharedComponentCommandView : EntityCommandView
        {
            public TypeIndex ComponentTypeIndex;
            public int HashCode;
            public short ComponentSize;
            public object ComponentValue;

            public EntityUnmanagedSharedComponentCommandView(EntityUnmanagedSharedComponentCommand* cmd, byte* componentValue)
            {
                CommandType = cmd->Header.Header.CommandType;
                SortKey = cmd->Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.TotalSize;
                Entity = cmd->Header.Entity;
                EntityCount = cmd->Header.EntityCount;
                Entities = cmd->Header.Entities;
                ComponentTypeIndex = cmd->ComponentTypeIndex;
                HashCode = cmd->HashCode;
                ComponentSize = (short)TypeManager.GetTypeInfo(cmd->ComponentTypeIndex).TypeSize;
                ComponentValue = TypeManager.ConstructComponentFromBuffer(cmd->ComponentTypeIndex, componentValue);
            }

            public override string ToString()
            {
                var type = TypeManager.GetType(ComponentTypeIndex);
                var typeName = type.Name + " ";
                return CommandType  == ECBCommand.AddUnmanagedSharedComponentData ? $"Add {typeName}UnmanagedSharedComponentData" : $"Set {typeName}UnmanagedSharedComponentData";
            }
        }

        internal class MultipleEntitiesComponentCommandView_WithUnmanagedSharedValue : MultipleEntitiesCommandView
        {
            public TypeIndex ComponentTypeIndex;
            public int ComponentSize;
            public object ComponentValue;

            public MultipleEntitiesComponentCommandView_WithUnmanagedSharedValue(MultipleEntitiesCommand_WithUnmanagedSharedComponent* cmd,
                byte* componentValue)
            {
                CommandType = cmd->Header.Header.CommandType;
                SortKey = cmd->Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.TotalSize;
                Entities = cmd->Header.Entities;
                EntitiesCount = cmd->Header.EntitiesCount;
                Allocator = cmd->Header.Allocator.ToAllocator;
                ComponentTypeIndex = cmd->ComponentTypeIndex;
                ComponentSize = cmd->ComponentSize;
                if (ComponentSize > 0 && componentValue != null)
                    ComponentValue = TypeManager.ConstructComponentFromBuffer(cmd->ComponentTypeIndex, componentValue);
                else
                    ComponentValue = default;
            }

            public override string ToString()
            {
                var type = TypeManager.GetType(ComponentTypeIndex);
                var typeName = type.Name + " ";
                return (CommandType == ECBCommand.AddUnmanagedSharedComponentValueForMultipleEntities)
                    ? $"Add {typeName}Unmanaged Shared Component to {EntitiesCount} Entities"
                    : $"Set {typeName}Unmanaged Shared Component from {EntitiesCount} Entities";
            }
        }

        internal class EntityQueryComponentCommandView_WithUnmanagedSharedValue : EntityQueryComponentCommandView
        {
            public int ComponentSize;
            public object ComponentValue;

            public EntityQueryComponentCommandView_WithUnmanagedSharedValue(EntityQueryComponentCommandWithUnmanagedSharedComponent* cmd,
                byte* componentValue)
            {
                CommandType = cmd->Header.Header.Header.CommandType;
                SortKey = cmd->Header.Header.Header.SortKey;
                TotalSizeInBytes = cmd->Header.Header.Header.TotalSize;
                Query = cmd->Header.Header.QueryImpl;
                ComponentTypeIndex = cmd->Header.ComponentTypeIndex;
                ComponentSize = cmd->ComponentSize;
                if (ComponentSize > 0 && componentValue != null)
                    ComponentValue = TypeManager.ConstructComponentFromBuffer(cmd->Header.ComponentTypeIndex, componentValue);
                else
                    ComponentValue = default;
            }

            public override string ToString()
            {
                var type = TypeManager.GetType(ComponentTypeIndex);
                var typeName = type.Name + " ";
                return (CommandType == ECBCommand.AddUnmanagedSharedComponentValueForEntityQuery)
                    ? $"Add {typeName}Unmanaged Shared Component to EntityQuery"
                    : $"Set {typeName}Unmanaged Shared Component from EntityQuery";
            }
        }

        internal sealed class EntityCommandBufferDebugView
        {
            private EntityCommandBuffer m_ecb;
            public EntityCommandBufferDebugView(EntityCommandBuffer ecb)
            {
                m_ecb = ecb;
            }

            public BasicCommandView ProcessDebugViewCommand(BasicCommand* header)
            {
                switch (header->CommandType)
                {
                    case ECBCommand.DestroyEntity:
                    case ECBCommand.InstantiateEntity:
                        return new EntityCommandView((EntityCommand*)header);

                    case ECBCommand.CreateEntity:
                        return new CreateCommandView((CreateCommand*)header);

                    case ECBCommand.RemoveMultipleComponents:
                    case ECBCommand.AddMultipleComponents:
                        return new EntityMultipleComponentsCommandView((EntityMultipleComponentsCommand*)header);

                    case ECBCommand.RemoveComponent:
                    case ECBCommand.AddComponent:
                    case ECBCommand.SetComponent:
                    case ECBCommand.AppendToBuffer:
                    case ECBCommand.ReplaceComponentLinkedEntityGroup:
                        var entityComponentCommand = (EntityComponentCommand*) header;
                        var entityComponentCommandValue = header->CommandType != ECBCommand.RemoveComponent ? (byte*)(entityComponentCommand+1) : null;
                        return new EntityComponentCommandView(entityComponentCommand, entityComponentCommandValue);

                    case ECBCommand.SetEntityEnabled:
                        return new EntityEnabledCommandView( (EntityEnabledCommand*)header);

                    case ECBCommand.SetComponentEnabled:
                        return new EntityComponentEnabledCommandView((EntityComponentEnabledCommand*)header);

                    case ECBCommand.SetName:
                        return new EntityNameCommandView((EntityNameCommand*)header);

                    case ECBCommand.AddBuffer:
                    case ECBCommand.SetBuffer:
                        return new EntityBufferCommandView((EntityBufferCommand*)header);

                    case ECBCommand.AddComponentForEntityQuery:
                    case ECBCommand.RemoveComponentForEntityQuery:
                        return new EntityQueryComponentCommandView((EntityQueryComponentCommand*)header);

                    case ECBCommand.AddComponentForMultipleEntities:
                    case ECBCommand.RemoveComponentForMultipleEntities:
                        var multipleEntitiesComponentCommand = (MultipleEntitiesComponentCommand*)header;
                        var multipleEntitiesComponentCommandValue = header->CommandType != ECBCommand.RemoveComponentForMultipleEntities
                            ? (byte*)(multipleEntitiesComponentCommand+1) : null;
                        return new MultipleEntitiesComponentCommandView(multipleEntitiesComponentCommand, multipleEntitiesComponentCommandValue);

                    case ECBCommand.AddMultipleComponentsForMultipleEntities:
                    case ECBCommand.RemoveMultipleComponentsForMultipleEntities:
                        return new MultipleEntitiesAndComponentsCommandView((MultipleEntitiesAndComponentsCommand*)header);

                    case ECBCommand.AddMultipleComponentsForEntityQuery:
                    case ECBCommand.RemoveMultipleComponentsForEntityQuery:
                        return new EntityQueryComponentTypeSetCommandView((EntityQueryComponentTypeSetCommand*)header);

                    case ECBCommand.DestroyMultipleEntities:
                        return new MultipleEntitiesCommandView((MultipleEntitiesCommand*)header);

                    case ECBCommand.DestroyForEntityQuery:
                        return new EntityQueryCommandView((EntityQueryCommand*)header);

                    case ECBCommand.AddUnmanagedSharedComponentData:
                    case ECBCommand.SetUnmanagedSharedComponentData:
                        var unmanagedSharedComponentCommand = (EntityUnmanagedSharedComponentCommand*)header;
                        byte* unmanagedSharedComponentCommandValue = (byte*)(unmanagedSharedComponentCommand + 1);
                        return new EntityUnmanagedSharedComponentCommandView(unmanagedSharedComponentCommand, unmanagedSharedComponentCommandValue);

                    case ECBCommand.AddUnmanagedSharedComponentValueForMultipleEntities:
                    case ECBCommand.SetUnmanagedSharedComponentValueForMultipleEntities:
                        var multiEntityUnmanagedSharedComponentCommand = (MultipleEntitiesCommand_WithUnmanagedSharedComponent*) header;
                        var multiEntityUnmanagedSharedComponentCommandValue = (byte*)(multiEntityUnmanagedSharedComponentCommand+1);
                        return new MultipleEntitiesComponentCommandView_WithUnmanagedSharedValue(multiEntityUnmanagedSharedComponentCommand,
                            multiEntityUnmanagedSharedComponentCommandValue);

                    case ECBCommand.AddUnmanagedSharedComponentValueForEntityQuery:
                    case ECBCommand.SetUnmanagedSharedComponentValueForEntityQuery:
                        var queryUnmanagedSharedComponentCommand = (EntityQueryComponentCommandWithUnmanagedSharedComponent*) header;
                        var queryUnmanagedSharedComponentCommandValue = (byte*)(queryUnmanagedSharedComponentCommand+1);
                        return new EntityQueryComponentCommandView_WithUnmanagedSharedValue(queryUnmanagedSharedComponentCommand,
                            queryUnmanagedSharedComponentCommandValue);

                    case ECBCommand.AddComponentLinkedEntityGroup:
                    case ECBCommand.SetComponentLinkedEntityGroup:
                        var maskCommand = (EntityQueryMaskCommand*) header;
                        var maskCommandValue = (byte*)(maskCommand+1);
                        return new EntityQueryMaskCommandView(maskCommand, maskCommandValue);

                    default:
                    {
                        throw new InvalidOperationException(
                            $"Invalid command type {(ECBCommand)header->CommandType} not recognized.");
                    }
                }
            }

            public BasicCommandView[] Commands
            {
                get {
                    var walker = new EcbWalker<DebugViewProcessor>(default, ECBProcessorType.DebugViewProcessor);
                    walker.processor.Init(m_ecb.m_Data->m_Allocator.ToAllocator);
                    walker.WalkChains(m_ecb);

                    //Convert the unsafe native list of pointers to the commands to an array of command views
                    var commandViewArray = new BasicCommandView[walker.processor.commands.Length];

                    for (var i = 0; i < walker.processor.commands.Length; i++)
                    {
                        commandViewArray[i] = ProcessDebugViewCommand(walker.processor.commands[i]);
                    }

                    walker.processor.Cleanup();
                    return commandViewArray;
                }
            }
        }
    }
}
