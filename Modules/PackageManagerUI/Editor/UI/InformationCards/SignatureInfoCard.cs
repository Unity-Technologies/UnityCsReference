// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class SignatureInfoCard : PackageInformationCard
{
    protected override string titleText => L10n.Tr("Signature");
    protected override InformationCardSize cardSize => InformationCardSize.Small;

    public override void Refresh(IPackageVersion version)
    {
        var signatureText = string.Empty;
        icon = Icon.None;
        iconTooltip = string.Empty;

        switch (version?.trustAndSignature)
        {
            case TrustAndSignature.FullTrustUnitySignature:
            case TrustAndSignature.FullTrustValidSignature:
            case TrustAndSignature.FullTrustBuiltInPackage:
                // The org name for a unity signed package is not always set, so we always hardcode it here to make the value consistent
                var orgName = version.trustAndSignature == TrustAndSignature.FullTrustValidSignature ? version.signatureOrgName : L10n.Tr("Unity Technologies");
                if (!string.IsNullOrEmpty(orgName))
                {
                    signatureText = orgName;
                    icon = Icon.Verified;
                    var fullTrustTooltip = L10n.Tr("Unity has verified the identity of this publisher.");
                    iconTooltip = fullTrustTooltip;
                    contentTooltip = fullTrustTooltip;
                }
                break;
            case TrustAndSignature.LimitedTrust:
                signatureText = version.signatureOrgName;
                icon = Icon.Info;
                iconTooltip = PackageSignatureHelpBox.k_LimitedTrustMessage;
                contentTooltip = PackageSignatureHelpBox.k_LimitedTrustMessage;
                break;
            case TrustAndSignature.UntrustedNoSignature:
                signatureText = L10n.Tr("Missing");
                icon = Icon.Warning;
                iconTooltip = PackageSignatureHelpBox.k_UnsignedMessage;
                contentTooltip = PackageSignatureHelpBox.k_UnsignedMessage;
                break;
            case TrustAndSignature.UntrustedInvalidSignature:
                signatureText = L10n.Tr("Invalid");
                icon = Icon.Error;
                iconTooltip = PackageSignatureHelpBox.k_InvalidSignatureMessage;
                contentTooltip = PackageSignatureHelpBox.k_InvalidSignatureMessage;
                break;
        }

        contentText = signatureText;

        var showSignature = !string.IsNullOrEmpty(signatureText);
        UIUtils.SetElementDisplay(this, showSignature);
    }
}
