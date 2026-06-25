// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;

[assembly: InternalsVisibleTo("Unity.AdaptivePerformance.Tests")]
[assembly: InternalsVisibleTo("Unity.AdaptivePerformance.Editor.Tests")]
namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Class to handle active loader and subsystem management for Adaptive Performance. This class is to be added as a
    /// ScriptableObject asset in your project and should only be referenced by an <see cref="AdaptivePerformanceGeneralSettings"/>
    /// instance for its use.
    ///
    /// Given a list of loaders, it will attempt to load each loader in the given order. Unity will use the first
    /// loader that is successful and ignore all remaining loaders. The successful loader
    /// is accessible through the <see cref="activeLoader"/> property on the manager.
    ///
    /// Depending on configuration, the <see cref="AdaptivePerformanceGeneralSettings"/> instance will automatically manage the active loader
    /// at the correct points in the application lifecycle. You can override certain points in the active loader lifecycle
    /// and manually manage them by toggling the <see cref="AdaptivePerformanceManagerSettings.automaticLoading"/> and <see cref="AdaptivePerformanceManagerSettings.automaticRunning"/>
    /// properties. Disabling <see cref="AdaptivePerformanceManagerSettings.automaticLoading"/> implies that you are responsible for the full lifecycle
    /// of the Adaptive Performance session normally handled by the <see cref="AdaptivePerformanceGeneralSettings"/> instance. Setting this to false also sets
    /// <see cref="AdaptivePerformanceManagerSettings.automaticRunning"/> to false.
    ///
    /// Disabling <see cref="AdaptivePerformanceManagerSettings.automaticRunning"/> only implies that you are responsible for starting and stopping
    /// the <see cref="activeLoader"/> through the <see cref="StartSubsystems"/> and <see cref="StopSubsystems"/> APIs.
    ///
    /// Unity executes atomatic lifecycle management as follows:
    ///
    /// * OnEnable calls <see cref="InitializeLoader"/> internally. The loader list will be iterated over and the first successful loader will be set as the active loader.
    /// * Start calls <see cref="StartSubsystems"/> internally. Ask the active loader to start all subsystems.
    /// * OnDisable calls <see cref="StopSubsystems"/> internally. Ask the active loader to stop all subsystems.
    /// * OnDestroy calls <see cref="DeinitializeLoader"/> internally. Deinitialize and remove the active loader.
    /// </summary>
    public sealed partial class AdaptivePerformanceManagerSettings : ScriptableObject
    {
        [HideInInspector]
        bool m_InitializationComplete = false;

        [SerializeField]
        [Tooltip("Determines if the Adaptive Performance Manager instance is responsible for creating and destroying the appropriate loader instance.")]
        bool m_AutomaticLoading = false;

        /// <summary>
        /// Get and set Automatic Loading state for this manager. When this is true, the manager will automatically call
        /// <see cref="InitializeLoader"/> and <see cref="DeinitializeLoader"/> for you. When false, <see cref="automaticRunning"/>
        /// is also set to false and remains that way. This means that disabling automatic loading disables all automatic behavior
        /// for the manager.
        /// </summary>
        public bool automaticLoading
        {
            get { return m_AutomaticLoading; }
            set { m_AutomaticLoading = value; }
        }

        [SerializeField]
        [Tooltip("Determines if the Adaptive Performance Manager instance is responsible for starting and stopping subsystems for the active loader instance.")]
        bool m_AutomaticRunning = false;

        /// <summary>
        /// Get and set the automatic running state for this manager. When this is true, the manager will call <see cref="StartSubsystems"/>
        /// and <see cref="StopSubsystems"/> APIs at appropriate times. When false, or when <see cref="automaticLoading"/> is false,
        /// it is up to the user of the manager to handle that same functionality.
        /// </summary>
        public bool automaticRunning
        {
            get { return m_AutomaticRunning; }
            set { m_AutomaticRunning = value; }
        }


        [SerializeField]
        [Tooltip("List of Adaptive Performance Loader instances arranged in desired load order.")]
        List<AdaptivePerformanceLoader> m_Loaders = new List<AdaptivePerformanceLoader>();

        /// <summary>
        /// List of loaders currently managed by this Adaptive Performance Manager instance.
        /// </summary>
        public List<AdaptivePerformanceLoader> loaders
        {
            get { return m_Loaders; }
            set { m_Loaders = value; }
        }


        /// <summary>
        /// Read-only boolean that is true if initialization is completed and false otherwise. Because initialization is
        /// handled as a Coroutine, applications that use the auto-lifecycle management of AdaptivePerformanceManager
        /// will need to wait for init to complete before checking for an ActiveLoader and calling StartSubsystems.
        /// </summary>
        public bool isInitializationComplete
        {
            get { return m_InitializationComplete; }
        }

        [HideInInspector]
        [AutoStaticsCleanup]
        static AdaptivePerformanceLoader s_ActiveLoader = null;

        ///<summary>
        /// Returns the current singleton active loader instance.
        ///</summary>
        [HideInInspector]
        public AdaptivePerformanceLoader activeLoader { get { return s_ActiveLoader; } private set { s_ActiveLoader = value; } }

        /// <summary>
        /// Returns the current active loader, cast to the requested type. Useful shortcut when you need
        /// to get the active loader as something less generic than AdaptivePerformanceLoader.
        /// </summary>
        /// <typeparam name="T">Requested type of the loader.</typeparam>
        /// <returns>The active loader as requested type, or null if no active loader currently exists.</returns>
        public T ActiveLoaderAs<T>() where T : AdaptivePerformanceLoader
        {
            return activeLoader as T;
        }

        /// <summary>
        /// Iterate over the configured list of loaders and attempt to initialize each one. The first one
        /// that succeeds is set as the active loader and initialization immediately terminates.
        ///
        /// When this completes, <see cref="isInitializationComplete"/> will be set to true. This will mark that it is safe to
        /// call other parts of the API, but does not guarantee that init successfully created a loader. To check that init successfully created a loader,
        /// you need to check that ActiveLoader is not null.
        ///
        /// **Note**: There can only be one active loader. Any attempt to initialize a new active loader with one
        /// already set will cause a warning to be logged and immediate exit of this function.
        ///
        /// This method is synchronous and on return all state should be immediately checkable.
        /// </summary>
        internal void InitializeLoaderSync()
        {
            if (isInitializationComplete && activeLoader != null)
            {
                Debug.LogWarning(
                    "Adaptive Performance Management has already initialized an active loader in this scene." +
                    "Please make sure to stop all subsystems and deinitialize the active loader before initializing a new one.");
                return;
            }

            foreach (var loader in loaders)
            {
                if (loader != null)
                {
                    var settings = loader.GetSettings();
                    if (settings == null)
                        break;
                    if (loader.Initialize())
                    {
                        activeLoader = loader;
                        m_InitializationComplete = true;
                        return;
                    }
                }
            }

            activeLoader = null;
        }

        /// <summary>
        /// Iterate over the configured list of loaders and attempt to initialize each one. The first one
        /// that succeeds is set as the active loader and initialization immediately terminates.
        ///
        /// When complete, <see cref="isInitializationComplete"/> will be set to true. This will mark that it is safe to
        /// call other parts of the API, but does not guarantee that init successfully created a loader. To check that init successfully created a loader,
        /// you need to check that ActiveLoader is not null.
        ///
        /// **Note:** There can only be one active loader. Any attempt to initialize a new active loader with one
        /// already set will cause a warning to be logged and this function wil immeditely exit.
        ///
        /// Iteration is done asynchronously. You must call this method within the context of a Coroutine.
        /// </summary>
        ///
        /// <returns>Enumerator marking the next spot to continue execution at.</returns>
        internal IEnumerator InitializeLoader()
        {
            if (isInitializationComplete && activeLoader != null)
            {
                Debug.LogWarning(
                    "Adaptive Performance Management has already initialized an active loader in this scene." +
                    "Please make sure to stop all subsystems and deinitialize the active loader before initializing a new one.");
                yield break;
            }

            foreach (var loader in loaders)
            {
                if (loader != null)
                {
                    if (loader.Initialize())
                    {
                        activeLoader = loader;
                        m_InitializationComplete = true;
                        yield break;
                    }
                }

                yield return null;
            }

            activeLoader = null;
        }

        /// <summary>
        /// If there is an active loader, this will request the loader to start all the subsystems that it
        /// is managing.
        ///
        /// You must wait for <see cref="isInitializationComplete"/> to be set to true before calling this API.
        /// </summary>
        internal void StartSubsystems()
        {
            if (!m_InitializationComplete)
            {
                Debug.LogWarning(
                    "Call to StartSubsystems without an initialized manager." +
                    "Please make sure to wait for initialization to complete before calling this API.");
                return;
            }

            if (activeLoader == null)
                return;

            activeLoader.Start();
        }

        /// <summary>
        /// If there is an active loader, this will request the loader to stop all the subsystems that it
        /// is managing.
        ///
        /// You must wait for <see cref="isInitializationComplete"/> to be set to true before calling this API.
        /// </summary>
        internal void StopSubsystems()
        {
            if (!m_InitializationComplete)
            {
                Debug.LogWarning(
                    "Call to StopSubsystems without an initialized manager." +
                    "Please make sure to wait for initialization to complete before calling this API.");
                return;
            }

            if (activeLoader == null)
                return;

            activeLoader.Stop();
        }

        /// <summary>
        /// If there is an active loader, this function will deinitialize it and remove the active loader instance from
        /// management. Unity will automatically call <see cref="StopSubsystems"/> before deinitialization to make sure
        /// that things are cleaned up appropriately.
        ///
        /// You must wait for <see cref="isInitializationComplete"/> to be set to true before calling this API.
        ///
        /// On return, <see cref="isInitializationComplete"/> will be set to false.
        /// </summary>
        internal void DeinitializeLoader()
        {
            if (!m_InitializationComplete)
            {
                Debug.LogWarning(
                    "Call to DeinitializeLoader without an initialized manager." +
                    "Please make sure to wait for initialization to complete before calling this API.");
                return;
            }

            StopSubsystems();
            if (activeLoader != null)
            {
                activeLoader.Deinitialize();
                activeLoader = null;
            }

            m_InitializationComplete = false;
        }

        void OnDisable()
        {
            // should we have an OnEnable()?

            if (automaticLoading && automaticRunning)
            {
                StopSubsystems();
            }
        }

        void OnDestroy()
        {
            if (automaticLoading)
            {
                DeinitializeLoader();
            }
        }
    }
}
