// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Connect;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    internal partial class EditorProjectAccess
    {
        const string kCloudServiceKey = "CloudServices";
        const string kCloudEnabled = "CloudEnabled";

        public void OpenLink(string link)
        {
            Help.BrowseURL(link);
        }

        public  bool IsOnline()
        {
            return UnityConnect.instance.online;
        }

        public  bool IsLoggedIn()
        {
            return UnityConnect.instance.loggedIn;
        }

        public string GetEnvironment()
        {
            return UnityConnect.instance.GetEnvironment();
        }

        public string GetUserName()
        {
            return UnityConnect.instance.userInfo.userName;
        }

        public string GetUserDisplayName()
        {
            return UnityConnect.instance.userInfo.displayName;
        }

        public string GetUserPrimaryOrganizationId()
        {
            return UnityConnect.instance.userInfo.primaryOrg;
        }

        public string GetUserAccessToken()
        {
            return UnityConnect.instance.GetAccessToken();
        }

        public string GetProjectName()
        {
            string name = UnityConnect.instance.projectInfo.projectName;
            if (name != "")
                return name;

            return PlayerSettings.productName;
        }

        public string GetProjectGUID()
        {
            return UnityConnect.instance.projectInfo.projectGUID;
        }


        public string GetProjectPath()
        {
            return Directory.GetCurrentDirectory();
        }

        public string GetProjectIcon()
        {
            // we should get the real project icon and base64 encode it and return it here
            return null;
        }

        public string GetOrganizationID()
        {
            return UnityConnect.instance.projectInfo.organizationId;
        }


        public string GetBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTarget.ToString();
        }

        public bool IsProjectBound()
        {
            return UnityConnect.instance.projectInfo.projectBound;
        }

        public void EnableCloud(bool enable)
        {
            // should store in settings + send info to server
            EditorUserSettings.SetConfigValue(kCloudServiceKey + "/" + kCloudEnabled, enable.ToString());
        }

        public void EnterPlayMode()
        {
            EditorApplication.isPlaying = true;
        }

        public bool IsPlayMode()
        {
            return EditorApplication.isPlaying;
        }

        public bool SaveCurrentModifiedScenesIfUserWantsTo()
        {
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        public int GetEditorSkinIndex()
        {
            int skinIndex = EditorGUIUtility.skinIndex;
            return skinIndex;
        }

        static EditorProjectAccess()
        {
            JSProxyMgr.GetInstance().AddGlobalObject("unity/project", new EditorProjectAccess());
        }

        public void GoToHistory()
        {
            CollabHistoryWindow.ShowHistoryWindow().Focus();
        }

        public static void ShowToolbarDropdown()
        {
            Toolbar.requestShowCollabToolbar = true;
            if (Toolbar.get)
            {
                Toolbar.get.Repaint();
            }
        }

        public void CloseToolbarWindow()
        {
            CollabToolbarWindow.CloseToolbarWindows();
        }

        public void CloseToolbarWindowImmediately()
        {
            CollabToolbarWindow.CloseToolbarWindowsImmediately();
        }

    }
}

