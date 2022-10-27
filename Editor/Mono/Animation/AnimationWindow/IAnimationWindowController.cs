// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    interface IAnimationWindowController
    {
        void OnCreate(AnimationWindow animationWindow, UnityEngine.Component component);
        void OnDestroy();
        void OnSelectionChanged();

        float time { get; set; }
        int frame { get; set; }

        bool canPlay { get; }
        bool playing { get; set; }

        bool PlaybackUpdate();

        bool canPreview { get; }
        bool previewing { get; set; }

        bool canRecord { get; }
        bool recording { get; set; }

        void ResampleAnimation();

        void ProcessCandidates();
        void ClearCandidates();

        EditorCurveBinding[] GetAnimatableBindings(GameObject gameObject);
        EditorCurveBinding[] GetAnimatableBindings();
        System.Type GetValueType(EditorCurveBinding binding);
        float GetFloatValue(EditorCurveBinding binding);
        int GetIntValue(EditorCurveBinding binding);
        UnityEngine.Object GetObjectReferenceValue(EditorCurveBinding binding);

    }
}
