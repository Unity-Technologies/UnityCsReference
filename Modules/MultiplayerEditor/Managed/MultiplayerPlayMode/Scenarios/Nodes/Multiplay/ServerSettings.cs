// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    struct ServerSettings
    {
        internal enum ServerDeployMode
        {
            Local,
            Simulated,
            Remote
        }

        [SerializeField] internal ServerDeployMode DeployMode;
        [SerializeField] internal SimulatorSettings SimulatorSettings;
        [SerializeField] internal ServerCliSettings CliSettings;

        /// <summary>
        /// Returns a string representation of the given <see cref="ServerSettings.ServerDeployMode"/>
        /// for use in analytics events, specifically for multiplay usage
        /// </summary>
        /// <param name="deployMode">The server deploy mode</param>
        /// <returns>
        /// "MultiplaySimulated" if <c>Simulated</c>,
        /// "Multiplay" if <c>Remote</c>,
        /// "" if <c>Local</c> or unrecognized.
        /// </returns>
        internal static string GetUseMultiplayString(ServerDeployMode deployMode)
        {
            return deployMode switch
            {
                ServerDeployMode.Simulated => "MultiplaySimulated",
                ServerDeployMode.Remote => "Multiplay",
                ServerDeployMode.Local => string.Empty,
                _ => string.Empty
            };
        }
    }
}
