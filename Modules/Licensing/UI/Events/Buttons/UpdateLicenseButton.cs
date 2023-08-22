// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Licensing.UI.Events.Text;

namespace UnityEditor.Licensing.UI.Events.Buttons;

sealed class UpdateLicenseButton : TemplateEventsButton
{
    INativeApiWrapper m_NativeApiWrapper;
    Action m_CloseAction;

    public UpdateLicenseButton(Action closeAction, Action additionalClickAction, INativeApiWrapper nativeApiWrapper)
        : base(LicenseTrStrings.BtnConnect, additionalClickAction)
    {
        m_NativeApiWrapper = nativeApiWrapper;
        m_CloseAction = closeAction;
    }

    protected override void Click()
    {
        if (m_NativeApiWrapper.UpdateLicense())
        {
            m_CloseAction?.Invoke();
        }
    }
}
