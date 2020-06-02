// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PlaceholderPackageVersion : BasePackageVersion
    {
        [SerializeField]
        private string m_UniqueId;
        public override string uniqueId => m_UniqueId;

        [SerializeField]
        private UIError[] m_Errors;
        public override IEnumerable<UIError> errors => m_Errors;

        public override string author => string.Empty;

        public override string authorLink => string.Empty;

        public override string category => string.Empty;

        public override bool isInstalled => false;

        public override bool isFullyFetched => true;

        public override bool isAvailableOnDisk => false;

        public override bool isDirectDependency => true;

        public override string localPath => string.Empty;

        public override string versionString => string.Empty;

        public override string versionId => string.Empty;

        public PlaceholderPackageVersion(string packageUniqueId, string uniqueId, string displayName, PackageTag tag = PackageTag.None, UIError error = null)
        {
            m_PackageUniqueId = packageUniqueId;
            m_UniqueId = uniqueId;
            m_DisplayName = displayName;
            m_Tag = tag;
            m_Version = new SemVersion(0);
            m_Errors = error != null ? new UIError[] { error } : new UIError[0];
        }
    }
}
