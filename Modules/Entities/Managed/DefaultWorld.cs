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
    /// This system group is configured by default to use a variable update rate of ~15fps (66ms).
    /// </summary>
    /// <remarks>
    /// The value of `Time.ElapsedTime` and `Time.DeltaTime` will be temporarily overriden
    /// while this group is updating to the value total elapsed time since the previous update.
    /// You can configure the update rate manually by replacing the <see cref="IRateManager"/>.
    /// </remarks>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
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
    /// Default group that runs at the end of the <see cref="SimulationSystemGroup"/>. Contains systems that perform simulation logic,
    /// but which should run after the main simulation logic is complete.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
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
