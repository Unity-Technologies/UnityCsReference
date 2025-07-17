// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    class DefaultAnimationWindowController : IAnimationWindowController
    {
        [SerializeField] private AnimationKeyTime m_Time;
        public float frameRate { get; set; }

        public void Dispose() {}

        public void OnSelectionChanged() {}
        public void OnPlayModeStateChanged(PlayModeStateChange state) { }

        public float time
        {
            get => m_Time.time;
            set => m_Time = AnimationKeyTime.Time(value, frameRate);
        }

        public int frame
        {
            get => m_Time.frame;
            set => m_Time = AnimationKeyTime.Frame(value, frameRate);
        }
        public bool canPlay => false;

        public bool playing
        {
            get => false;
            set {}
        }

        public bool PlaybackUpdate() => false;

        public bool canPreview => false;

        public bool previewing
        {
            get => false;
            set {}
        }

        public bool canRecord => false;

        public bool recording
        {
            get => false;
            set {}
        }
        public void ResampleAnimation() {}

        public void ProcessCandidates() {}

        public void ClearCandidates() {}

        public virtual float GetFloatValue(EditorCurveBinding _) => 0f;
        public virtual int GetIntValue(EditorCurveBinding _) => 0;
        public virtual Object GetObjectReferenceValue(EditorCurveBinding _) => null;
    }
}
