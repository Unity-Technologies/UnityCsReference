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
        void GetAccessToken(Action<string> doneCallback, Action<UIError> errorCallback);
    }

    [Serializable]
    [ExcludeFromCodeCoverage]
    internal class UnityConnectProxy : BaseService<IUnityConnectProxy>, IUnityConnectProxy
    {
        [SerializeField]
        private bool m_IsUserInfoReady;

        [SerializeField]
        private string m_AccessToken;

        [SerializeField]
        private string m_UserId = string.Empty;

        public event Action<bool, bool> onUserLoginStateChange = delegate {};
        public bool isUserInfoReady => m_IsUserInfoReady;
        public bool isUserLoggedIn => m_IsUserInfoReady && !string.IsNullOrEmpty(m_AccessToken);

        public override void OnEnable()
        {
            m_IsUserInfoReady = UnityConnect.instance.isUserInfoReady;
            m_AccessToken = UnityConnect.instance.userInfo.accessToken;
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

        public void GetAccessToken(Action<string> doneCallback, Action<UIError> errorCallback)
        {
            if (string.IsNullOrEmpty(m_AccessToken))
            {
                if (m_IsUserInfoReady)
                    errorCallback?.Invoke(new UIError(UIErrorCode.UserNotSignedIn,  L10n.Tr("You need to be signed in."), UIError.Attribute.HiddenFromUI));
                else
                    errorCallback?.Invoke(new UIError(UIErrorCode.UnityConnectUserStatusError, L10n.Tr("Invalid user status."), UIError.Attribute.HiddenFromUI));
                return;
            }
            doneCallback?.Invoke(m_AccessToken);
        }

        private void OnUserStateChanged(UserInfo newInfo)
        {
            var prevIsUserInfoReady = isUserInfoReady;
            var prevIsUserLoggedIn = isUserLoggedIn;

            m_IsUserInfoReady = UnityConnect.instance.isUserInfoReady;
            m_AccessToken = UnityConnect.instance.userInfo.accessToken;

            if (isUserInfoReady != prevIsUserInfoReady || isUserLoggedIn != prevIsUserLoggedIn || newInfo.userId != m_UserId)
                onUserLoginStateChange?.Invoke(isUserInfoReady, isUserLoggedIn);

            m_UserId = newInfo.userId;
        }
    }
}
