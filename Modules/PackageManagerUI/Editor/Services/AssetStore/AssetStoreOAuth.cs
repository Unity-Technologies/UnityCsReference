// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Connect;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStoreOAuth
    {
        [Serializable]
        public class AccessToken
        {
            public const long k_ExpirationBufferTime = 15L;

            public string accessToken;

            public string refreshToken;

            [SerializeField]
            private long m_ExpirationTimeTicks;

            public AccessToken(DateTime creationTime, Dictionary<string, object> rawData)
            {
                accessToken = rawData.GetString("access_token");
                refreshToken = rawData.GetString("refresh_token");

                var expiresInSeconds = rawData.GetStringAsLong("expires_in");
                m_ExpirationTimeTicks = creationTime.AddSeconds(expiresInSeconds - k_ExpirationBufferTime).Ticks;
            }

            public bool IsValid(DateTime currentTime)
            {
                return m_ExpirationTimeTicks > 0 && !string.IsNullOrEmpty(accessToken) && currentTime.Ticks < m_ExpirationTimeTicks;
            }
        }

        private const string k_OAuthUri = "/v1/oauth2/token";
        private const string k_ServiceId = "packman";

        private IAsyncHTTPClient m_AccessTokenRequest;

        // if the OAuth singleton does go through serialization, all events are destroyed and callbacks won't be triggered
        // therefore we choose not to serialize this filed so that we'll request auth code again
        [NonSerialized]
        private bool m_AuthCodeRequested;

        [SerializeField]
        private string m_AuthCode;

        [SerializeField]
        private AccessToken m_AccessToken;

        private event Action<AccessToken> onAccessTokenFetched;
        private event Action<UIError> onError;

        [NonSerialized]
        private DateTimeProxy m_DateTime;
        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private UnityOAuthProxy m_UnityOAuth;
        [NonSerialized]
        private HttpClientFactory m_HttpClientFactory;
        public void ResolveDependencies(DateTimeProxy dateTime,
            UnityConnectProxy unityConnect,
            UnityOAuthProxy unityOAuth,
            HttpClientFactory httpClientFactory)
        {
            m_DateTime = dateTime;
            m_UnityConnect = unityConnect;
            m_UnityOAuth = unityOAuth;
            m_HttpClientFactory = httpClientFactory;
        }

        public void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
        }

        public void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            ClearCache();
        }

        public virtual void ClearCache()
        {
            m_AuthCodeRequested = false;
            m_AccessTokenRequest?.Abort();
            m_AccessTokenRequest = null;

            m_AuthCode = null;
            m_AccessToken = null;

            ClearAccessTokenCallbacks();
        }

        public virtual void FetchAccessToken(Action<AccessToken> doneCallback, Action<UIError> errorCallback)
        {
            if (m_AccessToken?.IsValid(m_DateTime.utcNow) ?? false)
            {
                doneCallback?.Invoke(m_AccessToken);
                return;
            }

            onAccessTokenFetched += doneCallback;
            onError += errorCallback;

            var authorization = GetAuthorization();
            if (!string.IsNullOrEmpty(authorization))
                GetAccessToken(authorization);
            else
                GetAuthCodeAndAccessToken();
        }

        private void GetAuthCodeAndAccessToken()
        {
            if (m_AuthCodeRequested)
                return;

            m_AuthCodeRequested = true;
            m_UnityOAuth.GetAuthorizationCodeAsync(k_ServiceId, authCodeResponse =>
            {
                m_AuthCodeRequested = false;
                if (!string.IsNullOrEmpty(authCodeResponse.AuthCode))
                {
                    m_AuthCode = authCodeResponse.AuthCode;
                    GetAccessToken(GetAuthorization());
                }
                else
                {
                    OnOperationError(string.Format(L10n.Tr("Error while getting auth code: {0}"), authCodeResponse.Exception));
                }
            });
        }

        private string GetAuthorization()
        {
            var refreshToken = m_AccessToken?.refreshToken;
            if (!string.IsNullOrEmpty(refreshToken))
                return $"grant_type=refresh_token&refresh_token={refreshToken}";
            return !string.IsNullOrEmpty(m_AuthCode) ? $"grant_type=authorization_code&code={m_AuthCode}" : string.Empty;
        }

        private void GetAccessToken(string authorization)
        {
            if (m_AccessTokenRequest != null)
                return;

            var secret = m_UnityConnect.GetConfigurationURL(CloudConfigUrl.CloudPackagesKey);
            if (string.IsNullOrEmpty(secret))
            {
                OnOperationError(L10n.Tr("Error while getting access token: invalid configuration from Unity Connect"));
                return;
            }

            var host = m_UnityConnect.GetConfigurationURL(CloudConfigUrl.CloudIdentity);
            m_AccessTokenRequest = m_HttpClientFactory.PostASyncHTTPClient($"{host}{k_OAuthUri}", $"{authorization}&client_id={k_ServiceId}&client_secret={secret}");
            m_AccessTokenRequest.header["Content-Type"] = "application/x-www-form-urlencoded";
            m_AccessTokenRequest.doneCallback = httpClient =>
            {
                m_AccessTokenRequest = null;
                m_AccessToken = null;

                var response = m_HttpClientFactory.ParseResponseAsDictionary(httpClient);
                var errorMessage = response == null ? L10n.Tr("Unable to parse http response.") : response.GetString("errorMessage");
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    OnOperationError(string.Format(L10n.Tr("Error while getting access token: {0}"), errorMessage));
                    return;
                }

                var utcNow = m_DateTime.utcNow;
                var accessToken = new AccessToken(utcNow, response);
                if (accessToken.IsValid(utcNow))
                {
                    m_AccessToken = accessToken;
                    onAccessTokenFetched?.Invoke(m_AccessToken);
                    ClearAccessTokenCallbacks();
                }
                else
                    OnOperationError(L10n.Tr("Access token invalid"));
            };
            m_AccessTokenRequest.Begin();
        }

        private void OnOperationError(string errorMessage)
        {
            Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] {0}"), errorMessage));
            onError?.Invoke(new UIError(UIErrorCode.AssetStoreAuthorizationError, errorMessage, UIError.Attribute.DetailInConsole));

            // when we have trouble getting access token, it's most likely because the auth code is no longer valid.
            // therefore we want to clear the auth code in the case of error, such that new auth code will be fetched in the next refresh
            m_AuthCode = string.Empty;
            m_AuthCodeRequested = false;

            ClearAccessTokenCallbacks();
        }

        private void ClearAccessTokenCallbacks()
        {
            onAccessTokenFetched = delegate {};
            onError = delegate {};
        }
    }
}
