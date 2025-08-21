// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Adaptive Performance Loader abstract subclass used as a base class for specific provider implementations. Class provides some
    /// helper logic that can be used to handle subsystem handling in a typesafe manner, reducing potential boilerplate
    /// code.
    /// </summary>
    public abstract class AdaptivePerformanceLoaderHelper : AdaptivePerformanceLoader
    {
        /// <summary>
        /// Map of loaded susbsystems. Used so Unity doesn't always have to call AdaptivePerformanceManger and do a manual
        /// search to find the instance it loaded.
        /// </summary>
        protected Dictionary<Type, ISubsystem> m_SubsystemInstanceMap = new Dictionary<Type, ISubsystem>();

        /// <summary>
        /// Gets the loaded subsystem of the specified type. This is implementation-specific, because implementations contain data on
        /// what they have loaded and how best to get it.
        /// </summary>
        ///
        /// <typeparam name="T">Type of the subsystem to get.</typeparam>
        ///
        /// <returns>The loaded subsystem, or null if no subsystem found.</returns>
        public override T GetLoadedSubsystem<T>()
        {
            Type subsystemType = typeof(T);
            ISubsystem subsystem;
            m_SubsystemInstanceMap.TryGetValue(subsystemType, out subsystem);
            return subsystem as T;
        }

        /// <summary>
        /// Start a subsystem instance of a given type. Subsystem is assumed to already be loaded from
        /// a previous call to CreateSubsystem.
        /// </summary>
        /// <typeparam name="T">A subclass of <see cref="ISubsystem"/></typeparam>
        protected void StartSubsystem<T>() where T : class, ISubsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
                subsystem.Start();
        }

        /// <summary>
        /// Stop a subsystem instance of a given type. Subsystem is assumed to already be loaded from
        /// a previous call to CreateSubsystem.
        /// </summary>
        /// <typeparam name="T">A subclass of <see cref="ISubsystem"/></typeparam>
        protected void StopSubsystem<T>() where T : class, ISubsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
                subsystem.Stop();
        }

        /// <summary>
        /// Destroy a subsystem instance of a given type. Subsystem is assumed to already be loaded from
        /// a previous call to CreateSubsystem.
        /// </summary>
        /// <typeparam name="T">A subclass of <see cref="ISubsystem"/></typeparam>
        protected void DestroySubsystem<T>() where T : class, ISubsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
            {
                if (subsystem.running)
                    subsystem.Stop();

                var subsystemType = typeof(T);
                if (m_SubsystemInstanceMap.ContainsKey(subsystemType))
                    m_SubsystemInstanceMap.Remove(subsystemType);

                subsystem.Destroy();
            }
        }

        /// <summary>
        /// Creates a subsystem with a given list of descriptors and a specific subsystem id.
        /// </summary>
        /// <typeparam name="TDescriptor">The descriptor type being passed in.</typeparam>
        /// <typeparam name="TSubsystem">The subsystem type being requested.</typeparam>
        /// <param name="descriptors">List of TDescriptor instances to use for subsystem matching.</param>
        /// <param name="id">The identifier key of the particualr subsystem implementation being requested.</param>
        protected void CreateSubsystem<TDescriptor, TSubsystem>(List<TDescriptor> descriptors, string id)
            where TDescriptor : ISubsystemDescriptor
            where TSubsystem : ISubsystem
        {
            if (descriptors == null)
                throw new ArgumentNullException("descriptors");

            SubsystemManager.GetSubsystemDescriptors<TDescriptor>(descriptors);

            if (descriptors.Count > 0)
            {
                foreach (var descriptor in descriptors)
                {
                    ISubsystem subsys = null;
                    if (String.Compare(descriptor.id, id, true) == 0)
                    {
                        subsys = descriptor.Create();
                    }
                    if (subsys != null)
                    {
                        m_SubsystemInstanceMap[typeof(TSubsystem)] = subsys;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Override of <see cref="Deinitialize"/> to provide for clearing the instance map.
        ///
        /// If you override <see cref="Deinitialize"/> in your subclass, you must call the base
        /// implementation to allow the instance map tp be cleaned up correctly.
        /// </summary>
        ///
        /// <returns>True if de-initialization was successful.</returns>
        public override bool Deinitialize()
        {
            m_SubsystemInstanceMap.Clear();
            return base.Deinitialize();
        }
    }
}
