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
        private ConnectInfo m_ConnectInfo;

        public virtual event Action<bool> onUserLoginStateChange = delegate {};
        public virtual bool isUserLoggedIn => m_ConnectInfo.ready && m_ConnectInfo.loggedIn;

        public void OnEnable()
        {
            m_ConnectInfo = UnityConnect.instance.connectInfo;
            UnityConnect.instance.StateChanged += OnStateChanged;
        }

        public void OnDisable()
        {
            UnityConnect.instance.StateChanged -= OnStateChanged;
        }

        public virtual string GetConfigurationURL(CloudConfigUrl config)
        {
            return UnityConnect.instance.GetConfigurationURL(config);
        }

        public virtual void ShowLogin()
        {
            UnityConnect.instance.ShowLogin();
        }

        private void OnStateChanged(ConnectInfo newInfo)
        {
            var loginChanged = newInfo.ready && m_ConnectInfo.loggedIn != newInfo.loggedIn;

            m_ConnectInfo = newInfo;

            if (loginChanged)
                onUserLoginStateChange?.Invoke(m_ConnectInfo.loggedIn);
        }
    }
}
