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
        [Serializable]
        internal class UpmAddAndRemoveDryRun : UpmBaseOperation<AddAndRemoveRequest>
        {
            public override RefreshOptions refreshOptions => RefreshOptions.None;

            public IEnumerable<PackageInfo> dryRunResult;
            private string[] packageIdsToAdd { get; set; }
            private string[] packagesNamesToRemove { get; set; }

            protected override AddAndRemoveRequest CreateRequest()
            {
                return m_ClientProxy.AddAndRemove(packageIdsToAdd, packagesNamesToRemove, true);
            }

            public void StartDryRun(string[] packageIdsToAdd, string[] packagesNamesToRemove)
            {
                this.packageIdsToAdd = packageIdsToAdd;
                this.packagesNamesToRemove = packagesNamesToRemove;
                Start();
            }
        }

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
        private UpmAddAndRemoveDryRun m_DryRun = new ();

        [SerializeField]
        public bool isDryRunInProgress = false;

        [SerializeField]
        private string m_SpecialUniqueId = string.Empty;
        public bool isSpecialInstall => !string.IsNullOrEmpty(m_SpecialUniqueId);

        public override string packageIdOrName => string.IsNullOrEmpty(m_SpecialUniqueId) ? base.packageIdOrName : m_SpecialUniqueId;
        public override string packageName => string.IsNullOrEmpty(m_SpecialUniqueId) ? base.packageName : m_SpecialUniqueId;
        public override bool isInProgress => (m_Request != null && m_Request.Id != 0 && !m_IsCompleted) || isDryRunInProgress;

        public PackageInfo FindMainPackageInfoFromResult()
        {
            var result = m_Request?.Result ?? m_DryRun?.dryRunResult;
            if (result == null)
                return null;

            // Since in the "Add package by git url" UI, we don't restrict people to only install git packages we need to handle different special ids such as
            // `com.unity.a`, `com.unity.a@1`, `com.unity.a@1.0.0`, `file:/path/to/package` or `git@git.path.to.package.git`
            if (isSpecialInstall)
            {
                var extractedPackageName = m_SpecialUniqueId.Split(new[] { '@' }, 2)[0];
                return result.FirstOrDefault(p =>
                    p.packageId == m_SpecialUniqueId
                    || p.name == extractedPackageName
                    || p.projectDependenciesEntry == m_SpecialUniqueId);
            }

            var nameToMatch = packageName;
            return string.IsNullOrEmpty(nameToMatch) ? null : result.FirstOrDefault(p => p.name == nameToMatch);
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

        public void AddById(string versionId)
        {
            m_PackageIdsToReset = new string[0];
            m_PackageIdsToAdd = new  [] {versionId};
            m_PackagesNamesToRemove = new string[0];
            m_PackageIdOrName = versionId;
            m_SpecialUniqueId = string.Empty;
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

        protected new void Start()
        {
            m_DryRun = new UpmAddAndRemoveDryRun();
            m_DryRun.ResolveDependencies(m_ClientProxy, m_Application);
            m_DryRun.onProcessResult += HandleDryRunProcessResult;
            m_DryRun.onOperationError += (_, error) =>
            {
                isDryRunInProgress = false;
                OnError(error);
                Cancel();
            };
            m_DryRun.onOperationFinalized += _ =>
            {
                isDryRunInProgress = false;
            };
            m_DryRun.StartDryRun(m_PackageIdsToAdd, m_PackagesNamesToRemove);
            isDryRunInProgress = true;
        }

        private void HandleDryRunProcessResult(AddAndRemoveRequest request)
        {
            isDryRunInProgress = false;
            m_DryRun.dryRunResult = request?.Result;
            var upmCache = ServicesContainer.instance.Resolve<IUpmCache>();
            var newPackageInfos = upmCache.PreviewIncomingTrustIssuePackageInfos(request.Result);

            var trustIssuePackages = new List<PackageInfo>();
            foreach (var info in newPackageInfos)
            {
                if (info?.trustLevel == TrustLevel.FullTrust || info == null)
                    continue;

                // Some Unity packages with legacy signatures sometimes return both Error and Untrusted, in that case we don't want to show the dialog
                if (info?.signature.status == SignatureStatus.Error && info?.trustLevel == TrustLevel.Untrusted)
                    continue;

                if (info?.trustLevel == TrustLevel.LimitedTrust
                         || info?.trustLevel == TrustLevel.Untrusted
                         || info?.signature.status == SignatureStatus.Invalid
                         || info?.signature.status == SignatureStatus.Unsigned)
                {
                    trustIssuePackages.Add(info);
                }
            }

            if (trustIssuePackages.Count == 0)
            {
                base.Start();
                return;
            }

            var invalidSignaturePackages = trustIssuePackages.Where(p => p.signature.status == SignatureStatus.Invalid).ToArray();
            var missingSignaturePackages = trustIssuePackages.Where(p => p.signature.status == SignatureStatus.Unsigned).ToArray();
            var limitedTrustPackages = trustIssuePackages.Where(p => p.signature.status == SignatureStatus.Valid && p.trustLevel == TrustLevel.LimitedTrust).ToArray();
            if (ActiveTrustWindow.ShowActiveTrustWindow(invalidSignaturePackages, missingSignaturePackages, limitedTrustPackages) == ActiveTrustReturnValue.InstallAnyway)
                base.Start();
            else
                Cancel();
        }

        public new void RestoreProgress()
        {
            if (isDryRunInProgress)
            {
                m_DryRun.Cancel();
                isDryRunInProgress = false;
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
