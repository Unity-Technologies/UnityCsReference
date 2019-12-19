// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.Connect;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class ApplicationUtil
    {
        public static readonly string k_ResetPackagesMenuName = "Reset Packages to defaults";
        public static readonly string k_ResetPackagesMenuPath = "Help/" + k_ResetPackagesMenuName;
        public static readonly string k_IsTranslatedFlag = "textIsTranslated";

        static IApplicationUtil s_Instance = null;
        public static IApplicationUtil instance => s_Instance ?? ApplicationUtilInternal.instance;

        [Serializable]
        private class ApplicationUtilInternal : IApplicationUtil
        {
            private static ApplicationUtilInternal s_Instance;
            public static ApplicationUtilInternal instance => s_Instance ?? (s_Instance = new ApplicationUtilInternal());

            public event Action onFinishCompiling = delegate {};
            [SerializeField]
            private bool m_CheckingCompilation = false;

            public event Action<bool> onUserLoginStateChange = delegate {};
            public event Action<bool> onInternetReachabilityChange = delegate {};

            [SerializeField]
            private ConnectInfo m_ConnectInfo;

            [SerializeField]
            private bool m_IsInternetReachable;
            [SerializeField]
            private double m_LastInternetCheck;

            public string userAppDataPath => InternalEditorUtility.userAppDataFolder;

            private ApplicationUtilInternal()
            {
                m_ConnectInfo = UnityConnect.instance.connectInfo;
                UnityConnect.instance.StateChanged += OnStateChanged;

                m_IsInternetReachable = Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
                m_LastInternetCheck = EditorApplication.timeSinceStartup;
                EditorApplication.update += CheckInternetReachability;
            }

            private void CheckInternetReachability()
            {
                if (EditorApplication.timeSinceStartup - m_LastInternetCheck < 2.0)
                    return;

                m_LastInternetCheck = EditorApplication.timeSinceStartup;
                var isInternetReachable = Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
                if (isInternetReachable != m_IsInternetReachable)
                {
                    m_IsInternetReachable = isInternetReachable;
                    onInternetReachabilityChange?.Invoke(m_IsInternetReachable);
                }
            }

            private void OnStateChanged(ConnectInfo state)
            {
                var loginChanged = (m_ConnectInfo.ready && m_ConnectInfo.loggedIn && !state.loggedIn) ||
                    (m_ConnectInfo.ready && !m_ConnectInfo.loggedIn && state.loggedIn);

                var onlineChanged = (m_ConnectInfo.ready && m_ConnectInfo.online && !state.online) ||
                    (m_ConnectInfo.ready && !m_ConnectInfo.online && state.online);

                m_ConnectInfo = state;

                if (loginChanged)
                    onUserLoginStateChange?.Invoke(m_ConnectInfo.loggedIn);
                if (onlineChanged)
                    onInternetReachabilityChange?.Invoke(m_ConnectInfo.online);
            }

            public bool isPreReleaseVersion
            {
                get
                {
                    var lastToken = Application.unityVersion.Split('.').LastOrDefault();
                    return lastToken.Contains("a") || lastToken.Contains("b");
                }
            }

            public string shortUnityVersion
            {
                get
                {
                    var unityVersionParts = Application.unityVersion.Split('.');
                    return $"{unityVersionParts[0]}.{unityVersionParts[1]}";
                }
            }

            public bool isInternetReachable
            {
                get { return m_IsInternetReachable; }
            }

            public bool isUserLoggedIn
            {
                get { return m_ConnectInfo.ready && m_ConnectInfo.loggedIn; }
            }

            public void ShowLogin()
            {
                UnityConnect.instance.ShowLogin();
            }

            public void OpenURL(string url)
            {
                Application.OpenURL(url);
            }

            public bool isCompiling
            {
                get
                {
                    var result = EditorApplication.isCompiling;
                    if (result && !m_CheckingCompilation)
                    {
                        EditorApplication.update -= CheckCompilationStatus;
                        EditorApplication.update += CheckCompilationStatus;
                        m_CheckingCompilation = true;
                    }
                    return result;
                }
            }

            private void CheckCompilationStatus()
            {
                if (EditorApplication.isCompiling)
                    return;

                m_CheckingCompilation = false;
                EditorApplication.update -= CheckCompilationStatus;

                onFinishCompiling();
            }

            public IAsyncHTTPClient GetASyncHTTPClient(string url, string method = null)
            {
                return string.IsNullOrEmpty(method) ? new AsyncHTTPClient(url) : new AsyncHTTPClient(url, method);
            }

            public void GetAuthorizationCodeAsync(string clientId, Action<UnityOAuth.AuthCodeResponse> callback)
            {
                UnityOAuth.GetAuthorizationCodeAsync(clientId, callback);
            }

            public string GetTranslationForText(string text)
            {
                return L10n.Tr(text);
            }

            public void TranslateTextElement(TextElement textElement)
            {
                if (textElement.userData as string != k_IsTranslatedFlag)
                {
                    if (!string.IsNullOrEmpty(textElement.text))
                    {
                        textElement.text = GetTranslationForText(textElement.text);
                        textElement.userData = k_IsTranslatedFlag;
                    }
                    if (!string.IsNullOrEmpty(textElement.tooltip))
                    {
                        textElement.tooltip = GetTranslationForText(textElement.tooltip);
                        textElement.userData = k_IsTranslatedFlag;
                    }
                }
            }

            public int CalculateNumberOfElementsInsideContainerToDisplay(VisualElement container, float elementHeight)
            {
                float containerHeight = container.resolvedStyle.height;

                if (elementHeight != 0 && !float.IsNaN(containerHeight))
                    return (int)(containerHeight / elementHeight);

                return 0;
            }
        }
    }
}
