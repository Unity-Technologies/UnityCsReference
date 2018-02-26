// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
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

        /* The GUIDs are from a unitypackage (UnityChannel.unitypackage - several versions of it, factually).
         *
         * 8be17ad05558e40a9bb709807433a2b4: Assets/Plugins/UnityChannel/ChannelPurchase.dll.meta
         * b4a9e40b1d1574159814dede56a53cb3: Assets/Plugins/UnityChannel/UnityStore.dll.meta
         * 7f9b343cc0cc840ac9096a4e0c9e620a: Assets/Plugins/UnityChannel/Android/UnityChannel.aar.meta
         * ee71d6ce2ccbe44be9906cf88b06dd3c: Assets/Plugins/UnityChannel/Android.meta
         * cfd375853ab09456aaf09e607610061a: Assets/Plugins/UnityChannel/XiaomiSupport/AppStoreSettings.cs.meta
         * eca06513904414b078a830503af689fe: Assets/Plugins/UnityChannel/XiaomiSupport/Editor/XiaomiPackageNameExtension.cs.meta
         * 0668a01a6fc274116958b6c9a8326652: Assets/Plugins/UnityChannel/XiaomiSupport/Editor/AppStoreOnboardApi.cs.meta
         * ad6a04a95a0e44de5840a9487311e96b: Assets/Plugins/UnityChannel/XiaomiSupport/Editor/AppStoreSettingsEditor.cs.meta
         * 1727faade967f43b6a6dba36609f4bdb: Assets/Plugins/UnityChannel/XiaomiSupport/Editor/AppStoreModel.cs.meta
         * fcb4b3fa8d4f74acca2ce443650b2790: Assets/Plugins/UnityChannel/XiaomiSupport/Editor.meta
         * a095042fdce9f439e89ed68ae87f0035: Assets/Plugins/UnityChannel/XiaomiSupport.meta
         * 965c7e6a0de3a40e4a2ccd705b0305d0: Assets/Plugins/UnityChannel.meta
         */

        string[] guidsToDelete = new string[]
        {
            "965c7e6a0de3a40e4a2ccd705b0305d0",
            "8be17ad05558e40a9bb709807433a2b4",
            "a095042fdce9f439e89ed68ae87f0035",
            "7f9b343cc0cc840ac9096a4e0c9e620a",
            "b4a9e40b1d1574159814dede56a53cb3",
            "ee71d6ce2ccbe44be9906cf88b06dd3c",
            "cfd375853ab09456aaf09e607610061a",
            "fcb4b3fa8d4f74acca2ce443650b2790",
            "eca06513904414b078a830503af689fe",
            "0668a01a6fc274116958b6c9a8326652",
            "ad6a04a95a0e44de5840a9487311e96b",
            "1727faade967f43b6a6dba36609f4bdb"
        };

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

                    DeleteUnityChannelObjectsIfInProject();

                    NativeClient.StatusCode code = NativeClient.Add(out packmanOperationId, LatestXiaomiPackageId);
                    if (code == NativeClient.StatusCode.Error)
                    {
                        Debug.LogError("Add " + LatestXiaomiPackageId + " error, please add it again.");
                        return;
                    }
                    packmanOperationType = PackmanOperationType.Add;
                    System.Console.WriteLine("Add: OperationID " + packmanOperationId + " for " + LatestXiaomiPackageId);
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
                            NativeClient.StatusCode code = NativeClient.Add(out packmanOperationId, LatestXiaomiPackageId);
                            if (code == NativeClient.StatusCode.Error)
                            {
                                Debug.LogError("Update " + LatestXiaomiPackageId + " error, please update it again.");
                                return;
                            }
                            packmanOperationType = PackmanOperationType.Add;
                            System.Console.WriteLine("Update: OperationID " + packmanOperationId + " for " + LatestXiaomiPackageId);
                            packmanOperationRunning = true;
                        }
                    }
                }
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    if (packmanOperationRunning)
                        return;

                    NativeClient.StatusCode code = NativeClient.Remove(out packmanOperationId, CurrentXiaomiPackageId);
                    if (code == NativeClient.StatusCode.Error)
                    {
                        Debug.LogError("Remove " + CurrentXiaomiPackageId + " error, please remove it again.");
                        return;
                    }
                    packmanOperationType = PackmanOperationType.Remove;
                    System.Console.WriteLine("Remove: OperationID " + packmanOperationId + " for " + CurrentXiaomiPackageId);
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

            NativeClient.StatusCode getCurrentVersionOperationStatus;
            if (getCurrentVersionOperationId < 0)
                getCurrentVersionOperationStatus = NativeClient.List(out getCurrentVersionOperationId);
            else
                getCurrentVersionOperationStatus = NativeClient.GetOperationStatus(getCurrentVersionOperationId);

            // Reset and return false if it fails with StatusCode.Error or Status.NotFound.
            if (getCurrentVersionOperationStatus > NativeClient.StatusCode.Done)
            {
                getCurrentVersionOperationId = -1;
                return false;
            }

            NativeClient.StatusCode getLatestVersionOperationStatus;
            if (getLatestVersionOperationId < 0)
                getLatestVersionOperationStatus = NativeClient.Search(out getLatestVersionOperationId, xiaomiPackageName);
            else
                getLatestVersionOperationStatus = NativeClient.GetOperationStatus(getLatestVersionOperationId);

            // Reset and return false if it fails with StatusCode.Error or Status.NotFound.
            if (getLatestVersionOperationStatus > NativeClient.StatusCode.Done)
            {
                getLatestVersionOperationId = -1;
                return false;
            }

            // Get version info if both operations are done.
            if (getCurrentVersionOperationStatus == NativeClient.StatusCode.Done && getLatestVersionOperationStatus == NativeClient.StatusCode.Done)
            {
                CheckPackmanOperation(getCurrentVersionOperationId, PackmanOperationType.List);
                CheckPackmanOperation(getLatestVersionOperationId, PackmanOperationType.Search);

                System.Console.WriteLine("Current xiaomi package version is " + (string.IsNullOrEmpty(currentXiaomiPackageVersion) ? "empty" : currentXiaomiPackageVersion));
                System.Console.WriteLine("Latest xiaomi package version is " + (string.IsNullOrEmpty(latestXiaomiPackageVersion) ? "empty" : latestXiaomiPackageVersion));

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
            NativeClient.StatusCode statusCode = NativeClient.GetOperationStatus(operationId);
            if (statusCode == NativeClient.StatusCode.NotFound)
            {
                Debug.LogError("OperationID " + operationId + " Not Found");
                return true;
            }
            else if (statusCode == NativeClient.StatusCode.Error)
            {
                Error error = NativeClient.GetOperationError(operationId);
                Debug.LogError("OperationID " + operationId + " failed with Error: " + error);
                return true;
            }
            else if (statusCode == NativeClient.StatusCode.InProgress || statusCode == NativeClient.StatusCode.InQueue)
            {
                return false;
            }
            else if (statusCode == NativeClient.StatusCode.Done)
            {
                System.Console.WriteLine("OperationID " + operationId + " Done");
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
                        System.Console.WriteLine("Type " + operationType + " Not Supported");
                        break;
                }
                return true;
            }

            return true;
        }

        void ExtractCurrentXiaomiPackageInfo(long operationId)
        {
            // Get the package list to find if xiaomi package has been installed.
            OperationStatus operationStatus = NativeClient.GetListOperationData(operationId);
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
            UpmPackageInfo[] packageList = NativeClient.GetSearchOperationData(operationId);
            foreach (var package in packageList)
            {
                latestXiaomiPackageVersion = package.version;
            }
        }

        void DeleteUnityChannelObjectsIfInProject()
        {
            foreach (string g in guidsToDelete)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(g);
                if (string.IsNullOrEmpty(assetPath))
                    continue;
                AssetDatabase.DeleteAsset(assetPath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
