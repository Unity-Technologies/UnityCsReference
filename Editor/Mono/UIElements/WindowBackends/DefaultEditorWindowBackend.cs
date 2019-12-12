// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements.Debugger;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    class DefaultEditorWindowBackend : DefaultWindowBackend
    {
        private IMGUIContainer m_NotificationContainer;

        // Cached version of the static color for the actual object instance...
        Color m_PlayModeDarkenColor;

        protected IEditorWindowModel editorWindowModel => m_Model as IEditorWindowModel;

        public override void OnCreate(IWindowModel model)
        {
            base.OnCreate(model);

            m_PlayModeDarkenColor = UIElementsUtility.editorPlayModeTintColor = EditorApplication.isPlayingOrWillChangePlaymode ? editorWindowModel.playModeTintColor : Color.white;

            EditorApplication.playModeStateChanged += PlayModeStateChangedCallback;
            AnimationMode.onAnimationRecordingStart += RefreshStylesAfterExternalEvent;
            AnimationMode.onAnimationRecordingStop += RefreshStylesAfterExternalEvent;

            m_NotificationContainer = new IMGUIContainer();
            m_NotificationContainer.StretchToParentSize();
            m_NotificationContainer.pickingMode = PickingMode.Ignore;

            editorWindowModel.notificationVisibilityChanged = NotificationVisibilityChanged;
            editorWindowModel.blurred = Blured;
            editorWindowModel.focused = Focused;
            editorWindowModel.playModeTintColorChanged = PlayModeTintColorChanged;

            if (editorWindowModel.window != null)
            {
                OnRegisterWindow();
                ViewMarginsChanged();
            }

            editorWindowModel.onRegisterWindow = OnRegisterWindow;
            editorWindowModel.onUnegisterWindow = OnUnegisterWindow;
            editorWindowModel.onDisplayWindowMenu = AddUIElementsDebuggerToMenu;
            editorWindowModel.viewMarginsChanged = ViewMarginsChanged;
        }

        private void ViewMarginsChanged()
        {
            RectOffset margins = editorWindowModel.viewMargins;

            IStyle style = editorWindowModel.window.rootVisualElement.style;
            style.top = margins.top;
            style.bottom = margins.bottom;
            style.left = margins.left;
            style.right = margins.right;
            style.position = Position.Absolute;
        }

        private void OnRegisterWindow()
        {
            EditorWindow window = editorWindowModel.window;
            // TODO delay this until root is first accessed
            m_Panel.visualTree.Add(window.rootVisualElement);
            m_Panel.getViewDataDictionary = window.GetViewDataDictionary;
            m_Panel.saveViewData = window.SaveViewData;
            m_Panel.name = window.GetType().Name;

            m_NotificationContainer.onGUIHandler = window.DrawNotification;
        }

        private void OnUnegisterWindow()
        {
            EditorWindow window = editorWindowModel.window;
            var root = window.rootVisualElement;
            if (root.hierarchy.parent == m_Panel.visualTree)
            {
                root.RemoveFromHierarchy();
                m_Panel.getViewDataDictionary = null;
                m_Panel.saveViewData = null;
            }
            m_NotificationContainer.onGUIHandler = null;
        }

        private void PlayModeTintColorChanged()
        {
            UpdatePlayModeColor(EditorApplication.isPlayingOrWillChangePlaymode
                ? editorWindowModel.playModeTintColor
                : Color.white);
        }

        public override void OnDestroy(IWindowModel model)
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChangedCallback;
            AnimationMode.onAnimationRecordingStart -= RefreshStylesAfterExternalEvent;
            AnimationMode.onAnimationRecordingStop -= RefreshStylesAfterExternalEvent;

            editorWindowModel.notificationVisibilityChanged = null;
            editorWindowModel.blurred = null;
            editorWindowModel.focused = null;
            editorWindowModel.playModeTintColorChanged = null;
            editorWindowModel.onRegisterWindow = null;
            editorWindowModel.onUnegisterWindow = null;
            editorWindowModel.onDisplayWindowMenu = null;
            editorWindowModel.viewMarginsChanged = null;

            m_NotificationContainer.onGUIHandler = null;
            m_NotificationContainer.RemoveFromHierarchy();

            base.OnDestroy(model);
        }

        private void NotificationVisibilityChanged()
        {
            if (editorWindowModel.notificationVisible)
            {
                if (m_NotificationContainer.parent == null)
                {
                    m_Panel.visualTree.Add(m_NotificationContainer);
                    m_NotificationContainer.StretchToParentSize();
                }
            }
            else
            {
                m_NotificationContainer.RemoveFromHierarchy();
            }
        }

        private void PlayModeStateChangedCallback(PlayModeStateChange state)
        {
            Color newColorToUse = Color.white;
            if ((state == PlayModeStateChange.ExitingEditMode) ||
                (state == PlayModeStateChange.EnteredPlayMode))
            {
                newColorToUse = editorWindowModel.playModeTintColor;
            }
            else if ((state == PlayModeStateChange.ExitingPlayMode) || (state == PlayModeStateChange.EnteredEditMode))
            {
                newColorToUse = Color.white;
            }
            UpdatePlayModeColor(newColorToUse);
        }

        void UpdatePlayModeColor(Color newColorToUse)
        {
            // Check the cached color to dirty only if needed !
            if (m_PlayModeDarkenColor != newColorToUse)
            {
                m_PlayModeDarkenColor = newColorToUse;
                UIElementsUtility.editorPlayModeTintColor = newColorToUse;

                // Make sure to dirty the right imgui container in this HostView (and all its children / parents)
                // The MarkDirtyRepaint() function is dirtying the element itself and its parent, but not the children explicitly.
                // ... and in the repaint function, it check for the current rendered element, not the parent.
                // Since the HostView "hosts" an IMGUIContainer or any VisualElement, we have to make sure to dirty everything here.
                PropagateDirtyRepaint(m_Panel.visualTree);
            }
        }

        static void PropagateDirtyRepaint(VisualElement ve)
        {
            ve.MarkDirtyRepaint();
            var count = ve.hierarchy.childCount;
            for (var i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];
                PropagateDirtyRepaint(child);
            }
        }

        private void Focused()
        {
            m_Panel.Focus();
        }

        private void Blured()
        {
            m_Panel.Blur();
        }

        protected void AddUIElementsDebuggerToMenu(GenericMenu menu)
        {
            var itemContent = UIElementsDebugger.WindowName;
            var shortcut = ShortcutIntegration.instance.directory.FindShortcutEntry(UIElementsDebugger.k_WindowPath);
            if (shortcut != null && shortcut.combinations.Any())
                itemContent += $" {KeyCombination.SequenceToMenuString(shortcut.combinations)}";

            menu.AddItem(EditorGUIUtility.TextContent(itemContent), false, DebugWindow, editorWindowModel.window);
        }

        private void DebugWindow(object userData)
        {
            EditorWindow window = userData as EditorWindow;
            if (window == null)
                return;

            if (CommandService.Exists(UIElementsDebugger.OpenWindowCommand))
                CommandService.Execute(UIElementsDebugger.OpenWindowCommand, CommandHint.Menu);
            else
            {
                UIElementsDebugger.OpenAndInspectWindow(window);
            }
        }

        private void RefreshStylesAfterExternalEvent()
        {
            var panel = m_Panel.visualTree.elementPanel;
            if (panel == null)
                return;

            var updater = panel.GetUpdater(VisualTreeUpdatePhase.Bindings) as VisualTreeBindingsUpdater;
            if (updater == null)
                return;

            updater.PollElementsWithBindings((e, b) => BindingExtensions.HandleStyleUpdate(e));
        }
    }
}
