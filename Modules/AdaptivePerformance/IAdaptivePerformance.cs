// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.ComponentModel;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Constants used by Adaptive Performance.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The minimum temperature level.
        /// See <see cref="ThermalMetrics.TemperatureLevel"/>.
        /// </summary>
        /// <value>0.0</value>
        public const float MinTemperatureLevel = 0.0f;

        /// <summary>
        /// The maximum temperature level.
        /// See <see cref="ThermalMetrics.TemperatureLevel"/>.
        /// </summary>
        /// <value>1.0</value>
        public const float MaxTemperatureLevel = 1.0f;

        /// <summary>
        /// The minimum CPU level.
        /// Used by <see cref="IDevicePerformanceControl.CpuLevel"/> and <see cref="PerformanceMetrics.CurrentCpuLevel"/>.
        /// </summary>
        /// <value>0</value>
        public const int MinCpuPerformanceLevel = 0;

        /// <summary>
        /// The minimum GPU level.
        /// Used by <see cref="IDevicePerformanceControl.GpuLevel"/> and <see cref="PerformanceMetrics.CurrentGpuLevel"/>.
        /// </summary>
        /// <value>0</value>
        public const int MinGpuPerformanceLevel = 0;

        /// <summary>
        /// UnknownPerformanceLevel is the value of <see cref="IDevicePerformanceControl.GpuLevel"/>, <see cref="PerformanceMetrics.CurrentGpuLevel"/>,
        /// <see cref="IDevicePerformanceControl.CpuLevel"/>, and <see cref="PerformanceMetrics.CurrentCpuLevel"/> if the current performance level is unknown.
        /// This can happen when AdaptivePerformance is not supported or when the device is in throttling state (see <see cref="WarningLevel.Throttling"/>).
        /// </summary>
        /// <value>-1</value>
        public const int UnknownPerformanceLevel = -1;

        /// <summary>
        /// The number of past frames that are considered to calculate average frame times.
        /// </summary>
        /// <value>100</value>
        public const int DefaultAverageFrameCount = 100;
    }

    /// <summary>
    /// The main interface to access Adaptive Performance.
    /// None of the properties in this interface change after startup.
    /// This means the references returned by the properties may be cached by the user.
    /// </summary>
    public interface IAdaptivePerformance
    {
        /// <summary>
        /// Returns true if Adaptive Performance was initialized successfully, false otherwise.
        /// This means that Adaptive Performance is enabled in StartupSettings and the application runs on a device that supports Adaptive Performance.
        /// </summary>
        /// <value>True when Adaptive Performance is initialized, running and available, false otherwise.</value>
        bool Initialized { get; }

        /// <summary>
        /// Returns true if Adaptive Performance was initialized and is actively running, false otherwise.
        /// This means that Adaptive Performance is enabled in StartupSettings.
        /// </summary>
        /// <value>True when Adaptive Performance is initialized and available, false otherwise.</value>
        bool Active { get; }

        /// <summary>
        /// Access thermal status information of the device.
        /// </summary>
        /// <value>Interface to access thermal status information of the device.</value>
        IThermalStatus ThermalStatus { get; }

        /// <summary>
        /// Access performance status information of the device and your application.
        /// </summary>
        /// <value>Interface to access performance status information of the device and your application.</value>
        IPerformanceStatus PerformanceStatus { get; }

        /// <summary>
        /// Control CPU and GPU performance of the device.
        /// </summary>
        /// <value>Interface to control CPU and GPU performance levels of the device.</value>
        IDevicePerformanceControl DevicePerformanceControl { get; }

        /// <summary>
        /// Access performance mode status information of the device.
        /// </summary>
        /// <value>Interface to access performance mode status information of the device.</value>
        IPerformanceModeStatus PerformanceModeStatus { get; }

        /// <summary>
        /// Access to development (logging) settings.
        /// </summary>
        /// <value>Interface to control CPU and GPU performance levels of the device.</value>
        IDevelopmentSettings DevelopmentSettings { get; }

        /// <summary>
        /// Access to the Indexer system. See <see cref="AdaptivePerformanceIndexer"/>
        /// </summary>
        /// <value>Interface to scalers that are active and their associated settings.</value>
        AdaptivePerformanceIndexer Indexer { get; }

        /// <summary>
        /// Access to the Settings. See <see cref="IAdaptivePerformanceSettings"/>.
        /// </summary>
        /// <value>Interface to settings that are loaded from the provider settings object during startup.</value>
        IAdaptivePerformanceSettings Settings { get; }

        /// <summary>
        /// Access to the active Subsystem. See <see cref="Provider.AdaptivePerformanceSubsystem"/>.
        /// </summary>
        /// <value>Reference to active Subsystem.</value>
        Provider.AdaptivePerformanceSubsystem Subsystem { get; }

        /// <summary>
        /// List of supported Features by the loaded provider. See <see cref="Provider.Feature"/>.
        /// </summary>
        /// <param name="feature">The feature in question. See <see cref="Provider.Feature"/>.</param>
        /// <returns>True if the requested feature is supported, false otherwise.</returns>
        bool SupportedFeature(Provider.Feature feature);

        /// <summary>
        /// Initiates the initialization process for Adaptive Performance by attempting to initialize the loaders. When
        /// this completes successfully, <see cref="Initialized"/> will be <c>true</c>. Adaptive Performance can now be
        /// started by calling the <see cref="StartAdaptivePerformance"/> method.
        /// </summary>
        void InitializeAdaptivePerformance();

        /// <summary>
        /// Attempts to start Adaptive Performance by requesting the active loader and all subsystems to start. When
        /// this completes successfully, <see cref="Active"/> will be <c>true</c>.
        /// </summary>
        void StartAdaptivePerformance();

        /// <summary>
        /// Attempts to stop Adaptive Performance by requesting the active loader and all subsystems to stop. When
        /// this completes successfully, <see cref="Active"/> will be <c>false</c>.
        /// </summary>
        void StopAdaptivePerformance();

        /// <summary>
        /// Stops Adaptive Performance (if still running) and initiates the tear down process. When this completes
        /// successfully, <see cref="Initialized"/> will be <c>false</c>.
        /// </summary>
        void DeinitializeAdaptivePerformance();
    }

    /// <summary>
    /// Global access to the default Adaptive Performance interface and lifecycle management controls.
    /// </summary>
    /// <remarks>
    /// The `Holder` class provides a singleton pattern for accessing Adaptive Performance functionality.
    /// Use this class to manually control the Adaptive Performance lifecycle. You can also use it to access
    /// the main interface when **Initialize Adaptive Performance on Startup** is disabled in the Adaptive Performance settings.
    ///
    /// The <see cref="Holder.Instance"/> property becomes available after successful initialization and remains
    /// available until deinitialization completes.
    ///
    /// </remarks>
    /// <example>
    /// The following example shows how to manually initialize Adaptive Performance and access thermal status.
    /// <code lang="cs"><![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.AdaptivePerformance;
    ///
    /// public class AdaptivePerformanceExample : MonoBehaviour
    /// {
    ///     void Start()
    ///     {
    ///         // Initialize Adaptive Performance manually if Adaptive Performance isn't already initialized.
    ///         if (Holder.Instance == null)
    ///         {
    ///             Holder.Initialize();
    ///         }
    ///         
    ///         // Access thermal status after initialization.
    ///         IAdaptivePerformance adaptivePerformance = Holder.Instance;
    ///         if (adaptivePerformance != null && adaptivePerformance.Initialized)
    ///         {
    ///             IThermalStatus thermalStatus = adaptivePerformance.ThermalStatus;
    ///             // The TemperatureLevel is located inside the ThermalMetrics struct.
    ///             float currentTemperature = thermalStatus.ThermalMetrics.TemperatureLevel;
    ///             Debug.Log($"Temperature level: {currentTemperature}");
    ///         }
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    public static class Holder
    {
        static IAdaptivePerformance m_Instance;

        /// <summary>
        /// The default Adaptive Performance interface instance for accessing all Adaptive Performance functionality.
        /// </summary>
        /// <remarks>
        /// This property becomes available after calling <see cref="Initialize"/> and successful initialization.
        /// The instance remains available until <see cref="Deinitialize"/> is called.
        ///
        /// Check that this property isn't null and that <see cref="IAdaptivePerformance.Initialized"/>
        /// is true before using Adaptive Performance features.
        ///
        /// Setting this property triggers <see cref="LifecycleEventHandler"/> events when the value changes.
        /// </remarks>
        public static IAdaptivePerformance Instance
        {
            get { return m_Instance; }
            internal set
            {
                if(value == null)
                    LifecycleEventHandler?.Invoke(m_Instance, LifecycleChangeType.Destroyed);
                else
                    LifecycleEventHandler?.Invoke(value, LifecycleChangeType.Created);

                m_Instance = value;
            }
        }

        /// <summary>
        /// Initializes Adaptive Performance and makes the instance available for use.
        /// </summary>
        /// <remarks>
        /// Only use this method when **Initialize Adaptive Performance on Startup** is disabled in Adaptive Performance settings.
        ///
        /// This method initializes the Adaptive Performance provider infrastructure and then completes initialization on the resulting instance by calling
        /// <see cref="IAdaptivePerformance.InitializeAdaptivePerformance"/>.
        ///
        /// If the instance is already available, this method returns immediately without performing any operations.
        /// After successful initialization, the <see cref="Holder.Instance"/> property becomes available and you can use it to access
        /// Adaptive Performance functionality.
        /// </remarks>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// using UnityEngine;
        /// using UnityEngine.AdaptivePerformance;
        ///
        /// public class AdaptivePerformanceManager : MonoBehaviour
        /// {
        ///     void Start()
        ///     {
        ///         // Check if manual initialization is needed.
        ///         if (Holder.Instance == null)
        ///         {
        ///             Holder.Initialize();
        ///             // Verify initialization succeeded.
        ///             if (Holder.Instance != null && Holder.Instance.Initialized)
        ///             {
        ///                 Debug.Log("Adaptive Performance initialized successfully.");
        ///             }
        ///         }
        ///     }
        /// }
        /// ]]></code>
        /// </example>


        public static void Initialize()
        {
            if (Instance != null)
                return;

            AdaptivePerformanceInitializer.Initialize();

            if (Instance != null)
                Instance.InitializeAdaptivePerformance();
        }

        /// <summary>
        /// Stops Adaptive Performance and cleans up all associated resources and objects.
        /// </summary>
        /// <remarks>
        /// Use this method to perform a complete shutdown of the Adaptive Performance system. This method first stops
        /// any running Adaptive Performance operations by calling <see cref="IAdaptivePerformance.DeinitializeAdaptivePerformance"/>.
        /// It then cleans up the provider infrastructure and sets <see cref="Holder.Instance"/> to null.
        ///
        /// After this method completes successfully, all
        /// Adaptive Performance functionality is unavailable until <see cref="Initialize"/> is called again.
        ///
        /// It's safe to call this method even if Adaptive Performance isn't currently initialized.
        /// </remarks>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// using UnityEngine;
        /// using UnityEngine.AdaptivePerformance;
        ///
        /// public class AdaptivePerformanceManager : MonoBehaviour
        /// {
        ///     void OnApplicationPause(bool pauseStatus)
        ///     {
        ///         if (pauseStatus)
        ///         {
        ///             // Adaptive Performance state before deinitialization.
        ///             Debug.Log($"Adaptive Performance initialized: {Holder.Instance.Initialized}");
        ///
        ///             // Clean up Adaptive Performance when app is paused.
        ///             Holder.Deinitialize();
        ///             Debug.Log("Adaptive Performance deinitialized");
        ///
        ///             // Adaptive Performance state after deinitialization.
        ///             Debug.Log($"Adaptive Performance initialized: {Holder.Instance!=null && Holder.Instance.Initialized}");
        ///         }
        ///     }
        /// }
        /// ]]></code>
        /// </example>
        public static void Deinitialize()
        {
            if (Instance != null)
                Instance.DeinitializeAdaptivePerformance();

            AdaptivePerformanceInitializer.Deinitialize();
            Instance = null;
        }

        /// <summary>
        /// Occurs when the Adaptive Performance instance is created or destroyed during lifecycle changes.
        /// </summary>
        /// <remarks>
        /// This event fires automatically when the <see cref="Holder.Instance"/> property value changes. A <see cref="LifecycleChangeType.Created"/> event
        /// fires when a new <see cref="IAdaptivePerformance"/> instance is assigned, and a <see cref="LifecycleChangeType.Destroyed"/> event fires
        /// when the instance is set to null.
        ///
        /// Subscribe to this event to track when Adaptive Performance becomes available or unavailable. Doing so
        /// is useful for managing dependent systems or UI state that relies on Adaptive Performance.
        ///
        /// The event fires from the <see cref="Holder.Instance"/> property setter, so it occurs during <see cref="Holder.Initialize"/> and
        /// <see cref="Holder.Deinitialize"/> operations.
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.AdaptivePerformance;
        /// 
        /// public class AdaptivePerformanceTracker : MonoBehaviour
        /// {
        ///     void Start()
        ///     {
        ///         // Subscribe to lifecycle events.
        ///         Holder.LifecycleEventHandler += OnAdaptivePerformanceLifecycle;
        ///     }
        /// 
        ///     void OnDestroy()
        ///     {
        ///         // Unsubscribe to prevent memory leaks.
        ///         Holder.LifecycleEventHandler -= OnAdaptivePerformanceLifecycle;
        ///     }
        /// 
        ///     void OnAdaptivePerformanceLifecycle(IAdaptivePerformance instance, LifecycleChangeType changeType)
        ///     {
        ///         switch (changeType)
        ///         {
        ///             case LifecycleChangeType.Created:
        ///                 Debug.Log("Adaptive Performance is now available.");
        ///                 break;
        ///             case LifecycleChangeType.Destroyed:
        ///                 Debug.Log("Adaptive Performance is no longer available.");
        ///                 break;
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public static event LifecycleEventHandler LifecycleEventHandler;
    }

    /// <summary>
    /// Adaptive Performance lifecycle events which are sent when the lifecycle of <see cref="IAdaptivePerformance"/>
    /// is changed.
    /// </summary>
    /// <param name="instance"><see cref="IAdaptivePerformance"/> instance.</param>
    /// <param name="changeType">Type of lifecycle change on <c>instance</c></param>
    public delegate void LifecycleEventHandler(IAdaptivePerformance instance, LifecycleChangeType changeType);

    /// <summary>
    /// Types of Adaptive Performance lifecycle changes.
    /// </summary>
    public enum LifecycleChangeType
    {
        /// <summary>
        /// Adaptive Performance was created.
        /// </summary>
        Created,

        /// <summary>
        /// Adaptive Performance was destroyed.
        /// </summary>
        Destroyed
    }
}
