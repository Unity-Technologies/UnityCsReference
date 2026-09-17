// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal partial class ModalWindowContainer : EditorWindow
{

    [AutoStaticsCleanupOnCodeReload]
    private static ModalWindowContainer instance { get; set; }

    private ModalContent m_Content;
    public static bool ShowModal(ModalContent content)
    {
        if (instance is not null || content == null)
            return false;

        instance = CreateInstance<ModalWindowContainer>();
        instance.rootVisualElement.Add(content);
        instance.titleContent = new GUIContent(content.windowTitle);
        instance.m_Content = content;
#pragma warning disable UAL0018 // the content lives in this window's visual tree only: closing the modal clears both the window reference and the content, so the back-reference cannot outlive the window
        content.container = instance;
#pragma warning restore UAL0018

        instance.m_Content?.OnBeforeShowModal();
        instance.ShowModal();
        return true;
    }

    private void OnDisable()
    {
        instance = null;

        m_Content?.OnModalClosed();
        m_Content = null;
    }

    internal override bool CanMaximize() => false;
}
