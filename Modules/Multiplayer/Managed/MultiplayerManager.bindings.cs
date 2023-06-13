// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;

namespace UnityEngine.Multiplayer.Internal
{
    [NativeHeader("Modules/Multiplayer/MultiplayerManager.h")]
    [StaticAccessor("GetMultiplayerManager()", StaticAccessorType.Dot)]
    internal static class MultiplayerManager
    {
        public static extern MultiplayerRole activeMultiplayerRole { get; }

        public static extern MultiplayerRoleFlags GetMultiplayerRoleMaskForGameObject(GameObject gameObject);
        public static extern MultiplayerRoleFlags GetMultiplayerRoleMaskForComponent(Component component);
    }
}
