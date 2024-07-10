// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DeprecatedVersionHelpBox : PackageBaseHelpBox
    {
        public DeprecatedVersionHelpBox()
        {
            messageType = HelpBoxMessageType.Error;
        }

        public override void Refresh(IPackageVersion version)
        {
            var isVisible = version is { isInstalled: true } && version.HasTag(PackageTag.Deprecated);
            UIUtils.SetElementDisplay(this, isVisible);

            if (!isVisible)
                return;

            var message = version.deprecationMessage;
            text = string.IsNullOrEmpty(message) ? L10n.Tr("This installed version of the package is deprecated.") : message;
        }
    }
}
