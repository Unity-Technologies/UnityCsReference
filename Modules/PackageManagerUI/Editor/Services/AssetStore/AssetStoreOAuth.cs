// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Connect;

namespace UnityEditor.PackageManager.UI.AssetStore
{
    internal class AssetStoreOAuth
    {
        static IAssetStoreOAuth s_Instance = null;
        public static IAssetStoreOAuth instance { get { return s_Instance ?? AssetStoreOAuthInternal.instance; } }

        public class AssetStoreToken
        {
            private const long k_BufferTime = 15L; // We make sure we still have 15 seconds with the token

            public string access_token;
            public string expires_in
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

            private double m_ExpirationStart;
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

        public class AccessToken : AssetStoreToken
        {
            public string token_type;
            public string refresh_token;
            public string user;
            public string display_name;
        }

        public class TokenInfo : AssetStoreToken
        {
            public string sub;
            public string scopes;
            public string client_id;
            public string ip_address;
        }

        public class UserInfo
        {
            public string id;
            public string username;
            public string defaultOrganization;

            private bool m_IsValid;
            public bool isValid
            {
                get
                {
                    return m_IsValid && accessToken != null && accessToken.IsValid() && tokenInfo != null && tokenInfo.IsValid();
                }
                set
                {
                    m_IsValid = value;
                }
            }

            public string errorMessage;
            public AccessToken accessToken;
            public TokenInfo tokenInfo;
            public string authCode;
        }

        private class AssetStoreOAuthInternal : ScriptableSingleton<AssetStoreOAuthInternal>, IAssetStoreOAuth
        {
            private string m_Host = "";
            private string m_Secret = "";
            private const string kOAuthUri = "/v1/oauth2/token";
            private const string kTokenInfoUri = "/v1/oauth2/tokeninfo";
            private const string kUserInfoUri = "/v1/users";
            private const string kServiceId = "packman";

            private IAsyncHTTPClient m_UserInfoRequest;
            private IAsyncHTTPClient m_AccessTokenRequest;
            private IAsyncHTTPClient m_TokenRequest;

            private UserInfo m_UserInfo;
            private List<Action<UserInfo>> m_DoneCallbackList;

            private AssetStoreOAuthInternal()
            {
                m_UserInfo = new UserInfo();
                m_UserInfo.isValid = false;
                m_DoneCallbackList = new List<Action<UserInfo>>();
            }

            public void OnEnable()
            {
                if (string.IsNullOrEmpty(m_Secret))
                {
                    m_Secret = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudPackagesKey);
                }

                if (string.IsNullOrEmpty(m_Host))
                {
                    m_Host = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudIdentity);
                }

                if (ApplicationUtil.instance.isUserLoggedIn)
                    GetAuthCode();

                ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
            }

            private void OnDisable()
            {
                ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;
            }

            private void OnUserLoginStateChange(bool loggedIn)
            {
                m_UserInfo = new UserInfo();

                m_AuthCodeRequested = false;
                m_TokenRequest?.Abort();
                m_UserInfoRequest?.Abort();
                m_AccessTokenRequest?.Abort();

                if (loggedIn)
                {
                    GetAuthCode();
                }
            }

            public void FetchUserInfo(Action<UserInfo> doneCallbackInfo)
            {
                if (m_UserInfo.isValid)
                    doneCallbackInfo?.Invoke(m_UserInfo);
                else
                {
                    if (doneCallbackInfo != null)
                        m_DoneCallbackList.Add(doneCallbackInfo);
                    GetAuthCode();
                }
            }

            private void OnDoneFetchUserInfo()
            {
                var currList = m_DoneCallbackList;
                m_DoneCallbackList = new List<Action<UserInfo>>();
                for (int i = 0; i < currList.Count; i++)
                    currList[i].Invoke(m_UserInfo);
            }

            private bool m_AuthCodeRequested;

            private void GetAuthCode()
            {
                if (!string.IsNullOrEmpty(m_UserInfo.authCode))
                {
                    GetAccessToken();
                    return;
                }
                if (m_AuthCodeRequested)
                {
                    return;
                    // a request is already running, no need to recall
                }

                m_AuthCodeRequested = true;
                try
                {
                    UnityOAuth.GetAuthorizationCodeAsync(kServiceId, authCodeResponse =>
                    {
                        if (authCodeResponse.AuthCode != null)
                        {
                            m_UserInfo.authCode = authCodeResponse.AuthCode;
                            GetAccessToken();
                        }
                        else
                        {
                            m_UserInfo.authCode = "";
                            m_UserInfo.errorMessage = authCodeResponse.Exception.ToString();
                            OnDoneFetchUserInfo();
                        }
                        m_AuthCodeRequested = false;
                    });
                }
                catch (Exception e)
                {
                    m_UserInfo.authCode = "";
                    m_UserInfo.errorMessage = e.Message;
                    OnDoneFetchUserInfo();
                    m_AuthCodeRequested = false;
                }
            }

            private void GetAccessToken()
            {
                if (string.IsNullOrEmpty(m_UserInfo.authCode))
                {
                    GetAuthCode();
                    return;
                }
                if (m_UserInfo.accessToken != null && m_UserInfo.accessToken.IsValid())
                {
                    GetTokenInfo();
                    return;
                }
                if (m_AccessTokenRequest != null)
                {
                    return;
                    // a request is already running, no need to recall
                }
                m_AccessTokenRequest = ApplicationUtil.instance.GetASyncHTTPClient($"{m_Host}{kOAuthUri}", "POST");
                m_AccessTokenRequest.postData = $"grant_type=authorization_code&code={m_UserInfo.authCode}&client_id=packman&client_secret={m_Secret}&redirect_uri=packman://unity";
                m_AccessTokenRequest.header["Content-Type"] = "application/x-www-form-urlencoded";
                m_AccessTokenRequest.doneCallback = httpClient =>
                {
                    m_AccessTokenRequest = null;
                    m_UserInfo.accessToken = null;
                    if (httpClient.IsSuccess())
                    {
                        try
                        {
                            var res = Json.Deserialize(httpClient.text) as Dictionary<string, object>;
                            if (res != null)
                            {
                                var accessTokenResponse = new AccessToken();
                                accessTokenResponse.access_token = res["access_token"] as string;
                                accessTokenResponse.token_type = res["token_type"] as string;
                                accessTokenResponse.expires_in = res["expires_in"] as string;
                                accessTokenResponse.refresh_token = res["refresh_token"] as string;
                                accessTokenResponse.user = res["user"] as string;
                                accessTokenResponse.display_name = res["display_name"] as string;
                                m_UserInfo.accessToken = accessTokenResponse;
                                if (m_UserInfo.accessToken.IsValid())
                                {
                                    GetTokenInfo();
                                    return;
                                }

                                m_UserInfo.errorMessage = "Access token invalid";
                            }
                            else
                            {
                                m_UserInfo.errorMessage = "Failed to parse JSON.";
                            }
                        }
                        catch (Exception e)
                        {
                            m_UserInfo.errorMessage = $"Failed to parse JSON: {e.Message}";
                        }
                    }
                    else
                    {
                        m_UserInfo.errorMessage = httpClient.text;
                    }

                    OnDoneFetchUserInfo();
                };
                m_AccessTokenRequest.Begin();
            }

            private void GetTokenInfo()
            {
                if (m_UserInfo.accessToken == null || !m_UserInfo.accessToken.IsValid())
                {
                    GetAccessToken();
                    return;
                }
                if (m_UserInfo.tokenInfo != null && m_UserInfo.tokenInfo.IsValid())
                {
                    GetUserInfo();
                    return;
                }
                if (m_TokenRequest != null)
                {
                    return;
                    // a request is already running, no need to recall
                }

                m_TokenRequest = ApplicationUtil.instance.GetASyncHTTPClient($"{m_Host}{kTokenInfoUri}?access_token={m_UserInfo.accessToken.access_token}");
                m_TokenRequest.doneCallback = httpClient =>
                {
                    m_TokenRequest = null;
                    m_UserInfo.tokenInfo = null;

                    if (httpClient.IsSuccess())
                    {
                        try
                        {
                            var res = Json.Deserialize(httpClient.text) as Dictionary<string, object>;
                            if (res != null)
                            {
                                var tokenInfo = new TokenInfo();
                                tokenInfo.sub = res["sub"] as string;
                                tokenInfo.scopes = res["scopes"] as string;
                                tokenInfo.expires_in = res["expires_in"] as string;
                                tokenInfo.client_id = res["client_id"] as string;
                                tokenInfo.ip_address = res["ip_address"] as string;
                                tokenInfo.access_token = res["access_token"] as string;
                                m_UserInfo.tokenInfo = tokenInfo;
                                if (m_UserInfo.tokenInfo.IsValid())
                                {
                                    GetUserInfo();
                                    return;
                                }

                                m_UserInfo.errorMessage = "TokenInfo invalid";
                            }
                            else
                            {
                                m_UserInfo.errorMessage = "Failed to parse JSON.";
                            }
                        }
                        catch (Exception e)
                        {
                            m_UserInfo.errorMessage = $"Failed to parse JSON: {e.Message}";
                        }
                    }
                    else
                    {
                        m_UserInfo.errorMessage = httpClient.text;
                    }

                    OnDoneFetchUserInfo();
                };
                m_TokenRequest.Begin();
            }

            private void GetUserInfo()
            {
                if (m_UserInfo.accessToken == null || !m_UserInfo.accessToken.IsValid())
                {
                    GetAccessToken();
                    return;
                }
                if (m_UserInfo.tokenInfo == null || !m_UserInfo.tokenInfo.IsValid())
                {
                    GetTokenInfo();
                    return;
                }
                if (m_UserInfoRequest != null)
                {
                    return;
                    // a request is already running, no need to recall
                }

                m_UserInfoRequest = ApplicationUtil.instance.GetASyncHTTPClient($"{m_Host}{kUserInfoUri}/{m_UserInfo.tokenInfo.sub}");
                m_UserInfoRequest.header["Authorization"] = "Bearer " + m_UserInfo.accessToken.access_token;
                m_UserInfoRequest.doneCallback = httpClient =>
                {
                    m_UserInfoRequest = null;
                    m_UserInfo.isValid = false;

                    if (httpClient.IsSuccess())
                    {
                        try
                        {
                            var res = Json.Deserialize(httpClient.text) as Dictionary<string, object>;
                            if (res != null)
                            {
                                m_UserInfo.id = res["id"] as string;
                                m_UserInfo.username = res["username"] as string;
                                var extended = res["extendedProperties"] as Dictionary<string, object>;
                                m_UserInfo.defaultOrganization = extended["UNITY_DEFAULT_ORGANIZATION"] as string;
                                m_UserInfo.isValid = true;
                                m_UserInfo.errorMessage = "";
                            }
                            else
                            {
                                m_UserInfo.errorMessage = "Failed to parse JSON.";
                            }
                        }
                        catch (Exception e)
                        {
                            m_UserInfo.errorMessage = $"Failed to parse JSON: {e.Message}";;
                        }
                    }
                    else
                    {
                        m_UserInfo.errorMessage = httpClient.text;
                    }
                    OnDoneFetchUserInfo();
                };
                m_UserInfoRequest.Begin();
            }
        }
    }
}
