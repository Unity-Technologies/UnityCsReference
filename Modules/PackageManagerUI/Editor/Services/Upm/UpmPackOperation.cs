// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmPackOperation : UpmBaseOperation<PackRequest>
    {
        [SerializeField]
        private string m_PackageFolder;

        [SerializeField]
        private string m_ExportPath;

        [SerializeField]
        private string m_OrgId;

        public override RefreshOptions refreshOptions => RefreshOptions.None;

        protected override string operationErrorMessage => string.Format(L10n.Tr("Error exporting package: {0}."), m_PackageFolder);

        public void Pack(string packageName, string packageFolder, string exportPath, string orgId)
        {
            m_PackageIdOrName = packageName;
            m_PackageFolder = packageFolder;
            m_ExportPath = exportPath;
            m_OrgId = orgId;
            Start();
        }

        protected override PackRequest CreateRequest()
        {
            return m_ClientProxy.Pack(m_PackageFolder, m_ExportPath, m_OrgId);
        }
    }
}
