// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// You can subscribe to the performance mode event delegate which sends the <see cref="PerformanceMode"/> when the performance mode changes.
    /// </summary>
    /// <param name="performanceMode"></param>
    public delegate void PerformanceModeEventHandler(PerformanceMode performanceMode);

    /// <summary>
    /// Use the performance mode status interface to receive performance mode status events of the device.
    /// </summary>
    public interface IPerformanceModeStatus
    {
        /// <summary>
        /// The latest performance mode available.
        /// </summary>
        /// <value>The latest performance mode.</value>
        PerformanceMode PerformanceMode { get; }

        /// <summary>
        /// Subscribe to performance mode events which Adaptive Performance sends when the performance mode of the device changes.
        /// </summary>
        event PerformanceModeEventHandler PerformanceModeEvent;
    }
}
