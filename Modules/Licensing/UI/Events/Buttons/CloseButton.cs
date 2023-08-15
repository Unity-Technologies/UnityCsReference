// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEditor.Licensing.UI.Events.Text;

namespace UnityEditor.Licensing.UI.Events.Buttons;

sealed class CloseButton : TemplateEventsButton
{
    Action m_CloseAction;

    public CloseButton(Action closeAction, Action additionalClickAction)
        : base(LicenseTrStrings.BtnClose, additionalClickAction)
    {
        m_CloseAction = closeAction;
    }

    protected override void Click()
    {
        m_CloseAction?.Invoke();
    }
}
