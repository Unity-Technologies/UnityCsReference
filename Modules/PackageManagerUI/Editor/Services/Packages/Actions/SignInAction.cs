// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class SignInAction : PackageAction
{
    private readonly UnityConnectProxy m_UnityConnect;
    private readonly ApplicationProxy m_Application;
    public SignInAction(UnityConnectProxy unityConnect, ApplicationProxy application)
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
        return L10n.Tr("Sign in");
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfNoNetwork(m_Application);
    }
}
