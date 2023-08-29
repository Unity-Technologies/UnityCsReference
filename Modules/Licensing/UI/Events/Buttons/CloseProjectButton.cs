// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Licensing.UI.Events.Text;

namespace UnityEditor.Licensing.UI.Events.Buttons
{
sealed class CloseProjectButton : TemplateEventsButton
{
    INativeApiWrapper m_NativeApiWrapper;
    Action m_CloseAction;

    public CloseProjectButton(Action additionalClickEvent, Action closeAction, INativeApiWrapper nativeApiWrapper)
        : base(LicenseTrStrings.BtnCloseProject, additionalClickEvent)
    {
        m_CloseAction = closeAction;
        m_NativeApiWrapper = nativeApiWrapper;
    }

    protected override void Click()
    {
        m_CloseAction?.Invoke();
        m_NativeApiWrapper.ExitEditor();
    }
}
}
