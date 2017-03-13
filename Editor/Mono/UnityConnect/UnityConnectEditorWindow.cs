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
    internal class UnityConnectEditorWindow : WebViewEditorWindowTabs
    {
        private List<string> m_ServiceUrls;
        public string ErrorUrl { get; set; }

        private bool m_ClearInitialOpenURL;
        public string currentUrl
        {
            get { return m_InitialOpenURL; }
            set
            {
                m_InitialOpenURL = value;
                LoadPage();
            }
        }

        public static UnityConnectEditorWindow Create(string title, List<string> serviceUrls)
        {
            //Pop back the drawer window if it exist we have to look from all the layout
            //because window can appear at anytime when a layout get reload
            var wins = Resources.FindObjectsOfTypeAll(typeof(UnityConnectEditorWindow)) as UnityConnectEditorWindow[];
            if (wins != null)
            {
                // cannot test with title anymore since we are addind [env] to the title if not  production
                foreach (var win in wins.Where(win => win != null /*&& win.title == title*/))
                {
                    win.titleContent = new GUIContent(title);
                    return win;
                }
            }
            //Create a new window if it do not exist
            var window = GetWindow<UnityConnectEditorWindow>(title, typeof(UnityEditor.InspectorWindow));

            //Prevent the reset of the url, we want the reset to happen when the layout is creating this window not in this case
            //Here the user ask to see the hub
            window.m_ClearInitialOpenURL = false;
            //Always select the first service when creating the service drawer
            window.initialOpenUrl = serviceUrls[0];
            window.Init();
            return window;
        }

        protected UnityConnectEditorWindow()
        {
            m_ServiceUrls = new List<string>();
            m_ClearInitialOpenURL = true;
        }

        public bool UrlsMatch(List<string> referenceUrls)
        {
            if (m_ServiceUrls.Count != referenceUrls.Count)
                return false;

            return !m_ServiceUrls.Where((t, idx) => t != referenceUrls[idx]).Any();
        }

        public new void OnEnable()
        {
            //Construct the UnityConnectEditor singleton

            m_ServiceUrls = UnityConnectServiceCollection.instance.GetAllServiceUrls();
            base.OnEnable();
        }

        public new void OnInitScripting()
        {
            base.OnInitScripting();
        }

        public new void ToggleMaximize()
        {
            base.ToggleMaximize();
        }

        public new void OnLoadError(string url)
        {
            if (webView == null)
                return;

            webView.LoadFile(EditorApplication.userJavascriptPackagesPath + "unityeditor-cloud-hub/dist/index.html?failure=load_error&reload_url=" +  WWW.EscapeURL(url));
            if (url.StartsWith("http://") || url.StartsWith("https://"))
                UnregisterWebviewUrl(url);
        }

        new public void OnGUI()
        {
            //We must prevent the layout serialization to load a url, we want to load
            //the hub when we start the application.
            if (m_ClearInitialOpenURL)
            {
                m_ClearInitialOpenURL = false;
                m_InitialOpenURL = m_ServiceUrls.Count > 0 ? UnityConnectServiceCollection.instance.GetUrlForService(HubAccess.kServiceName) : null;
            }
            base.OnGUI();
        }
    }
}
