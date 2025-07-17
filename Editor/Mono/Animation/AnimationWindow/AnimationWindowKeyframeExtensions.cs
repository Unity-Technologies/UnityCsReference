// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEditor
{
    static class AnimationWindowKeyframeExtensions
    {
        public static void FromKeyframe(this AnimationWindowKeyframe awk, AnimationWindowCurve curve, Keyframe key)
        {
            awk.time = key.time;

            if (curve.isDiscreteCurve)
            {
                awk.value = ConvertFloatToDiscreteInt(key.value);
            }
            else
            {
                awk.value = key.value;
            }

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

        public static Keyframe ToKeyframe(this AnimationWindowKeyframe awk)
        {
            float floatValue;
            if (awk.curve.isDiscreteCurve)
            {
                // case 1395978
                // Negative int values converted to float create NaN values. Limiting discrete int values to only positive values
                // until we rewrite the animation backend with dedicated int curves.
                floatValue = ConvertDiscreteIntToFloat(Math.Max((int)awk.value, 0));
            }
            else
            {
                floatValue = (float)awk.value;
            }

            var keyframe = new Keyframe(awk.time, floatValue, awk.inTangent, awk.outTangent);

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

        static unsafe float ConvertDiscreteIntToFloat(int intToConvert)
        {
            float converted = 0f;
            UnsafeUtility.MemCpy(
                UnsafeUtility.AddressOf(ref converted),
                UnsafeUtility.AddressOf(ref intToConvert),
                UnsafeUtility.SizeOf<int>());
            return converted;
        }

        static unsafe int ConvertFloatToDiscreteInt(float floatToConvert)
        {
            int converted = 0;
            UnsafeUtility.MemCpy(
                UnsafeUtility.AddressOf(ref converted),
                UnsafeUtility.AddressOf(ref floatToConvert),
                UnsafeUtility.SizeOf<float>());
            return converted;
        }
    }
}
