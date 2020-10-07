// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEditor.Connect;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class ApplicationUtil
    {
        public static readonly string k_ResetPackagesMenuName = "Reset Packages to defaults";
        public static readonly string k_ResetPackagesMenuPath = "Help/" + k_ResetPackagesMenuName;

        static IApplicationUtil s_Instance = null;
        public static IApplicationUtil instance => s_Instance ?? ApplicationUtilInternal.instance;

        [Serializable]
        private class ApplicationUtilInternal : ScriptableSingleton<ApplicationUtilInternal>, IApplicationUtil
        {
            public event Action onFinishCompiling = delegate {};
            [SerializeField]
            private bool m_CheckingCompilation = false;

            public event Action<bool> onUserLoginStateChange = delegate {};
            public event Action<bool> onInternetReachabilityChange = delegate {};

            [SerializeField]
            private bool m_HasAccessToken;
            [SerializeField]
            private bool m_IsUserInfoReady;

            [SerializeField]
            private bool m_IsInternetReachable;
            [SerializeField]
            private double m_LastInternetCheck;

            public string userAppDataPath => InternalEditorUtility.userAppDataFolder;

            public void OnEnable()
            {
                m_HasAccessToken = !string.IsNullOrEmpty(UnityConnect.instance.userInfo.accessToken);
                m_IsUserInfoReady = UnityConnect.instance.isUserInfoReady;
                UnityConnect.instance.UserStateChanged += OnUserStateChanged;

                m_IsInternetReachable = Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
                m_LastInternetCheck = EditorApplication.timeSinceStartup;
                EditorApplication.update += CheckInternetReachability;
            }

            public void OnDisable()
            {
                UnityConnect.instance.UserStateChanged -= OnUserStateChanged;
                EditorApplication.update -= CheckInternetReachability;
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
                m_HasAccessToken = !string.IsNullOrEmpty(newInfo.accessToken);
                m_IsUserInfoReady = UnityConnect.instance.isUserInfoReady;
                onUserLoginStateChange?.Invoke(isUserLoggedIn);
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
                get { return m_IsUserInfoReady && m_HasAccessToken; }
            }

            public bool isUserInfoReady
            {
                get { return m_IsUserInfoReady; }
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

            private void CheckCompilationStatus()
            {
                if (EditorApplication.isCompiling)
                    return;

                m_CheckingCompilation = false;
                EditorApplication.update -= CheckCompilationStatus;

                onFinishCompiling();
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
