// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Actions;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.EditorTools
{
    [EditorToolContext, Icon(k_IconPath)]
    public sealed class GameObjectToolContext : EditorToolContext
    {
        const string k_IconPath = "GameObject Icon";
        GameObjectToolContext() {}

        public override void PopulateMenu(DropdownMenu menu)
        {
            ContextMenuUtility.AddGameObjectEntriesTo(menu);
        }
    }
}
