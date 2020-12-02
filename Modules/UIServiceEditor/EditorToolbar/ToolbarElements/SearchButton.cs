// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.Search
{
    [EditorToolbarElement("Editor Utility/Search", typeof(DefaultMainToolbar))]
    sealed class SearchButton : ToolbarButton
    {
        const string k_CommandName = "OpenQuickSearch";

        public SearchButton()
        {
            clicked += () => CommandService.Execute(k_CommandName);

            var icon = new VisualElement();
            icon.style.backgroundImage = new StyleBackground(EditorGUIUtility.FindTexture("Search Icon"));
            icon.AddToClassList(EditorToolbar.elementIconClassName);
            Add(icon);

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
                var searchShortcut = ShortcutManagement.ShortcutManager.instance.GetShortcutBinding("Main Menu/Edit/Search All...");
                return L10n.Tr($"Global Search ({searchShortcut})");
            }
            catch
            {
                return L10n.Tr($"Global Search");
            }
        }
    }
}
