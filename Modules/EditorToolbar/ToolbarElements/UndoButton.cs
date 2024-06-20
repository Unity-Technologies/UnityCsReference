// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Editor Utility/Undo", typeof(DefaultMainToolbar))]
    sealed class UndoButton : EditorToolbarButton
    {
        public UndoButton() : base(OpenUndoHistoryWindow)
        {
            name = "History";
            
            this.Q<Image>(className: EditorToolbar.elementIconClassName).style.display = DisplayStyle.Flex;

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
                var searchShortcut = ShortcutManagement.ShortcutManager.instance.GetShortcutBinding("Main Menu/Edit/Undo History");
                return L10n.Tr($"Undo History ({searchShortcut})");
            }
            catch
            {
                return L10n.Tr($"Undo History");
            }
        }

        static void OpenUndoHistoryWindow()
        {
            UndoHistoryWindow.OpenUndoHistory();
        }
    }
}
