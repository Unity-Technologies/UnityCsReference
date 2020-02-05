// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmAddOperation : UpmBaseOperation<AddRequest>
    {
        [SerializeField]
        private string m_SpecialUniqueId = string.Empty;

        public override string specialUniqueId { get { return m_SpecialUniqueId; } }

        public override RefreshOptions refreshOptions => RefreshOptions.None;

        public void Add(string packageId, string packageUniqueId = null)
        {
            m_PackageId = packageId;
            m_PackageName = string.Empty;
            m_SpecialUniqueId = string.Empty;
            m_PackageUniqueId = packageUniqueId ?? packageName;
            Start();
        }

        public void AddByUrlOrPath(string urlOrPath)
        {
            m_SpecialUniqueId = urlOrPath;
            m_PackageId = string.Empty;
            m_PackageName = string.Empty;
            m_PackageUniqueId = string.Empty;
            Start();
        }

        protected override AddRequest CreateRequest()
        {
            var uniqueId = string.IsNullOrEmpty(specialUniqueId) ? packageId : specialUniqueId;
            return Client.Add(uniqueId);
        }
    }
}
