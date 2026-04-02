// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.AnimationWindowBuiltin;
using UnityEngine;

// Compatibility functions to support Timeline's use of the AnimationWindow internal API
namespace UnityEditor
{
    static partial class RotationCurveInterpolation
    {
        public static EditorCurveBinding RemapAnimationBindingForRotationCurves(EditorCurveBinding curveBinding, AnimationClip clip) =>
            AnimationWindowBuiltin.RotationCurveInterpolation.RemapAnimationBindingForRotationCurves(curveBinding, clip);
    }
}

namespace UnityEditorInternal
{
    partial class AnimationWindowState : ICurveEditorState
    {
        public AnimationClip activeAnimationClip
        {
            get
            {
                if (activeClip is AnimationWindowClip clip)
                    return clip.animationClip;

                return null;
            }
            set
            {
                var clip = value != null ? new AnimationWindowClip(value, selection.gameObject) : null;
                activeClip = clip;
            }
        }
    }

    static partial class AnimationWindowUtility
    {
        public static void SaveCurve(AnimationClip clip, AnimationWindowCurve curve)
        {
            if (curve.clip.isReadOnly)
                throw new ArgumentException("Curve is not editable and shouldn't be saved.");

            if (curve.isPPtrCurve)
            {
                ObjectReferenceKeyframe[] objectCurve = curve.ToObjectCurve();

                if (objectCurve.Length == 0)
                    objectCurve = null;

                AnimationUtility.SetObjectReferenceCurve(clip, curve.binding, objectCurve);
            }
            else
            {
                AnimationCurve animationCurve = curve.ToAnimationCurve();

                if (animationCurve.keys.Length == 0)
                    animationCurve = null;
                else
                    AnimationUtility.UpdateTangentsFromMode(animationCurve);

                AnimationUtility.SetEditorCurve(clip, curve.binding, animationCurve);
            }
        }
    }

    class AnimationRecording : UnityEditor.AnimationWindowBuiltin.AnimationRecording
    {
    }

    interface IAnimationRecordingState : UnityEditor.AnimationWindowBuiltin.IAnimationRecordingState
    {
    }
}
