// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageRemoveImportedButton : PackageToolBarRegularButton
    {
        private ApplicationProxy m_ApplicationProxy;
        private PackageOperationDispatcher m_OperationDispatcher;
        public PackageRemoveImportedButton(ApplicationProxy applicationProxy, PackageOperationDispatcher operationDispatcher)
        {
            m_ApplicationProxy = applicationProxy;
            m_OperationDispatcher = operationDispatcher;
        }

        protected override bool TriggerAction(IList<IPackageVersion> versions)
        {
            if (!m_ApplicationProxy.DisplayDialog("removeMultiImported", L10n.Tr("Removing imported packages"),
                    L10n.Tr("Remove all assets from these packages?\nAny changes you made to the assets will be lost."),
                    L10n.Tr("Remove"), L10n.Tr("Cancel")))
                return false;

            m_OperationDispatcher.RemoveImportedAssets(versions);
            PackageManagerWindowAnalytics.SendEvent("removeImported", packageIds: versions.Select(v => v.package.uniqueId));
            return true;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_OperationDispatcher.RemoveImportedAssets(version.package);
            PackageManagerWindowAnalytics.SendEvent("removeImported", version.uniqueId);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            return version?.importedAssets?.Any() == true;
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return k_InProgressGenericTooltip;
            return string.Format(L10n.Tr("Remove this {0}'s imported assets from your project."), version.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Remove");
        }

        protected override bool IsInProgress(IPackageVersion version) => false;
    }
}
