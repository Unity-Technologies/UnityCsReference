// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmRemoveOperation : UpmBaseOperation<RemoveRequest>
    {
        public override RefreshOptions refreshOptions => RefreshOptions.None;

        public void Remove(string packageName, string packageUniqueId = null)
        {
            m_PackageName = packageName;
            m_PackageUniqueId = packageUniqueId ?? packageName;
            Start();
        }

        protected override RemoveRequest CreateRequest()
        {
            return Client.Remove(packageName);
        }
    }
}
