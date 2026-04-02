// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

using InternalManager = UnityEngine.Multiplayer.Internal.MultiplayerManager;

namespace Unity.Multiplayer
{
    /// <summary>
    /// Provides an api for managing multiplayer roles in runtime.
    /// </summary>
    public static class MultiplayerRolesManager
    {
        /// <summary>
        /// Gets the active multiplayer role mask.
        /// </summary>
        public static MultiplayerRoleFlags ActiveMultiplayerRoleMask
            => InternalManager.activeMultiplayerRoleMask;

        /// <summary>
        /// Gets the multiplayer role mask for a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject.</param>
        /// <returns>Returns the multiplayer role mask for the provided GameObject.</returns>
        public static MultiplayerRoleFlags GetMultiplayerRoleMaskForGameObject(GameObject gameObject)
            => InternalManager.GetMultiplayerRoleMaskForGameObject(gameObject);

        /// <summary>
        /// Gets the multiplayer role mask for a Component.
        /// </summary>
        /// <param name="component">The Component.</param>
        /// <returns>Returns the multiplayer role mask for the provided Component.</returns>
        public static MultiplayerRoleFlags GetMultiplayerRoleMaskForComponent(Component component)
            => InternalManager.GetMultiplayerRoleMaskForComponent(component);
    }
}
