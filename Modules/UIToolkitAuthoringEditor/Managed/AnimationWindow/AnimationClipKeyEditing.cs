// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    // Stateless clip-key/curve mutation helpers used by VisualElementAnimationWindowController.
    // Every operation here works directly against an AnimationClip / EditorCurveBinding /
    // AnimationCurve / ObjectReferenceKeyframe[] - Could be moved to the Animation's editor code at one point.
    internal static class AnimationClipKeyEditing
    {
        internal static bool BindingHasCurve(AnimationClip clip, EditorCurveBinding binding)
        {
            if (binding.isPPtrCurve)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                return keys != null && keys.Length > 0;
            }
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            return curve != null && curve.length > 0;
        }

        internal static bool HasKeyAtTime(AnimationClip clip, EditorCurveBinding binding, float time)
        {
            if (binding.isPPtrCurve)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (keys == null)
                    return false;
                for (int i = 0; i < keys.Length; i++)
                    if (Mathf.Approximately(keys[i].time, time))
                        return true;
                return false;
            }

            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null)
                return false;
            for (int i = 0; i < curve.length; i++)
                if (Mathf.Approximately(curve[i].time, time))
                    return true;
            return false;
        }

        internal static void RemoveKeyAtTime(AnimationClip clip, EditorCurveBinding binding, float time)
        {
            if (binding.isPPtrCurve)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (keys == null || keys.Length == 0)
                    return;
                var filtered = new List<ObjectReferenceKeyframe>(keys.Length);
                for (int i = 0; i < keys.Length; i++)
                    if (!Mathf.Approximately(keys[i].time, time))
                        filtered.Add(keys[i]);
                // Null collapses the binding entirely so empty curves don't linger; matches
                // AnimationWindowControl.RemoveCurve's "drop the row when no keys remain".
                var next = filtered.Count > 0 ? filtered.ToArray() : null;
                AnimationUtility.SetObjectReferenceCurve(clip, binding, next);
                return;
            }

            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null || curve.length == 0)
                return;
            for (int i = 0; i < curve.length; i++)
            {
                if (Mathf.Approximately(curve[i].time, time))
                {
                    curve.RemoveKey(i);
                    break;
                }
            }
            var nextCurve = curve.length > 0 ? curve : null;
            AnimationUtility.SetEditorCurve(clip, binding, nextCurve);
        }

        // Caller is responsible for bit-encoding the value when binding.isDiscreteCurve.
        internal static void AddOrReplaceKey(AnimationClip clip, EditorCurveBinding binding, float time, float value)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            if (binding.isDiscreteCurve)
            {
                AddOrReplaceDiscreteKey(curve, time, value);
            }
            else
            {
                AddOrReplaceFloatKey(curve, time, value);
            }
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        internal static void AddOrReplaceObjectReferenceKey(AnimationClip clip, EditorCurveBinding binding, float time, Object value)
        {
            var existing = AnimationUtility.GetObjectReferenceCurve(clip, binding) ?? Array.Empty<ObjectReferenceKeyframe>();
            var merged = ReplaceObjectReferenceKey(existing, time, value);
            AnimationUtility.SetObjectReferenceCurve(clip, binding, merged);
        }

        internal static void AddOrReplaceFloatKey(AnimationCurve curve, float time, float value)
        {
            for (int i = 0; i < curve.length; i++)
            {
                if (Mathf.Approximately(curve.keys[i].time, time))
                {
                    var k = curve.keys[i];
                    k.value = value;
                    curve.MoveKey(i, k);
                    return;
                }
            }
            curve.AddKey(time, value);
        }

        internal static void AddOrReplaceDiscreteKey(AnimationCurve curve, float time, float bitEncodedValue)
        {
            int existing = -1;
            for (int i = 0; i < curve.length; i++)
            {
                if (Mathf.Approximately(curve[i].time, time))
                {
                    existing = i;
                    break;
                }
            }

            var key = new Keyframe(time, bitEncodedValue) { weightedMode = WeightedMode.None };
            int index = existing >= 0 ? curve.MoveKey(existing, key) : curve.AddKey(key);
            // Step between bit-encoded ints instead of interpolating - same shape
            // as the panel-wide discrete recording path.
            if (index >= 0)
            {
                AnimationUtility.SetKeyBroken(curve, index, true);
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
            }
        }

        internal static ObjectReferenceKeyframe[] ReplaceObjectReferenceKey(ObjectReferenceKeyframe[] existing, float time, Object value)
        {
            for (int i = 0; i < existing.Length; i++)
            {
                if (Mathf.Approximately(existing[i].time, time))
                {
                    existing[i] = new ObjectReferenceKeyframe { time = time, value = value };
                    return existing;
                }
            }
            var merged = new ObjectReferenceKeyframe[existing.Length + 1];
            Array.Copy(existing, merged, existing.Length);
            merged[existing.Length] = new ObjectReferenceKeyframe { time = time, value = value };
            // SetObjectReferenceCurve requires chronologically sorted keyframes; appending
            // a new key earlier than the last existing one would otherwise break clip
            // duration calculations and the binary search used at evaluation time.
            Array.Sort(merged, (a, b) => a.time.CompareTo(b.time));
            return merged;
        }

        // PPtr curves step (no interpolation): last keyframe with time <= t wins.
        internal static Object EvaluateObjectReferenceCurve(ObjectReferenceKeyframe[] keys, float time)
        {
            Object value = null;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].time <= time)
                    value = keys[i].value;
                else
                    break;
            }
            return value;
        }
    }
}
