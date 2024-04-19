// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    /*
     Note that content of PopupWindow do not survive assembly reloading because it derives from interface PopupWindowContent.
     E.g use it for short lived content where closing on lost focus is ok.
     */
    public abstract class PopupWindowContent
    {
        public EditorWindow editorWindow { get; internal set; }

        public virtual void OnGUI(Rect rect) { }
        public virtual VisualElement CreateGUI() => null;

        public virtual Vector2 GetWindowSize()
        {
            return new Vector2(200, 200);
        }

        public virtual void OnOpen() {}
        public virtual void OnClose() {}
    }

    public class PopupWindow : EditorWindow
    {
        const string k_UssClassName = "unity-popup-window-root";
        const string k_InvalidSizeTemplate = "Invalid content size: {0}. Specify dimensions greater than zero in <i>CreateGUI</i>.";

        public static readonly string invalidSizeLabelUssClassName = "unity-popup-window__invalid-size-label";
        static readonly Vector2 k_DefaultWindowSize = new(10, 10);

        PopupWindowContent m_WindowContent;
        Vector2 m_LastWantedSize;
        Rect m_ActivatorRect;
        PopupLocation[] m_LocationPriorityOrder;
        static double s_LastClosedTime;
        static Rect s_LastActivatorRect;
        VisualElement m_UserContent;

        bool UseIMGUI => !UseUIToolkit;
        bool UseUIToolkit => m_UserContent != null;

        internal PopupWindow()
        {
        }

        public static void Show(Rect activatorRect, PopupWindowContent windowContent)
        {
            Show(activatorRect, windowContent, null);
        }

        internal static void Show(Rect activatorRect, PopupWindowContent windowContent, PopupLocation[] locationPriorityOrder)
        {
            Show(activatorRect, windowContent, locationPriorityOrder, ShowMode.PopupMenu);
        }

        // Shown on top of any previous windows
        internal static void Show(Rect activatorRect, PopupWindowContent windowContent, PopupLocation[] locationPriorityOrder, ShowMode showMode)
        {
            // If we already have a popup window showing this type of content, then just close
            // the existing one.
            var existingPopup = FindExistingPopupWindow(windowContent);
            if (existingPopup != null)
            {
                existingPopup.CloseWindow();
                return;
            }

            if (ShouldShowWindow(activatorRect))
            {
                PopupWindow win = CreateInstance<PopupWindow>();
                if (win != null)
                {
                    win.Init(activatorRect, windowContent, locationPriorityOrder, showMode, true);
                }
                if (Event.current != null)
                {
                    EditorGUIUtility.ExitGUI(); // Needed to prevent GUILayout errors on OSX
                }
            }
        }

        internal static bool ShouldShowWindow(Rect activatorRect)
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

        internal void Init(Rect activatorRect, PopupWindowContent windowContent, PopupLocation[] locationPriorityOrder, ShowMode showMode, bool giveFocus)
        {
            hideFlags = HideFlags.DontSave;
            wantsMouseMove = true;
            m_WindowContent = windowContent;
            m_WindowContent.editorWindow = this;
            m_WindowContent.OnOpen();
            m_ActivatorRect = GUIUtility.GUIToScreenRect(activatorRect);
            m_LastWantedSize = Vector2.zero;
            m_LocationPriorityOrder = locationPriorityOrder;

            ShowAsDropDown(m_ActivatorRect, k_DefaultWindowSize, locationPriorityOrder, showMode, giveFocus);

            if (UseUIToolkit)
                SetupPopupAutoSize();
            else
                FitWindowToContent();
        }

        internal void OnGUI()
        {
            if (UseIMGUI)
            {
                FitWindowToContent();
                Rect windowRect = new Rect(0, 0, position.width, position.height);
                m_WindowContent.OnGUI(windowRect);
                GUI.Label(windowRect, GUIContent.none, "grey_border");
                FitWindowToContent();
            }
        }

        void CreateGUI()
        {
            m_UserContent = m_WindowContent?.CreateGUI();
            if (m_UserContent != null)
            {
                var infiniteCanvas = new VisualElement { style = { position = Position.Absolute } };
                infiniteCanvas.AddToClassList(k_UssClassName);
                rootVisualElement.Add(infiniteCanvas);
                infiniteCanvas.Add(m_UserContent);
            }
        }

        void FitWindowToContent()
        {
            if (m_WindowContent == null)
                return;
            Vector2 wantedSize = m_WindowContent.GetWindowSize();
            SetWindowSize(wantedSize);
        }

        void SetWindowSize(Vector2 size)
        {
            if (this && m_LastWantedSize != size)
            {
                m_LastWantedSize = size;
                Rect screenRect = m_Parent.window.GetDropDownRect(m_ActivatorRect, size, size, m_LocationPriorityOrder);
                minSize = maxSize = new Vector2(screenRect.width, screenRect.height);
                position = screenRect;
            }
        }

        void SetupPopupAutoSize()
        {
            if (UseUIToolkit && rootVisualElement.panel is Panel panel)
            {
                panel.ValidateLayout();
                var contentSize = m_UserContent.layout.size;
                if (contentSize.x <= 0f || contentSize.y <= 0f)
                {
                    var invalidSizeMessage = string.Format(k_InvalidSizeTemplate, contentSize);
                    AddInvalidSizeLabel(invalidSizeMessage);
                    panel.ValidateLayout(); // measure again to get the size of the label
                }

                var canvasSize = m_UserContent.parent.layout.size;
                SetWindowSize(canvasSize);
                m_UserContent.RegisterCallback<GeometryChangedEvent>(OnContentGeometryChanged);
            }
        }

        void AddInvalidSizeLabel(string text)
        {
            var invalidSizeMessage = new TextElement
            {
                text = text,
                enableRichText = true
            };
            invalidSizeMessage.AddToClassList(invalidSizeLabelUssClassName);
            m_UserContent.Add(invalidSizeMessage);
        }

        void OnContentGeometryChanged(GeometryChangedEvent evt)
        {
            var size = evt.newRect.size;

            // Schedule the size change to avoid multiple layout events in the same frame
            rootVisualElement.schedule.Execute(() => { SetWindowSize(size); });
        }

        void CloseWindow()
        {
            Close();
        }

        protected virtual void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CloseWindow;
        }

        protected virtual void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= CloseWindow;

            s_LastClosedTime = EditorApplication.timeSinceStartup;
            CloseContent();
        }

        private protected void CloseContent()
        {
            m_WindowContent?.OnClose();
            m_UserContent?.UnregisterCallback<GeometryChangedEvent>(OnContentGeometryChanged);
            m_UserContent = null;
        }

        static PopupWindow FindExistingPopupWindow(PopupWindowContent windowContent)
        {
            var existingWindows = Resources.FindObjectsOfTypeAll(typeof(PopupWindow));
            if (existingWindows != null && existingWindows.Length > 0)
            {
                var existingPopup = existingWindows[0] as PopupWindow;
                if (existingPopup != null && existingPopup.m_WindowContent != null && windowContent != null)
                {
                    if (existingPopup.m_WindowContent.GetType() == windowContent.GetType())
                    {
                        return existingPopup;
                    }
                }
            }

            return null;
        }
    }
}
