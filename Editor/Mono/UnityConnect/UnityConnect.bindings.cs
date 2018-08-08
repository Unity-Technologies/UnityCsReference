// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEditor.Web;
using UnityEditorInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Connect
{
    //*undocumented*
    internal enum CloudConfigUrl
    {
        CloudCore = 0,
        CloudCollab = 1,
        CloudWebauth = 2,
        CloudLogin = 3,
        CloudLicense = 4,
        CloudActivation = 5,
        CloudIdentity = 6,
        CloudPortal = 7,
        CloudPerfEvents = 8,
        CloudAdsDashboard = 9,
        CloudServicesDashboard = 10
    }

    //*undocumented*
    internal enum COPPACompliance
    {
        COPPAUndefined = 0,
        COPPACompliant = 1,
        COPPANotCompliant = 2
    }

#pragma warning disable 649
    //*undocumented*
    [NativeType(CodegenOptions.Custom, "MonoUnityProjectInfo")]
    internal struct ProjectInfo
    {
        public bool valid { get { return m_Valid != 0; } }
        public bool buildAllowed { get { return m_BuildAllowed != 0; } }
        public bool projectBound { get { return m_ProjectBound != 0; } }
        public string projectGUID { get { return m_ProjectGUID; } }
        public string projectName { get { return m_ProjectName; } }
        public string organizationId { get { return m_OrganizationID; } }
        public string organizationName { get { return m_OrganizationName; } }
        public string organizationForeignKey { get { return m_OrganizationForeignKey; } }
        public COPPACompliance COPPA
        {
            get
            {
                if (m_COPPA == 1) return COPPACompliance.COPPACompliant;
                if (m_COPPA == 2) return COPPACompliance.COPPANotCompliant;
                return COPPACompliance.COPPAUndefined;
            }
        }
        public bool coppaLock { get { return m_COPPALock != 0; } }
        public bool moveLock { get { return m_MoveLock != 0; } }

        int m_Valid;
        int m_BuildAllowed;
        int m_ProjectBound;
        string m_ProjectGUID;
        string m_ProjectName;
        string m_OrganizationID;
        string m_OrganizationName;
        string m_OrganizationForeignKey;
        int m_COPPA;
        int m_COPPALock;
        int m_MoveLock;
    }

    //*undocumented*
    [NativeType(CodegenOptions.Custom, "MonoUnityUserInfo")]
    internal struct UserInfo
    {
        public bool valid { get { return m_Valid != 0; } }
        public string userId { get { return m_UserId; } }
        public string userName { get { return m_UserName; } }
        public string displayName { get { return m_DisplayName; } }
        public string primaryOrg { get { return m_PrimaryOrg; } }
        public bool whitelisted { get { return m_Whitelisted != 0; } }
        public string organizationForeignKeys { get { return m_OrganizationForeignKeys; } }
        public string accessToken { get { return m_AccessToken; } }
        public int accessTokenValiditySeconds { get { return m_AccessTokenValiditySeconds; } }

        int m_Valid;
        string m_UserId;
        string m_UserName;
        string m_DisplayName;
        string m_PrimaryOrg;
        int m_Whitelisted;
        string m_OrganizationForeignKeys;
        string m_AccessToken;
        int m_AccessTokenValiditySeconds;
    }

    //*undocumented*
    [NativeType(CodegenOptions.Custom, "MonoUnityConnectInfo")]
    internal struct ConnectInfo
    {
        public bool initialized { get { return m_Initialized != 0; } }
        public bool ready { get { return m_Ready != 0; } }
        public bool online { get { return m_Online != 0; } }
        public bool loggedIn { get { return m_LoggedIn != 0; } }
        public bool workOffline { get { return m_WorkOffline != 0; } }
        public bool showLoginWindow { get { return m_ShowLoginWindow != 0; } }
        public bool error { get { return m_Error != 0; } }
        public string lastErrorMsg { get { return m_LastErrorMsg; } }
        public bool maintenance { get { return m_Maintenance != 0; } }

        int m_Initialized;
        int m_Ready;
        int m_Online;
        int m_LoggedIn;
        int m_WorkOffline;
        int m_ShowLoginWindow;
        int m_Error;
        string m_LastErrorMsg;
        int m_Maintenance;
    }

#pragma warning disable 1635
#pragma warning restore 649
#pragma warning restore 1635

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
            client.doneCallback = delegate(AsyncHTTPClient c)
            {
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

    [NativeHeader("Editor/Src/UnityConnect/UnityErrors.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnityErrorInfo
    {
        public int code;
        public int priority;
        public int behaviour;
        public string msg;
        public string shortMsg;
        public string codeStr;
    }

    // Keep internal and undocumented until we expose more functionality
    //*undocumented
    [NativeHeader("Editor/Src/UnityConnect/UnityConnect.h")]
    [StaticAccessor("UnityConnect::Get()", StaticAccessorType.Dot)]
    [InitializeOnLoad]
    internal partial class UnityConnect
    {
        public event StateChangedDelegate StateChanged;
        public event ProjectStateChangedDelegate ProjectStateChanged;
        public event UserStateChangedDelegate UserStateChanged;

        Action<bool> m_AccessTokenRefreshed;

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
            ByParent = 2,
            ByChild = 4,
            All = 7
        };

        private UnityConnect()
        {
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
        [MenuItem("Window/Unity Connect/Clear Access Token", false, 1000, true)]
        public static void InvokeClearAccessTokenForTesting()
        {
            instance.ClearAccessToken();
        }

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

        public static extern bool preferencesEnabled { get; }

        public static extern bool skipMissingUPID { get; }

        private static extern bool Online();
        public bool online
        {
            get { return Online(); }
        }

        private static extern bool LoggedIn();
        public bool loggedIn
        {
            get { return LoggedIn(); }
        }

        private static extern bool ProjectValid();
        public bool projectValid
        {
            get { return ProjectValid(); }
        }

        private static extern bool WorkingOffline();
        public bool workingOffline
        {
            get { return WorkingOffline(); }
        }

        private static extern string GetConfigEnvironment();
        public string configuration
        {
            get { return GetConfigEnvironment(); }
        }

        private static extern string GetConfigUrl(CloudConfigUrl config);
        public string GetConfigurationURL(CloudConfigUrl config)
        {
            return GetConfigUrl(config);
        }

        [NativeMethod("isDisableServicesWindow")]
        private static extern bool isDisableServicesWindow_Internal();
        public bool isDisableServicesWindow
        {
            get
            {
                return isDisableServicesWindow_Internal();
            }
        }

        [NativeMethod("isDisableUserLogin")]
        private static extern bool isDisableUserLogin_Internal();
        public bool isDisableUserLogin
        {
            get
            {
                return isDisableUserLogin_Internal();
            }
        }

        [NativeMethod("isDisableCollabWindow")]
        private static extern bool isDisableCollabWindow_Internal();
        public bool isDisableCollabWindow
        {
            get
            {
                return isDisableCollabWindow_Internal();
            }
        }


        public string GetEnvironment()
        {
            return GetConfigEnvironment();
        }

        private static extern string GetConfigAPIVersion();
        public string GetAPIVersion()
        {
            return GetConfigAPIVersion();
        }

        [NativeMethod("GetUserId")]
        private static extern string GetUserId_Internal();
        public string GetUserId()
        {
            return GetUserId_Internal();
        }

        [NativeMethod("GetUserName")]
        private static extern string GetUserName_Internal();
        public string GetUserName()
        {
            return GetUserName_Internal();
        }

        [NativeMethod("GetAccessToken")]
        private static extern string GetAccessToken_Internal();
        public string GetAccessToken()
        {
            return GetAccessToken_Internal();
        }

        [NativeMethod("ClearAccessToken")]
        private static extern void ClearAccessToken_Internal();
        public void ClearAccessToken()
        {
            ClearAccessToken_Internal();
        }

        private static extern void RefreshAccessToken();
        public void RefreshAccessToken(Action<bool> refresh)
        {
            m_AccessTokenRefreshed += refresh;
            RefreshAccessToken();
        }

        [RequiredByNativeCode]
        internal static void AccessTokenRefreshed(bool success)
        {
            if (instance.m_AccessTokenRefreshed != null)
            {
                instance.m_AccessTokenRefreshed(success);
                instance.m_AccessTokenRefreshed = null;
            }
        }

        [NativeMethod("GetProjectGUID")]
        private static extern string GetProjectGUID_Internal();
        public string GetProjectGUID()
        {
            return GetProjectGUID_Internal();
        }

        [NativeMethod("GetProjectName")]
        private static extern string GetProjectName_Internal();
        public string GetProjectName()
        {
            return GetProjectName_Internal();
        }

        [NativeMethod("GetOrganizationId")]
        private static extern string GetOrganizationId_Internal();
        public string GetOrganizationId()
        {
            return GetOrganizationId_Internal();
        }

        [NativeMethod("GetOrganizationName")]
        private static extern string GetOrganizationName_Internal();
        public string GetOrganizationName()
        {
            return GetOrganizationName_Internal();
        }

        [NativeMethod("GetOrganizationForeignKey")]
        private static extern string GetOrganizationForeignKey_Internal();
        public string GetOrganizationForeignKey()
        {
            return GetOrganizationForeignKey_Internal();
        }

        [NativeMethod("RefreshProject")]
        private static extern void RefreshProject_Internal();
        public void RefreshProject()
        {
            RefreshProject_Internal();
        }

        [NativeMethod("ClearCache")]
        private static extern void ClearCache_Internal();
        public void ClearCache()
        {
            ClearCache_Internal();
        }

        private static extern void Logout(bool clearUserInfo);
        public void Logout()
        {
            Logout(true);
        }

        [NativeMethod("WorkOffline")]
        private static extern void WorkOffline_Internal(bool rememberDecision);
        public void WorkOffline(bool rememberDecision)
        {
            WorkOffline_Internal(rememberDecision);
        }

        [NativeMethod("ShowLogin")]
        private static extern void ShowLogin_Internal();
        public void ShowLogin()
        {
            ShowLogin_Internal();
        }

        [NativeMethod("OpenAuthorizedURLInWebBrowser")]
        private static extern void OpenAuthorizedURLInWebBrowser_Internal(string url);
        public void OpenAuthorizedURLInWebBrowser(string url)
        {
            OpenAuthorizedURLInWebBrowser_Internal(url);
        }

        [NativeMethod("BindProject")]
        private static extern void BindProject_Internal(string projectGUID, string projectName, string organizationId);
        public void BindProject(string projectGUID, string projectName, string organizationId)
        {
            BindProject_Internal(projectGUID, projectName, organizationId);
        }

        [NativeMethod("UnbindProject")]
        private static extern void UnbindProject_Internal();
        private void UnbindCloudProject()
        {
            UnbindProject_Internal();
        }

        [NativeMethod("SetCOPPACompliance")]
        private static extern void SetCOPPACompliance_Internal(COPPACompliance compliance);
        public bool SetCOPPACompliance(COPPACompliance compliance)
        {
            if (compliance == COPPACompliance.COPPAUndefined)
            {
                return false;
            }
            SetCOPPACompliance_Internal(compliance);
            return true;
        }

        private static extern string GetLastErrorMessage();
        public string lastErrorMessage
        {
            get { return GetLastErrorMessage(); }
        }

        private static extern int GetLastErrorCode();
        public int lastErrorCode
        {
            get { return GetLastErrorCode(); }
        }

        [NativeMethod("SetError")]
        private static extern void SetError_Internal(int errorCode);
        public void SetError(int errorCode)
        {
            SetError_Internal(errorCode);
        }

        [NativeMethod("ClearError")]
        private static extern void ClearError_Internal(int errorCode);
        public void ClearError(int errorCode)
        {
            ClearError_Internal(errorCode);
        }

        [NativeMethod("ClearErrors")]
        private static extern void ClearErrors_Internal();
        public void ClearErrors()
        {
            ClearErrors_Internal();
        }

        [NativeMethod("UnhandledError")]
        private static extern void UnhandledError_Internal(string request, int responseCode, string response);
        public void UnhandledError(string request, int responseCode, string response)
        {
            UnhandledError_Internal(request, responseCode, response);
        }

        private static extern void InvokeComputerGoesToSleep();
        public void ComputerGoesToSleep()
        {
            InvokeComputerGoesToSleep();
        }

        private static extern void InvokeComputerDidWakeUp();
        public void ComputerDidWakeUp()
        {
            InvokeComputerDidWakeUp();
        }

        [NativeMethod("GetUserInfo")]
        private static extern UserInfo GetUserInfo_Internal();
        public UserInfo userInfo
        {
            get { return GetUserInfo_Internal(); }
        }

        [NativeMethod("GetProjectInfo")]
        private static extern ProjectInfo GetProjectInfo_Internal();
        public ProjectInfo projectInfo
        {
            get { return GetProjectInfo_Internal(); }
        }

        [NativeMethod("GetConnectInfo")]
        private static extern ConnectInfo GetConnectInfo_Internal();
        public ConnectInfo connectInfo
        {
            get { return GetConnectInfo_Internal(); }
        }

        private static extern bool CanBuildWithUPID();
        public bool canBuildWithUPID
        {
            get { return CanBuildWithUPID(); }
        }
    }
}
