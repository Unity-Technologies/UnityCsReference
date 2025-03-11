// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEditor.ShortcutManagement;
using UnityEngine.Scripting;
using UnityEngine.Internal;
using SerializableJsonDictionary = UnityEditor.UIElements.SerializableJsonDictionary;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [UsedByNativeCode, ExcludeFromObjectFactory]
    public partial class EditorWindow : ScriptableObject
    {
        [HideInInspector]
        bool m_AutoRepaintOnSceneChange;

        [SerializeField]
        [HideInInspector]
        Vector2 m_MinSize = new Vector2(50, 50);

        [SerializeField]
        [HideInInspector]
        Vector2 m_MaxSize = new Vector2(4000, 4000);

        [SerializeField]
        [HideInInspector]
        internal GUIContent m_TitleContent;

        // We use a 24-bit depth/stencil buffer by default to support the UIRenderer clipping feature
        [HideInInspector]
        int m_DepthBufferBits = 24;
        [HideInInspector]
        int m_AntiAliasing = 1;

        [HideInInspector]
        bool m_ResetPanelRenderingOnAssetChange = true;

        [SerializeField]
        [HideInInspector]
        internal Rect m_Pos = new Rect(0, 0, 320, 550);

        [SerializeField]
        [HideInInspector]
        internal DataModeController m_SerializedDataModeController;
        public IDataModeController dataModeController => GetDataModeController_Internal(); // For each editor window.
        internal DataModeController GetDataModeController_Internal()  // For HostView to use internally.
            => m_SerializedDataModeController ??= new DataModeController();

        private VisualElement m_UIRootElement;

        internal VisualElement baseRootVisualElement
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_UIRootElement ??= CreateRoot();
        }

        public VisualElement rootVisualElement
        {
            get
            {
                if (this is ISupportsOverlays)
                {
                    if (!m_OverlaysInitialized)
                    {
                        var ve = overlayCanvas.rootVisualElement;
                        baseRootVisualElement.Add(ve);
                        overlayCanvas.Initialize(this);
                        m_OverlaysInitialized = true;
                    }

                    return overlayCanvas.windowRoot;
                }
                return baseRootVisualElement;
            }
        }

        internal virtual bool liveReloadPreferenceDefault => false;
        internal virtual BindingLogLevel defaultBindingLogLevel => BindingLogLevel.All;
        internal bool isUIToolkitWindow => m_UIRootElement != null && m_UIRootElement.childCount > 0;

        [HideInInspector]
        [SerializeField]
        private SerializableJsonDictionary m_ViewDataDictionary;

        [HideInInspector]
        [SerializeField]
        OverlayCanvas m_OverlayCanvas = new OverlayCanvas();

        public OverlayCanvas overlayCanvas => m_OverlayCanvas;

        [NonSerialized]
        bool m_OverlaysInitialized;

        private bool m_EnableViewDataPersistence;

        private bool m_RequestedViewDataSave;

        private static Action s_UpdateWindowMenuListingOff;

        internal SerializableJsonDictionary viewDataDictionary
        {
            get
            {
                // If persistence is disabled, just don't even create the dictionary. Return null.
                if (m_EnableViewDataPersistence && m_ViewDataDictionary == null)
                {
                    string editorPrefFileName = this.GetType().ToString();
                    m_ViewDataDictionary = UIElements.EditorWindowViewData.instance[editorPrefFileName];
                }
                return m_ViewDataDictionary;
            }
        }

        internal static List<EditorWindow> activeEditorWindows { get; } = new List<EditorWindow>();

        internal void SaveViewData()
        {
            m_RequestedViewDataSave = true;
        }

        private void SaveViewDataToDisk()
        {
            if (!m_EnableViewDataPersistence || m_ViewDataDictionary == null || !m_RequestedViewDataSave)
                return;

            string editorPrefFileName = this.GetType().ToString();
            EditorWindowViewData.instance.Save(editorPrefFileName, m_ViewDataDictionary);

            m_RequestedViewDataSave = false;
        }

        internal ISerializableJsonDictionary GetViewDataDictionary()
        {
            return viewDataDictionary;
        }

        // TODO: These should be made public when UIElements is no longer experimental.
        internal void DisableViewDataPersistence()
        {
            m_EnableViewDataPersistence = false;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void ClearPersistentViewData()
        {
            string editorPrefFileName = this.GetType().ToString();
            UIElements.EditorWindowViewData.instance.Clear(editorPrefFileName);
            DestroyImmediate(m_ViewDataDictionary);
            m_ViewDataDictionary = null;
        }

        [NonSerialized] internal bool m_IsPresented = false;

        // The GameView rect is in GUI space of the view
        Rect m_GameViewRect;
        Rect m_GameViewClippedRect;
        Vector2 m_GameViewTargetSize;
        EventInterests m_EventInterests = new EventInterests();
        bool m_DisableInputEvents;

        // Dockarea we're inside.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [NonSerialized]
        internal HostView m_Parent;

        // Overlay a notification message over the window.
        const double kWarningFadeoutWait = 4;
        const double kWarningFadeoutTime = 1;

        internal GUIContent m_Notification = null;
        Vector2 m_NotificationSize;
        internal float m_FadeoutTime = 0;

        // Mark the beginning area of all popup windows.
        public void BeginWindows()
        {
            EditorGUIInternal.BeginWindowsForward(1, GetInstanceID());
        }

        // Close a window group started with EditorWindow::ref::BeginWindows
        public void EndWindows()
        {
            GUI.EndWindows();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal virtual void OnResized()  {}

        internal virtual void OnBackgroundViewResized(Rect pos) {}

        // Does the GUI in this editor window want MouseMove events?
        public bool wantsMouseMove
        {
            get
            {
                return m_EventInterests.wantsMouseMove;
            }
            set
            {
                m_EventInterests.wantsMouseMove = value;
                MakeParentsSettingsMatchMe();
            }
        }

        // Does the GUI in this editor window want MouseEnter/LeaveWindow events?
        public bool wantsMouseEnterLeaveWindow
        {
            get
            {
                return m_EventInterests.wantsMouseEnterLeaveWindow;
            }
            set
            {
                m_EventInterests.wantsMouseEnterLeaveWindow = value;
                MakeParentsSettingsMatchMe();
            }
        }

        // Indicates that the editor window will only receive a layout pass before a repaint event.
        public bool wantsLessLayoutEvents
        {
            get
            {
                return m_EventInterests.wantsLessLayoutEvents;
            }
            set
            {
                m_EventInterests.wantsLessLayoutEvents = value;
                MakeParentsSettingsMatchMe();
            }
        }

        internal void CheckForWindowRepaint()
        {
            double time = EditorApplication.timeSinceStartup;
            if (time < m_FadeoutTime)
                return;
            if (time > m_FadeoutTime + kWarningFadeoutTime)
            {
                RemoveNotification();
                return;
            }
            Repaint();
        }

        internal CustomYieldInstruction WaitUntilPresented() => new WaitUntil(() => m_IsPresented);

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal GUIContent GetLocalizedTitleContent()
        {
            return GetLocalizedTitleContentFromType(GetType());
        }

        internal static GUIContent GetLocalizedTitleContentFromType(Type t)
        {
            EditorWindowTitleAttribute attr = GetEditorWindowTitleAttribute(t);
            if (attr != null)
            {
                string iconName = "";
                if (!string.IsNullOrEmpty(attr.icon))
                    iconName = attr.icon;
                else if (attr.useTypeNameAsIconName)
                    iconName = t.ToString();

                if (!string.IsNullOrEmpty(iconName) && EditorGUIUtility.LoadIcon(iconName))
                {
                    // This should error msg if icon is not found since icon has been explicitly requested by the user
                    return EditorGUIUtility.TrTextContentWithIcon(attr.title, iconName);
                }

                return EditorGUIUtility.TrTextContent(attr.title);
            }

            // Fallback to type name (Do not localize type name)
            return new GUIContent(t.Name);
        }

        static EditorWindowTitleAttribute GetEditorWindowTitleAttribute(Type t)
        {
            object[] attrs = t.GetCustomAttributes(true);
            foreach (object attr in attrs)
            {
                Attribute realAttr = (Attribute)attr;
                if ((Type)realAttr.TypeId == typeof(EditorWindowTitleAttribute))
                {
                    return (EditorWindowTitleAttribute)attr;
                }
            }
            return null;
        }

        // Show a notification message.
        public void ShowNotification(GUIContent notification)
        {
            ShowNotification(notification, kWarningFadeoutWait);
        }

        public void ShowNotification(GUIContent notification, double fadeoutWait)
        {
            m_Notification = new GUIContent(notification);
            if (m_FadeoutTime < Mathf.Epsilon)
                EditorApplication.update += CheckForWindowRepaint;
            m_FadeoutTime = (float)(EditorApplication.timeSinceStartup + fadeoutWait);
        }

        // Stop showing notification message.
        public void RemoveNotification()
        {
            if (m_FadeoutTime == 0)
                return;
            EditorApplication.update -= CheckForWindowRepaint;
            m_Notification = null;
            m_FadeoutTime = 0;
        }

        internal void DrawNotification()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            m_NotificationSize = EditorStyles.notificationText.CalcSize(m_Notification);

            Vector2 warningSize = m_NotificationSize;
            float targetWidth = position.width - EditorStyles.notificationText.margin.horizontal;
            float targetHeight = position.height - EditorStyles.notificationText.margin.vertical - 20;

            // See if we can fit horizontally. If not, rescale down.
            GUIStyle scaledNotificationText = EditorStyles.notificationText;
            if (targetWidth < m_NotificationSize.x)
            {
                float scale = targetWidth / m_NotificationSize.x;

                scaledNotificationText = new GUIStyle(EditorStyles.notificationText);
                scaledNotificationText.fontSize = Mathf.FloorToInt(scaledNotificationText.font.fontSize * scale);

                warningSize = scaledNotificationText.CalcSize(m_Notification);
            }

            warningSize.x += 1; //we'll give the text a little room to breathe to avoid word-wrapping issues with drop shadows

            if (warningSize.y > targetHeight)
                warningSize.y = targetHeight;

            Rect r = new Rect((position.width - warningSize.x - EditorStyles.notificationText.margin.horizontal) * .5f, 20 + (position.height - 20 - warningSize.y) * .7f, warningSize.x + EditorStyles.notificationText.margin.horizontal, warningSize.y);
            double time = EditorApplication.timeSinceStartup;
            if (time > m_FadeoutTime)
                GUI.color = new Color(1, 1, 1, 1 - (float)((time - m_FadeoutTime) / kWarningFadeoutTime));
            GUI.Label(r, GUIContent.none, EditorStyles.notificationBackground);
            EditorGUI.DoDropShadowLabel(r, m_Notification, scaledNotificationText, .3f);
        }

        // Does the window automatically repaint whenever the scene has changed?
        public bool autoRepaintOnSceneChange
        {
            get
            {
                return m_AutoRepaintOnSceneChange;
            }
            set
            {
                m_AutoRepaintOnSceneChange = value;
                MakeParentsSettingsMatchMe();
            }
        }

        public bool maximized
        {
            get
            {
                return WindowLayout.IsMaximized(this);
            }
            set
            {
                bool current = WindowLayout.IsMaximized(this);
                if (value != current)
                {
                    if (value)
                        WindowLayout.Maximize(this);
                    else
                        WindowLayout.Unmaximize(this);
                }
            }
        }

        internal virtual void OnMaximized() {}

        internal virtual bool CanMaximize()
        {
            return true;
        }

        // Is EditorWindow focused?
        public bool hasFocus { get { return m_Parent && m_Parent.actualView == this; } }

        // Is EditorWindow docked?
        public bool docked { get { return m_Parent != null && m_Parent.window != null && !m_Parent.window.IsNotDocked(); } }

        // This property can be used to stop OS events from being sent to the EditorWindow
        internal bool disableInputEvents
        {
            get { return m_DisableInputEvents; }
            set
            {
                if (m_DisableInputEvents == value)
                    return;
                m_DisableInputEvents = value;
                MakeParentsSettingsMatchMe();
            }
        }

        // The EditorWindow which currently has keyboard focus (RO)
        static public EditorWindow focusedWindow
        {
            get
            {
                HostView view = GUIView.focusedView as HostView;
                if (view != null)
                    return view.actualView;
                else
                    return null;
            }
        }

        [Obsolete($"Use {nameof(windowFocusChanged)} instead.", false)]
        static public Action focusedWindowChanged;

        static public event Action windowFocusChanged;

        static internal void OnWindowFocusChanged()
        {
            windowFocusChanged?.Invoke();
        }

        // The EditorWindow currently under the mouse cursor (RO)
        static public EditorWindow mouseOverWindow
        {
            get
            {
                HostView view = GUIView.mouseOverView as HostView;
                if (view != null)
                    return view.actualView;
                else
                    return null;
            }
        }

        internal int GetNumTabs()
        {
            DockArea da = m_Parent as DockArea;
            if (da)
            {
                return da.m_Panes.Count;
            }
            return 0;
        }

        internal bool ShowNextTabIfPossible()
        {
            DockArea da = m_Parent as DockArea;
            if (da)
            {
                int idx = da.m_Panes.IndexOf(this);
                idx = (idx + 1) % da.m_Panes.Count;
                if (da.selected != idx)
                {
                    da.selected = idx;
                    da.Repaint();
                    return true;
                }
            }
            return false;
        }

        public void ShowTab()
        {
            DockArea da = m_Parent as DockArea;
            if (da)
            {
                int idx = da.m_Panes.IndexOf(this);
                if (da.selected != idx)
                    da.selected = idx;
            }

            Repaint();
        }

        internal bool IsSelectedTab()
        {
            DockArea da = m_Parent as DockArea;
            if (da)
            {
                int idx = da.m_Panes.IndexOf(this);
                return da.selected == idx;
            }
            return false;
        }

        // Moves keyboard focus to this EditorWindow.
        public void Focus()
        {
            if (m_Parent)
            {
                ShowTab();
                m_Parent.Focus();
            }
        }

        internal void MakeParentsSettingsMatchMe()
        {
            if (!m_Parent || m_Parent.actualView != this)
                return;
            m_Parent.SetTitle(GetType().FullName);
            m_Parent.autoRepaintOnSceneChange = m_AutoRepaintOnSceneChange;
            bool parentChanged =  m_Parent.depthBufferBits != m_DepthBufferBits || m_Parent.antiAliasing != m_AntiAliasing;
            m_Parent.depthBufferBits = m_DepthBufferBits;
            m_Parent.antiAliasing = m_AntiAliasing;
            m_Parent.resetPanelRenderingOnAssetChange = m_ResetPanelRenderingOnAssetChange;
            m_Parent.SetInternalGameViewDimensions(m_GameViewRect, m_GameViewClippedRect, m_GameViewTargetSize);
            m_Parent.eventInterests = m_EventInterests;
            m_Parent.disableInputEvents = m_DisableInputEvents;
            Vector2 parentBorderSizes = new Vector2(m_Parent.borderSize.left + m_Parent.borderSize.right, m_Parent.borderSize.top + m_Parent.borderSize.bottom);
            m_Parent.SetMinMaxSizes(minSize + parentBorderSizes, maxSize + parentBorderSizes);
            m_Parent.SetPlayModeView(this is PlayModeView);
            if (parentChanged)
                m_Parent.RecreateContext();
        }

        // Show the EditorWindow as a floating utility window.
        public void ShowUtility()
        {
            ShowWithMode(ShowMode.Utility);
        }

        internal void ShowTooltip()
        {
            ShowPopupWithMode(ShowMode.Tooltip, false);
        }

        public void ShowPopup()
        {
            ShowPopupWithMode(ShowMode.PopupMenu, true);
        }

        void MakeModal()
        {
            // If we already have modal window up we don't need to setup another modal message loop
            if (ContainerWindow.s_Modal) return;

            try
            {
                ContainerWindow.s_Modal = true;

                SavedGUIState guiState = SavedGUIState.Create();
                // TODO need to promote this outside of UIE
                UnityEngine.UIElements.EventDispatcher.editorDispatcher.PushDispatcherContext();

                Internal_MakeModal(m_Parent.window);

                UnityEngine.UIElements.EventDispatcher.editorDispatcher.PopDispatcherContext();
                guiState.ApplyAndForget();
            }
            finally
            {
                ContainerWindow.s_Modal = false;
            }
        }

        public void ShowModalUtility()
        {
            ShowWithMode(ShowMode.ModalUtility);
            MakeModal();
        }

        // Used for popup style windows.
        internal void ShowPopupWithMode(ShowMode mode, bool giveFocus)
        {
            if (m_Parent == null)
            {
                ContainerWindow cw = ScriptableObject.CreateInstance<ContainerWindow>();
                cw.title = titleContent.text;
                HostView host = ScriptableObject.CreateInstance<HostView>();
                host.actualView = this;
                m_Parent = host;

                Rect r = m_Parent.borderSize.Add(new Rect(position.x, position.y, position.width, position.height));
                // Order is important here: first set rect of container, then assign main view, then apply various settings, then show.
                // Otherwise the rect won't be set until first resize happens.
                cw.position = r;
                cw.rootView = host;
                MakeParentsSettingsMatchMe();
                cw.ShowPopupWithMode(mode, giveFocus);
                cw.OnResize();
            }
        }

        internal void ShowWithMode(ShowMode mode)
        {
            if (m_Parent == null)
            {
                SavedGUIState oldState = SavedGUIState.Create();

                ContainerWindow cw = ScriptableObject.CreateInstance<ContainerWindow>();
                cw.title = titleContent.text;
                HostView host = ScriptableObject.CreateInstance<HostView>();
                host.actualView = this;
                m_Parent = host;

                Rect r = m_Parent.borderSize.Add(new Rect(position.x, position.y, position.width, position.height));
                // Order is important here: first set rect of container, then assign main view, then apply various settings, then show.
                // Otherwise the rect won't be set until first resize happens.
                cw.position = r;
                cw.rootView = host;
                MakeParentsSettingsMatchMe();
                cw.Show(mode, loadPosition: true, displayImmediately: false, setFocus: true);
                // set min/max size now that native window is not null so that it will e.g., use proper styleMask on macOS
                cw.SetMinMaxSizes(minSize, maxSize);
                cw.OnResize();
                oldState.ApplyAndForget();
            }
        }

        // Show window with dropdown behaviour (e.g. window is closed when it loses focus) and having
        public void ShowAsDropDown(Rect buttonRect, Vector2 windowSize)
        {
            ShowAsDropDown(buttonRect, windowSize, null);
        }

        internal void ShowAsDropDown(Rect buttonRect, Vector2 windowSize, PopupLocation[] locationPriorityOrder)
        {
            ShowAsDropDown(buttonRect, windowSize, locationPriorityOrder, ShowMode.PopupMenu);
        }

        internal void ShowAsDropDown(Rect buttonRect, Vector2 windowSize, PopupLocation[] locationPriorityOrder, ShowMode mode)
        {
            ShowAsDropDown(buttonRect, windowSize, locationPriorityOrder, mode, true);
        }

        // Show as drop down list with custom fit to screen callback
        // 'buttonRect' is used for displaying the dropdown below that rect if possible otherwise above
        // 'windowSize' is used for setting up initial size
        // 'locationPriorityOrder' is for manual popup direction, if null it uses default order: down, up, left or right
        // 'giveFocus' is for whether the window should immediately be given focus (default true)
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void ShowAsDropDown(Rect buttonRect, Vector2 windowSize, PopupLocation[] locationPriorityOrder, ShowMode mode, bool giveFocus)
        {
            // Setup position before bringing window live (otherwise the dropshadow on Windows will be placed in 0,0 first frame)
            position = ShowAsDropDownFitToScreen(buttonRect, windowSize, locationPriorityOrder);

            // ShowWithMode() always grabs window focus so we use ShowPopup() for popup windows so PopupWindowWithoutFocus
            // will work correctly (no focus when opened).
            if (ContainerWindow.IsPopup(mode))
                ShowPopupWithMode(mode, giveFocus);
            else
                ShowWithMode(mode);

            // Fit to screen again now that we have a container window
            position = ShowAsDropDownFitToScreen(buttonRect, windowSize, locationPriorityOrder);

            // Default to none resizable window
            minSize = new Vector2(position.width, position.height);
            maxSize = new Vector2(position.width, position.height);

            // Focus window
            if (giveFocus && focusedWindow != this)
                Focus();
            else
                Repaint();

            // Add after unfreezing display because AuxWindowManager.cpp assumes that aux windows are added after we got/lost- focus calls.
            m_Parent.AddToAuxWindowList();

            // Dropdown windows should not be saved to layout
            m_Parent.window.m_DontSaveToLayout = true;
        }

        internal Rect ShowAsDropDownFitToScreen(Rect buttonRect, Vector2 windowSize, PopupLocation[] locationPriorityOrder)
        {
            if (m_Parent == null)
                return new Rect(buttonRect.x, buttonRect.yMax, windowSize.x, windowSize.y);

            return m_Parent.window.GetDropDownRect(buttonRect, windowSize, windowSize, locationPriorityOrder);
        }

        public void Show()
        {
            Show(false);
        }

        // Show the EditorWindow.
        public void Show(bool immediateDisplay)
        {
            // If somebody called show on us, set up the neccessary structure for us.
            if (m_Parent == null)
                CreateNewWindowForEditorWindow(this, true, immediateDisplay);
        }

        // Show the editor window in the auxiliary window.
        public void ShowAuxWindow()
        {
            ShowWithMode(ShowMode.AuxWindow);

            // We ensure Focus change before calling AddToAuxWindowList because the
            // AuxWindowManager assumes that focus has been changed before a new window is added.
            Focus();

            m_Parent.AddToAuxWindowList();
        }

        // Show modal editor window. Other windows will not be accessible until this one is closed.
        public void ShowModal()
        {
            // It is normally bad to have different behavior on different platforms,
            // but in this case Linux is espcially picky about converting modal dialogs.
            // the only way we can ensure that without major API changes in window creation is to open them with this type.
            if (Application.platform == RuntimePlatform.LinuxEditor)
                ShowModalUtility();
            else
            {
                ShowWithMode(ShowMode.AuxWindow);
                // Make sure consumed events will not make window wait to get rendered
                RepaintImmediately();
                MakeModal();
            }
        }

        static void AssignTitle(EditorWindow win, string title)
        {
            if (title != null)
            {
                win.titleContent = new GUIContent(title);
                return;
            }

            // Do not assign anything new if the user has defined its own title.
            var titleContent = GetLocalizedTitleContentFromType(win.GetType());
            if (win.titleContent.text == win.GetType().ToString())
            {
                win.titleContent.text = titleContent.text;
            }
            if (win.titleContent.image == null)
            {
                win.titleContent.image = titleContent.image;
            }
        }

        // Returns the first EditorWindow of type /t/ which is currently on the screen.
        static EditorWindow GetWindowPrivate(System.Type t, bool utility, string title, bool focus, bool includeInheritingClasses = true)
        {
            UnityEngine.Object[] wins = Resources.FindObjectsOfTypeAll(t);
            EditorWindow win = wins.Length > 0 ? (EditorWindow)(wins[0]) : null;

            if(win != null && !includeInheritingClasses)
            {
                if(win.GetType().IsSubclassOf(t))
                {
                    win = null;
                    for(int i = 1; i<wins.Length && win == null; ++i)
                    {
                        if(wins[i] != null && !wins[i].GetType().IsSubclassOf(t))
                        {
                            win = (EditorWindow)(wins[i]);
                        }
                    }
                }
            }

            if (!win)
            {
                try
                {
                    win = ScriptableObject.CreateInstance(t) as EditorWindow;
                    AssignTitle(win, title);
                    if (utility)
                        win.ShowUtility();
                    else
                        win.Show();
                }
                catch
                {
                    win.Close();
                    throw;
                }
            }
            else if (focus)
            {
                win.Show();  // For some corner cases in saved layouts, the window can be in an unvisible state.
                             // Since the caller asked for focus, it's fair to assume he wants it always to be visible. (case 586743)
                win.Focus();
            }

            return win;
        }

        public static EditorWindow GetWindow(System.Type windowType, [DefaultValue("false")] bool utility, [DefaultValue("null")] string title, [DefaultValue("true")] bool focus)
        {
            return GetWindowPrivate(windowType, utility, title, focus);
        }

        [ExcludeFromDocs]
        public static EditorWindow GetWindow(System.Type windowType, bool utility, string title)
        {
            return GetWindowPrivate(windowType, utility, title, true);
        }

        [ExcludeFromDocs]
        public static EditorWindow GetWindow(System.Type windowType, bool utility)
        {
            return GetWindowPrivate(windowType, utility, null, true);
        }

        [ExcludeFromDocs]
        public static EditorWindow GetWindow(System.Type windowType)
        {
            return GetWindowPrivate(windowType, false, null, true);
        }

        public static EditorWindow GetWindowWithRect(System.Type windowType, Rect rect, [DefaultValue("false")] bool utility, [DefaultValue("null")] string title)
        {
            return GetWindowWithRectPrivate(windowType, rect, utility, title);
        }

        [ExcludeFromDocs]
        public static EditorWindow GetWindowWithRect(System.Type windowType, Rect rect, bool utility)
        {
            return GetWindowWithRectPrivate(windowType, rect, utility, null);
        }

        [ExcludeFromDocs]
        public static EditorWindow GetWindowWithRect(System.Type windowType, Rect rect)
        {
            return GetWindowWithRectPrivate(windowType, rect, false, null);
        }

        internal static T GetWindowWithExactType<T>() where T : EditorWindow
        {
            return GetWindowPrivate(typeof(T), false, null, true, false) as T;
        }

        public static T GetWindow<T>() where T : EditorWindow
        {
            return GetWindow<T>(false, null, true);
        }

        public static T GetWindow<T>(bool utility) where T : EditorWindow
        {
            return GetWindow<T>(utility, null, true);
        }

        public static T GetWindow<T>(bool utility, string title) where T : EditorWindow
        {
            return GetWindow<T>(utility, title, true);
        }

        public static T GetWindow<T>(string title) where T : EditorWindow
        {
            return GetWindow<T>(title, true);
        }

        public static T GetWindow<T>(string title, bool focus) where T : EditorWindow
        {
            return GetWindow<T>(false, title, focus);
        }

        // Returns the first EditorWindow of type /T/ which is currently on the screen.
        public static T GetWindow<T>(bool utility, string title, bool focus) where T : EditorWindow
        {
            return GetWindow(typeof(T), utility, title, focus) as T;
        }

        public static T GetWindow<T>(params System.Type[] desiredDockNextTo) where T : EditorWindow
        {
            return GetWindow<T>(null, true, desiredDockNextTo);
        }

        public static T GetWindow<T>(string title, params System.Type[] desiredDockNextTo) where T : EditorWindow
        {
            return GetWindow<T>(title, true, desiredDockNextTo);
        }

        // Returns the first EditorWindow of type /T/ which is currently on the screen.
        public static T GetWindow<T>(string title, bool focus, params System.Type[] desiredDockNextTo) where T : EditorWindow
        {
            var wins = Resources.FindObjectsOfTypeAll(typeof(T)) as T[];
            T win = wins.Length > 0 ? wins[0] : null;

            //If the window already exists just focus then return it...
            if (win != null)
            {
                if (focus)
                    win.Focus();
                return win;
            }

            return CreateWindow<T>(title, desiredDockNextTo);
        }

        public static T CreateWindow<T>(params System.Type[] desiredDockNextTo) where T : EditorWindow
        {
            return CreateWindow<T>(null, desiredDockNextTo);
        }

        public static T CreateWindow<T>(string title, params System.Type[] desiredDockNextTo) where T : EditorWindow
        {
            T win = CreateInstance<T>();

            AssignTitle(win, title);

            //Iterate the desired dock next to types...
            foreach (var desired in desiredDockNextTo)
            {
                var windows = ContainerWindow.windows;
                foreach (var w in windows)
                {
                    foreach (var view in w.rootView.allChildren)
                    {
                        var dockArea = view as DockArea;
                        if (dockArea == null) continue;
                        if (dockArea.m_Panes.Any(pane => pane.GetType() == desired))
                        {
                            dockArea.AddTab(win);
                            return win;
                        }
                    }
                }
            }
            win.Show();

            return win;
        }

        public static bool HasOpenInstances<T>() where T : UnityEditor.EditorWindow
        {
            UnityEngine.Object[] wins = Resources.FindObjectsOfTypeAll(typeof(T));
            return wins != null && wins.Length > 0;
        }

        // Focuses the first found EditorWindow of specified type if it is open.
        public static void FocusWindowIfItsOpen(System.Type t)
        {
            UnityEngine.Object[] wins = Resources.FindObjectsOfTypeAll(t);
            EditorWindow win = wins.Length > 0 ? (wins[0] as EditorWindow) : null;
            if (win)
                win.Focus();
        }

        // Focuses the first found EditorWindow of type /T/ if it is open.
        public static void FocusWindowIfItsOpen<T>() where T : EditorWindow
        {
            FocusWindowIfItsOpen(typeof(T));
        }

        internal void RemoveFromDockArea()
        {
            DockArea da = m_Parent as DockArea;
            if (da)
            {
                da.RemoveTab(this, true);
            }
        }

        // Returns the first EditorWindow of type /t/ which is currently on the screen.
        static EditorWindow GetWindowWithRectPrivate(System.Type t, Rect rect, bool utility, string title)
        {
            UnityEngine.Object[] wins = Resources.FindObjectsOfTypeAll(t);
            EditorWindow win = wins.Length > 0 ? (EditorWindow)(wins[0]) : null;

            if (!win)
            {
                win = ScriptableObject.CreateInstance(t) as EditorWindow;
                win.minSize = new Vector2(rect.width, rect.height);
                win.maxSize = new Vector2(rect.width, rect.height);
                win.position = rect;
                AssignTitle(win, title);
                if (utility)
                    win.ShowUtility();
                else
                    win.Show();
            }
            else
                win.Focus();

            return win;
        }

        public static T GetWindowWithRect<T>(Rect rect) where T : EditorWindow
        {
            return GetWindowWithRect<T>(rect, false, null, true);
        }

        public static T GetWindowWithRect<T>(Rect rect, bool utility) where T : EditorWindow
        {
            return GetWindowWithRect<T>(rect, utility, null, true);
        }

        public static T GetWindowWithRect<T>(Rect rect, bool utility, string title) where T : EditorWindow
        {
            return GetWindowWithRect<T>(rect, utility, title, true);
        }

        // Returns the first EditorWindow of type /t/ which is currently on the screen.
        public static T GetWindowWithRect<T>(Rect rect, bool utility, string title, bool focus) where T : EditorWindow
        {
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(typeof(T));
            T window;

            if (windows.Length > 0)
            {
                window = (T)windows[0];
                if (focus)
                    window.Focus();
            }
            else
            {
                window = ScriptableObject.CreateInstance<T>();
                window.minSize = new Vector2(rect.width, rect.height);
                window.maxSize = new Vector2(rect.width, rect.height);
                window.position = rect;
                AssignTitle(window, title);
                if (utility)
                    window.ShowUtility();
                else
                    window.Show();
            }

            return window;
        }

        internal static T GetWindowDontShow<T>() where T : EditorWindow
        {
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(typeof(T));
            return (windows.Length > 0) ? (T)windows[0] : ScriptableObject.CreateInstance<T>();
        }

        bool m_HasUnsavedChanges = false;
        public bool hasUnsavedChanges
        {
            get
            {
                return m_HasUnsavedChanges;
            }
            protected set
            {
                if (m_HasUnsavedChanges != value)
                {
                    m_HasUnsavedChanges = value;
                    m_Parent?.window?.UnsavedStateChanged();
                }
            }
        }

        public string saveChangesMessage { get; protected set; }

        public virtual void SaveChanges()
        {
            hasUnsavedChanges = false;
        }

        public virtual void DiscardChanges()
        {
            hasUnsavedChanges = false;
        }

        // Close the editor window.
        public void Close()
        {
            // Guard against multiple call to Close in the same stack
            // (since we call DestroyImmediate at the end of Close()).
            if (!this)
                return;

            // Ensure to restore normal workspace before destroying. Fix case 406657.
            if (WindowLayout.IsMaximized(this))
                WindowLayout.Unmaximize(this);

            // [UUM-58449] If the focused window got closed, reset the IME composition mode to the default value. The normal codepaths may not run since this object is immediately destroyed.
            if (focusedWindow == this)
            {
                GUIUtility.imeCompositionMode = IMECompositionMode.Auto;
            }

            DockArea da = m_Parent as DockArea;
            if (da)
            {
                m_Parent.Focus();
                da.RemoveTab(this, true);
            }
            else
            {
                m_Parent.window.Close();
            }
            UnityEngine.Object.DestroyImmediate(this, true);
            UpdateWindowMenuListing();
        }

        // Make the window repaint.
        public void Repaint()
        {
            if (m_Parent && m_Parent.actualView == this)
            {
                m_Parent.Repaint();
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void RepaintImmediately()
        {
            if (m_Parent && m_Parent.actualView == this)
                m_Parent.RepaintImmediately();
        }

        // the minimum size of this window
        public Vector2 minSize
        {
            get
            {
                return m_MinSize;
            }
            set
            {
                if (!View.IsValidViewSize(value))
                    throw new ArgumentException($"Invalid minSize: {value}");

                m_MinSize = value;
                MakeParentsSettingsMatchMe();
            }
        }

        // the maximum size of this window
        public Vector2 maxSize
        {
            get
            {
                return m_MaxSize;
            }
            set
            {
                if (!View.IsValidViewSize(value))
                    throw new ArgumentException($"Invalid minSize: {value}");

                m_MaxSize = value;
                MakeParentsSettingsMatchMe();
            }
        }


        // The title of this window (legacy)
        [System.Obsolete("Use titleContent instead (it supports setting a title icon as well).")]
        public string title
        {
            get
            {
                return titleContent.text;
            }
            set
            {
                titleContent = EditorGUIUtility.TextContent(value);
            }
        }

        public GUIContent titleContent
        {
            get
            {
                return m_TitleContent ?? (m_TitleContent = new GUIContent());  // Ensure m_TitleContent is not null (so we can prevent null checks)
            }
            set
            {
                m_TitleContent = value;
                if (m_TitleContent != null && m_Parent && m_Parent.window && m_Parent.window.rootView == m_Parent)
                    m_Parent.window.title = m_TitleContent.text;
            }
        }

        public int depthBufferBits
        {
            get { return m_DepthBufferBits; }
            set { m_DepthBufferBits = value; }
        }

        internal int antiAliasing
        {
            get { return m_AntiAliasing; }
            set { m_AntiAliasing = value; }
        }

        internal bool resetPanelRenderingOnAssetChange
        {
            get => m_ResetPanelRenderingOnAssetChange;
            set => m_ResetPanelRenderingOnAssetChange = value;
        }

        internal void SetParentGameViewDimensions(Rect rect, Rect clippedRect, Vector2 targetSize)
        {
            m_GameViewRect = rect;
            m_GameViewClippedRect = clippedRect;
            m_GameViewTargetSize = targetSize;
            m_Parent.SetInternalGameViewDimensions(m_GameViewRect, m_GameViewClippedRect, m_GameViewTargetSize);
        }

        internal void SetPlayModeViewSize(Vector2 targetSize)
        {
            m_GameViewTargetSize = targetSize;
            m_Parent.SetInternalGameViewDimensions(m_GameViewRect, m_GameViewClippedRect, m_GameViewTargetSize);
        }

        internal void SetMainPlayModeViewSize(Vector2 targetSize)
        {
            if (m_Parent != null)
                m_Parent.SetMainPlayModeViewSize(targetSize);
        }

        internal void SetDisplayViewSize(int displayId, Vector2 targetSize)
        {
            if (m_Parent != null)
                m_Parent.SetDisplayViewSize(displayId, targetSize);
        }

        internal Vector2 GetDisplayViewSize(int displayId)
        {
            if (m_Parent != null)
               return m_Parent.GetDisplayViewSize(displayId);
            return new Vector2(640, 480);
        }

        [Obsolete("AA is not supported on EditorWindows", false)]
        public int antiAlias
        {
            get { return 1; }
            set {}
        }

        // The position of the window in screen space.
        public Rect position
        {
            get
            {
                return m_Pos;
            }
            set
            {
                m_Pos = value;
                // We're setting the position of this editorWindow.
                // Only handle this is we have a parent. (unless we're just getting set up)
                if (m_Parent)
                {
                    // Figure out if we're the only window here. If we are not, we need to undock us. If we are not, we should just
                    // move the ContainerWindow that we're inside
                    DockArea da = m_Parent as DockArea;
                    if (!da)
                    {
                        if(m_Parent.window.showMode == ShowMode.Tooltip || m_Parent.window.showMode == ShowMode.PopupMenu)
                            m_Parent.window.position = ContainerWindow.FitRectToScreen(value, value.center, true, m_Parent.window);
                        else
                            m_Parent.window.position = value;
                        m_Parent.window.OnResize();
                    }
                    else if (da.parent && da.m_Panes.Count == 1 && !da.parent.parent)     // We should have a DockArea, then a splitView, then null
                    {
                        // This introduces a shift of Y coordinate by top value(20)
                        var newPosition = da.borderSize.Add(value);
                        // Adding the same top value
                        newPosition.y += da.borderSize.top;
                        da.window.position = newPosition;
                    }
                    else
                    {
                        // We're docked in a deeper hierarchy, so we need to undock us
                        da.RemoveTab(this);
                        // and then create a new window for us...
                        CreateNewWindowForEditorWindow(this, true, true);
                    }
                }
            }
        }

        // Sends an Event to a window.
        public bool SendEvent(Event e)
        {
            return m_Parent.SendEvent(e);
        }

        public EditorWindow()
        {
            m_EnableViewDataPersistence = true;
            m_RequestedViewDataSave = false;
            titleContent.text = GetType().ToString();
            saveChangesMessage = $"{GetType()} has unsaved changes.";

            UpdateWindowMenuListing();
        }

        void __internalAwake()
        {
            hideFlags = HideFlags.DontSave; // Can't be HideAndDontSave, as that would make scriptable wizard GUI be disabled
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            s_ShortcutContext = new OverlayShortcutContext();
            EditorApplication.delayCall += () => ShortcutIntegration.instance.contextManager.RegisterToolContext(s_ShortcutContext);
        }

        void OnEnableINTERNAL()
        {
            activeEditorWindows.Add(this);
        }

        void OnDisableINTERNAL()
        {
            m_OverlayCanvas.OnContainerWindowDisabled();
            SaveViewDataToDisk();
            activeEditorWindows.Remove(this);
        }

        internal void ReleaseViewData()
        {
            SaveViewDataToDisk();
            DestroyImmediate(m_ViewDataDictionary);
            m_ViewDataDictionary = null;
        }

        // Internal stuff:
        // Helper to show this EditorWindow
        internal static void CreateNewWindowForEditorWindow(EditorWindow window, bool loadPosition, bool showImmediately, bool setFocus = true)
        {
            ContainerWindow cw = ScriptableObject.CreateInstance<ContainerWindow>();
            SplitView sw = ScriptableObject.CreateInstance<SplitView>();
            cw.rootView = sw;
            DockArea da = ScriptableObject.CreateInstance<DockArea>();
            da.AddTab(window, setFocus);
            sw.AddChild(da);
            Rect r = window.m_Parent.borderSize.Add(window.position);
            cw.position = r;
            sw.position = new Rect(0, 0, r.width, r.height);
            window.MakeParentsSettingsMatchMe();
            cw.Show(ShowMode.NormalWindow, loadPosition, showImmediately, setFocus: true);
            //Need this, as show my change the size of the window, due to screen constraints
            cw.OnResize();
            cw.UnsavedStateChanged();
        }

        // This is such a hack, but will do for now
        [ContextMenu("Add Scene")]
        internal void AddSceneTab() {}

        [ContextMenu("Add Game")]
        internal void AddGameTab() {}

        public virtual IEnumerable<Type> GetExtraPaneTypes()
        {
            return Enumerable.Empty<Type>();
        }

        internal static void UpdateWindowMenuListing()
        {
            s_UpdateWindowMenuListingOff?.Invoke();
            s_UpdateWindowMenuListingOff = EditorApplication.CallDelayed(BuildWindowMenuListing);
        }

        internal static void BuildWindowMenuListing()
        {
            if (!ModeService.HasCapability(ModeCapability.LayoutWindowMenu, true))
                return;

            const string k_RootMenuItemName = "Window/Panels";

            Menu.RemoveMenuItem(k_RootMenuItemName);
            var editorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            int menuIdx = -15;

            Menu.AddMenuItem($"{k_RootMenuItemName}/Close all floating panels...", "", false, menuIdx++, () =>
            {
                var windows = Resources.FindObjectsOfTypeAll<ContainerWindow>();
                foreach (var win in windows.Where(w => !!w && w.showMode != ShowMode.MainWindow))
                    win.Close();
            }, null);
            Menu.AddSeparator($"{k_RootMenuItemName}/", menuIdx++);

            int menuIndex = 1;
            foreach (var win in editorWindows.Where(e => !!e).OrderBy(e => e.titleContent.text))
            {
                var title = win.titleContent.text;
                title = title.Replace("/", "\\");
                Menu.AddMenuItem($"{k_RootMenuItemName}/{menuIndex++} {title}", "", false, menuIdx++, () => win.Focus(), null);
            }
        }

        private VisualElement CreateRoot()
        {
            const string rootName = VisualElement.k_RootVisualContainerName;
            var root = new VisualElement()
            {
                name = VisualElementUtils.GetUniqueName(rootName),
                pickingMode = PickingMode.Ignore, // do not eat events so IMGUI gets them
                viewDataKey = rootName,
                renderHints = RenderHints.ClipWithScissors
            };
            root.pseudoStates |= PseudoStates.Root;
            UIElementsEditorUtility.AddDefaultEditorStyleSheets(root);
            root.style.overflow = Overflow.Hidden;
            return root;
        }

        static OverlayShortcutContext s_ShortcutContext = new OverlayShortcutContext();

        internal class OverlayShortcutContext : IShortcutContext
        {
            public EditorWindow editorWindow;
            public bool active
            {
                get
                {
                    var focusedWindow = EditorWindow.focusedWindow;
                    if (focusedWindow != null)
                    {
                        if (focusedWindow is ISupportsOverlays)
                        {
                            editorWindow = focusedWindow;
                            return true;
                        }
                    }

                    editorWindow = null;
                    return false;
                }
            }
        }

        [Shortcut("Overlays/Toggle All Overlays", typeof(OverlayShortcutContext), KeyCode.BackQuote, ShortcutModifiers.Shift)]
        static void ToggleAllOverlays(ShortcutArguments args)
        {
            if (!(args.context is OverlayShortcutContext context))
                return;
            var canvas = context.editorWindow.overlayCanvas;
            canvas.overlaysEnabled = !canvas.overlaysEnabled;
        }

        [Shortcut("Overlays/Hide Overlay", typeof(OverlayShortcutContext))]
        static void HideOverlay(ShortcutArguments args)
        {
            if(args.context is OverlayShortcutContext context)
                context.editorWindow.overlayCanvas.HideHoveredOverlay();
        }

        public bool TryGetOverlay(string id, out Overlay match)
        {
            return m_OverlayCanvas.TryGetOverlay(id, out match);
        }

        internal void OnBackingScaleFactorChangedInternal()
        {
            if(overlayCanvas != null)
                overlayCanvas.Rebuild();

            OnBackingScaleFactorChanged();
        }

        protected virtual void OnBackingScaleFactorChanged() { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class EditorWindowTitleAttribute : System.Attribute
    {
        public string title { get; set; }
        public string icon { get; set; }
        public bool useTypeNameAsIconName { get; set; }
    }

    /// <summary>
    /// This enumeration is used for creating instances of <see cref="UIFrameworkAttribute"/>.
    /// Values of this enumeration tell external tools which UI Framework is used on a given type of Editor UI
    /// (that is, a specific type of EditorWindow, Editor or PropertyDrawer).
    /// </summary>
    internal enum UIFrameworkUsage
    {
        /// <summary>
        /// By default, the usage of a UI Framework is auto-detected by reflection
        /// </summary>
        /// <remarks>
        /// <para>In many cases, the UI Framework of an Editor UI type can be auto-detected.</para>
        /// <para>If a custom EditorWindow only overrides <see cref="EditorWindow.CreateGUI"/>, it will be classified as using
        /// UI Toolkit. Similarly, if it only overrides <see cref="EditorWindow.OnGUI"/>, it will be classified as using
        /// IMGUI. Finally, if a custom EditorWindow overrides <see cref="EditorWindow.OnEnable"/> but does not override
        /// <see cref="EditorWindow.OnGUI"/>, it is classified as using UI Toolkit (this was often the usage pattern until
        /// the introduction of <see cref="EditorWindow.CreateGUI"/>).
        /// In any other case, the usage of the <see cref="UIFrameworkAttribute"/> is required.</para>
        /// <para>If a custom Editor only overrides <see cref="Editor.CreateInspectorGUI"/>, it will be classified as using
        /// UI Toolkit. Similarly, if it only overrides <see cref="EditorWindow.OnInspectorGUI"/>, it will be classified as using
        /// IMGUI. In any other case, the usage of the <see cref="UIFrameworkAttribute"/> is required.</para>
        /// </remarks>
        AutoDetected,
        /// <summary>
        /// This type of Editor UI uses IMGUI
        /// </summary>
        IMGUI,
        /// <summary>
        /// This type of Editor UI uses UI Toolkit
        /// </summary>
        UITK,
        /// <summary>
        /// This type of Editor UI uses a mix of IMGUI and UI Toolkit.
        /// </summary>
        /// <remarks>
        /// This value is appropriate when a type of Editor UI uses <see cref="IMGUIContainer"/> because it is only
        /// partially migrated to UI Toolkit, or because it supports immediate-mode callbacks for backwards
        /// compatibility with existing public APIs.
        /// </remarks>
        Mixed,
        /// <summary>
        /// This type of Editor UI should not be accounted for by tooling.
        /// </summary>
        /// <remarks>
        /// This value is appropriate when a type of Editor UI is not exposed to end-users.
        /// </remarks>
        Excluded
    }

    /// <summary>
    /// <para>This attribute tells external tools which UI Framework is used on a given type of Editor UI
    /// (that is, a specific type of EditorWindow, Editor or PropertyDrawer).</para>
    ///
    /// <para>To use this attribute, add to a custom EditorWindow or Editor class by giving it a value from the
    /// <see cref="UIFrameworkUsage"/> enumeration.</para>
    ///
    /// <para>In many cases, the UI Framework of an Editor UI type can be auto-detected without this attribute
    /// (see <see cref="UIFrameworkUsage.AutoDetected"/>).</para>
    /// </summary>.
    [AttributeUsage(AttributeTargets.Class)]
    internal class UIFrameworkAttribute : Attribute
    {
        /// <summary>
        /// UI framework used by the target type of the attribute
        /// </summary>
        public UIFrameworkUsage frameworkUsage { get; private set; }

        /// <summary>
        /// Constructs an instance of this class
        /// </summary>
        /// <param name="frameworkUsage">UI framework used by the target type of the attribute</param>
        public UIFrameworkAttribute(UIFrameworkUsage frameworkUsage)
        {
            this.frameworkUsage = frameworkUsage;
        }
    }

    namespace UIElements
    {
        public static class UIElementsEntryPoint
        {
            public static void SetAntiAliasing(this EditorWindow window, int aa)
            {
                window.antiAliasing = aa;
            }

            public static int GetAntiAliasing(this EditorWindow window)
            {
                return window.antiAliasing;
            }
        }
    }
} //namespace
