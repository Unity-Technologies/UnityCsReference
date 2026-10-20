// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Diagnostics;
using Unity.Core;
using Unity.Mathematics;
using Unity.Collections;

namespace Unity.Entities
{
    /// <summary> Obsolete. Use <see cref="IRateManager"/> instead.</summary>
    [Obsolete("This interface has been renamed to IRateManager (RemovedAFter Entities 1.0)", true)]
    public interface IFixedRateManager
    {
        /// <summary> Obsolete. Use <see cref="IRateManager.ShouldGroupUpdate"/> instead.</summary>
        /// <param name="group">The system group to check</param>
        /// <returns>
        /// True while the <paramref name="group"/> should update its member systems, and then false once the group should no longer update for this frame.
        /// Note: This is called infinitely until it returns false.
        /// </returns>
        bool ShouldGroupUpdate(ComponentSystemGroup group);
        /// <summary> Obsolete. Use <see cref="IRateManager.Timestep"/> instead.</summary>
        float Timestep { get; set; }
    }
    /// <summary>
    /// Interface to define custom behaviors for controlling when a <see cref="ComponentSystemGroup"/> should update,
    /// and what timestep should be visible to the systems in that group. This allows the implementation of Unity's
    /// traditional MonoBehaviour "FixedUpdate()" semantics within DOTS, as well as more advanced/flexible update schemes.
    /// </summary>
    public interface IRateManager
    {
        /// <summary>
        /// Determines whether a system group should proceed with its update, and also how many times it should update per frame.
        /// </summary>
        /// <param name="group">The system group to check</param>
        /// <returns>
        /// True while the <paramref name="group"/> should update its member systems, and then false once the group should no longer update for this frame.
        /// Note: This is called infinitely until it returns false.
        /// </returns>
        bool ShouldGroupUpdate(ComponentSystemGroup group);
        /// <summary>
        /// The timestep since the previous group update (in seconds).
        /// </summary>
        /// <remarks>
        /// This value will be pushed to the delta time of <see cref="World.Time"/> for the duration of the group update. New
        /// values will be clamped to the range [0.0001, 10.0].
        /// </remarks>
        float Timestep { get; set; }
    }

    /// <summary>
    /// Contains some default <see cref="IRateManager"/> implementations to address the most common use cases.
    /// </summary>
    public static class RateUtils
    {
        internal const float MinFixedDeltaTime = 0.0001f;
        internal const float MaxFixedDeltaTime = 10.0f;

        /// <summary>
        /// Implements a rate manager that updates the group exactly once per presentation frame, but uses a
        /// constant timestep instead of the actual elapsed time since the previous frame.
        /// </summary>
        /// <remarks>With this rate manager, the simulation will always tick at a constant timestep per rendered
        /// frame, even if the actual per-frame time is variable. This provides more consistent and more deterministic
        /// performance, and avoids issues stemming from the occasional extremely long or short frame. However, animations
        /// may start to appear jerky if the presentation time is consistently different from the fixed timestep. This mode
        /// is best suited for applications that reliably run very close to the specified fixed timestep, and want the
        /// extra consistency of a constant timestep instead of the usual slight variations.</remarks>
        public unsafe class FixedRateSimpleManager : IRateManager
        {
            float m_FixedTimestep;
            /// <inheritdoc cref="IRateManager.Timestep"/>
            public float Timestep
            {
                get => m_FixedTimestep;
                set => m_FixedTimestep = math.clamp(value, MinFixedDeltaTime, MaxFixedDeltaTime);
            }

            double m_LastFixedUpdateTime;
            bool m_DidPushTime;

            /// <summary>
            /// Double rewindable allocators to remember before pushing in rate group allocators.
            /// </summary>
            DoubleRewindableAllocators* m_OldGroupAllocators = null;

            /// <summary>
            /// Construct a new instance
            /// </summary>
            /// <param name="fixedDeltaTime">The constant fixed timestep to use during system group updates (in seconds)</param>
            public FixedRateSimpleManager(float fixedDeltaTime)
            {
                Timestep = fixedDeltaTime;
            }

            /// <inheritdoc cref="IRateManager.ShouldGroupUpdate"/>
            public bool ShouldGroupUpdate(ComponentSystemGroup group)
            {
                // if this is true, means we're being called a second or later time in a loop.
                if (m_DidPushTime)
                {
                    group.World.PopTime();
                    m_DidPushTime = false;

                    // Update the group allocators and restore the old allocator
                    group.World.RestoreGroupAllocator(m_OldGroupAllocators);

                    return false;
                }

                group.World.PushTime(new TimeData(
                    elapsedTime: m_LastFixedUpdateTime,
                    deltaTime: m_FixedTimestep));

                m_LastFixedUpdateTime += m_FixedTimestep;

                m_DidPushTime = true;

                // Back up current world or group allocator.
                m_OldGroupAllocators = group.World.CurrentGroupAllocators;
                // Replace current world or group allocator with this system group allocator.
                group.World.SetGroupAllocator(group.RateGroupAllocators);

                return true;
            }
        }

        /// <summary>
        /// Implements system update semantics similar to [UnityEngine.MonoBehaviour.FixedUpdate](https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html).
        /// </summary>
        /// <remarks>When this mode is enabled on a group, the group updates exactly once for each elapsed interval
        /// of the fixed timestep.<br/>
        /// <br/>
        /// For example, assume a fixed timestep of 0.02 seconds. If the previous frame updated
        /// at an elapsed time of 1.0 seconds, and the elapsed time for the current frame is now 1.05 seconds, then the
        /// system group updates twice in a row: one with an elapsed simulation time of 1.02 seconds, and a second time
        /// with an elapsed time of 1.04 seconds. In both cases, the delta time is reported as 0.02 seconds. If the
        /// elapsed wall time for the next frame is 1.06 seconds, then the system group doesn't update at all for that
        /// frame.<br/>
        /// <br/>
        /// This mode provides the strongest stability and determinism guarantees, and is best suited for systems implementing
        /// physics or netcode logic. However, the systems in the group will update at an unreliable rate each frame, and
        /// may not update at all if the actual elapsed time is small enough. The running time of systems in this group
        /// must therefore be kept to a minimum. If the wall time needed to simulate a single group update exceeds the
        /// fixed timestep interval, the group can end up even further behind than when it started, causing a negative
        /// feedback loop.</remarks>
        public unsafe class FixedRateCatchUpManager : IRateManager
        {
            float m_FixedTimestep;
            /// <inheritdoc cref="IRateManager.Timestep"/>
            public float Timestep
            {
                get => m_FixedTimestep;
                set
                {
                    m_FixedTimestep = math.clamp(value, MinFixedDeltaTime, MaxFixedDeltaTime);
                }
            }

            double m_LastFixedUpdateTime;
            long m_FixedUpdateCount;
            bool m_DidPushTime;
            double m_MaxFinalElapsedTime;

            /// <summary>
            /// Double rewindable allocators to remember before pushing in rate group allocators.
            /// </summary>
            DoubleRewindableAllocators* m_OldGroupAllocators = null;

            /// <summary>
            /// Construct a new instance
            /// </summary>
            /// <param name="fixedDeltaTime">The constant fixed timestep to use during system group updates (in seconds)</param>
            public FixedRateCatchUpManager(float fixedDeltaTime)
            {
                Timestep = fixedDeltaTime;
            }

            /// <inheritdoc cref="IRateManager.ShouldGroupUpdate"/>
            public bool ShouldGroupUpdate(ComponentSystemGroup group)
            {
                float worldMaximumDeltaTime = group.World.MaximumDeltaTime;
                float maximumDeltaTime = math.max(worldMaximumDeltaTime, m_FixedTimestep);

                // if this is true, means we're being called a second or later time in a loop
                if (m_DidPushTime)
                {
                    group.World.PopTime();
                }
                else
                {
                    m_MaxFinalElapsedTime = m_LastFixedUpdateTime + maximumDeltaTime;
                }

                var finalElapsedTime = math.min(m_MaxFinalElapsedTime, group.World.Time.ElapsedTime);
                if (m_FixedUpdateCount == 0)
                {
                    // First update should always occur at t=0
                }
                else if (finalElapsedTime - m_LastFixedUpdateTime >= m_FixedTimestep)
                {
                    // Advance the timestep and update the system group
                    m_LastFixedUpdateTime += m_FixedTimestep;
                }
                else
                {
                    // Restore old allocator only when we stop fixed updates
                    if (m_DidPushTime)
                    {
                        group.World.RestoreGroupAllocator(m_OldGroupAllocators);
                    }

                    // No update is necessary at this time
                    m_DidPushTime = false;
                    return false;
                }

                m_FixedUpdateCount++;

                group.World.PushTime(new TimeData(
                    elapsedTime: m_LastFixedUpdateTime,
                    deltaTime: m_FixedTimestep));

                // Set the world update allocator to be fixed rate system group allocator
                // at the start of fixed updates
                if(!m_DidPushTime)
                {
                    m_DidPushTime = true;
                    m_OldGroupAllocators = group.World.CurrentGroupAllocators;
                    group.World.SetGroupAllocator(group.RateGroupAllocators);
                }

                return true;
            }
        }

        /// <summary>
        /// A <see cref="IRateManager"/> implementation providing a variable update rate in milliseconds.
        /// </summary>
        public unsafe class VariableRateManager : IRateManager
        {
            /// <summary>
            /// The minimum allowed update rate in Milliseconds
            /// </summary>
            private const uint MinUpdateRateMS = 1;

            /// <summary>
            /// Should the world have <see cref="TimeData"/> pushed to it?
            /// </summary>
            private readonly bool m_ShouldPushToWorld;

            /// <summary>
            /// A cached copy of <see cref="Stopwatch.Frequency"/> as a <see cref="float"/>.
            /// </summary>
            /// <remarks>This is used explicitly when trying to calculate the <see cref="m_Timestep"/>.</remarks>
            private readonly float m_TicksPerSecond = Stopwatch.Frequency;

            /// <summary>
            ///     The required number of ticks to trigger an update when compared against <see cref="m_TickCount"/>
            ///     during <see cref="ShouldGroupUpdate"/>.
            /// </summary>
            private readonly long m_UpdateRate;

            /// <summary>
            /// The latest polled ticks from the timer mechanism.
            /// </summary>
            private long m_CurrentTimestamp;

            /// <summary>
            /// The elapsed time which the rate manager has operated.
            /// </summary>
            /// <remarks>
            ///     This does not have any protection against rollover issues, and is only updated if
            ///     <see cref="m_ShouldPushToWorld"/> is toggled.
            /// </remarks>
            private double m_ElapsedTime;

            /// <summary>
            /// The previous iterations ticks from the timer mechanism.
            /// </summary>
            private long m_PreviousTimestamp;

            /// <summary>
            /// Was <see cref="TimeData"/> pushed to the world?
            /// </summary>
            private bool m_DidPushTime;

            /// <summary>
            /// An accumulator of ticks observed during <see cref="ShouldGroupUpdate"/>.
            /// </summary>
            private long m_TickCount;

            /// <summary>
            /// The calculated delta time between updates.
            /// </summary>
            private float m_Timestep;

            /// <summary>
            /// Double rewindable allocators to remember before pushing in rate group allocators.
            /// </summary>
            DoubleRewindableAllocators* m_OldGroupAllocators = null;

            /// <summary>
            /// Construct a <see cref="VariableRateManager"/> with a given Millisecond update rate.
            /// </summary>
            /// <remarks>
            ///     Utilizes an accumulator where when it exceeds the indicated tick count, triggers the update and
            ///     resets the counter.
            /// </remarks>
            /// <param name="updateRateInMS">
            ///     The update rate for the manager in Milliseconds, if the value is less then
            ///     <see cref="MinUpdateRateMS"/> it will be set to it.
            /// </param>
            /// <param name="pushToWorld">
            ///     Should <see cref="TimeData"/> be pushed onto the world? If systems inside of this group do not
            ///     require the use of the <see cref="World.Time"/>, a minor performance gain can be made setting this
            ///     to false.
            /// </param>
            public VariableRateManager(uint updateRateInMS = 66, bool pushToWorld = true)
            {
                // Ensure update rate is valid
                if (updateRateInMS < MinUpdateRateMS)
                {
                    updateRateInMS = MinUpdateRateMS;
                }

                // Cache our update rate in ticks
                m_UpdateRate = (long)(updateRateInMS * (Stopwatch.Frequency / 1000f));
                m_ShouldPushToWorld = pushToWorld;

                // Initialize our time data
                m_CurrentTimestamp = Stopwatch.GetTimestamp();
                m_PreviousTimestamp = m_CurrentTimestamp;

                // Make sure that the first call updates
                m_TickCount = m_UpdateRate;
            }

            /// <summary>
            /// Determines if the group should be updated this invoke.
            /// </summary>
            /// <remarks>The while loop happens once.</remarks>
            /// <param name="group">The system group to check</param>
            /// <returns>True if <paramref name="group"/> should update its member systems, or false if the group should skip its update.</returns>
            public bool ShouldGroupUpdate(ComponentSystemGroup @group)
            {
                // We're going to use the internal ticks to ensure this works in worlds without time systems.
                m_CurrentTimestamp = Stopwatch.GetTimestamp();

                // Calculate the difference between our current timestamp and the previous, but also account for the
                // possibility that the value may have rolled over.
                long difference;
                if (m_CurrentTimestamp < m_PreviousTimestamp)
                {
                    // Rollover protection
                    difference = (long.MaxValue - m_PreviousTimestamp) + m_CurrentTimestamp;
                }
                else
                {
                    difference = m_CurrentTimestamp - m_PreviousTimestamp;
                }

                // Save/increment
                m_PreviousTimestamp = m_CurrentTimestamp;
                m_TickCount += difference;

                // Remove that time we pushed on the world
                if (m_ShouldPushToWorld && m_DidPushTime)
                {
                    @group.World.PopTime();
                    m_DidPushTime = false;
                    group.World.RestoreGroupAllocator(m_OldGroupAllocators);
                }

                // We haven't elapsed enough ticks, thus false.
                if (m_TickCount < m_UpdateRate)
                {
                    return false;
                }

                // Calculate what we believe is the delta elapsed since our last reset
                m_Timestep = m_TickCount / m_TicksPerSecond;

                // Push the current world time
                if (m_ShouldPushToWorld)
                {
                    m_ElapsedTime += m_Timestep;
                    group.World.PushTime(new TimeData(
                        elapsedTime: m_ElapsedTime,
                        deltaTime: m_Timestep));
                    m_DidPushTime = true;

                    m_OldGroupAllocators = group.World.CurrentGroupAllocators;
                    group.World.SetGroupAllocator(group.RateGroupAllocators);
                }

                // Reset tick count
                m_TickCount = 0;
                return true;
            }

            /// <inheritdoc />
            public float Timestep
            {
                get => m_Timestep;
                set => m_Timestep = value;
            }
        }
    }
}
