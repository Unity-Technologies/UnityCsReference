// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;

namespace UnityEditor
{
    public partial class BuildPlayerWindow : EditorWindow
    {
        enum PackmanOperationType : uint
        {
            None = 0,
            List,
            Add,
            Remove,
            Search,
            Outdated
        }

        private long getCurrentVersionOperationId = -1;
        private long getLatestVersionOperationId = -1;
        bool isVersionInitialized = false;

        private long packmanOperationId = -1;


        private PackmanOperationType packmanOperationType = PackmanOperationType.None;
        private bool packmanOperationRunning = false;

        private string xiaomiPackageName = "com.unity.xiaomi";
        private string currentXiaomiPackageVersion = "";
        private string latestXiaomiPackageVersion = "";

        private bool xiaomiPackageInstalled = false;

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
            EditorGUI.BeginDisabledGroup(!isVersionInitialized || packmanOperationRunning);

            if (!xiaomiPackageInstalled)
            {
                if (GUILayout.Button("Add", GUILayout.Width(60)))
                {
                    if (packmanOperationRunning)
                        return;

                    StatusCode code = Client.Add(out packmanOperationId, LatestXiaomiPackageId);
                    if (code == StatusCode.Error)
                    {
                        Debug.LogError("Add: '" + LatestXiaomiPackageId + "' Error");
                        return;
                    }
                    packmanOperationType = PackmanOperationType.Add;
                    Debug.Log("Add operationId: " + packmanOperationId + " for " + LatestXiaomiPackageId);
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
                            StatusCode code = Client.Add(out packmanOperationId, LatestXiaomiPackageId);
                            if (code == StatusCode.Error)
                            {
                                Debug.LogError("Update: '" + LatestXiaomiPackageId + "' Error");
                                return;
                            }
                            packmanOperationType = PackmanOperationType.Add;
                            Debug.Log("Update operationId: " + packmanOperationId + " for " + LatestXiaomiPackageId);
                            packmanOperationRunning = true;
                        }
                    }
                }
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    if (packmanOperationRunning)
                        return;

                    StatusCode code = Client.Remove(out packmanOperationId, CurrentXiaomiPackageId);
                    if (code == StatusCode.Error)
                    {
                        Debug.LogError("Remove: '" + CurrentXiaomiPackageId + "' Error");
                        return;
                    }
                    packmanOperationType = PackmanOperationType.Remove;
                    Debug.Log("Remove operationId: " + packmanOperationId + " for " + CurrentXiaomiPackageId);
                    packmanOperationRunning = true;
                }
                GUILayout.EndHorizontal();
            }

            EditorGUI.EndDisabledGroup();
        }

        bool CheckXiaomiPackageVersions()
        {
            if (isVersionInitialized)
                return true;

            StatusCode getCurrentVersionOperationStatus;
            if (getCurrentVersionOperationId < 0)
                getCurrentVersionOperationStatus = Client.List(out getCurrentVersionOperationId);
            else
                getCurrentVersionOperationStatus = Client.GetOperationStatus(getCurrentVersionOperationId);

            // Reset and return false if it fails with StatusCode.Error or Status.NotFound.
            if (getCurrentVersionOperationStatus > StatusCode.Done)
            {
                getCurrentVersionOperationId = -1;
                return false;
            }

            StatusCode getLatestVersionOperationStatus;
            if (getLatestVersionOperationId < 0)
                getLatestVersionOperationStatus = Client.Search(out getLatestVersionOperationId, xiaomiPackageName);
            else
                getLatestVersionOperationStatus = Client.GetOperationStatus(getLatestVersionOperationId);

            // Reset and return false if it fails with StatusCode.Error or Status.NotFound.
            if (getLatestVersionOperationStatus > StatusCode.Done)
            {
                getLatestVersionOperationId = -1;
                return false;
            }

            // Get version info if both operations are done.
            if (getCurrentVersionOperationStatus == StatusCode.Done && getLatestVersionOperationStatus == StatusCode.Done)
            {
                CheckPackmanOperation(getCurrentVersionOperationId, PackmanOperationType.List);
                CheckPackmanOperation(getLatestVersionOperationId, PackmanOperationType.Search);

                Debug.Log("Current xiaomi package version is " + currentXiaomiPackageVersion);
                Debug.Log("Latest xiaomi package version is " + latestXiaomiPackageVersion);

                isVersionInitialized = true;
                return true;
            }

            return false;
        }

        void Update()
        {
            // If initialization operations haven't been finished.
            if (!CheckXiaomiPackageVersions())
                return;

            if (!packmanOperationRunning)
                return;

            packmanOperationRunning = !CheckPackmanOperation(packmanOperationId, packmanOperationType);
        }

        bool CheckPackmanOperation(long operationId, PackmanOperationType operationType)
        {
            StatusCode statusCode = Client.GetOperationStatus(operationId);
            if (statusCode == StatusCode.NotFound)
            {
                Debug.Log("Operation " + operationId + " Not Found");
                return true;
            }
            else if (statusCode == StatusCode.Error)
            {
                Error error = Client.GetOperationError(operationId);
                Debug.LogError("Operation " + operationId + " failed with Error " + error);
                return true;
            }
            else if (statusCode == StatusCode.InProgress || statusCode == StatusCode.InQueue)
            {
                Debug.Log("OperationID " + operationId + " -> In Progress!");
                return false;
            }
            else if (statusCode == StatusCode.Done)
            {
                Debug.Log("OperationID " + operationId + " -> Done!");
                switch (operationType)
                {
                    case PackmanOperationType.List:
                        ExtractCurrentXiaomiPackageInfo(operationId);
                        break;
                    case PackmanOperationType.Add:
                        currentXiaomiPackageVersion = latestXiaomiPackageVersion;
                        xiaomiPackageInstalled = true;
                        break;
                    case PackmanOperationType.Remove:
                        currentXiaomiPackageVersion = "";
                        xiaomiPackageInstalled = false;
                        break;
                    case PackmanOperationType.Search:
                        ExtractLatestXiaomiPackageInfo(operationId);
                        break;
                    default:
                        Debug.Log("Type " + operationType + " Not Supported");
                        break;
                }
                return true;
            }

            return true;
        }

        void ExtractCurrentXiaomiPackageInfo(long operationId)
        {
            // Get the package list to find if xiaomi package has been installed.
            OperationStatus operationStatus = Client.GetListOperationData(operationId);
            foreach (var package in operationStatus.packageList)
            {
                if (package.packageId.StartsWith(xiaomiPackageName))
                {
                    xiaomiPackageInstalled = true;
                    currentXiaomiPackageVersion = package.version;
                }
            }
        }

        void ExtractLatestXiaomiPackageInfo(long operationId)
        {
            UpmPackageInfo[] packageList = Client.GetSearchOperationData(operationId);
            foreach (var package in packageList)
            {
                latestXiaomiPackageVersion = package.version;
            }
        }
    }
}
