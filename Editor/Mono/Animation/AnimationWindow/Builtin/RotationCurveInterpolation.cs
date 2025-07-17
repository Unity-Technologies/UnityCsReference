// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

using Mode = UnityEditor.RotationCurveInterpolation.Mode;

namespace UnityEditor.AnimationWindowBuiltin
{
    class RotationCurveInterpolation
    {
        public struct State
        {
            public bool allAreNonBaked;
            public bool allAreBaked;
            public bool allAreRaw;
            public bool allAreRotations;
        }

        public static char[] kPostFix = { 'x', 'y', 'z', 'w' };

        static char ExtractComponentCharacter(string name)
        {
            return name[name.Length - 1];
        }

        internal static EditorCurveBinding[] GenerateTransformCurveBindingArray(string path, string property, Type type, int count)
        {
            EditorCurveBinding[] bindings = new EditorCurveBinding[count];
            for (int i = 0; i < count; i++)
                bindings[i] = EditorCurveBinding.FloatCurve(path, type, property + kPostFix[i]);
            return bindings;
        }

        static public EditorCurveBinding[] RemapAnimationBindingForAddKey(EditorCurveBinding binding, AnimationClip clip)
        {
            if (!AnimationWindowUtility.IsTransformType(binding.type))
            {
                return null;
            }
            if (binding.propertyName.StartsWith("m_LocalRotation"))
            {
                return SelectRotationBindingForAddKey(binding, clip);
            }
            else if (binding.propertyName.StartsWith("m_LocalPosition."))
            {
                if (binding.type == typeof(Transform))
                    return GenerateTransformCurveBindingArray(binding.path, "m_LocalPosition.", binding.type, 3);
                else
                    return null;
            }
            else if (binding.propertyName.StartsWith("m_LocalScale."))
                return GenerateTransformCurveBindingArray(binding.path, "m_LocalScale.", binding.type, 3);
            else
                return null;
        }

        static EditorCurveBinding[] SelectRotationBindingForAddKey(EditorCurveBinding binding, AnimationClip clip)
        {
            EditorCurveBinding testBinding = binding;
            testBinding.propertyName = "localEulerAnglesBaked.x";
            if (AnimationUtility.GetEditorCurve(clip, testBinding) != null)
                return GenerateTransformCurveBindingArray(binding.path, "localEulerAnglesBaked.", binding.type, 3);
            else
            {
                testBinding.propertyName = "localEulerAngles.x";
                if (AnimationUtility.GetEditorCurve(clip, testBinding) != null)
                {
                    return GenerateTransformCurveBindingArray(binding.path, "localEulerAngles.", binding.type, 3);
                }
                else
                {
                    testBinding.propertyName = "localEulerAnglesRaw.x";
                    if (clip.legacy && AnimationUtility.GetEditorCurve(clip, testBinding) == null)
                        return GenerateTransformCurveBindingArray(binding.path, "localEulerAnglesBaked.", binding.type, 3);

                    return GenerateTransformCurveBindingArray(binding.path, "localEulerAnglesRaw.", binding.type, 3);
                }
            }
        }

        static public EditorCurveBinding RemapAnimationBindingForRotationCurves(EditorCurveBinding curveBinding, AnimationClip clip)
        {
            if (!AnimationWindowUtility.IsTransformType(curveBinding.type))
                return curveBinding;

            // Convert rotation binding to valid editor curve binding.
            // Only a single rotation binding (RawEuler, Baked or NonBaked) should be allowed in one clip.
            // RawQuaternions should be converted to appropriate euler bindings if available.
            Mode mode = UnityEditor.RotationCurveInterpolation.GetModeFromCurveData(curveBinding);
            if (mode != Mode.Undefined)
            {
                string suffix = curveBinding.propertyName.Split('.')[1];

                EditorCurveBinding newBinding = curveBinding;

                if (mode != Mode.NonBaked)
                {
                    newBinding.propertyName = UnityEditor.RotationCurveInterpolation.GetPrefixForInterpolation(Mode.NonBaked) + "." + suffix;
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, newBinding);
                    if (curve != null)
                        return newBinding;
                }

                if (mode != Mode.Baked)
                {
                    newBinding.propertyName = UnityEditor.RotationCurveInterpolation.GetPrefixForInterpolation(Mode.Baked) + "." + suffix;
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, newBinding);
                    if (curve != null)
                        return newBinding;
                }

                if (mode != Mode.RawEuler)
                {
                    newBinding.propertyName = UnityEditor.RotationCurveInterpolation.GetPrefixForInterpolation(Mode.RawEuler) + "." + suffix;
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, newBinding);
                    if (curve != null)
                        return newBinding;
                }

                return curveBinding;
            }
            else
                return curveBinding;
        }

        internal static void SetInterpolation(
            AnimationClip clip,
            IEnumerable<EditorCurveBinding> curveBindings,
            Mode newInterpolationMode)
        {
            if (clip.legacy && newInterpolationMode == Mode.RawEuler)
            {
                UnityEngine.Debug.LogWarning("Warning, Euler Angles interpolation mode is not fully supported for Legacy animation clips. If you mix clips using Euler Angles interpolation with clips using other interpolation modes (using Animation.CrossFade, Animation.Blend or other methods), you will get erroneous results. Use with caution.", clip);
            }
            List<EditorCurveBinding> newCurvesBindings = new List<EditorCurveBinding>();
            List<AnimationCurve> newCurveDatas = new List<AnimationCurve>();
            List<EditorCurveBinding> oldCurvesBindings = new List<EditorCurveBinding>();

            foreach (EditorCurveBinding curveBinding in curveBindings)
            {
                Mode currentMode = UnityEditor.RotationCurveInterpolation.GetModeFromCurveData(curveBinding);

                if (currentMode == Mode.Undefined)
                    continue;

                if (currentMode == Mode.RawQuaternions)
                {
                    UnityEngine.Debug.LogWarning("Can't convert quaternion curve: " + curveBinding.propertyName);
                    continue;
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, curveBinding);

                if (curve == null)
                    continue;

                string newPropertyPath = UnityEditor.RotationCurveInterpolation.GetPrefixForInterpolation(newInterpolationMode) + '.' + ExtractComponentCharacter(curveBinding.propertyName);

                EditorCurveBinding newBinding = new EditorCurveBinding();
                newBinding.propertyName = newPropertyPath;
                newBinding.type = curveBinding.type;
                newBinding.path = curveBinding.path;
                newCurvesBindings.Add(newBinding);
                newCurveDatas.Add(curve);

                EditorCurveBinding removeCurve = new EditorCurveBinding();
                removeCurve.propertyName = curveBinding.propertyName;
                removeCurve.type = curveBinding.type;
                removeCurve.path = curveBinding.path;
                oldCurvesBindings.Add(removeCurve);
            }

            Undo.RegisterCompleteObjectUndo(clip, L10n.Tr("Rotation Interpolation"));

            foreach (EditorCurveBinding binding in oldCurvesBindings)
                AnimationUtility.SetEditorCurve(clip, binding, null);

            foreach (EditorCurveBinding binding in newCurvesBindings)
                AnimationUtility.SetEditorCurve(clip, binding, newCurveDatas[newCurvesBindings.IndexOf(binding)]);
        }

        internal static EditorCurveBinding[] ConvertRotationPropertiesToDefaultInterpolation(AnimationClip clip, ReadOnlySpan<EditorCurveBinding> selection)
        {
            var mode = clip.legacy ? Mode.Baked : Mode.RawEuler;
            return UnityEditor.RotationCurveInterpolation.ConvertRotationPropertiesToInterpolationType(selection, mode);
        }
    }
}
