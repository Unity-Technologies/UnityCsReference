// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    internal class AnimationRecording
    {
        const string kLocalRotation = "m_LocalRotation";
        const string kLocalEulerAnglesHint = "m_LocalEulerAnglesHint";

        static bool HasAnyRecordableModifications(GameObject root, UndoPropertyModification[] modifications)
        {
            for (int i = 0; i < modifications.Length; i++)
            {
                EditorCurveBinding tempBinding;
                if (AnimationUtility.PropertyModificationToEditorCurveBinding(modifications[i].previousValue, root, out tempBinding) != null)
                    return true;
            }
            return false;
        }

        static PropertyModification FindPropertyModification(GameObject root, UndoPropertyModification[] modifications, EditorCurveBinding binding)
        {
            for (int i = 0; i < modifications.Length; i++)
            {
                EditorCurveBinding tempBinding;
                AnimationUtility.PropertyModificationToEditorCurveBinding(modifications[i].previousValue, root, out tempBinding);
                if (tempBinding == binding)
                    return modifications[i].previousValue;
            }

            return null;
        }

        static PropertyModification CreateDummyPropertyModification(GameObject root, PropertyModification baseProperty, EditorCurveBinding binding)
        {
            var property = new PropertyModification();
            property.target = baseProperty.target;
            property.propertyPath = binding.propertyName;

            object currentValue = CurveBindingUtility.GetCurrentValue(root, binding);
            if (binding.isPPtrCurve)
                property.objectReference = (Object)currentValue;
            else
                property.value = ((float)currentValue).ToString();

            return property;
        }

        static private UndoPropertyModification[] FilterModifications(IAnimationRecordingState state, ref UndoPropertyModification[] modifications)
        {
            AnimationClip clip = state.activeAnimationClip;
            GameObject rootGameObject = state.activeRootGameObject;

            List<UndoPropertyModification> discardedModifications = new List<UndoPropertyModification>();
            List<UndoPropertyModification> outModifications = new List<UndoPropertyModification>();
            for (int i = 0; i < modifications.Length; ++i)
            {
                UndoPropertyModification modification = modifications[i];
                PropertyModification prop = modification.previousValue;

                if (state.DiscardModification(prop))
                {
                    discardedModifications.Add(modification);
                    continue;
                }

                var binding = new EditorCurveBinding();
                if (AnimationUtility.PropertyModificationToEditorCurveBinding(prop, rootGameObject, out binding) != null)
                    outModifications.Add(modification);
                else
                    discardedModifications.Add(modification);
            }

            if (discardedModifications.Count > 0)
                modifications = outModifications.ToArray();

            return discardedModifications.ToArray();
        }

        internal class RotationModification
        {
            public UndoPropertyModification x;
            public UndoPropertyModification y;
            public UndoPropertyModification z;
            public UndoPropertyModification w;

            public UndoPropertyModification lastQuatModification;

            public bool useEuler = false;
            public UndoPropertyModification eulerX;
            public UndoPropertyModification eulerY;
            public UndoPropertyModification eulerZ;
        }

        static private void CollectRotationModifications(IAnimationRecordingState state, ref UndoPropertyModification[] modifications,
            ref System.Collections.Generic.Dictionary<object, RotationModification> rotationModifications)
        {
            List<UndoPropertyModification> outModifs = new List<UndoPropertyModification>();
            foreach (var modification in modifications)
            {
                PropertyModification prop = modification.previousValue;
                if (!(prop.target is Transform))
                {
                    outModifs.Add(modification);
                    continue;
                }

                EditorCurveBinding binding = new EditorCurveBinding();
                AnimationUtility.PropertyModificationToEditorCurveBinding(prop, state.activeRootGameObject, out binding);
                if (binding.propertyName.StartsWith(kLocalRotation))
                {
                    RotationModification rotationModification;

                    if (!rotationModifications.TryGetValue(prop.target, out rotationModification))
                    {
                        rotationModification = new RotationModification();
                        rotationModifications[prop.target] = rotationModification;
                    }

                    if (binding.propertyName.EndsWith("x"))
                        rotationModification.x = modification;
                    else if (binding.propertyName.EndsWith("y"))
                        rotationModification.y = modification;
                    else if (binding.propertyName.EndsWith("z"))
                        rotationModification.z = modification;
                    else if (binding.propertyName.EndsWith("w"))
                        rotationModification.w = modification;

                    rotationModification.lastQuatModification = modification;
                }
                else if (prop.propertyPath.StartsWith(kLocalEulerAnglesHint))
                {
                    RotationModification rotationModification;

                    if (!rotationModifications.TryGetValue(prop.target, out rotationModification))
                    {
                        rotationModification = new RotationModification();
                        rotationModifications[prop.target] = rotationModification;
                    }

                    rotationModification.useEuler = true;

                    if (prop.propertyPath.EndsWith("x"))
                        rotationModification.eulerX = modification;
                    else if (prop.propertyPath.EndsWith("y"))
                        rotationModification.eulerY = modification;
                    else if (prop.propertyPath.EndsWith("z"))
                        rotationModification.eulerZ = modification;
                }
                else
                {
                    outModifs.Add(modification);
                }
            }

            if (rotationModifications.Count > 0)
            {
                modifications = outModifs.ToArray();
            }
        }

        static private void DiscardRotationModification(RotationModification rotationModification, ref List<UndoPropertyModification> discardedModifications)
        {
            if (rotationModification.x.currentValue != null)
                discardedModifications.Add(rotationModification.x);
            if (rotationModification.y.currentValue != null)
                discardedModifications.Add(rotationModification.y);
            if (rotationModification.z.currentValue != null)
                discardedModifications.Add(rotationModification.z);
            if (rotationModification.w.currentValue != null)
                discardedModifications.Add(rotationModification.w);

            if (rotationModification.eulerX.currentValue != null)
                discardedModifications.Add(rotationModification.eulerX);
            if (rotationModification.eulerY.currentValue != null)
                discardedModifications.Add(rotationModification.eulerY);
            if (rotationModification.eulerZ.currentValue != null)
                discardedModifications.Add(rotationModification.eulerZ);
        }

        static private UndoPropertyModification[] FilterRotationModifications(IAnimationRecordingState state, ref Dictionary<object, RotationModification> rotationModifications)
        {
            AnimationClip clip = state.activeAnimationClip;
            GameObject rootGameObject = state.activeRootGameObject;

            List<object> itemsToRemove = new List<object>();

            List<UndoPropertyModification> discardedModifications = new List<UndoPropertyModification>();
            foreach (KeyValuePair<object, RotationModification> item in rotationModifications)
            {
                RotationModification m = item.Value;

                if (state.DiscardModification(m.lastQuatModification.currentValue))
                {
                    DiscardRotationModification(m, ref discardedModifications);
                    itemsToRemove.Add(item.Key);
                    continue;
                }

                EditorCurveBinding binding = new EditorCurveBinding();
                Type type = AnimationUtility.PropertyModificationToEditorCurveBinding(m.lastQuatModification.currentValue, rootGameObject, out binding);
                if (type == null)
                {
                    DiscardRotationModification(m, ref discardedModifications);
                    itemsToRemove.Add(item.Key);
                }
            }

            foreach (var key in itemsToRemove)
            {
                rotationModifications.Remove(key);
            }

            return discardedModifications.ToArray();
        }

        static private void AddRotationPropertyModification(IAnimationRecordingState state, EditorCurveBinding baseBinding, UndoPropertyModification modification)
        {
            if (modification.previousValue == null)
                return;

            // case 817356. Reuse baseBinding as basis for rotation binding.
            // This is needed to register valid bindings for m_LocalEulerAnglesHint
            // that cannot be converted to EditorCurveBinding otherwise.
            EditorCurveBinding binding = baseBinding;
            binding.propertyName = modification.previousValue.propertyPath;

            state.AddPropertyModification(binding, modification.previousValue, modification.keepPrefabOverride);
        }

        static private UndoPropertyModification[] ProcessRotationModifications(IAnimationRecordingState state, ref UndoPropertyModification[] modifications)
        {
            System.Collections.Generic.Dictionary<object, RotationModification> rotationModifications = new Dictionary<object, RotationModification>();

            //Strip out the rotation modifications from the rest
            CollectRotationModifications(state, ref modifications, ref rotationModifications);

            UndoPropertyModification[] discardedModifications = FilterRotationModifications(state, ref rotationModifications);

            foreach (KeyValuePair<object, RotationModification> item in rotationModifications)
            {
                RotationModification m = item.Value;
                Transform target = item.Key as Transform;

                if (target == null)
                    continue;

                EditorCurveBinding binding = new EditorCurveBinding();
                Type type = AnimationUtility.PropertyModificationToEditorCurveBinding(m.lastQuatModification.currentValue, state.activeRootGameObject, out binding);
                if (type == null)
                    continue;

                AddRotationPropertyModification(state, binding, m.x);
                AddRotationPropertyModification(state, binding, m.y);
                AddRotationPropertyModification(state, binding, m.z);
                AddRotationPropertyModification(state, binding, m.w);

                Quaternion previousValue = target.localRotation;
                Quaternion currentValue = target.localRotation;

                object x, y, z, w;
                if (ValueFromPropertyModification(m.x.previousValue, binding, out x))
                    previousValue.x = (float)x;
                if (ValueFromPropertyModification(m.y.previousValue, binding, out y))
                    previousValue.y = (float)y;
                if (ValueFromPropertyModification(m.z.previousValue, binding, out z))
                    previousValue.z = (float)z;
                if (ValueFromPropertyModification(m.w.previousValue, binding, out w))
                    previousValue.w = (float)w;

                if (ValueFromPropertyModification(m.x.currentValue, binding, out x))
                    currentValue.x = (float)x;
                if (ValueFromPropertyModification(m.y.currentValue, binding, out y))
                    currentValue.y = (float)y;
                if (ValueFromPropertyModification(m.z.currentValue, binding, out z))
                    currentValue.z = (float)z;
                if (ValueFromPropertyModification(m.w.currentValue, binding, out w))
                    currentValue.w = (float)w;

                // Favour euler hints if one or more exist on any axis.
                if (m.useEuler)
                {
                    AddRotationPropertyModification(state, binding, m.eulerX);
                    AddRotationPropertyModification(state, binding, m.eulerY);
                    AddRotationPropertyModification(state, binding, m.eulerZ);

                    Vector3 previousEulerAngles = target.GetLocalEulerAngles(RotationOrder.OrderZXY);
                    Vector3 currentEulerAngles = previousEulerAngles;

                    object eulerX, eulerY, eulerZ;
                    if (ValueFromPropertyModification(m.eulerX.previousValue, binding, out eulerX))
                        previousEulerAngles.x = (float)eulerX;
                    if (ValueFromPropertyModification(m.eulerY.previousValue, binding, out eulerY))
                        previousEulerAngles.y = (float)eulerY;
                    if (ValueFromPropertyModification(m.eulerZ.previousValue, binding, out eulerZ))
                        previousEulerAngles.z = (float)eulerZ;

                    if (ValueFromPropertyModification(m.eulerX.currentValue, binding, out eulerX))
                        currentEulerAngles.x = (float)eulerX;
                    if (ValueFromPropertyModification(m.eulerY.currentValue, binding, out eulerY))
                        currentEulerAngles.y = (float)eulerY;
                    if (ValueFromPropertyModification(m.eulerZ.currentValue, binding, out eulerZ))
                        currentEulerAngles.z = (float)eulerZ;

                    // Fallback to quaternion euler values if euler hint and quaternion are out of sync.
                    previousEulerAngles = AnimationUtility.GetClosestEuler(previousValue, previousEulerAngles, RotationOrder.OrderZXY);
                    currentEulerAngles = AnimationUtility.GetClosestEuler(currentValue, currentEulerAngles, RotationOrder.OrderZXY);

                    AddRotationKey(state, binding, type, previousEulerAngles, currentEulerAngles);
                }
                else
                {
                    Vector3 eulerAngles = target.GetLocalEulerAngles(RotationOrder.OrderZXY);

                    Vector3 previousEulerAngles = AnimationUtility.GetClosestEuler(previousValue, eulerAngles, RotationOrder.OrderZXY);
                    Vector3 currentEulerAngles = AnimationUtility.GetClosestEuler(currentValue, eulerAngles, RotationOrder.OrderZXY);

                    AddRotationKey(state, binding, type, previousEulerAngles, currentEulerAngles);
                }
            }

            return discardedModifications;
        }

        static public UndoPropertyModification[] ProcessModifications(IAnimationRecordingState state, UndoPropertyModification[] modifications)
        {
            AnimationClip clip = state.activeAnimationClip;
            GameObject root = state.activeRootGameObject;
            Animator animator = root.GetComponent<Animator>();

            UndoPropertyModification[] discardedModifications = FilterModifications(state, ref modifications);

            // Record modified properties
            for (int i = 0; i < modifications.Length; i++)
            {
                EditorCurveBinding binding = new EditorCurveBinding();

                PropertyModification prop = modifications[i].previousValue;
                Type type = AnimationUtility.PropertyModificationToEditorCurveBinding(prop, root, out binding);
                if (type != null)
                {
                    // TODO@mecanim keyframing of transform driven by an animator is disabled until we can figure out
                    // how to rebind partially a controller at each sampling.
                    if (animator != null && animator.isHuman && binding.type == typeof(Transform)
                        && animator.gameObject.transform != prop.target && animator.IsBoneTransform(prop.target as Transform))
                    {
                        Debug.LogWarning("Keyframing for humanoid rig is not supported!", prop.target as Transform);
                        continue;
                    }

                    EditorCurveBinding[] additionalBindings = RotationCurveInterpolation.RemapAnimationBindingForAddKey(binding, clip);
                    if (additionalBindings != null)
                    {
                        for (int a = 0; a < additionalBindings.Length; a++)
                        {
                            PropertyModification additionalProperty = FindPropertyModification(root, modifications, additionalBindings[a]);
                            if (additionalProperty == null)
                                additionalProperty = CreateDummyPropertyModification(root, prop, additionalBindings[a]);

                            state.AddPropertyModification(additionalBindings[a], additionalProperty, modifications[i].keepPrefabOverride);
                            AddKey(state, additionalBindings[a], type, additionalProperty);
                        }
                    }
                    else
                    {
                        state.AddPropertyModification(binding, prop, modifications[i].keepPrefabOverride);
                        AddKey(state, binding, type, prop);
                    }
                }
            }

            return discardedModifications;
        }

        static public UndoPropertyModification[] Process(IAnimationRecordingState state, UndoPropertyModification[] modifications)
        {
            GameObject root = state.activeRootGameObject;
            if (root == null)
                return modifications;

            // Fast path detect that nothing recordable has changed.
            if (!HasAnyRecordableModifications(root, modifications))
                return modifications;

            // Process quaternion rotation modifications first.
            UndoPropertyModification[] discardedRotationModifications = ProcessRotationModifications(state, ref modifications);
            // Process what's remaining.
            UndoPropertyModification[] discardedModifications = ProcessModifications(state, modifications);

            return discardedRotationModifications.Concat(discardedModifications).ToArray();
        }

        static bool ValueFromPropertyModification(PropertyModification modification, EditorCurveBinding binding, out object outObject)
        {
            if (modification == null)
            {
                outObject = null;
                return false;
            }
            else if (binding.isPPtrCurve)
            {
                outObject = modification.objectReference;
                return true;
            }
            else
            {
                float temp;
                if (float.TryParse(modification.value, out temp))
                {
                    outObject = temp;
                    return true;
                }
                else
                {
                    outObject = null;
                    return false;
                }
            }
        }

        static void AddKey(IAnimationRecordingState state, EditorCurveBinding binding, Type type, PropertyModification modification)
        {
            GameObject root = state.activeRootGameObject;
            AnimationClip clip = state.activeAnimationClip;

            if ((clip.hideFlags & HideFlags.NotEditable) != 0)
                return;

            AnimationWindowCurve curve = new AnimationWindowCurve(clip, binding, type);

            // Add key at current frame
            object currentValue = CurveBindingUtility.GetCurrentValue(root, binding);

            if (state.addZeroFrame)
            {
                // Is it a new curve?
                if (curve.length == 0)
                {
                    object oldValue = null;
                    if (!ValueFromPropertyModification(modification, binding, out oldValue))
                        oldValue = currentValue;

                    if (state.currentFrame != 0)
                        AnimationWindowUtility.AddKeyframeToCurve(curve, oldValue, type, AnimationKeyTime.Frame(0, clip.frameRate));
                }
            }

            AnimationWindowUtility.AddKeyframeToCurve(curve, currentValue, type, AnimationKeyTime.Frame(state.currentFrame, clip.frameRate));

            state.SaveCurve(curve);
        }

        static public void SaveModifiedCurve(AnimationWindowCurve curve, AnimationClip clip)
        {
            curve.m_Keyframes.Sort((a, b) => a.time.CompareTo(b.time));

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
                    QuaternionCurveTangentCalculation.UpdateTangentsFromMode(animationCurve, clip, curve.binding);

                AnimationUtility.SetEditorCurve(clip, curve.binding, animationCurve);
            }
        }

        static void AddRotationKey(IAnimationRecordingState state, EditorCurveBinding binding, Type type, Vector3 previousEulerAngles, Vector3 currentEulerAngles)
        {
            AnimationClip clip = state.activeAnimationClip;

            if ((clip.hideFlags & HideFlags.NotEditable) != 0)
                return;

            EditorCurveBinding[] additionalBindings = RotationCurveInterpolation.RemapAnimationBindingForRotationAddKey(binding, clip);

            // Add key at current frame
            for (int i = 0; i < 3; i++)
            {
                AnimationWindowCurve curve = new AnimationWindowCurve(clip, additionalBindings[i], type);

                if (state.addZeroFrame)
                {
                    // Is it a new curve?
                    if (curve.length == 0)
                    {
                        if (state.currentFrame != 0)
                        {
                            AnimationWindowUtility.AddKeyframeToCurve(curve, previousEulerAngles[i], type, AnimationKeyTime.Frame(0, clip.frameRate));
                        }
                    }
                }

                AnimationWindowUtility.AddKeyframeToCurve(curve, currentEulerAngles[i], type, AnimationKeyTime.Frame(state.currentFrame, clip.frameRate));
                state.SaveCurve(curve);
            }
        }
    }
}
