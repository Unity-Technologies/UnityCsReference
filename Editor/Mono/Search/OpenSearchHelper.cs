// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Search not yet converted
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.SearchService
{
    static partial class OpenSearchHelper
    {
        static class Styles
        {
            public static readonly GUIContent gotoSearch = EditorGUIUtility.TrIconContent("SearchJump Icon");
        }

        internal const string k_SearchMenuName = "Edit/Search/Search All...";
        internal const string k_SearchAllShortcutName = $"Main Menu/{k_SearchMenuName}";
        public const string k_OpenSearchInContextCommand = "OpenQuickSearchInContext";

        [NoAutoStaticsCleanup]
        static ShortcutBinding s_ShortcutBinding = ShortcutBinding.empty;
        public static ShortcutBinding shortcutBinding
        {
            get
            {
                if (s_ShortcutBinding.Equals(ShortcutBinding.empty))
                {
                    UpdateBindingAndTooltip();
                }

                return s_ShortcutBinding;
            }
        }

        [OnCodeLoaded]
        static void DelayInitialize()
        {
            EditorApplication.delayCall += UpdateBindingAndTooltip;
        }

        static void OnShortcutBindingChanged(ShortcutBindingChangedEventArgs obj)
        {
            UpdateBindingAndTooltip();
        }

        static void UpdateBindingAndTooltip()
        {
            ShortcutManager.instance.shortcutBindingChanged -= OnShortcutBindingChanged;
            ShortcutManager.instance.shortcutBindingChanged += OnShortcutBindingChanged;
            if (!IsShortcutAvailable())
                return;
            s_ShortcutBinding = ShortcutManager.instance.GetShortcutBinding(k_SearchAllShortcutName);
            Styles.gotoSearch.tooltip = L10n.Tr($"Open in Search ({s_ShortcutBinding})", null);
        }

        public static object HandleSearchEvent(EditorWindow window, Event evt, string searchText)
        {
            if (evt.type != EventType.KeyDown)
                return null;

            var keyCombination = KeyCombination.FromKeyboardInput(evt);
#pragma warning disable UAC2006 // Avoid Linq
            if (shortcutBinding.keyCombinationSequence.Any(shortcutCombination => keyCombination.Equals(shortcutCombination)))
#pragma warning restore UAC2006
            {
                evt.Use();
                return OpenSearchInContext(window, searchText, "jumpShortcut");
            }

            return null;
        }

        public static object OpenSearchInContext(EditorWindow window, string searchText, string openContext)
        {
            if (!CommandService.Exists(k_OpenSearchInContextCommand))
                return null;

            var result = CommandService.Execute(k_OpenSearchInContextCommand, CommandHint.Any, searchText, openContext);
            window?.Repaint();
            return result;
        }

        public static void DrawOpenButton(EditorWindow window, string searchText)
        {
            Rect r = GUILayoutUtility.GetRect(Styles.gotoSearch, EditorStyles.toolbarSearchFieldJumpButton, GUILayout.MaxHeight(EditorGUI.kSingleLineHeight));
            if (EditorGUI.Button(r, Styles.gotoSearch, EditorStyles.toolbarSearchFieldJumpButton))
            {
                OpenSearchInContext(window, searchText, "jumpButton");
            }
        }

        public static bool IsShortcutAvailable()
        {
            var shortcutIds = ShortcutManager.instance.GetAvailableShortcutIds();
#pragma warning disable UAC2006 // Avoid Linq
            return shortcutIds.Any(path => path == k_SearchAllShortcutName);
#pragma warning restore UAC2006
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
