// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class VersionTagHelpBox : PackageHelpBoxWithReadMore
    {
        public VersionTagHelpBox(IApplicationProxy application) : base(application)
        {
            messageType = HelpBoxMessageType.Info;
        }

        public override void Refresh(IPackageVersion version)
        {
            UIUtils.SetElementDisplay(this, false);
            if (version == null)
                return;

            if (version.HasTag(PackageTag.Experimental))
            {
                text = L10n.Tr("Experimental packages are new packages or experiments on mature packages in the early stages of development. " +
                               "Experimental packages are not supported by Unity.");
                m_ReadMoreUrl = $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/pack-exp.html";
                UIUtils.SetElementDisplay(this, true);
            }
            else if (version.HasTag(PackageTag.PreRelease))
            {
                text = L10n.Tr("Pre-release packages are in the process of becoming stable and will be available as production-ready by the end of this LTS release. " +
                               "We recommend using these only for testing purposes and to give us direct feedback until then.");
                m_ReadMoreUrl = $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/pack-preview.html";
                UIUtils.SetElementDisplay(this, true);
            }
            else if (version.HasTag(PackageTag.ReleaseCandidate))
            {
                text = L10n.Tr("Release Candidate (RC) versions of a package will transition to Released with the current editor release. " +
                               "RCs are supported by Unity");
                m_ReadMoreUrl = $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/pack-releasecandidate.html";
                UIUtils.SetElementDisplay(this, true);
            }
        }
    }
}
