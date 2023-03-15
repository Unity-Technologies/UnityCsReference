// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.UIElements.Experimental.UILayoutDebugger
{
    [InitializeOnLoad]
    [EditorWindowTitle(title = "UI Layout Debugger")]
    class UILayoutDebuggerWindow : EditorWindow
    {
        public const string k_WindowPath = "Window/UI Toolkit/Layout Debugger";
        public static readonly string WindowName = L10n.Tr("UI Toolkit Layout Debugger");
        public static readonly string OpenWindowCommand = nameof(OpenUIElementsDebugger);

        static UILayoutDebuggerWindow()
        {
            Menu.menuChanged += AddMenuItem;
        }

        private static void AddMenuItem()
        {
            Menu.menuChanged -= AddMenuItem;
            if (UIToolkitProjectSettings.enableLayoutDebugger)
                Menu.AddMenuItem(k_WindowPath, "", false, 3010, OpenAndInspectWindow, null);
        }

        private static void OpenUIElementsDebugger()
        {
            if (CommandService.Exists(OpenWindowCommand))
                CommandService.Execute(OpenWindowCommand, CommandHint.Menu);
            else
            {
                OpenAndInspectWindow(null);
            }
        }

        LayoutPanelDebuggerImpl m_DebuggerImpl;

        public static void OpenAndInspectWindow()
        {
            OpenAndInspectWindow(null);
        }

        public static void OpenAndInspectWindow(EditorWindow window)
        {
            var debuggerWindow = CreateDebuggerWindow();
            debuggerWindow.Show();

            if (window != null)
                debuggerWindow.m_DebuggerImpl.ScheduleWindowToDebug(window);
        }

        private static UILayoutDebuggerWindow CreateDebuggerWindow()
        {
            var window = CreateInstance<UILayoutDebuggerWindow>();
            window.titleContent = EditorGUIUtility.TextContent(WindowName);
            return window;
        }

        void OnEnable()
        {
            if (m_DebuggerImpl == null)
                m_DebuggerImpl = new LayoutPanelDebuggerImpl();
            m_DebuggerImpl.Initialize(this, rootVisualElement);
        }

        void OnDisable()
        {
            m_DebuggerImpl.OnDisable();
        }
    }
}
