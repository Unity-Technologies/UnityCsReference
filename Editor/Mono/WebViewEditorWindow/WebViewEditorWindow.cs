// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor.Web
{
    internal abstract class WebViewEditorWindow : EditorWindow , IHasCustomMenu
    {
        [SerializeField]
        protected string m_InitialOpenURL;

        [SerializeField]
        protected string m_GlobalObjectTypeName;

        internal WebScriptObject scriptObject;

        protected bool m_SyncingFocus;
        protected bool m_HasDelayedRefresh;

        // Use EditorWindow.GetWindow<WebViewEditorWindow> to get/create an instance of this class;
        protected WebViewEditorWindow()
        {
            m_HasDelayedRefresh = false;
        }

        // if isFileUrl set to true, it will prepend your path with file://
        // else it will prepend nothing and expects the user to set the http:// or
        // https:// or file:// prefix like a big kid.
        public static T Create<T>(string title,
            string sourcesPath,
            int minWidth,
            int minHeight,
            int maxWidth,
            int maxHeight) where T : WebViewEditorWindow
        {
            var window = CreateInstance<T>();
            window.m_GlobalObjectTypeName = typeof(T).FullName;
            CreateWindowCommon<T>(window as T, title, sourcesPath, minWidth, minHeight, maxWidth, maxHeight);
            window.Show();
            return window;
        }

        public static T CreateUtility<T>(string title,
            string sourcesPath,
            int minWidth,
            int minHeight,
            int maxWidth,
            int maxHeight) where T : WebViewEditorWindow
        {
            var window = CreateInstance<T>();
            window.m_GlobalObjectTypeName = typeof(T).FullName;
            CreateWindowCommon<T>(window as T, title, sourcesPath, minWidth, minHeight, maxWidth, maxHeight);
            window.ShowUtility();
            return window;
        }

        // if isFileUrl set to true, it will prepend your path with file://
        // else it will prepend nothing and expects the user to set the http:// or
        // https:// or file:// prefix like a big kid.
        public static  T CreateBase<T>(string title,
            string sourcesPath,
            int minWidth,
            int minHeight,
            int maxWidth,
            int maxHeight) where T : WebViewEditorWindow
        {
            var window = GetWindow<T>(title);
            CreateWindowCommon<T>(window as T, title, sourcesPath, minWidth, minHeight, maxWidth, maxHeight);
            window.Show();
            return window;
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Reload"), false, Reload);
            if (Unsupported.IsDeveloperBuild())
                menu.AddItem(new GUIContent("About"), false, About);
        }

        // Reloads the web view
        public void Reload()
        {
            if (webView == null)
                return;
            webView.Reload();
        }

        // About the web view
        public void About()
        {
            if (webView == null)
                return;
            webView.LoadURL("chrome://version");
        }

        public void OnLoadError(string url)
        {
            if (!webView)
                return;
        }

        public virtual void OnLocationChanged(string url)
        {
        }

        public void ToggleMaximize()
        {
            maximized = !maximized;
            Refresh();
            SetFocus(true);
        }

        virtual public void Init()
        {
        }

        public void OnGUI()
        {
            var m_WebViewRect = GUIClip.Unclip(new Rect(0, 0, position.width, position.height));

            GUILayout.BeginArea(m_WebViewRect);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Loading...", EditorStyles.label);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // TODO workaround for Mac
            if (Event.current.type == EventType.Repaint)
            {
                if (m_HasDelayedRefresh)
                {
                    Refresh();
                    m_HasDelayedRefresh = false;
                }
            }

            // If we haven't initialize our embedded webview yet,
            // do so now.
            if (m_InitialOpenURL != null)
            {
                if (!webView)
                {
                    InitWebView(m_WebViewRect);
                }
                if (Event.current.type == EventType.Repaint)
                {
                    webView.SetHostView(m_Parent);
                    webView.SetSizeAndPosition((int)m_WebViewRect.x, (int)m_WebViewRect.y, (int)m_WebViewRect.width, (int)m_WebViewRect.height);
                }
            }
        }

        public void OnBatchMode()
        {
            var m_WebViewRect = GUIClip.Unclip(new Rect(0, 0, position.width, position.height));

            // If we haven't initialize our embedded webview yet,
            // do so now.
            if (m_InitialOpenURL != null)
            {
                if (!webView)
                {
                    InitWebView(m_WebViewRect);
                }
            }
        }

        public void Refresh()
        {
            if (webView == null)
                return;
            webView.Hide();
            webView.Show();
        }

        public void OnFocus()
        {
            SetFocus(true);
        }

        public void OnLostFocus()
        {
            SetFocus(false);
        }

        public virtual void OnEnable()
        {
            Init();
        }

        public void OnBecameInvisible()
        {
            if (!webView)
                return;

            // Either our tab put into the background or we've been removed from our
            // dock and are about to be moved to some other dock.  Whatever the case,
            // unparent our WebView native window from the current dock's native window.
            // This will implicitly hide the window.
            webView.SetHostView(null);
        }

        public virtual void OnDestroy()
        {
            if (webView != null)
            {
                DestroyImmediate(webView);
            }
        }

        System.Timers.Timer m_PostLoadTimer;
        const int k_RepaintTimerDelay =  30;

        void DoPostLoadTask()
        {
            EditorApplication.update -= DoPostLoadTask;
            RepaintImmediately();
        }

        void RaisePostLoadCondition(object obj, System.Timers.ElapsedEventArgs args)
        {
            m_PostLoadTimer.Stop();
            m_PostLoadTimer = null;
            EditorApplication.update += DoPostLoadTask;
        }

        protected void LoadUri()
        {
            if (m_InitialOpenURL.StartsWith("http"))
            {
                webView.LoadURL(m_InitialOpenURL);
                m_PostLoadTimer = new System.Timers.Timer(k_RepaintTimerDelay);
                m_PostLoadTimer.Elapsed += RaisePostLoadCondition;
                m_PostLoadTimer.Enabled = true;
            }
            else if (m_InitialOpenURL.StartsWith("file"))
            {
                webView.LoadFile(m_InitialOpenURL);
            }
            else
            {
                // Load the startup file.
                var filePath = Path.Combine(System.Uri.EscapeUriString(Path.Combine(EditorApplication.applicationContentsPath, "Resources")), m_InitialOpenURL);
                webView.LoadFile(filePath);
            }
        }

        virtual protected void InitWebView(Rect m_WebViewRect)
        {
            if (!webView)
            {
                var x = (int)m_WebViewRect.x;
                var y = (int)m_WebViewRect.y;
                var width = (int)m_WebViewRect.width;
                var height = (int)m_WebViewRect.height;

                // Create WebView.
                webView = CreateInstance<WebView>();
                webView.InitWebView(m_Parent, x, y, width, height, false);
                webView.hideFlags = HideFlags.HideAndDontSave;

                // Sync focus.
                SetFocus(hasFocus);
            }

            // Direct WebView event callbacks to us.
            webView.SetDelegateObject(this);

            LoadUri();
            SetFocus(true);
        }

        virtual public void OnInitScripting()
        {
            SetScriptObject();
        }

        protected void NotifyVisibility(bool visible)
        {
            if (webView == null)
            {
                return;
            }

            string  eventCmd = "document.dispatchEvent(new CustomEvent('showWebView',{ detail: { visible:";

            eventCmd += (visible ? "true" : "false");
            eventCmd += "}, bubbles: true, cancelable: false }));";

            webView.ExecuteJavascript(eventCmd);
        }

        protected virtual void LoadPage()
        {
            if (!webView)
                return;

            NotifyVisibility(false);
            LoadUri();
            webView.Show();
        }

        protected void SetScriptObject()
        {
            if (!webView)
                return;

            CreateScriptObject();
            webView.DefineScriptObject("window.webScriptObject", scriptObject);
        }

        private static void CreateWindowCommon<T>(T window,
            string title,
            string sourcesPath,
            int minWidth,
            int minHeight,
            int maxWidth,
            int maxHeight) where T : WebViewEditorWindow
        {
            window.titleContent = new GUIContent(title);
            window.minSize = new Vector2(minWidth, minHeight);
            window.maxSize = new Vector2(maxWidth, maxHeight);
            window.m_InitialOpenURL = sourcesPath;
            window.Init();
        }

        private void CreateScriptObject()
        {
            if (scriptObject != null)
                return;

            scriptObject = CreateInstance<WebScriptObject>();
            scriptObject.hideFlags = HideFlags.HideAndDontSave;
            scriptObject.webView = webView;
        }

        private void InvokeJSMethod(string objectName, string name, params object[] args)
        {
            if (!webView)
                return;

            var scriptCodeBuffer = new StringBuilder();
            scriptCodeBuffer.Append(objectName);
            scriptCodeBuffer.Append('.');
            scriptCodeBuffer.Append(name);
            scriptCodeBuffer.Append('(');

            var isFirst = true;
            foreach (var arg in args)
            {
                if (!isFirst)
                    scriptCodeBuffer.Append(',');

                // Quote strings.  This is pretty simple-minded as we don't escape
                // things within the string.
                var isString = arg is string;
                if (isString)
                    scriptCodeBuffer.Append('"');

                scriptCodeBuffer.Append(arg);

                if (isString)
                    scriptCodeBuffer.Append('"');

                isFirst = false;
            }

            scriptCodeBuffer.Append(");");

            webView.ExecuteJavascript(scriptCodeBuffer.ToString());
        }

        private void SetFocus(bool value)
        {
            // Giving the focus to the browser's native window will cause our GuiView's
            // native window to lose focus.  If we blindly pass on our focus state to
            // the browser, we will end up making it lose focus while we try to make it
            // have focus.  However, we generally *do* want to pass on focus.
            //
            // So, what we do is to simply prevent recursion here.  Allows us to just
            // hand the focus over.

            if (m_SyncingFocus)
                return;

            m_SyncingFocus = true;

            if (webView != null)
            {
                // If we're being focused, sync up our current native parent window and force
                // the browser window to be visible.  We're doing this here rather than in
                // OnBecameVisible since this way it will also work when maximizing our
                // EditorWindow (which goes through some really whacky logic).  We'll be
                // setting this state more often than otherwise, but that doesn't really
                // hurt.
                if (value)
                {
                    webView.SetHostView(m_Parent);
                    if (Application.platform != RuntimePlatform.WindowsEditor)
                    {
                        // TODO investigate why a Show () so close to a reparenting causes issues on Mac
                        m_HasDelayedRefresh = true;
                    }
                    else
                    {
                        webView.Show();
                    }
                }
                //We have to check if the parent have the focus and also if we have the focus in the tab to
                //know if we have the application focus. The application focus is use by the CefFocusHandler
                //To know if the focus request is coming from unity editor or from cef browser. If the focus
                //came from cefBrowser we will refuse it so unity editor active window stay active.
                webView.SetApplicationFocus(m_Parent != null && m_Parent.hasFocus && hasFocus);
                webView.SetFocus(value);
            }

            m_SyncingFocus = false;
        }

        public string initialOpenUrl
        {
            set { m_InitialOpenURL = value; }
            get { return m_InitialOpenURL; }
        }

        internal abstract WebView webView
        {
            get;
            set;
        }
    }
}
