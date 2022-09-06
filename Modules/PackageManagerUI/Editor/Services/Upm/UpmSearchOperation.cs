// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmSearchOperation : UpmBaseOperation<SearchRequest>
    {
        public override RefreshOptions refreshOptions => isOfflineMode ? RefreshOptions.UpmSearchOffline : RefreshOptions.UpmSearch;

        protected override string operationErrorMessage => isOfflineMode ? L10n.Tr("Error searching for packages offline.") : L10n.Tr("Error searching for packages.");

        public void SearchAll()
        {
            m_OfflineMode = false;
            m_PackageIdOrName = string.Empty;
            Start();
        }

        public void SearchAllOffline(long timestamp)
        {
            m_OfflineMode = true;
            m_Timestamp = timestamp;
            m_PackageIdOrName = string.Empty;
            Start();
        }

        public void Search(string packageIdOrName)
        {
            m_OfflineMode = false;
            m_PackageIdOrName = packageIdOrName;
            Start();
        }

        public void SearchOffline(string packageIdOrName, long timestamp)
        {
            m_OfflineMode = true;
            m_Timestamp = timestamp;
            m_PackageIdOrName = packageIdOrName;
            Start();
        }

        protected override SearchRequest CreateRequest()
        {
            if (string.IsNullOrEmpty(m_PackageIdOrName))
                return m_ClientProxy.SearchAll(isOfflineMode);
            else
                return m_ClientProxy.Search(m_PackageIdOrName, isOfflineMode);
        }
    }
}
