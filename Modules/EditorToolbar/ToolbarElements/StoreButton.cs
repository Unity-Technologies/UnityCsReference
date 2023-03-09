// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define QUICK_SEARCH_STORE
using UnityEngine.UIElements;
using UnityEditor.Toolbars;
using UnityEditor.Connect;
using UnityEngine;

namespace UnityEditor.Search
{
    [EditorToolbarElement("Editor Utility/Store", typeof(DefaultMainToolbar))]
    sealed class StoreButton : EditorToolbarDropdown
    {
        const string k_SearchStoreCommand = "OpenSearchStore";
        const string k_OpenAssetStoreCommand = "OpenAssetStoreInBrowser";

        public StoreButton()
        {
            icon = EditorGUIUtility.FindTexture("AssetStore Icon");
            text = "Asset Store";

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            clicked += ActivateAssetStoreMenu;
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.delayCall += DelayInitialization;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ShortcutManagement.ShortcutManager.instance.shortcutBindingChanged -= UpdateTooltip;
        }

        private void DelayInitialization()
        {
            UpdateTooltip();
            style.display = CommandService.Exists(k_SearchStoreCommand) ? DisplayStyle.Flex : DisplayStyle.None;
            ShortcutManagement.ShortcutManager.instance.shortcutBindingChanged += UpdateTooltip;
        }

        private void UpdateTooltip()
        {
            tooltip = GetTooltipText();
        }

        private void UpdateTooltip(ShortcutManagement.ShortcutBindingChangedEventArgs obj)
        {
            UpdateTooltip();
        }

        private string GetTooltipText()
        {
            return L10n.Tr($"Asset Store");
        }

        private void ActivateAssetStoreMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("Asset Store Web"), false, () => CommandService.Execute(k_OpenAssetStoreCommand));
            menu.AddItem(EditorGUIUtility.TrTextContent("My Assets"), false, () => PackageManager.UI.PackageManagerWindow.SelectPackageAndPageStatic(pageId: PackageManager.UI.Internal.MyAssetsPage.k_Id));

            menu.DropDown(worldBound, true);
        }
    }
}
