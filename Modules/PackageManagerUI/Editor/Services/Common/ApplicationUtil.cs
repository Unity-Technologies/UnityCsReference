// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
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
            public event Action onEditorSelectionChanged = delegate {};

            [SerializeField]
            private bool m_HasAccessToken;

            [SerializeField]
            private bool m_IsInternetReachable;
            [SerializeField]
            private double m_LastInternetCheck;

            public string userAppDataPath => InternalEditorUtility.userAppDataFolder;

            private ApplicationUtilInternal()
            {
                m_HasAccessToken = !string.IsNullOrEmpty(UnityConnect.instance.userInfo.accessToken);
                UnityConnect.instance.UserStateChanged += OnUserStateChanged;

                m_IsInternetReachable = Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
                m_LastInternetCheck = EditorApplication.timeSinceStartup;
                EditorApplication.update += CheckInternetReachability;

                Selection.selectionChanged += OnEditorSelectionChanged;
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

            private void OnUserStateChanged(UserInfo newInfo)
            {
                m_HasAccessToken = !string.IsNullOrEmpty(UnityConnect.instance.userInfo.accessToken);
                onUserLoginStateChange?.Invoke(isUserLoggedIn);
            }

            private void OnEditorSelectionChanged()
            {
                onEditorSelectionChanged?.Invoke();
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
                get { return UnityConnect.instance.isUserInfoReady && m_HasAccessToken; }
            }

            public bool isUserInfoReady
            {
                get { return UnityConnect.instance.isUserInfoReady; }
            }

            public void ShowLogin()
            {
                UnityConnect.instance.ShowLogin();
            }

            public void OpenURL(string url)
            {
                Application.OpenURL(url);
            }

            public bool isBatchMode => Application.isBatchMode;

            public bool isUpmRunning => !Application.HasARGV("noUpm");

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

            public UnityEngine.Object activeSelection
            {
                get { return Selection.activeObject; }
                set { Selection.activeObject = value; }
            }

            private void CheckCompilationStatus()
            {
                if (EditorApplication.isCompiling)
                    return;

                m_CheckingCompilation = false;
                EditorApplication.update -= CheckCompilationStatus;

                onFinishCompiling();
            }

            public IAsyncHTTPClient GetASyncHTTPClient(string url)
            {
                return new AsyncHTTPClient(url);
            }

            public IAsyncHTTPClient PostASyncHTTPClient(string url, string postData)
            {
                return new AsyncHTTPClient(url, "POST") {postData = postData};
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

            public string OpenFilePanelWithFilters(string title, string directory, string[] filters)
            {
                return EditorUtility.OpenFilePanelWithFilters(title, directory, filters);
            }

            public string GetFileName(string path)
            {
                return Path.GetFileName(path);
            }
        }
    }
}
