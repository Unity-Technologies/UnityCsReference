// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    // IAnimationContextualResponder implementation for the per-element UIAnimationClip path,
    // invoked by VisualElementContextualPropertyMenu when the user right-clicks an animatable
    // style field in the inspector. Modifications target the active UIAnimationClip; the
    // propertyPath is the binder property name (e.g. "Opacity", "Translate.x"), so we resolve
    // each one to an EditorCurveBinding using the same channel-kind table that drives
    // ProcessConsumedModifications and operate directly on the clip.
    internal sealed partial class VisualElementAnimationWindowController
    {
        // Scratch list reused across GoToNextKeyframe / GoToPreviousKeyframe so the
        // per-call key-time collection doesn't allocate. Safe because the menu actions
        // are dispatched synchronously from the main thread and TryCollectKeyTimes
        // never recurses or escapes its caller's frame.
        [NonSerialized] List<float> m_KeyTimes;

        public bool IsAnimatable(PropertyModification[] modifications)
        {
            if (modifications == null)
                return false;

            var uiClip = m_Selection?.uiAnimationClip;
            if (uiClip == null)
                return false;

            for (int i = 0; i < modifications.Length; i++)
            {
                var mod = modifications[i];
                if (!ReferenceEquals(mod.target, uiClip))
                    continue;
                if (UIAnimationBinderEditorExtensions.TryGetChannelKindForBinding(mod.propertyPath, out _))
                    return true;
            }
            return false;
        }

        public bool IsEditable(Object targetObject)
        {
            if (m_Selection == null || m_Selection.disabled || m_Selection.isReadOnly)
                return false;
            if (!previewing)
                return false;
            return ReferenceEquals(targetObject, m_Selection.uiAnimationClip);
        }

        public bool KeyExists(PropertyModification[] modifications)
        {
            var animClip = animationClip;
            if (animClip == null)
                return false;
            foreach (var binding in EnumerateBindings(modifications))
            {
                if (AnimationClipKeyEditing.HasKeyAtTime(animClip, binding, m_Time))
                    return true;
            }
            return false;
        }

        public bool CandidateExists(PropertyModification[] modifications)
        {
            if (m_CandidateClip == null)
                return false;
            foreach (var binding in EnumerateBindings(modifications))
            {
                if (AnimationClipKeyEditing.BindingHasCurve(m_CandidateClip, binding))
                    return true;
            }
            return false;
        }

        public bool CurveExists(PropertyModification[] modifications)
        {
            var animClip = animationClip;
            if (animClip == null)
                return false;
            foreach (var binding in EnumerateBindings(modifications))
            {
                if (AnimationClipKeyEditing.BindingHasCurve(animClip, binding))
                    return true;
            }
            return false;
        }

        public bool HasAnyCandidates() => m_CandidateClip != null && !m_CandidateClip.empty;

        public bool HasAnyCurves()
        {
            var animClip = animationClip;
            if (animClip == null)
                return false;
            return AnimationUtility.GetCurveBindings(animClip).Length > 0
                || AnimationUtility.GetObjectReferenceCurveBindings(animClip).Length > 0;
        }

        public void AddKey(PropertyModification[] modifications)
        {
            var animClip = animationClip;
            var binder = m_Selection?.GetCanonicalBinder();
            if (animClip == null || binder == null)
                return;

            Undo.RegisterCompleteObjectUndo(animClip, "Add Key");

            foreach (var binding in EnumerateBindings(modifications))
            {
                // Pull the current resolved value via the binder so the seeded keyframe matches
                // the element's actual pose rather than zero / null.
                if (!binder.TryReadCurrentBoundValue(binding.propertyName, out _, out var f, out var iv, out var ev))
                    continue;

                if (binding.isPPtrCurve)
                    AnimationClipKeyEditing.AddOrReplaceObjectReferenceKey(animClip, binding, m_Time, Resources.EntityIdToObject(ev));
                else if (binding.isDiscreteCurve)
                    AnimationClipKeyEditing.AddOrReplaceKey(animClip, binding, m_Time, BitConverter.Int32BitsToSingle(iv));
                else
                    AnimationClipKeyEditing.AddOrReplaceKey(animClip, binding, m_Time, f);
            }

            RemoveBindingsFromCandidates(modifications);
            ResampleAnimation();
        }

        public void RemoveKey(PropertyModification[] modifications)
        {
            var animClip = animationClip;
            if (animClip == null)
                return;

            Undo.RegisterCompleteObjectUndo(animClip, "Remove Key");

            foreach (var binding in EnumerateBindings(modifications))
                AnimationClipKeyEditing.RemoveKeyAtTime(animClip, binding, m_Time);

            RemoveBindingsFromCandidates(modifications);
            ResampleAnimation();
        }

        public void RemoveCurve(PropertyModification[] modifications)
        {
            var animClip = animationClip;
            if (animClip == null)
                return;

            Undo.RegisterCompleteObjectUndo(animClip, "Remove Curve");

            foreach (var binding in EnumerateBindings(modifications))
            {
                if (binding.isPPtrCurve)
                    AnimationUtility.SetObjectReferenceCurve(animClip, binding, null);
                else
                    AnimationUtility.SetEditorCurve(animClip, binding, null);
            }

            RemoveBindingsFromCandidates(modifications);
            ResampleAnimation();
        }

        public void AddCandidateKeys()
        {
            // ProcessCandidates merges m_CandidateClip into the active clip at m_Time;
            // identical semantics to AnimationWindowControl's "Key All Modified".
            ProcessCandidates();
            ResampleAnimation();
        }

        public void AddAnimatedKeys()
        {
            var animClip = animationClip;
            if (animClip == null)
                return;

            Undo.RegisterCompleteObjectUndo(animClip, "Key All Animated");

            foreach (var binding in AnimationUtility.GetCurveBindings(animClip))
            {
                var curve = AnimationUtility.GetEditorCurve(animClip, binding);
                if (curve == null)
                    continue;
                var value = curve.Evaluate(m_Time);
                if (binding.isDiscreteCurve)
                    AnimationClipKeyEditing.AddOrReplaceDiscreteKey(curve, m_Time, value);
                else
                    AnimationClipKeyEditing.AddOrReplaceFloatKey(curve, m_Time, value);
                AnimationUtility.SetEditorCurve(animClip, binding, curve);
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(animClip))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(animClip, binding);
                if (keys == null || keys.Length == 0)
                    continue;
                var value = AnimationClipKeyEditing.EvaluateObjectReferenceCurve(keys, m_Time);
                var merged = AnimationClipKeyEditing.ReplaceObjectReferenceKey(keys, m_Time, value);
                AnimationUtility.SetObjectReferenceCurve(animClip, binding, merged);
            }

            ClearCandidates();
            ResampleAnimation();
        }

        const float TimeEpsilon = 0.00001f;

        public void GoToNextKeyframe(PropertyModification[] modifications)
        {
            if (!TryCollectKeyTimes(modifications, out var times))
                return;
            times.Sort();
            for (int i = 0; i < times.Count; i++)
            {
                if (times[i] > m_Time + TimeEpsilon)
                {
                    time = times[i];
                    return;
                }
            }
        }

        public void GoToPreviousKeyframe(PropertyModification[] modifications)
        {
            if (!TryCollectKeyTimes(modifications, out var times))
                return;
            times.Sort();
            for (int i = times.Count - 1; i >= 0; i--)
            {
                if (times[i] < m_Time - TimeEpsilon)
                {
                    time = times[i];
                    return;
                }
            }
        }

        // --- helpers ---

        // Channel-kind-driven binding shape mirrors ProcessConsumedModifications: PPtr rows are
        // object-reference curves, Int rows are discrete curves, Float rows are continuous curves.
        // All use the UIAnimationClip type and path="" because per-element clips are GameObject-less.
        IEnumerable<EditorCurveBinding> EnumerateBindings(PropertyModification[] modifications)
        {
            if (modifications == null)
                yield break;
            var uiClip = m_Selection?.uiAnimationClip;
            if (uiClip == null)
                yield break;

            for (int i = 0; i < modifications.Length; i++)
            {
                var mod = modifications[i];
                if (!ReferenceEquals(mod.target, uiClip))
                    continue;
                if (!UIAnimationBinderEditorExtensions.TryGetChannelKindForBinding(mod.propertyPath, out var kind))
                    continue;
                yield return ToEditorCurveBinding(mod.propertyPath, kind);
            }
        }

        static EditorCurveBinding ToEditorCurveBinding(string propertyName, UIAnimationBinder.AnimationChannelKind kind)
        {
            switch (kind)
            {
                case UIAnimationBinder.AnimationChannelKind.PPtr:
                    return EditorCurveBinding.PPtrCurve(string.Empty, typeof(UIAnimationClip), propertyName);
                case UIAnimationBinder.AnimationChannelKind.Int:
                    return EditorCurveBinding.DiscreteCurve(string.Empty, typeof(UIAnimationClip), propertyName);
                default:
                    return EditorCurveBinding.FloatCurve(string.Empty, typeof(UIAnimationClip), propertyName);
            }
        }

        void RemoveBindingsFromCandidates(PropertyModification[] modifications)
        {
            if (m_CandidateClip == null)
                return;

            bool removedAny = false;
            foreach (var binding in EnumerateBindings(modifications))
            {
                if (binding.isPPtrCurve)
                    AnimationUtility.SetObjectReferenceCurve(m_CandidateClip, binding, null);
                else
                    AnimationUtility.SetEditorCurve(m_CandidateClip, binding, null);
                removedAny = true;
            }

            // Stop candidate recording once the clip is fully drained so the candidate driver
            // doesn't hold state across an empty clip.
            if (removedAny && m_CandidateClip.empty)
                StopCandidateRecording();
        }

        bool TryCollectKeyTimes(PropertyModification[] modifications, out List<float> times)
        {
            times = m_KeyTimes ??= new List<float>();
            times.Clear();
            var animClip = animationClip;
            if (animClip == null)
                return false;
            foreach (var binding in EnumerateBindings(modifications))
            {
                if (binding.isPPtrCurve)
                {
                    var keys = AnimationUtility.GetObjectReferenceCurve(animClip, binding);
                    if (keys == null)
                        continue;
                    for (int i = 0; i < keys.Length; i++)
                        times.Add(keys[i].time);
                }
                else
                {
                    var curve = AnimationUtility.GetEditorCurve(animClip, binding);
                    if (curve == null)
                        continue;
                    for (int i = 0; i < curve.length; i++)
                        times.Add(curve[i].time);
                }
            }
            return times.Count > 0;
        }
    }
}
