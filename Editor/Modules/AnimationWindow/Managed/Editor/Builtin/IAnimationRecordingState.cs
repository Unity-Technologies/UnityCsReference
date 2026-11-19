// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor.AnimationWindowBuiltin
{
    interface IAnimationRecordingState
    {
        GameObject activeGameObject { get; }
        GameObject activeRootGameObject { get; }
        AnimationClip activeAnimationClip => null;
        IAnimationWindowClip activeClip => new AnimationWindowClip(activeAnimationClip);
        int currentFrame { get; }

        bool addZeroFrame { get; }

        bool hasRootCurves => activeAnimationClip != null && activeAnimationClip.hasRootCurves;

        bool DiscardModification(PropertyModification modification);
        void SaveCurve(AnimationWindowCurve curve);

        void AddKey(EditorCurveBinding binding, Type type, object previousValue, object currentValue)
        {
            var clip = activeClip;
            if (clip.isReadOnly)
                return;

            var curve = new AnimationWindowCurve(clip, binding, type);

            // Add previous value at first frame on empty curves.
            if (addZeroFrame)
            {
                // Is it a new curve?
                if (curve.length == 0)
                {
                    if (currentFrame != 0)
                    {
                        AnimationWindowUtility.AddKeyframeToCurve(curve, previousValue, type, AnimationKeyTime.Frame(0, clip.frameRate));
                    }
                }
            }

            // Add key at current frame.
            AnimationWindowUtility.AddKeyframeToCurve(curve, currentValue, type, AnimationKeyTime.Frame(currentFrame, clip.frameRate));

            SaveCurve(curve);
        }

        void AddPropertyModification(EditorCurveBinding binding, PropertyModification propertyModification,
            bool keepPrefabOverride);
    }
}
