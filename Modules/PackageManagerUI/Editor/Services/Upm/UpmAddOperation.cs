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

        // the special unique id is used when neither package unique id or version unique id applies
        // e.g. git url, tar ball path that does not contain any package name or version
        public virtual string specialUniqueId => m_SpecialUniqueId;

        [SerializeField]
        private PackageTag m_PackageTag = PackageTag.None;
        public virtual PackageTag packageTag => m_PackageTag;

        public override RefreshOptions refreshOptions => RefreshOptions.None;

        public void Add(string packageId, string packageUniqueId = null)
        {
            m_PackageId = packageId;
            m_PackageName = string.Empty;
            m_SpecialUniqueId = string.Empty;
            m_PackageUniqueId = packageUniqueId ?? packageName;
            m_PackageTag = PackageTag.None;
            Start();
        }

        public void AddByUrlOrPath(string urlOrPath, PackageTag tag)
        {
            m_SpecialUniqueId = urlOrPath;
            m_PackageId = string.Empty;
            m_PackageName = string.Empty;
            m_PackageUniqueId = string.Empty;
            m_PackageTag = tag;
            Start();
        }

        protected override AddRequest CreateRequest()
        {
            var uniqueId = string.IsNullOrEmpty(specialUniqueId) ? packageId : specialUniqueId;
            return Client.Add(uniqueId);
        }
    }
}
