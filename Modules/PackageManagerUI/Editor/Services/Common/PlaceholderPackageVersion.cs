// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PlaceholderPackageVersion : BasePackageVersion
    {
        [SerializeField]
        private string m_UniqueId;
        public override string uniqueId => m_UniqueId;

        public override string packageId => string.Empty;

        [SerializeField]
        private UIError[] m_Errors;
        public override IEnumerable<UIError> errors => m_Errors;

        public override string author => string.Empty;

        public override string category => string.Empty;

        public override bool isInstalled => false;

        public override bool isFullyFetched => true;

        public override bool isAvailableOnDisk => false;

        public override bool isDirectDependency => true;

        public override string localPath => string.Empty;

        public override string versionString => m_VersionString;

        public override string versionId => string.Empty;

        public PlaceholderPackageVersion(string uniqueId, string displayName, string versionString = "", PackageTag tag = PackageTag.None, UIError error = null)
        {
            m_UniqueId = uniqueId;
            m_DisplayName = displayName;
            m_VersionString = versionString;
            m_Tag = tag | PackageTag.Placeholder;
            m_Version = new SemVersion(0);
            m_Errors = error != null ? new UIError[] { error } : new UIError[0];

            SemVersionParser.TryParse(m_VersionString, out m_Version);
        }
    }
}
