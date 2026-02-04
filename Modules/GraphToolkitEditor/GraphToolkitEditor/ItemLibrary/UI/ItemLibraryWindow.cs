// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor;
using Unity.GraphToolkit.InternalBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.ItemLibrary.Editor
{
    /// <summary>
    /// Editor Window for the Item Library.
    /// </summary>
    [UnityRestricted]
    internal class ItemLibraryWindow : EditorWindow
    {
        // TODO VladN remove when moving this to editor module and use actual ShowMode enum
        enum ShowMode
        {
            PopupMenu,
            NormalWindow
        }

        const string k_StylesheetName = "ItemLibrary/ItemLibraryWindow.uss";
        const string k_WindowClassName = "unity-item-library-window";
        const string k_PopupWindowClassName = k_WindowClassName + "--popup";

        const string k_DefaultStatusText = "'Double-click' or hit 'Enter' to select an entry.";

        // internal accessors for tests
        internal ItemLibraryControl ItemLibraryControl => m_Control;
        internal ItemLibraryLibrary Library => ItemLibraryControl.Library;

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

        ItemLibraryControl m_Control;
        Vector2 m_OriginalMousePos;
        Rect m_OriginalWindowPos;
        Rect m_NewWindowPos;
        bool m_IsMouseDownOnResizer;
        bool m_IsMouseDownOnTitle;
        VisualElement m_FocusedBefore;
        Vector2 m_Size;
        string m_InitialStatusText;
        bool m_IgnoreFocusLost;

        EditorWindow m_ParentWindow;

        static void UpdateDefaultSize(ItemLibraryLibrary library)
        {
            var isPreviewPanelVisible = library != null && library.IsPreviewPanelVisible();
            var defaultWidth = ItemLibraryControl.DefaultSearchPanelWidth;
            if (isPreviewPanelVisible)
                defaultWidth += ItemLibraryControl.DefaultDetailsPanelWidth;
            s_DefaultSize = new Vector2(defaultWidth, ItemLibraryControl.DefaultHeight);
        }

        /// <summary>
        /// Shows a popup <see cref="ItemLibraryWindow"/> with a list of searchable items.
        /// </summary>
        /// <param name="host">The window to host this window in.</param>
        /// <param name="library">The <see cref="Library"/> to browse with this window.</param>
        /// <param name="displayPosition">The position where to display the window.</param>
        /// <param name="typeHandleInfos">The <see cref="TypeHandleInfos"/> to use for this view.</param>
        internal static ItemLibraryWindow Show(
            EditorWindow host,
            ItemLibraryLibrary library,
            Vector2 displayPosition,
            TypeHandleInfos typeHandleInfos)
        {
            UpdateDefaultSize(library);

            var rect = new Rect(displayPosition, s_DefaultSize);

            return Show(host, library, rect, typeHandleInfos);
        }

        /// <summary>
        /// Shows a popup <see cref="ItemLibraryWindow"/> restricted to a host window.
        /// </summary>
        /// <param name="host">The window to host this window in.</param>
        /// <param name="library">The <see cref="ItemLibraryLibrary"/> to browse with this window.</param>
        /// <param name="rect">The position and size of the window to create.</param>
        /// <param name="typeHandleInfos">The <see cref="TypeHandleInfos"/> to use for this view.</param>
        internal static ItemLibraryWindow Show(
            EditorWindow host,
            ItemLibraryLibrary library,
            Rect rect,
            TypeHandleInfos typeHandleInfos)
        {

            var window = CreateInstance<ItemLibraryWindow>();
            window.position = GetRectStartingInWindow(rect, host);
            window.minSize = MinSize;
            window.Initialize(library, ShowMode.PopupMenu, typeHandleInfos, host);
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
            pos = default(ItemLibraryWindowAlignment).AlignPosition(pos, rect.size);

            return new Rect(pos, rect.size);
        }

        void Initialize(ItemLibraryLibrary library, ShowMode showMode, TypeHandleInfos typeHandleInfos, EditorWindow host)
        {
            rootVisualElement.AddPackageStylesheet(k_StylesheetName);
            rootVisualElement.AddToClassList(k_WindowClassName);
            rootVisualElement.AddToClassList("unity-theme-env-variables");
            rootVisualElement.EnableInClassList(k_PopupWindowClassName, showMode == ShowMode.PopupMenu);
            m_ParentWindow = host;

            rootVisualElement.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

            SetupLibrary(library, typeHandleInfos);
        }

        void OnExecuteCommand(ExecuteCommandEvent e)
        {
            if (e.commandName == EventCommandNamesBridge.UndoRedoPerformed)
            {
                OnItemChosen(null);
                Close();
            }
        }

        void SetupLibrary(ItemLibraryLibrary library, TypeHandleInfos typeHandleInfos)
        {
            UpdateDefaultSize(library);

            m_Control ??= new ItemLibraryControl(typeHandleInfos);
            StatusBarText = m_InitialStatusText ?? k_DefaultStatusText;
            m_Control.Setup(library);
            m_Control.detailsPanelWidthChanged += OnDetailsPanelWidthChanged;

            m_Control.TitleContainer.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
            m_Control.TitleContainer.RegisterCallback<MouseUpEvent>(OnTitleMouseUp);

            m_Control.Resizer.RegisterCallback<MouseDownEvent>(OnResizerMouseDown);
            m_Control.Resizer.RegisterCallback<MouseUpEvent>(OnResizerMouseUp);

            m_Control.itemChosen += OnItemChosen;

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
                m_Control.TitleContainer.UnregisterCallback<MouseDownEvent>(OnTitleMouseDown);
                m_Control.TitleContainer.UnregisterCallback<MouseUpEvent>(OnTitleMouseUp);

                m_Control.Resizer.UnregisterCallback<MouseDownEvent>(OnResizerMouseDown);
                m_Control.Resizer.UnregisterCallback<MouseUpEvent>(OnResizerMouseUp);
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

            m_FocusedBefore = rootVisualElement.panel.focusController.focusedElement as VisualElement;

            m_Control.TitleContainer.RegisterCallback<MouseMoveEvent>(OnTitleMouseMove);
            m_Control.TitleContainer.RegisterCallback<KeyDownEvent>(OnControlKeyDown);
            m_Control.TitleContainer.CaptureMouse();
        }

        void OnTitleMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (!m_Control.TitleContainer.HasMouseCapture())
                return;

            FinishMove();
        }

        void FinishMove()
        {
            m_Control.TitleContainer.UnregisterCallback<MouseMoveEvent>(OnTitleMouseMove);
            m_Control.TitleContainer.UnregisterCallback<KeyDownEvent>(OnControlKeyDown);
            m_Control.TitleContainer.ReleaseMouse();
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

            m_FocusedBefore = rootVisualElement.panel.focusController.focusedElement as VisualElement;

            m_Control.Resizer.RegisterCallback<MouseMoveEvent>(OnResizerMouseMove);
            m_Control.Resizer.RegisterCallback<KeyDownEvent>(OnControlKeyDown);
            m_Control.Resizer.CaptureMouse();
        }

        void OnResizerMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (!m_Control.Resizer.HasMouseCapture())
                return;

            FinishResize();
        }

        void FinishResize()
        {
            m_Control.Resizer.UnregisterCallback<MouseMoveEvent>(OnResizerMouseMove);
            m_Control.Resizer.UnregisterCallback<KeyDownEvent>(OnControlKeyDown);
            m_Control.Resizer.ReleaseMouse();
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
            if (m_IgnoreFocusLost)
                return;

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
                rootVisualElement.schedule.Execute(() => OnItemChosen(null)).ExecuteLater(0);
            }
        }

        void OnDestroy()
        {
            m_IgnoreFocusLost = true;
            m_ParentWindow?.Focus();
        }

        public class TestAccess
        {
            readonly ItemLibraryWindow m_ItemLibraryWindow;
            public TestAccess(ItemLibraryWindow itemLibraryWindow)
            {
                m_ItemLibraryWindow = itemLibraryWindow;
            }

            public void InvokeItemChosen(ItemLibraryItem item) => m_ItemLibraryWindow.OnItemChosen(item);
        }
    }
}
