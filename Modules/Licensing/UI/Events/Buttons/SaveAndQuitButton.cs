// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Licensing.UI.Events.Text;

namespace UnityEditor.Licensing.UI.Events.Buttons
{
sealed class SaveAndQuitButton : TemplateEventsButton
{
    INativeApiWrapper m_NativeApiWrapper;
    Action m_CloseAction;

    public SaveAndQuitButton(Action closeAction, Action additionalClickAction, INativeApiWrapper nativeApiWrapper)
        : base(LicenseTrStrings.BtnSaveAndQuit, additionalClickAction)
    {
        m_NativeApiWrapper = nativeApiWrapper;
        m_CloseAction = closeAction;
    }

    protected override void Click()
    {
        m_CloseAction?.Invoke();

        // Method to save changes will not work if we are in playmode
        // that's why we cancel playmode and call save method on playmode state change event handler
        if (EditorApplication.isPlaying)
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorApplication.isPlaying = false;
        }
        else
        {
            SaveChangesAndExit();
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        SaveChangesAndExit();
    }

    private void SaveChangesAndExit()
    {
        m_NativeApiWrapper.SaveUnsavedChanges();
        m_NativeApiWrapper.ExitEditor();
    }
}
}
