// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Unity.Localization.Editor;

static class LanguageToolbar
{
    const string k_Path = "Localization/Language";

    // A project with no locales has nothing to preview, so the dropdown takes no toolbar space until it does.
    static bool HasLocales => LocalizationEditorSettings.ActiveSettings is { } settings && settings.AvailableLocales.Count > 0;

    /// <summary>
    /// Re-evaluates whether the toolbar dropdown has anything to show, after locales or settings change.
    /// </summary>
    internal static void Refresh() => MainToolbar.Refresh(k_Path);

    [MainToolbarElement(k_Path, defaultDockIndex = 40, defaultDockPosition = MainToolbarDockPosition.Left)]
    static MainToolbarElement Create()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        return new MainToolbarDropdown(new MainToolbarContent(CurrentLabel(), L10n.Tr("Preview locale", null)), ShowMenu)
        {
            displayed = HasLocales,
        };
    }

    static void OnSelectedLocaleChanged(Locale _) => MainToolbar.Refresh(k_Path);

    static string CurrentLabel()
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings == null)
            return L10n.Tr("No settings", null);
        var selected = LocalizationSettings.SelectedLocale ?? settings.ProjectLocale;
        return selected != null ? selected.LocaleName : L10n.Tr("None", null);
    }

    static void ShowMenu(Rect anchor)
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        var menu = new GenericMenu();
        var selected = LocalizationSettings.SelectedLocale;

        if (settings == null || settings.AvailableLocales.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent(L10n.Tr("No locales in the project", null)));
        }
        else
        {
            foreach (var locale in settings.AvailableLocales)
            {
                if (locale == null)
                    continue;
                var choice = locale;
                menu.AddItem(new GUIContent(locale.LocaleName), selected == choice, () => LocalizationSettings.SelectedLocale = choice);
            }
        }

        menu.AddSeparator(string.Empty);
        menu.AddItem(new GUIContent(L10n.Tr("Localization settings...", null)), false,
            () => SettingsService.OpenProjectSettings("Project/Localization"));
        menu.DropDown(anchor);
    }
}
