// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.Scripting;

namespace UnityEngine.AdaptivePerformance.Basic
 {
     // Class needed to register Descriptor
     internal class BasicProviderDescriptorRegistration
     {
         [RequiredByNativeCode(optional: false)]
         [DynamicDependency("#ctor()", typeof(BasicAdaptivePerformanceSubsystem))]
         [DynamicDependency("#ctor()", typeof(BasicAdaptivePerformanceSubsystem.BasicProvider))]
         static AdaptivePerformanceSubsystemDescriptor RegisterDescriptor()
         {
             return AdaptivePerformanceSubsystemDescriptor.RegisterDescriptor(
                 new AdaptivePerformanceSubsystemDescriptor.Cinfo
                 {
                     id = "BasicAdaptivePerformanceSubsystem",
                     providerType = typeof(BasicAdaptivePerformanceSubsystem.BasicProvider),
                     subsystemTypeOverride = typeof(BasicAdaptivePerformanceSubsystem)
                 });
         }
     }

     internal class BasicAdaptivePerformanceSubsystem : AdaptivePerformanceSubsystem
     {
        internal class BasicProvider : APProvider, IApplicationLifecycle, IDevicePerformanceLevelControl
        {
            PerformanceDataRecord m_UpdatedPerfRecord;

            /// <summary>
            /// Main constructor, used to initialize the provider capabilities.
            /// </summary>
            public BasicProvider()
            {
                Capabilities = Feature.None;
                m_UpdatedPerfRecord.PerformanceLevelControlAvailable = false;
                m_UpdatedPerfRecord.CpuPerformanceBoost = false;
                m_UpdatedPerfRecord.GpuPerformanceBoost = false;
                m_UpdatedPerfRecord.TemperatureLevel = -1;
                m_UpdatedPerfRecord.TemperatureTrend = -1;
            }

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
            public override string Stats => "Basic provider";

            /// <summary>
            /// Returns the initialization status of the system.
            /// </summary>
            public override bool Initialized { get; set; }

            public override Feature Capabilities { get; set; }

            protected internal override bool TryInitialize()
            {
                Initialized = true;
                return Initialized;
            }

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
                m_UpdatedPerfRecord.ChangeFlags &= Capabilities;
                var result = m_UpdatedPerfRecord;
                m_UpdatedPerfRecord.ChangeFlags = Feature.None;
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
                if (!m_UpdatedPerfRecord.PerformanceLevelControlAvailable)
                {
                    m_UpdatedPerfRecord.CpuPerformanceLevel = Constants.UnknownPerformanceLevel;
                    m_UpdatedPerfRecord.ChangeFlags |= Provider.Feature.CpuPerformanceLevel;
                    m_UpdatedPerfRecord.GpuPerformanceLevel = Constants.UnknownPerformanceLevel;
                    m_UpdatedPerfRecord.ChangeFlags |= Provider.Feature.GpuPerformanceLevel;
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
                return false;
            }

            /// <summary>
            /// Enable the boost mode for the GPU.
            /// </summary>
            /// <returns>Returns if GPU boost mode was successfully enabled.</returns>
            public bool EnableGpuBoost()
            {
                // Boost mode disables setPerformanceLevel
                return false;
            }

            /// <summary>
            /// The current version of the Basic Adaptive Performance Subsystem. Matches the version of the Adaptive Performance Subsystem. See <see cref="AdaptivePerformanceSubsystem.Version"/>.
            /// </summary>
            public override Version Version { get { return new Version(6, 0, 0); } }

            /// <summary>
            /// See <see cref="IDevicePerformanceLevelControl.MaxCpuPerformanceLevel"/>.
            /// </summary>
            public int MaxCpuPerformanceLevel { get { return -1; } }

            /// <summary>
            /// See <see cref="IDevicePerformanceLevelControl.MaxGpuPerformanceLevel"/>.
            /// </summary>
            public int MaxGpuPerformanceLevel { get { return -1; } }
        }
     }
 }
