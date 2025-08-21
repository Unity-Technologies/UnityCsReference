// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Adaptive Performance Loader abstract class used as a base class for specific provider implementations. Providers should implement
    /// subclasses of this to provide specific initialization and management implementations that make sense for their supported
    /// scenarios and needs.
    /// </summary>
    public abstract class AdaptivePerformanceLoader : ScriptableObject
    {
        /// <summary>
        /// Returns if the provider loader was initialized successfully.
        /// </summary>
        public abstract bool Initialized { get; }

        /// <summary>
        /// Returns if the provider loader is currently running.
        /// </summary>
        public abstract bool Running { get; }

        /// <summary>
        /// Initialize the loader. This should initialize all subsystems to support the desired runtime setup this
        /// loader represents.
        /// </summary>
        ///
        /// <returns>Whether or not initialization succeeded.</returns>
        public virtual bool Initialize() { return true; }

        /// <summary>
        /// Ask loader to start all initialized subsystems.
        /// </summary>
        ///
        /// <returns>Whether or not all subsystems were successfully started.</returns>
        public virtual bool Start() { return true; }

        /// <summary>
        /// Ask loader to stop all initialized subsystems.
        /// </summary>
        ///
        /// <returns>Whether or not all subsystems were successfully stopped.</returns>
        public virtual bool Stop() { return true; }

        /// <summary>
        /// Ask loader to deinitialize all initialized subsystems.
        /// </summary>
        ///
        /// <returns>Whether or not deinitialization succeeded.</returns>
        public virtual bool Deinitialize() { return true; }

        /// <summary>
        /// Gets the loaded subsystem of the specified type. This is implementation-specific, because implementations contain data on
        /// what they have loaded and how best to get it.
        /// </summary>
        ///
        /// <typeparam name="T">Type of the subsystem to get.</typeparam>
        ///
        /// <returns>The loaded subsystem, or null if no subsystem found.</returns>
        public abstract T GetLoadedSubsystem<T>() where T : class, ISubsystem;

        /// <summary>
        /// Gets the loaded default subsystem.
        /// </summary>
        /// <returns>The loaded subsystem, or null if no default subsystem is loaded.</returns>
        public abstract ISubsystem GetDefaultSubsystem();

        /// <summary>
        /// Gets the Settings of the loader used to descibe the loader and subsystems.
        /// </summary>
        /// <returns>The settings of the loader.</returns>
        public abstract IAdaptivePerformanceSettings GetSettings();
    }
}
