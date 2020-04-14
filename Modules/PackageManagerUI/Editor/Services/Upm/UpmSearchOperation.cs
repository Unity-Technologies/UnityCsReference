// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmSearchOperation : UpmBaseOperation<SearchRequest>
    {
        public override RefreshOptions refreshOptions => isOfflineMode ? RefreshOptions.UpmSearchOffline : RefreshOptions.UpmSearch;

        [SerializeField]
        private string m_PackageNameOrId;

        private void SetPackageNameOrId(string packageNameOrId)
        {
            m_PackageNameOrId = packageNameOrId;
            if (string.IsNullOrEmpty(packageNameOrId))
            {
                m_PackageId = string.Empty;
                m_PackageName = string.Empty;
            }
            else
            {
                var tokens = packageNameOrId.Split(new[] { '@' }, 2);
                m_PackageName = tokens[0];
                m_PackageId = tokens.Length > 1 ? packageNameOrId : string.Empty;
            }
        }

        public void SearchAll()
        {
            m_OfflineMode = false;
            SetPackageNameOrId(string.Empty);
            Start();
        }

        public void SearchAllOffline(long timestamp)
        {
            m_OfflineMode = true;
            m_Timestamp = timestamp;
            SetPackageNameOrId(string.Empty);
            Start();
        }

        public void Search(string packageNameOrId, string productId = null)
        {
            m_OfflineMode = false;
            SetPackageNameOrId(packageNameOrId);
            m_PackageUniqueId = productId ?? packageName;
            Start();
        }

        public void SearchOffline(string packageNameOrId, long timestamp, string productId = null)
        {
            m_OfflineMode = true;
            m_Timestamp = timestamp;
            SetPackageNameOrId(packageNameOrId);
            m_PackageUniqueId = productId ?? packageName;
            Start();
        }

        protected override SearchRequest CreateRequest()
        {
            if (string.IsNullOrEmpty(m_PackageNameOrId))
                return Client.SearchAll(isOfflineMode);
            else
                return Client.Search(m_PackageNameOrId, isOfflineMode);
        }

        public void Cancel()
        {
            CancelInternal();
        }
    }
}
