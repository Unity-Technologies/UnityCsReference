// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Web;
using System.Collections.Generic;

namespace UnityEditor.Connect
{
    [Serializable]
    internal class UnityConnectConsentView : WebViewEditorWindow
    {
        private String code = "";
        private String error = "";

        public String Code
        {
            get
            {
                return code;
            }
        }

        public String Error
        {
            get
            {
                return error;
            }
        }

        internal override WebView webView
        {
            get; set;
        }

        public static UnityConnectConsentView ShowUnityConnectConsentView(String URL)
        {
            UnityConnectConsentView consentView = ScriptableObject.CreateInstance<UnityConnectConsentView>();

            var rect = new Rect(100, 100, 800, 605);
            consentView.titleContent = EditorGUIUtility.TextContent("Unity Application Consent Window");
            consentView.minSize = new Vector2(rect.width, rect.height);
            consentView.maxSize = new Vector2(rect.width, rect.height);
            consentView.position = rect;
            consentView.m_InitialOpenURL = URL;
            consentView.ShowModal();

            consentView.m_Parent.window.m_DontSaveToLayout = true;

            return consentView;
        }

        override public void OnDestroy()
        {
            OnBecameInvisible();
        }

        override public void OnInitScripting()
        {
            base.SetScriptObject();
        }

        override public void OnLocationChanged(string url)
        {
            var location = new Uri(url);
            foreach (string item in location.Query.Split('&'))
            {
                string[] qs = item.Replace("?", String.Empty).Split('=');
                if (qs[0] == "code")
                {
                    code = qs[1];
                    break;
                }
                if (qs[0] == "error")
                {
                    error = qs[1];
                    break;
                }
            }

            if (!string.IsNullOrEmpty(code) || !string.IsNullOrEmpty(error))
            {
                this.Close();
                return;
            }
            base.OnLocationChanged(url);
        }
    }
}
