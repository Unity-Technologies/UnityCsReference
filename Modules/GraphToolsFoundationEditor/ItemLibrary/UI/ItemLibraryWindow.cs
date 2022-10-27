// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// Editor Window for the Item Library.
    /// </summary>
    class ItemLibraryWindow : EditorWindow
    {
        // TODO VladN remove when moving this to editor module and use actual ShowMode enum
        enum ShowMode
        {
            PopupMenu,
            NormalWindow
        }

        const string k_StylesheetName = "StyleSheets/ItemLibrary/ItemLibraryWindow.uss";
        const string k_WindowClassName = "unity-item-library-window";
        const string k_PopupWindowClassName = k_WindowClassName + "--popup";

        const string k_DefaultStatusText = "'Double-click' or hit 'Enter' to select an entry.";

        // internal accessors for tests
        internal ItemLibraryControl_Internal ItemLibraryControl_Internal => m_Control;
        internal ItemLibraryLibrary_Internal Library_Internal => ItemLibraryControl_Internal.Library_Internal;

        /// <summary>
        /// The text to display in the Status bar (at the bottom of the window).
        /// </summary>
        public string StatusBarText
        {
            get => m_Control != null ? m_Control.StatusBarText : m_InitialStatusText;
            set
            {
                if (m_Control != null)
                    m_Control.StatusBarText = value;
                else
                    m_InitialStatusText = value;
            }
        }

        /// <summary>
        /// Whether the window should automatically close when it loses focus.
        /// </summary>
        /// <remarks>Doesn't prevent closing when an item is chosen or escape is pressed.</remarks>
        public bool CloseOnFocusLost { get; set; } = true;

        static Vector2 s_DefaultSize = new Vector2(300, 300);

        /// <summary>
        /// Raised when an item is selected.
        /// </summary>
        public event Action<ItemLibraryItem> itemChosen;

        /// <summary>
        /// The minimum size windows should have.
        /// </summary>
        public static Vector2 MinSize => new Vector2(120, 130);

        internal event Action<ItemLibraryAnalyticsEvent_Internal> AnalyticsEventTriggered_Internal
        {
            add => m_Control.analyticsEventTriggered_Internal += value;
            remove => m_Control.analyticsEventTriggered_Internal -= value;
        }

        ItemLibraryControl_Internal m_Control;
        Vector2 m_OriginalMousePos;
        Rect m_OriginalWindowPos;
        Rect m_NewWindowPos;
        bool m_IsMouseDownOnResizer;
        bool m_IsMouseDownOnTitle;
        Focusable m_FocusedBefore;
        Vector2 m_Size;
        string m_InitialStatusText;

        static void UpdateDefaultSize(ItemLibraryLibrary_Internal library)
        {
            var isPreviewPanelVisible = library != null && library.IsPreviewPanelVisible();
            var defaultWidth = ItemLibraryControl_Internal.DefaultSearchPanelWidth;
            if (isPreviewPanelVisible)
                defaultWidth += ItemLibraryControl_Internal.DefaultDetailsPanelWidth;
            s_DefaultSize = new Vector2(defaultWidth, ItemLibraryControl_Internal.DefaultHeight);
        }

        /// <summary>
        /// Shows a popup <see cref="ItemLibraryWindow"/> with a list of searchable items.
        /// </summary>
        /// <param name="host">The window to host this window in.</param>
        /// <param name="library">The <see cref="Library_Internal"/> to browse with this window.</param>
        /// <param name="displayPosition">The position where to display the window.</param>
        internal static ItemLibraryWindow Show_Internal(
            EditorWindow host,
            ItemLibraryLibrary_Internal library,
            Vector2 displayPosition)
        {
            UpdateDefaultSize(library);

            var rect = new Rect(displayPosition, s_DefaultSize);

            return Show_Internal(host, library, rect);
        }

        /// <summary>
        /// Shows a popup <see cref="ItemLibraryWindow"/> restricted to a host window.
        /// </summary>
        /// <param name="host">The window to host this window in.</param>
        /// <param name="library">The <see cref="ItemLibraryLibrary_Internal"/> to browse with this window.</param>
        /// <param name="rect">The position and size of the window to create.</param>
        internal static ItemLibraryWindow Show_Internal(
            EditorWindow host,
            ItemLibraryLibrary_Internal library,
            Rect rect)
        {

            var window = CreateInstance<ItemLibraryWindow>();
            window.position = GetRectStartingInWindow(rect, host);
            window.minSize = MinSize;
            window.Initialize(library, ShowMode.PopupMenu);
            window.ShowPopup();
            window.Focus();
            return window;
        }

        static Rect GetRectStartingInWindow(Rect rect, EditorWindow host)
        {
            var pos = rect.position;
            pos.x = Mathf.Max(0, Mathf.Min(host.position.size.x, pos.x));
            pos.y = Mathf.Max(0, Mathf.Min(host.position.size.y, pos.y));
            pos += host.position.position;
            pos = default(ItemLibraryWindowAlignment_Internal).AlignPosition_Internal(pos, rect.size);

            return new Rect(pos, rect.size);
        }

        void Initialize(ItemLibraryLibrary_Internal library, ShowMode showMode)
        {
            rootVisualElement.AddStylesheetResource_Internal(k_StylesheetName);
            rootVisualElement.AddToClassList(k_WindowClassName);
            rootVisualElement.AddToClassList("unity-theme-env-variables");
            rootVisualElement.EnableInClassList(k_PopupWindowClassName, showMode == ShowMode.PopupMenu);

            SetupLibrary(library);
        }

        void SetupLibrary(ItemLibraryLibrary_Internal library)
        {
            UpdateDefaultSize(library);

            m_Control ??= new ItemLibraryControl_Internal();
            StatusBarText = m_InitialStatusText ?? k_DefaultStatusText;
            m_Control.Setup(library);
            m_Control.detailsPanelWidthChanged_Internal += OnDetailsPanelWidthChanged;

            m_Control.TitleContainer_Internal.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
            m_Control.TitleContainer_Internal.RegisterCallback<MouseUpEvent>(OnTitleMouseUp);

            m_Control.Resizer_Internal.RegisterCallback<MouseDownEvent>(OnResizerMouseDown);
            m_Control.Resizer_Internal.RegisterCallback<MouseUpEvent>(OnResizerMouseUp);

            m_Control.itemChosen_Internal += OnItemChosen;

            var root = rootVisualElement;
            root.style.flexGrow = 1;
            root.Add(m_Control);
        }

        void OnDetailsPanelWidthChanged(float widthDelta)
        {
            m_Size = position.size + widthDelta * Vector2.right;
            position = new Rect(position.position, m_Size);
            Repaint();
        }

        void OnDisable()
        {
            if (m_Control != null)
            {
                m_Control.TitleContainer_Internal.UnregisterCallback<MouseDownEvent>(OnTitleMouseDown);
                m_Control.TitleContainer_Internal.UnregisterCallback<MouseUpEvent>(OnTitleMouseUp);

                m_Control.Resizer_Internal.UnregisterCallback<MouseDownEvent>(OnResizerMouseDown);
                m_Control.Resizer_Internal.UnregisterCallback<MouseUpEvent>(OnResizerMouseUp);
            }
        }

        void OnTitleMouseDown(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            m_IsMouseDownOnTitle = true;

            m_NewWindowPos = position;
            m_OriginalWindowPos = position;
            m_OriginalMousePos = evt.mousePosition;

            m_FocusedBefore = rootVisualElement.panel.focusController.focusedElement;

            m_Control.TitleContainer_Internal.RegisterCallback<MouseMoveEvent>(OnTitleMouseMove);
            m_Control.TitleContainer_Internal.RegisterCallback<KeyDownEvent>(OnControlKeyDown);
            m_Control.TitleContainer_Internal.CaptureMouse();
        }

        void OnTitleMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (!m_Control.TitleContainer_Internal.HasMouseCapture())
                return;

            FinishMove();
        }

        void FinishMove()
        {
            m_Control.TitleContainer_Internal.UnregisterCallback<MouseMoveEvent>(OnTitleMouseMove);
            m_Control.TitleContainer_Internal.UnregisterCallback<KeyDownEvent>(OnControlKeyDown);
            m_Control.TitleContainer_Internal.ReleaseMouse();
            m_FocusedBefore?.Focus();
            m_IsMouseDownOnTitle = false;
        }

        void OnTitleMouseMove(MouseMoveEvent evt)
        {
            var delta = evt.mousePosition - m_OriginalMousePos;

            // TODO Temporary fix for Visual Scripting 1st drop. Find why position.position is 0,0 on MacOs in MouseMoveEvent
            // Bug occurs with Unity 2019.2.0a13
            m_NewWindowPos = new Rect(position.position + delta, position.size);
            Repaint();
        }

        void OnResizerMouseDown(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            m_IsMouseDownOnResizer = true;

            m_NewWindowPos = position;
            m_OriginalWindowPos = position;
            m_OriginalMousePos = evt.mousePosition;

            m_FocusedBefore = rootVisualElement.panel.focusController.focusedElement;

            m_Control.Resizer_Internal.RegisterCallback<MouseMoveEvent>(OnResizerMouseMove);
            m_Control.Resizer_Internal.RegisterCallback<KeyDownEvent>(OnControlKeyDown);
            m_Control.Resizer_Internal.CaptureMouse();
        }

        void OnResizerMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (!m_Control.Resizer_Internal.HasMouseCapture())
                return;

            FinishResize();
        }

        void FinishResize()
        {
            m_Control.Resizer_Internal.UnregisterCallback<MouseMoveEvent>(OnResizerMouseMove);
            m_Control.Resizer_Internal.UnregisterCallback<KeyDownEvent>(OnControlKeyDown);
            m_Control.Resizer_Internal.ReleaseMouse();
            m_FocusedBefore?.Focus();
            m_IsMouseDownOnResizer = false;
        }

        void OnResizerMouseMove(MouseMoveEvent evt)
        {
            var delta = evt.mousePosition - m_OriginalMousePos;
            m_Size = m_OriginalWindowPos.size + delta;

            if (m_Size.x < minSize.x)
                m_Size.x = minSize.x;
            if (m_Size.y < minSize.y)
                m_Size.y = minSize.y;

            // TODO Temporary fix for Visual Scripting 1st drop. Find why position.position is 0,0 on MacOs in MouseMoveEvent
            // Bug occurs with Unity 2019.2.0a13
            m_NewWindowPos = new Rect(position.position, m_Size);
            Repaint();
        }

        void OnControlKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                if (m_IsMouseDownOnTitle)
                {
                    FinishMove();
                    position = m_OriginalWindowPos;
                }
                else if (m_IsMouseDownOnResizer)
                {
                    FinishResize();
                    position = m_OriginalWindowPos;
                }
            }
        }

        void OnGUI()
        {
            if ((m_IsMouseDownOnTitle || m_IsMouseDownOnResizer) && Event.current.type == EventType.Layout)
                position = m_NewWindowPos;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && hasFocus)
                OnItemChosen(null);
        }

        void OnItemChosen(ItemLibraryItem item)
        {
            itemChosen?.Invoke(item);
            Close();
        }

        void OnLostFocus()
        {
            if (m_IsMouseDownOnTitle)
            {
                FinishMove();
            }
            else if (m_IsMouseDownOnResizer)
            {
                FinishResize();
            }

            // OnLostFocus can be called again by Close()
            // See: https://fogbugz.unity3d.com/f/cases/1004504/
            if (CloseOnFocusLost && hasFocus)
            {
                OnItemChosen(null);
            }
        }
    }
}
