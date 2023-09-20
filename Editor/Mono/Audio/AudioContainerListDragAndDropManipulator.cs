// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor;

class AudioContainerListDragAndDropManipulator : PointerManipulator
{
    internal delegate void AddAudioClipsDelegate(List<AudioClip> audioClips);

    internal AddAudioClipsDelegate addAudioClipsDelegate;

    public AudioContainerListDragAndDropManipulator(VisualElement root)
    {
        target = root.Q<VisualElement>("audio-clips-list-view");
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
        target.RegisterCallback<DragPerformEvent>(OnDragPerform);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target?.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
        target?.UnregisterCallback<DragPerformEvent>(OnDragPerform);
    }

    static void OnDragUpdate(DragUpdatedEvent _)
    {
        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
    }

    void OnDragPerform(DragPerformEvent evt)
    {
        var audioClips = new List<AudioClip>();

        foreach (var path in DragAndDrop.paths)
        {
            var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (audioClip != null)
                audioClips.Add(audioClip);
        }

        if (audioClips.Count > 0)
        {
            DragAndDrop.AcceptDrag();
            addAudioClipsDelegate?.Invoke(audioClips);
        }
    }
}
