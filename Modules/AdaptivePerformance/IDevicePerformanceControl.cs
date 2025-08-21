// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// The device performance control interface handles all control elements related to the device performance. You can
    /// change the <see cref="IDevicePerformanceControl.AutomaticPerformanceControl"/> settings or retrieve information about the <see cref="CpuLevel"/> and <see cref="GpuLevel"/>.
    /// </summary>
    public interface IDevicePerformanceControl
    {
        /// <summary>
        /// When set to true, which is the default value, Adaptive Performance automatically sets  <see cref="CpuLevel"/> and <see cref="GpuLevel"/>.
        /// </summary>
        /// <value>True when Adaptive Performance controls <see cref="CpuLevel"/> and <see cref="GpuLevel"/>, otherwise false. The default value is true. </value>
        bool AutomaticPerformanceControl { get; set; }

        /// <summary>
        /// The current PerformanceControlMode.
        /// PerformanceControlMode is affected by <see cref="AutomaticPerformanceControl"/>.
        /// </summary>
        /// <value>The current PerformanceControlMode</value>
        PerformanceControlMode PerformanceControlMode { get; }

        /// <summary>
        /// The maximum valid CPU performance level you use with <see cref="CpuLevel"/>.
        /// The minimum value returned is <see cref="Constants.MinCpuPerformanceLevel"/>.
        /// This value does not change after startup is complete.
        /// </summary>
        int MaxCpuPerformanceLevel { get; }

        /// <summary>
        /// The maximum valid GPU performance level you use with <see cref="GpuLevel"/>.
        /// The minimum value returned is <see cref="Constants.MinGpuPerformanceLevel"/>.
        /// This value does not change after startup is complete.
        /// </summary>
        int MaxGpuPerformanceLevel { get; }

        /// <summary>
        /// The requested CPU performance level.
        /// Higher levels typically allow CPU cores to run at higher clock speeds.
        /// The consequence is that thermal warnings and throttling might happen sooner when the device cannot sustain high clock speeds.
        /// Changes are applied once per frame.
        /// It is recommended to set the CpuLevel as low as possible to save power.
        /// The valid value range is [<see cref="Constants.MinCpuPerformanceLevel"/>, <see cref="IDevicePerformanceControl.MaxCpuPerformanceLevel"/>].
        /// </summary>
        /// <value>The requested CPU performance level</value>
        int CpuLevel { get; set; }

        /// <summary>
        /// The requested GPU performance level.
        /// Higher levels typically allow the GPU to run at higher clock speeds.
        /// The consequence is that thermal warnings and throttling might happen sooner when the device cannot sustain high clock speeds.
        /// Changes are applied once per frame.
        /// It is recommended to set the GpuLevel as low as possible to save power.
        /// The valid value range is [<see cref="Constants.MinGpuPerformanceLevel"/>, <see cref="IDevicePerformanceControl.MaxGpuPerformanceLevel"/>].
        /// </summary>
        /// <value>The requested GPU performance level</value>
        int GpuLevel { get; set; }

        /// <summary>
        /// The requested CPU boost mode state.
        /// Enabled typically allows CPU cores to run at higher clock speeds.
        /// The consequence is that thermal warnings and throttling might happen sooner when the device cannot sustain high clock speeds.
        /// Changes are applied once per frame.
        /// It is recommended to not use a boost often and certainly not continuously to save power.
        /// </summary>
        /// <value>True when CPU boost is active, otherwise false. The default value is false.</value>
        bool CpuPerformanceBoost { get; set; }

        /// <summary>
        /// The requested GPU boost mode state.
        /// Enabled typically allows GPU cores to run at higher clock speeds.
        /// The consequence is that thermal warnings and throttling might happen sooner when the device cannot sustain high clock speeds.
        /// Changes are applied once per frame.
        /// It is recommended to not use a boost often and certainly not continuously to save power.
        /// </summary>
        /// <value>True when CPU boost is active, otherwise false. The default value is false.</value>
        bool GpuPerformanceBoost { get; set; }
    }

    /// <summary>
    /// Enum used to describe the performance control mode used by Adaptive Performance. Can be read from <see cref="IDevicePerformanceControl.PerformanceControlMode"/>.
    /// </summary>
    public enum PerformanceControlMode
    {
        /// <summary>
        /// Adaptive Performance controls performance levels automatically (default).
        /// This mode is enabled by setting <see cref="IDevicePerformanceControl.AutomaticPerformanceControl"/> to true.
        /// </summary>
        Automatic,

        /// <summary>
        /// You can control performance levels via <see cref="IDevicePerformanceControl.CpuLevel"/> and <see cref="IDevicePerformanceControl.GpuLevel"/>.
        /// This mode is enabled by setting <see cref="IDevicePerformanceControl.AutomaticPerformanceControl"/> to false.
        /// </summary>
        Manual,

        /// <summary>
        /// The operating system controls performance levels.
        /// This happens if manual control is not supported or if the system is in a thermal throttling state, at which point the operating system takes over control automatically.
        /// </summary>
        System
    }
}
