// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Multiplayer.Center.Common
{
    /// <summary>
    /// The UI category the section will fall into.
    /// </summary>
    internal enum OnboardingSectionCategory
    {
        /// <summary>
        /// Comes at the top and should cover overarching topics for beginners
        /// </summary>
        Intro = 0,

        /// <summary>
        /// Section about the fundamentals of gameplay synchronization implementation and debugging.
        /// This includes netcode and tools related to netcode, as well as alternative solutions.
        /// </summary>
        Netcode = 1,

        /// <summary>
        /// Section gathering information about connecting players together, such as lobbies, voice chat and matchmaking.
        /// </summary>
        ConnectingPlayers = 2,

        /// <summary>
        /// Section gathering information about deploying, running and optimizing a game server.
        /// </summary>
        ServerInfrastructure = 3,

        /// <summary>
        /// Something else.
        /// </summary>
        Other = 4,

        /// <summary>
        /// LiveOps sections which are meant to be used after some development happened on the game.
        /// </summary>
        LiveOps = 5,

        /// <summary>
        /// The Getting Started section contains the basic sample for a selected <see cref="GameGenre"/>
        /// and a list of packages that would usually be installed for such genre.
        /// </summary>
        GettingStarted = 6,
    }
}
