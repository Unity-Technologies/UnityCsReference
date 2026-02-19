// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Use this interface to control how an animation clip is authored in the AnimationWindow.
    /// </summary>
    interface IAnimationWindowClip : IEquatable<IAnimationWindowClip>
    {
        string name { get; }
        EntityId id { get; }
        bool isValid { get; }
        bool isReadOnly { get; }
        float frameRate { get; set; }
        float length { get; }
        bool isLooping { get; }

        void LoadKeyframes(AnimationWindowCurve animationWindowCurve, List<AnimationWindowKeyframe> keyframes);
        void LoadCurves(List<AnimationWindowCurve> curves);
        void CreateCurves(ReadOnlySpan<EditorCurveBinding> bindings, List<AnimationWindowCurve> curves);

        void SaveCurve(AnimationWindowCurve curve, string undoLabel) =>
            SaveCurves(new[] { curve }, undoLabel);
        void SaveCurves(IEnumerable<AnimationWindowCurve> curves, string undoLabel);
        void SaveCurves(IEnumerable<CurveWrapper> curveWrappers, string undoLabel);

        void RemoveCurve(EditorCurveBinding binding, string undoLabel) =>
            RemoveCurves(new[] { binding }, undoLabel);
        void RemoveCurves(IEnumerable<EditorCurveBinding> bindings, string undoLabel);

        void RemoveOverridenCurve(EditorCurveBinding binding, string undoLabel) =>
            RemoveOverridenCurves(new[] { binding }, undoLabel);
        void RemoveOverridenCurves(IEnumerable<EditorCurveBinding> bindings, string undoLabel);

        void RenameCurve(EditorCurveBinding binding, string newPath, string undoLabel) =>
            RenameCurves(new[] { binding }, new[] { newPath }, undoLabel);
        public void RenameCurves(IEnumerable<EditorCurveBinding> bindings, IEnumerable<string> newPaths, string undoLabel);

        public void SetInterpolation(EditorCurveBinding binding, RotationCurveInterpolation.Mode interpolation, string undoLabel) =>
            SetInterpolations(new[] { binding }, interpolation, undoLabel);
        public void SetInterpolations(IEnumerable<EditorCurveBinding> bindings, RotationCurveInterpolation.Mode interpolation, string undoLabel);

        public EditorCurveBinding[] RemapAnimationBindingForAddKey(EditorCurveBinding binding);

        bool IsCurveCreated(EditorCurveBinding binding);
    }
}
