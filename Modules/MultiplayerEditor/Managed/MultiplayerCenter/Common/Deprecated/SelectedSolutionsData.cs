// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.Center.Common
{
    /// <summary>
    /// Stores the selection of the main solutions from the recommendation tab.
    /// </summary>
    [Serializable]
    [Obsolete("Use MultiplayerCenterSettings for MultiplayerCenter project-wide settings")]
    internal class SelectedSolutionsData
    {
        /// <summary>
        /// The possible hosting models that the user can select.
        /// </summary>
        public enum HostingModel
        {
            /// <summary>
            /// Empty (no selection)
            /// </summary>
            None,

            /// <summary>
            /// Client hosted model
            /// </summary>
            ClientHosted,

            /// <summary>
            /// Dedicated server model
            /// </summary>
            DedicatedServer,

            /// <summary>
            /// Most of the logic will be in the cloud.
            /// </summary>
            CloudCode,

            /// <summary>
            /// Distributed Authority (the authority over the game logic is spread across multiple clients).
            /// </summary>
            DistributedAuthority,
        }

        /// <summary>
        /// The possible netcode solutions that the user can select.
        /// </summary>
        public enum NetcodeSolution
        {
            /// <summary>
            /// Empty (no selection)
            /// </summary>
            None,

            /// <summary>
            /// Netcode for GameObjects
            /// </summary>
            NGO,

            /// <summary>
            /// Netcode for Entities
            /// </summary>
            N4E,

            /// <summary>
            /// Custom netcode solution, potentially based on Unity Transport
            /// </summary>
            CustomNetcode,

            /// <summary>
            /// No netcode (no real time synchronization needed)
            /// </summary>
            NoNetcode
        }

        /// <summary>
        /// The hosting model selected by the user.
        /// </summary>
        public HostingModel SelectedHostingModel;

        /// <summary>
        /// The netcode solution selected by the user.
        /// </summary>
        public NetcodeSolution SelectedNetcodeSolution;
    }
}
