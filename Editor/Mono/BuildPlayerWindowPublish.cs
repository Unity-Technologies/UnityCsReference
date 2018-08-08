// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor
{
    public partial class BuildPlayerWindow : EditorWindow
    {
        private struct RequestQueueItem
        {
            public Request Request;
            public PackmanOperationType OperationType;

            public RequestQueueItem(Request r, PackmanOperationType type)
            {
                Request = r;
                OperationType = type;
            }
        }

        enum PackmanOperationType : uint
        {
            None = 0,
            List,
            Add,
            Remove,
            Search,
            Outdated
        }

        private bool IsVersionInitialized
        {
            get { return currentVersionInitialized && latestVersionInitialized; }
        }

        private bool currentVersionInitialized = false;
        private bool latestVersionInitialized = false;
        private bool xiaomiPackageInstalled = false;
        private bool packmanOperationRunning = false;
        private string xiaomiPackageName = "com.unity.xiaomi";
        private string currentXiaomiPackageVersion = "";
        private string latestXiaomiPackageVersion = "";

        private List<RequestQueueItem> requestList = new List<RequestQueueItem>();

        private bool requestingCurrentPackage = false;
        private bool requestingLatestPackage = false;

        string CurrentXiaomiPackageId
        {
            get
            {
                return xiaomiPackageName + "@" + currentXiaomiPackageVersion;
            }
        }

        string LatestXiaomiPackageId
        {
            get
            {
                return xiaomiPackageName + "@" + latestXiaomiPackageVersion;
            }
        }

        class PublishStyles
        {
            public const int kIconSize = 32;
            public const int kRowHeight = 36;
            public GUIContent xiaomiIcon = EditorGUIUtility.IconContent("BuildSettings.Xiaomi");
            public GUIContent learnAboutXiaomiInstallation = EditorGUIUtility.TextContent("Installation and Setup");
            public GUIContent publishTitle = EditorGUIUtility.TextContent("SDKs for App Stores|Integrations with 3rd party app stores");
        }
        private PublishStyles publishStyles = null;

        private void AndroidPublishGUI()
        {
            if (publishStyles == null)
                publishStyles = new PublishStyles();

            GUILayout.BeginVertical();
            GUILayout.Label(publishStyles.publishTitle, styles.title);

            // Show Xiaomi UI.
            using (new EditorGUILayout.HorizontalScope(styles.box, new GUILayoutOption[] { GUILayout.Height(PublishStyles.kRowHeight) }))
            {
                GUILayout.BeginVertical();
                GUILayout.Space(3); // fix top padding for box style

                GUILayout.BeginHorizontal();
                GUILayout.Space(4); // left padding

                // icon
                GUILayout.BeginVertical();
                GUILayout.Space((PublishStyles.kRowHeight - PublishStyles.kIconSize) / 2);
                GUILayout.Label(publishStyles.xiaomiIcon, new GUILayoutOption[] { GUILayout.Width(PublishStyles.kIconSize), GUILayout.Height(PublishStyles.kIconSize) });
                GUILayout.EndVertical();

                // label
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Xiaomi Mi Game Center");
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                // link
                GUILayout.FlexibleSpace(); // right justify text
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                XiaomiPackageControlGUI();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                GUILayout.Space(4); // right padding
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
        }

        private void XiaomiPackageControlGUI()
        {
            EditorGUI.BeginDisabledGroup(!IsVersionInitialized || packmanOperationRunning);

            if (!xiaomiPackageInstalled)
            {
                if (GUILayout.Button("Add", GUILayout.Width(60)))
                {
                    if (packmanOperationRunning)
                        return;

                    AddRequest add = Client.Add(LatestXiaomiPackageId);
                    requestList.Add(new RequestQueueItem(add, PackmanOperationType.Add));
                    System.Console.WriteLine("Adding: " + LatestXiaomiPackageId);
                    packmanOperationRunning = true;
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (!string.IsNullOrEmpty(latestXiaomiPackageVersion) && currentXiaomiPackageVersion != latestXiaomiPackageVersion)
                {
                    if (GUILayout.Button("Update", GUILayout.Width(60)))
                    {
                        if (packmanOperationRunning)
                            return;

                        if (EditorUtility.DisplayDialog("Update Xiaomi SDK", "Are you sure you want to update to " + latestXiaomiPackageVersion + " ?", "Yes", "No"))
                        {
                            AddRequest add = Client.Add(LatestXiaomiPackageId);
                            requestList.Add(new RequestQueueItem(add, PackmanOperationType.Add));
                            System.Console.WriteLine("Updating to: " + LatestXiaomiPackageId);
                            packmanOperationRunning = true;
                        }
                    }
                }
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    if (packmanOperationRunning)
                        return;

                    RemoveRequest remove = Client.Remove(xiaomiPackageName);
                    requestList.Add(new RequestQueueItem(remove, PackmanOperationType.Remove));
                    System.Console.WriteLine("Removing Xiaomi Package: " + CurrentXiaomiPackageId);
                    packmanOperationRunning = true;
                }
                GUILayout.EndHorizontal();
            }

            EditorGUI.EndDisabledGroup();
        }

        void CheckXiaomiPackageVersions()
        {
            if (IsVersionInitialized)
                return;

            if (string.IsNullOrEmpty(currentXiaomiPackageVersion) && !requestingCurrentPackage)
            {
                requestList.Add(new RequestQueueItem(Client.List(), PackmanOperationType.List));
                packmanOperationRunning = requestingCurrentPackage = true;
            }

            if (string.IsNullOrEmpty(latestXiaomiPackageVersion) && !requestingLatestPackage)
            {
                requestList.Add(new RequestQueueItem(Client.Search(xiaomiPackageName), PackmanOperationType.Search));
                packmanOperationRunning = requestingLatestPackage = true;
            }
        }

        void Update()
        {
            //Initialization
            CheckXiaomiPackageVersions();

            if (!packmanOperationRunning)
                return;

            packmanOperationRunning = !CheckPackmanOperation();
        }

        bool CheckPackmanOperation()
        {
            if (requestList.Count == 0)
                return true;

            RequestQueueItem requestItem = requestList[0];
            StatusCode statusCode = requestItem.Request.Status;

            if (statusCode == StatusCode.Failure)
            {
                Error error = requestItem.Request.Error;
                Debug.LogError("Operation " + requestItem.Request + " failed with Error: " + error);
                return true;
            }
            else if (statusCode == StatusCode.InProgress)
            {
                return false;
            }
            else if (statusCode == StatusCode.Success)
            {
                System.Console.WriteLine("Operation " + requestItem.Request + " Done");
                switch (requestItem.OperationType)
                {
                    case PackmanOperationType.List:
                        ExtractCurrentXiaomiPackageInfo(requestItem);
                        break;
                    case PackmanOperationType.Add:
                        PerformAdd(requestItem);
                        break;
                    case PackmanOperationType.Remove:
                        PerformRemove(requestItem.Request.Status);
                        break;
                    case PackmanOperationType.Search:
                        ExtractLatestXiaomiPackageInfo(requestItem);
                        break;
                    default:
                        System.Console.WriteLine("Type " + requestItem.OperationType + " Not Supported");
                        break;
                }
                requestList.RemoveAt(0);
                if (requestList.Count > 0)
                    return false;
                else
                    return true;
            }
            return false;
        }

        void PerformAdd(RequestQueueItem request)
        {
            currentXiaomiPackageVersion = latestXiaomiPackageVersion = ((AddRequest)request.Request).Result.version;
            if (request.Request.Status == StatusCode.Failure)
            {
                Debug.LogError("Adding/Updating to " + latestXiaomiPackageVersion + " resulted in error, please add it again.");
                return;
            }
            xiaomiPackageInstalled = true;
        }

        void PerformRemove(StatusCode status)
        {
            if (status == StatusCode.Failure)
            {
                Debug.LogError("Remove " + currentXiaomiPackageVersion + " error, please remove it again.");
                return;
            }
            else if (status == StatusCode.Success)
            {
                currentXiaomiPackageVersion = null;
                xiaomiPackageInstalled = false;
            }
        }

        void ExtractCurrentXiaomiPackageInfo(RequestQueueItem request)
        {
            ListRequest currentPackageListRequest = (ListRequest)request.Request;
            if (currentPackageListRequest.Status == StatusCode.Success)
            {
                System.Console.WriteLine("Current xiaomi package version is " +
                    (string.IsNullOrEmpty(currentXiaomiPackageVersion)
                        ? "empty"
                        : currentXiaomiPackageVersion));

                if (currentPackageListRequest.IsCompleted && string.IsNullOrEmpty(currentXiaomiPackageVersion))
                {
                    PackageManager.PackageInfo info = currentPackageListRequest.Result.FirstOrDefault(p => p.name == xiaomiPackageName);
                    if (info != null)
                        currentXiaomiPackageVersion = info.version;
                    if (!string.IsNullOrEmpty(currentXiaomiPackageVersion))
                        xiaomiPackageInstalled = true;
                }
                currentVersionInitialized = true;
            }
            else
                System.Console.WriteLine(currentPackageListRequest + " failed with error: " +
                    currentPackageListRequest.Error);
        }

        void ExtractLatestXiaomiPackageInfo(RequestQueueItem request)
        {
            SearchRequest latestPackageSearchRequest = (SearchRequest)request.Request;
            if (latestPackageSearchRequest.Status == StatusCode.Success)
            {
                System.Console.WriteLine("Latest xiaomi package version is " +
                    (string.IsNullOrEmpty(latestXiaomiPackageVersion)
                        ? "empty"
                        : latestXiaomiPackageVersion));

                if (latestPackageSearchRequest.IsCompleted && latestPackageSearchRequest.Result.Length > 0 &&
                    string.IsNullOrEmpty(latestXiaomiPackageVersion))
                    latestXiaomiPackageVersion = latestPackageSearchRequest.Result[0].version;
                latestVersionInitialized = true;
            }
            else
                System.Console.WriteLine(latestPackageSearchRequest + " failed with error: " +
                    latestPackageSearchRequest.Error);
        }
    }
}
