// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;

namespace Unity.Multiplayer
{
    /// <summary>
    /// The role of the application in a multiplayer game.
    /// </summary>
    public enum MultiplayerRole
    {
        /// <summary>
        /// Indicates that the application is running as a client.
        /// </summary>
        Client = 0,

        /// <summary>
        /// Indicates that the application is running as a dedicated server.
        /// </summary>
        Server = 1,
    }

    /// <summary>
    /// Flags for the role of the application in a multiplayer game.
    /// </summary>
    [Flags]
    public enum MultiplayerRoleFlags
    {
        /// <summary>
        /// Flag for the client role.
        /// </summary>
        Client = 1 << MultiplayerRole.Client,

        /// <summary>
        /// Flag for the server role.
        /// </summary>
        Server = 1 << MultiplayerRole.Server,

        /// <summary>
        /// Flag for both the client and server roles.
        /// </summary>
        ClientAndServer = Client | Server,
    }
}
