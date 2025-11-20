// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using System.Collections.Generic;
using UnityEngine.SubsystemsImplementation;

namespace UnityEngine.AdaptivePerformance.Provider
{
    internal static class AdaptivePerformanceSubsystemRegistry
    {
        /// <summary>
        /// Only for internal use.
        /// </summary>
        /// <param name="cinfo"></param>
        /// <returns></returns>
        public static AdaptivePerformanceSubsystemDescriptor RegisterDescriptor(AdaptivePerformanceSubsystemDescriptor.Cinfo cinfo)
        {
            var desc = new AdaptivePerformanceSubsystemDescriptor(cinfo);
            SubsystemDescriptorStore.RegisterDescriptor(desc);
            return desc;
        }

        /// <summary>
        /// Only for internal use.
        /// </summary>
        /// <returns></returns>
        public static List<AdaptivePerformanceSubsystemDescriptor> GetRegisteredDescriptors()
        {
            var perfDescriptors = new List<AdaptivePerformanceSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(perfDescriptors);
            return perfDescriptors;
        }
    }

    /// <summary>
    /// The Adaptive Performance Subsystem Descriptor is used for describing the subsystem so it can be picked up by the subsystem management system.
    /// </summary>
    public sealed class AdaptivePerformanceSubsystemDescriptor : SubsystemDescriptorWithProvider<AdaptivePerformanceSubsystem, AdaptivePerformanceSubsystem.APProvider>
    {
        /// <summary>
        /// Cinfo stores the ID and subsystem implementation type which is used to identify the subsystem during subsystem initialization.
        /// </summary>
        public struct Cinfo
        {
            /// <summary>
            /// The ID stores the name of the subsystem used to identify it in the subsystem registry.
            /// </summary>
            public string id { get; set; }

            /// <summary>
            /// Specifies the provider implementation type to use for instantiation.
            /// </summary>
            /// <value>
            /// The provider implementation type to use for instantiation.
            /// </value>
            public Type providerType { get; set; }

            /// <summary>
            /// Specifies the <c>AdaptivePerformanceSubsystem</c>-derived type that forwards casted calls to its provider.
            /// </summary>
            /// <value>
            /// The type of the subsystem to use for instantiation. If null, <c>Subsystem</c> will be instantiated.
            /// </value>
            public Type subsystemTypeOverride { get; set; }

            /// <summary>
            /// The subsystem implementation type stores the the type used for initialization in the subsystem registry.
            /// </summary>
            [Obsolete("AdaptivePerformanceSubsystem no longer supports the deprecated set of base classes for subsystems as of Unity 2023.1. Use providerType and, optionally, subsystemTypeOverride instead.", true)]
            public Type subsystemImplementationType { get; set; }
        }

        /// <summary>
        /// Constructor to fill the subsystem descriptor with all information to register the subsystem successfully.
        /// </summary>
        /// <param name="cinfo">Pass in the information about the subsystem.</param>
        public AdaptivePerformanceSubsystemDescriptor(Cinfo cinfo)
        {
            id = cinfo.id;
            providerType = cinfo.providerType;
            subsystemTypeOverride = cinfo.subsystemTypeOverride;
        }

        /// <summary>
        /// Register the subsystem with the subsystem registry and make it available to use during runtime.
        /// </summary>
        /// <param name="cinfo">Pass in the information about the subsystem.</param>
        /// <returns>Returns an active subsystem descriptor.</returns>
        public static AdaptivePerformanceSubsystemDescriptor RegisterDescriptor(Cinfo cinfo)
        {
            var registeredDescriptors = AdaptivePerformanceSubsystemRegistry.GetRegisteredDescriptors();
            foreach (var descriptor in registeredDescriptors)
            {
                // return the descriptor if already registered.
                if (descriptor.id == cinfo.id)
                {
                    return descriptor;
                }
            }
            return AdaptivePerformanceSubsystemRegistry.RegisterDescriptor(cinfo);
        }
    }
}
