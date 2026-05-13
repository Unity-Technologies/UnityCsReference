// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.SubsystemsImplementation;

namespace UnityEngine.AdaptivePerformance.Provider
{
    /// <summary>
    /// Feature flags
    /// See <see cref="PerformanceDataRecord.ChangeFlags"/> and <seealso cref="AdaptivePerformanceSubsystem.Capabilities"/>.
    /// </summary>
    [Flags]
    public enum Feature
    {
        /// <summary>
        /// No features
        /// </summary>
        None = 0,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.WarningLevel"/>
        /// </summary>
        WarningLevel = 0x1,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.TemperatureLevel"/>
        /// </summary>
        TemperatureLevel = 0x2,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.TemperatureTrend"/>
        /// </summary>
        TemperatureTrend = 0x4,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.CpuPerformanceLevel"/> and <seealso cref="IDevicePerformanceLevelControl.SetPerformanceLevel"/>
        /// </summary>
        CpuPerformanceLevel = 0x8,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.GpuPerformanceLevel"/> and <seealso cref="IDevicePerformanceLevelControl.SetPerformanceLevel"/>
        /// </summary>
        GpuPerformanceLevel = 0x10,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.PerformanceLevelControlAvailable"/> and <seealso cref="AdaptivePerformanceSubsystem.PerformanceLevelControl"/>
        /// </summary>
        PerformanceLevelControl = 0x20,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.GpuFrameTime"/>
        /// </summary>
        GpuFrameTime = 0x40,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.CpuFrameTime"/>
        /// </summary>
        CpuFrameTime = 0x80,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.OverallFrameTime"/>
        /// </summary>
        OverallFrameTime = 0x100,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.CpuPerformanceBoost"/> and <seealso cref="IDevicePerformanceLevelControl.EnableCpuBoost"/>
        /// </summary>
        CpuPerformanceBoost = 0x200,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.GpuPerformanceBoost"/> and <seealso cref="IDevicePerformanceLevelControl.EnableGpuBoost"/>
        /// </summary>
        GpuPerformanceBoost = 0x400,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.ClusterInfo"/>
        /// </summary>
        ClusterInfo = 0x800,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.PerformanceMode"/>
        /// </summary>
        PerformanceMode = 0x1000,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.CpuUtilization"/>
        /// </summary>
        CpuUtilization = 0x2000,
        /// <summary>
        /// See <see cref="PerformanceDataRecord.GpuUtilization"/>
        /// </summary>
        GpuUtilization = 0x4000
    }

    /// <summary>
    /// The performance data record stores all information about the thermal and performance status and delivers it from the provider subsystem to Adaptive Performance for further processing.
    /// </summary>
    public struct PerformanceDataRecord
    {
        /// <summary>
        /// A bitset of features which indicate if their value changed in the last frame or at startup.
        /// Unsupported features will never change.
        /// Fields not changing always have valid data as long as its capability is supported.
        /// </summary>
        /// <value>Bitset</value>
        public Feature ChangeFlags { get; set; }

        /// <summary>
        /// The current normalized temperature level in the range of [0.0, 1.0], or -1.0 when not supported or not available right now.
        /// A level of 1.0 means that the device is thermal throttling.
        /// The temperature level has changed when the <see cref="Feature.TemperatureLevel"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        /// <value>Temperature level in the range of [0.0, 1.0] or -1.0</value>
        public float TemperatureLevel { get; set; }

        /// <summary>
        /// The current temperature trend in the range of [-1.0, 1.0] that is a metric of temperature change over time.
        /// The temperature trend is constant at 0.0 in case the feature is not supported.
        /// The temperature trend has changed when <see cref="Feature.TemperatureTrend"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        /// <value>Temperature trend in the range of [-1.0, 1.0]</value>
        public float TemperatureTrend { get; set; }

        /// <summary>
        /// The current warning level as documented in <see cref="Feature.WarningLevel"/>.
        /// The warning level has changed when <see cref="Feature.WarningLevel"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        /// <value>The current warning level</value>
        public WarningLevel WarningLevel { get; set; }

        /// <summary>
        /// The currently active CPU performance level. This is typically the value previously set with <see cref="IDevicePerformanceLevelControl.SetPerformanceLevel"/> once the levels are successfully applied.
        /// Adaptive Performance might also change this level on its own. This typically happens when the device is thermal throttling or when <see cref="IDevicePerformanceLevelControl.SetPerformanceLevel"/> failed.
        /// CPU performance level has a value in the range of [<see cref="Constants.MinCpuPerformanceLevel"/>, <see cref="IDevicePerformanceLevelControl.MaxCpuPerformanceLevel"/>], or <seealso cref="Constants.UnknownPerformanceLevel"/>.
        /// A value of <see cref="Constants.UnknownPerformanceLevel"/> means that Adaptive Performance took control of performance levels.
        /// CPU performance level has changed when <see cref="Feature.CpuPerformanceLevel"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        /// <value></value>
        public int CpuPerformanceLevel { get; set; }

        /// <summary>
        /// The currently active GPU performance level. This is typically the value previously set with <see cref="IDevicePerformanceLevelControl.SetPerformanceLevel"/> once the levels are successfully applied.
        /// Adaptive Performance might also change this level on its own. This typically happens when the device is thermal throttling or when <see cref="IDevicePerformanceLevelControl.SetPerformanceLevel"/> failed.
        /// GPU performance level has a value in the range of [<see cref="Constants.MinCpuPerformanceLevel"/>, <see cref="IDevicePerformanceLevelControl.MaxGpuPerformanceLevel"/>], or <seealso cref="Constants.UnknownPerformanceLevel"/>.
        /// A value of <see cref="Constants.UnknownPerformanceLevel"/> means that Adaptive Performance took control of performance levels.
        /// GPU performance level has changed when <see cref="Feature.GpuPerformanceLevel"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        public int GpuPerformanceLevel { get; set; }

        /// <summary>
        /// True if =performance levels can currently be controlled manually and aren't controlled by Adaptive Performance or the operating system.
        /// Has changed when <see cref="Feature.PerformanceLevelControl"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        public bool PerformanceLevelControlAvailable { get; set; }

        /// <summary>
        /// The time in seconds spent by the CPU for rendering the last complete frame.
        /// Has changed when <see cref="Feature.CpuFrameTime"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        public float CpuFrameTime { get; set; }

        /// <summary>
        /// The time in seconds spent by the GPU for rendering the last complete frame.
        /// Has changed when <see cref="Feature.GpuFrameTime"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        public float GpuFrameTime { get; set; }

        /// <summary>
        /// The total time in seconds spent for the frame.
        /// Has changed when <see cref="Feature.OverallFrameTime"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        public float OverallFrameTime { get; set; }

        /// <summary>
        /// The currently active CPU boost state. This is typically true if previously enabled with <see cref="IDevicePerformanceLevelControl.EnableCpuBoost"/> once the boost is successfully applied.
        /// Adaptive Performance might also change this level on its own. This typically happens when the device is thermal throttling or when <see cref="IDevicePerformanceLevelControl.EnableCpuBoost"/> fails.
        /// Once the CPU boost is enabled it is active until you receive a callback that it is disabled.
        /// CPU boost level has changed when <see cref="Feature.CpuPerformanceBoost"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        public bool CpuPerformanceBoost { get; set; }

        /// <summary>
        /// The currently active GPU boost state. This is typically true if previously enabled with <see cref="IDevicePerformanceLevelControl.EnableGpuBoost"/> once the boost is successfully applied.
        /// Adaptive Performance might also change this level on its own. This typically happens when the device is thermal throttling or when <see cref="IDevicePerformanceLevelControl.EnableGpuBoost"/> fails.
        /// Once the GPU boost is enabled it is active until you receive a callback that it is disabled.
        /// GPU boost level has changed when <see cref="Feature.GpuPerformanceBoost"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        public bool GpuPerformanceBoost { get; set; }

        /// <summary>
        /// Current CPU cluster information information. Includes number of big, medium and small cores use at the application startup.
        /// </summary>
        public ClusterInfo ClusterInfo { get; set; }

        /// <summary>
        /// Current Performance mode information.
        /// </summary>
        public PerformanceMode PerformanceMode { get; set; }

        /// <summary>
        /// The current normalized CPU utilization in the range of [0.0, 1.0], or -1.0 when not supported.
        /// A value of 0.0 indicates the CPU is lightly loaded and cool.
        /// A value of 1.0 indicates the CPU is heavily loaded or hot.
        /// Has changed when <see cref="Feature.CpuUtilization"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        /// <value>CPU utilization in the range of [0.0, 1.0] or -1.0</value>
        public float CpuUtilization { get; set; }

        /// <summary>
        /// The current normalized GPU utilization in the range of [0.0, 1.0], or -1.0 when not supported.
        /// A value of 0.0 indicates the GPU is lightly loaded and cool.
        /// A value of 1.0 indicates the GPU is heavily loaded or hot.
        /// Has changed when <see cref="Feature.GpuUtilization"/> bit is set in <see cref="ChangeFlags"/>.
        /// </summary>
        /// <value>GPU utilization in the range of [0.0, 1.0] or -1.0</value>
        public float GpuUtilization { get; set; }
    }

    /// <summary>
    /// This interface describes how the Adaptive Performance provider lifecycle behaves.
    /// </summary>
    public interface IApplicationLifecycle
    {
        /// <summary>
        /// Called before an application pauses.
        /// To be called from `MonoBehaviour.OnApplicationPause`.
        /// </summary>
        void ApplicationPause();

        /// <summary>
        /// Called after an application resumes.
        /// To be called from `MonoBehaviour.OnApplicationPause`.
        /// </summary>
        void ApplicationResume();
    }

    /// <summary>
    /// The device performance level control lets you change CPU and GPU levels and informs you about the current levels.
    /// </summary>
    public interface IDevicePerformanceLevelControl
    {
        /// <summary>
        /// Maximum supported CPU performance level. This should not change after startup.
        /// <see cref="Constants.UnknownPerformanceLevel"/> in case performance levels are not supported.
        /// Value in the range of [<see cref="Constants.MinCpuPerformanceLevel"/>, 10].
        /// </summary>
        /// <value>Value in the range of [<see cref="Constants.MinCpuPerformanceLevel"/>, 10]</value>
        int MaxCpuPerformanceLevel { get; }

        /// <summary>
        /// Maximum supported GPU performance level. This should not change after startup.
        /// <see cref="Constants.UnknownPerformanceLevel"/> in case performance levels are not supported.
        /// Value in the range of [<see cref="Constants.MinGpuPerformanceLevel"/>, 10].
        /// </summary>
        /// <value>Value in the range of [<see cref="Constants.MinGpuPerformanceLevel"/>, 10]</value>
        int MaxGpuPerformanceLevel { get; }

        /// <summary>
        /// Request a performance level change.
        /// If <see cref="Constants.UnknownPerformanceLevel"/> is passed, the subsystem picks the level to be used.
        /// </summary>
        /// <param name="cpu">
        /// The new performance level. Can be <see cref="Constants.UnknownPerformanceLevel"/> or range of [<see cref="Constants.MinCpuPerformanceLevel"/>, <see cref="IDevicePerformanceLevelControl.MaxCpuPerformanceLevel"/>].
        /// If <see cref="Feature.CpuPerformanceLevel"/> is not supported (see <see cref="AdaptivePerformanceSubsystem.Capabilities"/>), this parameter is ignored.
        /// </param>
        /// <param name="gpu">
        /// The new performance level. Can be <see cref="Constants.UnknownPerformanceLevel"/> or range of [<see cref="Constants.MinCpuPerformanceLevel"/>, <see cref="IDevicePerformanceLevelControl.MaxGpuPerformanceLevel"/>].
        /// If <see cref="Feature.GpuPerformanceLevel"/> is not supported (see <see cref="AdaptivePerformanceSubsystem.Capabilities"/>), this parameter is ignored.
        /// </param>
        /// <returns>Returns true on success. When this fails, it means that the system took control of the active performance levels.</returns>
        bool SetPerformanceLevel(ref int cpu, ref int gpu);

        /// <summary>
        /// Request a CPU performance boost.
        /// </summary>
        /// If <see cref="Feature.CpuPerformanceBoost"/> is not supported (see <see cref="AdaptivePerformanceSubsystem.Capabilities"/>), this function is ignored.
        /// <returns>Returns true on success. When this fails, it means that the system took control and does not allow boosts.</returns>
        bool EnableCpuBoost();

        /// <summary>
        /// Request a GPU performance boost.
        /// </summary>
        /// If <see cref="Feature.GpuPerformanceBoost"/> is not supported (see <see cref="AdaptivePerformanceSubsystem.Capabilities"/>), this function is ignored.
        /// <returns>Returns true on success. When this fails, it means that the system took control and does not allow boosts.</returns>
        bool EnableGpuBoost();
    }

    /// <summary>
    /// Base class for subsystems to create your custom provider subsystem to deliver data from your provider to Adaptive Performance.
    /// </summary>
    /// <typeparam name="TSubsystem">Concrete subsystem deriving from AdaptivePerformanceSubsystemBase.</typeparam>
    /// <typeparam name="TSubsystemDescriptor">The subsystem descriptor for the underlying subsystem.</typeparam>
    /// <typeparam name="TProvider">Provider type for the AdaptivePerformanceSubsystem-derived subsystem.</typeparam>
    public abstract class AdaptivePerformanceSubsystemBase<TSubsystem, TSubsystemDescriptor, TProvider>
        : SubsystemWithProvider<TSubsystem, TSubsystemDescriptor, TProvider>
        where TSubsystem : SubsystemWithProvider, new()
        where TSubsystemDescriptor : SubsystemDescriptorWithProvider
        where TProvider : SubsystemProvider<TSubsystem>
    {
        /// <summary>
        /// Bitset of supported features.
        /// Does not change after startup.
        /// </summary>
        /// <value>Bitset</value>
        public abstract Feature Capabilities { get; protected set; }

        /// <summary>
        /// To be called once per frame.
        /// The returned data structure's fields are populated with the latest available data, according to the supported <see cref="Capabilities"/>.
        /// </summary>
        /// <returns>Data structure with the most recent performance data.</returns>
        public abstract PerformanceDataRecord Update();

        /// <summary>
        /// Application lifecycle events to be consumed by subsystem.
        /// Can be null if the subsystem does not need special handling on life-cycle events.
        /// The returned reference does not change after startup.
        /// </summary>
        /// <value>Application lifecycle object</value>
        public abstract IApplicationLifecycle ApplicationLifecycle { get; }

        /// <summary>
        /// Control CPU or GPU performance levels of the device.
        /// Can be null if the subsystem does not support controlling CPU/GPU performance levels.
        /// Is null when the <see cref="Feature.PerformanceLevelControl"/> bit is not set in <see cref="Capabilities"/>.
        /// The returned reference does not change after startup.
        /// </summary>
        /// <value>Performance level control object</value>
        public abstract IDevicePerformanceLevelControl PerformanceLevelControl { get; }

        /// <summary>
        /// Returns the version of the subsystem implementation.
        /// Can be used together with SubsystemDescriptor to identify a subsystem.
        /// </summary>
        /// <value>Version number</value>
        public abstract Version Version { get; }

        /// <summary>
        /// Generates a human readable string of subsystem internal stats.
        /// Optional and only used for development.
        /// </summary>
        /// <value>String with subsystem specific statistics</value>
        public abstract string Stats { get; }

        /// <summary>
        /// Returns if the subsystem is initialized successfully.
        /// </summary>
        /// <value>Boolean to tell if subsystem was initialized successfully.</value>
        public abstract bool Initialized { get; protected set; }
    }
    /// <summary>
    /// A class to define a provider subsystem for Adaptive Performance.
    /// </summary>
    public class AdaptivePerformanceSubsystem : AdaptivePerformanceSubsystemBase<AdaptivePerformanceSubsystem, AdaptivePerformanceSubsystemDescriptor, AdaptivePerformanceSubsystem.APProvider>
    {
        /// <summary>
        /// Main constructor, not used in the subsystem specifically.
        /// </summary>
        public AdaptivePerformanceSubsystem()
        {
        }

        /// <summary>
        /// Lifecycle of the Subsystem.
        /// </summary>
        public override IApplicationLifecycle ApplicationLifecycle => provider.ApplicationLifecycle;
        /// <summary>
        /// Control CPU or GPU performance levels of the device.
        /// Can be null if the subsystem does not support controlling CPU/GPU performance levels.
        /// Is null when the <see cref="Feature.PerformanceLevelControl"/> bit is not set in <see cref="Capabilities"/>.
        /// The returned reference does not change after startup.
        /// </summary>
        /// <value>Performance level control object</value>
        public override IDevicePerformanceLevelControl PerformanceLevelControl => provider.PerformanceLevelControl;
        /// <summary>
        /// Returns the version of the subsystem implementation.
        /// Can be used together with SubsystemDescriptor to identify a subsystem.
        /// </summary>
        /// <value>Version number</value>
        public override Version Version => provider.Version;
        /// <summary>
        /// Bitset of supported features.
        /// Does not change after startup.
        /// </summary>
        /// <value>Bitset</value>
        public override Feature Capabilities { get => provider.Capabilities; protected set => provider.Capabilities = value; }
        /// <summary>
        /// Generates a human readable string of subsystem internal stats.
        /// Optional and only used for development.
        /// </summary>
        /// <value>String with subsystem specific statistics</value>
        public override string Stats => provider.Stats;
        /// <summary>
        /// Returns if the subsystem is initialized successfully.
        /// </summary>
        /// <value>Boolean to tell if subsystem was initialized successfully.</value>
        public override bool Initialized { get => provider.Initialized; protected set => provider.Initialized = value; }
        /// <summary>
        /// To be called once per frame.
        /// The returned data structure's fields are populated with the latest available data, according to the supported <see cref="Capabilities"/>.
        /// </summary>
        /// <returns>Data structure with the most recent performance data.</returns>
        public override PerformanceDataRecord Update()
        {
            return provider.Update();
        }

        /// <summary>
        /// An abstract class to be implemented by providers of this subsystem.
        /// </summary>
        public abstract class APProvider : SubsystemProvider<AdaptivePerformanceSubsystem>
        {
            /// <summary>
            /// Returns if the provider is currently running.
            /// </summary>
            protected new bool m_Running;

            /// <summary>
            /// Bitset of supported features.
            /// Does not change after startup.
            /// </summary>
            /// <value>Bitset</value>
            public abstract Feature Capabilities { get; set; }

            /// <summary>
            /// To be called once per frame.
            /// The returned data structure's fields are populated with the latest available data, according to the supported <see cref="Capabilities"/>.
            /// </summary>
            /// <returns>Data structure with the most recent performance data.</returns>
            public abstract PerformanceDataRecord Update();

            /// <summary>
            /// Application lifecycle events to be consumed by subsystem.
            /// Can be null if the subsystem does not need special handling on life-cycle events.
            /// The returned reference does not change after startup.
            /// </summary>
            /// <value>Application lifecycle object</value>
            public abstract IApplicationLifecycle ApplicationLifecycle { get; }

            /// <summary>
            /// Control CPU or GPU performance levels of the device.
            /// Can be null if the subsystem does not support controlling CPU/GPU performance levels.
            /// Is null when the <see cref="Feature.PerformanceLevelControl"/> bit is not set in <see cref="Capabilities"/>.
            /// The returned reference does not change after startup.
            /// </summary>
            /// <value>Performance level control object</value>
            public abstract IDevicePerformanceLevelControl PerformanceLevelControl { get; }

            /// <summary>
            /// Returns the version of the subsystem implementation.
            /// Can be used together with SubsystemDescriptor to identify a subsystem.
            /// </summary>
            /// <value>Version number</value>
            public abstract Version Version { get; }

            /// <summary>
            /// Generates a human readable string of subsystem internal stats.
            /// Optional and only used for development.
            /// </summary>
            /// <value>String with subsystem specific statistics</value>
            public virtual string Stats { get { return ""; } }

            /// <summary>
            /// Returns if the subsystem is initialized successfully.
            /// </summary>
            /// <value>Boolean to tell if subsystem was initialized successfully.</value>
            public abstract bool Initialized { get; set; }

            /// <summary>
            /// Returns if the subsystem is running.
            /// </summary>
            /// <value>Boolean to tell if subsystem is running.</value>
            public new bool running
            {
                get { return m_Running; }
            }
        }
    }
}
