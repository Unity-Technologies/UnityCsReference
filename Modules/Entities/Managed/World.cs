// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using Assert = UnityEngine.Assertions.AssertCore;
using Debug = Unity.Scripting.LowLevel.Debug;
using ProfilerMarker = Unity.Entities.EntitiesProfilerMarker;

namespace Unity.Entities
{
    /// <summary>
    /// Specify all traits a <see cref="World"/> can have.
    /// </summary>
    [Flags]
    public enum WorldFlags : int
    {
        /// <summary>
        /// Default WorldFlags value.
        /// </summary>
        None       = 0,

        /// <summary>
        /// The main <see cref="World"/> for a game/application.
        /// This flag is combined with <see cref="Editor"/>, <see cref="Game"/> and <see cref="Simulation"/>.
        /// </summary>
        Live       = 1,

        /// <summary>
        /// Main <see cref="Live"/> <see cref="World"/> running in the Editor.
        /// </summary>
        Editor     = 1 << 1 | Live,

        /// <summary>
        /// Main <see cref="Live"/> <see cref="World"/> running in the Player.
        /// </summary>
        Game       = 1 << 2 | Live,

        /// <summary>
        /// Any additional <see cref="Live"/> <see cref="World"/> running in the application for background processes that
        /// queue up data for other <see cref="Live"/> <see cref="World"/> (ie. physics, AI simulation, networking, etc.).
        /// </summary>
        Simulation = 1 << 3 | Live,

        /// <summary>
        /// <see cref="World"/> on which conversion systems run to transform authoring data to runtime data.
        /// </summary>
        Conversion = 1 << 4,

        /// <summary>
        /// <see cref="World"/> in which temporary results are staged before being moved into a <see cref="Live"/> <see cref="World"/>.
        /// Typically combined with <see cref="Conversion"/> to represent an intermediate step in the full conversion process.
        /// </summary>
        Staging    = 1 << 5,

        /// <summary>
        /// <see cref="World"/> representing a previous state of another <see cref="World"/> typically to compute
        /// a diff of runtime data - for example useful for undo/redo or Live Link.
        /// </summary>
        Shadow     = 1 << 6,

        /// <summary>
        /// Dedicated <see cref="World"/> for managing incoming streamed data to the Player.
        /// </summary>
        Streaming  = 1 << 7,

        /// <summary>
        /// Server <see cref="Live"/> <see cref="World"/> running in the Player.
        /// </summary>
        GameServer = 1 << 8 | Live,

        /// <summary>
        /// Client <see cref="Live"/> <see cref="World"/> running in the Player.
        /// </summary>
        GameClient = 1 << 9 | Live,

        /// <summary>
        /// Thin client <see cref="Live"/> <see cref="World"/> running in the Player.
        /// </summary>
        GameThinClient = 1 << 10 | Live,
    }

    /// <summary>
    /// When entering playmode or the game starts in the Player a default world is created.
    /// Sometimes you need multiple worlds to be setup when the game starts or perform some
    /// custom world initialization. This lets you override the bootstrap of game code world creation.
    /// </summary>
    public interface ICustomBootstrap
    {
        /// <summary>
        /// Called during default world initialization to give the application a chance to set up additional worlds beyond
        /// (or instead of) the default world, and to perform additional one-time initialization before any worlds are created.
        /// </summary>
        /// <param name="defaultWorldName">The name of the default <see cref="World"/> that will be created</param>
        /// <returns>true if the bootstrap has performed initialization, or false if default world initialization should be performed.</returns>
        /// <remarks>
        /// If this method returns true, it must also set <see cref="World.DefaultGameObjectInjectionWorld"/> to the world that
        /// GameObject-based code and the Editor integration should use. Unity asserts if the property is still null.
        /// </remarks>
        bool Initialize(string defaultWorldName);
    }

    /// <summary>
    /// Encapsulates a set of entities, component data, and systems.
    /// </summary>
    /// <remarks>Multiple Worlds may exist concurrently in the same application, but they are isolated from each other;
    /// an <see cref="EntityQuery"/> will only match entities in the World that created the query, a World's <see cref="EntityManager"/> can only
    /// process entities from the same World, etc.</remarks>
    [DebuggerDisplay("{Name} - {Flags} (#{SequenceNumber})")]
    [DebuggerTypeProxy(typeof(WorldDebugView))]
    public unsafe partial class World : IDisposable
    {
        [NoAutoStaticsCleanup] // DefaultWorldInitialization disposes every world as code unloads.
        internal static readonly List<World> s_AllWorlds = new List<World>();

        /// <summary>
        /// The default <see cref="World"/>. The code that runs outside a system reads this property to reach a world.
        /// </summary>
        /// <remarks>
        /// <see cref="DefaultWorldInitialization.Initialize"/> assigns this property, so it's null until Unity creates the default
        /// world, and null again after you dispose that world.
        ///
        /// Code that runs outside a system has no world to work from, so it reads this property instead. This applies both to
        /// your own MonoBehaviour scripts and to Editor tools such as the SubScene Inspector and the Entity Inspector. The most
        /// common use in your own code is to reach an <see cref="EntityManager"/>, for example
        /// `World.DefaultGameObjectInjectionWorld.EntityManager`.
        ///
        /// Assigning a different world to this property redirects all of those callers to that world.
        /// </remarks>
        /// <seealso cref="ICustomBootstrap"/>
        /// <seealso cref="DefaultWorldInitialization"/>
        [NoAutoStaticsCleanup]
        public static World DefaultGameObjectInjectionWorld { get; set; }

        internal Dictionary<Type, ComponentSystemBase> m_SystemLookup = new Dictionary<Type, ComponentSystemBase>();

        /// <summary>
        /// List of all Worlds that currently exist in the application
        /// </summary>
        [NoAutoStaticsCleanup]
        public static NoAllocReadOnlyCollection<World> All { get; } = new NoAllocReadOnlyCollection<World>(s_AllWorlds);

        /// <inheritdoc cref="WorldUnmanaged.NextSequenceNumber"/>
        public static ulong NextSequenceNumber => WorldUnmanaged.NextSequenceNumber.Data;

        /// <summary>
        /// Event invoked after system has been fully constructed.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<World, ComponentSystemBase> SystemCreated;

        /// <summary>
        /// Event invoked before system is disposed.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<World, ComponentSystemBase> SystemDestroyed;

        List<ComponentSystemBase> m_Systems = new List<ComponentSystemBase>();
        /// <summary>
        /// List of all managed systems in this World
        /// </summary>
        public NoAllocReadOnlyCollection<ComponentSystemBase> Systems { get; }

        [NoAutoStaticsCleanup]
        static ProfilerMarker s_NewWorldMarker = new ProfilerMarker("new World()");
        [NoAutoStaticsCleanup]
        static ProfilerMarker s_DisposeWorldMarker = new ProfilerMarker("World Dispose()");

        private WorldUnmanaged m_Unmanaged;

        /// <summary>
        /// Gives access to the unmanaged data of an instance of the World. This is useful when your code needs to run
        /// with Burst.
        /// </summary>
        public WorldUnmanaged Unmanaged => m_Unmanaged;

        /// <inheritdoc cref="WorldUnmanaged.Flags"/>
        public WorldFlags Flags => m_Unmanaged.Flags;

        /// <inheritdoc cref="WorldUnmanaged.Name"/>
        public string Name { get; }

        /// <summary>
        /// Returns the name of the World
        /// </summary>
        /// <returns>The World's name</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <inheritdoc cref="WorldUnmanaged.Version"/>
        public int Version => m_Unmanaged.Version;

        /// <inheritdoc cref="WorldUnmanaged.EntityManager"/>
        public EntityManager EntityManager => m_Unmanaged.EntityManager;

        /// <inheritdoc cref="WorldUnmanaged.IsCreated"/>
        public bool IsCreated => m_Unmanaged.IsCreated;

        /// <inheritdoc cref="WorldUnmanaged.SequenceNumber"/>
        public ulong SequenceNumber => m_Unmanaged.SequenceNumber;

        /// <inheritdoc cref="WorldUnmanaged.Time"/>
        public ref TimeData Time => ref m_Unmanaged.Time;

        /// <summary>
        /// Property to get and set enable block free flag, a flag indicating whether the allocator should enable individual block to be freed.
        /// </summary>
        public bool UpdateAllocatorEnableBlockFree
        {
            get => m_Unmanaged.UpdateAllocatorEnableBlockFree;
            set => m_Unmanaged.UpdateAllocatorEnableBlockFree = value;
        }

        /// <inheritdoc cref="WorldUnmanaged.MaximumDeltaTime"/>
        public float MaximumDeltaTime
        {
            get => m_Unmanaged.MaximumDeltaTime;
            set => m_Unmanaged.MaximumDeltaTime = value;
        }

        private EntityQuery m_TimeSingletonQuery;

        /// <summary>
        /// Construct a new World instance
        /// </summary>
        /// <param name="name">The name to assign to the new World</param>
        /// <param name="flags">The flags to assign to the new World</param>
        public World(string name, WorldFlags flags = WorldFlags.Simulation)
        {
            Name = name;
            Systems = new NoAllocReadOnlyCollection<ComponentSystemBase>(m_Systems);

            Init(flags, Allocator.Persistent);
        }

        /// <summary>
        /// Construct a new World instance
        /// </summary>
        /// <param name="name">The name to assign to the new World</param>
        /// <param name="flags">The flags to assign to the new World</param>
        /// <param name="backingAllocatorHandle">The allocator to use for any of the world's internal memory allocations</param>
        public World(string name, WorldFlags flags, AllocatorManager.AllocatorHandle backingAllocatorHandle)
        {
            Name = name;
            Systems = new NoAllocReadOnlyCollection<ComponentSystemBase>(m_Systems);

            Init(flags, backingAllocatorHandle);
        }

        void Init(WorldFlags flags, AllocatorManager.AllocatorHandle backingAllocatorHandle)
        {
            s_NewWorldMarker.Begin();

            SystemState.InitSystemIdCell();

            m_Unmanaged.Create(this, flags, backingAllocatorHandle);

            s_AllWorlds.Add(this);

            m_TimeSingletonQuery = EntityManager.CreateEntityQuery( 
                ComponentType.ReadWrite<WorldTime>(),
                ComponentType.ReadWrite<WorldTimeQueue>(),
                ComponentType.ReadWrite<HideInHierarchy>());

            s_NewWorldMarker.End();
        }

        /// <summary>
        /// Dispose of a World instance
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the World has already been disposed, or if one of the World's systems is still executing.</exception>
        public void Dispose()
        {
            if (!IsCreated)
                throw new ArgumentException("The World has already been Disposed.");

            if (Unmanaged.ExecutingSystem != SystemHandle.Null)
                throw new ArgumentException("The World can not be disposed while a system in that world is executing " + Unmanaged.ExecutingSystem.m_WorldSeqNo);

            s_DisposeWorldMarker.Begin();

            // Debug.LogError("Dispose World "+ Name + " - " + GetHashCode());

            m_Unmanaged.EntityManager.PreDisposeCheck();


            // We don't want any jobs making changes to this world as we are disposing it.
            // This could be particularly bad if we are destroying blobs referenced by Components as a job attempts to access them.
            EntityManager.ExclusiveEntityTransactionDependency.Complete();
            EntityManager.EndExclusiveEntityTransaction();
            EntityManager.CompleteAllTrackedJobs();
            m_Unmanaged.AllowGetSystem = true;

            DestroyAllSystemsAndLogException(out bool errorsWhileDestroyingSystems);
            m_SystemLookup = null;
            m_Systems = null;

            m_Unmanaged.AllowGetSystem = false;

            m_TimeSingletonQuery.Dispose();
            s_AllWorlds.Remove(this);

            if (DefaultGameObjectInjectionWorld == this)
                DefaultGameObjectInjectionWorld = null;

            m_Unmanaged.Dispose();

            s_DisposeWorldMarker.End();
        }

        /// <summary>
        /// Destroys all existing Worlds
        /// </summary>
        public static void DisposeAllWorlds()
        {
            while (s_AllWorlds.Count != 0)
            {
                s_AllWorlds[0].Dispose();
            }
        }

        // Time management

        /// <summary>
        /// Singleton instance of the <see cref="WorldTime"/> component. This data is generally accessed through <see cref="World.Time"/>.
        /// </summary>
        protected Entity TimeSingleton
        {
            get
            {
                if (m_TimeSingletonQuery.IsEmptyIgnoreFilter)
                {
                    var timeTypes = stackalloc ComponentType[3];
                    timeTypes[0] = ComponentType.ReadWrite<WorldTime>();
                    timeTypes[1] = ComponentType.ReadWrite<WorldTimeQueue>();
                    timeTypes[2] = ComponentType.ReadWrite<HideInHierarchy>();
                    var entity = EntityManager.CreateEntity(EntityManager.CreateArchetype(timeTypes, 3));
                    EntityManager.SetName(entity, "WorldTime");
                }

                return m_TimeSingletonQuery.GetSingletonEntity();
            }
        }

        /// <summary>
        /// Assigns a new value to the World's current time.
        /// </summary>
        /// <param name="newTimeData">The new time to assign for this World</param>
        public void SetTime(TimeData newTimeData)
        {
            EntityManager.SetComponentData(TimeSingleton, new WorldTime() {Time = newTimeData});
            this.Time = newTimeData;
        }

        /// <summary>
        /// Push a new temporary time value to the World's current time
        /// </summary>
        /// <remarks>This is generally used to temporarily override the time for a world for the duration of a single
        /// <see cref="ComponentSystemGroup"/> update, when using fixed-timestep semantics. The original time can
        /// subsequently be restored with <see cref="PopTime"/>.</remarks>
        /// <param name="newTimeData">The temporary TimeData.</param>
        /// <seealso cref="PopTime"/>
        public void PushTime(TimeData newTimeData)
        {
            var queue = EntityManager.GetBuffer<WorldTimeQueue>(TimeSingleton);
            queue.Add(new WorldTimeQueue() { Time = this.Time });
            SetTime(newTimeData);
        }

        /// <summary>
        /// Restore the previous World time, after pushing a temporary time value with <see cref="PushTime"/>
        /// </summary>
        /// <seealso cref="PushTime"/>
        public void PopTime()
        {
            var queue = EntityManager.GetBuffer<WorldTimeQueue>(TimeSingleton);

            Assert.IsTrue(queue.Length > 0, "PopTime without a matching PushTime");

            var prevTime = queue[queue.Length - 1];
            queue.RemoveAt(queue.Length - 1);
            SetTime(prevTime.Time);
        }

        // Internal system management

        ComponentSystemBase CreateSystemInternal(SystemTypeIndex type)
        {
            var system = AllocateSystemInternal(type);
            AddSystem_Add_Internal(system);
            AddSystem_OnCreate_Internal(system);
            return system;
        }

        ComponentSystemBase AllocateSystemInternal(SystemTypeIndex type)
        {
            if (!m_Unmanaged.AllowGetSystem)
                throw new ArgumentException(
                    "During destruction of a system you are not allowed to create more systems.");

            return TypeManager.ConstructSystem(TypeManager.GetSystemType(type));
        }

        ComponentSystemBase GetExistingSystemInternal(SystemTypeIndex type)
        {
            if (m_SystemLookup.TryGetValue(TypeManager.GetSystemType(type), out var system))
                return system;

            return null;
        }

        internal void AddTypeLookupInternal(Type type, ComponentSystemBase system)
        {
            while (type != typeof(ComponentSystemBase))
            {
                // Due to the order of registration, a derived type instance may already be registered to this base type key.
                // If the existing system's actual type doesn't match 'type', overwrite it with the correct instance.
                if (m_SystemLookup.TryGetValue(type, out var existingSystem))
                {
                    var existingSystemType = existingSystem.GetType();
                    if (existingSystemType != type && existingSystemType != system.GetType())
                        m_SystemLookup[type] = system;
                }
                else
                {
                    m_SystemLookup.Add(type, system);
                }

                type = type.BaseType;
            }
        }

        void AddSystem_Add_Internal(ComponentSystemBase system)
        {
            m_Systems.Add(system);
            var systemType = system.GetType();

            system.m_StatePtr = m_Unmanaged.AllocateSystemStateForManagedSystem(this, system);

            AddTypeLookupInternal(systemType, system);

            UnityEngine.Assertions.AssertCore.AreEqual(system,
                (ComponentSystemBase)
                m_Unmanaged.ResolveSystemState(system.SystemHandle)->m_ManagedSystem
                    .Target);

            m_Unmanaged.GetImpl().sysHandlesInCreationOrder.Add(new PerWorldSystemInfo
            {
                handle = system.SystemHandle,
                systemTypeIndex = TypeManager.GetSystemTypeIndex(systemType)
            });
        }

        void AddSystem_OnCreate_Internal(ComponentSystemBase system)
        {
            try
            {
                system.CreateInstance(this);
            }
            catch
            {
                RemoveSystemInternal(system);
                throw;
            }
            m_Unmanaged.BumpVersion();

            EntitiesProfiler.OnSystemCreated(system.m_StatePtr->m_SystemTypeIndex, system.SystemHandle);

            SystemCreated?.Invoke(this, system);
        }

        void RemoveSystemInternal(ComponentSystemBase system)
        {
            ref var list = ref Unmanaged.GetImpl().sysHandlesInCreationOrder;
            int toremove = -1;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].handle == system.SystemHandle)
                {
                    toremove = i;
                    break;
                }
            }

            if (toremove != -1)
            {
                list.RemoveAt(toremove);
            }

            if (!m_Systems.Remove(system))
                throw new ArgumentException($"System does not exist in the world");

            m_Unmanaged.BumpVersion();

            var type = system.GetType();
            while (type != typeof(ComponentSystemBase))
            {
                var haskey = m_SystemLookup.TryGetValue(type, out var lookedUpSystem);
                if (haskey && lookedUpSystem == system)
                {
                    m_SystemLookup.Remove(type);

                    foreach (var otherSystem in m_Systems)
                    {
                        var otherSystemType = otherSystem.GetType();

                        if (type != otherSystemType && type.IsAssignableFrom(otherSystemType))
                        {
                            AddTypeLookupInternal(otherSystemType, otherSystem);
                            break;
                        }
                    }
                }

                type = type.BaseType;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void CheckGetOrCreateSystem()
        {
            if (!IsCreated)
                throw new ObjectDisposedException("The World has already been Disposed.");

            if (!m_Unmanaged.AllowGetSystem)
                throw new ArgumentException("You are not allowed to get or create more systems during destruction of a system.");
        }

        // Public system management

        /// <summary>
        /// Retrieve the handle for the instance of a system of type <typeparamref name="T"/> from the current World. If the system
        /// does not exist in this World, it will first be created.
        /// </summary>
        /// <remarks>
        /// **Important:** This function creates a sync point if a system is created, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <typeparam name="T">The system type</typeparam>
        /// <returns>The handle for the instance of system type <typeparamref name="T"/> in this World. If the system
        /// does not exist in this World, it will first be created.</returns>
        public SystemHandle GetOrCreateSystem<T>() where T : ComponentSystemBase
        {
            CheckGetOrCreateSystem();

            var systemTypeIndex = TypeManager.GetSystemTypeIndex<T>();
            var system = GetExistingSystemInternal(systemTypeIndex);
            return system == null ? CreateSystemInternal(systemTypeIndex).SystemHandle : system.SystemHandle;
        }

        /// <summary>
        /// Retrieve the instance of a system of type <typeparamref name="T"/> from the current World. If the system
        /// does not exist in this World, it will first be created.
        /// </summary>
        /// <remarks>
        /// **Important:** This function creates a sync point if a system is created, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        ///
        /// **Note:** This system reference is not guaranteed to be safe to use. If the system or world is destroyed then the OnDestroy
        /// and cleanup functionality will have been called for this system.
        ///
        /// If possible, using <see cref="GetOrCreateSystem"/> is preferred, and instead of public member data, component data is recommended for
        /// system level data that needs to be shared between systems or externally to them. This defines a data protocol for the
        /// system which is separated from the system functionality.
        ///
        /// Private member data which is only used internally to the system is recommended.
        ///
        /// Keep in mind using a managed reference for systems
        /// - encourages coupling of data and functionality
        /// - couples data to the system type with no direct path to decouple
        /// - does not provide lifetime or thread safety guarantees for data access
        /// - does not provide lifetime or thread safety guarantees for system access through the returned managed reference
        /// </remarks>
        /// <typeparam name="T">The system type</typeparam>
        /// <returns>The instance of system type <typeparamref name="T"/> in this World. If the system
        /// does not exist in this World, it will first be created.</returns>
        public T GetOrCreateSystemManaged<T>() where T : ComponentSystemBase
        //sadly, we have to use reflection to account for the fact that T might not have been registered at startup.
        //someday, we can ban this and avoid reflection here.
        {
            var idx = TypeManager.GetSystemTypeIndexNoThrow<T>();
            if (idx == SystemTypeIndex.Null)
            {
                TypeManager.AddSystemTypeToTablesAfterInit(typeof(T));
                idx = TypeManager.GetSystemTypeIndex<T>();
            }
            return (T)GetOrCreateSystemManaged(idx);
        }

        /// <summary>
        /// Retrieve the handle for the instance of a system of type <paramref name="type"/> from the current World. If the system
        /// does not exist in this World, it will first be created.
        /// </summary>
        /// <remarks>
        /// **Important:** This function creates a sync point if a system is created, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <param name="type">The system type</param>
        /// <returns>The handle for the instance of system type <paramref name="type"/> in this World. If the system
        /// does not exist in this World, it will first be created.</returns>
        public SystemHandle GetOrCreateSystem(Type type)
        {
            return GetOrCreateSystem(TypeManager.GetSystemTypeIndex(type));
        }

        /// <summary>
        /// Retrieve the handle for the instance of a system of type <paramref name="type"/> from the current World. If the system
        /// does not exist in this World, it will first be created.
        /// </summary>
        /// <param name="type">The system type</param>
        /// <returns>The handle for the instance of system type <paramref name="type"/> in this World. If the system
        /// does not exist in this World, it will first be created.</returns>
        public SystemHandle GetOrCreateSystem(SystemTypeIndex type)
        {
            CheckGetOrCreateSystem();

            if (type.IsManaged)
            {
                var system = GetExistingSystemInternal(type);
                return system == null ? CreateSystemInternal(type).SystemHandle : system.SystemHandle;
            }

            return Unmanaged.GetOrCreateUnmanagedSystem(type);
        }

        /// <summary>
        /// Retrieve the instance of a system of type <paramref name="type"/> from the current World. If the system
        /// does not exist in this World, it will first be created.
        /// </summary>
        /// <remarks>
        /// **Important:** This function creates a sync point if a system is created, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        ///
        /// **Note:** This system reference is not guaranteed to be safe to use. If the system or world is destroyed then the OnDestroy
        /// and cleanup functionality will have been called for this system.
        ///
        /// If possible, using <see cref="GetOrCreateSystem"/> is preferred, and instead of public member data, component data is recommended for
        /// system level data that needs to be shared between systems or externally to them. This defines a data protocol for the
        /// system which is separated from the system functionality.
        ///
        /// Private member data which is only used internally to the system is recommended.
        ///
        /// Keep in mind using a managed reference for systems
        /// - encourages coupling of data and functionality
        /// - couples data to the system type with no direct path to decouple
        /// - does not provide lifetime or thread safety guarantees for data access
        /// - does not provide lifetime or thread safety guarantees for system access through the returned managed reference
        /// </remarks>
        /// <param name="type">The system type</param>
        /// <returns>The instance of system type <paramref name="type"/> in this World. If the system
        /// does not exist in this World, it will first be created.</returns>
        public ComponentSystemBase GetOrCreateSystemManaged(Type type)
        {
            CheckGetOrCreateSystem();

            return GetOrCreateSystemManaged(TypeManager.GetSystemTypeIndex(type));
        }

        /// <summary>
        /// Retrieve the instance of a system of type <paramref name="type"/> from the current World. If the system
        /// does not exist in this World, it will first be created.
        /// </summary>
        /// <param name="type">The system type</param>
        /// <returns>The instance of system type <paramref name="type"/> in this World. If the system
        /// does not exist in this World, it will first be created.</returns>
        public ComponentSystemBase GetOrCreateSystemManaged(SystemTypeIndex type)
        {
            CheckGetOrCreateSystem();

            var system = GetExistingSystemInternal(type);
            return system ?? CreateSystemInternal(type);
        }

        /// <summary>
        /// Create and return a handle to an instance of a system of type <typeparamref name="T"/> in this World.
        /// </summary>
        /// <remarks>
        /// This can result in multiple instances of the same system in a single World, which is generally undesirable.
        ///
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <typeparam name="T">The system type</typeparam>
        /// <returns>A handle to the new instance of system type <typeparamref name="T"/> in this World.</returns>
        public SystemHandle CreateSystem<T>() where T : ComponentSystemBase, new()
        {
            CheckGetOrCreateSystem();
            return CreateSystemInternal(TypeManager.GetSystemTypeIndex<T>()).SystemHandle;
        }

        /// <summary>
        /// Create and return an instance of a system of type <typeparamref name="T"/> in this World.
        /// </summary>
        /// <remarks>
        /// This can result in multiple instances of the same system in a single World, which is generally undesirable.
        ///
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        ///
        /// **Note:** This system reference is not guaranteed to be safe to use. If the system or world is destroyed then the OnDestroy
        /// and cleanup functionality will have been called for this system.
        ///
        /// If possible, using <see cref="CreateSystem"/> is preferred, and instead of public member data, component data is recommended for
        /// system level data that needs to be shared between systems or externally to them. This defines a data protocol for the
        /// system which is separated from the system functionality.
        ///
        /// Private member data which is only used internally to the system is recommended.
        ///
        /// Keep in mind using a managed reference for systems
        /// - encourages coupling of data and functionality
        /// - couples data to the system type with no direct path to decouple
        /// - does not provide lifetime or thread safety guarantees for data access
        /// - does not provide lifetime or thread safety guarantees for system access through the returned managed reference
        ///
        /// If <typeparamref name="T"/> has not been registered with the TypeManager, this method will register it before adding the system.
        /// </remarks>
        /// <typeparam name="T">The system type</typeparam>
        /// <returns>The new instance of system type <typeparamref name="T"/> in this World.</returns>
        public T CreateSystemManaged<T>() where T : ComponentSystemBase, new()
        {
            var idx = TypeManager.GetSystemTypeIndexNoThrow<T>();
            if (idx <= 0)
            {
                TypeManager.AddSystemTypeToTablesAfterInit(typeof(T));
                idx = TypeManager.GetSystemTypeIndex<T>();
            }

            return (T)CreateSystemManaged(idx);
        }

        /// <summary>
        /// Create and return a handle to an instance of a system of type <paramref name="type"/> in this World.
        /// </summary>
        /// <remarks>
        /// This can result in multiple instances of the same system in a single World, which is generally undesirable.
        ///
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <param name="type">The system type</param>
        /// <returns>A handle to the new instance of system type <paramref name="type"/> in this World.</returns>
        public SystemHandle CreateSystem(Type type)
        {
            return CreateSystem(TypeManager.GetSystemTypeIndex(type));
        }

        /// <summary>
        /// Create and return a handle to an instance of a system of type <paramref name="type"/> in this World.
        /// </summary>
        /// <param name="type">The system type</param>
        /// <returns>A handle to the new instance of system type <paramref name="type"/> in this World.</returns>
        public SystemHandle CreateSystem(SystemTypeIndex type)
        {
            CheckGetOrCreateSystem();

            if (type.IsManaged)
                return CreateSystemInternal(type).SystemHandle;

            return Unmanaged.GetOrCreateUnmanagedSystem(type);
        }

        /// <summary>
        /// Create and return an instance of a system of type <paramref name="type"/> in this World.
        /// </summary>
        /// <remarks>
        /// This can result in multiple instances of the same system in a single World, which is generally undesirable.
        ///
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        ///
        /// **Note:** This system reference is not guaranteed to be safe to use. If the system or world is destroyed then the OnDestroy
        /// and cleanup functionality will have been called for this system.
        ///
        /// If possible, using <see cref="CreateSystem"/> is preferred, and instead of public member data, component data is recommended for
        /// system level data that needs to be shared between systems or externally to them. This defines a data protocol for the
        /// system which is separated from the system functionality.
        ///
        /// Private member data which is only used internally to the system is recommended.
        ///
        /// Keep in mind using a managed reference for systems
        /// - encourages coupling of data and functionality
        /// - couples data to the system type with no direct path to decouple
        /// - does not provide lifetime or thread safety guarantees for data access
        /// - does not provide lifetime or thread safety guarantees for system access through the returned managed reference
        /// </remarks>
        /// <param name="type">The system type</param>
        /// <returns>The new instance of system type <paramref name="type"/> in this World.</returns>
        public ComponentSystemBase CreateSystemManaged(Type type)
        {
            return CreateSystemManaged(TypeManager.GetSystemTypeIndex(type));
        }

        /// <summary>
        /// Create and return an instance of a system of type <paramref name="type"/> in this World.
        /// </summary>
        /// <param name="type">The system type</param>
        /// <returns>The new instance of system type <paramref name="type"/> in this World.</returns>
        public ComponentSystemBase CreateSystemManaged(SystemTypeIndex type)
        {
            CheckGetOrCreateSystem();
            return CreateSystemInternal(type);
        }

        /// <summary> Obsolete. Use <see cref="AddSystemManaged{T}(T)"/> instead.</summary>
        /// <typeparam name="T">The system type</typeparam>
        /// <param name="system">The existing system instance to add</param>
        /// <returns>The input <paramref name="system"/></returns>
        [Obsolete("(UnityUpgradable) -> AddSystemManaged<T>(*)", true)]
        public T AddSystem<T>(T system) where T : ComponentSystemBase
            => AddSystemManaged(system);

        /// <summary>
        /// Adds an existing system instance to this World
        /// </summary>
        /// <remarks>
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before adding the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        ///
        /// **Note:** This system reference is not guaranteed to be safe to use. If the system or world is destroyed then the OnDestroy
        /// and cleanup functionality will have been called for this system.
        ///
        /// If possible, using <see cref="CreateSystem"/> is preferred, and instead of public member data, component data is recommended for
        /// system level data that needs to be shared between systems or externally to them. This defines a data protocol for the
        /// system which is separated from the system functionality.
        ///
        /// Private member data which is only used internally to the system is recommended.
        ///
        /// Keep in mind using a managed reference for systems
        /// - encourages coupling of data and functionality
        /// - couples data to the system type with no direct path to decouple
        /// - does not provide lifetime or thread safety guarantees for data access
        /// - does not provide lifetime or thread safety guarantees for system access through the returned managed reference
        ///
        /// If <typeparamref name="T"/> has not been registered with the TypeManager, this method will register it before adding the system.
        /// </remarks>
        /// <typeparam name="T">The system type</typeparam>
        /// <param name="system">The existing system instance to add</param>
        /// <returns>The input <paramref name="system"/></returns>
        /// <exception cref="Exception">Thrown if a system of type <typeparamref name="T"/> already exists in this World</exception>
        public T AddSystemManaged<T>(T system) where T : ComponentSystemBase
        {
            CheckGetOrCreateSystem();
            var systemTypeIndex = TypeManager.GetSystemTypeIndexNoThrow<T>();
            if (systemTypeIndex <= 0)
            {
                TypeManager.AddSystemTypeToTablesAfterInit(typeof(T));
                systemTypeIndex = TypeManager.GetSystemTypeIndex<T>();
            }
            if (GetExistingSystemInternal(systemTypeIndex) != null)
                throw new Exception($"Attempting to add system '{TypeManager.GetSystemName(systemTypeIndex)}' which has already been added to world '{Name}'");

            AddSystem_Add_Internal(system);
            AddSystem_OnCreate_Internal(system);
            return (T)system;
        }

        /// <summary>
        /// Return a handle to an existing instance of a system of type <typeparamref name="T"/> in this World.
        /// </summary>
        /// <typeparam name="T">The system type</typeparam>
        /// <returns>A handle to the existing instance of system type <typeparamref name="T"/> in this World. If no such instance exists, the method returns default.</returns>
        public SystemHandle GetExistingSystem<T>() where T : ComponentSystemBase
            => GetExistingSystem(TypeManager.GetSystemTypeIndex<T>());

        /// <summary>
        /// Return an existing instance of a system of type <typeparamref name="T"/> in this World.
        /// </summary>
        /// <remarks>
        /// **Note:** This system reference is not guaranteed to be safe to use. If the system or world is destroyed then the OnDestroy
        /// and cleanup functionality will have been called for this system.
        ///
        /// If possible, using <see cref="GetExistingSystem"/> is preferred, and instead of public member data, component data is recommended for
        /// system level data that needs to be shared between systems or externally to them. This defines a data protocol for the
        /// system which is separated from the system functionality.
        ///
        /// Private member data which is only used internally to the system is recommended.
        ///
        /// Keep in mind using a managed reference for systems
        /// - encourages coupling of data and functionality
        /// - couples data to the system type with no direct path to decouple
        /// - does not provide lifetime or thread safety guarantees for data access
        /// - does not provide lifetime or thread safety guarantees for system access through the returned managed reference
        /// </remarks>
        /// <typeparam name="T">The system type</typeparam>
        /// <returns>The existing instance of system type <typeparamref name="T"/> in this World. If no such instance exists, the method returns null.</returns>
        public T GetExistingSystemManaged<T>() where T : ComponentSystemBase
            => (T)GetExistingSystemManaged(typeof(T));

        /// <summary>
        /// Return a handle to an existing instance of a system of type <paramref name="type"/> in this World. Prefer
        /// the version that takes a SystemTypeIndex where possible to avoid unnecessary reflection.
        /// </summary>
        /// <param name="type">The system type</param>
        /// <returns>A handle to the existing instance of system type <paramref name="type"/> in this World. If no such instance exists, the method returns default.</returns>
        public SystemHandle GetExistingSystem(Type type)
        {
            CheckGetOrCreateSystem();

            return GetExistingSystem(TypeManager.GetSystemTypeIndex(type));
        }

        /// <summary>
        /// Return a handle to an existing instance of a system of type <paramref name="type"/> in this World. This
        /// version avoids unnecessary reflection.
        /// </summary>
        /// <param name="type">The system type</param>
        /// <returns>A handle to the existing instance of system type <paramref name="type"/> in this World. If no such instance exists, the method returns default.</returns>
        public SystemHandle GetExistingSystem(SystemTypeIndex type)
        {
            CheckGetOrCreateSystem();

            if (type.IsManaged)
            {
                var system = GetExistingSystemInternal(type);
                return system == null ? default : system.SystemHandle;
            }

            return Unmanaged.GetExistingUnmanagedSystem(type);
        }

        /// <summary>
        /// Return an existing instance of a system of type <paramref name="type"/> in this World. Prefer the version
        /// that takes a SystemTypeIndex where possible to avoid unnecessary reflection.
        /// </summary>
        /// <remarks>
        /// **Note:** This system reference is not guaranteed to be safe to use. If the system or world is destroyed then the OnDestroy
        /// and cleanup functionality will have been called for this system.
        ///
        /// If possible, using <see cref="GetExistingSystem"/> is preferred, and instead of public member data, component data is recommended for
        /// system level data that needs to be shared between systems or externally to them. This defines a data protocol for the
        /// system which is separated from the system functionality.
        ///
        /// Private member data which is only used internally to the system is recommended.
        ///
        /// Keep in mind using a managed reference for systems
        /// - encourages coupling of data and functionality
        /// - couples data to the system type with no direct path to decouple
        /// - does not provide lifetime or thread safety guarantees for data access
        /// - does not provide lifetime or thread safety guarantees for system access through the returned managed reference
        /// </remarks>
        /// <param name="type">The system type</param>
        /// <returns>The existing instance of system type <paramref name="type"/> in this World. If no such instance exists, the method returns null.</returns>
        public ComponentSystemBase GetExistingSystemManaged(Type type)
        {
            return GetExistingSystemManaged(TypeManager.GetSystemTypeIndex(type));
        }

        /// <summary>
        /// Return an existing instance of a system of type <paramref name="type"/> in this World. This avoids
        /// unnecessary reflection.
        /// </summary>
        /// <remarks>
        /// **Note:** This system reference is not guaranteed to be safe to use. If the system or world is destroyed then the OnDestroy
        /// and cleanup functionality will have been called for this system.
        ///
        /// If possible, using <see cref="GetExistingSystem"/> is preferred, and instead of public member data, component data is recommended for
        /// system level data that needs to be shared between systems or externally to them. This defines a data protocol for the
        /// system which is separated from the system functionality.
        ///
        /// Private member data which is only used internally to the system is recommended.
        ///
        /// Keep in mind using a managed reference for systems
        /// - encourages coupling of data and functionality
        /// - couples data to the system type with no direct path to decouple
        /// - does not provide lifetime or thread safety guarantees for data access
        /// - does not provide lifetime or thread safety guarantees for system access through the returned managed reference
        /// </remarks>
        /// <param name="type">The system type</param>
        /// <returns>The existing instance of system type <paramref name="type"/> in this World. If no such instance exists, the method returns null.</returns>

        public ComponentSystemBase GetExistingSystemManaged(SystemTypeIndex type)
        {
            CheckGetOrCreateSystem();
            return GetExistingSystemInternal(type);
        }

        /// <summary>
        /// Destroys one of the World's existing system instances.
        /// </summary>
        /// <remarks>
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before destroying the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <param name="sysHandle">The system to destroy. Must be an existing instance in this World.</param>
        /// <exception cref="ArgumentException">Thrown if any of the World's systems are currently executing.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the system handle is invalid or does not belong to this world.</exception>
        public void DestroySystem(SystemHandle sysHandle)
        {
            CheckGetOrCreateSystem();
            if (Unmanaged.ExecutingSystem != default)
                throw new ArgumentException("A system can not be disposed while another system in that world is executing");

            var sysState = Unmanaged.ResolveSystemStateChecked(sysHandle);
            if (sysState != null && sysState->m_ManagedSystem.IsAllocated)
            {
                DestroySystemManaged(sysState->ManagedSystem);
                return;
            }

            Unmanaged.DestroyUnmanagedSystem(sysHandle);
        }

        /// <summary>
        /// Destroys one of the World's existing system instances.
        /// </summary>
        /// <remarks>
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before destroying the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <param name="system">The system to destroy. Must be an existing instance in this World.</param>
        /// <exception cref="ArgumentException">Thrown if any of the World's systems are currently executing.</exception>
        public void DestroySystemManaged(ComponentSystemBase system)
        {
            CheckGetOrCreateSystem();
            if (Unmanaged.ExecutingSystem != default)
                throw new ArgumentException("A system can not be disposed while another system in that world is executing");

            SystemDestroyed?.Invoke(this, system);
            RemoveSystemInternal(system);
            system.DestroyInstance();
        }

        /// <summary>
        /// Destroy all system instances in the World. Any errors encountered during individual system destruction will be logged to the console.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if any of the World's systems are currently executing.</exception>
        [Obsolete("This function now takes an out bool parameter to report if any exceptions were logged.", false)]
        public void DestroyAllSystemsAndLogException()
        {
            DestroyAllSystemsAndLogException(out _);
        }
        /// <summary>
        /// Destroy all system instances in the World. Any errors encountered during individual system destruction will be logged to the console.
        /// </summary>
        /// <param name="loggedException">If one or more exceptions were logged while destroying systems, this value will be set to true.
        /// This allows callers to detect whether any errors were encountered. If no exceptions were logged, this value will be set to false.</param>
        /// <exception cref="ArgumentException">Thrown if any of the World's systems are currently executing.</exception>
        public void DestroyAllSystemsAndLogException(out bool loggedException)
        {
            loggedException = false;
            if (!IsCreated)
            {
                return;
            }

            if (Unmanaged.ExecutingSystem != default)
                throw new ArgumentException($"{nameof(DestroyAllSystemsAndLogException)} while another system is running on the same world is not allowed.");

            // Systems are destroyed in reverse order from construction, in three phases:
            // 1. Stop all systems from running (if they weren't already stopped), to ensure OnStopRunning() is called.
            // 2. Call each system's OnDestroy() method
            // 3. Actually destroy each system
            var sysHandlesInCreationOrder = Unmanaged.GetImpl().sysHandlesInCreationOrder;
            for (int i = sysHandlesInCreationOrder.Length - 1; i >= 0; i--)
            {
                var system = sysHandlesInCreationOrder[i].handle;
                var state = Unmanaged.ResolveSystemState(system);
                if (state == null) continue;
                if (state->m_ManagedSystem.IsAllocated)
                {
                    try
                    {
                        SystemDestroyed?.Invoke(this, m_Systems[i]);
                        state->ManagedSystem.OnBeforeDestroyInternal();
                    }
                    catch (Exception e)
                    {
                        loggedException = true;
                        Debug.LogException(e);
                    }
                }
                else
                {
                    if (state->PreviouslyEnabled &&
                        (TypeManager.GetSystemTypeFlags(sysHandlesInCreationOrder[i].systemTypeIndex) &
                         TypeManager.SystemTypeInfo.kIsSystemISystemStartStopFlag) !=
                        0)
                    {
                        SystemBaseRegistry.CallOnStopRunning(state);
                    }
                }

            }

            for (int i = sysHandlesInCreationOrder.Length - 1; i >= 0; --i)
            {
                var system = sysHandlesInCreationOrder[i].handle;

                bool prevAllow = m_Unmanaged.AllowGetSystem;
                try
                {
                    var state = Unmanaged.ResolveSystemState(system);

                    if (state == null) continue;
                    m_Unmanaged.AllowGetSystem = false;
                    if (state->m_ManagedSystem.IsAllocated)
                    {
                        state->ManagedSystem.OnDestroy_Internal();
                    }
                    else
                    {
                        SystemBaseRegistry.CallOnDestroy(state);
                    }
                }
                catch (Exception e)
                {
                    loggedException = true;
                    Debug.LogException(e);
                }
                finally
                {
                    m_Unmanaged.AllowGetSystem = prevAllow;
                }
            }
            for (int i = sysHandlesInCreationOrder.Length - 1; i >= 0; --i)
            {
                var system = sysHandlesInCreationOrder[i].handle;

                try
                {
                    var state = Unmanaged.ResolveSystemState(system);
                    if (state == null) continue;
                    if (state->m_ManagedSystem.IsAllocated)
                        state->ManagedSystem.OnAfterDestroyInternal();
                    else
                    {
                        ref var impl = ref m_Unmanaged.GetImpl();
                        impl.InvalidateSystemHandle(system);
                        impl.FreeSlotWithoutOnDestroy(system.m_Handle, state);
                    }
                }
                catch (Exception e)
                {
                    loggedException = true;
                    Debug.LogException(e);
                }
            }
            m_Systems.Clear();
            Unmanaged.GetImpl().sysHandlesInCreationOrder.Clear();
            m_SystemLookup.Clear();
        }

        /// <summary>
        /// For the systems from the list of types which are not already created yet in the current world,
        /// create systems in the order they are passed in, NOT checking createafter/createbefore validity.
        /// </summary>
        /// <remarks>
        /// If errors are encountered either when creating the system or when calling OnCreate, a default
        /// SystemHandle will be returned for that system.
        /// </remarks>
        /// <param name="types">The system types to create</param>
        /// <param name="typesCount">The number of elements in the <paramref name="types"/> enumeration</param>
        /// <param name="allocator">The allocator to use to allocate the output system list</param>
        /// <returns>A list of system instances</returns>
        internal NativeList<SystemHandle> GetOrCreateSystemsAndLogException(
            NativeList<SystemTypeIndex> types,
            int typesCount,
            AllocatorManager.AllocatorHandle allocator)
        {
            CheckGetOrCreateSystem();

            //don't enumerate twice

            var sysHandlesToReturn = new NativeList<SystemHandle>(typesCount, allocator);

            var startIndex = sysHandlesToReturn.Length;
            var actuallyAddedTypesList = new NativeList<SystemTypeIndex>(16, Allocator.Temp);

            for (int i=0; i<types.Length; i++)
            {
                var type = types[i];
                var handle = SystemHandle.Null;

                try
                {
                    if (!type.IsManaged)
                    {
                        handle = m_Unmanaged.GetExistingUnmanagedSystem(type);
                        if (handle != default)
                        {
                            continue;
                        }

                        handle = Unmanaged.CreateUnmanagedSystem(type, false);
                        actuallyAddedTypesList.Add(type);
                    }
                    else
                    {
                        // If AddSystem_Add_Internal is called for the derived type first,
                        // the base type lookup can return that derived instance.
                        // Hence, we check that the actual type matches the expected type.
                        var system = GetExistingSystemInternal(type);
                        if (system != null && system.GetType() == TypeManager.GetSystemType(type))
                        {
                            handle = system.SystemHandle;
                            continue;
                        }

                        system = AllocateSystemInternal(type);
                        if (system == null)
                        {
                            continue;
                        }

                        AddSystem_Add_Internal(system);
                        handle = system.SystemHandle;
                        actuallyAddedTypesList.Add(type);

                        UnityEngine.Assertions.AssertCore.AreEqual(
                            (ComponentSystemBase)system,
                            (ComponentSystemBase)
                            m_Unmanaged.ResolveSystemState(system.SystemHandle)->m_ManagedSystem
                                .Target);

                    }
                }
                catch (Exception exc)
                {
                    Debug.LogException(exc);
                }
                finally
                {
                    sysHandlesToReturn.Add(handle);
                    if (actuallyAddedTypesList.Length < sysHandlesToReturn.Length)
                        actuallyAddedTypesList.Add(SystemTypeIndex.Null);
                }
            }
            for (int i = startIndex; i != startIndex + actuallyAddedTypesList.Length; i++)
            {
                try
                {
                    var type = actuallyAddedTypesList[i-startIndex];
                    if (type == SystemTypeIndex.Null) continue;

                    if (!type.IsManaged)
                    {
                        var handle = m_Unmanaged.GetExistingUnmanagedSystem(type);
                        var systemState = m_Unmanaged.ResolveSystemState(handle);
                        if (systemState != null)
                            m_Unmanaged.GetImplPtr()->CallSystemOnCreateWithCleanup(systemState);
                    }
                    else
                    {
                        CheckGetOrCreateSystem();
                        m_SystemLookup.TryGetValue(TypeManager.GetSystemType(type), out var system);

                        AddSystem_OnCreate_Internal(system);
                    }
                }
                catch (Exception exc)
                {
                    sysHandlesToReturn[i - startIndex] = default;
                    Debug.LogException(exc);
                }
            }

            return sysHandlesToReturn;
        }

        /// <summary>
        /// Creates systems from the list of types which aren't already created in the current world.
        /// </summary>
        /// <remarks>
        /// This function creates systems in the order they are passed in, and ignores <see cref="CreateBeforeAttribute"/>
        /// and <see cref="CreateAfterAttribute"/> validity.
        /// If errors are encountered either when creating the system or when calling OnCreate, a default
        /// <see cref="SystemHandle"/> will be returned for that system.
        ///
        /// **Important:** This function creates a sync point if any systems are created, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the systems, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <param name="types">The system types to create, in the order in which they should be created.</param>
        /// <param name="allocator">The allocator to use to allocate the output system list</param>
        /// <returns>A list of system instances</returns>
        public NativeList<SystemHandle> GetOrCreateSystemsAndLogException(NativeList<SystemTypeIndex> types, AllocatorManager.AllocatorHandle allocator)
        {
            return GetOrCreateSystemsAndLogException(types, types.Length, allocator);
        }

        /// <summary>
        /// Set this property to true to abort a world update after the next system update.
        /// </summary>
        public bool QuitUpdate { get; set; }

        /// <inheritdoc cref="WorldUnmanaged.UpdateAllocator"/>
        public ref RewindableAllocator UpdateAllocator => ref Unmanaged.UpdateAllocator;

        /// <summary>
        /// Retrieve current double rewindable allocator for this World.
        /// </summary>
        public DoubleRewindableAllocators* CurrentGroupAllocators => m_Unmanaged.GetImpl().DoubleUpdateAllocators;

        /// <summary>
        /// Push group allocator into a stack.
        /// </summary>
        /// <remarks>System groups use the group allocator to rewind memory at a different rate from the world update.
        /// To do this, use the rate manager's ShouldGroupUpdate(), when pushing time into world. You can also set the group
        /// allocator to replace the world's update allocator. When popping time out of the world, you can rewind the allocator,
        /// and if not world owned update the allocator, and then restore the old allocator back.</remarks>
        /// <param name="newGroupAllocators">The group allocator to push into a stack.</param>
        public void SetGroupAllocator(DoubleRewindableAllocators* newGroupAllocators)
        {
            m_Unmanaged.GetImpl().SetGroupAllocator(newGroupAllocators);
        }

        /// <summary>
        /// Pop group allocator out of the stack.
        /// </summary>
        /// <remarks>System group can make use of group allocator to rewind memory at a different rate from the world update.
        /// User can achieve this in rate manager's ShouldGroupUpdate(), when pushing time into world, user also set group
        /// allocator to replace world update allocator. When popping time out of the world, user rewinds the allocator
        /// if not world owned update allocator and then restore the old allocator back.</remarks>
        /// <param name="oldGroupAllocators">The group allocator to pop from the stack.</param>
        public void RestoreGroupAllocator(DoubleRewindableAllocators* oldGroupAllocators)
        {
            m_Unmanaged.GetImpl().RestoreGroupAllocator(oldGroupAllocators);
        }

        /// <summary>
        /// Update the World's default system groups.
        /// </summary>
        /// <remarks>The system group update order is:
        /// 1. <see cref="InitializationSystemGroup"/>
        /// 2. <see cref="SimulationSystemGroup"/>
        /// 3. <see cref="PresentationSystemGroup"/>
        ///
        /// Generally this is not necessary within the context of a UnityEngine application; instead, use
        /// <see cref="ScriptBehaviourUpdateOrder.AppendWorldToPlayerLoop"/> to insert these system groups into the
        /// UnityEngine player loop, where they'll be interleaved with existing UnityEngine updates.</remarks>
        public void Update()
        {
            m_Unmanaged.GetImpl().m_WorldAllocatorHelper.Allocator.Update(); // frees were deferred, do them now
            GetExistingSystemManaged<InitializationSystemGroup>()?.Update();
            GetExistingSystemManaged<SimulationSystemGroup>()?.Update();
            GetExistingSystemManaged<PresentationSystemGroup>()?.Update();

            Assert.IsTrue(EntityManager.GetBuffer<WorldTimeQueue>(TimeSingleton).Length == 0, "PushTime without matching PopTime");
        }

        /// <summary>
        /// Read only collection that doesn't generate garbage when used in a foreach.
        /// </summary>
        /// <typeparam name="T">The list element type</typeparam>
        public struct NoAllocReadOnlyCollection<T> : IEnumerable<T>
        {
            readonly List<T> m_Source;

            /// <summary>
            /// Construct a new instance
            /// </summary>
            /// <param name="source">The source list</param>
            public NoAllocReadOnlyCollection(List<T> source) => m_Source = source;

            /// <summary>
            /// The number of list elements
            /// </summary>
            public int Count => m_Source.Count;

            /// <summary>
            /// Look up a list element by index
            /// </summary>
            /// <param name="index">The list index to look up</param>
            public T this[int index] => m_Source[index];

            /// <summary>
            /// Get an enumerator interface to the list
            /// </summary>
            /// <returns>A list enumerator</returns>
            public List<T>.Enumerator GetEnumerator() => m_Source.GetEnumerator();

            /// <summary>
            /// Check if the list contains a specific element
            /// </summary>
            /// <param name="item">The itme to search for</param>
            /// <returns>True if the element is found in the list, or false if not.</returns>
            public bool Contains(T item) => m_Source.Contains(item);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => throw new NotSupportedException($"To avoid boxing, do not cast {nameof(NoAllocReadOnlyCollection<T>)} to IEnumerable<T>.");
            IEnumerator IEnumerable.GetEnumerator()
                => throw new NotSupportedException($"To avoid boxing, do not cast {nameof(NoAllocReadOnlyCollection<T>)} to IEnumerable.");
        }

        internal static unsafe SystemState* FindSystemStateForChangeVersion(EntityComponentStore* componentStore, uint changeVersion)
        {
            foreach (var world in World.All)
            {
                if (world.EntityManager.GetUncheckedEntityDataAccess()->EntityComponentStore == componentStore)
                {
                    if (changeVersion == componentStore->GlobalSystemVersion)
                    {
                        var systemState = world.m_Unmanaged.ResolveSystemState(world.m_Unmanaged.ExecutingSystem);
                        if (systemState != null)
                            return systemState;
                    }

                    foreach (var system in world.Systems)
                    {
                        if (system == null) continue;

                        var statePtr = system.CheckedState();
                        if (statePtr->m_LastSystemVersion == changeVersion)
                            return statePtr;

                    }
                }
            }

            return null;
        }

        /// <returns>Null if not found.</returns>
        internal static SystemState* FindSystemStateForId(int systemId)
        {
            foreach (var world in All)
            {
                var state = world.Unmanaged.TryGetSystemStateForId(systemId);
                if (state != null)
                    return state;
            }
            return null;
        }
    }

    /// <summary>
    /// Variants of World methods that support unmanaged systems (<see cref="ISystem"/>s)
    /// </summary>
    public static class WorldExtensions
    {
        /// <summary>
        /// Create and return a handle for an instance of a system of type <typeparamref name="T"/> in this World.
        /// </summary>
        /// <remarks>
        /// This can result in multiple instances of the same system in a single World, which is generally undesirable.
        ///
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <param name="self">The World</param>
        /// <typeparam name="T">The system type</typeparam>
        /// <exception cref="ArgumentException">Thrown if the system type <typeparamref name="T"/>has not been registered with the TypeManager.</exception>
        /// <returns>The new system instance's handle of system type <typeparamref name="T"/> in this World.</returns>
        public static SystemHandle CreateSystem<T>(this World self) where T : unmanaged, ISystem
        {
            return self.Unmanaged.CreateUnmanagedSystem<T>(self, true);
        }

        /// <summary>
        /// Return an existing handle for an instance of a system of type <typeparamref name="T"/> in this World.
        /// </summary>
        /// <typeparam name="T">The system type</typeparam>
        /// <param name="self">The World</param>
        /// <returns>The existing system instance's handle of system type <typeparamref name="T"/> in this World. If no such instance exists, the method returns SystemHandle.Null.</returns>
        public static SystemHandle GetExistingSystem<T>(this World self) where T : unmanaged, ISystem
        {
            return self.Unmanaged.GetExistingUnmanagedSystem<T>();
        }

        /// <summary>
        /// Retrieve the handle for an instance of a system of type <typeparamref name="T"/> from the current World. If the system
        /// does not exist in this World, it will first be created.
        /// </summary>
        /// <remarks>
        /// **Important:** This function creates a sync point if a system is created, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the system, and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <typeparam name="T">The system type</typeparam>
        /// <param name="self">The World</param>
        /// <returns>The instance's handle of system type <typeparamref name="T"/> in this World. If the system
        /// does not exist in this World, it will first be created.</returns>
        /// <exception cref="ArgumentException">Thrown if the system type <typeparamref name="T"/>has not been registered with the TypeManager.</exception>
        public static SystemHandle GetOrCreateSystem<T>(this World self) where T : unmanaged, ISystem
        {
            return self.Unmanaged.GetOrCreateUnmanagedSystem<T>();
        }

        /// <summary> Obsolete. Use <see cref="World.GetOrCreateSystem(Type)"/> instead.</summary>
        /// <param name="self">The World</param>
        /// <param name="unmanagedType">The type.</param>
        /// <returns></returns>
        [Obsolete("Use World.GetOrCreateSystem instead")]
        public static SystemHandle GetOrCreateSystem(World self, Type unmanagedType)
        {
            return self.GetOrCreateSystem(unmanagedType);
        }
        /// <summary> Obsolete. Use <see cref="World.DestroySystem(SystemHandle)"/> instead.</summary>
        /// <param name="self">The World</param>
        /// <param name="sysHandle">The system handle.</param>
        [Obsolete("Use World.DestroySystem instead")]
        public static void DestroySystem(World self, SystemHandle sysHandle)
        {
            self.DestroySystem(sysHandle);
        }
        /// <summary> Obsolete. Use <see cref="World.CreateSystem{T}"/> instead.</summary>
        /// <param name="self">The World</param>
        /// <typeparam name="T">The system</typeparam>
        /// <returns></returns>
        [Obsolete("Use World.CreateSystem instead")]
        public static SystemHandle AddSystem<T>(this World self) where T : unmanaged, ISystem
        {
            return CreateSystem<T>(self);
        }
        /// <summary> Obsolete. Use <see cref="World.GetOrCreateSystem"/> instead.</summary>
        /// <param name="self">The World</param>
        /// <param name="unmanagedType">The type.</param>
        /// <returns></returns>
        [Obsolete("Use World.GetOrCreateSystem instead")]
        public static SystemHandle GetOrCreateUnmanagedSystem(this World self, Type unmanagedType)
        {
            return GetOrCreateSystem(self, unmanagedType);
        }
        /// <summary> Obsolete. Use <see cref="World.DestroySystem"/> instead.</summary>
        /// <param name="self">The World</param>
        /// <param name="sysHandle">The system handle.</param>
        [Obsolete("Use World.DestroySystem instead")]
        public static void DestroyUnmanagedSystem(this World self, SystemHandle sysHandle)
        {
            DestroySystem(self, sysHandle);
        }
    }
}
