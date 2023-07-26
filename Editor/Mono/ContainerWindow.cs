// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.StyleSheets;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine.Pool;
using UnityEngine.Scripting;

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

        internal bool m_IsMppmCloneWindow;

        internal bool m_DontSaveToLayout = false;
        private bool m_HasUnsavedChanges = false;
        private List<EditorWindow> m_UnsavedEditorWindows;

        private int m_ButtonCount;
        private float m_TitleBarWidth;

        const float kTitleHeight = 24f;
        internal const float kButtonWidth = 16f, kButtonHeight = 16f;

        static internal bool macEditor => Application.platform == RuntimePlatform.OSXEditor;

        static internal bool s_Modal = false;
        private static ContainerWindow s_MainWindow;
        static internal ContainerWindow mainWindow { get => s_MainWindow; }

        static readonly string s_ContextMenuID = "UnityEditor.UIElements.EditorMenuExtensions+ContextMenu";

        private static class Styles
        {
            public static float borderSize => macEditor ? osxBorderSize : winBorderSize;
            public static float buttonMargin => macEditor ? osxBorderMargin : winBorderMargin;

            private static SVC<float> winBorderSize = new SVC<float>("--container-window-buttons-right-margin-win");
            private static SVC<float> osxBorderSize = new SVC<float>("--container-window-buttons-right-margin-osx");
            private static SVC<float> winBorderMargin = new SVC<float>("--container-window-button-left-right-margin-win");
            private static SVC<float> osxBorderMargin = new SVC<float>("--container-window-button-left-right-margin-osx");
        }
        static internal float buttonHorizontalSpace => (kButtonWidth + Styles.buttonMargin * 2f);
        static internal float buttonStackWidth => Styles.borderSize;
        public ContainerWindow()
        {
            m_PixelRect = new Rect(0, 0, 400, 300);
            m_UnsavedEditorWindows = new List<EditorWindow>();
        }

        internal void __internalAwake()
        {
            hideFlags = HideFlags.DontSave;
        }

        private void OnDestroy()
        {
            Internal_Destroy();
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
            try
            {
                if (mode == ShowMode.MainWindow)
                    throw new ArgumentException("Cannot create more than one main window.");

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
            catch
            {
                DestroyImmediate(this, true);
                throw;
            }
        }

        private static readonly Color lightSkinColor = new Color(0.541f, 0.541f, 0.541f, 1.0f);
        private static readonly Color darkSkinColor = new Color(0.098f, 0.098f, 0.098f, 1.0f);
        static Color skinBackgroundColor => EditorGUIUtility.isProSkin ? darkSkinColor : lightSkinColor;

        // Show the editor window.
        public void Show(ShowMode showMode, bool loadPosition, bool displayImmediately, bool setFocus)
        {
            try
            {
                if (showMode == ShowMode.MainWindow && s_MainWindow && s_MainWindow != this)
                    throw new InvalidOperationException("Trying to create a second main window from layout when one already exists.");

                bool useMousePos = showMode == ShowMode.AuxWindow;
                if (showMode == ShowMode.AuxWindow)
                    showMode = ShowMode.Utility;

                if (showMode == ShowMode.Utility
                    || showMode == ShowMode.ModalUtility
                    || showMode == ShowMode.AuxWindow
                    || IsPopup(showMode))
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

                if (showMode == ShowMode.MainWindow)
                    s_MainWindow = this;

                // Fit window to screen - needs to be done after bringing the window live
                FitWindowToScreen(useMousePos);

                rootView.Reflow();

                // save position right away
                Save();

                // Restore the initial maximized state since Internal_BringLiveAfterCreation might not be reflected right away and Save() might alter it.
                m_Maximized = initialMaximizedState;
            }
            catch
            {
                DestroyImmediate(this, true);
                throw;
            }
        }

        internal void FitWindowToScreen(bool useMousePos)
        {
            position = FitWindowRectToScreen(m_PixelRect, true, useMousePos);
            if (rootView)
                rootView.position = new Rect(0, 0, GUIUtility.RoundToPixelGrid(m_PixelRect.width), GUIUtility.RoundToPixelGrid(m_PixelRect.height));
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

        static internal bool CanCloseAll(bool includeMainWindow)
        {
            using (var pooledObject = ListPool<EditorWindow>.Get(out List<EditorWindow> unsaved))
            {
                foreach (var window in windows)
                {
                    if (includeMainWindow || window.showMode != ShowMode.MainWindow)
                    {
                        unsaved.AddRange(window.m_UnsavedEditorWindows);
                    }
                }

                if (unsaved.Count > 0)
                    return AskToClose(unsaved);

                return true;
            }
        }

        private bool CanClose()
        {
            if (Application.isHumanControllingUs && hasUnsavedChanges)
            {
                return AskToClose(m_UnsavedEditorWindows);
            }

            return true;
        }

        internal static bool CanClose(EditorWindow dockedTab)
        {
            if (Application.isHumanControllingUs && dockedTab.hasUnsavedChanges)
            {
                var unsaved = new List<EditorWindow>() { dockedTab };
                return AskToClose(unsaved);
            }

            return true;
        }

        internal bool CanCloseAllExcept(EditorWindow editorWindow)
        {
            if (!Application.isHumanControllingUs)
                return true;

            using (var pooledObject = ListPool<EditorWindow>.Get(out List<EditorWindow> unsaved))
            {
                foreach (var w in m_UnsavedEditorWindows)
                {
                    if (w != editorWindow)
                        unsaved.Add(w);
                }

                if (unsaved.Count > 0)
                    return AskToClose(unsaved);
            }

            return true;
        }

        private static bool AskToClose(List<EditorWindow> allUnsaved)
        {
            Debug.Assert(Application.isHumanControllingUs && allUnsaved.Count > 0);

            const int kSave = 0;
            const int kCancel = 1;
            const int kDiscard = 2;

            int option = 1; // Cancel

            if (allUnsaved.Count == 1)
            {
                var title = allUnsaved[0].titleContent.text;

                option = EditorUtility.DisplayDialogComplex((string.IsNullOrEmpty(title) ? "" : (title + " - ")) + L10n.Tr("Unsaved Changes Detected"),
                    allUnsaved[0].saveChangesMessage,
                    L10n.Tr("Save"),
                    L10n.Tr("Cancel"),
                    L10n.Tr("Discard"));
            }
            else
            {
                var savedChangesBuilder = new StringBuilder();

                int last = allUnsaved.Count - 1;
                for (int i = 0; i < last; ++i)
                {
                    savedChangesBuilder.Append(allUnsaved[i].saveChangesMessage);
                    savedChangesBuilder.Append('\n');
                }
                savedChangesBuilder.Append(allUnsaved[last]);

                option = EditorUtility.DisplayDialogComplex(L10n.Tr("Unsaved Changes Detected"),
                    savedChangesBuilder.ToString(),
                    L10n.Tr("Save All"),
                    L10n.Tr("Cancel"),
                    L10n.Tr("Discard All"));
            }

            try
            {
                switch (option)
                {
                    case kSave:
                        bool areAllSaved = true;
                        foreach (var w in allUnsaved)
                        {
                            w.SaveChanges();
                            areAllSaved &= !w.hasUnsavedChanges;
                        }
                        return areAllSaved;
                    case kDiscard:
                        foreach (var w in allUnsaved)
                            w.DiscardChanges();
                        break;
                    case kCancel:
                        break;
                    default:
                        Debug.LogError("Unrecognized option.");
                        break;
                }

                return option != kCancel;
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(L10n.Tr("Save Changes Failed"),
                    ex.Message,
                    L10n.Tr("OK"));

                return false;
            }
        }

        // Closes the Window _without prompting_
        public void Close()
        {
            // Because this is a UnityObject, DestroyImmediate _will_ nullify the this pointer.
            // This can guard against double-close
            if (this == null)
                return;

            Save();

            if (s_MainWindow == this)
                s_MainWindow = null;

            if (m_RootView)
            {
                DestroyImmediate(m_RootView, true);
                m_RootView = null;
            }

            DestroyImmediate(this, true);
            EditorWindow.UpdateWindowMenuListing();
        }

        [RequiredByNativeCode]
        private void RequestCloseSentByNativeCode()
        {
            if (CanClose())
            {
                Close();
            }
        }

        [RequiredByNativeCode]
        internal bool IsMultiplayerClone()
        {
            return m_IsMppmCloneWindow;
        }

        private static List<EditorWindow> FindUnsavedChanges(View view)
        {
            var unsavedChanges = new List<EditorWindow>();

            foreach (View v in view.allChildren)
            {
                switch (v)
                {
                    case DockArea dockArea:
                        foreach (var windowClose in dockArea.m_Panes)
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

        internal bool IsNotDocked()
        {
            return ( // hallelujah

                (m_ShowMode == (int)ShowMode.Utility || m_ShowMode == (int)ShowMode.AuxWindow || m_ShowMode == (int)ShowMode.PopupMenu) ||
                (m_ShowMode == (int)ShowMode.MainWindow && rootView is HostView) ||
                (rootView is SplitView &&
                    rootView.children.Length == 1 &&
                    rootView.children[0] is DockArea &&
                    ((DockArea)rootView.children[0]).m_Panes.Count == 1)
            );
        }

        internal string GetWindowID()
        {
            if (!rootView)
                return string.Empty;

            HostView v = rootView as HostView;

            if (v == null && rootView is SplitView && rootView.children.Length > 0)
                v = rootView.children[0] as HostView;

            if (v == null || !v.actualView)
                return rootView.GetType().ToString();

            if (rootView.children.Length > 0)
            {
                DockArea dockArea = null;

                foreach (var child in rootView.children)
                {
                    if (child is DockArea)
                    {
                        dockArea = (DockArea)child;
                        break;
                    }
                }

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
            return m_ShowMode == (int)ShowMode.MainWindow && m_DontSaveToLayout == false;
        }

        internal void SaveGeometry()
        {
            string ID = windowID;
            if (string.IsNullOrEmpty(ID) || IsValidContextMenu())
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

        bool IsValidContextMenu()
        {
            return string.Equals(s_ContextMenuID, windowID);
        }

        internal void LoadGeometry(bool loadPosition)
        {
            string ID = windowID;
            // Check for invalid ID and validate context menu to avoid soft locks on invalid positions UW-105
            if (string.IsNullOrEmpty(ID) || IsValidContextMenu())
                return;

            // get position/size
            Rect p = m_PixelRect;
            if (loadPosition)
            {
                // Use the current mouse position as the 'default' position if we cant
                // load the position from our saved preferences. This allows the newly created
                // window to pop up on the same monitor as the main window when a saved position
                // couldn't be loaded rather than defaulting to monitor '0'
                Vector2 mousePos = Editor.GetCurrentMousePosition();

                p.x = EditorPrefs.GetFloat(ID + "x", mousePos.x);
                p.y = EditorPrefs.GetFloat(ID + "y", mousePos.y);
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
    }
} //namespace
