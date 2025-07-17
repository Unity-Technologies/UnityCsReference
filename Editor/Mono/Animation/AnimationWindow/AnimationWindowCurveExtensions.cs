// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    static class AnimationWindowCurveExtensions
    {
        public static void FromAnimationCurve(this AnimationWindowCurve awc, AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
            {
                var awk = new AnimationWindowKeyframe();
                awk.FromKeyframe(awc, curve[i]);
                awc.AddKeyframeFast(awk);
            }
        }

        public static void FromObjectReferenceCurve(this AnimationWindowCurve awc, ObjectReferenceKeyframe[] curve)
        {
            for (int i = 0; i < curve.Length; i++)
            {
                var awk = new AnimationWindowKeyframe();
                awk.FromObjectReferenceKeyframe(awc, curve[i]);
                awc.AddKeyframeFast(awk);
            }
        }

        public static AnimationCurve ToAnimationCurve(this AnimationWindowCurve curve)
        {
            int length = curve.keyframes.Count;
            AnimationCurve animationCurve = new AnimationCurve();

            var keys = new Keyframe[length];

            for (int i = 0; i < length; i++)
            {
                Keyframe newKeyframe = curve.keyframes[i].ToKeyframe();
                keys[i] = newKeyframe;
            }

            animationCurve.keys = keys;

            return animationCurve;
        }

        public static ObjectReferenceKeyframe[] ToObjectCurve(this AnimationWindowCurve curve)
        {
            int length = curve.keyframes.Count;

            var keys = new ObjectReferenceKeyframe[length];

            for (int i = 0; i < length; i++)
            {
                ObjectReferenceKeyframe newKeyframe = curve.keyframes[i].ToObjectReferenceKeyframe();
                keys[i] = newKeyframe;
            }

            Array.Sort(keys, (a, b) => a.time.CompareTo(b.time));

            return keys;
        }
    }
}
