// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.Connect;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IUnityConnectProxy : IService
    {
        event Action<bool, bool> onUserLoginStateChange;
        bool isUserInfoReady { get; }
        bool isUserLoggedIn { get; }

        string GetConfigurationURL(CloudConfigUrl config);
        void ShowLogin();
        void OpenAuthorizedURLInWebBrowser(string url);
    }

    [Serializable]
    [ExcludeFromCodeCoverage]
    internal class UnityConnectProxy : BaseService<IUnityConnectProxy>, IUnityConnectProxy
    {
        [SerializeField]
        private bool m_IsUserInfoReady;

        [SerializeField]
        private bool m_HasAccessToken;

        [SerializeField]
        private string m_UserId = string.Empty;

        public event Action<bool, bool> onUserLoginStateChange = delegate {};
        public bool isUserInfoReady => m_IsUserInfoReady;
        public bool isUserLoggedIn => m_IsUserInfoReady && m_HasAccessToken;

        public override void OnEnable()
        {
            m_IsUserInfoReady = UnityConnect.instance.isUserInfoReady;
            m_HasAccessToken = !string.IsNullOrEmpty(UnityConnect.instance.userInfo.accessToken);
            UnityConnect.instance.UserStateChanged += OnUserStateChanged;
        }

        public override void OnDisable()
        {
            UnityConnect.instance.UserStateChanged -= OnUserStateChanged;
        }

        public string GetConfigurationURL(CloudConfigUrl config)
        {
            return UnityConnect.instance.GetConfigurationURL(config);
        }

        public void ShowLogin()
        {
            UnityConnect.instance.ShowLogin();
        }

        public void OpenAuthorizedURLInWebBrowser(string url)
        {
            UnityConnect.instance.OpenAuthorizedURLInWebBrowser(url);
        }

        private void OnUserStateChanged(UserInfo newInfo)
        {
            var prevIsUserInfoReady = isUserInfoReady;
            var prevIsUserLoggedIn = isUserLoggedIn;

            m_IsUserInfoReady = UnityConnect.instance.isUserInfoReady;
            m_HasAccessToken = !string.IsNullOrEmpty(UnityConnect.instance.userInfo.accessToken);

            if (isUserInfoReady != prevIsUserInfoReady || isUserLoggedIn != prevIsUserLoggedIn || newInfo.userId != m_UserId)
                onUserLoginStateChange?.Invoke(isUserInfoReady, isUserLoggedIn);

            m_UserId = newInfo.userId;
        }
    }
}
