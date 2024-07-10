// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DeprecatedPackageHelpBox : PackageBaseHelpBox
    {
        public DeprecatedPackageHelpBox()
        {
            messageType = HelpBoxMessageType.Warning;
        }

        public override void Refresh(IPackageVersion version)
        {
            var isVisible = version?.package.isDeprecated == true;
            UIUtils.SetElementDisplay(this, isVisible);

            if (!isVisible)
                return;

            var message = version.package.deprecationMessage;
            text = string.IsNullOrEmpty(message) ? L10n.Tr("This package is no longer supported.") : message;
        }
    }
}
