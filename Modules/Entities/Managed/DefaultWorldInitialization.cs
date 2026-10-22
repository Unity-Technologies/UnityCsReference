// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using UnityEngine;
using Unity.Collections;
using Unity.Scripting.LifecycleManagement;
using Assert = UnityEngine.Assertions.AssertCore;
using Debug = Unity.Scripting.LowLevel.Debug;
using ProfilerMarker = Unity.Entities.EntitiesProfilerMarker;

namespace Unity.Entities
{
    /// <summary>
    /// Utilities to help initialize the default ECS <see cref="World"/>.
    /// </summary>
    public static partial class DefaultWorldInitialization
    {
#pragma warning disable 0067 // unused variable
        /// <summary>
        /// Invoked after the Worlds are destroyed.
        /// </summary>
        [AutoStaticsCleanup]
        internal static event Action DefaultWorldDestroyed;
#pragma warning restore 0067 // unused variable

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
        /// Releases the Entities state shared by every World. Every World has been disposed by then.
        /// </summary>
        [EntitiesCleanup]
        static void ShutdownEntities()
        {
            EntityComponentStore.s_entityStore.Data.Dispose();
        }

        /// <summary>
        /// Disposes all Worlds when the EntitiesWorld scope exits: on a code reload, at shutdown, and around play
        /// mode changes in the Editor. Their systems have already been destroyed by <see cref="DestroySystems"/>.
        /// </summary>
        [EntitiesWorldCleanup]
        static void CleanupWorldBeforeSceneLoad()
        {
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                DomainUnloadOrPlayModeChangeShutdown();
            }
        }

        /// <summary>
        /// Initializes Entities allocator budgets from native memory settings before any World is created.
        /// </summary>
        [OnCodeLoaded]
        static void InitializeAllocatorBudgets()
        {
            var archetypeBudget = CoreModuleLightShim.Hooks.ArchetypeAllocatorBudget;
            var queryBudget = CoreModuleLightShim.Hooks.QueryAllocatorBudget;

            const int kDefaultBudget = EntityComponentStore.kDefaultAllocatorBudgetInBytes;
            EntityComponentStore.ArchetypeChunkAllocatorBudgetInBytes = archetypeBudget > 0 ? (int)Math.Min(archetypeBudget, int.MaxValue) : kDefaultBudget;
            EntityQueryManager.EntityQueryDataChunkAllocatorBudgetInBytes = queryBudget > 0 ? (int)Math.Min(queryBudget, int.MaxValue) : kDefaultBudget;
        }

        /// <summary>
        /// Disposes every World and the per-World state: player loop entries and the Entities profiler.
        /// State shared by every World is released by <see cref="ShutdownEntities"/>.
        /// </summary>
        internal static void DomainUnloadOrPlayModeChangeShutdown()
        {
            EntitiesLightShim.Hooks.RemoveWorldsFromCurrentPlayerLoop(World.s_AllWorlds);

            EntitiesLightShim.Hooks.UnregisterFrameUpdateToCurrentPlayerLoop();

            World.DisposeAll();
            s_SystemsInitialized = false;
            s_CustomBootstrap = null;

            EntitiesLightShim.Hooks.ShutdownEntitiesProfilerReporting();
            DefaultWorldDestroyed?.Invoke();
        }


        /// <summary>
        /// Initializes the default world or runs ICustomBootstrap if one is available.
        /// </summary>
        /// <param name="defaultWorldName">The name of the world that will be created. Unless there is a custom bootstrap.</param>
        /// <param name="editorWorld">Editor worlds by default only include systems with [WorldSystemFilter(WorldSystemFilterFlags.Editor)]. If editorWorld is true, ICustomBootstrap will not be used.</param>
        /// <returns>The initialized <see cref="World"/> object.</returns>
        public static World Initialize(string defaultWorldName, bool editorWorld = false)
        {
            InitializeWithoutSystems(defaultWorldName, editorWorld);
            InitializeAdditionalWorldsAndSystems();

            return World.DefaultGameObjectInjectionWorld;
        }

        /// <summary>
        /// Can be called when in edit mode in the editor to initialize a the default world.
        /// </summary>
        [Obsolete("DefaultLazyEditModeInitialize is obsolete and calling it does nothing. The default World is created automatically.", false)]
        public static void DefaultLazyEditModeInitialize()
        {
            Assert.IsTrue(World.DefaultGameObjectInjectionWorld != null,
                "DefaultLazyEditModeInitialize was called, but no default World exists.");
        }

        /// <summary>
        /// Initializes the default world or runs ICustomBootstrap if one is available.
        /// </summary>
        /// <param name="defaultWorldName">The name of the world that will be created. Unless there is a custom bootstrap.</param>
        /// <param name="editorWorld">Editor worlds by default only include systems with [WorldSystemFilter(WorldSystemFilterFlags.Editor)]. If editorWorld is true, ICustomBootstrap will not be used.</param>
        internal static void InitializeWithoutSystems(string defaultWorldName, bool editorWorld = false)
        {
            using var marker = new ProfilerMarker("Create World & Systems").Auto();

            // The type registry and journaling are set up once per code load by InitializeEntities.
            EntitiesLightShim.Hooks.InitializeEntitiesProfilerReporting();

            s_CustomBootstrap = null;
            if (!editorWorld)
                s_CustomBootstrap = CreateBootStrap();

            //Always create a base world, even if there is a boostrap
            World.PushWorldStack(new World(defaultWorldName, editorWorld ? WorldFlags.Editor : WorldFlags.Game));

            int prevWorldStackCount = World.s_ActiveWorldsStack.Data.Length;
            if (s_CustomBootstrap != null && s_CustomBootstrap.CreateDefaultWorldWithoutSystems(defaultWorldName))
            {
                Assert.IsTrue(prevWorldStackCount <  World.s_ActiveWorldsStack.Data.Length ,
                    $"{s_CustomBootstrap.GetType()}.CreateDefaultWorldWithoutSystems() returned true, which " +
                    $"promises it pushed a World, but the active World stack went from {prevWorldStackCount} " +
                    $"to {World.s_ActiveWorldsStack.Data.Length} entries. Either push a World with " +
                    $"World.PushWorldStack(), or return false to let the default initialization run.");
            }
            s_SystemsInitialized = false;
        }

        [AutoStaticsCleanupOnCodeReload]
        static bool s_SystemsInitialized;
        [AutoStaticsCleanupOnCodeReload]
        static ICustomBootstrap s_CustomBootstrap;

        [EntitiesWorldInitialize]
        static void InitializeBeforeSceneLoading()
        {
            if (!EntitiesLightShim.IsInstalled)
                return;

            if (World.DefaultGameObjectInjectionWorld == null)
            {
                var editorWorld = !CoreModuleLightShim.Hooks.IsPlaying;


                InitializeWithoutSystems(editorWorld ? "Editor World" : "Default World", editorWorld);
            }
        }

        // System OnCreate methods can load assets and read managed objects, e.g. EntitiesGraphicsSystem disables
        // itself when GraphicsSettings.currentRenderPipeline is null. The EntitiesSystems scope guarantees both.
        [EntitiesSystemsInitialize]
        static void InitializeSystems()
        {
            InitializeAdditionalWorldsAndSystems();
        }

        // Runs while managed objects are still valid, so system OnDestroy methods can use them.
        // The Worlds and their entities stay alive until [EntitiesWorldCleanup].
        [EntitiesSystemsCleanup]
        static void DestroySystems()
        {
            if (!EntitiesLightShim.IsInstalled)
                return;

            EntitiesLightShim.Hooks.RemoveWorldsFromCurrentPlayerLoop(World.s_AllWorlds);

            EntitiesLightShim.Hooks.UnregisterFrameUpdateToCurrentPlayerLoop();

            // Copied: a system's OnDestroy may dispose a World it owns. Disposed Worlds are skipped by the callee.
            foreach (var w in World.s_AllWorlds.ToArray())
            {
                if (!w.IsCreated)
                    continue;
                w.EntityManager.CompleteAllTrackedJobs();
                w.DestroyAllSystemsAndLogException(out _);
            }

            s_SystemsInitialized = false;
        }

        [OnCodeLoaded]
        static void RegisterEntitiesWorldHooks()
        {
            if (!EntitiesLightShim.IsInstalled)
                return;

            EntitiesWorldHooks.CreateTestWorldFn = CreateTestWorld;
            EntitiesWorldHooks.DisposeWorldFn = DisposeWorld;
            EntitiesWorldHooks.PopCurrentlyActiveWorldFn = PopCurrentlyActiveWorld;
        }

        /// <summary>
        /// Creates a test World and appends it to the player loop for a fixture to use.
        /// </summary>
        internal static GCHandle CreateTestWorld(bool isEditMode)
        {
            s_TestWorldDisplacedWorldFromPlayerLoop = false;
            if (World.DefaultGameObjectInjectionWorld is { IsCreated: true } displacedWorld
                && EntitiesLightShim.Hooks.IsWorldInCurrentPlayerLoop(displacedWorld))
            {
                EntitiesLightShim.Hooks.RemoveWorldFromCurrentPlayerLoop(displacedWorld);
                s_TestWorldDisplacedWorldFromPlayerLoop = true;
            }

            var testWorld = new World(isEditMode ? "Editor World" : "Default World",
                isEditMode ? WorldFlags.Editor : WorldFlags.Game);

            AddSystemToRootLevelSystemGroupsInternal(testWorld, GetAllSystemTypeIndices(WorldSystemFilterFlags.Default, isEditMode));
            EntitiesLightShim.Hooks.AppendWorldToCurrentPlayerLoop(testWorld);
            World.PushWorldStack(testWorld);

            return GCHandle.Alloc(testWorld, GCHandleType.Normal);
        }

        [AutoStaticsCleanupOnCodeReload]
        static bool s_TestWorldDisplacedWorldFromPlayerLoop;

        internal static void DisposeWorld(GCHandle world)
        {
            if (world.Target is World { IsCreated: true } w)
            {
                EntitiesLightShim.Hooks.RemoveWorldFromCurrentPlayerLoop(w);
                w.Dispose();
            }
        }

        internal static void PopCurrentlyActiveWorld(GCHandle worldHandle)
        {
            World world = worldHandle.Target as World;

            if (world is { IsCreated: true }
                && World.s_ActiveWorldsStack.Data.Length > 1
                && ReferenceEquals(World.PeekWorldStack(), world))
            {
                World.PopWorldStack(world);

                if (s_TestWorldDisplacedWorldFromPlayerLoop
                    && World.DefaultGameObjectInjectionWorld is { IsCreated: true } displacedWorld)
                {
                    EntitiesLightShim.Hooks.AppendWorldToCurrentPlayerLoop(displacedWorld);
                }
                s_TestWorldDisplacedWorldFromPlayerLoop = false;
            }
        }

        /// <summary>
        /// Second half of default world initialization: gives <see cref="ICustomBootstrap"/> its chance to
        /// claim the default world and create additional ones, and otherwise creates the default world's
        /// systems. Must be called after <see cref="InitializeWithoutSystems"/>.
        /// </summary>
        internal static void InitializeAdditionalWorldsAndSystems()
        {
            if (s_SystemsInitialized || World.DefaultGameObjectInjectionWorld == null)
                return;

            if (EntitiesLightShim.Hooks.IsAssetImportWorkerProcess())
            {
                s_SystemsInitialized = true;
                return;
            }
            EntitiesLightShim.Hooks.RegisterFrameUpdateToCurrentPlayerLoop();

            //Add the base world to the loop
            var isEditor = (World.DefaultGameObjectInjectionWorld.Flags & WorldFlags.Editor) == WorldFlags.Editor;
            AddSystemToRootLevelSystemGroupsInternal(World.DefaultGameObjectInjectionWorld, GetAllSystemTypeIndices(WorldSystemFilterFlags.Default, isEditor));
            EntitiesLightShim.Hooks.AppendWorldToCurrentPlayerLoop(World.DefaultGameObjectInjectionWorld);

            // Allow bootstrap creation on top of the default world
            if (s_CustomBootstrap != null)
            {
                s_CustomBootstrap.Initialize(World.DefaultGameObjectInjectionWorld.Name);
            }

            s_SystemsInitialized = true;
            s_CustomBootstrap = null;
        }

        /// <summary>
        /// Adds the collection of systems to the world by injecting them into the root level system groups
        /// (InitializationSystemGroup, SimulationSystemGroup and PresentationSystemGroup). Prefer the version that
        /// takes SystemTypeIndex's as an argument to avoid unnecessary reflection.
        /// </summary>
        /// <param name="world">The World in which the root-level system groups should be created.</param>
        /// <param name="systemTypes">The system types to create and add.</param>
        public static void AddSystemsToRootLevelSystemGroups(World world, IEnumerable<Type> systemTypes)
        {
            var systemTypeIndices = new NativeList<SystemTypeIndex>(16, Allocator.Temp);
            foreach (var t in systemTypes)
            {
                systemTypeIndices.Add(TypeManager.GetSystemTypeIndex(t));
            }
            AddSystemToRootLevelSystemGroupsInternal(world, systemTypeIndices);
        }

        /// <summary>
        /// Adds the collection of systems to the world by injecting them into the root level system groups
        /// (InitializationSystemGroup, SimulationSystemGroup and PresentationSystemGroup). Prefer the version that
        /// takes SystemTypeIndex's as an argument to avoid unnecessary reflection.
        /// </summary>
        /// <param name="world">The World in which the root-level system groups should be created.</param>
        /// <param name="systemTypes">The system types to create and add.</param>
        public static void AddSystemsToRootLevelSystemGroups(World world, IReadOnlyList<Type> systemTypes)
        {
            var systemTypeIndices = new NativeList<SystemTypeIndex>(16, Allocator.Temp);
            foreach (var t in systemTypes)
            {
                systemTypeIndices.Add(TypeManager.GetSystemTypeIndex(t));
            }
            AddSystemToRootLevelSystemGroupsInternal(world, systemTypeIndices);
        }

        /// <summary>
        /// Adds the collection of systems to the world by injecting them into the root level system groups
        /// (InitializationSystemGroup, SimulationSystemGroup and PresentationSystemGroup). Prefer the version that
        /// takes SystemTypeIndex's as an argument to avoid unnecessary reflection.
        /// </summary>
        /// <param name="world">The World in which the root-level system groups should be created.</param>
        /// <param name="systemTypes">The system types to create and add.</param>
        public static void AddSystemsToRootLevelSystemGroups(World world, params Type[] systemTypes)
        {
            var indices = new NativeList<SystemTypeIndex>(systemTypes.Length, Allocator.Temp);
            for (int i = 0; i < systemTypes.Length; i++)
            {
                indices.Add(TypeManager.GetSystemTypeIndex(systemTypes[i]));
            }
            AddSystemToRootLevelSystemGroupsInternal(world, indices);
        }

        /// <summary>
        /// Adds the collection of systems to the world by injecting them into the root level system groups
        /// (InitializationSystemGroup, SimulationSystemGroup and PresentationSystemGroup). This version avoids
        /// unnecessary reflection.
        /// </summary>
        /// <param name="world">The World in which the root-level system groups should be created.</param>
        /// <param name="systemTypes">The system types to create and add.</param>
        public static void AddSystemsToRootLevelSystemGroups(World world, NativeList<SystemTypeIndex> systemTypes)
        {
            AddSystemToRootLevelSystemGroupsInternal(world, systemTypes);
        }

        /// <summary>
        /// This internal interface is used when adding systems to the default world to identify the root groups in your
        /// setup. They will then be skipped when we try to find the parent of each system (because they don't need a
        /// parent).
        /// </summary>
        internal interface IIdentifyRootGroups
        {
            bool IsRootGroup(SystemTypeIndex type);
        }

        struct DefaultRootGroups : IIdentifyRootGroups
        {
            public bool IsRootGroup(SystemTypeIndex type) =>
                type == TypeManager.GetSystemTypeIndex<InitializationSystemGroup>() ||
                type == TypeManager.GetSystemTypeIndex<SimulationSystemGroup>() ||
                type == TypeManager.GetSystemTypeIndex<PresentationSystemGroup>();
        }

        internal static unsafe void AddSystemToRootLevelSystemGroupsInternal<T>(World world, NativeList<SystemTypeIndex> systemTypesOrig, ComponentSystemGroup defaultGroup, T rootGroups)
            where T : struct, IIdentifyRootGroups
        {
            var managedTypes = new List<SystemTypeIndex>();
            var unmanagedTypes = new List<SystemTypeIndex>();

            foreach (var stype in systemTypesOrig)
            {
                if (!TypeManager.IsSystemTypeIndex(stype))
                    throw new InvalidOperationException("Bad type");
                if (stype.IsManaged)
                    managedTypes.Add(stype);
                else
                    unmanagedTypes.Add(stype);
            }

            var allSystemHandlesToAdd = world.GetOrCreateSystemsAndLogException(systemTypesOrig, Allocator.Temp);

            // Add systems to their groups, based on the [UpdateInGroup] attribute.
            for (int i=0; i<systemTypesOrig.Length; i++)
            {
                SystemHandle system = allSystemHandlesToAdd[i];

                // Skip the built-in root-level system groups
                if (rootGroups.IsRootGroup(systemTypesOrig[i]))
                {
                    continue;
                }

                var updateInGroupAttributes = TypeManager.GetSystemAttributes(systemTypesOrig[i],
                    TypeManager.SystemAttributeKind.UpdateInGroup);
                if (updateInGroupAttributes.Length == 0)
                {
                    defaultGroup.AddSystemToUpdateList(system);
                }

                foreach (var attr in updateInGroupAttributes)
                {
                    var group = FindGroup(world, systemTypesOrig[i], attr);
                    if (group != null)
                    {
                        group.AddSystemToUpdateList(system);
                    }
                }
            }
        }

        internal static void AddSystemToRootLevelSystemGroupsInternal(World world, NativeList<SystemTypeIndex> systemTypesOrig)
        {
            using var marker = new ProfilerMarker("AddSystems").Auto();

            var initializationSystemGroup = world.GetOrCreateSystemManaged<InitializationSystemGroup>();
            var simulationSystemGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            var presentationSystemGroup = world.GetOrCreateSystemManaged<PresentationSystemGroup>();

            AddSystemToRootLevelSystemGroupsInternal(world, systemTypesOrig, simulationSystemGroup, new DefaultRootGroups());

            // Update player loop
            initializationSystemGroup.SortSystems();
            simulationSystemGroup.SortSystems();
            presentationSystemGroup.SortSystems();
        }

        private static ComponentSystemGroup FindGroup(World world, SystemTypeIndex systemType, TypeManager.SystemAttribute attr)
        {
            var groupTypeIndex = attr.TargetSystemTypeIndex;

            if (!TypeManager.IsSystemTypeIndex(groupTypeIndex) || !groupTypeIndex.IsGroup)
            {
                throw new InvalidOperationException($"Invalid [{nameof(UpdateInGroupAttribute)}] attribute for {systemType}: target group must be derived from {nameof(ComponentSystemGroup)}.");
            }
            if ((attr.Flags & TypeManager.SystemAttribute.kOrderFirstFlag) != 0 && (attr.Flags & TypeManager.SystemAttribute.kOrderLastFlag) != 0)
            {
                throw new InvalidOperationException($"The system {systemType} can not specify both OrderFirst=true and OrderLast=true in its [{nameof(UpdateInGroupAttribute)}] attribute.");
            }

            var groupSys = world.GetExistingSystemManaged(groupTypeIndex);
            if (groupSys == null)
            {
                // Warn against unexpected behaviour combining DisableAutoCreation and UpdateInGroup
                var parentDisableAutoCreation = TypeManager.GetSystemAttributes(groupTypeIndex, TypeManager.SystemAttributeKind.DisableAutoCreation).Length > 0;
                var name = TypeManager.GetSystemName(groupTypeIndex);
                if (parentDisableAutoCreation)
                {
                    Debug.LogWarning($"A system {systemType} wants to execute in {name} but this group has [{nameof(DisableAutoCreationAttribute)}] and {systemType} does not. The system will not be added to any group and thus not update.");
                }
                else
                {
                    Debug.LogWarning(
                        $"A system {systemType} could not be added to group {name}, because the group was not created in the world {world.Name}. Fix these errors before continuing. The system will not be added to any group and thus not update.");
                }
            }

            return groupSys as ComponentSystemGroup;
        }

        /// <summary>
        /// Calculates a list of all systems filtered with WorldSystemFilterFlags, [DisableAutoCreation] etc. Prefer
        /// GetAllSystemTypeIndices where possible to avoid extra reflection.
        /// </summary>
        /// <param name="filterFlags">The filter flags to search for.</param>
        /// <param name="requireExecuteInEditor">Optionally require that [WorldSystemFilter(WorldSystemFilterFlags.Editor)] is present on the system. This is used when creating edit mode worlds.</param>
        /// <returns>The list of filtered systems</returns>
        public static IReadOnlyList<Type> GetAllSystems(WorldSystemFilterFlags filterFlags, bool requireExecuteInEditor = false)
        {
            using var marker = new ProfilerMarker("GetAllSystems").Auto();
            var indices = GetAllSystemTypeIndices(filterFlags, requireExecuteInEditor);
            var ret = new List<Type>();
            for (int i = 0; i < indices.Length; i++)
            {
                ret.Add(TypeManager.GetSystemType(indices[i]));
            }

            return ret;
        }

        /// <summary>
        /// Calculates a list of all systems filtered with WorldSystemFilterFlags, [DisableAutoCreation] etc.
        /// Prefer this over GetAllSystems if possible, to avoid extra reflection usage.
        /// </summary>
        /// <param name="filterFlags">The filter flags to search for.</param>
        /// <param name="requireExecuteInEditor">Optionally require that [WorldSystemFilter(WorldSystemFilterFlags.Editor)] is present on the system. This is used when creating edit mode worlds.</param>
        /// <returns>The list of filtered systems</returns>
        public static NativeList<SystemTypeIndex> GetAllSystemTypeIndices(WorldSystemFilterFlags filterFlags, bool requireExecuteInEditor = false)
        {
            return TypeManager.GetSystemTypeIndices(filterFlags, requireExecuteInEditor ? WorldSystemFilterFlags.Editor : 0);
        }

        static ICustomBootstrap CreateBootStrap()
        {
            IEnumerable<Type> bootstrapTypes = TypeManager.GetTypesDerivedFrom(typeof(ICustomBootstrap));

            // Filter out types with DisableBootstrapOverridesAttribute
            var filteredBootstrapTypes = new List<Type>();
            foreach (var type in bootstrapTypes)
            {
                if (type.GetCustomAttribute<DisableBootstrapOverridesAttribute>() == null && type.Assembly.GetCustomAttribute<DisableBootstrapOverridesAttribute>() == null)
                    filteredBootstrapTypes.Add(type);
            }
            bootstrapTypes = filteredBootstrapTypes;

            Type selectedType = null;

            foreach (var bootType in bootstrapTypes)
            {
                if (bootType.IsAbstract || bootType.ContainsGenericParameters)
                    continue;

                if (selectedType == null)
                    selectedType = bootType;
                else if (selectedType.IsAssignableFrom(bootType))
                    selectedType = bootType;
                else if (!bootType.IsAssignableFrom(selectedType))
                    Debug.LogError("Multiple custom ICustomBootstrap specified, ignoring " + bootType);
            }
            ICustomBootstrap bootstrap = null;
            if (selectedType != null)
                bootstrap = Activator.CreateInstance(selectedType) as ICustomBootstrap;

            return bootstrap;
        }
    }

    /// <summary>
    /// Prevents a <see cref="ICustomBootstrap"/> from automatically being used by world initialization.
    /// </summary>
    /// <remarks>
    /// By default, all systems (classes derived from <see cref="ComponentSystemBase"/> or <see cref="ISystem"/>) are automatically discovered,
    /// instantiated, and added to the default <see cref="World"/> when that World is created.
    ///
    /// Add this attribute to a type implementing ICustomBootstrap that you do not want to be used as the default bootstrap. Note that the attribute is not
    /// inherited by any subclasses. Also note that if multiple bootstraps are found, the one with the most derived type is used unless the attribute is present.
    ///
    /// <code>
    /// using Unity.Entities;
    ///
    /// [DisableBootstrapOverrides]
    /// public class CustomBootstrap : ICustomBootstrap
    /// { // Implementation... }
    /// </code>
    ///
    /// You can also apply this attribute to an entire assembly to prevent any bootstrap in that assembly from being
    /// created automatically. This is useful for test assemblies containing many bootstraps that expect to be tested
    /// in isolation.
    ///
    /// To declare an assembly attribute, place it in any C# file compiled into the assembly, outside the namespace
    /// declaration:
    /// <code>
    /// using Unity.Entities;
    ///
    /// [assembly: DisableBootstrapOverrides]
    /// namespace Tests{}
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Assembly, Inherited=false)]
    public class DisableBootstrapOverridesAttribute : Attribute
    {
    }
}
