// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Text;
using System.Threading;
using UnityEditor.Web;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Asset Store", icon = "Asset Store")]
    internal class AssetStoreWindow : EditorWindow , IHasCustomMenu
    {
        public bool initialized { get { return (null != webView); } }

        public static void OpenURL(string url)
        {
            AssetStoreWindow window = Init();
            bool shouldDefer = !window.initialized;

            window.InvokeJSMethod("document.AssetStore", "openURL", url);
            AssetStoreContext.GetInstance().initialOpenURL = url;
            if (shouldDefer)
                // Ugly hack - help, asset store team!
                window.ScheduleOpenURL(TimeSpan.FromSeconds(3));
        }

        // Use this for initialization
        public static AssetStoreWindow Init()
        {
            AssetStoreWindow window = EditorWindow.GetWindow<AssetStoreWindow>(typeof(SceneView));
            window.SetMinMaxSizes();
            window.Show();
            return window;
        }

        private void SetMinMaxSizes()
        {
            this.minSize = new Vector2(400, 100);
            this.maxSize = new Vector2(2048, 2048);
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Reload"), false, Reload);
        }

        // Forces user logout
        public void Logout()
        {
            InvokeJSMethod("document.AssetStore.login", "logout");
        }

        // Reloads the web view
        public void Reload()
        {
            m_CurrentSkin = EditorGUIUtility.skinIndex;
            m_IsDocked = docked;
            webView.Reload();
        }

        public void OnLoadError(string url)
        {
            if (!webView)
                return;

            if (m_IsOffline)
            {
                Debug.LogErrorFormat("Unexpected error: Failed to load offline Asset Store page (url={0})", url);
                return;
            }

            m_IsOffline = true;
            webView.LoadFile(AssetStoreUtils.GetOfflinePath());
        }

        /// <summary>
        /// This is called by WebView right after the script execution environment
        /// of a page has been initialized but before any of its code is actually
        /// executed.  This allows us to inject our own "windows.context" object for
        /// the A$ scripts.
        /// </summary>
        public void OnInitScripting()
        {
            SetScriptObject();
        }

        /// <summary>
        /// This is called by WebView when a link is to be opened in a new window.
        /// Opens the link in the system browser.
        /// </summary>
        /// <param name="url">URL of the link.</param>
        public void OnOpenExternalLink(string url)
        {
            if (url.StartsWith("http://") ||
                url.StartsWith("https://"))
                Application.OpenURL(url);
        }

        public void OnEnable()
        {
            SetMinMaxSizes();
            titleContent = GetLocalizedTitleContent();
            AssetStoreUtils.RegisterDownloadDelegate(this);
        }

        public void OnDisable()
        {
            AssetStoreUtils.UnRegisterDownloadDelegate(this);
        }

        public void OnDownloadProgress(string id, string message, ulong bytes, ulong total)
        {
            InvokeJSMethod("document.AssetStore.pkgs", "OnDownloadProgress", id, message, bytes, total);
        }

        public void OnGUI()
        {
            var webViewRect = GUIClip.Unclip(new Rect(0, 0, position.width, position.height));

            // If we haven't initialize our embedded webview yet,
            // do so now.
            if (!webView)
                InitWebView(webViewRect);

            // TODO workaround for Mac
            if (m_RepeatedShow-- > 0)
            {
                Refresh();
            }

            // If there's been a layout change, update our
            if (Event.current.type == EventType.Repaint)
            {
                webView.SetHostView(m_Parent);
                webView.SetSizeAndPosition((int)webViewRect.x, (int)webViewRect.y, (int)webViewRect.width, (int)webViewRect.height);

                // Sync skin.
                if (m_CurrentSkin != EditorGUIUtility.skinIndex)
                {
                    m_CurrentSkin = EditorGUIUtility.skinIndex;
                    InvokeJSMethod("document.AssetStore", "refreshSkinIndex");
                }

                UpdateDockStatusIfNeeded();
            }
        }

        void Update()
        {
            if (!m_ShouldRetryInitialURL)
                return;

            m_ShouldRetryInitialURL = false;
            string url = AssetStoreContext.GetInstance().initialOpenURL;
            if (!string.IsNullOrEmpty(url))
                OpenURL(url);
        }

        public void UpdateDockStatusIfNeeded()
        {
            if (m_IsDocked != docked)
            {
                m_IsDocked = docked;

                if (scriptObject != null)
                {
                    AssetStoreContext.GetInstance().docked = docked;
                    InvokeJSMethod("document.AssetStore", "updateDockStatus");
                }
            }
        }

        public void ToggleMaximize()
        {
            maximized = !maximized;
            Refresh();
            SetFocus(true);
        }

        public void Refresh()
        {
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

        public void OnDestroy()
        {
            DestroyImmediate(webView);
        }

        private void InitWebView(Rect webViewRect)
        {
            m_CurrentSkin = EditorGUIUtility.skinIndex;
            m_IsDocked = docked;
            m_IsOffline = false;

            if (!webView)
            {
                var x = (int)webViewRect.x;
                var y = (int)webViewRect.y;
                int width = (int)webViewRect.width;
                int height = (int)webViewRect.height;

                // Create WebView.
                webView = ScriptableObject.CreateInstance<WebView>();
                webView.InitWebView(m_Parent, x, y, width, height, false);
                webView.hideFlags = HideFlags.HideAndDontSave;
                webView.AllowRightClickMenu(true);

                // Sync focus.
                if (hasFocus)
                    SetFocus(true);
            }

            // Direct WebView event callbacks to us.
            webView.SetDelegateObject(this);

            // Load the A$ startup file.
            webView.LoadFile(AssetStoreUtils.GetLoaderPath());
        }

        private void CreateScriptObject()
        {
            if (scriptObject != null)
                return;

            scriptObject = ScriptableObject.CreateInstance<WebScriptObject>();
            scriptObject.hideFlags = HideFlags.HideAndDontSave;
            scriptObject.webView = webView;
        }

        private void SetScriptObject()
        {
            if (!webView)
                return;

            CreateScriptObject();

            //We need to leave unityScriptObject name instead of webScriptObject because it's
            //been deployed on the asset store servers.
            webView.DefineScriptObject("window.unityScriptObject", scriptObject);
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

            if (webView)
            {
                // If we're being focused, sync up our current native parent window and force
                // the browser window to be visible.  We're doing this here rather than in
                // OnBecameVisible since this way it will also work when maximizing our
                // EditorWindow(which goes through some really whacky logic).  We'll be
                // setting this state more often than otherwise, but that doesn't really
                // hurt.
                if (value)
                {
                    webView.SetHostView(m_Parent);
                    webView.Show();
                    if (RuntimePlatform.OSXEditor == Application.platform)
                        // TODO investigate why a Show() so close to a reparenting causes issues on Mac
                        m_RepeatedShow = 5;
                }

                webView.SetFocus(value);
            }

            m_SyncingFocus = false;
        }

        void ScheduleOpenURL(TimeSpan timeout)
        {
            ThreadPool.QueueUserWorkItem(delegate {
                    Thread.Sleep(timeout);
                    m_ShouldRetryInitialURL = true;
                });
        }

        internal WebView webView;
        internal WebScriptObject scriptObject;

        private int m_CurrentSkin;
        private bool m_IsDocked;
        private bool m_IsOffline;
        private bool m_SyncingFocus;
        private int m_RepeatedShow;
        private bool m_ShouldRetryInitialURL;
    }
}
