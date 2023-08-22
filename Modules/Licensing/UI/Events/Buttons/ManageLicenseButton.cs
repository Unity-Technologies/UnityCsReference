// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Licensing.UI.Events.Text;

namespace UnityEditor.Licensing.UI.Events.Buttons;

sealed class ManageLicenseButton : TemplateEventsButton
{
    INativeApiWrapper m_NativeApiWrapper;
    Action m_CloseAction;

    public ManageLicenseButton(Action closeAction, Action additionalClickAction, INativeApiWrapper nativeApiWrapper)
        : base(LicenseTrStrings.BtnManageLicense, additionalClickAction)
    {
        m_NativeApiWrapper = nativeApiWrapper;
        m_CloseAction = closeAction;
    }

    protected override void Click()
    {
        m_CloseAction?.Invoke();

        m_NativeApiWrapper.OpenHubLicenseManagementWindow();
    }
}
