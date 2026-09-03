// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: SceneTooling not yet converted
using UnityEditor.Toolbars;
using UnityEditor.SearchService;
using UnityEditor.Overlays;
using UnityEngine;

namespace UnityEditor.Search
{
    static class SearchButton
    {
        const string k_Path = "Editor Controls/Search";
        const string k_CommandName = "OpenQuickSearch";

        static SearchButton()
        {
            EditorApplication.delayCall += DelayInitialization;
        }

        static void DelayInitialization()
        {
            MainToolbar.Refresh(k_Path);
            ShortcutManagement.ShortcutManager.instance.shortcutBindingChanged += (args) => MainToolbar.Refresh(k_Path);
        }

        static string GetTooltipText()
        {
            try
            {
                var searchShortcut = ShortcutManagement.ShortcutManager.instance.GetShortcutBinding(OpenSearchHelper.k_SearchAllShortcutName);
                return L10n.Tr($"Global Search ({searchShortcut})", null);
            }
            catch
            {
                return L10n.Tr($"Global Search", null);
            }
        }

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement(k_Path, defaultDockIndex = 2, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement CreateButton()
        {
            return new MainToolbarButton(new MainToolbarContent("", EditorGUIUtility.FindTexture("Search Icon"), GetTooltipText()), () => CommandService.Execute(k_CommandName))
            {
                displayed = CommandService.Exists(k_CommandName)
            };
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
