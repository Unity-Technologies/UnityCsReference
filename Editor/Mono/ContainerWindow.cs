// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.StyleSheets;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using System.Linq;

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
        private bool m_HasUnsavedChanges = false;
        private List<EditorWindow> m_UnsavedEditorWindows;

        private int m_ButtonCount;
        private float m_TitleBarWidth;

        const float kTitleHeight = 24f;
        internal const float kButtonWidth = 16f, kButtonHeight = 16f;

        static internal bool macEditor => Application.platform == RuntimePlatform.OSXEditor;
        static internal bool linuxEditor => Application.platform == RuntimePlatform.LinuxEditor;

        static internal bool s_Modal = false;

        private static class Styles
        {
            // Title Bar Buttons (Non)
            public static GUIStyle buttonMin = "WinBtnMinMac";
            public static GUIStyle buttonClose = macEditor ? "WinBtnCloseMac" : "WinBtnClose";
            public static GUIStyle buttonMax = macEditor ? "WinBtnMaxMac" : "WinBtnMax";
            public static GUIStyle buttonRestore = macEditor ? "WinBtnRestoreMac" : "WinBtnRestore";

            public static float borderSize => macEditor ? osxBorderSize : winBorderSize;
            public static float buttonMargin => macEditor ? osxBorderMargin : winBorderMargin;

            public static SVC<float> buttonTop = new SVC<float>("--container-window-button-top-margin");

            private static SVC<float> winBorderSize = new SVC<float>("--container-window-buttons-right-margin-win");
            private static SVC<float> osxBorderSize = new SVC<float>("--container-window-buttons-right-margin-osx");
            private static SVC<float> winBorderMargin = new SVC<float>("--container-window-button-left-right-margin-win");
            private static SVC<float> osxBorderMargin = new SVC<float>("--container-window-button-left-right-margin-osx");
        }

        private const float kButtonCountOSX = 0;
        private const float kButtonCountWin = 2;
        static internal float buttonHorizontalSpace => (kButtonWidth + Styles.buttonMargin * 2f);
        static internal float buttonStackWidth => buttonHorizontalSpace * (macEditor || linuxEditor ? kButtonCountOSX : kButtonCountWin) + Styles.borderSize;

        public ContainerWindow()
        {
            m_PixelRect = new Rect(0, 0, 400, 300);
            m_UnsavedEditorWindows = new List<EditorWindow>();
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
            rootView.position = new Rect(0, 0, GUIUtility.RoundToPixelGrid(m_PixelRect.width), GUIUtility.RoundToPixelGrid(m_PixelRect.height));
            rootView.Reflow();
        }

        private static readonly Color lightSkinColor = new Color(0.541f, 0.541f, 0.541f, 1.0f);
        private static readonly Color darkSkinColor = new Color(0.098f, 0.098f, 0.098f, 1.0f);
        static Color skinBackgroundColor => EditorGUIUtility.isProSkin ? darkSkinColor : lightSkinColor;

        // Show the editor window.
        public void Show(ShowMode showMode, bool loadPosition, bool displayImmediately, bool setFocus)
        {
            bool useMousePos = showMode == ShowMode.AuxWindow;
            if (showMode == ShowMode.AuxWindow)
                showMode = ShowMode.Utility;

            if (showMode == ShowMode.Utility || showMode == ShowMode.ModalUtility || IsPopup(showMode))
                m_DontSaveToLayout = true;

            m_ShowMode = (int)showMode;

            // Load previous position/size
            if (!isPopup)
                Load(loadPosition);
            if (useMousePos)
                LoadInCurrentMousePosition();

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
            position = FitWindowRectToScreen(m_PixelRect, true, useMousePos);
            rootView.position = new Rect(0, 0, GUIUtility.RoundToPixelGrid(m_PixelRect.width), GUIUtility.RoundToPixelGrid(m_PixelRect.height));

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

        internal bool InternalRequestClose()
        {
            if (hasUnsavedChanges)
            {
                return PrivateRequestClose(m_UnsavedEditorWindows);
            }

            return true;
        }

        internal bool InternalRequestClose(EditorWindow dockedTab)
        {
            if (dockedTab.hasUnsavedChanges)
            {
                var unsaved = new List<EditorWindow>() { dockedTab };
                return PrivateRequestClose(unsaved);
            }

            return true;
        }

        private bool PrivateRequestClose(List<EditorWindow> allUnsaved)
        {
            Debug.Assert(allUnsaved.Count > 0);

            const int kSave = 0;
            const int kCancel = 1;
            const int kDiscard = 2;

            int option = 1; // Cancel

            if (allUnsaved.Count == 1)
            {
                option = EditorUtility.DisplayDialogComplex(L10n.Tr("Unsaved Changes Detected"),
                    allUnsaved.First().saveChangesMessage,
                    L10n.Tr("Save"),
                    L10n.Tr("Cancel"),
                    L10n.Tr("Discard"));
            }
            else
            {
                string unsavedChangesMessage = string.Join("\n", allUnsaved.Select(v => v.saveChangesMessage).ToArray());

                option = EditorUtility.DisplayDialogComplex(L10n.Tr("Unsaved Changes Detected"),
                    unsavedChangesMessage,
                    L10n.Tr("Save All"),
                    L10n.Tr("Cancel"),
                    L10n.Tr("Discard All"));
            }

            switch (option)
            {
                case kSave:
                    foreach (var w in allUnsaved)
                        w.SaveChanges();
                    break;
                case kCancel:
                case kDiscard:
                    break;
                default:
                    Debug.LogError("Unrecognized option.");
                    break;
            }

            return option != kCancel;
        }

        internal void InternalCloseWindow()
        {
            Save();
            if (m_RootView)
            {
                DestroyImmediate(m_RootView, true);
                m_RootView = null;
            }

            DestroyImmediate(this, true);
        }

        private static List<EditorWindow> FindUnsavedChanges(View view)
        {
            var unsavedChanges = new List<EditorWindow>();

            foreach (View v in view.allChildren)
            {
                switch (v)
                {
                    case DockArea dockArea:
                        foreach (var windowClose in dockArea.m_Panes.OfType<EditorWindow>())
                            if (windowClose.hasUnsavedChanges)
                                unsavedChanges.Add(windowClose);
                        break;
                    case HostView hostView:
                        if (hostView.actualView?.hasUnsavedChanges ?? false)
                            unsavedChanges.Add(hostView.actualView);
                        break;
                    default:
                        break;
                }
            }

            return unsavedChanges;
        }

        public void UnsavedStateChanged()
        {
            m_UnsavedEditorWindows = FindUnsavedChanges(rootView);
            hasUnsavedChanges = m_UnsavedEditorWindows.Count > 0;
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

            if (v == null || !v.actualView)
                return rootView.GetType().ToString();

            if (rootView.children.Length > 0)
            {
                var dockArea = rootView.children.FirstOrDefault(c => c is DockArea) as DockArea;
                if (dockArea && dockArea.m_Panes.Count > 0)
                {
                    return (m_ShowMode == (int)ShowMode.Utility || m_ShowMode == (int)ShowMode.AuxWindow) ? v.actualView.GetType().ToString()
                        : dockArea.m_Panes[0].GetType().ToString();
                }
            }

            return v.actualView.GetType().ToString();
        }

        public bool IsMainWindow()
        {
            if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Master)
                return m_ShowMode == (int)ShowMode.MainWindow && m_DontSaveToLayout == false;
            return false;
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

        internal void LoadInCurrentMousePosition()
        {
            Vector2 mousePos = Editor.GetCurrentMousePosition();

            Rect p = m_PixelRect;
            p.x = mousePos.x;
            p.y = mousePos.y;
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

            // Depending on the context, GUIUtility.pixelsPerPoint isn't reliable at this point, so we must not round.
            // Anyway the backend is responsible of providing a window size that is aligned with the pixel grid, so it
            // shouldn't be necessary. In the past position.width and position.height were rounded. When moving a window
            // from a monitor that had a given scaling to another monitor with a different scaling, this could cause the
            // original pixelsPerPoint to be used. As a result, black borders could appear for the first frame.
            rootView.position = new Rect(0, 0, position.width, position.height);

            // save position
            Save();
        }

        internal void OnMove()
        {
            if (IsMainWindow() && this.rootView is MainView)
            {
                MainView mv = (MainView)this.rootView;
                foreach (HostView view in mv.allChildren.OfType<HostView>())
                {
                    view.OnMainWindowMove();
                }
            }
        }

        // The title of the window, including unsaved changes markings, if any.
        public string displayedTitle { get; private set; }

        private void UpdateTitle()
        {
            displayedTitle = hasUnsavedChanges ? m_Title + "*" : m_Title;
            Internal_SetTitle(displayedTitle);
        }

        // The title of the window
        public string title
        {
            get
            {
                return m_Title;
            }
            set
            {
                if (m_Title != value)
                {
                    m_Title = value;
                    UpdateTitle();
                }
            }
        }

        public bool hasUnsavedChanges
        {
            get
            {
                return m_HasUnsavedChanges;
            }
            private set
            {
                if (m_HasUnsavedChanges != value)
                {
                    m_HasUnsavedChanges = value;
                    Internal_SetHasUnsavedChanges(value);
                    UpdateTitle();
                }
            }
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
            if (!macEditor && !linuxEditor)
            {
                bool hasTitleBar = (windowPosition.y == 0 && (showMode != ShowMode.Utility && showMode != ShowMode.MainWindow) && !isPopup);

                if (!hasTitleBar)
                    return;

                bool hasWindowButtons = Mathf.Abs(windowPosition.xMax - position.width) < 2;
                if (hasWindowButtons)
                {
                    GUIStyle min = Styles.buttonMin;
                    GUIStyle close = Styles.buttonClose;
                    GUIStyle maxOrRestore = maximized ? Styles.buttonRestore : Styles.buttonMax;

                    BeginTitleBarButtons(windowPosition);
                    if (TitleBarButton(close))
                    {
                        if (InternalRequestClose())
                        {
                            Close();
                            GUIUtility.ExitGUI();
                        }
                    }

                    var canMaximize = m_MaxSize.x == 0 || m_MaxSize.y == 0 || m_MaxSize.x >= Screen.currentResolution.width || m_MaxSize.y >= Screen.currentResolution.height;
                    EditorGUI.BeginDisabled(!canMaximize);
                    if (TitleBarButton(maxOrRestore))
                        ToggleMaximize();
                    EditorGUI.EndDisabled();
                }

                DragTitleBar(new Rect(0, 0, position.width, kTitleHeight));
            }
        }

        private void BeginTitleBarButtons(Rect windowPosition)
        {
            m_ButtonCount = 0;
            m_TitleBarWidth = windowPosition.width;
        }

        private bool TitleBarButton(GUIStyle style)
        {
            var buttonRect = new Rect(m_TitleBarWidth - Styles.borderSize - (buttonHorizontalSpace * ++m_ButtonCount), Styles.buttonTop, kButtonWidth, kButtonHeight);
            var guiView = rootView as GUIView;
            if (guiView == null)
            {
                var splitView = rootView as SplitView;
                if (splitView != null)
                    guiView = splitView.children.Length > 0 ? splitView.children[0] as GUIView : null;
            }
            if (guiView != null)
                guiView.MarkHotRegion(GUIClip.UnclipToWindow(buttonRect));

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
                        Event.current.Use();
                        m_DraggingNativeTitleBarCaption = true;
                        SendCaptionEvent(m_DraggingNativeTitleBarCaption);
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
