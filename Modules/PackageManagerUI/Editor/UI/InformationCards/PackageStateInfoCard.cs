// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class PackageStateInfoCard : PackageInformationCard
{
    protected override string titleText => L10n.Tr("Package State");
    protected override InformationCardSize cardSize => InformationCardSize.Small;

    [SerializeField]
    private Button m_LinkButton;
    [SerializeField]
    private string m_ButtonUrl;
    [SerializeField]
    private string m_AnalyticsId;
    [SerializeField]
    private IPackageVersion m_Version;

    private IApplicationProxy m_ApplicationProxy;

    public PackageStateInfoCard(IApplicationProxy applicationProxy)
    {
        m_ApplicationProxy = applicationProxy;
        m_LinkButton = new Button { classList = { "link" } };
        m_Content.Add(m_LinkButton);
        contentText = string.Empty;

        m_LinkButton.clickable.clicked += () =>
        {
            m_ApplicationProxy.OpenURL(m_ButtonUrl);
            PackageManagerWindowAnalytics.SendEvent(m_AnalyticsId, m_Version);
        };
    }

    public override void Refresh(IPackageVersion version)
    {
        var buttonText = string.Empty;
        m_ButtonUrl = string.Empty;
        m_AnalyticsId = string.Empty;
        m_Version = version;
        if (version.HasTag(PackageTag.Experimental))
        {
            buttonText = L10n.Tr("Experimental");
            m_ButtonUrl = $"https://docs.unity3d.com/{m_ApplicationProxy.shortUnityVersion}/Documentation/Manual/pack-exp.html";
            m_AnalyticsId = "package-state-experimental-link";
        }
        else if (version.HasTag(PackageTag.PreRelease))
        {
            buttonText = L10n.Tr("Pre-Release");
            m_ButtonUrl = $"https://docs.unity3d.com/{m_ApplicationProxy.shortUnityVersion}/Documentation/Manual/pack-preview.html";
            m_AnalyticsId = "package-state-pre-release-link";
        }

        var isVisible = !string.IsNullOrEmpty(m_ButtonUrl);
        UIUtils.SetElementDisplay(this, isVisible);

        if (!isVisible)
            return;

        m_LinkButton.text = buttonText;
    }
}
