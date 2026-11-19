// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DeprecatedVersionHelpBox : PackageBaseHelpBox
    {
        public DeprecatedVersionHelpBox(IApplicationProxy application) : base(application)
        {
            messageType = HelpBoxMessageType.Error;
            readMoreAnalyticsId = "deprecated-version-help-box";
        }

        public static bool IsVisibleFor(IPackageVersion version) => version is { isInstalled: true } && version.HasTag(PackageTag.Deprecated);

        public override void Refresh(IPackageVersion version)
        {
            var isVisible = IsVisibleFor(version);
            UIUtils.SetElementDisplay(this, isVisible);

            if (!isVisible)
                return;

            var message = version.deprecationMessage;
            text = string.IsNullOrEmpty(message) ? L10n.Tr("This installed version of the package is deprecated.", null) : message;
        }
    }
}
