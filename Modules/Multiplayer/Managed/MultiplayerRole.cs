// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;

namespace UnityEngine.Multiplayer.Internal
{
    // The Multiplayer role the application will play: Client, Server or Client Host.
    // This will be used to select the content that is going to be included or stripped from the build.

    [VisibleToOtherModules]
    internal enum MultiplayerRole
    {
        Client = 0,
        Server = 1,
    }

    [VisibleToOtherModules]
    [Flags]
    internal enum MultiplayerRoleFlags
    {
        Client = 1 << MultiplayerRole.Client,
        Server = 1 << MultiplayerRole.Server,
        ClientAndServer = Client | Server,
    }
}
