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

        [SerializeField]
        protected string[] m_PackageIdsToReset = new string[0];
        public IEnumerable<string> packageIdsToReset => m_PackageIdsToReset;

        [SerializeField]
        protected string[] m_PackageIdsToAdd = new string[0];
        public IEnumerable<string> packageIdsToAdd => m_PackageIdsToAdd;

        [SerializeField]
        protected string[] m_PackagesNamesToRemove = new string[0];
        public IEnumerable<string> packagesNamesToRemove => m_PackagesNamesToRemove;

        public void AddByIds(IEnumerable<string> versionIds)
        {
            m_PackageIdsToReset = new string[0];
            m_PackageIdsToAdd = (versionIds ?? Enumerable.Empty<string>()).ToArray();
            m_PackagesNamesToRemove = new string[0];
            SetPrimaryPackageNameOrId();
            Start();
        }

        public void RemoveByNames(IEnumerable<string> packagesNames)
        {
            m_PackageIdsToReset = new string[0];
            m_PackageIdsToAdd = new string[0];
            m_PackagesNamesToRemove = (packagesNames ?? Enumerable.Empty<string>()).ToArray();
            SetPrimaryPackageNameOrId();
            Start();
        }

        public void AddAndResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames)
        {
            m_PackageIdsToReset = new string[0];
            m_PackageIdsToAdd = new string[1] { packageId };
            m_PackagesNamesToRemove = (dependencyPackagesNames ?? Enumerable.Empty<string>()).ToArray();
            SetPrimaryPackageNameOrId(packageId: packageId);
            Start();
        }

        public void ResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames)
        {
            m_PackageIdsToReset = new string[1] { packageId };
            m_PackageIdsToAdd = new string[0];
            m_PackagesNamesToRemove = (dependencyPackagesNames ?? Enumerable.Empty<string>()).ToArray();
            SetPrimaryPackageNameOrId(packageId: packageId);
            Start();
        }

        private void SetPrimaryPackageNameOrId(string packageId = null, string packageName = null, string packageUniqueId = null)
        {
            m_PackageId = packageId ?? string.Empty;
            m_PackageName = packageName ?? string.Empty;
            m_PackageUniqueId = packageUniqueId ?? this.packageName;
        }

        protected override AddAndRemoveRequest CreateRequest()
        {
            return m_ClientProxy.AddAndRemove(m_PackageIdsToAdd, m_PackagesNamesToRemove);
        }
    }
}
