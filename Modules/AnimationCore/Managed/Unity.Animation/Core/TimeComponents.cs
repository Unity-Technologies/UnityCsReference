// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.IntegerTime;
using UnityEngine;

namespace Unity.Animations
{
    /// <summary>
    /// Determines if the corresponding timer/clock state is currently playing or paused, stopped
    /// </summary>
    /// <remarks>
    /// The distinction between Stopped and Paused is important regarding clip markup gathering :
    /// - If the timer state transitions from Paused to Playing at time t, we don't want to gather
    /// markups located exactly at time t, since they were gathered previously when the timer
    /// was paused at time t.
    /// - If the timer state transitions from Stopped to Playing at time t, we want to gather
    /// markups located exactly at time t, since they were not gathered previously.
    /// </remarks>
    public enum TimeControlState
    {
        /// <summary>The timer/clock has been manually stopped by the user and isn't advancing.</summary>
        Stopped = 0,

        /// <summary>The timer/clock has been manually paused by the user and isn't advancing.</summary>
        Paused,

        /// <summary>The timer/clock is active and advancing</summary>
        Playing
    }

    /// <summary>
    /// Data used to represent the current state of an animation timer.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct TimerState
    {
        /// <summary>The time on the previous frame.</summary>
        public DiscreteTime PreviousTime { get; internal set; }

        /// <summary>The time this frame.</summary>
        /// <remarks>
        /// To move the timer to another time, call <see cref="TimerStateExtensions.JumpToTime"/> or
        /// <see cref="TimerStateExtensions.Rewind"/>. Those methods also wrap the time into <see cref="Range"/>, apply
        /// <see cref="CycleOffset"/>, and set <see cref="PreviousTime"/> and <see cref="DeltaTime"/> so that root
        /// motion and animation events don't count the skipped time.
        /// </remarks>
        public DiscreteTime Time { get; internal set; }

        /// <summary>The delta time this frame.</summary>
        public DiscreteTime DeltaTime { get; internal set; }

        /// <summary>Determines what is the valid time range.</summary>
        public DiscreteTimeInterval Range;

        /// <summary>The time scale of the timer. </summary>
        public double TimeScale;

        /// <summary>
        /// The playback speed that synchronized timers use to compute their time scale.
        /// </summary>
        internal double Speed;

        /// <summary>The number of times this has been looped in total until the previous frame (included)</summary>
        public int PreviousLoopCount { get; internal set; }

        /// <summary>The number of times this has been looped in total until the current frame (included)</summary>
        public int LoopCount { get; internal set; }

        /// <summary>The play state on the previous frame.</summary>
        public TimeControlState PreviousPlayState { get; internal set; }

        /// <summary>The play state for the current frame.</summary>
        public TimeControlState PlayState { get; internal set; }

        /// <summary>
        /// The play state that the next frame. This means <see cref="PlayState"/> will be set to this value
        /// the next frame.
        /// </summary>
        public TimeControlState NextPlayState { get; internal set; }

        /// <summary>
        /// Whether the timer advances on the next <see cref="TimerStateExtensions.Update"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="TimerStateExtensions.Play"/>, <see cref="TimerStateExtensions.Pause"/>, and
        /// <see cref="TimerStateExtensions.Stop"/> change this value right away. <see cref="PlayState"/> changes only
        /// on the next `Update`.
        /// </remarks>
        public readonly bool IsPlaying => NextPlayState == TimeControlState.Playing;

        /// <summary>
        /// How the timer treats time outside of its range.
        /// </summary>
        /// <remarks>
        /// When the value is <see cref="UnityEngine.WrapMode.Default"/>, time is unbounded and keeps advancing past the
        /// range. A timer from <see cref="Clip.ToTimerState(DiscreteTime)"/> takes its wrap mode from the **Loop Time**
        /// setting of the clip.
        /// </remarks>
        public WrapMode WrapMode;

        /// <summary>
        /// The point of the cycle where the timer starts, as a fraction of <see cref="Range"/> duration.
        /// </summary>
        /// <remarks>
        /// The offset has the same unit as **Cycle Offset** in the clip settings. It applies only to a timer whose
        /// <see cref="WrapMode"/> is <see cref="UnityEngine.WrapMode.Loop"/>. Other wrap modes ignore the offset.
        ///
        /// Only the methods that move the timer apply the offset.
        /// <see cref="TimerStateExtensions.Rewind"/> and <see cref="TimerStateExtensions.JumpToTime"/> add the offset,
        /// multiplied by the duration of <see cref="Range"/>, to the time that they set. Updating the timer doesn't add
        /// the offset again, because `Time` already includes it. When `Rewind` applies a negative offset, the time
        /// wraps backward and <see cref="LoopCount"/> is `-1` or less.
        ///
        /// The default value is 0, which leaves a timer unchanged. <see cref="Clip.ToTimerState(DiscreteTime)"/> takes
        /// the offset from the clip.
        /// </remarks>
        public float CycleOffset;

        // The timer was rewound or jumped since the last update, so that the next update starts from a reset time
        internal byte Reset;

        // The last update started from a reset time, so that the gatherers include the markups at the previous time
        internal byte WasReset;

        /// <summary>
        /// Creates a playing timer at the start of a range.
        /// </summary>
        /// <remarks>
        /// The timer has a <see cref="TimeScale"/> of 1, and is rewound with
        /// <see cref="TimerStateExtensions.Rewind"/>, so the cycle offset applies when the wrap mode is
        /// <see cref="UnityEngine.WrapMode.Loop"/>. The first <see cref="TimerStateExtensions.Update"/> advances it.
        /// To create the timer of a clip, use <see cref="Clip.ToTimerState(DiscreteTime)"/>.
        /// </remarks>
        /// <param name="range">The time range of the timer.</param>
        /// <param name="wrapMode">How the timer treats time outside of <paramref name="range"/>.</param>
        /// <param name="cycleOffset">The point of the cycle where the timer starts, as a fraction of the duration of
        /// <paramref name="range"/>. Only <see cref="UnityEngine.WrapMode.Loop"/> uses it.</param>
        /// <returns>The new timer.</returns>
        public static TimerState Create(DiscreteTimeInterval range, WrapMode wrapMode, float cycleOffset = 0f)
        {
            var timer = new TimerState
            {
                Range = range,
                WrapMode = wrapMode,
                CycleOffset = cycleOffset,
                TimeScale = 1.0,
                Speed = 1.0,
                PlayState = TimeControlState.Playing,
                NextPlayState = TimeControlState.Playing,
            };
            timer.Rewind();
            return timer;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="TimerState"/> to control playback.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Play"/> to start a timer, and call <see cref="Update"/> once per frame to advance it. A timer
    /// advances only while it's playing. The methods modify the timer, so call them on a variable, not on a readonly
    /// field or an `in` parameter.
    ///
    /// To move a timer to another time without playing through the time in between, use one of these methods:
    ///
    ///- <see cref="JumpToTime"/> sets the timer to a time that you choose. The time you skip doesn't count as played.
    ///- <see cref="Rewind"/> sets the timer to the start of its range, without changing whether the timer is playing.
    ///
    /// If the timer advances in the next `Update` after either method,
    /// <see cref="AnimationEventUtility.GatherAnimationEvents(ref Clip, in TimerState, NativeList{GatheredAnimationEvent})"/>
    /// includes the animation events at the new time. Both methods apply the <see cref="TimerState.CycleOffset"/> only
    /// when the wrap mode of the timer is <see cref="UnityEngine.WrapMode.Loop"/>.
    ///
    /// The following example uses the `Unity.Animations`, `Unity.Entities`, `Unity.IntegerTime`, and `UnityEngine`
    /// namespaces. It creates a timer for a clip with <see cref="Clip.ToTimerState(DiscreteTime)"/> and updates it once
    /// per frame.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/TimerStateExample.cs#PlayClipWithTimerState}]]></code>
    /// </example>
    /// <seealso cref="TimerState"/>
    [GenerateTestsForBurstCompatibility]
    public static class TimerStateExtensions
    {
        /// <summary>
        /// Update timerState's with deltaTime and apply a PingPong.
        /// </summary>
        /// <param name="timerState">The timer state to update.</param>
        /// <param name="deltaTime">The deltatime to add.</param>
        /// <remarks>
        /// For even timerState.LoopCount the time is increasing and for odd timerState.LoopCount the time is decreasing.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UpdatePingPong(this ref TimerState timerState, DiscreteTime deltaTime)
        {
            deltaTime = (timerState.LoopCount & 1) == 1 ? -deltaTime : deltaTime;

            var loopCount = timerState.LoopCount;
            timerState.Time += deltaTime * timerState.TimeScale;
            timerState.Time = Core.PingPong(timerState.Time, timerState.Range, ref loopCount);
            timerState.LoopCount = loopCount;
        }

        /// <summary>
        /// Update timerState's time with deltaTime and apply a Repeat.
        /// </summary>
        /// <param name="timerState">The timer state to update.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UpdateLoop(this ref TimerState timerState)
        {
            var loopCount = timerState.LoopCount;
            timerState.Time += timerState.DeltaTime;
            timerState.Time = Core.Repeat(timerState.Time, timerState.Range, ref loopCount);
            timerState.LoopCount = loopCount;
        }

        /// <summary>
        /// Update timerState's time with deltaTime and apply a Clamp.
        /// </summary>
        /// <param name="timerState">The timer state to update.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UpdateClamp(this ref TimerState timerState)
        {
            timerState.Time += timerState.DeltaTime;
            timerState.Time = timerState.Time.Clamp(timerState.Range.Start, timerState.Range.End);
        }

        /// <summary>
        /// Advances the time of a timer state by its delta time and stops the timer when the time passes the end of
        /// the range.
        /// </summary>
        /// <remarks>
        /// When the time passes the end of the range in the playback direction, the time moves to the opposite end of
        /// the range, the loop count increases by one when playing forward and decreases by one when playing backward,
        /// and the timer stops on the next update.
        /// </remarks>
        /// <param name="timerState">The timer state to advance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UpdateOnce(this ref TimerState timerState)
        {
            var time = timerState.Time + timerState.DeltaTime;
            var forward = timerState.DeltaTime >= DiscreteTime.Zero;
            if (forward ? time > timerState.Range.End : time < timerState.Range.Start)
            {
                timerState.Time = forward ? timerState.Range.Start : timerState.Range.End;
                timerState.LoopCount += forward ? 1 : -1;
                timerState.NextPlayState = TimeControlState.Stopped;
            }
            else
                timerState.Time = time;
        }

        /// <summary>
        /// Update timerState's time with deltaTime without any time constraint.
        /// </summary>
        /// <param name="timerState">The timer state to update.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UpdateTime(this ref TimerState timerState)
        {
            timerState.Time += timerState.DeltaTime;
        }

        /// <summary>
        /// Jumps a timer state to a specific time, without advancing the timer from the current time to that time.
        /// </summary>
        /// <remarks>
        /// The jump doesn't count the skipped span as played: <see cref="TimerState.PreviousTime"/> and
        /// <see cref="TimerState.Time"/> both become the target time, and <see cref="TimerState.DeltaTime"/> becomes
        /// zero. When the wrap mode is <see cref="UnityEngine.WrapMode.Once"/> or
        /// <see cref="UnityEngine.WrapMode.ClampForever"/>, the timer clamps the target time to
        /// <see cref="TimerState.Range"/>. When the wrap mode is <see cref="UnityEngine.WrapMode.Loop"/> or
        /// <see cref="UnityEngine.WrapMode.PingPong"/>, the timer repeats or reflects the target time into the range.
        /// When the wrap mode is <see cref="UnityEngine.WrapMode.Default"/>, the timer doesn't change the target time.
        ///
        /// The time is a curve time, in the same unit as `Range`. When the wrap mode is `Loop`, the timer moves to
        /// <paramref name="time"/> shifted by <see cref="TimerState.CycleOffset"/> times the duration of the range.
        /// Other wrap modes ignore the offset. For `Loop` and `PingPong`, the jump sets
        /// <see cref="TimerState.LoopCount"/> to <see cref="TimerState.PreviousLoopCount"/>.
        ///
        /// A jump also resets the timer for event gathering: if the timer advances in the next
        /// <see cref="Update"/>,
        /// <see cref="AnimationEventUtility.GatherAnimationEvents(ref Clip, in TimerState, NativeList{GatheredAnimationEvent})"/>
        /// includes the animation events at the target time, once. The events between the old time and the target time
        /// aren't gathered.
        /// </remarks>
        /// <param name="timerState">The timer state to jump. The method changes this timer state.</param>
        /// <param name="time">The time to jump to, in the same unit as `TimerState.Range`. The wrap mode of the timer
        /// decides what happens to a time outside the range.</param>
        public static void JumpToTime(this ref TimerState timerState, DiscreteTime time)
        {
            var loopCount = timerState.LoopCount;
            switch (timerState.WrapMode)
            {
                case WrapMode.Default:
                {
                    timerState.Time = time;
                } break;
                case WrapMode.Once:
                case WrapMode.ClampForever:
                {
                    timerState.Time = time.Clamp(timerState.Range.Start, timerState.Range.End);
                } break;
                case WrapMode.Loop:
                {
                    timerState.Time = Core.Repeat(time + GetCycleShift(timerState), timerState.Range, ref loopCount);
                    timerState.LoopCount = timerState.PreviousLoopCount;
                } break;
                case WrapMode.PingPong:
                {
                    timerState.Time = Core.PingPong(time, timerState.Range, ref loopCount);
                    timerState.LoopCount = timerState.PreviousLoopCount;
                } break;
            }

            timerState.PreviousTime = timerState.Time;
            timerState.DeltaTime = DiscreteTime.Zero;
            timerState.Reset = 1;
        }

        /// <summary>
        /// Ticks the timer state with deltaTime.
        /// </summary>
        /// <param name="timerState">The timer state to update.</param>
        /// <param name="deltaTime">The deltatime to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(this ref TimerState timerState, DiscreteTime deltaTime)
        {
            timerState.WasReset = timerState.Reset;
            timerState.Reset = 0;
            timerState.PreviousPlayState = timerState.PlayState;
            timerState.PreviousLoopCount = timerState.LoopCount;
            timerState.PreviousTime = timerState.Time;

            timerState.PlayState = timerState.NextPlayState;

            if (timerState.PlayState != TimeControlState.Playing)
            {
                timerState.DeltaTime = DiscreteTime.Zero;
            }
            else
            {
                timerState.DeltaTime = deltaTime * timerState.TimeScale;
                switch (timerState.WrapMode)
                {
                    case WrapMode.Loop:
                        timerState.UpdateLoop();
                        break;
                    case WrapMode.PingPong:
                        timerState.UpdatePingPong(deltaTime);
                        break;
                    case WrapMode.Once:
                        timerState.UpdateOnce();
                        break;
                    case WrapMode.ClampForever:
                        timerState.UpdateClamp();
                        break;
                    default:
                        timerState.UpdateTime();
                        break;
                }
            }
        }

        /// <summary>
        /// Start playing a <see cref="TimerState"/>, meaning it will be advanced during
        /// all following updates of the <see cref="TimerState"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="TimerState.PlayState"/> becomes <see cref="TimeControlState.Playing"/> on the next
        /// <see cref="Update"/>. <see cref="TimerState.IsPlaying"/> is `true` right away.
        /// </remarks>
        /// <param name="timerState">The timer state to play.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Play(this ref TimerState timerState)
        {
            timerState.NextPlayState = TimeControlState.Playing;
        }

        /// <summary>
        /// Pause a <see cref="TimerState"/>, meaning it won't be advanced during
        /// following updates of the <see cref="TimerState"/>. The behavior of a <see cref="TimerState"/>
        /// when paused is the same than stopped, the only difference is how other systems interpret those
        /// two states.
        /// </summary>
        /// <remarks>
        /// <see cref="TimerState.PlayState"/> becomes <see cref="TimeControlState.Paused"/> on the next
        /// <see cref="Update"/>. <see cref="TimerState.IsPlaying"/> is `false` right away.
        /// </remarks>
        /// <param name="timerState">The timer state to pause.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pause(this ref TimerState timerState)
        {
            timerState.NextPlayState = TimeControlState.Paused;
        }

        /// <summary>
        /// Stop a <see cref="TimerState"/>, meaning it won't be advanced during
        /// following updates of the <see cref="TimerState"/>. The behavior of a <see cref="TimerState"/>
        /// when stopped is the same than paused, the only difference is how other systems interpret those
        /// two states.
        /// </summary>
        /// <remarks>
        /// <see cref="TimerState.PlayState"/> becomes <see cref="TimeControlState.Stopped"/> on the next
        /// <see cref="Update"/>. <see cref="TimerState.IsPlaying"/> is `false` right away.
        /// </remarks>
        /// <param name="timerState">The timer state to stop.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Stop(this ref TimerState timerState)
        {
            timerState.NextPlayState = TimeControlState.Stopped;
        }

        /// <summary>
        /// Rewinds a timer state to the start of its range.
        /// </summary>
        /// <remarks>
        /// The method moves <see cref="TimerState.PreviousTime"/> and <see cref="TimerState.Time"/> to the start of
        /// <see cref="TimerState.Range"/> and sets <see cref="TimerState.DeltaTime"/> to zero. It doesn't change
        /// <see cref="TimerState.PlayState"/>, so a playing timer keeps playing.
        ///
        /// When the wrap mode is <see cref="UnityEngine.WrapMode.Loop"/>, the method also applies
        /// <see cref="TimerState.CycleOffset"/>. The time moves by the cycle offset times the duration of the range,
        /// and wraps back into the range. The method sets <see cref="TimerState.PreviousLoopCount"/> and
        /// <see cref="TimerState.LoopCount"/> to the cycle offset rounded down to a whole number. For example, with a
        /// range from `0` to `2`, a cycle offset of `1.25` sets <see cref="TimerState.Time"/> to `0.5` and both loop
        /// counts to `1`, and a cycle offset of `-0.25` sets `Time` to `1.5` and both loop counts to `-1`. With any
        /// other wrap mode, the time stays at the start of the range and the method sets both loop counts to `0`.
        /// </remarks>
        /// <param name="timerState">The timer state to rewind. The method changes this timer state.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Rewind(this ref TimerState timerState)
        {
            var time = timerState.Range.Start;
            var loopCount = 0;
            if (timerState.WrapMode == WrapMode.Loop)
                time = Core.Repeat(time + GetCycleShift(timerState), timerState.Range, ref loopCount);

            timerState.PreviousTime = time;
            timerState.Time = time;
            timerState.DeltaTime = DiscreteTime.Zero;
            timerState.PreviousLoopCount = loopCount;
            timerState.LoopCount = loopCount;
            timerState.Reset = 1;
        }

        // The cycle offset in time units, zero unless the timer loops
        static DiscreteTime GetCycleShift(in TimerState timerState) =>
            timerState.WrapMode == WrapMode.Loop ? timerState.Range.Duration * (double)timerState.CycleOffset : DiscreteTime.Zero;

        /// <summary>
        /// Synchronizes an animation source timer state to a graph timer state, to keep sources of different
        /// durations, such as a walk and a run, in step.
        /// </summary>
        /// <remarks>
        /// The source copies the play state of the graph timer and takes its time from the normalized time of the graph
        /// timer. The source time is the point of its own range that matches the normalized time. The source copies the
        /// loop counts of the graph timer too. When the <see cref="TimerState.Speed"/> of the source is negative, the
        /// source plays the graph time backwards, and its times and loop counts mirror the graph timer values instead.
        ///
        /// The cycle offset applies only when the wrap mode of the source is <see cref="UnityEngine.WrapMode.Loop"/>.
        /// The offset shifts the source time, and the wraps that the shift causes count as loops. The method also sets
        /// `TimeScale` and `DeltaTime` from the speeds and the times.
        /// </remarks>
        /// <param name="sourceTimerState">
        /// The animation source timer state to synchronize.
        /// </param>
        /// <param name="graphTimerState">
        /// The graph timer state to read the time from. It must use the unit range
        /// <see cref="DiscreteTimeInterval.UnitRange"/>, so that its times are normalized.
        /// </param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the graph timer
        /// state doesn't use the unit range.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SynchronizeSource(this ref TimerState sourceTimerState, TimerState graphTimerState)
        {
            ValidateIsUnitTimeInterval(graphTimerState.Range);

            sourceTimerState.PreviousPlayState = graphTimerState.PreviousPlayState;
            sourceTimerState.PlayState = graphTimerState.PlayState;
            sourceTimerState.NextPlayState = graphTimerState.NextPlayState;

            // Cases to consider:
            // a) Graph speed is '+', source speed is '+' => resulting source speed is '+' -----> reuse graph sampling direction
            // b) Graph speed is '+', source speed is '-' => resulting source speed is '-' -----> flip graph sampling direction
            // c) Graph speed is '-', source speed is '+' => resulting source speed is '-' -----> reuse graph sampling direction
            // d) Graph speed is '-', source speed is '-' => resulting source speed is '+' -----> flip graph sampling direction

            // Important: GraphTimerState.Time and GraphTimerState.PreviousTime are stored as normalized time
            var previousNormalizedTime = (double)graphTimerState.PreviousTime;
            var normalizedTime = (double)graphTimerState.Time;
            if (sourceTimerState.Speed < 0)
            {
                previousNormalizedTime = 1.0 - previousNormalizedTime;
                normalizedTime = 1.0 - normalizedTime;
                sourceTimerState.PreviousLoopCount = -(graphTimerState.PreviousLoopCount + 1);
                sourceTimerState.LoopCount = -(graphTimerState.LoopCount + 1);
            }
            else
            {
                sourceTimerState.PreviousLoopCount = graphTimerState.PreviousLoopCount;
                sourceTimerState.LoopCount = graphTimerState.LoopCount;
            }

            var duration = sourceTimerState.Range.Duration;
            var start = sourceTimerState.Range.Start;
            if (sourceTimerState.WrapMode == WrapMode.Loop && sourceTimerState.CycleOffset != 0f)
            {
                // The offset moves the wrap points, so the wraps it causes count as loops
                var offset = (double)sourceTimerState.CycleOffset;
                var previousLoopCount = sourceTimerState.PreviousLoopCount;
                var loopCount = sourceTimerState.LoopCount;
                sourceTimerState.PreviousTime = Core.Repeat(start + duration * (previousNormalizedTime + offset),
                    sourceTimerState.Range, ref previousLoopCount);
                sourceTimerState.Time = Core.Repeat(start + duration * (normalizedTime + offset),
                    sourceTimerState.Range, ref loopCount);
                sourceTimerState.PreviousLoopCount = previousLoopCount;
                sourceTimerState.LoopCount = loopCount;
            }
            else
            {
                sourceTimerState.PreviousTime = start + duration * previousNormalizedTime;
                sourceTimerState.Time = start + duration * normalizedTime;
            }

            // Apply unit time scale because Time/PreviousTime are already scaled based on main timer
            sourceTimerState.TimeScale = math.sign(graphTimerState.Speed * sourceTimerState.Speed);
            sourceTimerState.DeltaTime = (sourceTimerState.Time - sourceTimerState.PreviousTime) * math.abs(sourceTimerState.TimeScale);
        }

        /// <summary>
        /// Synchronizes a graph timer state to another graph timer state, so that several graphs play in step.
        /// </summary>
        /// <remarks>
        /// The driven timer copies the play state, the times, and the loop counts of the driver timer. When the signs
        /// of their <see cref="TimerState.Speed"/> differ, the driven timer takes the mirrored time, which is one minus
        /// the driver time. It also takes mirrored loop counts, so that it keeps its own playback direction. Both
        /// timers use normalized time, so <see cref="TimerState.CycleOffset"/> doesn't apply. The method also sets
        /// `TimeScale` and `DeltaTime` from the speeds and the times.
        /// </remarks>
        /// <param name="drivenGraphTimerState">
        /// The driven graph timer state to synchronize. It must use the unit range
        /// <see cref="DiscreteTimeInterval.UnitRange"/>.
        /// </param>
        /// <param name="driverGraphTimerState">
        /// The driver graph timer state to read the time from. It must use the unit range
        /// <see cref="DiscreteTimeInterval.UnitRange"/>.
        /// </param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and either timer
        /// state doesn't use the unit range.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SynchronizeGraph(this ref TimerState drivenGraphTimerState, TimerState driverGraphTimerState)
        {
            ValidateIsUnitTimeInterval(driverGraphTimerState.Range);
            ValidateIsUnitTimeInterval(drivenGraphTimerState.Range);

            drivenGraphTimerState.PreviousPlayState = driverGraphTimerState.PreviousPlayState;
            drivenGraphTimerState.PlayState = driverGraphTimerState.PlayState;
            drivenGraphTimerState.NextPlayState = driverGraphTimerState.NextPlayState;

            // When synchronizing multiple graphs together there is always one graph timer driving all the others at
            // different moments in time during possibly multiple active transitions.
            // The complexity lies when multiple graphs are synchronized but mix positive/negative playback speeds.
            //
            // Contrary to graph-source synchronization, you cannot flip sampling directionality as soon as the master timer
            // changes during a transition as this has for effect of creating time discontinuities in the overall motion.
            // Hence to preserve continuity of motion, synchronized graphs maintain their respective
            // sampling direction throughout the evaluation and flip the drivers sampling direction when needed.
            //
            // Cases to consider:
            // a) Driver speed is '+', driven speed is '+' => expected driven speed is '+' -----> reuse the driver sampling direction
            // b) Driver speed is '+', driven speed is '-' => expected driven speed is '-' -----> flip the driver sampling direction
            // c) Driver speed is '-', driven speed is '+' => expected driven speed is '+' -----> flip the driver sampling direction
            // d) Driver speed is '-', driven speed is '-' => expected driven speed is '-' -----> reuse the driver sampling direction

            if (MathUtility.sgn(drivenGraphTimerState.Speed) != MathUtility.sgn(driverGraphTimerState.Speed))
            {
                drivenGraphTimerState.PreviousTime = (DiscreteTimeExtensions.One - driverGraphTimerState.PreviousTime);
                drivenGraphTimerState.Time = (DiscreteTimeExtensions.One - driverGraphTimerState.Time);
                drivenGraphTimerState.PreviousLoopCount = -(driverGraphTimerState.PreviousLoopCount + 1);
                drivenGraphTimerState.LoopCount = -(driverGraphTimerState.LoopCount + 1);
            }
            else
            {
                drivenGraphTimerState.PreviousTime = driverGraphTimerState.PreviousTime;
                drivenGraphTimerState.Time = driverGraphTimerState.Time;
                drivenGraphTimerState.PreviousLoopCount = driverGraphTimerState.PreviousLoopCount;
                drivenGraphTimerState.LoopCount = driverGraphTimerState.LoopCount;
            }

            // Apply unit time scale because Time/PreviousTime are already scaled based on main timer
            drivenGraphTimerState.TimeScale = math.select(0.0, math.sign(drivenGraphTimerState.Speed), math.abs(driverGraphTimerState.Speed) > 0);
            drivenGraphTimerState.DeltaTime = (drivenGraphTimerState.Time - drivenGraphTimerState.PreviousTime) * math.abs(drivenGraphTimerState.TimeScale);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateIsUnitTimeInterval(DiscreteTimeInterval interval)
        {
            if (!interval.Equals(DiscreteTimeInterval.UnitRange))
                throw new System.InvalidOperationException("Time interval is not of unit range.");
        }
    }
}
