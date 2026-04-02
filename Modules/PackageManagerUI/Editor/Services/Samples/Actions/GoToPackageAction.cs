// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class GoToPackageAction: SampleAction
    {
        private readonly IPackageManagerWindowProxy m_PackageManagerWindowProxy;
        public GoToPackageAction(IPackageManagerWindowProxy packageManagerWindowProxy)
        {
            m_PackageManagerWindowProxy = packageManagerWindowProxy;
        }

        public override string GetText(Sample sample, bool isInProgress) => L10n.Tr("Go to Package View");
        public override string GetTooltip(Sample sample, bool isInProgress) => L10n.Tr("Click to view the package this sample belongs to.");

        protected override bool TriggerActionImplementation(Sample sample)
        {
            var packageName = sample.package?.name;
            if (string.IsNullOrEmpty(packageName))
                return false;

            m_PackageManagerWindowProxy.OpenAndSelectPackage(packageName, InProjectPage.k_Id);
            return true;
        }
    }
}
