// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.PlayMode.Editor
{
    /// <summary>
    /// Represents the different states of a play mode scenario.
    /// </summary>
    public enum PlayModeScenarioState
    {
        /// <summary>
        /// The current play mode scenario is not running.
        /// </summary>
        Idle,

        /// <summary>
        /// The current play mode scenario is starting.
        /// </summary>
        Starting,

        /// <summary>
        /// The current play mode scenario is running.
        /// </summary>
        Running,

        /// <summary>
        /// The current play mode scenario is stopping.
        /// </summary>
        Stopping,
    }
}
