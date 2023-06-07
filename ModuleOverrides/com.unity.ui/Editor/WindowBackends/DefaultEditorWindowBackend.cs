// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements.Debugger;
using UnityEditor.UIElements.Experimental.UILayoutDebugger;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class DefaultEditorWindowBackend : DefaultWindowBackend, IEditorWindowBackend
    {
        private const string k_LiveReloadMenuText = "UI Toolkit Live Reload";
        private const string k_LiveReloadPreferenceKeySuffix = ".LiveReloadOn";
        private const string k_BindingLogLevelKeySuffix = ".DataBinding.LogLevel";
        private static string k_GameViewLiveReloadPreferenceKey = null;

        private IMGUIContainer m_NotificationContainer;
        private IMGUIContainer m_OverlayContainer;

        // Cached version of the static color for the actual object instance...
        Color m_PlayModeDarkenColor;

        protected IEditorWindowModel editorWindowModel => m_Model as IEditorWindowModel;

        private class EditorWindowVisualTreeAssetTracker : BaseLiveReloadVisualTreeAssetTracker
        {
            private DefaultEditorWindowBackend m_Owner;

            public EditorWindowVisualTreeAssetTracker(DefaultEditorWindowBackend owner)
            {
                m_Owner = owner;
            }

            internal override void OnVisualTreeAssetChanged()
            {
                if (m_Owner.editorWindowModel != null)
                    m_Owner.RecreateWindow();
            }
        }

        private EditorWindowVisualTreeAssetTracker m_LiveReloadVisualTreeAssetTracker = null;

        private string m_LiveReloadPreferenceKey;
        private string m_BindingLogLevelKey;

        public override void OnCreate(IWindowModel model)
        {
            try
            {
                base.OnCreate(model);

                m_LiveReloadVisualTreeAssetTracker = new EditorWindowVisualTreeAssetTracker(this);
                m_PlayModeDarkenColor = UIElementsUtility.editorPlayModeTintColor =
                    EditorApplication.isPlayingOrWillChangePlaymode ? editorWindowModel.playModeTintColor : Color.white;

                EditorApplication.playModeStateChanged += PlayModeStateChangedCallback;
                AnimationMode.onAnimationRecordingStart += RefreshStylesAfterExternalEvent;
                AnimationMode.onAnimationRecordingStop += RefreshStylesAfterExternalEvent;

                m_NotificationContainer = new IMGUIContainer();
                m_NotificationContainer.StretchToParentSize();
                m_NotificationContainer.pickingMode = PickingMode.Ignore;

                m_OverlayContainer = new IMGUIContainer();
                m_OverlayContainer.StretchToParentSize();
                m_OverlayContainer.pickingMode = PickingMode.Ignore;

                RegisterImguiContainerGUICallbacks();

                // Window is non-null when set by deserialization; it's usually null when OnCreate is called.
                if (editorWindowModel.window != null)
                {
                    RegisterWindow(true);
                }
            }
            catch (Exception e)
            {
                // Log error to easily diagnose issues with panel initialization and then rethrow it.
                Debug.LogException(e);
                throw;
            }
        }

        void IEditorWindowBackend.ViewMarginsChanged()
        {
            UpdateStyleMargins();
        }

        void UpdateStyleMargins()
        {
            RectOffset margins = editorWindowModel.viewMargins;
            IStyle style = editorWindowModel.window.baseRootVisualElement.style;
            style.top = margins.top;
            style.bottom = margins.bottom;
            style.left = margins.left;
            style.right = margins.right;
            style.position = Position.Absolute;
        }

        void IEditorWindowBackend.OnRegisterWindow()
        {
            RegisterWindow();
        }

        void IEditorWindowBackend.OnUnregisterWindow()
        {
            UnregisterWindow();
        }

        private bool m_WindowRegistered;
        void RegisterWindow(bool duringOnCreate = false)
        {
            if (m_WindowRegistered)
                return;

            EditorWindow window = editorWindowModel.window;

            // Live Reload is off by default for all Editor Windows, except for the ones overriding liveReloadPreferenceDefault (Game View, UI Builder)
            m_LiveReloadPreferenceKey = GetWindowLiveReloadPreferenceKey(editorWindowModel.window.GetType());
            m_Panel.enableAssetReload = EditorPrefs.GetBool(m_LiveReloadPreferenceKey, editorWindowModel.window.liveReloadPreferenceDefault);

            m_BindingLogLevelKey = GetWindowBindingLogLevelKey(editorWindowModel.window.GetType());
            Binding.SetPanelLogLevel(m_Panel, GetBindingLogLevel(m_BindingLogLevelKey));

            var root = window.baseRootVisualElement;
            m_Panel.liveReloadSystem.RegisterVisualTreeAssetTracker(m_LiveReloadVisualTreeAssetTracker, root);
            if (root.hierarchy.parent != m_Panel.visualTree)
            {
                AddRootElement(root);
            }

            m_Panel.getViewDataDictionary = window.GetViewDataDictionary;
            m_Panel.saveViewData = window.SaveViewData;
            m_Panel.name = window.GetType().Name;
            m_NotificationContainer.onGUIHandler = window.DrawNotification;
            m_OverlayContainer.onGUIHandler = () => m_OverlayGUIHandler?.Invoke();

            UpdateStyleMargins();
            m_WindowRegistered = true;

            SendInitializeIfNecessary(duringOnCreate);
        }

        void UnregisterWindow()
        {
            if (!m_WindowRegistered)
                return;

            var root = editorWindowModel.window.baseRootVisualElement;
            if (root.hierarchy.parent == m_Panel.visualTree)
            {
                RemoveRootElement(root);
                m_Panel.getViewDataDictionary = null;
                m_Panel.saveViewData = null;
            }

            editorWindowModel.window.ReleaseViewData();

            m_NotificationContainer.onGUIHandler = null;
            m_OverlayContainer.onGUIHandler = null;
            m_WindowRegistered = false;
        }

        const TrickleDown k_TricklePhase = TrickleDown.TrickleDown;

        private VisualElement m_RegisteredRoot;

        private void AddRootElement(VisualElement root)
        {
            m_Panel.visualTree.Add(root);
        }

        private void RemoveRootElement(VisualElement root)
        {
            root.RemoveFromHierarchy();
        }

        private void RegisterImguiContainerGUICallbacks()
        {
            var root = m_Panel.visualTree;
            root.RegisterCallback<MouseDownEvent>(SendEventToSplitterGUI, k_TricklePhase);
            root.RegisterCallback<MouseUpEvent>(SendEventToSplitterGUI, k_TricklePhase);
            root.RegisterCallback<MouseMoveEvent>(SendEventToSplitterGUI, k_TricklePhase);
            root.RegisterCallback<MouseUpEvent>(SendMouseUpOutsideWindowToDockArea);
            m_RegisteredRoot = root;
        }

        private void UnregisterImguiContainerGUICallbacks()
        {
            var root = m_Panel.visualTree;
            if (root == m_RegisteredRoot)
            {
                m_RegisteredRoot = null;
                root.UnregisterCallback<MouseDownEvent>(SendEventToSplitterGUI, k_TricklePhase);
                root.UnregisterCallback<MouseUpEvent>(SendEventToSplitterGUI, k_TricklePhase);
                root.UnregisterCallback<MouseMoveEvent>(SendEventToSplitterGUI, k_TricklePhase);
                root.UnregisterCallback<MouseUpEvent>(SendMouseUpOutsideWindowToDockArea);
            }
        }

        private void SendEventToSplitterGUI(EventBase ev)
        {
            if (ev.imguiEvent == null || ev.imguiEvent.rawType == EventType.Used || ev.target == imguiContainer)
                return;

            // This will only be called after OnCreate and before OnDestroy, so
            // we assume imguiContainer != null && editorWindowModel != null

            imguiContainer.HandleIMGUIEvent(ev.imguiEvent, editorWindowModel.onSplitterGUIHandler, false);

            if (ev.imguiEvent.rawType == EventType.Used)
                ev.StopPropagation();
        }

        private void SendMouseUpOutsideWindowToDockArea(MouseUpEvent ev)
        {
            // Fix for case 1306631 - a MouseUp event received outside of the GameView
            // is re-directed to the DockArea IMGUIContainer.

            if (ev.imguiEvent == null || ev.imguiEvent.rawType == EventType.Used || ev.target != visualTree)
                return;

            imguiContainer.HandleIMGUIEvent(ev.imguiEvent, false);

            if (ev.imguiEvent.rawType == EventType.Used)
                ev.StopPropagation();
        }

        void IEditorWindowBackend.PlayModeTintColorChanged()
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

            m_NotificationContainer.onGUIHandler = null;
            m_NotificationContainer.RemoveFromHierarchy();

            m_OverlayContainer.onGUIHandler = null;
            m_OverlayContainer.RemoveFromHierarchy();

            UnregisterImguiContainerGUICallbacks();
            UnregisterWindow();

            base.OnDestroy(model);
        }

        void IEditorWindowBackend.NotificationVisibilityChanged()
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

        private event Action m_OverlayGUIHandler;
        public event Action overlayGUIHandler
        {
            add
            {
                m_OverlayGUIHandler += value;
                OverlayChanged();
            }
            remove
            {
                m_OverlayGUIHandler -= value;
                OverlayChanged();
            }
        }

        private void OverlayChanged()
        {
            if (m_OverlayGUIHandler != null)
            {
                if (m_OverlayContainer.parent == null)
                {
                    m_Panel.visualTree.Add(m_OverlayContainer);
                    m_OverlayContainer.StretchToParentSize();
                }
            }
            else
            {
                m_OverlayContainer.RemoveFromHierarchy();
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

        private static readonly string k_InitializedWindowPropertyName = "Initialized";
        void SendInitializeIfNecessary(bool duringOnCreate = false)
        {
            if (editorWindowModel == null)
                return;

            var window = editorWindowModel.window;

            if (window != null)
            {
                var rootElement = window.rootVisualElement;

                if (EditorApplication.isUpdating || duringOnCreate)
                {
                    rootElement.schedule.Execute(() => { SendInitializeIfNecessary(false); });
                    return;
                }

                if (rootElement.GetProperty(k_InitializedWindowPropertyName) != null)
                    return;

                //we make sure styles have been applied
                UIElementsEditorUtility.AddDefaultEditorStyleSheets(rootElement);

                rootElement.SetProperty(k_InitializedWindowPropertyName, true);

                Invoke("CreateGUI");
            }
        }

        protected void Invoke(string methodName)
        {
            try
            {
                MethodInfo mi = GetPaneMethod(methodName, editorWindowModel.window);
                mi?.Invoke(editorWindowModel.window, null);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected MethodInfo GetPaneMethod(string methodName, object obj)
        {
            if (obj == null)
                return null;

            Type t = obj.GetType();

            while (t != null)
            {
                var method = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    return method;

                t = t.BaseType;
            }
            return null;
        }

        void IEditorWindowBackend.Focused()
        {
            m_Panel.Focus();
        }

        void IEditorWindowBackend.Blurred()
        {
            m_Panel.Blur();
        }

        void IEditorWindowBackend.OnDisplayWindowMenu(GenericMenu menu)
        {
            AddLiveReloadOptionToMenu(menu);
            if (UIToolkitProjectSettings.enableLayoutDebugger)
            {
                AddUIELayoutDebuggerToMenu(menu);
            }
            AddUIElementsDebuggerToMenu(menu);
            AddBindingLogOptionsToMenu(menu);
        }

        private void AddLiveReloadOptionToMenu(GenericMenu menu)
        {
            // Live Reload is off by default for all Editor Windows, except for the ones overriding liveReloadPreferenceDefault (Game View, UI Builder)
            panel.enableAssetReload = EditorPrefs.GetBool(m_LiveReloadPreferenceKey, editorWindowModel.window.liveReloadPreferenceDefault);
            menu.AddItem(EditorGUIUtility.TextContent(k_LiveReloadMenuText), panel.enableAssetReload, ToggleLiveReloadForWindowType, editorWindowModel.window);
        }

        internal void ToggleLiveReloadForWindowType(object userData)
        {
            panel.enableAssetReload = !panel.enableAssetReload;
            EditorPrefs.SetBool(m_LiveReloadPreferenceKey, panel.enableAssetReload);

            // We recreate the window regardless of Live Reload being on or off to guarantee tracking is there or not
            // depending on the option being on or off, and we don't leave leftover tracking by turning it off.
            RecreateWindow();

            if (SetupLiveReloadPanelTrackers != null && editorWindowModel?.window.GetType() == typeof(GameView))
            {
                SetupLiveReloadPanelTrackers(panel.enableAssetReload);
            }
        }

        private void AddUIElementsDebuggerToMenu(GenericMenu menu)
        {
            var itemContent = UIElementsDebugger.WindowName;
            var shortcut = ShortcutIntegration.instance.directory.FindShortcutEntry(UIElementsDebugger.k_WindowPath);
            if (shortcut != null && shortcut.combinations.Any())
                itemContent += $" {KeyCombination.SequenceToMenuString(shortcut.combinations)}";

            menu.AddItem(EditorGUIUtility.TrTextContent(itemContent), false, DebugWindow, editorWindowModel.window);
        }

        private void AddUIELayoutDebuggerToMenu(GenericMenu menu)
        {
            var itemContent = UILayoutDebuggerWindow.WindowName;
            menu.AddItem(EditorGUIUtility.TextContent(itemContent), false, LayoutDebugWindow, editorWindowModel.window);
        }

        private void DebugWindow(object userData)
        {
            EditorWindow window = userData as EditorWindow;
            if (window == null)
                return;

            if (CommandService.Exists(UIElementsDebugger.OpenWindowCommand))
                CommandService.Execute(UIElementsDebugger.OpenWindowCommand, CommandHint.Menu, window);
            else
            {
                UIElementsDebugger.OpenAndInspectWindow(window);
            }
        }

        private void AddBindingLogOptionsToMenu(GenericMenu menu)
        {
            var cachedEnum = EnumDataUtility.GetCachedEnumData(typeof(BindingLogLevel));
            var i = 0;
            foreach (var optionObj in cachedEnum.values)
            {
                var name = cachedEnum.displayNames[i++];
                var content = EditorGUIUtility.TrTextContent($"Binding Console Logs/{name}");
                var isOn = panel.dataBindingManager.logLevel == (BindingLogLevel)optionObj;
                menu.AddItem(content, isOn, o => SetBindingLogLevel((BindingLogLevel)o), optionObj);
            }
        }

        private BindingLogLevel GetBindingLogLevel(string key)
        {
            var optionStr = EditorPrefs.GetString(key, null);
            return Enum.TryParse(optionStr, out BindingLogLevel result) ? result : editorWindowModel.window.defaultBindingLogLevel;
        }

        // Internal for tests.
        internal void SetBindingLogLevel(BindingLogLevel logLevel)
        {
            Binding.SetPanelLogLevel(panel, logLevel);
            EditorPrefs.SetString(m_BindingLogLevelKey, logLevel.ToString());
        }

        private void LayoutDebugWindow(object userData)
        {
            EditorWindow window = userData as EditorWindow;
            if (window == null)
                return;

            if (CommandService.Exists(UILayoutDebuggerWindow.OpenWindowCommand))
                CommandService.Execute(UILayoutDebuggerWindow.OpenWindowCommand, CommandHint.Menu, window);
            else
            {
                UILayoutDebuggerWindow.OpenAndInspectWindow(window);
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

        private void RecreateWindow()
        {
            // Validate that recreating the window will work, otherwise users end up with a broken window.
            if (MonoScript.FromScriptableObject(editorWindowModel.window) == null)
            {
                Debug.LogError("Window serialization will fail for " + editorWindowModel.window.GetType() +
                    ", will not reload it. Make sure that there are no compile errors and that the file name and class name match.");
                return;
            }

            if (editorWindowModel.window.rootVisualElement.panel is BaseVisualElementPanel panel)
            {
                var view = panel.ownerObject as HostView;
                if (view != null && view.actualView != null)
                {
                    view.Reload(view.actualView);
                }
            }
        }

        internal static Action<bool> SetupLiveReloadPanelTrackers;

        private static string GetWindowLiveReloadPreferenceKey(Type windowType)
        {
            return windowType + k_LiveReloadPreferenceKeySuffix;
        }

        private static string GetWindowBindingLogLevelKey(Type windowType)
        {
            return windowType + k_BindingLogLevelKeySuffix;
        }

        internal static bool IsGameViewWindowLiveReloadOn()
        {
            if (k_GameViewLiveReloadPreferenceKey == null)
            {
                k_GameViewLiveReloadPreferenceKey = GetWindowLiveReloadPreferenceKey(typeof(GameView));
            }

            return EditorPrefs.GetBool(k_GameViewLiveReloadPreferenceKey, true);
        }
    }
}
