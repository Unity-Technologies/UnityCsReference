// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor.Toolbars
{
    static partial class SelectionHistoryButtons
    {
        const string k_ElementName = "Editor Controls/Selection History";

        [AutoStaticsCleanupOnCodeReload]
        static bool s_SubscribedToShortcutBindingChanged;

        [OnCodeLoaded]
        static void Initialize()
        {
            SelectionHistory.indexChanged += Refresh;
        }

        [OnCodeUnloading]
        static void Shutdown()
        {
            SelectionHistory.indexChanged -= Refresh;

            // Accessing ShortcutManager.instance creates the shortcut controller, so only do it if we subscribed.
            if (s_SubscribedToShortcutBindingChanged)
                ShortcutManager.instance.shortcutBindingChanged -= Refresh;
        }

        static void SubscribeToShortcutBindingChanged()
        {
            if (s_SubscribedToShortcutBindingChanged)
                return;

            ShortcutManager.instance.shortcutBindingChanged += Refresh;
            s_SubscribedToShortcutBindingChanged = true;
        }

        static void Refresh(ShortcutBindingChangedEventArgs _) => Refresh();
        static void Refresh() => MainToolbar.Refresh(k_ElementName);

        [MainToolbarElement(k_ElementName, defaultDockPosition = MainToolbarDockPosition.Right)]
        [UnityOnlyMainToolbarPreset]
        static IEnumerable<MainToolbarElement> CreateSelectionHistoryElement()
        {
            SubscribeToShortcutBindingChanged();

            yield return new MainToolbarButton(new MainToolbarContent(EditorGUIUtility.LoadIcon("tab_prev"), GetTooltip("Previous Selection")), SelectionHistory.instance.GoBack)
            {
                enabled = SelectionHistory.instance.GetIndex() < SelectionHistory.instance.GetHistory().Count - 1
            };

            yield return new MainToolbarButton(new MainToolbarContent(EditorGUIUtility.LoadIcon("tab_next"), GetTooltip("Next Selection")), SelectionHistory.instance.GoForward)
            {
                enabled = SelectionHistory.instance.GetIndex() > 0
            };
        }

        static string GetTooltip(string baseTooltip)
        {
            try
            {
                var shortcut = ShortcutManager.instance.GetShortcutBinding($"Main Menu/Edit/{baseTooltip}");
                return L10n.Tr($"{baseTooltip} ({shortcut})", null);
            }
            catch
            {
                return L10n.Tr(baseTooltip, null);
            }
        }
    }
}
