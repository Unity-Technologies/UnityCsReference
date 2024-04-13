// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    /// <summary>
    /// Determines which mode of time measurement to use in wait operations.
    /// </summary>
    public enum WaitTimeoutMode
    {
        /// <summary>
        /// Evaluates timeout values as units of real time.
        /// </summary>
        Realtime,
        /// <summary>
        /// Evaluates timeout values as units of in-game time, which is scaled according to the value of <see cref="Time.timeScale"/>.
        /// </summary>
        InGameTime
    }
}
