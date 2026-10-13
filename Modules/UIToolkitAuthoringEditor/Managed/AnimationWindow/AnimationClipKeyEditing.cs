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
            if (binding.isDiscreteCurve)
            {
                var keys = AnimationUtility.GetDiscreteCurve(clip, binding);
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
            if (binding.isDiscreteCurve)
            {
                var keys = AnimationUtility.GetDiscreteCurve(clip, binding);
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
            if (binding.isDiscreteCurve)
            {
                var keys = AnimationUtility.GetDiscreteCurve(clip, binding);
                if (keys == null || keys.Length == 0)
                    return;
                var filtered = new List<DiscreteKeyframe>(keys.Length);
                for (int i = 0; i < keys.Length; i++)
                    if (!Mathf.Approximately(keys[i].time, time))
                        filtered.Add(keys[i]);
                // Null collapses the binding entirely so empty curves don't linger; matches
                // AnimationWindowControl.RemoveCurve's "drop the row when no keys remain".
                var next = filtered.Count > 0 ? filtered.ToArray() : null;
                AnimationUtility.SetDiscreteCurve(clip, binding, next);
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

        internal static void AddOrReplaceFloatKey(AnimationClip clip, EditorCurveBinding binding, float time, float value)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            AddOrReplaceFloatKey(curve, time, value);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        internal static void AddOrReplaceDiscreteKey(AnimationClip clip, EditorCurveBinding binding, float time, int value)
        {
            var curve = AnimationUtility.GetDiscreteCurve(clip, binding) ?? Array.Empty<DiscreteKeyframe>();
            curve = AddOrReplaceDiscreteKey(curve, time, value);
            AnimationUtility.SetDiscreteCurve(clip, binding, curve);
        }

        internal static void AddOrReplaceObjectReferenceKey(AnimationClip clip, EditorCurveBinding binding, float time, Object value)
        {
            MigrateLegacyBackgroundImageCurve(clip, binding);
            var existing = AnimationUtility.GetObjectReferenceCurve(clip, binding) ?? Array.Empty<ObjectReferenceKeyframe>();
            var merged = ReplaceObjectReferenceKey(existing, time, value);
            AnimationUtility.SetObjectReferenceCurve(clip, binding, merged);
        }

        const string k_CompositeImageChannel = "BackgroundImage.image";

        // Legacy clips bound the background-image PPtr slot under the bare property name; the
        // composite gradient layout renamed it to ".image". Both resolve to channel 0 at playback
        // (UIAnimationBinder.cpp's empty-channel fallback), so folding the legacy curve into the
        // new binding on write keeps a re-recorded clip from carrying two rows that drive the
        // same property.
        internal static void MigrateLegacyBackgroundImageCurve(AnimationClip clip, EditorCurveBinding binding)
        {
            if (!binding.propertyName.EndsWith(k_CompositeImageChannel, StringComparison.Ordinal))
                return;

            var legacy = binding;
            legacy.propertyName = binding.propertyName.Substring(0, binding.propertyName.Length - ".image".Length);
            var legacyKeys = AnimationUtility.GetObjectReferenceCurve(clip, legacy);
            if (legacyKeys == null || legacyKeys.Length == 0)
                return;

            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys == null || keys.Length == 0)
            {
                keys = legacyKeys;
            }
            else
            {
                // Keys already recorded under the new name win on time collisions.
                var merged = new List<ObjectReferenceKeyframe>(keys);
                foreach (var legacyKey in legacyKeys)
                {
                    bool collides = false;
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (Mathf.Approximately(keys[i].time, legacyKey.time))
                        {
                            collides = true;
                            break;
                        }
                    }
                    if (!collides)
                        merged.Add(legacyKey);
                }
                merged.Sort((a, b) => a.time.CompareTo(b.time));
                keys = merged.ToArray();
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationUtility.SetObjectReferenceCurve(clip, legacy, null);
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

        internal static DiscreteKeyframe[] AddOrReplaceDiscreteKey(DiscreteKeyframe[] curve, float time, int value)
        {
            for (int i = 0; i < curve.Length; i++)
            {
                if (Mathf.Approximately(curve[i].time, time))
                {
                    curve[i] = new DiscreteKeyframe { time = time, value = value };
                    return curve;
                }
            }

            var merged = new DiscreteKeyframe[curve.Length + 1];
            Array.Copy(curve, merged, curve.Length);
            merged[curve.Length] = new DiscreteKeyframe { time = time, value = value };
            // SetDiscreteCurve requires chronologically sorted keyframes; appending
            // a new key earlier than the last existing one would otherwise break clip
            // duration calculations and the binary search used at evaluation time.
            Array.Sort(merged, (a, b) => a.time.CompareTo(b.time));
            return merged;
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

        // Discrete curves step (no interpolation): last keyframe with time <= t wins.
        internal static int EvaluateDiscreteCurve(DiscreteKeyframe[] keys, float time)
        {
            int value = 0;
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
