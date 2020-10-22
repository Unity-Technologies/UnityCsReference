// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;
using UnityEngine;
using System;

namespace UnityEditor.Search
{
    [EditorToolbarElement("Editor Utility/Search", typeof(DefaultMainToolbar))]
    sealed class SearchButton : ToolbarButton
    {
        const string k_CommandName = "OpenQuickSearch";

        public SearchButton()
        {
            tooltip = GetTooltipText();
            clicked += () => CommandService.Execute(k_CommandName);
            style.display = CommandService.Exists(k_CommandName) ? DisplayStyle.Flex : DisplayStyle.None;

            var icon = new VisualElement();
            icon.style.backgroundImage = new StyleBackground(EditorGUIUtility.FindTexture("Search Icon"));
            icon.AddToClassList(EditorToolbar.elementIconClassName);
            Add(icon);

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            ShortcutManagement.ShortcutManager.instance.shortcutBindingChanged += UpdateTooltip;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ShortcutManagement.ShortcutManager.instance.shortcutBindingChanged -= UpdateTooltip;
        }

        private void UpdateTooltip(ShortcutManagement.ShortcutBindingChangedEventArgs obj)
        {
            tooltip = GetTooltipText();
        }

        private string GetTooltipText()
        {
            var searchShortcut = ShortcutManagement.ShortcutManager.instance.GetShortcutBinding("Main Menu/Edit/Search All...");
            return L10n.Tr($"Global Search ({searchShortcut})");
        }
    }
}
