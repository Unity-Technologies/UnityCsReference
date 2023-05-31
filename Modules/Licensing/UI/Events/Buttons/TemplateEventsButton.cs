// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Licensing.UI.Events.Buttons;

abstract class TemplateEventsButton : Button
{
    Action m_AdditionalClickAction;

    protected TemplateEventsButton(string labelText, Action additionalClickAction)
    {
        m_AdditionalClickAction = additionalClickAction;

        text = labelText;
        name = string.Empty;

        foreach (var c in text)
        {
            if (!char.IsWhiteSpace(c))
            {
                name += c;
            }
        }

        // For guidelines refer to:
        // https://www.figma.com/file/GdibV2c1v5vP8kgBEraDgj/%E2%9D%96-Editor-Foundations-%E2%80%93%E2%80%93-Figma-Toolkit-(Community)?node-id=4159-111248&t=Uuszom0SMChZTRHQ-0
        style.marginRight = 0;
        style.marginLeft = 3;

        clicked += OnClicked;
    }

    void OnClicked()
    {
        m_AdditionalClickAction?.Invoke();

        Click();
    }

    protected abstract void Click();
}
