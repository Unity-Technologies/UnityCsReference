// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    internal abstract class IAnimationWindowControl : ScriptableObject
    {
        public virtual void OnEnable() { hideFlags = HideFlags.HideAndDontSave; }
        public abstract void OnSelectionChanged();

        public abstract AnimationKeyTime time { get; }

        public abstract void GoToTime(float time);
        public abstract void GoToFrame(int frame);

        public abstract void StartScrubTime();
        public abstract void ScrubTime(float time);
        public abstract void EndScrubTime();

        public abstract void GoToPreviousFrame();
        public abstract void GoToNextFrame();
        public abstract void GoToPreviousKeyframe();
        public abstract void GoToNextKeyframe();
        public abstract void GoToFirstKeyframe();
        public abstract void GoToLastKeyframe();

        public abstract bool canPlay { get; }
        public abstract bool playing { get; }

        public abstract bool StartPlayback();
        public abstract void StopPlayback();
        public abstract bool PlaybackUpdate();

        public abstract bool canPreview { get; }
        public abstract bool previewing { get; }

        public abstract bool StartPreview();
        public abstract void StopPreview();

        public abstract bool canRecord { get; }
        public abstract bool recording { get; }

        public abstract bool StartRecording(Object targetObject);
        public abstract void StopRecording();
        public abstract void ResampleAnimation();

        public abstract void ProcessCandidates();
        public abstract void ClearCandidates();
    }
}
