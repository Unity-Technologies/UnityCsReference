// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DisabledPackageHelpBox : PackageBaseHelpBox
    {
        public DisabledPackageHelpBox()
        {
            text = L10n.Tr("Unfortunately, this package is no longer available.");
            messageType = HelpBoxMessageType.Warning;
        }

        public override void Refresh(IPackageVersion version)
        {
            UIUtils.SetElementDisplay(this, version?.HasTag(PackageTag.Disabled) == true);
        }
    }
}
