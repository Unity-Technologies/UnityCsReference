// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    /// <summary>
    /// The <see cref="EntityCommandBufferSystem"/> at the beginning of the <see cref="InitializationSystemGroup"/>.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class BeginInitializationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        /// <summary>
        /// Call <see cref="SystemAPI.GetSingleton{T}"/> to get this component for this system, and then call
        /// <see cref="CreateCommandBuffer"/> on this singleton to create an ECB to be played back by this system.
        /// </summary>
        /// <remarks>
        /// Useful if you want to record entity commands now, but play them back at a later point in
        /// the frame, or early in the next frame.
        /// </remarks>
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            /// <summary>
            /// Create a command buffer for the parent system to play back.
            /// </summary>
            /// <remarks>The command buffers created by this method are automatically added to the system's list of
            /// pending buffers.</remarks>
            /// <param name="world">The world in which to play it back.</param>
            /// <returns>The command buffer to record to.</returns>
            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            /// <summary>
            /// Sets the list of command buffers to play back when this system updates.
            /// </summary>
            /// <remarks>This method is only intended for internal use, but must be in the public API due to language
            /// restrictions. Command buffers created with <see cref="CreateCommandBuffer"/> are automatically added to
            /// the system's list of pending buffers to play back.</remarks>
            /// <param name="buffers">The list of buffers to play back. This list replaces any existing pending command buffers on this system.</param>
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }
        /// <inheritdoc cref="EntityCommandBufferSystem.OnCreate"/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }

    /// <summary>
    /// The <see cref="EntityCommandBufferSystem"/> at the end of the <see cref="InitializationSystemGroup"/>.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class EndInitializationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        /// <summary>
        /// Call <see cref="SystemAPI.GetSingleton{T}"/> to get this component for this system, and then call
        /// <see cref="CreateCommandBuffer"/> on this singleton to create an ECB to be played back by this system.
        /// </summary>
        /// <remarks>
        /// Useful if you want to record entity commands now, but play them back at a later point in
        /// the frame, or early in the next frame.
        /// </remarks>
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            /// <summary>
            /// Create a command buffer for the parent system to play back.
            /// </summary>
            /// <remarks>The command buffers created by this method are automatically added to the system's list of
            /// pending buffers.</remarks>
            /// <param name="world">The world in which to play it back.</param>
            /// <returns>The command buffer to record to.</returns>
            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            /// <summary>
            /// Sets the list of command buffers to play back when this system updates.
            /// </summary>
            /// <remarks>This method is only intended for internal use, but must be in the public API due to language
            /// restrictions. Command buffers created with <see cref="CreateCommandBuffer"/> are automatically added to
            /// the system's list of pending buffers to play back.</remarks>
            /// <param name="buffers">The list of buffers to play back. This list replaces any existing pending command buffers on this system.</param>
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }
        /// <inheritdoc cref="EntityCommandBufferSystem.OnCreate"/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }

    /// <summary>
    /// The earliest of the default system groups to run in each frame. Contains systems related to setting up each new frame.
    /// </summary>
    public partial class InitializationSystemGroup : ComponentSystemGroup
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        [RequiredMember]
        public InitializationSystemGroup()
        {
        }

        /// <inheritdoc cref="ComponentSystemGroup.OnUpdate"/>
        protected override void OnUpdate()
        {
            JobsUtility.ClearSystemIds();
            base.OnUpdate();
        }
    }

    /// <summary>
    /// The <see cref="EntityCommandBufferSystem"/> at the beginning of the <see cref="FixedStepSimulationSystemGroup"/>.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    public partial class BeginFixedStepSimulationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        /// <summary>
        /// Call <see cref="SystemAPI.GetSingleton{T}"/> to get this component for this system, and then call
        /// <see cref="CreateCommandBuffer"/> on this singleton to create an ECB to be played back by this system.
        /// </summary>
        /// <remarks>
        /// Useful if you want to record entity commands now, but play them back at a later point in
        /// the frame, or early in the next frame.
        /// </remarks>
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            /// <summary>
            /// Create a command buffer for the parent system to play back.
            /// </summary>
            /// <remarks>The command buffers created by this method are automatically added to the system's list of
            /// pending buffers.</remarks>
            /// <param name="world">The world in which to play it back.</param>
            /// <returns>The command buffer to record to.</returns>
            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            /// <summary>
            /// Sets the list of command buffers to play back when this system updates.
            /// </summary>
            /// <remarks>This method is only intended for internal use, but must be in the public API due to language
            /// restrictions. Command buffers created with <see cref="CreateCommandBuffer"/> are automatically added to
            /// the system's list of pending buffers to play back.</remarks>
            /// <param name="buffers">The list of buffers to play back. This list replaces any existing pending command buffers on this system.</param>
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }
        /// <inheritdoc cref="EntityCommandBufferSystem.OnCreate"/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }

    /// <summary>
    /// The <see cref="EntityCommandBufferSystem"/> at the end of the <see cref="FixedStepSimulationSystemGroup"/>.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
    public partial class EndFixedStepSimulationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        /// <summary>
        /// Call <see cref="SystemAPI.GetSingleton{T}"/> to get this component for this system, and then call
        /// <see cref="CreateCommandBuffer"/> on this singleton to create an ECB to be played back by this system.
        /// </summary>
        /// <remarks>
        /// Useful if you want to record entity commands now, but play them back at a later point in
        /// the frame, or early in the next frame.
        /// </remarks>
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            /// <summary>
            /// Create a command buffer for the parent system to play back.
            /// </summary>
            /// <remarks>The command buffers created by this method are automatically added to the system's list of
            /// pending buffers.</remarks>
            /// <param name="world">The world in which to play it back.</param>
            /// <returns>The command buffer to record to.</returns>
            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            /// <summary>
            /// Sets the list of command buffers to play back when this system updates.
            /// </summary>
            /// <remarks>This method is only intended for internal use, but must be in the public API due to language
            /// restrictions. Command buffers created with <see cref="CreateCommandBuffer"/> are automatically added to
            /// the system's list of pending buffers to play back.</remarks>
            /// <param name="buffers">The list of buffers to play back. This list replaces any existing pending command buffers on this system.</param>
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }
        /// <inheritdoc cref="EntityCommandBufferSystem.OnCreate"/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }

    /// <summary>
    /// This system group is configured by default to use a fixed timestep for the duration of its
    /// updates.
    /// </summary>
    /// <remarks>
    /// The value of `Time.ElapsedTime` and `Time.DeltaTime` will be temporarily overriden
    /// while this group is updating. The systems in this group will update as many times as necessary
    /// at the fixed timestep in order to "catch up" to the actual elapsed time since the previous frame.
    /// The default fixed timestep is 1/60 seconds. This value can be overriden at runtime by modifying
    /// the system group's `Timestep` property.
    /// </remarks>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public partial class FixedStepSimulationSystemGroup : ComponentSystemGroup
    {
        /// <summary>
        /// Set the timestep use by this group, in seconds. The default value is 1/60 seconds.
        /// This value will be clamped to the range [0.0001f ... 10.0f].
        /// </summary>
        public float Timestep
        {
            get => RateManager != null ? RateManager.Timestep : 0;
            set
            {
                if (RateManager != null)
                    RateManager.Timestep = value;
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        [RequiredMember]
        public FixedStepSimulationSystemGroup()
        {
            float defaultFixedTimestep = 1.0f / 60.0f;
            SetRateManagerCreateAllocator(new RateUtils.FixedRateCatchUpManager(defaultFixedTimestep));
        }
    }

    /// <summary>
    /// The <see cref="EntityCommandBufferSystem"/> at the beginning of the <see cref="VariableRateSimulationSystemGroup"/>.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup), OrderFirst = true)]
    public partial class BeginVariableRateSimulationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        /// <summary>
        /// Call <see cref="SystemAPI.GetSingleton{T}"/> to get this component for this system, and then call
        /// <see cref="CreateCommandBuffer"/> on this singleton to create an ECB to be played back by this system.
        /// </summary>
        /// <remarks>
        /// Useful if you want to record entity commands now, but play them back at a later point in
        /// the frame, or early in the next frame.
        /// </remarks>
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            /// <summary>
            /// Create a command buffer for the parent system to play back.
            /// </summary>
            /// <remarks>The command buffers created by this method are automatically added to the system's list of
            /// pending buffers.</remarks>
            /// <param name="world">The world in which to play it back.</param>
            /// <returns>The command buffer to record to.</returns>
            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            /// <summary>
            /// Sets the list of command buffers to play back when this system updates.
            /// </summary>
            /// <remarks>This method is only intended for internal use, but must be in the public API due to language
            /// restrictions. Command buffers created with <see cref="CreateCommandBuffer"/> are automatically added to
            /// the system's list of pending buffers to play back.</remarks>
            /// <param name="buffers">The list of buffers to play back. This list replaces any existing pending command buffers on this system.</param>
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }
        /// <inheritdoc cref="EntityCommandBufferSystem.OnCreate"/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }

    /// <summary>
    /// The <see cref="EntityCommandBufferSystem"/> at the end of the <see cref="VariableRateSimulationSystemGroup"/>.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup), OrderLast = true)]
    public partial class EndVariableRateSimulationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        /// <summary>
        /// Call <see cref="SystemAPI.GetSingleton{T}"/> to get this component for this system, and then call
        /// <see cref="CreateCommandBuffer"/> on this singleton to create an ECB to be played back by this system.
        /// </summary>
        /// <remarks>
        /// Useful if you want to record entity commands now, but play them back at a later point in
        /// the frame, or early in the next frame.
        /// </remarks>
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            /// <summary>
            /// Create a command buffer for the parent system to play back.
            /// </summary>
            /// <remarks>The command buffers created by this method are automatically added to the system's list of
            /// pending buffers.</remarks>
            /// <param name="world">The world in which to play it back.</param>
            /// <returns>The command buffer to record to.</returns>
            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            /// <summary>
            /// Sets the list of command buffers to play back when this system updates.
            /// </summary>
            /// <remarks>This method is only intended for internal use, but must be in the public API due to language
            /// restrictions. Command buffers created with <see cref="CreateCommandBuffer"/> are automatically added to
            /// the system's list of pending buffers to play back.</remarks>
            /// <param name="buffers">The list of buffers to play back. This list replaces any existing pending command buffers on this system.</param>
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }
        /// <inheritdoc cref="EntityCommandBufferSystem.OnCreate"/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }

    /// <summary>
    /// This system group is configured by default to use a variable update rate of ~15fps (66ms).
    /// </summary>
    /// <remarks>
    /// The value of `Time.ElapsedTime` and `Time.DeltaTime` will be temporarily overriden
    /// while this group is updating to the value total elapsed time since the previous update.
    /// You can configure the update rate manually by replacing the <see cref="IRateManager"/>.
    /// </remarks>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public partial class VariableRateSimulationSystemGroup : ComponentSystemGroup
    {
        /// <summary>
        /// The timestep use by this group, in seconds. This value will reflect the total elapsed time since the last update.
        /// </summary>
        public float Timestep
        {
            get => RateManager != null ? RateManager.Timestep : 0;
        }

        /// <summary>
        /// Construct a new VariableRateSimulationSystemGroup object
        /// </summary>
        [RequiredMember]
        public VariableRateSimulationSystemGroup()
        {
            SetRateManagerCreateAllocator(new RateUtils.VariableRateManager());
        }
    }

    /// <summary>
    /// The <see cref="EntityCommandBufferSystem"/> at the beginning of the <see cref="SimulationSystemGroup"/>.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class BeginSimulationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        /// <summary>
        /// Call <see cref="SystemAPI.GetSingleton{T}"/> to get this component for this system, and then call
        /// <see cref="CreateCommandBuffer"/> on this singleton to create an ECB to be played back by this system.
        /// </summary>
        /// <remarks>
        /// Useful if you want to record entity commands now, but play them back at a later point in
        /// the frame, or early in the next frame.
        /// </remarks>
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            /// <summary>
            /// Create a command buffer for the parent system to play back.
            /// </summary>
            /// <remarks>The command buffers created by this method are automatically added to the system's list of
            /// pending buffers.</remarks>
            /// <param name="world">The world in which to play it back.</param>
            /// <returns>The command buffer to record to.</returns>
            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            /// <summary>
            /// Sets the list of command buffers to play back when this system updates.
            /// </summary>
            /// <remarks>This method is only intended for internal use, but must be in the public API due to language
            /// restrictions. Command buffers created with <see cref="CreateCommandBuffer"/> are automatically added to
            /// the system's list of pending buffers to play back.</remarks>
            /// <param name="buffers">The list of buffers to play back. This list replaces any existing pending command buffers on this system.</param>
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }
        /// <inheritdoc cref="EntityCommandBufferSystem.OnCreate"/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }

    /// <summary>
    /// The <see cref="EntityCommandBufferSystem"/> at the end of the <see cref="SimulationSystemGroup"/>.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class EndSimulationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        /// <summary>
        /// Call <see cref="SystemAPI.GetSingleton{T}"/> to get this component for this system, and then call
        /// <see cref="CreateCommandBuffer"/> on this singleton to create an ECB to be played back by this system.
        /// </summary>
        /// <remarks>
        /// Useful if you want to record entity commands now, but play them back at a later point in
        /// the frame, or early in the next frame.
        /// </remarks>
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            /// <summary>
            /// Create a command buffer for the parent system to play back.
            /// </summary>
            /// <remarks>The command buffers created by this method are automatically added to the system's list of
            /// pending buffers.</remarks>
            /// <param name="world">The world in which to play it back.</param>
            /// <returns>The command buffer to record to.</returns>
            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            /// <summary>
            /// Sets the list of command buffers to play back when this system updates.
            /// </summary>
            /// <remarks>This method is only intended for internal use, but must be in the public API due to language
            /// restrictions. Command buffers created with <see cref="CreateCommandBuffer"/> are automatically added to
            /// the system's list of pending buffers to play back.</remarks>
            /// <param name="buffers">The list of buffers to play back. This list replaces any existing pending command buffers on this system.</param>
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }
        /// <inheritdoc cref="EntityCommandBufferSystem.OnCreate"/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }

    /// <summary>
    /// Default group that runs at the end of the <see cref="SimulationSystemGroup"/>. Contains systems that perform simulation logic,
    /// but which should run after the main simulation logic is complete.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial class LateSimulationSystemGroup : ComponentSystemGroup {}

    /// <summary>
    /// Default system group that contains systems that update the simulated world for a new frame.
    /// </summary>
    public partial class SimulationSystemGroup : ComponentSystemGroup
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        [RequiredMember]
        public SimulationSystemGroup()
        {
        }
    }

    /// <summary>
    /// The <see cref="EntityCommandBufferSystem"/> at the beginning of the <see cref="PresentationSystemGroup"/>.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    public partial class BeginPresentationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        /// <summary>
        /// Call <see cref="SystemAPI.GetSingleton{T}"/> to get this component for this system, and then call
        /// <see cref="CreateCommandBuffer"/> on this singleton to create an ECB to be played back by this system.
        /// </summary>
        /// <remarks>
        /// Useful if you want to record entity commands now, but play them back at a later point in
        /// the frame, or early in the next frame.
        /// </remarks>
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            /// <summary>
            /// Create a command buffer for the parent system to play back.
            /// </summary>
            /// <remarks>The command buffers created by this method are automatically added to the system's list of
            /// pending buffers.</remarks>
            /// <param name="world">The world in which to play it back.</param>
            /// <returns>The command buffer to record to.</returns>
            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            /// <summary>
            /// Sets the list of command buffers to play back when this system updates.
            /// </summary>
            /// <remarks>This method is only intended for internal use, but must be in the public API due to language
            /// restrictions. Command buffers created with <see cref="CreateCommandBuffer"/> are automatically added to
            /// the system's list of pending buffers to play back.</remarks>
            /// <param name="buffers">The list of buffers to play back. This list replaces any existing pending command buffers on this system.</param>
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }
        /// <inheritdoc cref="EntityCommandBufferSystem.OnCreate"/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }

    /// <summary>
    /// The system group containing systems related to rendering the simulated world.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Presentation, WorldSystemFilterFlags.Presentation)]
    public partial class PresentationSystemGroup : ComponentSystemGroup
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        [RequiredMember]
        public PresentationSystemGroup()
        {
        }
    }
}
