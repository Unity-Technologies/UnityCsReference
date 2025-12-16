// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.AdaptivePerformance.Provider;
using Provider = UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.SubsystemsImplementation;

namespace UnityEditor.AdaptivePerformance.Simulator.Editor
{
    /// <summary>
    /// The subsystem is used for simulating Adaptive Performance in the Editor with the <see href="https://docs.unity3d.com/Manual/DeviceSimulator.html">Device Simulator</see>.
    /// It is also used for Adaptive Performance tests and to simulate Adaptive Performance when it is not available on the hardware you work with.
    /// </summary>
    public class SimulatorAdaptivePerformanceSubsystem : AdaptivePerformanceSubsystem
    {
        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.ChangeFlags"/>.
        /// </summary>
        public Feature ChangeFlags
        {
            get { return ((SimulatorProvider)provider).ChangeFlags; }
            set { ((SimulatorProvider)provider).ChangeFlags = value; }
        }

        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.TemperatureLevel"/>.
        /// </summary>
        public float TemperatureLevel
        {
            get { return ((SimulatorProvider)provider).TemperatureLevel; }
            set { ((SimulatorProvider)provider).TemperatureLevel = value;}
        }

        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.TemperatureTrend"/>.
        /// </summary>
        public float TemperatureTrend
        {
            get { return ((SimulatorProvider)provider).TemperatureTrend; }
            set { ((SimulatorProvider)provider).TemperatureTrend = value; }
        }

        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.WarningLevel"/>.
        /// </summary>
        public WarningLevel WarningLevel
        {
            get { return ((SimulatorProvider)provider).WarningLevel; }
            set { ((SimulatorProvider)provider).WarningLevel = value; }
        }

        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.CpuPerformanceLevel"/>.
        /// </summary>
        public int CpuPerformanceLevel
        {
            get { return ((SimulatorProvider)provider).CpuPerformanceLevel; }
            set { ((SimulatorProvider)provider).CpuPerformanceLevel = value; }
        }

        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.GpuPerformanceLevel"/>.
        /// </summary>
        public int GpuPerformanceLevel
        {
            get { return ((SimulatorProvider)provider).GpuPerformanceLevel; }
            set { ((SimulatorProvider)provider).GpuPerformanceLevel = value; }
        }

        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.CpuPerformanceBoost"/>.
        /// </summary>
        public bool CpuPerformanceBoost
        {
            get { return ((SimulatorProvider)provider).CpuPerformanceBoost; }
            set { ((SimulatorProvider)provider).CpuPerformanceBoost = value; }
        }

        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.GpuPerformanceBoost"/>.
        /// </summary>
        public bool GpuPerformanceBoost
        {
            get { return ((SimulatorProvider)provider).GpuPerformanceBoost; }
            set { ((SimulatorProvider)provider).GpuPerformanceBoost = value; }
        }

        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.GpuFrameTime"/>.
        /// </summary>
        public float NextGpuFrameTime
        {
            get { return ((SimulatorProvider)provider).NextGpuFrameTime; }
            set { ((SimulatorProvider)provider).NextGpuFrameTime = value; }
        }

        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.CpuFrameTime"/>.
        /// </summary>
        public float NextCpuFrameTime
        {
            get { return ((SimulatorProvider)provider).NextCpuFrameTime; }
            set { ((SimulatorProvider)provider).NextCpuFrameTime = value; }
        }

        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.OverallFrameTime"/>.
        /// </summary>
        public float NextOverallFrameTime
        {
            get { return ((SimulatorProvider)provider).NextOverallFrameTime; }
            set { ((SimulatorProvider)provider).NextOverallFrameTime = value; }
        }

        /// <summary>
        /// Required to simulate performance changes. To change AutomaticPerformanceControl, you have to set AcceptsPerformanceLevel to `true`. See <see cref="PerformanceDataRecord.PerformanceLevelControlAvailable"/>.
        /// </summary>
        public bool AcceptsPerformanceLevel
        {
            get { return ((SimulatorProvider)provider).AcceptsPerformanceLevel; }
            set { ((SimulatorProvider)provider).AcceptsPerformanceLevel = value; }
        }

        /// <summary>
        /// Helper for the device simulator to change cluster info settings. Those settings are usually changed by a device directly.
        /// </summary>
        /// <param name="clusterInfo">New Cluster Info values.</param>
        public void SetClusterInfo(ClusterInfo clusterInfo)
        {
            ((SimulatorProvider)provider).SetClusterInfo(clusterInfo);
        }

        /// <summary>
        /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.PerformanceMode"/>.
        /// </summary>
        public PerformanceMode PerformanceMode
        {
            get { return ((SimulatorProvider)provider).PerformanceMode; }
            set { ((SimulatorProvider)provider).PerformanceMode = value; }
        }

        /// <summary>
        /// The Simulator Provider controls Subsystems needed to access Adaptive Performance features and the systems lifecycle.
        /// </summary>
        public class SimulatorProvider : APProvider, IApplicationLifecycle, IDevicePerformanceLevelControl
        {
            PerformanceDataRecord updateResult = new PerformanceDataRecord();
            float resetCpuBoostMode = 0;
            float resetGpuBoostMode = 0;

            /// <summary>
            /// Main constructor, used to initialize the provider capabilities.
            /// </summary>
            public SimulatorProvider()
            {
                Capabilities = Feature.CpuPerformanceLevel | Feature.GpuPerformanceLevel | Feature.PerformanceLevelControl |
                    Feature.TemperatureLevel | Feature.WarningLevel | Feature.TemperatureTrend | Feature.CpuFrameTime | Feature.GpuFrameTime |
                    Feature.OverallFrameTime | Feature.CpuPerformanceBoost | Feature.GpuPerformanceBoost | Feature.ClusterInfo | Feature.PerformanceMode;
                updateResult.PerformanceLevelControlAvailable = true;
                updateResult.ClusterInfo = new ClusterInfo { BigCore = 1, MediumCore = 3, LittleCore = 4 }; // define as useful numbers for the device simulator (and to not throw off tests)
                updateResult.ChangeFlags |= Provider.Feature.ClusterInfo;
            }

            /// <summary>
            /// Returns the capabilities of the provider.
            /// </summary>
            public override Feature Capabilities { get; set; }

            /// <summary>
            /// Returns the application lifecycle.
            /// </summary>
            public override IApplicationLifecycle ApplicationLifecycle => this;

            /// <summary>
            /// Returns the performance level control.
            /// </summary>
            public override IDevicePerformanceLevelControl PerformanceLevelControl => this;

            /// <summary>
            /// Returns the stats of the provider.
            /// </summary>
            public override string Stats => "Simulator provider";

            /// <summary>
            /// Returns the initialization status of the system.
            /// </summary>
            public override bool Initialized { get; set; }

            /// <summary>
            /// Perform initialization of the subsystem.
            /// </summary>
            public override void Start()
            {
                m_Running = true;
            }

            /// <summary>
            /// Stop running the subsystem.
            /// </summary>
            public override void Stop()
            {
                m_Running = false;
            }

            /// <summary>
            /// Cleanup when the subsystem object is destroyed.
            /// </summary>
            public override void Destroy()
            {
                Initialized = false;
            }

            /// <summary>
            /// Update current results and flags.
            /// </summary>
            /// <returns>The latest PerformanceDataRecord object.</returns>
            public override PerformanceDataRecord Update()
            {
                // Boost mode disables setPerformanceLevel and only lasts 15 seconds
                if ((int)resetCpuBoostMode == (int)Time.realtimeSinceStartup) // use int to avoid high precision of float
                {
                    resetCpuBoostMode = 0;
                    updateResult.PerformanceLevelControlAvailable = true;
                    updateResult.ChangeFlags |= Provider.Feature.PerformanceLevelControl;
                    updateResult.CpuPerformanceBoost = false;
                    updateResult.ChangeFlags |= Provider.Feature.CpuPerformanceBoost;
                }
                if ((int)resetGpuBoostMode == (int)Time.realtimeSinceStartup)
                {
                    resetGpuBoostMode = 0;
                    updateResult.PerformanceLevelControlAvailable = true;
                    updateResult.ChangeFlags |= Provider.Feature.PerformanceLevelControl;
                    updateResult.GpuPerformanceBoost = false;
                    updateResult.ChangeFlags |= Provider.Feature.GpuPerformanceBoost;
                }


                updateResult.ChangeFlags &= Capabilities;
                var result = updateResult;
                updateResult.ChangeFlags = Feature.None;
                return result;
            }

            /// <summary>
            /// Callback that is called when the application goes into a pause state.
            /// </summary>
            public void ApplicationPause()
            {
            }

            /// <summary>
            /// Callback that is called when the application resumes after being paused.
            /// </summary>
            public void ApplicationResume()
            {
            }

            /// <summary>
            /// Set the performance level for both the CPU and GPU.
            /// </summary>
            /// <param name="cpuLevel">The CPU performance level to request.</param>
            /// <param name="gpuLevel">The GPU performance level to request.</param>
            /// <returns>Returns if the levels were successfully set.</returns>
            public bool SetPerformanceLevel(ref int cpuLevel, ref int gpuLevel)
            {
                if (!updateResult.PerformanceLevelControlAvailable)
                {
                    updateResult.CpuPerformanceLevel = Constants.UnknownPerformanceLevel;
                    updateResult.ChangeFlags |= Provider.Feature.CpuPerformanceLevel;
                    updateResult.GpuPerformanceLevel = Constants.UnknownPerformanceLevel;
                    updateResult.ChangeFlags |= Provider.Feature.GpuPerformanceLevel;
                    return false;
                }

                return cpuLevel >= 0 && gpuLevel >= 0 && cpuLevel <= MaxCpuPerformanceLevel && gpuLevel <= MaxGpuPerformanceLevel;
            }

            /// <summary>
            /// Enable the boost mode for the CPU.
            /// </summary>
            /// <returns>Returns if CPU boost mode was successfully enabled.</returns>
            public bool EnableCpuBoost()
            {
                // Boost mode disables setPerformanceLevel
                updateResult.PerformanceLevelControlAvailable = false;
                updateResult.ChangeFlags |= Provider.Feature.PerformanceLevelControl;
                resetCpuBoostMode = Time.realtimeSinceStartup + 15;

                updateResult.CpuPerformanceBoost = true;
                updateResult.ChangeFlags |= Provider.Feature.CpuPerformanceBoost;
                return true;
            }

            /// <summary>
            /// Enable the boost mode for the GPU.
            /// </summary>
            /// <returns>Returns if GPU boost mode was successfully enabled.</returns>
            public bool EnableGpuBoost()
            {
                // Boost mode disables setPerformanceLevel
                updateResult.PerformanceLevelControlAvailable = false;
                updateResult.ChangeFlags |= Provider.Feature.PerformanceLevelControl;
                resetGpuBoostMode = Time.realtimeSinceStartup + 15;

                updateResult.GpuPerformanceBoost = true;
                updateResult.ChangeFlags |= Provider.Feature.GpuPerformanceBoost;
                return true;
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.ChangeFlags"/>.
            /// </summary>
            public virtual Feature ChangeFlags
            {
                get { return updateResult.ChangeFlags; }
                set { updateResult.ChangeFlags = value; }
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.TemperatureLevel"/>.
            /// </summary>
            public virtual float TemperatureLevel
            {
                get { return updateResult.TemperatureLevel; }
                set
                {
                    updateResult.TemperatureLevel = value;
                    updateResult.ChangeFlags |= Provider.Feature.TemperatureLevel;
                }
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.TemperatureTrend"/>.
            /// </summary>
            public virtual float TemperatureTrend
            {
                get { return updateResult.TemperatureTrend; }
                set
                {
                    updateResult.TemperatureTrend = value;
                    updateResult.ChangeFlags |= Provider.Feature.TemperatureTrend;
                }
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.WarningLevel"/>.
            /// </summary>
            public virtual WarningLevel WarningLevel
            {
                get { return updateResult.WarningLevel; }
                set
                {
                    updateResult.WarningLevel = value;
                    updateResult.ChangeFlags |= Provider.Feature.WarningLevel;
                }
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.CpuPerformanceLevel"/>.
            /// </summary>
            public virtual int CpuPerformanceLevel
            {
                get { return updateResult.CpuPerformanceLevel; }
                set
                {
                    updateResult.CpuPerformanceLevel = value;
                    updateResult.ChangeFlags |= Provider.Feature.CpuPerformanceLevel;
                }
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.GpuPerformanceLevel"/>.
            /// </summary>
            public virtual int GpuPerformanceLevel
            {
                get { return updateResult.GpuPerformanceLevel; }
                set
                {
                    updateResult.GpuPerformanceLevel = value;
                    updateResult.ChangeFlags |= Provider.Feature.GpuPerformanceLevel;
                }
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.CpuPerformanceBoost"/>.
            /// </summary>
            public virtual bool CpuPerformanceBoost
            {
                get { return updateResult.CpuPerformanceBoost; }
                set
                {
                    updateResult.CpuPerformanceBoost = value;
                    updateResult.ChangeFlags |= Provider.Feature.CpuPerformanceBoost;
                }
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.GpuPerformanceBoost"/>.
            /// </summary>
            public virtual bool GpuPerformanceBoost
            {
                get { return updateResult.GpuPerformanceBoost; }
                set
                {
                    updateResult.GpuPerformanceBoost = value;
                    updateResult.ChangeFlags |= Provider.Feature.GpuPerformanceBoost;
                }
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.GpuFrameTime"/>.
            /// </summary>
            public virtual float NextGpuFrameTime
            {
                get { return updateResult.GpuFrameTime; }
                set
                {
                    updateResult.GpuFrameTime = value;
                    updateResult.ChangeFlags |= Provider.Feature.GpuFrameTime;
                }
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.CpuFrameTime"/>.
            /// </summary>
            public virtual float NextCpuFrameTime
            {
                get { return updateResult.CpuFrameTime; }
                set
                {
                    updateResult.CpuFrameTime = value;
                    updateResult.ChangeFlags |= Provider.Feature.CpuFrameTime;
                }
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.OverallFrameTime"/>.
            /// </summary>
            public virtual float NextOverallFrameTime
            {
                get { return updateResult.OverallFrameTime; }
                set
                {
                    updateResult.OverallFrameTime = value;
                    updateResult.ChangeFlags |= Provider.Feature.OverallFrameTime;
                }
            }

            /// <summary>
            /// Required to simulate performance changes. To change AutomaticPerformanceControl, you have to set AcceptsPerformanceLevel to `true`. See <see cref="PerformanceDataRecord.PerformanceLevelControlAvailable"/>.
            /// </summary>
            public virtual bool AcceptsPerformanceLevel
            {
                get { return updateResult.PerformanceLevelControlAvailable; }
                set
                {
                    updateResult.PerformanceLevelControlAvailable = value;
                    updateResult.ChangeFlags |= Provider.Feature.PerformanceLevelControl;
                }
            }

            /// <summary>
            /// Helper for the device simulator to change cluster info settings. Those settings are usually changed by a device directly.
            /// </summary>
            /// <param name="clusterInfo">New Cluster Info values.</param>
            public virtual void SetClusterInfo(ClusterInfo clusterInfo)
            {
                updateResult.ClusterInfo = clusterInfo;
                updateResult.ChangeFlags |= Provider.Feature.ClusterInfo;
            }

            /// <summary>
            /// This property is a wrapper around an internal PerformanceDataRecord object. For more details, see <see cref="PerformanceDataRecord.PerformanceMode"/>.
            /// </summary>
            public virtual PerformanceMode PerformanceMode
            {
                get { return updateResult.PerformanceMode; }
                set
                {
                    updateResult.PerformanceMode = value;
                    updateResult.ChangeFlags |= Provider.Feature.PerformanceMode;
                }
            }

            /// <summary>
            /// The current version of the Device Simulator Adaptive Performance Subsystem. Matches the version of the Adaptive Performance Subsystem. See <see cref="AdaptivePerformanceSubsystem.Version"/>.
            /// </summary>
            public override Version Version { get { return new Version(6, 0, 0); } }

            /// <summary>
            /// See <see cref="IDevicePerformanceLevelControl.MaxCpuPerformanceLevel"/>.
            /// </summary>
            public int MaxCpuPerformanceLevel { get { return 6; } }

            /// <summary>
            /// See <see cref="IDevicePerformanceLevelControl.MaxGpuPerformanceLevel"/>.
            /// </summary>
            public int MaxGpuPerformanceLevel { get { return 4; } }

            /// <summary>
            /// Simulator subsystem try to initialize initializes successfully.
            /// </summary>
            /// <returns>true if initialized</returns>
            protected internal override bool TryInitialize()
            {
                Initialized = true;

                return Initialized;
            }
        }
    }

    // Class needed to register Descriptor
    internal class SimulatorProviderDescriptorRegistration
    {
        /// <summary>
        /// Register the subsystem with the subsystem registry and make it available to use during runtime.
        /// </summary>
        /// <returns>Returns an active subsystem descriptor.</returns>
        [RequiredByNativeCode(optional: true)]
        static AdaptivePerformanceSubsystemDescriptor RegisterDescriptor()
        {
            return AdaptivePerformanceSubsystemDescriptor.RegisterDescriptor(new AdaptivePerformanceSubsystemDescriptor.Cinfo
            {
                id = "SimulatorAdaptivePerformanceSubsystem",
                providerType = typeof(SimulatorAdaptivePerformanceSubsystem.SimulatorProvider),
                subsystemTypeOverride = typeof(SimulatorAdaptivePerformanceSubsystem)
            });
        }
    }
}
