// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Event arguments for performance bottleneck changes. These are used in the <see cref="PerformanceBottleneckChangeHandler"/>.
    /// </summary>
    public struct PerformanceBottleneckChangeEventArgs
    {
        /// <summary>
        /// The performance bottleneck reported in the event.
        /// </summary>
        public PerformanceBottleneck PerformanceBottleneck { get; set; }
    }

    /// <summary>
    /// You can subscribe to the bottleneck event delegate which sends the <see cref="PerformanceBottleneckChangeEventArgs"/> when the bottleneck changes.
    /// </summary>
    /// <param name="bottleneckEventArgs">The <see cref="PerformanceBottleneckChangeEventArgs"/> that describes the performance bottleneck state.</param>
    public delegate void PerformanceBottleneckChangeHandler(PerformanceBottleneckChangeEventArgs bottleneckEventArgs);

    /// <summary>
    /// Event arguments for boost changes. These are used in the <see cref="PerformanceBottleneckChangeHandler"/>.
    /// </summary>
    public struct PerformanceBoostChangeEventArgs
    {
        /// <summary>
        /// Is the CPU boosted.
        /// </summary>
        public bool CpuBoost { get; set; }

        /// <summary>
        /// Is the GPU boosted
        /// </summary>
        public bool GpuBoost { get; set; }
    }

    /// <summary>
    /// You can subscribe to the boost event delegate which sends the <see cref="PerformanceBoostChangeEventArgs"/> when a boost changes.
    /// </summary>
    /// <param name="boostEventArgs">The <see cref="PerformanceBoostChangeEventArgs"/> that describes the boost event.</param>
    public delegate void PerformanceBoostChangeHandler(PerformanceBoostChangeEventArgs boostEventArgs);

    /// <summary>
    /// Arguments for the performance level change event. These are used in the <see cref="PerformanceLevelChangeHandler"/>.
    /// </summary>
    public struct PerformanceLevelChangeEventArgs
    {
        /// <summary>
        /// The new CPU level.
        /// </summary>
        public int CpuLevel { get; set; }

        /// <summary>
        /// The difference in CPU levels
        /// 0 if the previous or new level equals <see cref="Constants.UnknownPerformanceLevel"/>.
        /// </summary>
        public int CpuLevelDelta { get; set; }

        /// <summary>
        /// The new GPU level.
        /// </summary>
        public int GpuLevel { get; set; }

        /// <summary>
        /// The difference in GPU levels.
        /// 0 if either the previous or the new level equals <see cref="Constants.UnknownPerformanceLevel"/>.
        /// </summary>
        public int GpuLevelDelta { get; set; }

        /// <summary>
        /// The current PerformanceControlMode. See <see cref="IDevicePerformanceControl.PerformanceControlMode"/>.
        /// </summary>
        public PerformanceControlMode PerformanceControlMode { get; set; }

        /// <summary>
        /// True if the change was caused by manual adjustments to <see cref="IDevicePerformanceControl.CpuLevel"/> or <see cref="IDevicePerformanceControl.GpuLevel"/> during automatic mode, false otherwise.
        /// </summary>
        public bool ManualOverride { get; set; }
    }

    /// <summary>
    /// You can subscribe to the performance level event delegate which sends the <see cref="PerformanceLevelChangeEventArgs"/> when the performance level changes.
    /// </summary>
    /// <param name="levelChangeEventArgs">The performance level change event is sent to the delegate via the level change event argument.</param>
    public delegate void PerformanceLevelChangeHandler(PerformanceLevelChangeEventArgs levelChangeEventArgs);

    /// <summary>
    /// You can use the performance status interface to obtain performance metrics, frame timing, and subscribe to bottleneck and performance event changes.
    /// </summary>
    public interface IPerformanceStatus
    {
        /// <summary>
        /// Allows you to query the latest performance metrics.
        /// </summary>
        PerformanceMetrics PerformanceMetrics { get; }

        /// <summary>
        /// Allows you to query the latest frame timing measurements.
        /// </summary>
        FrameTiming FrameTiming { get; }

        /// <summary>
        /// Subscribe to performance events and get updates when the bottleneck changes.
        /// </summary>
        event PerformanceBottleneckChangeHandler PerformanceBottleneckChangeEvent;

        /// <summary>
        /// Subscribe to events and get updates when the the current CPU or GPU level changes.
        /// </summary>
        event PerformanceLevelChangeHandler PerformanceLevelChangeEvent;

        /// <summary>
        /// Subscribe to events and get updates when the the current CPU or GPU is boosted.
        /// </summary>
        event PerformanceBoostChangeHandler PerformanceBoostChangeEvent;

        /// <summary>
        /// Allows you to query the latest performance mode.
        /// </summary>
        PerformanceMode PerformanceMode { get; }
    }

    /// <summary>
    /// PerformanceMetrics store the current bottleneck, CPU, and GPU levels
    /// </summary>
    public struct PerformanceMetrics
    {
        /// <summary>
        /// Current CPU performance level.
        /// This value updates once per frame when changes are applied to <see cref="IDevicePerformanceControl.CpuLevel"/>.
        /// Value in the range [<see cref="Constants.MinCpuPerformanceLevel"/>, <see cref="IDevicePerformanceControl.MaxCpuPerformanceLevel"/>] or <see cref="Constants.UnknownPerformanceLevel"/>.
        /// </summary>
        /// <value>Current CPU performance level</value>
        public int CurrentCpuLevel { get; set; }

        /// <summary>
        /// Current GPU performance level.
        /// This value updates once per frame when changes are applied to <see cref="IDevicePerformanceControl.GpuLevel"/>.
        /// Value in the range [<see cref="Constants.MinGpuPerformanceLevel"/>, <see cref="IDevicePerformanceControl.MaxGpuPerformanceLevel"/>] or <see cref="Constants.UnknownPerformanceLevel"/>.
        /// </summary>
        /// <value>Current GPU performance level</value>
        public int CurrentGpuLevel { get; set; }

        /// <summary>
        /// Current performance bottleneck which describes if the program is CPU, GPU, or `Application.targetFrameRate` bound.
        /// </summary>
        public PerformanceBottleneck PerformanceBottleneck { get; set; }

        /// <summary>
        /// CPU boosted.
        /// </summary>
        /// <value>CPU boosted</value>
        public bool CpuPerformanceBoost { get; set; }

        /// <summary>
        /// GPU boosted.
        /// </summary>
        /// <value>GPU boosted</value>
        public bool GpuPerformanceBoost { get; set; }

        /// <summary>
        /// Current CPU cluster information information. Updated at application startup.
        /// </summary>
        /// <value> CPU cluster information</value>
        public ClusterInfo ClusterInfo { get; set; }
    }

    /// <summary>
    /// FrameTiming stores timing information about CPU, GPU, and the overall frame time.
    /// </summary>
    public struct FrameTiming
    {
        /// <summary>
        /// The overall frame time in seconds.
        /// Returns `-1.0f` if no timing information is available (for example, in the first frame or directly after resume).
        /// </summary>
        /// <value>Frame time in seconds</value>
        public float CurrentFrameTime { get; set; }

        /// <summary>
        /// The overall frame time as an average over the past 100 frames (in seconds).
        /// Returns -1.0f if no timing information is available (for example, in the first frame or directly after resume).
        /// </summary>
        /// <value>Frame time in seconds</value>
        public float AverageFrameTime { get; set; }

        /// <summary>
        /// Returns the GPU time of the last completely rendered frame (in seconds).
        /// Returns `-1.0f` if no timing information is available.
        /// The GPU time only includes the time the GPU spent on rendering a frame (for example, in the first frame or directly after resume).
        /// </summary>
        /// <value>Frame time in seconds</value>
        public float CurrentGpuFrameTime { get; set; }

        /// <summary>
        /// Returns the overall frame time as an average over the past 100 frames (in seconds).
        /// Returns `-1.0f` if no timing information is available.
        /// The GPU time only includes the time the GPU spent on rendering a frame (for example, in the first frame or directly after resume).
        /// </summary>
        /// <value>Frame time value in seconds</value>
        public float AverageGpuFrameTime { get; set; }

        /// <summary>
        /// Returns the main thread CPU time of the last frame (in seconds).
        /// The CPU time includes only time the CPU spent executing Unity's main and/or render threads.
        /// Returns `-1.0f` if no timing information is available (for example, in the first frame or directly after resume).
        /// </summary>
        /// <value>Frame time value in seconds</value>
        public float CurrentCpuFrameTime { get; set; }

        /// <summary>
        /// Returns the main thread CPU time as an average over the past 100 frames (in seconds).
        /// Returns `-1.0f` if this is not available (for example, in the first frame or directly after resume).
        /// The CPU time includes only the time the CPU spent executing Unity's main and/or render threads.
        /// </summary>
        /// <value>Frame time in seconds</value>
        public float AverageCpuFrameTime { get; set; }
    }

    /// <summary>
    /// The performance mode enum describes what is currently the active performance mode of the stystem.
    /// </summary>
    public enum PerformanceMode
    {
        /// <summary>
        /// Performance mode is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// Default performance mode.
        /// </summary>
        Standard,

        /// <summary>
        /// Performance mode is optimized and may be a mix of accelerating both CPU and GPU.
        /// </summary>
        Optimize,

        /// <summary>
        /// Performance mode is accelerates CPU.
        /// </summary>
        CPU,

        /// <summary>
        /// Performance mode is accelerates GPU.
        /// </summary>
        GPU,

        /// <summary>
        /// Performance is limited as mode is set to preserve battery.
        /// </summary>
        Battery
    }

    /// <summary>
    /// The performance bottleneck enum describes what is currently limiting the system.
    /// </summary>
    public enum PerformanceBottleneck
    {
        /// <summary>
        /// Framerate bottleneck is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// Framerate is limited by CPU processing.
        /// </summary>
        CPU,

        /// <summary>
        /// Framerate is limited by GPU processing.
        /// </summary>
        GPU,

        /// <summary>
        /// Framerate is limited by `Application.targetFrameRate`.
        /// In this case, you should consider lowering the application's performance requirements (see <see cref="IDevicePerformanceControl.AutomaticPerformanceControl"/>).
        /// </summary>
        TargetFrameRate
    }

    /// <summary>
    /// The cluster info describes the CPU Cluster setup.
    /// </summary>
    public struct ClusterInfo
    {
        /// <summary>
        /// Number of big cores supported by the CPU.
        /// </summary>
        public int BigCore { get; set; }

        /// <summary>
        /// Number of medium cores supported by the CPU.
        /// </summary>
        public int MediumCore { get; set; }

        /// <summary>
        /// Number of little cores supported by the CPU.
        /// </summary>
        public int LittleCore { get; set; }
    }
}
