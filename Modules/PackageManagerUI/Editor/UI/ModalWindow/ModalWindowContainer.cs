// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal partial class ModalWindowContainer : EditorWindow
{
    #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
    internal ModalWindowContainer() {}
    #pragma warning restore UAL0015

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
        #pragma warning disable UAL0018 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
