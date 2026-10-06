// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Identifies a device hardware subsystem that can report energy consumption through <see cref="EnergyUsage"/>.
    /// </summary>
    /// <remarks>
    /// Pass a value from this enum to <see cref="EnergyUsage.Get"/> to read how much energy a single subsystem consumed
    /// since energy-usage tracking was started or reset with <see cref="IEnergyUsageControl.StartEnergyUsageTracking"/>.
    ///
    /// Each subsystem reports independently of the others, because devices expose different sets of power monitors.
    /// A platform reports only the subsystems its monitors cover and returns an unavailable reading for the rest, so
    /// check <see cref="EnergyUsageReading.Available"/> on each reading before you use its values instead of assuming
    /// that a subsystem missing on one device is missing on all devices.
    /// </remarks>
    /// <example>
    /// The following example tracks energy usage while a component is enabled and logs how much energy the CPU
    /// subsystem consumed since tracking started.
    /// <code lang="cs"><![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.AdaptivePerformance;
    ///
    /// public class CpuEnergyUsageExample : MonoBehaviour
    /// {
    ///     IAdaptivePerformance adaptivePerformance;
    ///     IEnergyUsageControl energyUsageControl;
    ///
    ///     void OnEnable()
    ///     {
    ///         adaptivePerformance = Holder.Instance;
    ///         if (adaptivePerformance == null || !adaptivePerformance.Initialized)
    ///             return;
    ///
    ///         energyUsageControl = adaptivePerformance.EnergyUsageControl();
    ///         if (energyUsageControl == null || !energyUsageControl.StartEnergyUsageTracking())
    ///             Debug.Log("Energy-usage tracking isn't available on this device.");
    ///     }
    ///
    ///     void Update()
    ///     {
    ///         if (energyUsageControl == null || !energyUsageControl.EnergyUsageTrackingActive)
    ///             return;
    ///
    ///         EnergyUsage energyUsage = adaptivePerformance.PerformanceStatus.PerformanceMetrics.EnergyUsage;
    ///         EnergyUsageReading reading = energyUsage.Get(EnergyUsageSubsystem.Cpu);
    ///
    ///         // This device doesn't report energy for this subsystem, so skip it.
    ///         if (!reading.Available)
    ///             return;
    ///
    ///         Debug.Log($"The CPU used {reading.Energy} microwatt-seconds over {reading.Interval} ms.");
    ///     }
    ///
    ///     void OnDisable()
    ///     {
    ///         energyUsageControl?.StopEnergyUsageTracking();
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    public enum EnergyUsageSubsystem
    {
        /// <summary>
        /// The CPU subsystem.
        /// </summary>
        Cpu,

        /// <summary>
        /// The GPU subsystem.
        /// </summary>
        Gpu,

        /// <summary>
        /// The Tensor (neural) processing unit subsystem.
        /// </summary>
        Tpu,

        /// <summary>
        /// The display subsystem.
        /// </summary>
        Display,

        /// <summary>
        /// The memory (DRAM) subsystem.
        /// </summary>
        Memory,
    }

    /// <summary>
    /// The energy consumed by a single device subsystem since energy-usage tracking was started or reset,
    /// together with the time interval over which it was measured.
    /// </summary>
    /// <remarks>
    /// Get a reading for a subsystem with <see cref="EnergyUsage.Get"/>. Readings are cumulative rather than absolute:
    /// <see cref="Energy"/> covers the period that starts when tracking was started or reset with
    /// <see cref="IEnergyUsageControl.StartEnergyUsageTracking"/> and ends when the system last updated the reading,
    /// and <see cref="Interval"/> is the length of that same period.
    ///
    /// Check <see cref="Available"/> first, because it gates the other two values: when it's false, the device didn't
    /// report this subsystem and <see cref="Energy"/> and <see cref="Interval"/> are meaningless. Divide
    /// <see cref="Energy"/> by <see cref="Interval"/> to get the average power the subsystem drew over the period,
    /// in milliwatts.
    /// </remarks>
    /// <example>
    /// The following example tracks energy usage while a component is enabled and logs the average power the GPU
    /// subsystem drew since tracking started.
    /// <code lang="cs"><![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.AdaptivePerformance;
    ///
    /// public class GpuEnergyReadingExample : MonoBehaviour
    /// {
    ///     IAdaptivePerformance adaptivePerformance;
    ///     IEnergyUsageControl energyUsageControl;
    ///
    ///     void OnEnable()
    ///     {
    ///         adaptivePerformance = Holder.Instance;
    ///         if (adaptivePerformance == null || !adaptivePerformance.Initialized)
    ///             return;
    ///
    ///         energyUsageControl = adaptivePerformance.EnergyUsageControl();
    ///         if (energyUsageControl == null || !energyUsageControl.StartEnergyUsageTracking())
    ///             Debug.Log("Energy-usage tracking isn't available on this device.");
    ///     }
    ///
    ///     void Update()
    ///     {
    ///         if (energyUsageControl == null || !energyUsageControl.EnergyUsageTrackingActive)
    ///             return;
    ///
    ///         EnergyUsage energyUsage = adaptivePerformance.PerformanceStatus.PerformanceMetrics.EnergyUsage;
    ///         EnergyUsageReading reading = energyUsage.Get(EnergyUsageSubsystem.Gpu);
    ///
    ///         // Energy and Interval are only meaningful when the device reported this subsystem.
    ///         if (!reading.Available)
    ///             return;
    ///
    ///         // Microwatt-seconds divided by milliseconds gives average power in milliwatts.
    ///         float averagePower = (float)reading.Energy / reading.Interval;
    ///         Debug.Log($"The GPU averaged {averagePower} mW since tracking started.");
    ///     }
    ///
    ///     void OnDisable()
    ///     {
    ///         energyUsageControl?.StopEnergyUsageTracking();
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    /// <seealso cref="EnergyUsage"/>
    /// <seealso cref="EnergyUsageSubsystem"/>
    /// <seealso cref="IEnergyUsageControl"/>
    public readonly struct EnergyUsageReading
    {
        /// <summary>
        /// True if the device reported data for this subsystem in the current sample, false otherwise.
        /// When false, <see cref="Energy"/> and <see cref="Interval"/> are not meaningful.
        /// </summary>
        public bool Available { get; }

        /// <summary>
        /// The energy consumed in microwatt-seconds since energy-usage tracking was started or reset.
        /// </summary>
        public long Energy { get; }

        /// <summary>
        /// The measurement interval in milliseconds that <see cref="Energy"/> covers, measured from the
        /// start or last reset of energy-usage tracking until the system last updated the reading.
        /// </summary>
        public long Interval { get; }

        /// <summary>
        /// Initializes a new <see cref="EnergyUsageReading"/>.
        /// </summary>
        /// <param name="available">Whether the subsystem reported data.</param>
        /// <param name="energy">Energy consumed in microwatt-seconds.</param>
        /// <param name="interval">Measurement interval in milliseconds.</param>
        public EnergyUsageReading(bool available, long energy, long interval)
        {
            Available = available;
            Energy = energy;
            Interval = interval;
        }
    }

    /// <summary>
    /// A collection of cross-platform per-subsystem energy consumption readings, surfaced through
    /// <see cref="PerformanceMetrics.EnergyUsage"/>.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Get"/> with a <see cref="EnergyUsageSubsystem"/> value to read one subsystem's energy
    /// consumption since energy-usage tracking was started or reset with
    /// <see cref="IEnergyUsageControl.StartEnergyUsageTracking"/>.
    ///
    /// Individual readings can be unavailable independently of each other, so check
    /// <see cref="EnergyUsageReading.Available"/> on every reading you use. Values update at the platform's native
    /// cadence, which can be considerably slower than once per frame: on Android, readings refresh roughly every 20 to
    /// 30 seconds. Poll the readings periodically rather than expecting a new value each frame.
    /// </remarks>
    /// <example>
    /// The following example tracks energy usage while a component is enabled and logs the energy that the CPU and
    /// GPU subsystems consumed since tracking started.
    /// <code lang="cs"><![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.AdaptivePerformance;
    ///
    /// public class EnergyUsageExample : MonoBehaviour
    /// {
    ///     IAdaptivePerformance adaptivePerformance;
    ///     IEnergyUsageControl energyUsageControl;
    ///
    ///     void OnEnable()
    ///     {
    ///         adaptivePerformance = Holder.Instance;
    ///         if (adaptivePerformance == null || !adaptivePerformance.Initialized)
    ///             return;
    ///
    ///         energyUsageControl = adaptivePerformance.EnergyUsageControl();
    ///         if (energyUsageControl == null || !energyUsageControl.StartEnergyUsageTracking())
    ///             Debug.Log("Energy-usage tracking isn't available on this device.");
    ///     }
    ///
    ///     void Update()
    ///     {
    ///         if (energyUsageControl == null || !energyUsageControl.EnergyUsageTrackingActive)
    ///             return;
    ///
    ///         EnergyUsage energyUsage = adaptivePerformance.PerformanceStatus.PerformanceMetrics.EnergyUsage;
    ///         LogSubsystem(energyUsage, EnergyUsageSubsystem.Cpu);
    ///         LogSubsystem(energyUsage, EnergyUsageSubsystem.Gpu);
    ///     }
    ///
    ///     void LogSubsystem(EnergyUsage energyUsage, EnergyUsageSubsystem subsystem)
    ///     {
    ///         EnergyUsageReading reading = energyUsage.Get(subsystem);
    ///
    ///         // This device doesn't report energy for this subsystem, so skip it.
    ///         if (!reading.Available)
    ///             return;
    ///
    ///         Debug.Log($"{subsystem} used {reading.Energy} microwatt-seconds over {reading.Interval} ms.");
    ///     }
    ///
    ///     void OnDisable()
    ///     {
    ///         energyUsageControl?.StopEnergyUsageTracking();
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    /// <seealso cref="EnergyUsageReading"/>
    /// <seealso cref="EnergyUsageSubsystem"/>
    /// <seealso cref="IEnergyUsageControl"/>
    /// <seealso cref="PerformanceMetrics.EnergyUsage"/>
    public struct EnergyUsage
    {
        // Per-subsystem readings indexed by (int)EnergyUsageSubsystem. Lazily allocated by Set,
        // so adding a new EnergyUsageSubsystem value needs no change here.
        EnergyUsageReading[] m_Readings;

        // Pre-allocation hint for the number of EnergyUsageSubsystem values, so the common case where
        // every subsystem is set allocates the backing array once instead of resizing repeatedly.
        // Computed once so a newly added subsystem is picked up automatically. Correctness doesn't
        // depend on this being exact: Set uses Math.Max, so any value still works.
        static readonly int s_SubsystemCount = System.Enum.GetValues(typeof(EnergyUsageSubsystem)).Length;

        /// <summary>
        /// Initializes a new <see cref="EnergyUsage"/> with the given readings.
        /// </summary>
        /// <param name="readings">The readings to record, each paired with the subsystem it belongs to.</param>
        public EnergyUsage(params (EnergyUsageSubsystem subsystem, EnergyUsageReading reading)[] readings)
        {
            m_Readings = null;
            if (readings == null)
                return;

            foreach (var entry in readings)
                Set(entry.subsystem, entry.reading);
        }

        /// <summary>
        /// Returns the reading for the given subsystem, or a default (unavailable) reading if none has been recorded.
        /// </summary>
        /// <param name="subsystem">The device hardware subsystem to retrieve the energy reading for.</param>
        /// <returns>The reading for the requested subsystem.</returns>
        public EnergyUsageReading Get(EnergyUsageSubsystem subsystem)
        {
            int index = (int)subsystem;
            if (m_Readings == null || index < 0 || index >= m_Readings.Length)
                return default;
            return m_Readings[index];
        }

        /// <summary>
        /// Records the reading for the given subsystem.
        /// </summary>
        /// <param name="subsystem">The device hardware subsystem to record the energy reading for.</param>
        /// <param name="reading">The energy reading to store for the subsystem.</param>
        void Set(EnergyUsageSubsystem subsystem, EnergyUsageReading reading)
        {
            int index = (int)subsystem;
            if (index < 0)
                return;

            if (m_Readings == null)
                m_Readings = new EnergyUsageReading[System.Math.Max(index + 1, s_SubsystemCount)];
            else if (m_Readings.Length <= index)
                System.Array.Resize(ref m_Readings, index + 1);

            m_Readings[index] = reading;
        }
    }

    /// <summary>
    /// Use the energy-usage control interface to start, reset, and stop cross-platform per-subsystem energy tracking.
    /// While tracking is active, the latest readings are available through <see cref="PerformanceMetrics.EnergyUsage"/>.
    /// </summary>
    /// <remarks>
    /// Power-usage tracking reports energy consumed relative to the point at which tracking was started or reset,
    /// not an absolute value. Support depends on the active provider and the device (see <see cref="Provider.Feature.EnergyUsage"/>).
    /// </remarks>
    public interface IEnergyUsageControl
    {
        /// <summary>
        /// Returns true if the active provider and device support energy-usage tracking, otherwise returns false.
        /// </summary>
        /// <remarks>
        /// On some platforms whether the device actually exposes usable energy monitors is resolved
        /// asynchronously after <see cref="StartEnergyUsageTracking"/> is called, so this property can start
        /// out <c>true</c> and later become <c>false</c> (for example, when the device turns out to have no
        /// usable power monitors). Re-check it after starting rather than only once up front, and treat a
        /// transition to <c>false</c> as tracking no longer being available.
        /// </remarks>
        bool EnergyUsageTrackingSupported { get; }

        /// <summary>
        /// True while energy-usage tracking is active, false otherwise.
        /// </summary>
        bool EnergyUsageTrackingActive { get; }

        /// <summary>
        /// Starts energy-usage tracking, or resets the baseline if tracking is already active so that subsequent
        /// <see cref="PerformanceMetrics.EnergyUsage"/> readings are measured from now.
        /// </summary>
        /// <remarks>
        /// A <c>false</c> result has two causes: energy-usage tracking is not supported on this device, or
        /// Adaptive Performance is not currently active. In the latter case tracking can succeed once Adaptive
        /// Performance becomes active, so check <see cref="IAdaptivePerformance.Active"/> before giving up.
        /// </remarks>
        /// <returns>True if tracking was started or reset; false if energy-usage tracking is not supported on this
        /// device or Adaptive Performance is not active.</returns>
        bool StartEnergyUsageTracking();

        /// <summary>
        /// Stops energy-usage tracking. After calling this method, <see cref="PerformanceMetrics.EnergyUsage"/> readings
        /// remain unavailable until <see cref="StartEnergyUsageTracking"/> is called again.
        /// </summary>
        void StopEnergyUsageTracking();
    }

    /// <summary>
    /// Extension methods for <see cref="IAdaptivePerformance"/> related to energy-usage tracking.
    /// </summary>
    /// <remarks>
    /// Call <see cref="EnergyUsageControl"/> on an Adaptive Performance instance, such as
    /// <see cref="Holder.Instance"/>, to reach the <see cref="IEnergyUsageControl"/> interface that starts, resets,
    /// and stops energy-usage tracking.
    ///
    /// The extension returns null when the active Adaptive Performance implementation doesn't provide energy-usage
    /// tracking at all, so check the result before you use it. A non-null result doesn't guarantee that the device
    /// itself can report energy: check <see cref="IEnergyUsageControl.EnergyUsageTrackingSupported"/> as well.
    /// </remarks>
    /// <example>
    /// The following example acquires the energy-usage control, verifies that the device supports tracking, and
    /// tracks energy usage while a component is enabled.
    /// <code lang="cs"><![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.AdaptivePerformance;
    ///
    /// public class EnergyUsageControlExample : MonoBehaviour
    /// {
    ///     IEnergyUsageControl energyUsageControl;
    ///
    ///     void OnEnable()
    ///     {
    ///         IAdaptivePerformance adaptivePerformance = Holder.Instance;
    ///         if (adaptivePerformance == null || !adaptivePerformance.Initialized)
    ///             return;
    ///
    ///         // The result is null when this Adaptive Performance implementation has no energy-usage tracking.
    ///         energyUsageControl = adaptivePerformance.EnergyUsageControl();
    ///         if (energyUsageControl == null)
    ///         {
    ///             Debug.Log("This Adaptive Performance implementation doesn't provide energy-usage tracking.");
    ///             return;
    ///         }
    ///
    ///         // A non-null control doesn't guarantee that this device has usable power monitors.
    ///         if (!energyUsageControl.EnergyUsageTrackingSupported)
    ///         {
    ///             Debug.Log("This device doesn't support energy-usage tracking.");
    ///             return;
    ///         }
    ///
    ///         energyUsageControl.StartEnergyUsageTracking();
    ///     }
    ///
    ///     void OnDisable()
    ///     {
    ///         energyUsageControl?.StopEnergyUsageTracking();
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    /// <seealso cref="IAdaptivePerformance"/>
    /// <seealso cref="IEnergyUsageControl"/>
    /// <seealso cref="EnergyUsage"/>
    public static class AdaptivePerformanceEnergyUsageExtensions
    {
        /// <summary>
        /// Returns the energy-usage control for the given Adaptive Performance instance, or <c>null</c> if the
        /// active implementation does not support energy-usage tracking.
        /// </summary>
        /// <remarks>
        /// Exposed as an extension method rather than a member of <see cref="IAdaptivePerformance"/> so that it can
        /// be added without breaking existing external implementations of that interface.
        /// </remarks>
        /// <param name="adaptivePerformance">The Adaptive Performance instance to query.</param>
        /// <returns>The energy-usage control, or <c>null</c> when unsupported.</returns>
        public static IEnergyUsageControl EnergyUsageControl(this IAdaptivePerformance adaptivePerformance)
        {
            return adaptivePerformance as IEnergyUsageControl;
        }
    }
}
