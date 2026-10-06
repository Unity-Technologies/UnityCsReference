// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// The operation mode that Adaptive Performance uses to prioritize how it adjusts application quality.
    /// </summary>
    /// <remarks>
    /// Each operation mode is backed by an <see cref="IAdaptivePerformanceModeProvider"/> that applies the scaler and performance actions for that mode.
    /// Use <see cref="IOperationModeStatus.CurrentOperationalModeProvider"/> to read or change the active mode provider at runtime.
    /// </remarks>
    public enum OperationMode
    {
        /// <summary>
        /// Prioritizes the highest achievable frame rate.
        /// </summary>
        NormalMode, // target the highest achievable framerate.
        /// <summary>
        /// Prioritizes battery life over performance.
        /// </summary>
        BatteryMode, // target battery duration
        /// <summary>
        /// Prioritizes a stable frame time by minimizing frame time variance.
        /// </summary>
        StableFrameMode, // target stable frame time (minimize variance).
        //DetailMode // target the highest quality.
    }

    /// <summary>
    /// Provides access to the active operation mode provider.
    /// </summary>
    public interface IOperationModeStatus
    {
        /// <summary>
        /// The mode provider that controls the active operation mode.
        /// </summary>
        /// <remarks>
        /// Set this property to switch the mode provider at runtime. Adaptive Performance calls <see cref="IAdaptivePerformanceModeProvider.OnOperationModeEnd"/> on the outgoing provider and <see cref="IAdaptivePerformanceModeProvider.OnOperationModeStart"/> on the incoming provider.
        /// </remarks>
        public IAdaptivePerformanceModeProvider CurrentOperationalModeProvider { get; set; }
    }
}
