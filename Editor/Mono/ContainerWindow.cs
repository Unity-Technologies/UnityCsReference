// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEditor
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial class ContainerWindow : ScriptableObject
    {
        [SerializeField] MonoReloadableIntPtr m_WindowPtr;
        [SerializeField] Rect m_PixelRect;
        [SerializeField] int m_ShowMode;
        [SerializeField] string m_Title = "";

        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_MainView")] View m_RootView;
        [SerializeField] Vector2 m_MinSize = new Vector2(120, 80);
        [SerializeField] Vector2 m_MaxSize = new Vector2(8192, 8192);
        [SerializeField] bool m_Maximized;

        internal bool m_DontSaveToLayout = false;

        const float kBorderSize = 4;
        const float kTitleHeight = 24;

        private int m_ButtonCount;
        private float m_TitleBarWidth;

        // Fit a container window to the screen.
        static internal bool macEditor { get { return Application.platform == RuntimePlatform.OSXEditor; } }

        const float kButtonWidth = 13, kButtonHeight = 13, kButtonSpacing = 3, kButtonTop = 0;

        private static class Styles
        {
            // Title Bar Buttons (Non)
            public static GUIStyle buttonClose = macEditor ? "WinBtnCloseMac" : "WinBtnClose";
            public static GUIStyle buttonMin = macEditor ? "WinBtnMinMac" : "WinBtnClose";
            public static GUIStyle buttonMax = macEditor ? "WinBtnMaxMac" : "WinBtnMax";

            // Title Bar Button when window is not focused (OSX only)
            public static GUIStyle buttonInactive = "WinBtnInactiveMac";
        }

        public ContainerWindow()
        {
            m_PixelRect = new Rect(0, 0, 400, 300);
        }

        internal void __internalAwake()
        {
            hideFlags = HideFlags.DontSave;
        }

        internal ShowMode showMode => (ShowMode)m_ShowMode;

        private string m_WindowID = null;
        internal string windowID
        {
            get
            {
                if (String.IsNullOrEmpty(m_WindowID))
                    return GetWindowID();
                return m_WindowID;
            }

            set
            {
                m_WindowID = value;
            }
        }

        internal static bool IsPopup(ShowMode mode)
        {
            return (ShowMode.PopupMenu == mode);
        }

        internal bool isPopup => IsPopup((ShowMode)m_ShowMode);

        internal void ShowPopup()
        {
            ShowPopupWithMode(ShowMode.PopupMenu, true);
        }

        internal void ShowTooltip()
        {
            ShowPopupWithMode(ShowMode.Tooltip, false);
        }

        internal void ShowPopupWithMode(ShowMode mode, bool giveFocus)
        {
            m_ShowMode = (int)mode;
            Internal_Show(m_PixelRect, m_ShowMode, m_MinSize, m_MaxSize);
            if (m_RootView)
                m_RootView.SetWindowRecurse(this);
            Internal_SetTitle(m_Title);
            Save();
            //  only set focus if mode is a popupMenu.
            Internal_BringLiveAfterCreation(true, giveFocus, false);

            // Fit window to screen - needs to be done after bringing the window live
            position = FitWindowRectToScreen(m_PixelRect, true, false);
            rootView.position = new Rect(0, 0, Mathf.Ceil(m_PixelRect.width), Mathf.Ceil(m_PixelRect.height));
            rootView.Reflow();
        }

        static Color skinBackgroundColor => EditorGUIUtility.isProSkin ? Color.gray.RGBMultiplied(0.3f).AlphaMultiplied(0.5f) : Color.gray.AlphaMultiplied(0.32f);

        // Show the editor window.
        public void Show(ShowMode showMode, bool loadPosition, bool displayImmediately, bool setFocus)
        {
            if (showMode == ShowMode.AuxWindow)
                showMode = ShowMode.Utility;

            if (showMode == ShowMode.Utility || IsPopup(showMode))
                m_DontSaveToLayout = true;

            m_ShowMode = (int)showMode;

            // Load previous position/size
            if (!isPopup)
                Load(loadPosition);

            var initialMaximizedState = m_Maximized;

            Internal_Show(m_PixelRect, m_ShowMode, m_MinSize, m_MaxSize);

            // Tell the main view its now in this window (quick hack to get platform-specific code to move its views to the right window)
            if (m_RootView)
                m_RootView.SetWindowRecurse(this);
            Internal_SetTitle(m_Title);

            SetBackgroundColor(skinBackgroundColor);

            Internal_BringLiveAfterCreation(displayImmediately, setFocus, initialMaximizedState);

            // Window could be killed by now in user callbacks...
            if (!this)
                return;

            // Fit window to screen - needs to be done after bringing the window live
            position = FitWindowRectToScreen(m_PixelRect, true, false);
            rootView.position = new Rect(0, 0, Mathf.Ceil(m_PixelRect.width), Mathf.Ceil(m_PixelRect.height));

            rootView.Reflow();

            // save position right away
            Save();

            // Restore the initial maximized state since Internal_BringLiveAfterCreation might not be reflected right away and Save() might alter it.
            m_Maximized = initialMaximizedState;
        }

        public void OnEnable()
        {
            if (m_RootView)
                m_RootView.Initialize(this);
            SetBackgroundColor(skinBackgroundColor);
        }

        public void SetMinMaxSizes(Vector2 min, Vector2 max)
        {
            m_MinSize = min;
            m_MaxSize = max;
            Rect r = position;
            Rect r2 = r;
            r2.width = Mathf.Clamp(r.width, min.x, max.x);
            r2.height = Mathf.Clamp(r.height, min.y, max.y);
            if (r2.width != r.width || r2.height != r.height)
                position = r2;
            Internal_SetMinMaxSizes(min, max);
        }

        internal void InternalCloseWindow()
        {
            Save();
            if (m_RootView)
            {
                if (m_RootView is GUIView)
                    ((GUIView)m_RootView).RemoveFromAuxWindowList();
                DestroyImmediate(m_RootView, true);
                m_RootView = null;
            }

            DestroyImmediate(this, true);
        }

        public void Close()
        {
            Save();

            // Guard against destroy window or window in the process of being destroyed.
            if (this && m_WindowPtr.m_IntPtr != IntPtr.Zero)
                InternalClose();
            DestroyImmediate(this, true);
        }

        internal bool IsNotDocked()
        {
            return ( // hallelujah

                (m_ShowMode == (int)ShowMode.Utility || m_ShowMode == (int)ShowMode.AuxWindow) ||
                (m_ShowMode == (int)ShowMode.MainWindow && rootView is HostView) ||
                (rootView is SplitView &&
                    rootView.children.Length == 1 &&
                    rootView.children[0] is DockArea &&
                    ((DockArea)rootView.children[0]).m_Panes.Count == 1)
            );
        }

        internal string GetWindowID()
        {
            HostView v = rootView as HostView;

            if (v == null && rootView is SplitView && rootView.children.Length > 0)
                v = rootView.children[0] as HostView;

            if (v == null)
                return rootView.GetType().ToString();

            if (rootView.children.Length > 0)
                return (m_ShowMode == (int)ShowMode.Utility || m_ShowMode == (int)ShowMode.AuxWindow) ? v.actualView.GetType().ToString()
                    : ((DockArea)rootView.children[0]).m_Panes[0].GetType().ToString();

            return v.actualView.GetType().ToString();
        }

        public bool IsMainWindow()
        {
            return m_ShowMode == (int)ShowMode.MainWindow && m_DontSaveToLayout == false;
        }

        internal void SaveGeometry()
        {
            string ID = windowID;
            if (String.IsNullOrEmpty(ID))
                return;

            // save position/size
            EditorPrefs.SetFloat(ID + "x", m_PixelRect.x);
            EditorPrefs.SetFloat(ID + "y", m_PixelRect.y);
            EditorPrefs.SetFloat(ID + "w", m_PixelRect.width);
            EditorPrefs.SetFloat(ID + "h", m_PixelRect.height);
            EditorPrefs.SetBool(ID + "z", m_Maximized);
        }

        public void Save()
        {
            m_Maximized = IsZoomed();
            SaveGeometry();
        }

        internal void LoadGeometry(bool loadPosition)
        {
            string ID = windowID;
            if (String.IsNullOrEmpty(ID))
                return;

            // get position/size
            Rect p = m_PixelRect;
            if (loadPosition)
            {
                p.x = EditorPrefs.GetFloat(ID + "x", m_PixelRect.x);
                p.y = EditorPrefs.GetFloat(ID + "y", m_PixelRect.y);
                p.width = EditorPrefs.GetFloat(ID + "w", m_PixelRect.width);
                p.height = EditorPrefs.GetFloat(ID + "h", m_PixelRect.height);
                m_Maximized = EditorPrefs.GetBool(ID + "z");
            }
            p.width = Mathf.Min(Mathf.Max(p.width, m_MinSize.x), m_MaxSize.x);
            p.height = Mathf.Min(Mathf.Max(p.height, m_MinSize.y), m_MaxSize.y);
            m_PixelRect = p;
        }

        private void Load(bool loadPosition)
        {
            if (!IsMainWindow() && IsNotDocked())
                LoadGeometry(loadPosition);
        }

        internal void OnResize()
        {
            if (rootView == null)
                return;
            rootView.position = new Rect(0, 0, Mathf.Ceil(position.width), Mathf.Ceil(position.height));

            // save position
            Save();
        }

        // The title of the window
        public string title
        {
            get { return m_Title; }
            set { m_Title = value; Internal_SetTitle(value); }
        }

        // Array of all visible ContainerWindows, from frontmost to last
        static List<ContainerWindow> s_AllWindows = new List<ContainerWindow>();
        public static ContainerWindow[] windows
        {
            get
            {
                s_AllWindows.Clear();
                GetOrderedWindowList();
                return s_AllWindows.ToArray();
            }
        }

        internal void AddToWindowList()
        {
            s_AllWindows.Add(this);
        }

        // TODO: Handle title bar height and other things
        public Vector2 WindowToScreenPoint(Vector2 windowPoint)
        {
            Vector2 hmm = Internal_GetTopleftScreenPosition();
            return windowPoint + hmm;// + new Vector2 (position.x, position.y) + hmm;
        }

        public View rootView
        {
            get { return m_RootView; }
            set
            {
                m_RootView = value;
                m_RootView.SetWindowRecurse(this);
                m_RootView.position = new Rect(0, 0, Mathf.Ceil(position.width), Mathf.Ceil(position.height));
                m_MinSize = value.minSize;
                m_MaxSize = value.maxSize;
            }
        }

        public SplitView rootSplitView
        {
            get
            {
                if (m_ShowMode == (int)ShowMode.MainWindow && rootView && rootView.children.Length == 3)
                    return rootView.children[1] as SplitView;
                if (m_ShowMode == (int)ShowMode.MainWindow && rootView && rootView.children.Length == 2)
                    return rootView.children[0] as SplitView;

                foreach (var c in rootView.children)
                {
                    var sv = c as SplitView;
                    if (sv)
                        return sv;
                }

                return rootView as SplitView;
            }
        }

        internal string DebugHierarchy()
        {
            return rootView.DebugHierarchy(0);
        }

        internal Rect GetDropDownRect(Rect buttonRect, Vector2 minSize, Vector2 maxSize, PopupLocation[] locationPriorityOrder)
        {
            return PopupLocationHelper.GetDropDownRect(buttonRect, minSize, maxSize, this, locationPriorityOrder);
        }

        internal Rect GetDropDownRect(Rect buttonRect, Vector2 minSize, Vector2 maxSize)
        {
            return PopupLocationHelper.GetDropDownRect(buttonRect, minSize, maxSize, this);
        }

        public void HandleWindowDecorationEnd(Rect windowPosition)
        {
            // No Op
        }

        public void HandleWindowDecorationStart(Rect windowPosition)
        {
            bool hasTitleBar = (windowPosition.y == 0 && showMode != ShowMode.Utility && !isPopup);

            if (!hasTitleBar)
                return;

            bool hasWindowButtons = Mathf.Abs(windowPosition.xMax - position.width) < 2;
            if (hasWindowButtons)
            {
                GUIStyle close = Styles.buttonClose;
                GUIStyle min = Styles.buttonMin;
                GUIStyle max = Styles.buttonMax;

                if (macEditor && (GUIView.focusedView == null || GUIView.focusedView.window != this))
                    close = min = max = Styles.buttonInactive;

                BeginTitleBarButtons(windowPosition);
                if (TitleBarButton(close))
                {
                    Close();
                    GUIUtility.ExitGUI();
                }
                if (macEditor && TitleBarButton(min))
                {
                    Minimize();
                    GUIUtility.ExitGUI();
                }

                var canMaximize = m_MaxSize.x == 0 || m_MaxSize.y == 0 || m_MaxSize.x >= Screen.currentResolution.width || m_MaxSize.y >= Screen.currentResolution.height;
                EditorGUI.BeginDisabled(!canMaximize);
                if (TitleBarButton(max))
                    ToggleMaximize();
                EditorGUI.EndDisabled();
            }

            DragTitleBar(new Rect(0, 0, position.width, kTitleHeight));
        }

        private void BeginTitleBarButtons(Rect windowPosition)
        {
            m_ButtonCount = 0;
            m_TitleBarWidth = windowPosition.width;
        }

        private bool TitleBarButton(GUIStyle style)
        {
            var buttonRect = new Rect(m_TitleBarWidth - kButtonWidth * ++m_ButtonCount - kBorderSize, kButtonTop, kButtonWidth, kButtonHeight);
            return GUI.Button(buttonRect, GUIContent.none, style);
        }

        // Snapping windows
        private static Vector2 s_LastDragMousePos;
        private float startDragDpi;

        // Indicates that we are using the native title bar caption dragging.
        private bool m_DraggingNativeTitleBarCaption = false;

        private void DragTitleBar(Rect titleBarRect)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Repaint:
                    if (m_DraggingNativeTitleBarCaption)
                        m_DraggingNativeTitleBarCaption = false;
                    EditorGUIUtility.AddCursorRect(titleBarRect, MouseCursor.Arrow);
                    break;
                case EventType.MouseDown:
                    // If the mouse is inside the title bar rect, we say that we're the hot control
                    if (titleBarRect.Contains(evt.mousePosition) && GUIUtility.hotControl == 0 && evt.button == 0)
                    {
                        if (Application.platform == RuntimePlatform.WindowsEditor)
                        {
                            Event.current.Use();
                            m_DraggingNativeTitleBarCaption = true;
                            SendCaptionEvent(m_DraggingNativeTitleBarCaption);
                        }
                        else
                        {
                            GUIUtility.hotControl = id;
                            Event.current.Use();
                            s_LastDragMousePos = evt.mousePosition;
                            startDragDpi = GUIUtility.pixelsPerPoint;
                            Unsupported.SetAllowCursorLock(false, Unsupported.DisallowCursorLockReasons.SizeMove);
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (m_DraggingNativeTitleBarCaption)
                        break;

                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                        Unsupported.SetAllowCursorLock(true, Unsupported.DisallowCursorLockReasons.SizeMove);
                    }
                    break;
                case EventType.MouseDrag:
                    if (m_DraggingNativeTitleBarCaption)
                        break;

                    if (GUIUtility.hotControl == id)
                    {
                        Vector2 mousePos = evt.mousePosition;
                        if (startDragDpi != GUIUtility.pixelsPerPoint)
                        {
                            // We ignore this mouse event when changing screens in multi monitor setups with
                            // different dpi scalings as funky things might/will happen
                            startDragDpi = GUIUtility.pixelsPerPoint;
                            s_LastDragMousePos = mousePos;
                        }
                        else
                        {
                            Vector2 movement = mousePos - s_LastDragMousePos;

                            float minimumDelta = 1.0f / GUIUtility.pixelsPerPoint;

                            if (Mathf.Abs(movement.x) >= minimumDelta || Mathf.Abs(movement.y) >= minimumDelta)
                            {
                                Rect dragPosition = position;
                                dragPosition.x += movement.x;
                                dragPosition.y += movement.y;
                                position = dragPosition;

                                GUI.changed = true;
                            }
                        }
                    }
                    break;
            }
        }
    }
} //namespace
