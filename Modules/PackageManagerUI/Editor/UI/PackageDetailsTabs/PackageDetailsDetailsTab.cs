// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsDetailsTab : PackageDetailsTabElement
    {
        public const string k_Id = "description";

        private const string k_EmptyDescriptionClass = "empty";
        private const int k_MaxDescriptionCharacters = 10000;

        public override bool IsValid(IPackageVersion version)
        {
            return version?.HasTag(PackageTag.UpmFormat) == true;
        }

        public PackageDetailsDetailsTab(IUnityConnectProxy unityConnect, IResourceLoader resourceLoader) : base(unityConnect)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Details");
            var root = resourceLoader.GetTemplate("DetailsTabs/PackageDetailsDetailsTab.uxml");
            m_ContentContainer.Add(root);
            m_Cache = new VisualElementCache(root);
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            packagePlatformList.Refresh(version);
            RefreshDescription(version);
            RefreshSourcePath(version);
            RefreshTechnicalName(version);
            RefreshSignature(version);
            RefreshMinimumUnityVersion(version);
        }

        private void RefreshDescription(IPackageVersion version)
        {
            var hasVersionDescription = !string.IsNullOrEmpty(version.description);
            var desc = hasVersionDescription ? version.description : L10n.Tr("There is no description for this package.");
            if (desc.Length > k_MaxDescriptionCharacters)
                desc = desc.Substring(0, k_MaxDescriptionCharacters);
            detailDescription.EnableInClassList(k_EmptyDescriptionClass, !hasVersionDescription);
            detailDescription.text = desc;
        }

        private void RefreshSourcePath(IPackageVersion version)
        {
            var sourcePath = (version as UpmPackageVersion)?.sourcePath;
            UIUtils.SetElementDisplay(detailSourcePathContainer, !string.IsNullOrEmpty(sourcePath));

            if (!string.IsNullOrEmpty(sourcePath))
                detailSourcePath.text = sourcePath.EscapeBackslashes();
        }

        void RefreshTechnicalName(IPackageVersion version)
        {
            // We use package.name instead of version.name because `version.name` would be empty for a PlaceholderPackageVersion
            var technicalName = version?.package?.name ?? string.Empty;
            detailTechnicalName.text = technicalName;
            copyIcon.SetTextToCopy(technicalName);
        }

        private void RefreshSignature(IPackageVersion version)
        {
            var signatureText = string.Empty;
            var icon = Icon.None;
            var iconTooltip = string.Empty;

            switch (version?.trustAndSignature)
            {
                case TrustAndSignature.FullTrustUnitySignature:
                case TrustAndSignature.FullTrustValidSignature:
                    // The org name for a unity signed package is not always set, so we always hardcode it here to make the value consistent
                    var orgName = version.trustAndSignature == TrustAndSignature.FullTrustUnitySignature ? L10n.Tr("Unity Technologies") : version.signatureOrgName;
                    if (!string.IsNullOrEmpty(orgName))
                    {
                        signatureText = string.Format(L10n.Tr("Signed for {0}"), orgName);
                        icon = Icon.Verified;
                        iconTooltip = L10n.Tr("Unity has verified the identity of this publisher.");
                    }
                    break;
                case TrustAndSignature.FullTrustBuiltInPackage:
                    signatureText = L10n.Tr("Made by Unity Technologies");
                    icon = Icon.Verified;
                    iconTooltip = L10n.Tr("Unity has verified the identity of this publisher.");
                    break;
                case TrustAndSignature.LimitedTrust:
                    signatureText = string.Format(L10n.Tr("Signed for {0}"), version.signatureOrgName);
                    icon = Icon.Info;
                    iconTooltip = PackageSignatureHelpBox.k_LimitedTrustMessage;
                    break;
                case TrustAndSignature.UntrustedNoSignature:
                    signatureText = L10n.Tr("Missing");
                    icon = Icon.Warning;
                    iconTooltip = PackageSignatureHelpBox.k_UnsignedMessage;
                    break;
                case TrustAndSignature.UntrustedInvalidSignature:
                    signatureText = L10n.Tr("Invalid");
                    icon = Icon.Error;
                    iconTooltip = PackageSignatureHelpBox.k_InvalidSignatureMessage;
                    break;
            }

            detailSignature.text = signatureText;

            signatureStateIcon.ClearClassList();
            signatureStateIcon.AddToClassList(icon.ClassName());
            signatureStateIcon.tooltip = iconTooltip;

            var showSignature = !string.IsNullOrEmpty(signatureText);
            UIUtils.SetElementDisplay(detailSignatureTitle, showSignature);
            UIUtils.SetElementDisplay(signatureStateIcon, showSignature);
            UIUtils.SetElementDisplay(detailSignature, showSignature);
        }

        private void RefreshMinimumUnityVersion(IPackageVersion version)
        {
            var isVisible = !version.HasTag(PackageTag.Feature | PackageTag.BuiltIn);
            UIUtils.SetElementDisplay(detailMinimumUnityVersionTitle, isVisible);
            UIUtils.SetElementDisplay(detailMinimumUnityVersion, isVisible);

            if (!isVisible)
                return;

            var minimumUnityVersion = !string.IsNullOrEmpty(version.minimumUnityVersion) ? version.minimumUnityVersion : L10n.Tr("Not set");
            detailMinimumUnityVersion.text = minimumUnityVersion;
        }

        private readonly VisualElementCache m_Cache;
        private PackagePlatformList packagePlatformList => m_Cache.Get<PackagePlatformList>("detailPlatformList");
        private SelectableLabel detailDescription => m_Cache.Get<SelectableLabel>("detailDescription");
        private VisualElement detailSourcePathContainer => m_Cache.Get<VisualElement>("detailSourcePathContainer");
        private SelectableLabel detailSourcePath => m_Cache.Get<SelectableLabel>("detailSourcePath");
        private SelectableLabel detailTechnicalName => m_Cache.Get<SelectableLabel>("detailTechnicalName");
        private Label detailSignatureTitle => m_Cache.Get<Label>("detailSignatureTitle");
        private VisualElement signatureStateIcon => m_Cache.Get<VisualElement>("signatureStateIcon");
        private SelectableLabel detailSignature => m_Cache.Get<SelectableLabel>("detailSignature");
        private Label detailMinimumUnityVersionTitle => m_Cache.Get<Label>("detailMinimumUnityVersionTitle");
        private SelectableLabel detailMinimumUnityVersion => m_Cache.Get<SelectableLabel>("detailMinimumUnityVersion");
        private CopyIconButton copyIcon => m_Cache.Get<CopyIconButton>("copyIcon");
    }
}
