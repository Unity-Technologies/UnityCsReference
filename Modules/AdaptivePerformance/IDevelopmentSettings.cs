// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Use the developer settings interface to access and change settings which are available only in development mode.
    /// </summary>
    public interface IDevelopmentSettings
    {
        /// <summary>
        /// Returns true if logging was enabled in StartupSettings.
        /// </summary>
        bool Logging { get; set; }

        /// <summary>
        /// Adjust the frequency in frames at which the application logs frame statistics to the console.
        /// This is only relevant when logging is enabled.
        /// </summary>
        int LoggingFrequencyInFrames { get; set; }
    }
}
