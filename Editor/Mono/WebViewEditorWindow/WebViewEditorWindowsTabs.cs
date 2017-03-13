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
    // This class is used to create a window that has an array of webViews which can be toggled
    // visible/hidden. They will appear as one window (such as the services tab).
    internal class WebViewEditorWindowTabs : WebViewEditorWindow , IHasCustomMenu, ISerializationCallbackReceiver
    {
        protected object m_GlobalObject = null;

        internal WebView m_WebView;

        [SerializeField]
        private List<string> m_RegisteredViewURLs;

        [SerializeField]
        private List<WebView> m_RegisteredViewInstances;

        private Dictionary<string, WebView> m_RegisteredViews;

        // Use EditorWindow.GetWindow<WebViewEditorWindowTabs> to get/create an instance of this class;
        protected WebViewEditorWindowTabs()
        {
            m_RegisteredViewURLs = new List<string>();
            m_RegisteredViewInstances = new List<WebView>();
            m_RegisteredViews = new Dictionary<string, WebView>();
            m_GlobalObject = null;
        }

        public override void Init()
        {
            if (m_GlobalObject == null && !string.IsNullOrEmpty(m_GlobalObjectTypeName))
            {
                var instanceType = Type.GetType(m_GlobalObjectTypeName);
                if (instanceType != null)
                {
                    m_GlobalObject = ScriptableObject.CreateInstance(instanceType);
                    JSProxyMgr.GetInstance().AddGlobalObject(m_GlobalObject.GetType().Name, m_GlobalObject);
                }
            }
        }

        public override void OnDestroy()
        {
            if (webView != null)
            {
                DestroyImmediate(webView);
            }

            m_GlobalObject = null;

            foreach (WebView view in m_RegisteredViews.Values)
            {
                if (view != null)
                    DestroyImmediate(view);
            }

            m_RegisteredViews.Clear();
            m_RegisteredViewURLs.Clear();
            m_RegisteredViewInstances.Clear();
        }

        public void OnBeforeSerialize()
        {
            m_RegisteredViewURLs = new List<string>();
            m_RegisteredViewInstances = new List<WebView>();
            foreach (var kvp in m_RegisteredViews)
            {
                m_RegisteredViewURLs.Add(kvp.Key);
                m_RegisteredViewInstances.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            m_RegisteredViews = new Dictionary<string, WebView>();
            for (int i = 0; i != Math.Min(m_RegisteredViewURLs.Count, m_RegisteredViewInstances.Count); i++)
            {
                m_RegisteredViews.Add(m_RegisteredViewURLs[i], m_RegisteredViewInstances[i]);
            }
        }

        static string MakeUrlKey(string webViewUrl)
        {
            string result;
            int index = webViewUrl.IndexOf("#");
            if (index != -1)
            {
                result = webViewUrl.Substring(0, index);
            }
            else
            {
                result = webViewUrl;
            }

            index = result.LastIndexOf("/");
            if (index == (result.Length - 1))
            {
                return result.Substring(0, index);
            }

            return result;
        }

        protected void UnregisterWebviewUrl(string webViewUrl)
        {
            var url = MakeUrlKey(webViewUrl);
            m_RegisteredViews[url] = null;
        }

        private void RegisterWebviewUrl(string webViewUrl, WebView view)
        {
            var url = MakeUrlKey(webViewUrl);
            m_RegisteredViews[url] = view;
        }

        private bool FindWebView(string webViewUrl, out WebView webView)
        {
            webView = null;
            var url = MakeUrlKey(webViewUrl);
            return m_RegisteredViews.TryGetValue(url, out webView);
        }

        public WebView GetWebViewFromURL(string url)
        {
            var urlKey = MakeUrlKey(url);
            return m_RegisteredViews[urlKey];
        }

        public override void OnInitScripting()
        {
            base.SetScriptObject();
        }

        protected override void InitWebView(Rect webViewRect)
        {
            base.InitWebView(webViewRect);
            if (m_InitialOpenURL != null && webView != null)
            {
                RegisterWebviewUrl(m_InitialOpenURL, webView);
            }
        }

        protected override void LoadPage()
        {
            if (!webView)
                return;

            WebView tmpWebView;

            if (!FindWebView(m_InitialOpenURL, out tmpWebView) || tmpWebView == null)
            {
                NotifyVisibility(false);
                //We have to create a webview cache for this url
                webView.SetHostView(null);
                webView = null;
                var webViewRect = GUIClip.Unclip(new Rect(0, 0, position.width, position.height));
                InitWebView(webViewRect);
                RegisterWebviewUrl(m_InitialOpenURL, webView);
                NotifyVisibility(true);
            }
            else
            {
                if (tmpWebView != webView)
                {
                    NotifyVisibility(false);

                    tmpWebView.SetHostView(m_Parent);
                    webView.SetHostView(null);
                    webView = tmpWebView;
                    NotifyVisibility(true);
                    webView.Show();
                }

                //This load Uri causes the flashing.  We have NotifyVisibilty we can use to
                //to tell the javascript it's being shown
                LoadUri();
            }
        }

        internal override WebView webView
        {
            get {return m_WebView; }
            set {m_WebView = value; }
        }
    }
}
