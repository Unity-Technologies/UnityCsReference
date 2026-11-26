// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IntegerTime;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animations
{
    /// <summary>
    /// An animation event that a clip timer crossed, with the function name, the parameters, and the time of the
    /// event.
    /// </summary>
    /// <remarks>
    /// Pass a `NativeList&lt;GatheredAnimationEvent&gt;` to
    /// <see cref="AnimationEventUtility.GatherAnimationEvents(ref Clip, in TimerState, NativeList{GatheredAnimationEvent})"/>
    /// to get the animation events that a timer crossed. The clip data stores the function name and the parameters of
    /// each event, so you can read them from jobs and Burst-compiled code. The function name and the string parameter
    /// are stored as <see cref="StringHash"/> values. Compare them to `new StringHash("name")` values that you
    /// compute on the main thread. The object reference parameter is an `EntityId`. Use <see cref="EventIndex"/> to
    /// read the complete event from the source animation clip on the main thread.
    ///
    /// The following example uses the `System.Collections.Generic`, `Unity.Animations`, `Unity.Collections`,
    /// `Unity.Entities`, `Unity.IntegerTime`, and `UnityEngine` namespaces. It gathers the animation events that the
    /// playback time of a clip passes between two updates. It then uses the event index of each gathered event to
    /// read the function name of the event from the animation clip.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/GatherEventsExample.cs#GatherEvents}]]></code>
    /// </example>
    /// <seealso cref="AnimationEventUtility"/>
    public struct GatheredAnimationEvent
    {
        /// <summary>
        /// The index of the event in the events of the source animation clip.
        /// </summary>
        public int EventIndex;

        /// <summary>
        /// The hash of the name of the function of the event.
        /// </summary>
        public StringHash FunctionName;

        /// <summary>
        /// The float parameter value that the event stores.
        /// </summary>
        public float FloatParameter;

        /// <summary>
        /// The int parameter value that the event stores.
        /// </summary>
        public int IntParameter;

        /// <summary>
        /// The hash of the string parameter of the event.
        /// </summary>
        public StringHash StringParameter;

        /// <summary>
        /// The object reference parameter of the event, or `EntityId.None` if the event has none.
        /// </summary>
        public EntityId ObjectReferenceParameter;

        /// <summary>
        /// The time of the event on the curves of the clip, in seconds.
        /// </summary>
        /// <remarks>
        /// The value is between <see cref="Clip.StartTime"/> and <see cref="Clip.StopTime"/>, the same range as the
        /// curves of the clip. It isn't the time since the start of the clip.
        /// </remarks>
        public DiscreteTime Time;

        /// <summary>
        /// The loop count of the clip cycle in which the timer crossed the event, as a
        /// <see cref="TimerState.LoopCount"/> value.
        /// </summary>
        /// <remarks>
        /// When a frame spans several loops, each gathered event carries the loop count of its own cycle.
        /// </remarks>
        public int Loop;
    }

    // Mirror of the native GameplayMarkupMetadata (gameplaymarkup.h)
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal struct MarkupMetadataBlob
    {
        [FieldOffset(0)]  public long StartTime;
        [FieldOffset(8)]  public long Duration;
        [FieldOffset(16)] public uint TypeHash;
        [FieldOffset(20)] public int  MarkupDataOffset;
        [FieldOffset(24)] public int  MarkupSize;
        [FieldOffset(28)] public byte IsRanged;
    }

    /// <summary>
    /// Functions to read the animation events baked into a clip.
    /// </summary>
    /// <remarks>
    /// The clip data stores the time, the function name, and the parameters of each animation event of the source
    /// animation clip. Use
    /// <see cref="GatherAnimationEvents(ref Clip, in TimerState, NativeList{GatheredAnimationEvent})"/> to read the
    /// events that a clip timer crossed during a frame. The timer is the one that you pass to
    /// <see cref="ClipSampler.Sample(ref AnimationStream, in TimerState)"/>, so the events match the sampled pose.
    ///
    /// The following example uses the `System.Collections.Generic`, `Unity.Animations`, `Unity.Collections`,
    /// `Unity.Entities`, `Unity.IntegerTime`, and `UnityEngine` namespaces. It gathers the animation events that the
    /// playback time of a clip passes between two updates. The range can span several loops of a looping clip.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/GatherEventsExample.cs#GatherEvents}]]></code>
    /// </example>
    /// <seealso cref="GatheredAnimationEvent"/>
    [GenerateTestsForBurstCompatibility]
    public static unsafe class AnimationEventUtility
    {
        // AnimationEvent::kMarkupTypeHash (AnimationEvent.h)
        internal const uint AnimationEventMarkupTypeHash = 0xAE000001u;

        /// <summary>
        /// Appends the animation events that a timer crossed during its last update to a list.
        /// </summary>
        /// <remarks>
        /// Use this method with a clip timer from <see cref="Clip.ToTimerState(DiscreteTime)"/>, after you update the
        /// timer. The timer holds the time on the curves of the clip. The events therefore use the same time as the
        /// pose that <see cref="ClipSampler"/> samples from the timer, including the start time, the stop time, and
        /// the cycle offset of the clip.
        ///
        /// The method gathers the events after <see cref="TimerState.PreviousTime"/>, up to and including
        /// <see cref="TimerState.Time"/>. When the timer loops, the method gathers the events once for each cycle
        /// that the timer crossed. A frame that spans several loops therefore returns the events of every loop. A
        /// timer that has a negative <see cref="TimerState.TimeScale"/> plays backward and returns the events in
        /// descending time order. When the wrap mode is <see cref="WrapMode.PingPong"/>, the timer turns around at
        /// the ends of the range. It returns an event at an end once. The method doesn't gather events when the timer
        /// is paused or stopped, or when its delta time is 0.
        ///
        /// The method includes the events at `TimerState.PreviousTime` in the following cases, so that an event at
        /// the start of the clip isn't missed:
        ///
        ///- The update is the first update of a timer from `Clip.ToTimerState` with a time of 0.
        ///- The timer was stopped during the previous update.
        ///- The timer was rewound with <see cref="TimerStateExtensions.Rewind"/> or moved with
        /// <see cref="TimerStateExtensions.JumpToTime"/> since the previous update.
        ///
        /// The method appends the events in playback order and doesn't clear the list first.
        /// </remarks>
        /// <param name="clip">The clip that holds the animation events to gather.</param>
        /// <param name="timer">The clip timer, updated for the frame. Its range must be inside the clip.</param>
        /// <param name="events">The list to append the gathered events to.</param>
        /// <exception cref="ArgumentException">Thrown if safety checks are enabled and the range of
        /// <paramref name="timer"/> isn't inside the clip.</exception>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and the clip stores events of
        /// a version that this method can't read. Reimport the animation clip.</exception>
        public static void GatherAnimationEvents(ref Clip clip, in TimerState timer, NativeList<GatheredAnimationEvent> events)
        {
            ValidateRangeIsInsideClip(ref clip, in timer);

            var markups = new EventMarkups(ref clip);
            if (markups.Count == 0 || timer.PlayState != TimeControlState.Playing || timer.DeltaTime == DiscreteTime.Zero)
                return;

            var start = timer.Range.Start.Value;
            var end = timer.Range.End.Value;
            var previousTime = timer.PreviousTime.Value;
            var time = timer.Time.Value;
            var forward = timer.DeltaTime > DiscreteTime.Zero;
            // The events at the previous time haven't been gathered when the timer starts, or when it was rewound or jumped
            var includeStart = timer.PreviousPlayState == TimeControlState.Stopped || timer.WasReset != 0;

            switch (timer.WrapMode)
            {
                case WrapMode.Loop:
                    GatherLoop(in markups, in timer, start, end, includeStart, forward, events);
                    break;
                case WrapMode.PingPong:
                    GatherPingPong(in markups, in timer, start, end, includeStart, forward, events);
                    break;
                case WrapMode.Once:
                    var passedTheEnd = timer.LoopCount != timer.PreviousLoopCount;
                    markups.Append(previousTime, includeStart, passedTheEnd ? (forward ? end : start) : time, timer.PreviousLoopCount, events);
                    break;
                default:
                    markups.Append(Clamp(previousTime, start, end), includeStart, Clamp(time, start, end), timer.PreviousLoopCount, events);
                    break;
            }
        }

        /// <summary>
        /// Appends the animation events that a clip crosses between two playback times to a list.
        /// </summary>
        /// <remarks>
        /// Use this method when you track the playback time of the clip yourself. The times are playback times from
        /// the start of the clip, as in <see cref="ClipSampler.Sample(ref AnimationStream, DiscreteTime)"/>. The
        /// method creates a clip timer at <paramref name="from"/>, advances it to <paramref name="to"/>, and passes
        /// it to <see cref="GatherAnimationEvents(ref Clip, in TimerState, NativeList{GatheredAnimationEvent})"/>, so
        /// the events are the ones that the sampled pose crosses.
        ///
        /// When <paramref name="from"/> is greater than <paramref name="to"/>, the clip plays backward from
        /// <paramref name="from"/> to <paramref name="to"/>. To step forward over the end of a looping clip, pass a
        /// <paramref name="to"/> that is greater than the length of the clip. The range can span any number of loops.
        /// The range excludes <paramref name="from"/> and includes <paramref name="to"/>, except that a
        /// <paramref name="from"/> of 0 includes the events at the start of the clip.
        /// </remarks>
        /// <param name="clip">The clip that holds the animation events to gather.</param>
        /// <param name="from">The start of the range, in seconds from the start of the clip.</param>
        /// <param name="to">The end of the range, in seconds from the start of the clip.</param>
        /// <param name="events">The list to append the gathered events to.</param>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and the clip stores events of
        /// a version that this method can't read. Reimport the animation clip.</exception>
        public static void GatherAnimationEvents(ref Clip clip, DiscreteTime from, DiscreteTime to, NativeList<GatheredAnimationEvent> events)
        {
            var timer = clip.ToTimerState(from);
            timer.Update(to - from);
            GatherAnimationEvents(ref clip, in timer, events);
        }

        /// <summary>
        /// Appends the animation events that a timer crossed during its last update to a list.
        /// </summary>
        /// <remarks>
        /// This method behaves like
        /// <see cref="GatherAnimationEvents(ref Clip, in TimerState, NativeList{GatheredAnimationEvent})"/>. Use it
        /// with the reference that <see cref="AnimationClipExtensions.ToClipReference"/> returns, or with
        /// <see cref="ClipSampler.Clip"/>, so that you don't convert the reference first.
        /// </remarks>
        /// <param name="clip">The reference to the clip that holds the animation events to gather. It must be valid.</param>
        /// <param name="timer">The clip timer, updated for the frame. Its range must be inside the clip.</param>
        /// <param name="events">The list to append the gathered events to.</param>
        /// <exception cref="ArgumentException">Thrown if safety checks are enabled and the range of
        /// <paramref name="timer"/> isn't inside the clip.</exception>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and the clip stores events of
        /// a version that this method can't read. Reimport the animation clip.</exception>
        public static void GatherAnimationEvents(BlobObjectReference<Clip> clip, in TimerState timer, NativeList<GatheredAnimationEvent> events)
        {
            var clipAsset = clip.AsBlobAssetReference();
            GatherAnimationEvents(ref clipAsset.Value, in timer, events);
        }

        /// <summary>
        /// Appends the animation events that a clip crosses between two playback times to a list.
        /// </summary>
        /// <remarks>
        /// This method behaves like
        /// <see cref="GatherAnimationEvents(ref Clip, DiscreteTime, DiscreteTime, NativeList{GatheredAnimationEvent})"/>.
        /// Use it with the reference that <see cref="AnimationClipExtensions.ToClipReference"/> returns, or with
        /// <see cref="ClipSampler.Clip"/>, so that you don't convert the reference first.
        /// </remarks>
        /// <param name="clip">The reference to the clip that holds the animation events to gather. It must be valid.</param>
        /// <param name="from">The start of the range, in seconds from the start of the clip.</param>
        /// <param name="to">The end of the range, in seconds from the start of the clip.</param>
        /// <param name="events">The list to append the gathered events to.</param>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and the clip stores events of
        /// a version that this method can't read. Reimport the animation clip.</exception>
        public static void GatherAnimationEvents(BlobObjectReference<Clip> clip, DiscreteTime from, DiscreteTime to, NativeList<GatheredAnimationEvent> events)
        {
            var clipAsset = clip.AsBlobAssetReference();
            GatherAnimationEvents(ref clipAsset.Value, from, to, events);
        }

        static void GatherLoop(in EventMarkups markups, in TimerState timer, long start, long end, bool includeStart, bool forward, NativeList<GatheredAnimationEvent> events)
        {
            var previousTime = timer.PreviousTime.Value;
            var time = timer.Time.Value;
            var cycleCount = math.max(forward ? timer.LoopCount - timer.PreviousLoopCount : timer.PreviousLoopCount - timer.LoopCount, 0);
            var direction = forward ? 1 : -1;

            if (cycleCount == 0)
            {
                markups.Append(previousTime, includeStart, time, timer.PreviousLoopCount, events);
                return;
            }

            // The wrap jumps from one end to the other, so both ends fire, and the cycle that starts includes its first time
            markups.Append(previousTime, includeStart, forward ? end : start, timer.PreviousLoopCount, events);
            for (var cycle = 1; cycle < cycleCount; ++cycle)
                markups.Append(forward ? start : end, true, forward ? end : start, timer.PreviousLoopCount + direction * cycle, events);

            markups.Append(forward ? start : end, true, time, timer.LoopCount, events);
        }

        static void GatherPingPong(in EventMarkups markups, in TimerState timer, long start, long end, bool includeStart, bool forward, NativeList<GatheredAnimationEvent> events)
        {
            var previousTime = timer.PreviousTime.Value;
            var time = timer.Time.Value;
            var turnCount = math.abs(timer.LoopCount - timer.PreviousLoopCount);
            var loopDirection = timer.LoopCount >= timer.PreviousLoopCount ? 1 : -1;

            // The timer moves toward the end of the range in the even cycles, and toward the start in the odd cycles
            var towardEnd = forward == ((timer.PreviousLoopCount & 1) == 0);

            if (turnCount == 0)
            {
                markups.Append(previousTime, includeStart, time, timer.PreviousLoopCount, events);
                return;
            }

            // A turn point fires when the timer arrives, and not when it leaves
            markups.Append(previousTime, includeStart, towardEnd ? end : start, timer.PreviousLoopCount, events);
            for (var turn = 1; turn < turnCount; ++turn)
            {
                towardEnd = !towardEnd;
                markups.Append(towardEnd ? start : end, false, towardEnd ? end : start, timer.PreviousLoopCount + loopDirection * turn, events);
            }

            towardEnd = !towardEnd;
            markups.Append(towardEnd ? start : end, false, time, timer.LoopCount, events);
        }

        static long Clamp(long value, long min, long max) => value < min ? min : (value > max ? max : value);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateRangeIsInsideClip(ref Clip clip, in TimerState timer)
        {
            if (timer.Range.Start < new DiscreteTime(clip.StartTime) || timer.Range.End > new DiscreteTime(clip.StopTime))
                throw new ArgumentException("The range of the timer is not inside the clip. Create the timer with Clip.ToTimerState.");
        }

        // The animation event markups of a clip, sorted by time
        readonly struct EventMarkups
        {
            readonly MarkupMetadataBlob* m_Metadata;
            readonly byte* m_Data;
            readonly int m_PayloadSize;

            public readonly int Count;

            public EventMarkups(ref Clip clip)
            {
                m_Metadata = (MarkupMetadataBlob*)clip.m_MarkupsMetadata.Get();
                m_Data = (byte*)clip.m_MarkupsData.Get();
                Count = m_Metadata != null ? (int)clip.m_MarkupsMetadataCount : 0;

                // The payload is the fields of GatheredAnimationEvent before Time
                var gathered = default(GatheredAnimationEvent);
                m_PayloadSize = (int)((byte*)&gathered.Time - (byte*)&gathered);
            }

            // Appends the events from a time, to a time that is included. The events go in the order of the times
            public void Append(long from, bool includeFrom, long to, int loop, NativeList<GatheredAnimationEvent> events)
            {
                if (from <= to)
                {
                    for (var i = 0; i < Count; ++i)
                    {
                        var time = m_Metadata[i].StartTime;
                        if ((includeFrom ? time >= from : time > from) && time <= to && IsEvent(i))
                            events.Add(Read(i, loop));
                    }
                }
                else
                {
                    for (var i = Count - 1; i >= 0; --i)
                    {
                        var time = m_Metadata[i].StartTime;
                        if ((includeFrom ? time <= from : time < from) && time >= to && IsEvent(i))
                            events.Add(Read(i, loop));
                    }
                }
            }

            bool IsEvent(int index)
            {
                if (m_Metadata[index].TypeHash != AnimationEventMarkupTypeHash)
                    return false;

                // The clip was baked before the payload carried the parameters of the event
                if (m_Metadata[index].MarkupSize != m_PayloadSize)
                {
                    ThrowIncompatiblePayload();
                    return false;
                }

                return true;
            }

            GatheredAnimationEvent Read(int index, int loop)
            {
                var gathered = default(GatheredAnimationEvent);
                UnsafeUtility.MemCpy(&gathered, m_Data + m_Metadata[index].MarkupDataOffset, m_PayloadSize);
                gathered.Time = DiscreteTime.FromTicks(m_Metadata[index].StartTime);
                gathered.Loop = loop;
                return gathered;
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            static void ThrowIncompatiblePayload() =>
                throw new InvalidOperationException("The clip stores animation events with a payload that this version can't read. Reimport the animation clip.");
        }
    }
}
