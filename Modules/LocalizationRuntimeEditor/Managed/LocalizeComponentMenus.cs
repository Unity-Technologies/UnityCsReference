// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Localization.Components;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEventTools = UnityEditor.Events.UnityEventTools;

namespace Unity.Localization.Editor;

static partial class LocalizeComponentMenus
{
    // The localization package takes the menu over while its own settings still drive the project.
    [AutoStaticsCleanup] // the package re-registers on every domain load
    internal static Func<AudioSource, bool> AudioSourceOverride;

    [MenuItem("CONTEXT/AudioSource/Localize")]
    static void LocalizeAudioSource(MenuCommand command)
    {
        var target = (AudioSource)command.context;
        if (AudioSourceOverride != null && AudioSourceOverride(target))
            return;
        var component = Undo.AddComponent<LocalizeAudioClipEvent>(target.gameObject);

        var setClip = (UnityAction<AudioClip>)Delegate.CreateDelegate(typeof(UnityAction<AudioClip>), target, typeof(AudioSource).GetProperty("clip").GetSetMethod());
        UnityEventTools.AddPersistentListener(component.OnUpdateAsset, setClip);
        UnityEventTools.AddVoidPersistentListener(component.OnUpdateAsset, target.Play);
        component.OnUpdateAsset.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
        component.OnUpdateAsset.SetPersistentListenerState(1, UnityEventCallState.EditorAndRuntime);
    }
}
