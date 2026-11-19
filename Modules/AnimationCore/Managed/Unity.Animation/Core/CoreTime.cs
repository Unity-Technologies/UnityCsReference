// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.IntegerTime;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    static public partial class Core
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DiscreteTime Repeat(DiscreteTime t, DiscreteTime length)
        {
            if (length <= DiscreteTime.Zero)
                return DiscreteTime.Zero;

            // Mecanim rounds the normalized time up in float, so a time that a float computation left a few ticks short
            // of a whole number of cycles belongs to the next cycle. The remainder of such a time clamps to 0.
            var ratio = (double)t / (double)length;
            var cycles = math.min(math.floor(RoundUpToFloat(ratio)), math.floor(ratio) + 1.0);
            return (t - new DiscreteTime(cycles) * (float)length).Clamp(DiscreteTime.Zero, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double RoundUpToFloat(double value)
        {
            var rounded = (float)value;
            if ((double)rounded < value)
                rounded = math.asfloat(math.asint(rounded) + (rounded < 0f ? -1 : 1));
            return rounded;
        }

        /// <summary>
        /// Loops the value t, so that it is never larger than interval.End and never smaller than interval.Start.
        /// </summary>
        /// <param name="t">The time to loop.</param>
        /// <param name="interval">The time range.</param>
        /// <param name="loopCount">The number of loop.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal DiscreteTime Repeat(DiscreteTime t, DiscreteTimeInterval interval, ref int loopCount)
        {
            var duration = interval.Duration;
            t = t - interval.Start;

            var remainder = Repeat(t, duration);

            // Before the start, a whole number of cycles maps to the end of the interval, like Mecanim
            if (t < DiscreteTime.Zero && remainder == DiscreteTime.Zero)
                remainder = duration;

            var ratio = math.select((int)math.round((double)((t - remainder) / (double)interval.Duration)), 0, interval.Duration == DiscreteTime.Zero);
            loopCount += ratio;

            return interval.Start + remainder;
        }

        /// <summary>
        /// PingPong returns a value that will increment and decrement between the value interval.Start and interval.End.
        /// </summary>
        /// <param name="t">The time to ping pong.</param>
        /// <param name="interval">The time range.</param>
        /// <param name="loopCount">The number of loop. A full cycle from start to end to start count for 2</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal DiscreteTime PingPong(DiscreteTime t, DiscreteTimeInterval interval, ref int loopCount)
        {
            var duration = interval.Duration;
            t = t - interval.Start;

            var remainder = Repeat(t, duration);

            var ratio = math.select((int)math.round((double)((t - remainder) / (double)interval.Duration)), 0, interval.Duration == DiscreteTime.Zero);
            loopCount += ratio;

            remainder = remainder.Select(duration - remainder, (ratio & 1) > 0);

            return interval.Start + remainder;
        }

        /// <summary>
        /// Scales the duration of a clip or graph given a speed and optional multiplier.
        ///
        /// When playback speed of a synchronized [graph/clip] is close to zero, the total effective duration being
        /// inversely proportional tends toward infinity. We pad the reciprocal of the playback speed with
        /// math.FLT_MIN_NORMAL to generate a signicicant value that is still finite and usable.
        ///
        /// We specifically don't use rcpsafe because it has the inverse effect of what we want to accomplish as it clamps
        /// the [graph/clip]'s playback speed contribution to zero. This creates a situation of an effective duration that tends towards
        /// zero instead of infinity. For example, if you transition from a graph with playback speed of zero to another graph
        /// with "normal" speed, the first few frames of transition animate really quickly because the total effective duration
        /// is way too small until the mixer weights converge to the new state.
        ///
        /// Instead, transitioning in or out of a graph with zero playback speed just dramatically increases the groups total effective
        /// duration and dampens the overall animation progress as you would expect.
        /// </summary>
        /// <param name="duration">A clip duration or graph effective duration.</param>
        /// <param name="playbackSpeed">A clip or graph playback speed.</param>
        /// <param name="multiplier">Optional multiplier, used primarily for weighting synchronized graphs.</param>
        /// <returns>The scaled duration.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal DiscreteTime ScaleDuration(DiscreteTime duration, double playbackSpeed, double multiplier = 1.0) =>
            duration * (multiplier * math.rcp(math.max(math.EPSILON, math.abs(playbackSpeed))));

    }

    static class DiscreteTimeExtensions
    {
        public static readonly DiscreteTime One = new DiscreteTime(1);
    }
}
