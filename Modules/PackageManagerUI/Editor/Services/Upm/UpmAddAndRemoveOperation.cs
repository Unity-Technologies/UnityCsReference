// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmAddAndRemoveOperation : UpmBaseOperation<AddAndRemoveRequest>
    {
        public override RefreshOptions refreshOptions => RefreshOptions.None;

        protected override string operationErrorMessage
        {
            get
            {
                var packageIds = packageIdsToAdd.Concat(packagesNamesToRemove);
                return string.Format(L10n.Tr("Error adding/removing packages: {0}."), string.Join(",", packageIds.ToArray()));
            }
        }

        [SerializeField]
        protected string[] m_PackageIdsToReset = new string[0];
        public IReadOnlyCollection<string> packageIdsToReset => m_PackageIdsToReset;

        [SerializeField]
        protected string[] m_PackageIdsToAdd = new string[0];
        public IReadOnlyCollection<string> packageIdsToAdd => m_PackageIdsToAdd;

        [SerializeField]
        protected string[] m_PackagesNamesToRemove = new string[0];
        public IReadOnlyCollection<string> packagesNamesToRemove => m_PackagesNamesToRemove;

        [SerializeField]
        private string m_SpecialUniqueId = string.Empty;
        public bool isSpecialInstall => !string.IsNullOrEmpty(m_SpecialUniqueId);

        public override string packageIdOrName => string.IsNullOrEmpty(m_SpecialUniqueId) ? base.packageIdOrName : m_SpecialUniqueId;
        public override string packageName => string.IsNullOrEmpty(m_SpecialUniqueId) ? base.packageName : m_SpecialUniqueId;

        public PackageInfo FindMainPackageInfoFromResult()
        {
            // Since in the "Add package by git url" UI, we don't restrict people to only install git packages we need to handle different special ids such as
            // `com.unity.a`, `com.unity.a@1`, `com.unity.a@1.0.0`, `file:/path/to/package` or `git@git.path.to.package.git`
            if (isSpecialInstall)
            {
                var extractedPackageName = m_SpecialUniqueId.Split(new[] { '@' }, 2)[0];
                return m_Request.Result.FirstOrDefault(p => p.packageId == m_SpecialUniqueId || p.name == extractedPackageName || p.projectDependenciesEntry == m_SpecialUniqueId);
            }
            var nameToMatch = packageName;
            return string.IsNullOrEmpty(nameToMatch) ? null : m_Request.Result.FirstOrDefault(p => p.name == nameToMatch);
        }

        public void AddByPathOrUrl(string pathOrUrl)
        {
            m_PackageIdsToReset = new string[0];
            m_PackageIdsToAdd = new [] {pathOrUrl};
            m_PackagesNamesToRemove = new string[0];
            m_PackageIdOrName = pathOrUrl;
            m_SpecialUniqueId = pathOrUrl;
            Start();
        }

        public void AddById(string versionId, bool isUnlistedPackage)
        {
            m_PackageIdsToReset = new string[0];
            m_PackageIdsToAdd = new  [] {versionId};
            m_PackagesNamesToRemove = new string[0];
            m_PackageIdOrName = versionId;
            // Unlisted packages potentially can't be found in the package database so we treat it the same way as if it's from git/url/path
            m_SpecialUniqueId = isUnlistedPackage ? versionId : string.Empty;
            Start();
        }

        public void AddByIds(IEnumerable<string> versionIds)
        {
            m_PackageIdsToReset = new string[0];
            m_PackageIdsToAdd = (versionIds ?? Enumerable.Empty<string>()).ToArray();
            m_PackagesNamesToRemove = new string[0];
            m_PackageIdOrName = string.Empty;
            m_SpecialUniqueId = string.Empty;
            Start();
        }

        public void RemoveByNames(IEnumerable<string> packagesNames)
        {
            m_PackageIdsToReset = new string[0];
            m_PackageIdsToAdd = new string[0];
            m_PackagesNamesToRemove = (packagesNames ?? Enumerable.Empty<string>()).ToArray();
            m_PackageIdOrName = string.Empty;
            m_SpecialUniqueId = string.Empty;
            Start();
        }

        public void AddAndResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames)
        {
            m_PackageIdsToReset = new string[0];
            m_PackageIdsToAdd = new [] { packageId };
            m_PackagesNamesToRemove = (dependencyPackagesNames ?? Enumerable.Empty<string>()).ToArray();
            m_PackageIdOrName = packageId;
            m_SpecialUniqueId = string.Empty;
            Start();
        }

        public void ResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames)
        {
            m_PackageIdsToReset = new [] { packageId };
            m_PackageIdsToAdd = new string[0];
            m_PackagesNamesToRemove = (dependencyPackagesNames ?? Enumerable.Empty<string>()).ToArray();
            m_PackageIdOrName = packageId;
            m_SpecialUniqueId = string.Empty;
            Start();
        }

        protected override AddAndRemoveRequest CreateRequest()
        {
            return m_ClientProxy.AddAndRemove(m_PackageIdsToAdd, m_PackagesNamesToRemove);
        }
    }
}
