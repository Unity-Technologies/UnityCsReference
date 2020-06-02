// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Connect;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UnityConnectProxy
    {
        [SerializeField]
        private bool m_IsUserInfoReady;

        [SerializeField]
        private bool m_HasAccessToken;

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

        private void OnUserStateChanged(UserInfo newInfo)
        {
            var isUerInfoReadyOld = isUserInfoReady;
            var isUserLoggedInOld = isUserLoggedIn;

            m_IsUserInfoReady = UnityConnect.instance.isUserInfoReady;
            m_HasAccessToken = !string.IsNullOrEmpty(UnityConnect.instance.userInfo.accessToken);

            if (isUerInfoReadyOld != isUserInfoReady || isUserLoggedIn != isUserLoggedInOld)
                onUserLoginStateChange?.Invoke(isUserInfoReady, isUserLoggedIn);
        }
    }
}
