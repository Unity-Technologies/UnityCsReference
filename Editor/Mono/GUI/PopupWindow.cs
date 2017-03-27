// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    /*
     Note that content of PopupWindow do not survive assembly reloading because it derives from interface PopupWindowContent.
     E.g use it for short lived content where closing on lost focus is ok.
     */

    public abstract class PopupWindowContent
    {
        public EditorWindow editorWindow { get; internal set; }

        public abstract void OnGUI(Rect rect);
        public virtual Vector2 GetWindowSize()
        {
            return new Vector2(200, 200);
        }

        public virtual void OnOpen() {}
        public virtual void OnClose() {}
    }

    public class PopupWindow : EditorWindow
    {
        PopupWindowContent m_WindowContent;
        Vector2 m_LastWantedSize = Vector2.zero;
        Rect m_ActivatorRect;
        static double s_LastClosedTime;
        static Rect s_LastActivatorRect;

        internal PopupWindow()
        {
        }

        public static void Show(Rect activatorRect, PopupWindowContent windowContent)
        {
            Show(activatorRect, windowContent, null);
        }

        internal static void Show(Rect activatorRect, PopupWindowContent windowContent, PopupLocationHelper.PopupLocation[] locationPriorityOrder)
        {
            Show(activatorRect, windowContent, locationPriorityOrder, ShowMode.PopupMenu);
        }

        // Shown on top of any previous windows
        internal static void Show(Rect activatorRect, PopupWindowContent windowContent, PopupLocationHelper.PopupLocation[] locationPriorityOrder, ShowMode showMode)
        {
            if (ShouldShowWindow(activatorRect))
            {
                PopupWindow win = CreateInstance<PopupWindow>();
                if (win != null)
                {
                    win.Init(activatorRect, windowContent, locationPriorityOrder, showMode);
                }
                EditorGUIUtility.ExitGUI(); // Needed to prevent GUILayout errors on OSX
            }
        }

        static bool ShouldShowWindow(Rect activatorRect)
        {
            const double kJustClickedTime = 0.2;
            bool justClosed = (EditorApplication.timeSinceStartup - s_LastClosedTime) < kJustClickedTime;
            if (!justClosed || activatorRect != s_LastActivatorRect)
            {
                s_LastActivatorRect = activatorRect;
                return true;
            }
            return false;
        }

        void Init(Rect activatorRect, PopupWindowContent windowContent, PopupLocationHelper.PopupLocation[] locationPriorityOrder)
        {
            Init(activatorRect, windowContent, locationPriorityOrder, ShowMode.PopupMenu);
        }

        void Init(Rect activatorRect, PopupWindowContent windowContent, PopupLocationHelper.PopupLocation[] locationPriorityOrder, ShowMode showMode)
        {
            hideFlags = HideFlags.DontSave;
            wantsMouseMove = true;
            m_WindowContent = windowContent;
            m_WindowContent.editorWindow = this;
            m_WindowContent.OnOpen();
            m_ActivatorRect = GUIUtility.GUIToScreenRect(activatorRect);
            ShowAsDropDown(m_ActivatorRect, m_WindowContent.GetWindowSize(), locationPriorityOrder, showMode);
        }

        internal void OnGUI()
        {
            FitWindowToContent();
            Rect windowRect = new Rect(0, 0, position.width, position.height);
            m_WindowContent.OnGUI(windowRect);
            GUI.Label(windowRect, GUIContent.none, "grey_border");
        }

        private void FitWindowToContent()
        {
            Vector2 wantedSize = m_WindowContent.GetWindowSize();
            if (m_LastWantedSize != wantedSize)
            {
                m_LastWantedSize = wantedSize;
                Rect screenRect = m_Parent.window.GetDropDownRect(m_ActivatorRect, wantedSize, wantedSize);
                minSize = maxSize = new Vector2(screenRect.width, screenRect.height);
                position = screenRect;
            }
        }

        void CloseWindow()
        {
            Close();
        }

        void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CloseWindow;
        }

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= CloseWindow;

            s_LastClosedTime = EditorApplication.timeSinceStartup;
            if (m_WindowContent != null)
                m_WindowContent.OnClose();
        }
    }
}
