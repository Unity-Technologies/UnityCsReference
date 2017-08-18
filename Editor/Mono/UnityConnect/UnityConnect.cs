// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEditor.Web;
using UnityEngine;

namespace UnityEditor.Connect
{
    internal delegate void StateChangedDelegate(ConnectInfo state);
    internal delegate void ProjectStateChangedDelegate(ProjectInfo state);
    internal delegate void UserStateChangedDelegate(UserInfo state);

    public static class UnityOAuth
    {
        public static event Action UserLoggedIn;
        public static event Action UserLoggedOut;

        public struct AuthCodeResponse
        {
            public string AuthCode { get; set; }
            public Exception Exception { get; set; }
        }

        public static void GetAuthorizationCodeAsync(string clientId, Action<AuthCodeResponse> callback)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentException("clientId is null or empty.", "clientId");
            }

            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            if (string.IsNullOrEmpty(UnityConnect.instance.GetAccessToken()))
            {
                throw new InvalidOperationException("User is not logged in or user status invalid.");
            }

            string url = string.Format("{0}/v1/oauth2/authorize", UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudIdentity));

            AsyncHTTPClient client = new AsyncHTTPClient(url);
            client.postData = string.Format("client_id={0}&response_type=code&format=json&access_token={1}&prompt=none",
                    clientId,
                    UnityConnect.instance.GetAccessToken());
            client.doneCallback = delegate(AsyncHTTPClient c) {
                    AuthCodeResponse response = new AuthCodeResponse();
                    if (!c.IsSuccess())
                    {
                        response.Exception = new InvalidOperationException("Failed to call Unity ID to get auth code.");
                    }
                    else
                    {
                        try
                        {
                            var json = new JSONParser(c.text).Parse();
                            if (json.ContainsKey("code") && !json["code"].IsNull())
                            {
                                response.AuthCode = json["code"].AsString();
                            }
                            else if (json.ContainsKey("message"))
                            {
                                response.Exception = new InvalidOperationException(string.Format("Error from server: {0}", json["message"].AsString()));
                            }
                            else if (json.ContainsKey("location") && !json["location"].IsNull())
                            {
                                UnityConnectConsentView consentView = UnityConnectConsentView.ShowUnityConnectConsentView(json["location"].AsString());
                                if (!string.IsNullOrEmpty(consentView.Code))
                                {
                                    response.AuthCode = consentView.Code;
                                }
                                else if (!string.IsNullOrEmpty(consentView.Error))
                                {
                                    response.Exception = new InvalidOperationException(string.Format("Error from server: {0}", consentView.Error));
                                }
                                else
                                {
                                    response.Exception = new InvalidOperationException("Consent Windows was closed unexpected.");
                                }
                            }
                            else
                            {
                                response.Exception = new InvalidOperationException("Unexpected response from server.");
                            }
                        }
                        catch (JSONParseException)
                        {
                            response.Exception = new InvalidOperationException("Unexpected response from server: Failed to parse JSON.");
                        }
                    }

                    callback(response);
                };
            client.Begin();
        }

        private static void OnUserLoggedIn()
        {
            if (UserLoggedIn != null)
                UserLoggedIn();
        }

        private static void OnUserLoggedOut()
        {
            if (UserLoggedOut != null)
                UserLoggedOut();
        }
    }

    [InitializeOnLoad]
    internal partial class UnityConnect
    {
        public event StateChangedDelegate StateChanged;
        public event ProjectStateChangedDelegate ProjectStateChanged;
        public event UserStateChangedDelegate UserStateChanged;

        private static readonly UnityConnect s_Instance;

        [Flags]
        internal enum UnityErrorPriority
        {
            Critical = 0,
            Error,
            Warning,
            Info,
            None
        };

        [Flags]
        internal enum UnityErrorBehaviour
        {
            Alert = 0,
            Automatic,
            Hidden,
            ConsoleOnly,
            Reconnect
        };

        [Flags]
        internal enum UnityErrorFilter
        {
            ByContext = 1,
            ByParent  = 2,
            ByChild   = 4,
            All       = 7
        };

        private UnityConnect()
        {
            PackageUtils.instance.RetrievePackageInfo();
        }

        public void GoToHub(string page)
        {
            UnityEditor.Connect.UnityConnectServiceCollection.instance.ShowService(UnityEditor.Web.HubAccess.kServiceName, page, true, "goto_hub_method");
        }


        public void UnbindProject()
        {
            UnbindCloudProject();
            UnityConnectServiceCollection.instance.UnbindAllServices();
        }

        // For Javascript Only
        public ProjectInfo GetProjectInfo()
        {
            return projectInfo;
        }


        public UserInfo GetUserInfo()
        {
            return userInfo;
        }

        public ConnectInfo GetConnectInfo()
        {
            return connectInfo;
        }

        public string GetConfigurationUrlByIndex(int index)
        {
            if (index == 0)
                return GetConfigurationURL(CloudConfigUrl.CloudCore);
            if (index == 1)
                return GetConfigurationURL(CloudConfigUrl.CloudCollab);
            if (index == 2)
                return GetConfigurationURL(CloudConfigUrl.CloudWebauth);
            if (index == 3)
                return GetConfigurationURL(CloudConfigUrl.CloudLogin);
            // unityeditor-cloud only called this API with index as {0,1,2,3}.
            // We add the new URLs in case some module might need them in the future
            if (index == 6)
                return GetConfigurationURL(CloudConfigUrl.CloudIdentity);
            if (index == 7)
                return GetConfigurationURL(CloudConfigUrl.CloudPortal);

            return "";
        }

        public string GetCoreConfigurationUrl()
        {
            return GetConfigurationURL(CloudConfigUrl.CloudCore);
        }

        public bool DisplayDialog(string title, string message, string okBtn, string cancelBtn)
        {
            return EditorUtility.DisplayDialog(title, message, okBtn, cancelBtn);
        }

        public bool SetCOPPACompliance(int compliance)
        {
            return SetCOPPACompliance((COPPACompliance)compliance);
        }

        // End for Javascript Only

        [MenuItem("Window/Unity Connect/Computer GoesToSleep", false, 1000, true)]
        public static void TestComputerGoesToSleep()
        {
            instance.ComputerGoesToSleep();
        }

        [MenuItem("Window/Unity Connect/Computer DidWakeUp", false, 1000, true)]
        public static void TestComputerDidWakeUp()
        {
            instance.ComputerDidWakeUp();
        }

        public static UnityConnect instance
        {
            get
            {
                return s_Instance;
            }
        }

        static UnityConnect()
        {
            s_Instance = new UnityConnect();
            JSProxyMgr.GetInstance().AddGlobalObject("unity/connect", s_Instance);
        }

        private static void OnStateChanged()
        {
            var handler = instance.StateChanged;
            if (handler != null)
                handler(instance.connectInfo);
        }

        private static void OnProjectStateChanged()
        {
            var handler = instance.ProjectStateChanged;
            if (handler != null)
                handler(instance.projectInfo);
        }

        private static void OnUserStateChanged()
        {
            var handler = instance.UserStateChanged;
            if (handler != null)
                handler(instance.userInfo);
        }
    };
}
