// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define QUICK_SEARCH_STORE
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.Search
{
    static class StoreButton
    {
        const string k_SearchStoreCommand = "OpenSearchStore";
        const string k_OpenAssetStoreCommand = "OpenAssetStoreInBrowser";
        static Texture2D s_Icon;

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement("Editor Utility/Store", true, defaultDockPosition = MainToolbarDockPosition.Left, defaultDockIndex = 10)]
        static MainToolbarElement QueryStoreButtonInfo()
        {
            return new MainToolbarDropdown(new MainToolbarContent("Asset Store", s_Icon, L10n.Tr($"Asset Store")), ActivateAssetStoreMenu)
            {
                displayed = CommandService.Exists(k_SearchStoreCommand)
            };
        }

        static StoreButton()
        {
            s_Icon = EditorGUIUtility.FindTexture("AssetStore Icon");
        }

        static void ActivateAssetStoreMenu(Rect worldBound)
        {
            var menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("Asset Store Web"), false, () => CommandService.Execute(k_OpenAssetStoreCommand));
            menu.AddItem(EditorGUIUtility.TrTextContent("My Assets"), false, () => PackageManager.UI.PackageManagerWindow.OpenAndSelectPage(PackageManager.UI.Internal.MyAssetsPage.k_Id));

            menu.DropDown(worldBound, true);
        }
    }
}
