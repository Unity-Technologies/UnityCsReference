// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmListOperation : UpmBaseOperation<ListRequest>
    {
        public override RefreshOptions refreshOptions => isOfflineMode ? RefreshOptions.UpmListOffline : RefreshOptions.UpmList;

        protected override string operationErrorMessage => isOfflineMode ? L10n.Tr("Error fetching package list offline.") : L10n.Tr("Error fetching package list.");

        public void List()
        {
            m_OfflineMode = false;
            Start();
        }

        public void ListOffline(long offlineDataTimestamp)
        {
            m_OfflineMode = true;
            m_OfflineDataTimestamp = offlineDataTimestamp;
            Start();
        }

        protected override ListRequest CreateRequest()
        {
            return m_ClientProxy.List(isOfflineMode, true);
        }
    }
}
