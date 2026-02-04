// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmAddAndRemoveOperation : UpmBaseOperation<AddAndRemoveRequest>
    {
        [Serializable]
        internal class UpmAddAndRemoveDryRun : UpmBaseOperation<AddAndRemoveRequest>
        {
            public override RefreshOptions refreshOptions => RefreshOptions.None;

            [SerializeField]
            protected string[] m_PackageIdsToAdd = Array.Empty<string>();

            [SerializeField]
            protected string[] m_PackagesNamesToRemove = Array.Empty<string>();

            protected override AddAndRemoveRequest CreateRequest()
            {
                return m_ClientProxy.AddAndRemove(m_PackageIdsToAdd, m_PackagesNamesToRemove, true);
            }

            public void StartDryRun(string[] packageIdsToAdd, string[] packagesNamesToRemove)
            {
                m_PackageIdsToAdd = packageIdsToAdd;
                m_PackagesNamesToRemove = packagesNamesToRemove;
                Start();
            }
        }

        private Func<PackageCollection, bool> m_ShouldProceedAfterDryRun;
        public void SetDryRunFunction(Func<PackageCollection, bool> shouldProceedAfterDryRun)
        {
            m_ShouldProceedAfterDryRun = shouldProceedAfterDryRun;
        }

        public override RefreshOptions refreshOptions => RefreshOptions.None;

        protected override string operationErrorMessage => string.Format(L10n.Tr("Error adding/removing packages: {0}."), string.Join(",", packageIdsToAdd.Join(packagesNamesToRemove)));

        [SerializeField]
        protected string[] m_PackageIdsToReset = Array.Empty<string>();
        public IReadOnlyCollection<string> packageIdsToReset => m_PackageIdsToReset;

        [SerializeField]
        protected string[] m_PackageIdsToAdd = Array.Empty<string>();
        public IReadOnlyCollection<string> packageIdsToAdd => m_PackageIdsToAdd;

        [SerializeField]
        protected string[] m_PackagesNamesToRemove = Array.Empty<string>();
        public IReadOnlyCollection<string> packagesNamesToRemove => m_PackagesNamesToRemove;

        [SerializeField]
        private UpmAddAndRemoveDryRun m_DryRun = new ();

        [SerializeField]
        private string m_SpecialUniqueId = string.Empty;
        public bool isSpecialInstall => !string.IsNullOrEmpty(m_SpecialUniqueId);

        public override string packageIdOrName => string.IsNullOrEmpty(m_SpecialUniqueId) ? base.packageIdOrName : m_SpecialUniqueId;
        public override string packageName => string.IsNullOrEmpty(m_SpecialUniqueId) ? base.packageName : m_SpecialUniqueId;
        public override bool isInProgress => base.isInProgress || m_DryRun?.isInProgress == true;

        public PackageInfo FindMainPackageInfoFromResult()
        {
            var result = m_Request?.Result;
            if (result == null)
                return null;

            // Since in the "Add package by git url" UI, we don't restrict people to only install git packages we need to handle different special ids such as
            // `com.unity.a`, `com.unity.a@1`, `com.unity.a@1.0.0`, `file:/path/to/package` or `git@git.path.to.package.git`
            if (isSpecialInstall)
            {
                var extractedPackageName = m_SpecialUniqueId.Split(new[] { '@' }, 2)[0];
                return result.FirstMatch(p =>
                    p.packageId == m_SpecialUniqueId
                    || p.name == extractedPackageName
                    || p.projectDependenciesEntry == m_SpecialUniqueId);
            }

            var nameToMatch = packageName;
            return string.IsNullOrEmpty(nameToMatch) ? null : result.FirstMatch(p => p.name == nameToMatch);
        }

        public void AddByPathOrUrl(string pathOrUrl)
        {
            m_PackageIdsToReset = Array.Empty<string>();
            m_PackageIdsToAdd = new [] {pathOrUrl};
            m_PackagesNamesToRemove = Array.Empty<string>();
            m_PackageIdOrName = pathOrUrl;
            m_SpecialUniqueId = pathOrUrl;
            Start();
        }

        public void AddById(string versionId, bool isUnlistedPackage)
        {
            m_PackageIdsToReset = Array.Empty<string>();
            m_PackageIdsToAdd = new  [] {versionId};
            m_PackagesNamesToRemove = Array.Empty<string>();
            m_PackageIdOrName = versionId;
            // Unlisted packages potentially can't be found in the package database so we treat it the same way as if it's from git/url/path
            m_SpecialUniqueId = isUnlistedPackage ? versionId : string.Empty;
            Start();
        }

        public void AddByIds(string[] versionIds)
        {
            m_PackageIdsToReset = Array.Empty<string>();
            m_PackageIdsToAdd = versionIds ?? Array.Empty<string>();
            m_PackagesNamesToRemove = Array.Empty<string>();
            m_PackageIdOrName = string.Empty;
            m_SpecialUniqueId = string.Empty;
            Start();
        }

        public void RemoveByNames(string[] packagesNames)
        {
            m_PackageIdsToReset = Array.Empty<string>();
            m_PackageIdsToAdd = Array.Empty<string>();
            m_PackagesNamesToRemove = packagesNames ?? Array.Empty<string>();
            m_PackageIdOrName = string.Empty;
            m_SpecialUniqueId = string.Empty;
            Start();
        }

        public void AddAndResetDependencies(string packageId, string[] dependencyPackagesNames)
        {
            m_PackageIdsToReset = Array.Empty<string>();
            m_PackageIdsToAdd = new [] { packageId };
            m_PackagesNamesToRemove = dependencyPackagesNames ?? Array.Empty<string>();
            m_PackageIdOrName = packageId;
            m_SpecialUniqueId = string.Empty;
            Start();
        }

        public void ResetDependencies(string packageId, string[] dependencyPackagesNames)
        {
            m_PackageIdsToReset = new [] { packageId };
            m_PackageIdsToAdd = Array.Empty<string>();
            m_PackagesNamesToRemove = dependencyPackagesNames ?? Array.Empty<string>();
            m_PackageIdOrName = packageId;
            m_SpecialUniqueId = string.Empty;
            Start();
        }

        protected new void Start()
        {
            m_DryRun ??= new UpmAddAndRemoveDryRun();
            m_DryRun.ResolveDependencies(m_ClientProxy, m_Application);
            m_DryRun.onProcessResult += request =>
            {
                if (m_ShouldProceedAfterDryRun?.Invoke(request.Result) == true)
                    base.Start();
                else
                {
                    Cancel();
                    // The Resolve() call below is used to clean up the project cache after an add/remove operation is cancelled by the user.
                    // Since core unpacks the package in the project cache during the dry run, we need to resolve the cache to ensure consistency.
                    m_ClientProxy.Resolve();
                }
            };
            m_DryRun.onOperationError += (_, error) =>
            {
                OnError(error);
                OnFinalize();
            };
            m_DryRun.StartDryRun(m_PackageIdsToAdd, m_PackagesNamesToRemove);
        }

        public new void RestoreProgress()
        {
            if (m_DryRun?.isInProgress == true)
            {
                m_DryRun.Cancel();
                Start();
            }
            else
                base.RestoreProgress();
        }

        protected override AddAndRemoveRequest CreateRequest()
        {
            return m_ClientProxy.AddAndRemove(m_PackageIdsToAdd, m_PackagesNamesToRemove, dryRun: false);
        }
    }
}
