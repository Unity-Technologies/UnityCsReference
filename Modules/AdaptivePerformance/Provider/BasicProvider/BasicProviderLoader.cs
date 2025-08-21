// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AdaptivePerformance;
using UnityEngine.AdaptivePerformance.Basic;
using UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.Bindings;


namespace UnityEngine.AdaptivePerformance.Basic
{
    /// <summary>
    /// BasicProviderLoader implements the loader for the Adaptive Performance Device Basic plugin.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.AdaptivePerformanceModule")]
    internal class BasicProviderLoader : AdaptivePerformanceLoaderHelper
    {
        static List<AdaptivePerformanceSubsystemDescriptor> s_BasicSubsystemDescriptors =
            new List<AdaptivePerformanceSubsystemDescriptor>();

        /// <summary>
        /// Returns if the provider loader was initialized successfully.
        /// </summary>
        public override bool Initialized
        {
            get { return BasicSubsystem != null; }
        }

        /// <summary>
        /// Returns if the provider loader is currently running.
        /// </summary>
        public override bool Running
        {
            get { return BasicSubsystem != null && BasicSubsystem.running; }
        }

        /// <summary>Returns the currently active Basic Subsystem instance, if any.</summary>
        public BasicAdaptivePerformanceSubsystem BasicSubsystem
        {
            get { return GetLoadedSubsystem<BasicAdaptivePerformanceSubsystem>(); }
        }

        /// <summary>
        /// Implementation of <see cref="AdaptivePerformanceLoader.GetDefaultSubsystem"/>
        /// </summary>
        /// <returns>The Basic as currently loaded default subsystem. Adaptive Performance always initializes the first subsystem and uses it as a default, because only one subsystem can be present at a given time. You can change subsystem order in the Adaptive Performance Provider Settings.</returns>
        public override ISubsystem GetDefaultSubsystem()
        {
            return BasicSubsystem;
        }

        /// <summary>
        /// Implementation of <see cref="AdaptivePerformanceLoader.GetSettings"/>.
        /// </summary>
        /// <returns>Returns the Basic settings.</returns>
        public override IAdaptivePerformanceSettings GetSettings()
        {
            return BasicProviderSettings.GetSettings();
        }

        /// <summary>Implementation of <see cref="AdaptivePerformanceLoader.Initialize"/>.</summary>
        /// <returns>True if successfully initialized the Simulator subsystem, false otherwise.</returns>
        public override bool Initialize()
        {
            CreateSubsystem<AdaptivePerformanceSubsystemDescriptor, BasicAdaptivePerformanceSubsystem>(s_BasicSubsystemDescriptors, "BasicAdaptivePerformanceSubsystem");
            if (BasicSubsystem == null)
            {
                Debug.LogError("Unable to start the Basic subsystem.");
            }

            return BasicSubsystem != null;
        }

        /// <summary>Implementation of <see cref="AdaptivePerformanceLoader.Start"/>.</summary>
        /// <returns>True if successfully started the Basic subsystem, false otherwise.</returns>
        public override bool Start()
        {
            StartSubsystem<BasicAdaptivePerformanceSubsystem>();
            return true;
        }

        /// <summary>Implementation of <see cref="AdaptivePerformanceLoader.Stop"/>.</summary>
        /// <returns>True if successfully stopped the Basic subsystem, false otherwise</returns>
        public override bool Stop()
        {
            StopSubsystem<BasicAdaptivePerformanceSubsystem>();
            return true;
        }

        /// <summary>Implementation of <see cref="AdaptivePerformanceLoader.Deinitialize"/>.</summary>
        /// <returns>True if successfully deinitialized the Basic subsystem, false otherwise.</returns>
        public override bool Deinitialize()
        {
            DestroySubsystem<BasicAdaptivePerformanceSubsystem>();
            return base.Deinitialize();
        }
    }
}
