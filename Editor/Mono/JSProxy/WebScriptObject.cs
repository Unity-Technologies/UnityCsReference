// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngineInternal;

namespace UnityEditor.Web
{
    internal class WebScriptObject : ScriptableObject
    {
        public WebView webView
        {
            get
            {
                return m_WebView;
            }
            set
            {
                m_WebView = value;
            }
        }

        private WebView m_WebView;

        private WebScriptObject()
        {
            m_WebView = null;
        }

        public bool ProcessMessage(string jsonRequest, WebViewV8CallbackCSharp callback)
        {
            if (m_WebView != null)
            {
                return JSProxyMgr.GetInstance().DoMessage(jsonRequest, (object result) =>
                    {
                        string res = JSProxyMgr.GetInstance().Stringify(result);
                        callback.Callback(res);
                    }, m_WebView
                    );
            }
            return false;
        }

        // For legacy external website JS invocation like AssetStore
        public bool processMessage(string jsonRequest, WebViewV8CallbackCSharp callback)
        {
            return ProcessMessage(jsonRequest, callback);
        }
    };
}
