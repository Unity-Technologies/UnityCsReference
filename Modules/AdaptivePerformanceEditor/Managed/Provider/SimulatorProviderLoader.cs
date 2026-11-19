// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.AdaptivePerformance;
using UnityEditor.AdaptivePerformance.Editor;
using UnityEngine.AdaptivePerformance.Provider;

namespace UnityEditor.AdaptivePerformance.Simulator.Editor
{
    /// <summary>
    /// SimulatorProviderLoader implements the loader for the Adaptive Performance Device Simulator plugin.
    /// </summary>
    [AdaptivePerformanceSupportedBuildTargetAttribute(BuildTargetGroup.Standalone)]
    public class SimulatorProviderLoader : AdaptivePerformanceLoaderHelper
    {
        [NoAutoStaticsCleanup] // populated by native subsystem registration, not C# state
        static List<AdaptivePerformanceSubsystemDescriptor> s_SimulatorSubsystemDescriptors =
            new List<AdaptivePerformanceSubsystemDescriptor>();

        /// <summary>
        /// Returns if the provider loader was initialized successfully.
        /// </summary>
        public override bool Initialized
        {
            get { return simulatorSubsystem != null; }
        }

        /// <summary>
        /// Returns if the provider loader is currently running.
        /// </summary>
        public override bool Running
        {
            get { return simulatorSubsystem != null && simulatorSubsystem.running; }
        }

        /// <summary>Returns the currently active Simulator Subsystem instance, if any.</summary>
        public SimulatorAdaptivePerformanceSubsystem simulatorSubsystem
        {
            get { return GetLoadedSubsystem<SimulatorAdaptivePerformanceSubsystem>(); }
        }

        /// <summary>
        /// Implementation of <see cref="AdaptivePerformanceLoader.GetDefaultSubsystem"/>
        /// </summary>
        /// <returns>The Simulator as currently loaded default subststem. Adaptive Performance always initializes the first subsystem and uses it as a default, because only one subsystem can be present at a given time. You can change subsystem order in the Adaptive Performance Provider Settings.</returns>
        public override ISubsystem GetDefaultSubsystem()
        {
            return simulatorSubsystem;
        }

        /// <summary>
        /// Implementation of <see cref="AdaptivePerformanceLoader.GetSettings"/>.
        /// </summary>
        /// <returns>Returns the Simulator settings.</returns>
        public override IAdaptivePerformanceSettings GetSettings()
        {
            return SimulatorProviderSettings.GetSettings();
        }

        /// <summary>Implementation of <see cref="AdaptivePerformanceLoader.Initialize"/>.</summary>
        /// <returns>True if successfully initialized the Simulator subsystem, false otherwise.</returns>
        public override bool Initialize()
        {
            CreateSubsystem<AdaptivePerformanceSubsystemDescriptor, SimulatorAdaptivePerformanceSubsystem>(s_SimulatorSubsystemDescriptors, "SimulatorAdaptivePerformanceSubsystem");
            if (simulatorSubsystem == null)
            {
                Debug.LogError("Unable to start the Simulator subsystem.");
            }

            return simulatorSubsystem != null;
        }

        /// <summary>Implementation of <see cref="AdaptivePerformanceLoader.Start"/>.</summary>
        /// <returns>True if successfully started the Simulator subsystem, false otherwise.</returns>
        public override bool Start()
        {
            StartSubsystem<SimulatorAdaptivePerformanceSubsystem>();
            return true;
        }

        /// <summary>Implementation of <see cref="AdaptivePerformanceLoader.Stop"/>.</summary>
        /// <returns>True if successfully stopped the Simulator subsystem, false otherwise</returns>
        public override bool Stop()
        {
            StopSubsystem<SimulatorAdaptivePerformanceSubsystem>();
            return true;
        }

        /// <summary>Implementation of <see cref="AdaptivePerformanceLoader.Deinitialize"/>.</summary>
        /// <returns>True if successfully deinitialized the Simulator subsystem, false otherwise.</returns>
        public override bool Deinitialize()
        {
            DestroySubsystem<SimulatorAdaptivePerformanceSubsystem>();
            return base.Deinitialize();
        }
    }
}
