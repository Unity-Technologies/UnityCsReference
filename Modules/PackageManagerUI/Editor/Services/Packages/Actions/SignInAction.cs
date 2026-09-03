// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class SignInAction : PackageAction
{
    private readonly IUnityConnectProxy m_UnityConnect;
    private readonly IApplicationProxy m_Application;
    public SignInAction(IUnityConnectProxy unityConnect, IApplicationProxy application)
    {
        m_UnityConnect = unityConnect;
        m_Application = application;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        m_UnityConnect.ShowLogin();
        PackageManagerWindowAnalytics.SendEvent("signInFromToolbar", version?.uniqueId);
        return true;
    }

    public override bool IsVisible(IPackageVersion version) => !m_UnityConnect.isUserLoggedIn &&
                                                               (version?.package.hasEntitlementsError == true ||
                                                                version?.package.product != null);

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return string.Empty;
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Sign in", null);
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    protected override DisableConditionList<IPackageVersion> CreateTemporaryDisableConditions() => new(
        new DisableIfNoNetwork(m_Application)
    );
}
