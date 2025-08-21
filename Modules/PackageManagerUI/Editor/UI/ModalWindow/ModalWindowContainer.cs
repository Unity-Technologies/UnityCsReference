// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal class ModalWindowContainer : EditorWindow
{
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
        content.container = instance;

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
