// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Connect;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UnityConnectProxy
    {
        [SerializeField]
        private bool m_IsUserInfoReady;

        [SerializeField]
        private bool m_HasAccessToken;

        [SerializeField]
        private string m_UserId = string.Empty;

        public virtual event Action<bool, bool> onUserLoginStateChange = delegate {};
        public virtual bool isUserInfoReady => m_IsUserInfoReady;
        public virtual bool isUserLoggedIn => m_IsUserInfoReady && m_HasAccessToken;

        public void OnEnable()
        {
            m_IsUserInfoReady = UnityConnect.instance.isUserInfoReady;
            m_HasAccessToken = !string.IsNullOrEmpty(UnityConnect.instance.userInfo.accessToken);
            UnityConnect.instance.UserStateChanged += OnUserStateChanged;
        }

        public void OnDisable()
        {
            UnityConnect.instance.UserStateChanged -= OnUserStateChanged;
        }

        public virtual string GetConfigurationURL(CloudConfigUrl config)
        {
            return UnityConnect.instance.GetConfigurationURL(config);
        }

        public virtual void ShowLogin()
        {
            UnityConnect.instance.ShowLogin();
        }

        public virtual void OpenAuthorizedURLInWebBrowser(string url)
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
