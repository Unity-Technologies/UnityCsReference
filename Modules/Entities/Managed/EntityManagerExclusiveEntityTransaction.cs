// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Entities
{
    public unsafe partial struct EntityManager
    {
        // ----------------------------------------------------------------------------------------------------------
        // PUBLIC
        // ----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// An optional <see cref="JobHandle"/> that corresponds to the job currently using an ExclusiveEntityTransaction.
        /// This job is completed if this EntityManager's <see cref="World"/> is destroyed.
        /// </summary>
        public JobHandle ExclusiveEntityTransactionDependency
        {
            get
            {
                // Note this can't use read/write checking
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                return GlobalEntityManager.GetExclusiveEntityTransactionDependency(m_EntityDataAccess);
            }
            set
            {
                // Note this can't use read/write checking
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
                GlobalEntityManager.SetExclusiveEntityTransactionDependency(m_EntityDataAccess, value);
            }
        }

        /// <summary>
        /// Check whether or not a new exclusive entity transaction can begin.
        /// </summary>
        /// <returns><see langword="true"/> if a new exclusive transaction can begin, <see langword="false"/> otherwise.</returns>
        public bool CanBeginExclusiveEntityTransaction()
        {
            if (IsInExclusiveTransaction)
                return false;

            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);

            var access = GetUncheckedEntityDataAccess();
            if (access->DependencyManager->IsInTransaction)
                return false;

            return true;
        }

        /// <summary>
        /// Begins an exclusive entity transaction, which allows you to make structural changes inside a Job.
        /// </summary>
        /// <remarks>
        /// <see cref="ExclusiveEntityTransaction"/> allows you to create and destroy entities from a job. The purpose is
        /// to enable procedural generation scenarios where instantiation on big scale must happen on jobs. As the
        /// name implies it is exclusive to any other access to the EntityManager.
        ///
        /// An exclusive entity transaction should be used on a manually created <see cref="World"/> that acts as a
        /// staging area to construct and setup entities.
        ///
        /// After the job has completed you can end the transaction and use
        /// <see cref="MoveEntitiesFrom(EntityManager)"/> to move the entities to an active <see cref="World"/>.
        /// </remarks>
        /// <returns>A transaction object that provides an functions for making structural changes.</returns>
        public ExclusiveEntityTransaction BeginExclusiveEntityTransaction()
        {
            if (IsInExclusiveTransaction)
                throw new InvalidOperationException("An exclusive transaction is already in process.");
            return GlobalEntityManager.BeginExclusiveEntityTransaction(GetCheckedEntityDataAccess());
        }

        /// <summary>
        /// Ends an active <see cref="ExclusiveEntityTransaction"/> and returns ownership of the
        /// <see cref="EntityManager"/> to the main thread.
        /// </summary>
        /// <seealso cref="ExclusiveEntityTransaction"/>
        /// <seealso cref="BeginExclusiveEntityTransaction()"/>
        public void EndExclusiveEntityTransaction()
        {
            if (m_IsInExclusiveTransaction == 1)
                throw new InvalidOperationException("Transactions can only be ended from the main thread");

            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            GlobalEntityManager.EndExclusiveEntityTransaction(m_EntityDataAccess);
        }

        // The EntityManager the holder of a World's exclusive transaction uses
        internal static EntityManager ExclusiveTransactionHolder(EntityDataAccess* access
            , AtomicSafetyHandle safety
        )
        {
            var holder = new EntityManager { m_EntityDataAccess = access };
            holder.m_Safety = safety;
            holder.m_IsInExclusiveTransaction = 1;
            return holder;
        }

        // ----------------------------------------------------------------------------------------------------------
        // INTERNAL
        // ----------------------------------------------------------------------------------------------------------


        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        internal void AddSharedComponent<T>(NativeArray<ArchetypeChunk> chunks, T componentData)
            where T : unmanaged, ISharedComponentData
        {
            if (StructuralChangesProfiler.Enabled)
            {
                m_EntityDataAccess->StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_EntityDataAccess->m_WorldUnmanaged);
                m_EntityDataAccess->StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.SetSharedComponent, in m_EntityDataAccess->m_WorldUnmanaged);
            }

            var componentType = ComponentType.ReadWrite<T>();
            var archetypeChanges = m_EntityDataAccess->BeginStructuralChanges();
            int sharedComponentIndex = m_EntityDataAccess->InsertSharedComponent_Unmanaged(componentData);
            m_EntityDataAccess->AddSharedComponentDataDuringStructuralChange(chunks, sharedComponentIndex, componentType);
            m_EntityDataAccess->EndStructuralChanges(ref archetypeChanges);

            if (StructuralChangesProfiler.Enabled)
            {
                m_EntityDataAccess->StructuralChangesRecorder.End(); // SetSharedComponent
                m_EntityDataAccess->StructuralChangesRecorder.End(); // AddComponent
            }
        }
    }
}
