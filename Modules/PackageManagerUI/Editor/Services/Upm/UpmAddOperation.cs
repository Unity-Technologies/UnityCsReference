// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmAddOperation : UpmBaseOperation<AddRequest>
    {
        [SerializeField]
        private string m_SpecialUniqueId = string.Empty;

        [SerializeField]
        private PackageTag m_PackageTag = PackageTag.None;
        public virtual PackageTag packageTag => m_PackageTag;

        public override RefreshOptions refreshOptions => RefreshOptions.None;

        protected override string operationErrorMessage => string.Format(L10n.Tr("Error adding package: {0}."), packageIdOrName);

        public override string packageIdOrName => string.IsNullOrEmpty(m_SpecialUniqueId) ? base.packageIdOrName : m_SpecialUniqueId;
        public override string packageName => string.IsNullOrEmpty(m_SpecialUniqueId) ? base.packageName : m_SpecialUniqueId;

        public void Add(string packageIdOrName)
        {
            m_PackageIdOrName = packageIdOrName;
            m_SpecialUniqueId = string.Empty;
            m_PackageTag = PackageTag.None;
            Start();
        }

        public void AddByUrlOrPath(string urlOrPath, PackageTag tag)
        {
            m_PackageIdOrName = urlOrPath;
            m_SpecialUniqueId = urlOrPath;
            m_PackageTag = tag;
            Start();
        }

        protected override AddRequest CreateRequest()
        {
            return m_ClientProxy.Add(packageIdOrName);
        }
    }
}
