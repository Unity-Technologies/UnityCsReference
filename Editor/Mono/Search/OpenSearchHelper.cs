// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.SearchService
{
    static class OpenSearchHelper
    {
        static class Styles
        {
            public static GUIContent gotoSearch = EditorGUIUtility.TrIconContent("SearchJump Icon");
        }

        internal const string k_SearchMenuName = "Edit/Search/Search All...";
        internal const string k_SearchAllShortcutName = $"Main Menu/{k_SearchMenuName}";
        public const string k_OpenSearchInContextCommand = "OpenQuickSearchInContext";

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

        static OpenSearchHelper()
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
            Styles.gotoSearch.tooltip = L10n.Tr($"Open in Search ({s_ShortcutBinding})");
        }

        public static void HandleSearchEvent(EditorWindow window, Event evt, string searchText)
        {
            if (evt.type != EventType.KeyDown)
                return;

            var keyCombination = KeyCombination.FromKeyboardInput(evt);
            if (shortcutBinding.keyCombinationSequence.Any(shortcutCombination => keyCombination.Equals(shortcutCombination)))
            {
                evt.Use();
                OpenSearchInContext(window, searchText, "jumpShortcut");
            }
        }

        public static void OpenSearchInContext(EditorWindow window, string searchText, string openContext)
        {
            if (!CommandService.Exists(k_OpenSearchInContextCommand))
                return;

            CommandService.Execute(k_OpenSearchInContextCommand, CommandHint.Any, searchText, openContext);
            window?.Repaint();
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
            return shortcutIds.Any(path => path == k_SearchAllShortcutName);
        }
    }
}
