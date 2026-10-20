// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace Unity.Core
{
    /// <summary>
    /// Encapsulates state to measure a <see cref="Unity.Entities.World"/>'s simulation time.
    /// </summary>
    /// <remarks>
    /// This data is most frequently read using <see cref="Unity.Entities.World.Time"/>. It is updated every frame by
    /// <see cref="T:Unity.Entities.UpdateWorldTimeSystem"/>. To temporarily override the time values,
    /// use <see cref="Unity.Entities.World.SetTime"/> or <see cref="Unity.Entities.World.PushTime"/>.
    /// </remarks>
    public readonly struct TimeData
    {
        /// <summary>
        /// The total cumulative elapsed time in seconds.
        /// </summary>
        /// <remarks>The ElapsedTime for each World is initialized to zero when the World is created. Thus,
        /// comparing timestamps across Worlds (or between Worlds and MonoBehaviours) is generally an error.</remarks>
        public readonly double ElapsedTime;

        /// <summary>
        /// The time in seconds since the last time-updating event occurred. (For example, a frame.)
        /// </summary>
        public readonly float DeltaTime;

        /// <summary>
        /// Create a new TimeData struct with the given values.
        /// </summary>
        /// <param name="elapsedTime">Time since the start of time collection.</param>
        /// <param name="deltaTime">Elapsed time since the last time-updating event occurred.</param>
        public TimeData(double elapsedTime, float deltaTime)
        {
            ElapsedTime = elapsedTime;
            DeltaTime = deltaTime;
        }

        /// <summary>
        /// Currently, an alias to <see cref="UnityEngine.Time.fixedDeltaTime"/>.
        /// </summary>
        /// <remarks>This member will be deprecated once a native fixed delta time is introduced in Unity.Entities.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public float fixedDeltaTime => Entities.CoreModuleLightShim.Hooks.FixedDeltaTime;
    }
}
