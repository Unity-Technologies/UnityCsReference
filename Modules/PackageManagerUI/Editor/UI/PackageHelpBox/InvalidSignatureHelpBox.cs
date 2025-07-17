// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class InvalidSignatureHelpBox : PackageBaseHelpBox
{
    public InvalidSignatureHelpBox()
    {
        customIcon = Icon.PackageErrorLarge;
        analyticsId = "invalid-signature-package-help-box";
        text = L10n.Tr("This package has an invalid signature. This could indicate the package is potentially unsafe or malicious.");
        readMoreUrl = "https://docs.unity3d.com/Manual/upm-errors.html#pkg-invalid-sig";
    }

    public override void Refresh(IPackageVersion version)
    {
        var isVisible = version is { isInstalled: true, signatureInfo.status: SignatureStatus.Invalid };
        UIUtils.SetElementDisplay(this, isVisible);
    }
}
