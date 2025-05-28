// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ScopedRegistryHelpBox : PackageBaseHelpBox
    {
        public ScopedRegistryHelpBox(IApplicationProxy application)
        {
            text = L10n.Tr("This package is hosted on a Scoped Registry.");
            messageType = HelpBoxMessageType.Info;
            readMoreUrl = $"https://docs.unity3d.com/{application.shortUnityVersion}/Documentation/Manual/upm-scoped.html";
        }

        public override void Refresh(IPackageVersion version)
        {
            if (version == null || version.package.product != null)
            {
                UIUtils.SetElementDisplay(this, false);
                return;
            }
            UIUtils.SetElementDisplay(this, version.availableRegistry == RegistryType.MyRegistries);
        }
    }
}
