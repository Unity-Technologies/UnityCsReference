// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class VersionTagHelpBox : PackageBaseHelpBox
    {
        private readonly IApplicationProxy m_Application;
        public VersionTagHelpBox(IApplicationProxy application)
        {
            m_Application = application;

            messageType = HelpBoxMessageType.Info;
        }

        public override void Refresh(IPackageVersion version)
        {
            UIUtils.SetElementDisplay(this, false);
            if (version == null)
                return;

            if (version.HasTag(PackageTag.Experimental))
            {
                text = L10n.Tr("Experimental package versions are experiments in the early stages of development. They are usually not supported by package authors and it is not recommended to use them in production.");
                readMoreUrl = $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/pack-exp.html";
                UIUtils.SetElementDisplay(this, true);
            }
            else if (version.HasTag(PackageTag.PreRelease))
            {
                text = L10n.Tr("Pre-release package versions are in the process of becoming stable. The recommended best practice is to use them only for testing purposes and to give direct feedback to the authors.");
                readMoreUrl = $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/pack-preview.html";
                UIUtils.SetElementDisplay(this, true);
            }
        }
    }
}
