// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor.AnimationWindowBuiltin
{
    class AnimationWindowClip : IAnimationWindowClip, IEquatable<AnimationWindowClip>
    {
        AnimationClip m_Clip;
        GameObject m_RootGameObject;

        public string name => m_Clip.name;
        public int id => m_Clip.GetInstanceID();
        public bool isValid => m_Clip != null;
        public bool isReadOnly => m_Clip.hideFlags == HideFlags.NotEditable;

        public float frameRate
        {
            get => m_Clip != null ? m_Clip.frameRate : 60f;
            set
            {
                if (m_Clip != null)
                    m_Clip.frameRate = value;
            }
        }

        public float length => m_Clip != null ? m_Clip.length : 0f;
        public bool isLooping => m_Clip != null ? m_Clip.isLooping : false;
        public AnimationClip animationClip => m_Clip;

        public AnimationWindowClip(AnimationClip clip)
        {
            m_Clip = clip;
            m_RootGameObject = null;
        }

        public AnimationWindowClip(AnimationClip clip, GameObject rootGameObject)
        {
            m_Clip = clip;
            m_RootGameObject = rootGameObject;
        }

        public void LoadKeyframes(AnimationWindowCurve animationWindowCurve, List<AnimationWindowKeyframe> keyframes)
        {
            keyframes.Clear();

            if (m_Clip == null)
                return;

            if (!animationWindowCurve.isPPtrCurve)
            {
                var curve = AnimationUtility.GetEditorCurve(m_Clip, animationWindowCurve.binding);
                if (curve != null)
                {
                    animationWindowCurve.FromAnimationCurve(curve);
                }
            }
            else
            {
                ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(m_Clip, animationWindowCurve.binding);
                if (curve != null)
                {
                    animationWindowCurve.FromObjectReferenceCurve(curve);
                }
            }
        }

        public void LoadCurves(List<AnimationWindowCurve> curves)
        {
            if (m_Clip == null)
                return;

            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(m_Clip);
            EditorCurveBinding[] objectCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(m_Clip);

            List<AnimationWindowCurve> transformCurves = new List<AnimationWindowCurve>();

            for (int i = 0; i < curveBindings.Length; i++)
            {
                var curveBinding = curveBindings[i];
                if (AnimationWindowUtility.ShouldShowAnimationWindowCurve(curveBinding))
                {
                    AnimationWindowCurve curve = new AnimationWindowCurve(
                        this,
                        RotationCurveInterpolation.RemapAnimationBindingForRotationCurves(curveBinding, m_Clip),
                        GetValueType(curveBinding));
                    curves.Add(curve);

                    if (AnimationWindowUtility.IsActualTransformCurve(curveBinding))
                    {
                        transformCurves.Add(curve);
                    }
                }
            }

            foreach (EditorCurveBinding curveBinding in objectCurveBindings)
            {
                AnimationWindowCurve curve = new AnimationWindowCurve(this, curveBinding, GetValueType(curveBinding));
                curves.Add(curve);
            }

            transformCurves.Sort();
            if (transformCurves.Count > 0)
            {
                FillInMissingTransformCurves(m_Clip, transformCurves, ref curves);
            }

            // Curves need to be sorted with path/type/property name so it's possible to construct hierarchy from them
            // Sorting logic in AnimationWindowCurve.CompareTo()
            curves.Sort();
        }

        private void FillInMissingTransformCurves(AnimationClip animationClip, List<AnimationWindowCurve> transformCurves, ref List<AnimationWindowCurve> curvesCache)
        {
            EditorCurveBinding lastBinding = transformCurves[0].binding;
            var propertyGroup = new EditorCurveBinding ? [3];
            string propertyGroupName;
            foreach (var transformCurve in transformCurves)
            {
                var transformBinding = transformCurve.binding;
                //if it's a new property group
                if (transformBinding.path != lastBinding.path
                    || AnimationWindowUtility.GetPropertyGroupName(transformBinding.propertyName) != AnimationWindowUtility.GetPropertyGroupName(lastBinding.propertyName))
                {
                    propertyGroupName = AnimationWindowUtility.GetPropertyGroupName(lastBinding.propertyName);

                    FillPropertyGroup(animationClip, ref propertyGroup, lastBinding, propertyGroupName, ref curvesCache);

                    lastBinding = transformBinding;

                    propertyGroup = new EditorCurveBinding ? [3];
                }

                AssignBindingToRightSlot(transformBinding, ref propertyGroup);
            }
            FillPropertyGroup(animationClip, ref propertyGroup, lastBinding, AnimationWindowUtility.GetPropertyGroupName(lastBinding.propertyName), ref curvesCache);
        }

        private void FillPropertyGroup(AnimationClip animationClip, ref EditorCurveBinding?[] propertyGroup, EditorCurveBinding lastBinding, string propertyGroupName, ref List<AnimationWindowCurve> curvesCache)
        {
            var newBinding = lastBinding;
            newBinding.isPhantom = true;
            if (!propertyGroup[0].HasValue)
            {
                newBinding.propertyName = propertyGroupName + ".x";
                AnimationWindowCurve curve = new AnimationWindowCurve(this, newBinding, GetValueType(newBinding));
                curvesCache.Add(curve);
            }

            if (!propertyGroup[1].HasValue)
            {
                newBinding.propertyName = propertyGroupName + ".y";
                AnimationWindowCurve curve = new AnimationWindowCurve(this, newBinding, GetValueType(newBinding));
                curvesCache.Add(curve);
            }

            if (!propertyGroup[2].HasValue)
            {
                newBinding.propertyName = propertyGroupName + ".z";
                AnimationWindowCurve curve = new AnimationWindowCurve(this, newBinding, GetValueType(newBinding));
                curvesCache.Add(curve);
            }
        }

        private void AssignBindingToRightSlot(EditorCurveBinding transformBinding, ref EditorCurveBinding?[] propertyGroup)
        {
            if (transformBinding.propertyName.EndsWith(".x"))
            {
                propertyGroup[0] = transformBinding;
            }
            else if (transformBinding.propertyName.EndsWith(".y"))
            {
                propertyGroup[1] = transformBinding;
            }
            else if (transformBinding.propertyName.EndsWith(".z"))
            {
                propertyGroup[2] = transformBinding;
            }
        }

        public void CreateCurves(ReadOnlySpan<EditorCurveBinding> bindings, List<AnimationWindowCurve> curves)
        {
            if (m_Clip == null)
                return;

            var properties = RotationCurveInterpolation.ConvertRotationPropertiesToDefaultInterpolation(m_Clip, bindings);

            if (properties.Length == 0)
                return;

            curves.Capacity = properties.Length;
            foreach (EditorCurveBinding prop in properties)
            {
                var valueType = GetValueType(prop);
                curves.Add(new AnimationWindowCurve(this, prop, valueType));
            }
        }

        public void SaveCurve(AnimationWindowCurve curve, string undoLabel)
        {
            if (m_Clip == null)
                return;

            Undo.RegisterCompleteObjectUndo(m_Clip, undoLabel);

            if (curve.isPPtrCurve)
            {
                ObjectReferenceKeyframe[] objectCurve = curve.ToObjectCurve();

                if (objectCurve.Length == 0)
                    objectCurve = null;

                AnimationUtility.SetObjectReferenceCurve(m_Clip, curve.binding, objectCurve);
            }
            else
            {
                AnimationCurve animationCurve = curve.ToAnimationCurve();

                if (animationCurve.keys.Length == 0)
                    animationCurve = null;
                else
                    AnimationUtility.UpdateTangentsFromMode(animationCurve);

                AnimationUtility.SetEditorCurve(m_Clip, curve.binding, animationCurve);
            }
        }

        public void SaveCurves(IEnumerable<AnimationWindowCurve> curves, string undoLabel)
        {
            if (m_Clip == null)
                return;

            Undo.RegisterCompleteObjectUndo(m_Clip, undoLabel);
            using var batchOperations = new AnimationClipBatchOperations(m_Clip);

            foreach (AnimationWindowCurve curve in curves)
            {
                if (curve.isPPtrCurve)
                {
                    ObjectReferenceKeyframe[] objectCurve = curve.ToObjectCurve();

                    if (objectCurve.Length == 0)
                        objectCurve = null;

                    batchOperations.SetObjectReferenceCurve(curve.binding, objectCurve);
                }
                else
                {
                    AnimationCurve animationCurve = curve.ToAnimationCurve();

                    if (animationCurve.keys.Length == 0)
                        animationCurve = null;
                    else
                        AnimationUtility.UpdateTangentsFromMode(animationCurve);

                    batchOperations.SetEditorCurve(curve.binding, animationCurve);
                }
            }
        }

        public void SaveCurves(IEnumerable<CurveWrapper> curveWrappers, string undoLabel)
        {
            if (m_Clip == null)
                return;

            Undo.RegisterCompleteObjectUndo(m_Clip, undoLabel);

            var bindings = new List<EditorCurveBinding>();
            var curves = new List<AnimationCurve>();

            foreach (var curveWrapper in curveWrappers)
            {
                if (curveWrapper.changed)
                {
                    bindings.Add(curveWrapper.binding);
                    curves.Add(curveWrapper.curve.length > 0 ? curveWrapper.curve : null);

                    curveWrapper.changed = false;
                }
            }

            if (curves.Count > 0)
            {
                using var batchOperations = new AnimationClipBatchOperations(m_Clip);
                for (int i = 0; i < curves.Count; ++i)
                {
                    batchOperations.SetEditorCurve(bindings[i], curves[i]);
                }
            }
        }

        public void RemoveCurves(IEnumerable<EditorCurveBinding> bindings, string undoLabel)
        {
            if (m_Clip == null)
                return;

            Undo.RegisterCompleteObjectUndo(m_Clip, undoLabel);
            using var batchOperations = new AnimationClipBatchOperations(m_Clip);

            foreach (var binding in bindings)
            {
                if (binding.isPPtrCurve)
                    batchOperations.SetObjectReferenceCurve(binding, null);
                else
                    batchOperations.SetEditorCurve(binding, null);
            }
        }

        public void RemoveOverridenCurves(IEnumerable<EditorCurveBinding> bindings, string undoLabel)
        {
            throw new NotImplementedException();
        }

        public void RenameCurves(IEnumerable<EditorCurveBinding> bindings, IEnumerable<string> newPaths, string undoLabel)
        {
            if (m_Clip == null)
                return;

            Undo.RegisterCompleteObjectUndo(m_Clip, undoLabel);
            using var batchOperations = new AnimationClipBatchOperations(m_Clip);

            var bindingEnumerator = bindings.GetEnumerator();
            var pathEnumerator = newPaths.GetEnumerator();

            while (bindingEnumerator.MoveNext() && pathEnumerator.MoveNext())
            {
                var binding = bindingEnumerator.Current;
                var newPath = pathEnumerator.Current;

                var newBinding = binding;
                newBinding.path = newPath;

                if (binding.isPPtrCurve)
                {
                    var curve = AnimationUtility.GetObjectReferenceCurve(m_Clip, binding);
                    batchOperations.SetObjectReferenceCurve(binding, null);
                    batchOperations.SetObjectReferenceCurve(newBinding, curve);
                }
                else
                {
                    var curve = AnimationUtility.GetEditorCurve(m_Clip, binding);
                    batchOperations.SetEditorCurve(binding, null);
                    batchOperations.SetEditorCurve(newBinding, curve);
                }
            }
        }

        public void SetInterpolations(IEnumerable<EditorCurveBinding> bindings, UnityEditor.RotationCurveInterpolation.Mode interpolation, string undoLabel)
        {
            if (m_Clip == null)
                return;

            Undo.RegisterCompleteObjectUndo(m_Clip, undoLabel);
            AnimationWindowBuiltin.RotationCurveInterpolation.SetInterpolation(m_Clip, bindings, interpolation);
        }

        public EditorCurveBinding[] RemapAnimationBindingForAddKey(EditorCurveBinding binding) =>
            RotationCurveInterpolation.RemapAnimationBindingForAddKey(binding, m_Clip);

        public bool IsCurveCreated(EditorCurveBinding binding)
        {
            if (m_Clip == null)
                return false;

            if (binding.isPPtrCurve)
            {
                return AnimationUtility.GetObjectReferenceCurve(m_Clip, binding) != null;
            }
            else
            {
                // For RectTransform.position we only want .z
                if (IsRectTransformPosition(binding))
                    binding.propertyName = binding.propertyName.Replace(".x", ".z").Replace(".y", ".z");
                if (IsRotationCurve(binding))
                {
                    return AnimationUtility.GetEditorCurve(m_Clip, binding) != null || HasOtherRotationCurve(m_Clip, binding);
                }

                return AnimationUtility.GetEditorCurve(m_Clip, binding) != null;
            }
        }

        static bool HasOtherRotationCurve(AnimationClip clip, EditorCurveBinding rotationBinding)
        {
            if (rotationBinding.propertyName.StartsWith("m_LocalRotation"))
            {
                EditorCurveBinding x = rotationBinding;
                EditorCurveBinding y = rotationBinding;
                EditorCurveBinding z = rotationBinding;
                x.propertyName = "localEulerAnglesRaw.x";
                y.propertyName = "localEulerAnglesRaw.y";
                z.propertyName = "localEulerAnglesRaw.z";

                return AnimationUtility.GetEditorCurve(clip, x) != null ||
                    AnimationUtility.GetEditorCurve(clip, y) != null ||
                    AnimationUtility.GetEditorCurve(clip, z) != null;
            }
            else
            {
                EditorCurveBinding x = rotationBinding;
                EditorCurveBinding y = rotationBinding;
                EditorCurveBinding z = rotationBinding;
                EditorCurveBinding w = rotationBinding;

                x.propertyName = "m_LocalRotation.x";
                y.propertyName = "m_LocalRotation.y";
                z.propertyName = "m_LocalRotation.z";
                w.propertyName = "m_LocalRotation.w";

                return AnimationUtility.GetEditorCurve(clip, x) != null ||
                    AnimationUtility.GetEditorCurve(clip, y) != null ||
                    AnimationUtility.GetEditorCurve(clip, z) != null ||
                    AnimationUtility.GetEditorCurve(clip, w) != null;
            }
        }

        static bool IsRotationCurve(EditorCurveBinding curveBinding)
        {
            string propertyName = AnimationWindowUtility.GetPropertyGroupName(curveBinding.propertyName);
            return propertyName == "m_LocalRotation" || propertyName == "localEulerAnglesRaw";
        }

        static bool IsRectTransformPosition(EditorCurveBinding curveBinding)
        {
            return curveBinding.type == typeof(RectTransform) && AnimationWindowUtility.GetPropertyGroupName(curveBinding.propertyName) == "m_LocalPosition";
        }

        Type GetValueType(EditorCurveBinding binding)
        {
            if (m_RootGameObject != null)
            {
                return AnimationUtility.GetEditorCurveValueType(m_RootGameObject, binding);
            }
            else
            {
                if (binding.isPPtrCurve)
                {
                    // Cannot extract type of PPtrCurve.
                    return null;
                }
                else
                {
                    // Cannot extract type of AnimationCurve.  Default to float.
                    return typeof(float);
                }
            }
        }

        public bool Equals(IAnimationWindowClip other) => Equals(other as AnimationWindowClip);

        public bool Equals(AnimationWindowClip other) => animationClip == other?.animationClip;
    }
}
