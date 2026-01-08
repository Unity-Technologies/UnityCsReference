// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Toolbars;

namespace UnityEditor.Multiplayer.Internal
{
    class MultiplayerRoleDropdown : EditorToolbarDropdown
    {
        const string k_ElementPath = "Multiplayer/Multiplayer Role";
        
        public MultiplayerRoleDropdown()
        {
            EditorMultiplayerManager.CreateMultiplayerRoleDropdown(this);
        }
        
        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement(k_ElementPath, defaultDockIndex = 5, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement Create()
        {
            return new MainToolbarCustom(() => new MultiplayerRoleDropdown());
        }

        [InitializeOnLoadMethod]
        static void InitializeCallbacks()
        {
            EditorMultiplayerManager.enableMultiplayerRolesChanged += OnEnableMultiplayerRolesChanged;
        }

        [MainToolbarElementAvailability(k_ElementPath)]
        static bool IsAvailable()
        {
            return EditorMultiplayerManager.enableMultiplayerRoles;
        }

        static void OnEnableMultiplayerRolesChanged()
        {
            MainToolbar.Refresh(k_ElementPath);
        }   
    }
}
