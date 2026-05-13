// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UnityEditor.Connect;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IUnityConnectProxy : IService
    {
        event Action<bool, bool> onUserLoginStateChange;
        bool isUserInfoReady { get; }
        bool isUserLoggedIn { get; }
        string userPrimaryOrg { get; }
        string displayName { get; }
        string GetConfigurationURL(CloudConfigUrl config);
        void ShowLogin();
        void OpenAuthorizedURLInWebBrowser(string url);
        public Task<OrganizationInfo[]> ParseOrganizationInfosAsync(Action<OrganizationInfo[]> onResult);
    }

    [Serializable]
    [ExcludeFromCodeCoverage]
    internal class UnityConnectProxy : BaseService<IUnityConnectProxy>, IUnityConnectProxy
    {
        internal class OrganizationInfoParser
        {
            private static OrganizationInfo[] Parse()
            {
                var foreignKeys = UnityConnect.instance.userInfo.organizationForeignKeys.Split(',');
                var parsedOrganizationInfos = new OrganizationInfo[foreignKeys.Length];
                for (var i = 0; i < foreignKeys.Length; i++)
                {
                    var name = UnityConnect.instance.userInfo.organizationNames[i];
                    var orgInfo = new OrganizationInfo
                    {
                        name = name,
                        foreignKey = foreignKeys[i]
                    };
                    parsedOrganizationInfos[i] = orgInfo;
                }

                return parsedOrganizationInfos;
            }

            public async Task<OrganizationInfo[]> ParseAsync(Action<OrganizationInfo[]> onResult = null)
            {
                OrganizationInfo[] parsedOrganizationInfos;
                try
                {
                    var organizations = await UnityConnectRequests.GetOrganizationsAsync();
                    List<OrganizationInfo> sortedOrganizationList = new();
                    foreach (var org in organizations)
                    {
                        sortedOrganizationList.Add(new OrganizationInfo
                        {
                            name = org.Name,
                            foreignKey = org.GenesisId
                        });
                    }
                    sortedOrganizationList.Sort((o, v) => o.name.CompareTo(v.name));
                    parsedOrganizationInfos = sortedOrganizationList.ToArray();
                }
                catch (Exception)
                {
                    parsedOrganizationInfos = Parse();
                }
                onResult?.Invoke(parsedOrganizationInfos);
                return parsedOrganizationInfos;
            }
        }

        [SerializeField]
        private bool m_IsUserInfoReady;

        [SerializeField]
        private bool m_HasAccessToken;

        [SerializeField]
        private string m_UserId = string.Empty;

        [SerializeField]
        private string m_DisplayName = string.Empty;

        [SerializeField]
        private string m_UserPrimaryOrg = string.Empty;

        private readonly OrganizationInfoParser m_OrganizationInfoParser = new();

        public event Action<bool, bool> onUserLoginStateChange = delegate {};
        public bool isUserInfoReady => m_IsUserInfoReady;
        public bool isUserLoggedIn => m_IsUserInfoReady && m_HasAccessToken;
        public string userPrimaryOrg => m_UserPrimaryOrg;
        public string displayName => m_DisplayName;

        public override void OnEnable()
        {
            RefreshUserData();
            UnityConnect.instance.UserStateChanged += OnUserStateChanged;
        }

        public override void OnDisable()
        {
            UnityConnect.instance.UserStateChanged -= OnUserStateChanged;
        }

        private void RefreshUserData()
        {
            m_IsUserInfoReady = UnityConnect.instance.isUserInfoReady;
            m_UserPrimaryOrg = UnityConnect.instance.userInfo.valid ? UnityConnect.instance.userInfo.primaryOrg : string.Empty;
            m_HasAccessToken = !string.IsNullOrEmpty(UnityConnect.instance.userInfo.accessToken);
            m_UserId = UnityConnect.instance.userInfo.userId;
            m_DisplayName = UnityConnect.instance.userInfo.displayName;
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

        public Task<OrganizationInfo[]> ParseOrganizationInfosAsync(Action<OrganizationInfo[]> onResult = null)
        {
            return m_OrganizationInfoParser.ParseAsync(onResult);
        }

        private void OnUserStateChanged(UserInfo newInfo)
        {
            var prevIsUserInfoReady = isUserInfoReady;
            var prevIsUserLoggedIn = isUserLoggedIn;
            var prevUserId = m_UserId;
            var prevDisplayName = m_DisplayName;

            RefreshUserData();

            if (isUserInfoReady != prevIsUserInfoReady || isUserLoggedIn != prevIsUserLoggedIn || prevUserId != m_UserId || prevDisplayName != m_DisplayName)
                onUserLoginStateChange?.Invoke(isUserInfoReady, isUserLoggedIn);
        }
    }
}
