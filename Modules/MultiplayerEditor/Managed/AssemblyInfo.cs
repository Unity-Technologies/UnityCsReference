// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Modules.Multiplayer.MultiplayerRoles.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.Modules.Multiplayer.MultiplayerRoles.Tests.Performance")]
[assembly: InternalsVisibleTo("Unity.Modules.Multiplayer.PlayMode.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.DedicatedServer.Editor")]
[assembly: InternalsVisibleTo("Unity.DedicatedServer.MultiplayerRoles.Editor")]
[assembly: InternalsVisibleTo("Unity.DedicatedServer.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.Center.Editor")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.Center.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.Playmode")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.PlayMode.Editor")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.Playmode.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.PlayMode.Services.Editor")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.Workflows.IntegrationTests.PlayMode.Editor")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.Workflows.Tests.Common.Editor")]
[assembly: InternalsVisibleTo("UnityEditor.QuickSearchModule")]

// Multiplayer Center's Common types are internal to this assembly; the quickstart package's
// integrations assembly is their one external consumer.
[assembly: InternalsVisibleTo("Unity.Multiplayer.Center.Integrations")]
[assembly: InternalsVisibleTo("Unity.Modules.Multiplayer.Center.Tests.Editor")]

// Early adopters of the extensibility API. Remove once API is public.
[assembly: InternalsVisibleTo("Unity.Services.CloudCode.PlayMode.Editor")]
