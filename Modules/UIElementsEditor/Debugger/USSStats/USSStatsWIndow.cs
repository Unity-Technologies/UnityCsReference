// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.UIElements.Experimental.USSStats
{
    [InitializeOnLoad]
    [EditorWindowTitle(title = "USS Stats")]
    class USSStatsWindow : EditorWindow
    {
        public const string k_WindowPath = "Window/UI Toolkit/USS Stats";
        public static readonly string WindowName = L10n.Tr("UI Toolkit USS Stats");

        static USSStatsWindow()
        {
            Menu.menuChanged += AddMenuItem;
        }

        private static void AddMenuItem()
        {
            Menu.menuChanged -= AddMenuItem;
            if (UIToolkitProjectSettings.enableUSSStats)
                Menu.AddMenuItem(k_WindowPath, "", false, 3010, OpenAndInspectWindow, null);
        }

        USSStatsImpl m_DebuggerImpl;

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

        private static USSStatsWindow CreateDebuggerWindow()
        {
            var window = CreateInstance<USSStatsWindow>();
            window.titleContent = EditorGUIUtility.TextContent(WindowName);
            return window;
        }

        void OnEnable()
        {
            if (m_DebuggerImpl == null)
                m_DebuggerImpl = new USSStatsImpl();
            m_DebuggerImpl.Initialize(this, rootVisualElement);
        }

        void OnDisable()
        {
            m_DebuggerImpl.OnDisable();
        }
    }
}
