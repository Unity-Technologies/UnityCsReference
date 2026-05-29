// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class PackageSignatureHelpBox : PackageBaseHelpBox
{
    public static readonly string k_LimitedTrustMessage = L10n.Tr("This package is signed, but its publisher is not verified by Unity. Please ensure you understand where this package originated from.");
    public static readonly string k_UnsignedMessage = L10n.Tr("Unity can't verify this package because it doesn't have a signature. Use signed packages to reduce risk to your project.");
    public static readonly string k_InvalidSignatureMessage = L10n.Tr("This package has an invalid signature which can indicate unsafe or malicious content. Remove this package to reduce risk to your project.");

    public PackageSignatureHelpBox(IApplicationProxy application) : base(application)
    {
    }

    public override void Refresh(IPackageVersion version)
    {
        var showHelpBox = true;
        switch (version?.trustAndSignature)
        {
            case TrustAndSignature.LimitedTrust:
                customIcon = Icon.PackageOptionLarge;
                readMoreAnalyticsId = "limited-trust-package-help-box";
                text = k_LimitedTrustMessage;
                readMoreUrl =$"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/upm-signature.html";
                break;
            case TrustAndSignature.UntrustedNoSignature:
                customIcon = Icon.PackageWarningLarge;
                readMoreAnalyticsId = "unsigned-package-help-box";
                text = k_UnsignedMessage;
                readMoreUrl =$"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/upm-signature.html";
                break;
            case TrustAndSignature.UntrustedInvalidSignature:
                customIcon = Icon.PackageErrorLarge;
                readMoreAnalyticsId = "invalid-signature-package-help-box";
                text = k_InvalidSignatureMessage;
                readMoreUrl =$"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/upm-errors.html#pkg-invalid-sig";
                break;
            default:
                showHelpBox = false;
                break;
        }
        UIUtils.SetElementDisplay(this, showHelpBox);
    }
}
