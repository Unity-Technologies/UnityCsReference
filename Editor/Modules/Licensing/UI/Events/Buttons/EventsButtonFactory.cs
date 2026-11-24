// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Licensing.UI.Events.Buttons;

class EventsButtonFactory : IEventsButtonFactory
{
    INativeApiWrapper m_NativeApiWrapper;
    Action m_CloseModalAction;

    public EventsButtonFactory(INativeApiWrapper nativeApiWrapper,
        Action closeModalAction)
    {
        m_CloseModalAction = closeModalAction;
        m_NativeApiWrapper = nativeApiWrapper;
    }

    public VisualElement Create(EventsButtonType buttonType, Action additionalCustomClickAction)
    {
        var visualElement = buttonType switch
        {
            EventsButtonType.Ok => new OkButton(m_CloseModalAction, additionalCustomClickAction, m_NativeApiWrapper),
            EventsButtonType.ManageLicense => new ManageLicenseButton(m_CloseModalAction, additionalCustomClickAction, m_NativeApiWrapper),
            EventsButtonType.SaveAndQuit => BuildSaveAndCloseBundle(additionalCustomClickAction),
            EventsButtonType.UpdateLicense => new UpdateLicenseButton(m_CloseModalAction, additionalCustomClickAction, m_NativeApiWrapper),
            EventsButtonType.OpenUnityHub => new OpenUnityHubButton(m_CloseModalAction, additionalCustomClickAction, m_NativeApiWrapper),
            _ => throw new ArgumentException($"Unknown button type: {buttonType}")
        };

        return visualElement;
    }

    VisualElement BuildSaveAndCloseBundle(Action additionalCustomAction)
    {
        VisualElement visualElement = new VisualElement();
        visualElement.Add(new CloseProjectButton(additionalCustomAction, m_CloseModalAction, m_NativeApiWrapper));
        if (m_NativeApiWrapper.HasUnsavedScenes())
        {
            visualElement.Add(new SaveAndQuitButton(m_CloseModalAction, additionalCustomAction, m_NativeApiWrapper));
        }

        return visualElement;
    }
}
