// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: SceneTooling not yet converted
namespace UnityEditor.Toolbars
{
    static class UndoButton
    {
        const string k_Path = "Editor Controls/Undo";

        static UndoButton()
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
                var searchShortcut = ShortcutManagement.ShortcutManager.instance.GetShortcutBinding("Main Menu/Window/General/Undo History");
                return L10n.Tr($"Undo History ({searchShortcut})", null);
            }
            catch
            {
                return L10n.Tr($"Undo History", null);
            }
        }

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement(k_Path, defaultDockIndex = 4, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement CreateButton()
        {
            return new MainToolbarButton(new MainToolbarContent(EditorGUIUtility.LoadIcon("Icons/UndoHistory.png"), GetTooltipText()), UndoHistoryWindow.OpenUndoHistory);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
