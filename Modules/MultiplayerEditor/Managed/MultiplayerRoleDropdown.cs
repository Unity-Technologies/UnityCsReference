// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;

namespace UnityEditor.Multiplayer.Internal
{
    [EditorToolbarElement("Multiplayer/MultiplayerRole", typeof(DefaultMainToolbar))]
    class MultiplayerRoleDropdown : ToolbarButton
    {
        public MultiplayerRoleDropdown()
        {
            EditorMultiplayerManager.CreateMultiplayerRoleDropdown(this);
        }
    }
}
