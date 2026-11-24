// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.Entities
{
    // The Entities state shared by every World. CoreModule's DefaultWorldInitialization creates the Worlds.
    static partial class EntitiesInitialization
    {
        /// <summary>
        /// Sets up the Entities state shared by every World for one code load. Runs after Burst is initialized
        /// and before [OnCodeLoaded]; the EntitiesWorld scope is only entered after this.
        /// </summary>
        [EntitiesInitialize]
        static void InitializeEntities()
        {
            TypeManager.Initialize();
        }

        /// <summary>
        /// Releases the Entities state shared by every World. A load can still have its loading World here.
        /// </summary>
        [EntitiesCleanup]
        static void ShutdownEntities()
        {
            // Lifecycle methods of one attribute run in no defined order, so this runs here, before the entity store goes away.
            LoadingWorldSystem.DisposeInUseAtShutdown();
            EntityComponentStore.s_entityStore.Data.Dispose();
        }

        /// <summary>
        /// Initializes Entities allocator budgets from native memory settings before any World is created.
        /// </summary>
        [OnCodeLoaded]
        static void InitializeAllocatorBudgets()
        {
            var archetypeBudget = EntitiesMemorySetup.GetArchetypeAllocatorBudget();
            var queryBudget = EntitiesMemorySetup.GetQueryAllocatorBudget();

            const int kDefaultBudget = EntityComponentStore.kDefaultAllocatorBudgetInBytes;
            EntityComponentStore.ArchetypeChunkAllocatorBudgetInBytes = archetypeBudget > 0 ? (int)Math.Min(archetypeBudget, int.MaxValue) : kDefaultBudget;
            EntityQueryManager.EntityQueryDataChunkAllocatorBudgetInBytes = queryBudget > 0 ? (int)Math.Min(queryBudget, int.MaxValue) : kDefaultBudget;
        }
    }
}
