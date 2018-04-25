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
        const string kLocalPosition = "m_LocalPosition";
        const string kLocalRotation = "m_LocalRotation";
        const string kLocalScale = "m_LocalScale";
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

        static private void ProcessRotationModifications(IAnimationRecordingState state, ref System.Collections.Generic.Dictionary<object, RotationModification> rotationModifications)
        {
            AnimationClip clip = state.activeAnimationClip;
            GameObject root = state.activeRootGameObject;

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
        }

        internal class Vector3Modification
        {
            public UndoPropertyModification x;
            public UndoPropertyModification y;
            public UndoPropertyModification z;

            public UndoPropertyModification last;
        }

        static private void CollectVector3Modifications(IAnimationRecordingState state, ref UndoPropertyModification[] modifications,
            ref System.Collections.Generic.Dictionary<object, Vector3Modification> vector3Modifications, string propertyName)
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
                if (binding.propertyName.StartsWith(propertyName))
                {
                    Vector3Modification vector3Modification;

                    if (!vector3Modifications.TryGetValue(prop.target, out vector3Modification))
                    {
                        vector3Modification = new Vector3Modification();
                        vector3Modifications[prop.target] = vector3Modification;
                    }

                    if (binding.propertyName.EndsWith("x"))
                        vector3Modification.x = modification;
                    else if (binding.propertyName.EndsWith("y"))
                        vector3Modification.y = modification;
                    else if (binding.propertyName.EndsWith("z"))
                        vector3Modification.z = modification;

                    vector3Modification.last = modification;
                }
                else
                {
                    outModifs.Add(modification);
                }
            }

            if (vector3Modifications.Count > 0)
            {
                modifications = outModifs.ToArray();
            }
        }

        static private void ProcessVector3Modification(IAnimationRecordingState state, EditorCurveBinding baseBinding, UndoPropertyModification modification, Transform target, string axis)
        {
            var binding = baseBinding;
            var property = modification.previousValue;

            binding.propertyName = binding.propertyName.Remove(binding.propertyName.Length - 1, 1) + axis;

            if (property == null)
            {
                // create dummy
                property = new PropertyModification();
                property.target = target;
                property.propertyPath = binding.propertyName;
                object currentValue = CurveBindingUtility.GetCurrentValue(state.activeRootGameObject, binding);
                property.value = ((float)currentValue).ToString();
            }

            state.AddPropertyModification(binding, property, modification.keepPrefabOverride);
            AddKey(state, binding, typeof(float), property);
        }

        static public void ProcessVector3Modifications(IAnimationRecordingState state, ref System.Collections.Generic.Dictionary<object, Vector3Modification> vector3Modifications)
        {
            AnimationClip clip = state.activeAnimationClip;
            GameObject root = state.activeRootGameObject;

            foreach (KeyValuePair<object, Vector3Modification> item in vector3Modifications)
            {
                Vector3Modification m = item.Value;
                Transform target = item.Key as Transform;

                if (target == null)
                    continue;

                EditorCurveBinding binding = new EditorCurveBinding();
                Type type = AnimationUtility.PropertyModificationToEditorCurveBinding(m.last.currentValue, state.activeRootGameObject, out binding);
                if (type == null)
                    continue;

                ProcessVector3Modification(state, binding, m.x, target, "x");
                ProcessVector3Modification(state, binding, m.y, target, "y");
                ProcessVector3Modification(state, binding, m.z, target, "z");
            }
        }

        static public void ProcessModifications(IAnimationRecordingState state, UndoPropertyModification[] modifications)
        {
            AnimationClip clip = state.activeAnimationClip;
            GameObject root = state.activeRootGameObject;

            // Record modified properties
            for (int i = 0; i < modifications.Length; i++)
            {
                EditorCurveBinding binding = new EditorCurveBinding();

                PropertyModification prop = modifications[i].previousValue;
                Type type = AnimationUtility.PropertyModificationToEditorCurveBinding(prop, root, out binding);
                if (type != null)
                {
                    state.AddPropertyModification(binding, prop, modifications[i].keepPrefabOverride);
                    AddKey(state, binding, type, prop);
                }
            }
        }

        static public UndoPropertyModification[] Process(IAnimationRecordingState state, UndoPropertyModification[] modifications)
        {
            GameObject root = state.activeRootGameObject;
            if (root == null)
                return modifications;

            // Fast path detect that nothing recordable has changed.
            if (!HasAnyRecordableModifications(root, modifications))
                return modifications;

            System.Collections.Generic.Dictionary<object, RotationModification> rotationModifications = new Dictionary<object, RotationModification>();
            System.Collections.Generic.Dictionary<object, Vector3Modification> scaleModifications = new Dictionary<object, Vector3Modification>();
            System.Collections.Generic.Dictionary<object, Vector3Modification> positionModifications = new Dictionary<object, Vector3Modification>();

            //Strip out the rotation modifications from the rest
            CollectRotationModifications(state, ref modifications, ref rotationModifications);
            UndoPropertyModification[] discardedRotationModifications = FilterRotationModifications(state, ref rotationModifications);
            UndoPropertyModification[] discardedModifications = FilterModifications(state, ref modifications);
            CollectVector3Modifications(state, ref modifications, ref positionModifications, kLocalPosition);
            CollectVector3Modifications(state, ref modifications, ref scaleModifications, kLocalScale);

            // Process Animator Modifications.
            ProcessAnimatorModifications(state, ref positionModifications, ref rotationModifications, ref scaleModifications);
            // Process position modifications.
            ProcessVector3Modifications(state, ref positionModifications);
            // Process rotation modifications.
            ProcessRotationModifications(state, ref rotationModifications);
            // Process scale modifications.
            ProcessVector3Modifications(state, ref scaleModifications);
            // Process what's remaining.
            ProcessModifications(state, modifications);

            discardedModifications.Concat(discardedRotationModifications);
            return discardedModifications.ToArray();
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

        internal class RootMotionModification
        {
            public UndoPropertyModification px;
            public UndoPropertyModification py;
            public UndoPropertyModification pz;
            public UndoPropertyModification lastP;

            public UndoPropertyModification rx;
            public UndoPropertyModification ry;
            public UndoPropertyModification rz;
            public UndoPropertyModification rw;
            public UndoPropertyModification lastR;
        }

        static private void ProcessRootMotionModifications(IAnimationRecordingState state,
            ref System.Collections.Generic.Dictionary<object, RootMotionModification> rootMotionModifications)
        {
            AnimationClip clip = state.activeAnimationClip;
            GameObject root = state.activeRootGameObject;
            Animator animator = root.GetComponent<Animator>();

            bool isHuman = animator != null ? animator.isHuman : false;

            foreach (KeyValuePair<object, RootMotionModification> item in rootMotionModifications)
            {
                RootMotionModification m = item.Value;
                Transform target = item.Key as Transform;

                Vector3 scale = target.localScale * (isHuman ? animator.humanScale : 1);
                Vector3 position = target.localPosition;
                Quaternion rotation = target.localRotation;

                if (m.lastP.previousValue != null)
                {
                    ProcessAnimatorModification(state, animator, m.px, "MotionT.x", position.x, scale.x);
                    ProcessAnimatorModification(state, animator, m.py, "MotionT.y", position.y, scale.y);
                    ProcessAnimatorModification(state, animator, m.pz, "MotionT.z", position.z, scale.z);
                }

                if (m.lastR.previousValue != null)
                {
                    ProcessAnimatorModification(state, animator, m.rx, "MotionQ.x", rotation.x, 1);
                    ProcessAnimatorModification(state, animator, m.ry, "MotionQ.y", rotation.y, 1);
                    ProcessAnimatorModification(state, animator, m.rz, "MotionQ.z", rotation.z, 1);
                    ProcessAnimatorModification(state, animator, m.rw, "MotionQ.w", rotation.w, 1);
                }
            }
        }

        static private void ProcessAnimatorModifications(IAnimationRecordingState state,
            ref System.Collections.Generic.Dictionary<object, Vector3Modification> positionModifications,
            ref System.Collections.Generic.Dictionary<object, RotationModification> rotationModifications,
            ref System.Collections.Generic.Dictionary<object, Vector3Modification> scaleModifications)
        {
            System.Collections.Generic.Dictionary<object, RootMotionModification> rootMotionModifications = new Dictionary<object, RootMotionModification>();

            AnimationClip clip = state.activeAnimationClip;
            GameObject root = state.activeRootGameObject;
            Animator animator = root.GetComponent<Animator>();

            bool isHuman = animator != null ? animator.isHuman : false;

            // timeline root motion scale revert
            // bool hasRootMotion = animator != null ? animator.hasRootMotion : false;
            // bool applyRootMotion = animator != null ? animator.applyRootMotion : false;

            // process animator positions
            List<object> discardListPos = new List<object>();

            foreach (KeyValuePair<object, Vector3Modification> item in positionModifications)
            {
                Vector3Modification m = item.Value;
                Transform target = item.Key as Transform;

                if (target == null)
                    continue;

                EditorCurveBinding binding = new EditorCurveBinding();
                Type type = AnimationUtility.PropertyModificationToEditorCurveBinding(m.last.currentValue, state.activeRootGameObject, out binding);
                if (type == null)
                    continue;

                bool isRootTransform = root.transform == target;
                // timeline root motion scale revert
                // bool isRootMotion = (isHuman || hasRootMotion || applyRootMotion) && isRootTransform;
                bool isRootMotion = false;

                bool isHumanBone = isHuman && !isRootTransform && animator.IsBoneTransform(target);

                if (isHumanBone)
                {
                    Debug.LogWarning("Keyframing translation on humanoid rig is not supported!", target as Transform);
                    discardListPos.Add(item.Key);
                }
                else if (isRootMotion)
                {
                    RootMotionModification rootMotionModification;

                    if (!rootMotionModifications.TryGetValue(target, out rootMotionModification))
                    {
                        rootMotionModification = new RootMotionModification();
                        rootMotionModifications[target] = rootMotionModification;
                    }

                    rootMotionModification.lastP = m.last;
                    rootMotionModification.px = m.x;
                    rootMotionModification.py = m.y;
                    rootMotionModification.pz = m.z;

                    discardListPos.Add(item.Key);
                }
            }

            foreach (object key in discardListPos)
            {
                positionModifications.Remove(key);
            }

            // process animator rotation
            List<object> discardListRot = new List<object>();

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

                bool isRootTransform = root.transform == target;

                // timeline root motion scale revert
                // bool isRootMotion = (isHuman || hasRootMotion || applyRootMotion) && isRootTransform;
                bool isRootMotion = false;

                bool isHumanBone = isHuman && !isRootTransform && animator.IsBoneTransform(target);

                if (isHumanBone)
                {
                    Debug.LogWarning("Keyframing rotation on humanoid rig is not supported!", target as Transform);
                    discardListRot.Add(item.Key);
                }
                else if (isRootMotion)
                {
                    RootMotionModification rootMotionModification;

                    if (!rootMotionModifications.TryGetValue(target, out rootMotionModification))
                    {
                        rootMotionModification = new RootMotionModification();
                        rootMotionModifications[target] = rootMotionModification;
                    }

                    rootMotionModification.lastR = m.lastQuatModification;
                    rootMotionModification.rx = m.x;
                    rootMotionModification.ry = m.y;
                    rootMotionModification.rz = m.z;
                    rootMotionModification.rw = m.w;

                    discardListRot.Add(item.Key);
                }
            }

            foreach (object key in discardListRot)
            {
                rotationModifications.Remove(key);
            }

            // process animator scales
            List<object> discardListScale = new List<object>();

            foreach (KeyValuePair<object, Vector3Modification> item in scaleModifications)
            {
                Vector3Modification m = item.Value;
                Transform target = item.Key as Transform;

                if (target == null)
                    continue;

                EditorCurveBinding binding = new EditorCurveBinding();
                Type type = AnimationUtility.PropertyModificationToEditorCurveBinding(m.last.currentValue, state.activeRootGameObject, out binding);
                if (type == null)
                    continue;

                bool isRootTransform = root.transform == target;
                bool isHumanBone = isHuman && !isRootTransform && animator.IsBoneTransform(target);
                if (isHumanBone)
                {
                    Debug.LogWarning("Keyframing scale on humanoid rig is not supported!", target as Transform);
                    discardListScale.Add(item.Key);
                }
            }

            foreach (object key in discardListScale)
            {
                scaleModifications.Remove(key);
            }

            ProcessRootMotionModifications(state, ref rootMotionModifications);
        }

        static void ProcessAnimatorModification(IAnimationRecordingState state, Animator animator, UndoPropertyModification modification, string name, float value, float scale)
        {
            AnimationClip clip = state.activeAnimationClip;

            if ((clip.hideFlags & HideFlags.NotEditable) != 0)
                return;

            float prevValue = value;

            object oValue;

            if (ValueFromPropertyModification(modification.currentValue, new EditorCurveBinding(), out oValue))
                value = (float)oValue;

            if (ValueFromPropertyModification(modification.previousValue, new EditorCurveBinding(), out oValue))
                prevValue = (float)oValue;

            value = Mathf.Abs(scale) > Mathf.Epsilon ? value / scale : value;
            prevValue = Mathf.Abs(scale) > Mathf.Epsilon ? prevValue / scale : prevValue;

            var binding = new EditorCurveBinding();
            binding.propertyName = name;
            binding.path = "";
            binding.type = typeof(Animator);

            var prop = new PropertyModification();
            prop.target = animator;
            prop.propertyPath = binding.propertyName;
            prop.value = value.ToString();

            state.AddPropertyModification(binding, prop, modification.keepPrefabOverride);

            AnimationWindowCurve curve = new AnimationWindowCurve(clip, binding, typeof(float));

            if (state.addZeroFrame && state.currentFrame != 0 && curve.length == 0)
            {
                AnimationWindowUtility.AddKeyframeToCurve(curve, prevValue, typeof(float), AnimationKeyTime.Frame(0, clip.frameRate));
            }

            AnimationWindowUtility.AddKeyframeToCurve(curve, value, typeof(float), AnimationKeyTime.Frame(state.currentFrame, clip.frameRate));

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
    }
}
