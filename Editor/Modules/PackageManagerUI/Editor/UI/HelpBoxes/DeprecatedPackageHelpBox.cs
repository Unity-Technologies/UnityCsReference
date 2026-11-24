// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DeprecatedPackageHelpBox : PackageBaseHelpBox
    {
        public DeprecatedPackageHelpBox(IApplicationProxy application) : base(application)
        {
            messageType = HelpBoxMessageType.Warning;
            readMoreAnalyticsId = "deprecated-package-help-box";
        }

        public override void Refresh(IPackageVersion version)
        {
            // Showing two deprecation message boxes with similar text just introduces noise to the UI
            // and we decided to only show the more severe one (version deprecation error)
            var isVisible = version?.package.isDeprecated == true && !DeprecatedVersionHelpBox.IsVisibleFor(version);
            UIUtils.SetElementDisplay(this, isVisible);

            if (!isVisible)
                return;

            var message = version.package.deprecationMessage;
            text = string.IsNullOrEmpty(message) ? L10n.Tr("This package is no longer supported.", null) : message;
        }
    }
}
