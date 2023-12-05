// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEditor.Toolbars;

namespace UnityEditor.Search
{
    [EditorToolbarElement("Editor Utility/Search", typeof(DefaultMainToolbar))]
    sealed class SearchButton : EditorToolbarButton
    {
        const string k_CommandName = "OpenQuickSearch";

        public SearchButton() : base(() => CommandService.Execute(k_CommandName))
        {
            icon = EditorGUIUtility.FindTexture("Search Icon");

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
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
            style.display = CommandService.Exists(k_CommandName) ? DisplayStyle.Flex : DisplayStyle.None;
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
            try
            {
                var searchShortcut = ShortcutManagement.ShortcutManager.instance.GetShortcutBinding("Main Menu/Edit/Search/Search All...");
                return L10n.Tr($"Global Search ({searchShortcut})");
            }
            catch
            {
                return L10n.Tr($"Global Search");
            }
        }
    }
}
