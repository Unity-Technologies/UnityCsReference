// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine.Bindings;

namespace UnityEditor.Connect
{
    [NativeHeader("Editor/Src/CloudServicesSettings/PackageUtils/PackageUtils.h")]
    internal class PackageUtils
    {
        private static readonly PackageUtils s_Instance;

        private bool m_outdatedOperationRunning = false;
        private long m_outdatedOperationId = 0;
        private bool m_listOperationRunning = false;
        private long m_listOperationId = 0;
        private Dictionary<string, UpmPackageInfo> m_currentPackages = new Dictionary<string, UpmPackageInfo>();
        private Dictionary<string, UpmPackageInfo> m_outdatedPackages = new Dictionary<string, UpmPackageInfo>();

        extern private static bool WaitForPackageManagerOperation(long operationId, string progressBarText);

        public static PackageUtils instance
        {
            get
            {
                return s_Instance;
            }
        }

        static PackageUtils()
        {
            s_Instance = new PackageUtils();
        }

        public void RetrievePackageInfo()
        {
            if (Client.List(out m_listOperationId) == StatusCode.Error)
            {
                Debug.LogWarning("Failed to call list packages!");
                return;
            }
            m_listOperationRunning = true;

            if (Client.Outdated(out m_outdatedOperationId) == StatusCode.Error)
            {
                Debug.LogWarning("Failed to call outdated package!");
                return;
            }
            m_outdatedOperationRunning = true;
        }

        public string GetCurrentVersion(string packageName)
        {
            CheckRunningOperations();
            if (m_currentPackages.ContainsKey(packageName))
            {
                return m_currentPackages[packageName].version;
            }
            return string.Empty;
        }

        public string GetLatestVersion(string packageName)
        {
            CheckRunningOperations();
            if (m_outdatedPackages.ContainsKey(packageName))
            {
                return m_outdatedPackages[packageName].version;
            }
            return GetCurrentVersion(packageName);
        }

        public bool UpdateLatest(string packageName)
        {
            if (!m_outdatedPackages.ContainsKey(packageName))
            {
                return false;
            }

            long addOperationId = 0;
            if (Client.Add(out addOperationId, m_outdatedPackages[packageName].packageId) == StatusCode.Error)
            {
                Debug.LogWarningFormat("Failed to update outdated package {0}!", packageName);
                return false;
            }

            if (WaitForPackageManagerOperation(addOperationId, string.Format("Updating Package {0} to version {1}", packageName, m_outdatedPackages[packageName].version)))
            {
                UpmPackageInfo packageInfo = Client.GetAddOperationData(addOperationId);
                m_currentPackages[GetPackageRootName(packageInfo)] = packageInfo;
                return true;
            }
            return false;
        }

        private void CheckRunningOperations()
        {
            if (m_outdatedOperationRunning)
            {
                StatusCode status = Client.GetOperationStatus(m_outdatedOperationId);
                switch (status)
                {
                    case StatusCode.Error:
                    case StatusCode.NotFound:
                        m_outdatedOperationRunning = false;
                        Debug.LogWarning("Failed to retrieve outdated package list!");
                        break;
                    case StatusCode.Done:
                    {
                        m_outdatedPackages.Clear();
                        Dictionary<string, OutdatedPackage> outdatedData = Client.GetOutdatedOperationData(m_outdatedOperationId);
                        foreach (string key in outdatedData.Keys)
                        {
                            m_outdatedPackages[key] = outdatedData[key].latest;
                        }
                        m_outdatedOperationRunning = false;
                    }
                    break;
                    case StatusCode.InProgress:
                    case StatusCode.InQueue:
                    default:
                        break;
                }
            }

            if (m_listOperationRunning)
            {
                StatusCode status = Client.GetOperationStatus(m_listOperationId);
                switch (status)
                {
                    case StatusCode.Error:
                    case StatusCode.NotFound:
                        m_listOperationRunning = false;
                        Debug.LogWarning("Failed to retrieve package list!");
                        break;
                    case StatusCode.Done:
                    {
                        m_currentPackages.Clear();
                        OperationStatus listData = Client.GetListOperationData(m_listOperationId);
                        for (int i = 0; i < listData.packageList.Length; ++i)
                        {
                            m_currentPackages[GetPackageRootName(listData.packageList[i])] = listData.packageList[i];
                        }
                        m_listOperationRunning = false;
                    }
                    break;
                    case StatusCode.InProgress:
                    case StatusCode.InQueue:
                    default:
                        break;
                }
            }
        }

        private string GetPackageRootName(UpmPackageInfo packageInfo)
        {
            return packageInfo.packageId.Substring(0, packageInfo.packageId.Length - packageInfo.version.Length - 1);
        }
    }
}
