// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Toolbars;

namespace UnityEditor.Multiplayer.Internal
{
    class MultiplayerRoleDropdown : EditorToolbarDropdown
    {
        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement("Multiplayer/Multiplayer Role", true, defaultDockIndex = 5, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement Create()
        {
            return new MainToolbarCustom(() => new MultiplayerRoleDropdown());
        }

        public MultiplayerRoleDropdown()
        {
            EditorMultiplayerManager.CreateMultiplayerRoleDropdown(this);
        }
    }
}
