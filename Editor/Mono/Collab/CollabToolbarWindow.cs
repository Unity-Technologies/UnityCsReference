// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Collaboration;
using UnityEditor.Web;
using UnityEditor.Connect;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class WebViewStatic : ScriptableSingleton<WebViewStatic>
    {
        [SerializeField]
        WebView m_WebView;

        static public WebView GetWebView()
        {
            return instance.m_WebView;
        }

        static public void SetWebView(WebView webView)
        {
            instance.m_WebView = webView;
        }
    }


    internal class CollabToolbarWindow : WebViewEditorStaticWindow, IHasCustomMenu
    {
        internal override WebView webView
        {
            get {return WebViewStatic.GetWebView(); }
            set {WebViewStatic.SetWebView(value); }
        }

        private const string kWindowName = "Unity Collab Toolbar";

        private static long s_LastClosedTime;
        private static CollabToolbarWindow s_CollabToolbarWindow;

        public static bool s_ToolbarIsVisible = false;

        const int kWindowWidth = 320;
        const int kWindowHeight = 350;

        public static void CloseToolbarWindows()
        {
            // Make sure browser-side code finishes executing before we kill the window
            EditorApplication.CallDelayed(CloseToolbarWindowsImmediately, 1f);
        }

        public static void CloseToolbarWindowsImmediately()
        {
            foreach (CollabToolbarWindow window in Resources.FindObjectsOfTypeAll<CollabToolbarWindow>())
                window.Close();
        }

        [MenuItem("Window/Collab Toolbar", false /*IsValidateFunction*/, 2011, true /* IsInternalMenu */)]
        public static CollabToolbarWindow ShowToolbarWindow()
        {
            //Create a new window if it do not exist
            return GetWindow<CollabToolbarWindow>(false, kWindowName) as CollabToolbarWindow;
        }

        [MenuItem("Window/Collab Toolbar", true /*IsValidateFunction*/)]
        public static bool ValidateShowToolbarWindow()
        {
            return true;
        }

        internal static bool ShowCenteredAtPosition(Rect buttonRect)
        {
            buttonRect.x -= kWindowWidth / 2;
            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (!justClosed)
            {
                // Method may have been triggered programmatically, without a user event to consume.
                if (Event.current.type != EventType.Layout)
                {
                    Event.current.Use();
                }
                if (s_CollabToolbarWindow == null)
                    s_CollabToolbarWindow = CreateInstance<CollabToolbarWindow>() as CollabToolbarWindow;
                var windowSize = new Vector2(kWindowWidth, kWindowHeight);
                s_CollabToolbarWindow.initialOpenUrl = "file:///" + EditorApplication.userJavascriptPackagesPath + "unityeditor-collab-toolbar/dist/index.html";
                s_CollabToolbarWindow.Init();
                s_CollabToolbarWindow.ShowAsDropDown(buttonRect, windowSize);
                s_CollabToolbarWindow.OnFocus();
                return true;
            }
            return false;
        }

        // Receives HTML title
        public void OnReceiveTitle(string title)
        {
            titleContent.text = title;
        }

        public new void OnInitScripting()
        {
            base.OnInitScripting();
        }

        public override void OnEnable()
        {
            minSize = new Vector2(kWindowWidth, kWindowHeight);
            maxSize = new Vector2(kWindowWidth, kWindowHeight);
            initialOpenUrl = "file:///" + EditorApplication.userJavascriptPackagesPath + "unityeditor-collab-toolbar/dist/index.html";
            base.OnEnable();
            if (s_CollabToolbarWindow)
            {
                s_ToolbarIsVisible = true;
                NotifyVisibility(s_ToolbarIsVisible);
            }
        }

        internal void OnDisable()
        {
            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            if (s_CollabToolbarWindow)
            {
                s_ToolbarIsVisible = false;
                NotifyVisibility(s_ToolbarIsVisible);
            }
            s_CollabToolbarWindow = null;
        }

        public new void OnDestroy()
        {
            OnLostFocus();
            base.OnDestroy();
        }
    }
}
