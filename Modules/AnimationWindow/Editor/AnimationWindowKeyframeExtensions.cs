// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    static class AnimationWindowKeyframeExtensions
    {
        public static void FromKeyframe(this AnimationWindowKeyframe awk, AnimationWindowCurve curve, Keyframe key)
        {
            awk.time = key.time;
            awk.value = key.value;
            awk.curve = curve;
            awk.inTangent = key.inTangent;
            awk.outTangent = key.outTangent;
            awk.inWeight = key.inWeight;
            awk.outWeight = key.outWeight;
            awk.weightedMode = key.weightedMode;
            awk.leftTangentMode = AnimationUtility.GetKeyLeftTangentMode(key);
            awk.rightTangentMode = AnimationUtility.GetKeyRightTangentMode(key);
            awk.brokenTangent = AnimationUtility.GetKeyBroken(key);
        }

        public static void FromObjectReferenceKeyframe(this AnimationWindowKeyframe awk, AnimationWindowCurve curve, ObjectReferenceKeyframe key)
        {
            awk.time = key.time;
            awk.value = key.value;
            awk.curve = curve;
        }

        public static void FromDiscreteKeyframe(this AnimationWindowKeyframe awk, AnimationWindowCurve curve, DiscreteKeyframe key)
        {
            awk.time = key.time;
            awk.value = key.value;
            awk.curve = curve;
        }

        public static Keyframe ToKeyframe(this AnimationWindowKeyframe awk)
        {
            var keyframe = new Keyframe(awk.time, (float)awk.value, awk.inTangent, awk.outTangent);

            AnimationUtility.SetKeyLeftTangentMode(ref keyframe, awk.leftTangentMode);
            AnimationUtility.SetKeyRightTangentMode(ref keyframe, awk.rightTangentMode);
            AnimationUtility.SetKeyBroken(ref keyframe, awk.brokenTangent);

            keyframe.weightedMode = awk.weightedMode;
            keyframe.inWeight = awk.inWeight;
            keyframe.outWeight = awk.outWeight;

            return keyframe;
        }

        public static ObjectReferenceKeyframe ToObjectReferenceKeyframe(this AnimationWindowKeyframe awk)
        {
            var keyframe = new ObjectReferenceKeyframe();

            keyframe.time = awk.time;
            keyframe.value = (UnityEngine.Object)awk.value;

            return keyframe;
        }

        public static DiscreteKeyframe ToDiscreteKeyframe(this AnimationWindowKeyframe awk)
        {
            var keyframe = new DiscreteKeyframe();

            keyframe.time = awk.time;
            keyframe.value = (int)awk.value;

            return keyframe;
        }
    }
}
