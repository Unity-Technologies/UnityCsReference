// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SignatureValidationDisabledHelpBox : PackageBaseHelpBox
    {
        public SignatureValidationDisabledHelpBox(IApplicationProxy application) : base(application)
        {
            text = L10n.Tr("Signature validation is off because Closed network environment is enabled in Project Settings.", null);
            customIcon = Icon.PackageOptionLarge;
        }

        public override void Refresh(IPackageVersion version)
        {
            UIUtils.SetElementDisplay(this, version?.trustAndSignature == TrustAndSignature.SignatureValidationDisabled);
        }
    }
}
