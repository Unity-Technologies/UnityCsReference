// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class PackageSignatureHelpBox : PackageBaseHelpBox
{
    public override void Refresh(IPackageVersion version)
    {
        if (version is not { isInstalled: true } || version.HasTag(PackageTag.BuiltIn))
        {
            UIUtils.SetElementDisplay(this, false);
            return;
        }

        var showHelpBox = true;
        switch (version.trustLevel)
        {
            case TrustLevel.FullTrust:
            case TrustLevel.LimitedTrust:
            case TrustLevel.OutOfTrust:
                showHelpBox = false;
                break;
            case TrustLevel.Untrusted:
            default:
            {
                switch (version.signature?.status)
                {
                    case SignatureStatus.Unsigned:
                        customIcon = Icon.PackageWarningLarge;
                        analyticsId = "unsigned-package-help-box";
                        text = L10n.Tr("Unity cannot verify this package because it lacks a signature. To protect your project, it is best practice to only use signed packages. Learn more about why signature is important.");
                        readMoreUrl = "https://docs.unity3d.com/Manual/upm-signature.html";
                        break;
                    case SignatureStatus.Invalid:
                        customIcon = Icon.PackageErrorLarge;
                        analyticsId = "invalid-signature-package-help-box";
                        text = L10n.Tr("This package has an invalid signature, which might indicate the package has been tampered with, is unsafe, or malicious. Consider removing this package.");
                        readMoreUrl = "https://docs.unity3d.com/Manual/upm-errors.html#pkg-invalid-sig";
                        break;
                    default:
                        showHelpBox = false;
                        break;
                }
                break;
            }
        }
        UIUtils.SetElementDisplay(this, showHelpBox);
    }
}
