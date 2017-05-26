// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEditor;
using UnityEditor.Connect;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    internal class TroubleshooterAccess
    {
        private TroubleshooterAccess()
        {
            // Nothing to do
        }

        public string GetUserName()
        {
            var uc = UnityConnect.instance;
            if (!uc.GetConnectInfo().loggedIn)
            {
                return "Anonymous";
            }
            return uc.GetUserName();
        }

        public string GetUserId()
        {
            var uc = UnityConnect.instance;
            if (!uc.GetConnectInfo().loggedIn)
            {
                return string.Empty;
            }
            return uc.GetUserInfo().userId;
        }

        public void SignIn()
        {
            UnityConnect.instance.ShowLogin();
        }

        public void SignOut()
        {
            UnityConnect.instance.Logout();
        }

        public void StartBugReporter()
        {
            EditorUtility.LaunchBugReporter();
        }

        static TroubleshooterAccess()
        {
            JSProxyMgr.GetInstance().AddGlobalObject("/unity/editor/troubleshooter", new TroubleshooterAccess());
        }
    }
}

