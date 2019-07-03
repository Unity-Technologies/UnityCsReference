// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmListOperation : UpmBaseOperation<ListRequest>
    {
        public void List()
        {
            m_OfflineMode = false;
            Start();
        }

        public void ListOffline(long timestamp)
        {
            m_OfflineMode = true;
            m_Timestamp = timestamp;
            Start();
        }

        protected override ListRequest CreateRequest()
        {
            return Client.List(isOfflineMode, true);
        }

        public void Cancel()
        {
            CancelInternal();
        }
    }
}
