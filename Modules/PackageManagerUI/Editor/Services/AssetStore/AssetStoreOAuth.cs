// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Connect;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class AssetStoreOAuth
    {
        [Serializable]
        public class AssetStoreToken
        {
            private const long k_BufferTime = 15L; // We make sure we still have 15 seconds with the token

            public string accessToken;
            public string expiresIn
            {
                get
                {
                    return m_ExpirationIn.ToString();
                }
                set
                {
                    m_ExpirationIn = long.Parse(value);
                    m_ExpirationStart = EpochSeconds;
                }
            }

            [SerializeField]
            private double m_ExpirationStart;
            [SerializeField]
            private long m_ExpirationIn;

            private static double EpochSeconds => (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            public AssetStoreToken()
            {
                m_ExpirationIn = 0;
                m_ExpirationStart = EpochSeconds;
            }

            public bool IsValid(long bufferTime = k_BufferTime)
            {
                return m_ExpirationIn > 0 && (EpochSeconds - m_ExpirationStart) < (m_ExpirationIn - bufferTime);
            }
        }

        [Serializable]
        public class AccessToken : AssetStoreToken
        {
            public string tokenType;
            public string refreshToken;
            public string user;
            public string displayName;

            public AccessToken(Dictionary<string, object> rawData)
            {
                accessToken = rawData.GetString("access_token");
                tokenType = rawData.GetString("token_type");
                expiresIn = rawData.GetString("expires_in");
                refreshToken = rawData.GetString("refresh_token");
                user = rawData.GetString("user");
                displayName = rawData.GetString("display_name");
            }
        }

        [Serializable]
        public class TokenInfo : AssetStoreToken
        {
            public string sub;
            public string scopes;
            public string clientId;
            public string ipAddress;

            public TokenInfo(Dictionary<string, object> rawData)
            {
                sub = rawData.GetString("sub");
                scopes = rawData.GetString("scopes");
                expiresIn = rawData.GetString("expires_in");
                clientId = rawData.GetString("client_id");
                ipAddress = rawData.GetString("ip_address");
                accessToken = rawData.GetString("access_token");
            }
        }

        [Serializable]
        public class UserInfo
        {
            public string id;
            public string username;
            public string defaultOrganization;

            [SerializeField]
            private TokenInfo m_TokenInfo;

            public bool isValid => m_TokenInfo?.IsValid() ?? false;

            public string accessToken => m_TokenInfo?.accessToken;

            public UserInfo(Dictionary<string, object> rawData, TokenInfo tokenInfo)
            {
                id = rawData.GetString("id");
                username = rawData.GetString("username");
                defaultOrganization = rawData.GetDictionary("extendedProperties")?.GetString("UNITY_DEFAULT_ORGANIZATION");

                m_TokenInfo = tokenInfo;
            }
        }

        private const string k_OAuthUri = "/v1/oauth2/token";
        private const string k_TokenInfoUri = "/v1/oauth2/tokeninfo";
        private const string k_UserInfoUri = "/v1/users";
        private const string k_ServiceId = "packman";

        private IAsyncHTTPClient m_UserInfoRequest;
        private IAsyncHTTPClient m_AccessTokenRequest;
        private IAsyncHTTPClient m_TokenRequest;

        // if the OAuth singleton does go through serialization, all events are destroyed and callbacks won't be triggered
        // therefore we choose not to serialize this filed so that we'll request auth code again
        [NonSerialized]
        private bool m_AuthCodeRequested;

        [SerializeField]
        private UserInfo m_UserInfo;

        [SerializeField]
        private string m_AuthCode;

        [SerializeField]
        private AccessToken m_AccessToken;

        [SerializeField]
        private TokenInfo m_TokenInfo;

        private event Action<string> m_OnAuthCodeFetched;
        private event Action<AccessToken> m_OnAccessTokenFetched;
        private event Action<TokenInfo> m_OnTokenInfoFetched;
        private event Action<UserInfo> m_OnUserInfoFetched;

        // this onError callback is shared between all steps in this process, no matter which step has the error, we'll call this event
        private event Action<UIError> m_OnError;

        private string m_Host;
        private string host
        {
            get
            {
                if (string.IsNullOrEmpty(m_Host))
                    m_Host = m_UnityConnect.GetConfigurationURL(CloudConfigUrl.CloudIdentity);
                return m_Host;
            }
        }

        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private UnityOAuthProxy m_UnityOAuth;
        [NonSerialized]
        private AssetStoreUtils m_AssetStoreUtils;
        [NonSerialized]
        private HttpClientFactory m_HttpClientFactory;
        public void ResolveDependencies(UnityConnectProxy unityConnect,
            UnityOAuthProxy unityOAuth,
            AssetStoreUtils assetStoreUtils,
            HttpClientFactory httpClientFactory)
        {
            m_UnityConnect = unityConnect;
            m_UnityOAuth = unityOAuth;
            m_AssetStoreUtils = assetStoreUtils;
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

            if (loggedIn)
                GetUserInfo(null);
        }

        public virtual void ClearCache()
        {
            m_AuthCodeRequested = false;
            m_TokenRequest?.Abort();
            m_UserInfoRequest?.Abort();
            m_AccessTokenRequest?.Abort();

            m_TokenRequest = null;
            m_UserInfoRequest = null;
            m_AccessTokenRequest = null;

            m_UserInfo = null;
            m_AuthCode = null;
            m_AccessToken = null;
            m_TokenInfo = null;
        }

        // Fetching UserInfo is a 4 step process
        // GetAuthCode -> GetAccessToken -> GetTokenInfo -> GetUserInfo
        // These 4 steps are chained together by async callbacks
        public virtual void FetchUserInfo(Action<UserInfo> doneCallback, Action<UIError> errorCallback)
        {
            m_OnError += errorCallback;
            GetUserInfo(doneCallback);
        }

        private void GetAuthCode(Action<string> doneCallback)
        {
            if (!string.IsNullOrEmpty(m_AuthCode))
            {
                doneCallback?.Invoke(m_AuthCode);
                return;
            }

            // if the result is not cached already, we will register the callback to be called later async
            // this is needed because we only want to have one request going at a time and don't want to lose any callback events
            // because of early `return`s
            m_OnAuthCodeFetched += doneCallback;

            if (m_AuthCodeRequested)
                return;

            m_AuthCodeRequested = true;
            var errorMessage = string.Empty;
            try
            {
                m_UnityOAuth.GetAuthorizationCodeAsync(k_ServiceId, authCodeResponse =>
                {
                    m_AuthCode = "";
                    m_AuthCodeRequested = false;
                    if (!string.IsNullOrEmpty(authCodeResponse.AuthCode))
                    {
                        m_AuthCode = authCodeResponse.AuthCode;
                        m_OnAuthCodeFetched?.Invoke(m_AuthCode);
                        m_OnAuthCodeFetched = null;
                    }
                    else
                    {
                        OnOperationError(authCodeResponse.Exception.ToString());
                    }
                });
            }
            catch (Exception e)
            {
                m_AuthCode = "";
                m_AuthCodeRequested = false;
                OnOperationError(e.Message);
            }
        }

        private void GetAccessToken(Action<AccessToken> doneCallback)
        {
            GetAuthCode(authCode =>
            {
                if (m_AccessToken?.IsValid() ?? false)
                {
                    doneCallback?.Invoke(m_AccessToken);
                    return;
                }

                m_OnAccessTokenFetched += doneCallback;

                if (m_AccessTokenRequest != null)
                    return;

                var secret = m_UnityConnect.GetConfigurationURL(CloudConfigUrl.CloudPackagesKey);

                m_AccessTokenRequest = m_HttpClientFactory.PostASyncHTTPClient(
                    $"{host}{k_OAuthUri}",
                    $"grant_type=authorization_code&code={authCode}&client_id=packman&client_secret={secret}&redirect_uri=packman://unity");
                m_AccessTokenRequest.header["Content-Type"] = "application/x-www-form-urlencoded";
                m_AccessTokenRequest.doneCallback = httpClient =>
                {
                    m_AccessTokenRequest = null;
                    m_AccessToken = null;

                    var response = AssetStoreUtils.ParseResponseAsDictionary(httpClient, OnGetAccessTokenError);
                    if (response != null)
                    {
                        var accessToken = new AccessToken(response);
                        if (accessToken.IsValid())
                        {
                            m_AccessToken = accessToken;
                            m_OnAccessTokenFetched?.Invoke(m_AccessToken);
                            m_OnAccessTokenFetched = null;
                            return;
                        }
                        else
                            OnGetAccessTokenError(L10n.Tr("Access token invalid"));
                    }
                };
                m_AccessTokenRequest.Begin();
            });
        }

        private void GetTokenInfo(Action<TokenInfo> doneCallback)
        {
            GetAccessToken(accessToken =>
            {
                if (m_TokenInfo?.IsValid() ?? false)
                {
                    doneCallback?.Invoke(m_TokenInfo);
                    return;
                }

                m_OnTokenInfoFetched += doneCallback;

                if (m_TokenRequest != null)
                    return;

                m_TokenRequest = m_HttpClientFactory.GetASyncHTTPClient($"{host}{k_TokenInfoUri}?access_token={accessToken.accessToken}");
                m_TokenRequest.doneCallback = httpClient =>
                {
                    m_TokenRequest = null;
                    m_TokenInfo = null;

                    var response = AssetStoreUtils.ParseResponseAsDictionary(httpClient, OnOperationError);
                    if (response != null)
                    {
                        var tokenInfo = new TokenInfo(response);
                        if (tokenInfo.IsValid())
                        {
                            m_TokenInfo = tokenInfo;
                            m_OnTokenInfoFetched?.Invoke(m_TokenInfo);
                            m_OnTokenInfoFetched = null;
                        }
                        else
                            OnOperationError(L10n.Tr("TokenInfo invalid"));
                    }
                };
                m_TokenRequest.Begin();
            });
        }

        private void GetUserInfo(Action<UserInfo> doneCallback)
        {
            GetTokenInfo(tokenInfo =>
            {
                if (m_UserInfo?.isValid ?? false)
                {
                    doneCallback?.Invoke(m_UserInfo);
                    m_OnError = null;
                    return;
                }

                m_OnUserInfoFetched += doneCallback;

                if (m_UserInfoRequest != null)
                    return;

                m_UserInfoRequest = m_HttpClientFactory.GetASyncHTTPClient($"{host}{k_UserInfoUri}/{tokenInfo.sub}");
                m_UserInfoRequest.header["Authorization"] = "Bearer " + tokenInfo.accessToken;
                m_UserInfoRequest.doneCallback = httpClient =>
                {
                    m_UserInfoRequest = null;
                    m_UserInfo = null;

                    var response = AssetStoreUtils.ParseResponseAsDictionary(httpClient, OnOperationError);
                    if (response != null)
                    {
                        var userInfo = new UserInfo(response, tokenInfo);
                        if (userInfo.isValid)
                        {
                            m_UserInfo = userInfo;
                            m_OnUserInfoFetched?.Invoke(m_UserInfo);
                            m_OnUserInfoFetched = null;
                            // note that we only clear error callbacks on the when user info is fetched
                            // as we need the error callback to be present for the whole process.
                            m_OnError = null;
                        }
                        else
                            OnOperationError(L10n.Tr("UserInfo invalid"));
                    }
                };
                m_UserInfoRequest.Begin();
            });
        }

        private void OnGetAccessTokenError(string errorMessage)
        {
            // when we have trouble getting access token, it's most likely because the auth code is no longer valid.
            // therefore we want to clear the auth code in the case of error, such that new auth code will be fetched in the next refresh
            m_AuthCode = string.Empty;
            OnOperationError(errorMessage);
        }

        private void OnOperationError(string errorMessage)
        {
            m_OnError?.Invoke(new UIError(UIErrorCode.AssetStoreAuthorizationError, errorMessage));
            m_OnError = null;
        }
    }
}
