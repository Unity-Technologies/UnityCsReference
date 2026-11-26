// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    // Used to diff the world version of transforms.
    sealed class TransformWorldSpaceGUIData
    {
        // Rotations closer than this, in degrees, are considered equal when checking whether an euler hint is still valid.
        const float k_RotationHintTolerance = 1e-4f;

        // Components of positions and scales smaller than this, relative to the estimate of their rounding error, are floating-point noise.
        const double k_NoiseThreshold = 1e-12;

        // Angles smaller than this, in degrees, are noise from the float precision of the local rotations (around 1e-5 degrees per level).
        const double k_AngleNoiseThreshold = k_RotationHintTolerance;

        // Angle, in degrees, between a float quaternion and the exact rotation it was computed from, which differ by a few ulps.
        const double k_FloatRotationPrecision = 5e-5;

        const string k_LocalPositionPropertyPath = "m_LocalPosition";
        const string k_LocalRotationPropertyPath = "m_LocalRotation";

        readonly Transform[] m_Transforms;
        // Kept in double precision until the local positions are set, so that the edit of an axis does not round the others.
        readonly Vector3Double[] m_Positions;
        readonly Vector3[] m_EulerAngles;
        readonly Vector3[] m_Scales;

        readonly Vector3Double[] m_PositionsAtEditStart;
        readonly Vector3[] m_EulerAnglesAtEditStart;
        // Text field edited when the values at edit start were taken, or 0 when no text field was edited.
        int m_EditedControlAtEditStart;

        readonly Vector3?[] m_EulerHints;

        // World transforms of the targets and their parents, computed once per recompute and shared by the targets with common parents.
        readonly Dictionary<Transform, WorldTransform> m_WorldTransforms = new Dictionary<Transform, WorldTransform>();
        readonly List<Transform> m_UncomputedTransforms = new List<Transform>();
        // Parents of the transform being set by GetLocalPosition, reused to avoid allocating for each target.
        readonly List<ParentSpace> m_ParentSpaces = new List<ParentSpace>();

        // Local transform of a parent, with the current position of the transform being set in the space of this parent.
        struct ParentSpace
        {
            public Vector3Double position;
            public QuaternionDouble inverseRotation;
            public Vector3 scale;
            public Vector3Double currentPositionInSpace;
        }

        // World rotation, position and rotation-scale matrix of a transform, in double precision.
        struct WorldTransform
        {
            public QuaternionDouble rotation;
            // Used as reference when picking the displayed world euler angles.
            public Vector3Double summedLocalEulerAngles;
            public Vector3Double position;
            public Vector3Double axisX;
            public Vector3Double axisY;
            public Vector3Double axisZ;
            // For each component, the sum of the magnitudes of the terms it was computed from, to estimate its rounding error.
            // Kept per component, as a large value on an axis says nothing about the error of a small value on another axis.
            public Vector3Double positionErrorScale;
            public Vector3Double axisXErrorScale;
            public Vector3Double axisYErrorScale;
            public Vector3Double axisZErrorScale;
            // Signs of the local scales of the transform and its parents, multiplied together, which mirror the rotations of its children.
            public Vector3 scaleSigns;

            public readonly Vector3Double TransformVector(Vector3Double vector) => axisX * vector.x + axisY * vector.y + axisZ * vector.z;

            // Error scale of TransformVector, for a vector with the given error scale.
            public readonly Vector3Double TransformErrorScale(Vector3Double errorScale) =>
                (Abs(axisX) + axisXErrorScale) * errorScale.x +
                (Abs(axisY) + axisYErrorScale) * errorScale.y +
                (Abs(axisZ) + axisZErrorScale) * errorScale.z;
        }

        static readonly WorldTransform k_RootWorldTransform = new WorldTransform
        {
            rotation = QuaternionDouble.identity,
            axisX = new Vector3Double(1, 0, 0),
            axisY = new Vector3Double(0, 1, 0),
            axisZ = new Vector3Double(0, 0, 1),
            scaleSigns = Vector3.one,
        };

        // Values of the first target, with a bit set for each axis where the targets have different values.
        public Vector3Double position { get; private set; }
        public int positionMixedFlags { get; private set; }
        public Vector3Double eulerRotation { get; private set; }
        public int rotationMixedFlags { get; private set; }
        public Vector3Double scale { get; private set; }
        public int scaleMixedFlags { get; private set; }

        public TransformWorldSpaceGUIData(Object[] targets)
        {
            var transforms = new List<Transform>(targets.Length);
            foreach (var target in targets)
            {
                if (target is Transform transform)
                    transforms.Add(transform);
            }

            m_Transforms = transforms.ToArray();
            int count = m_Transforms.Length;
            m_Positions = new Vector3Double[count];
            m_EulerAngles = new Vector3[count];
            m_Scales = new Vector3[count];
            m_PositionsAtEditStart = new Vector3Double[count];
            m_EulerAnglesAtEditStart = new Vector3[count];
            m_EulerHints = new Vector3?[count];

            Recompute(true);
        }

        public void Update()
        {
            // Ensure that recomputes are only done once per frame
            if (Event.current != null && Event.current.type != EventType.Layout)
                return;

            Recompute(false);
        }

        // Recomputes the world values from the transforms. The values at edit start are kept while a text field is edited.
        void Recompute(bool resetEditStart)
        {
            if (m_Transforms.Length == 0)
                return;

            m_WorldTransforms.Clear();
            for (int i = 0; i < m_Transforms.Length; i++)
            {
                Transform transform = m_Transforms[i];
                if (transform == null)
                    continue;

                m_Positions[i] = GetWorldPosition(transform);
                m_EulerAngles[i] = GetWorldEulerAngles(i, transform);
                m_Scales[i] = GetWorldScale(transform);
            }

            // Clicking another field ends the edit and starts the next one in the same event, so the edited field is compared too.
            int editedControl = EditorGUIUtility.editingTextField ? GUIUtility.keyboardControl : 0;
            if (resetEditStart || editedControl == 0 || editedControl != m_EditedControlAtEditStart)
            {
                m_Positions.CopyTo(m_PositionsAtEditStart, 0);
                m_EulerAngles.CopyTo(m_EulerAnglesAtEditStart, 0);
                m_EditedControlAtEditStart = editedControl;
            }

            // Displayed in float precision, which also hides the rounding errors of the double computation.
            position = (Vector3)m_Positions[0];
            positionMixedFlags = GetMixedFlags(m_Positions);
            eulerRotation = m_EulerAngles[0];
            rotationMixedFlags = GetMixedFlags(m_EulerAngles);
            scale = m_Scales[0];
            scaleMixedFlags = GetMixedFlags(m_Scales);
        }

        // Compared as displayed, in float precision.
        static int GetMixedFlags(Vector3Double[] values)
        {
            int mixedFlags = 0;
            for (int i = 1; i < values.Length; i++)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    if (!Mathf.Approximately((float)values[i][axis], (float)values[0][axis]))
                        mixedFlags |= 1 << axis;
                }
            }

            return mixedFlags;
        }

        static int GetMixedFlags(Vector3[] values)
        {
            int mixedFlags = 0;
            for (int i = 1; i < values.Length; i++)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    if (!Mathf.Approximately(values[i][axis], values[0][axis]))
                        mixedFlags |= 1 << axis;
                }
            }

            return mixedFlags;
        }

        // Sets the changed axes of the world position of each transform. When animated, the change is recorded by the animation.
        public void ApplyPosition(EditorGUI.NumberFieldValue[] values, int changedAxes, bool animated)
        {
            Undo.RecordObjects(m_Transforms, "Inspector"); // Generic undo title as remove duplicates will discard the name.
            m_WorldTransforms.Clear(); // The transforms may have changed since the last recompute.

            foreach (int i in GetIndicesFromAncestorsToDescendants())
            {
                Transform transform = m_Transforms[i];
                if (transform == null)
                    continue;

                Vector3Double worldPosition = GetWorldPosition(transform);
                SetSpecificAxes(ref worldPosition, values, changedAxes, m_PositionsAtEditStart[i], i);

                Vector3 localPosition = GetLocalPosition(transform, worldPosition);
                if (animated)
                    SetAnimatedProperty(transform, k_LocalPositionPropertyPath, property => property.vector3Value = localPosition);
                else
                    transform.localPosition = localPosition;

                m_WorldTransforms.Clear(); // The children of the transform have moved too, and can also be targets.
            }

            Transform transform0 = m_Transforms[0];
            Undo.SetCurrentGroupName($"Set World Position to {(Vector3)GetWorldPosition(transform0)} in {(Selection.knownCount == 1 ? transform0.name : "Selected Objects")}");
            Recompute(false);
        }

        // Sets the changed axes of the world euler angles of each transform. When animated, the change is recorded by the animation.
        public void ApplyRotation(EditorGUI.NumberFieldValue[] values, int changedAxes, bool animated)
        {
            Undo.RecordObjects(m_Transforms, "Inspector"); // Generic undo title as remove duplicates will discard the name.
            m_WorldTransforms.Clear(); // The transforms may have changed since the last recompute.

            // Read before any target is set, as rotating a selected parent changes the world angles of its selected children on all axes.
            for (int i = 0; i < m_Transforms.Length; i++)
            {
                if (m_Transforms[i] != null)
                    m_EulerAngles[i] = GetWorldEulerAngles(i, m_Transforms[i]);
            }

            foreach (int i in GetIndicesFromAncestorsToDescendants())
            {
                Transform transform = m_Transforms[i];
                if (transform == null)
                    continue;

                Vector3Double editedEulerAngles = m_EulerAngles[i];
                SetSpecificAxes(ref editedEulerAngles, values, changedAxes, m_EulerAnglesAtEditStart[i], i);
                var eulerAngles = (Vector3)editedEulerAngles;

                QuaternionDouble localRotation = QuaternionDouble.Euler(eulerAngles);
                if (transform.parent != null)
                {
                    WorldTransform parentWorldTransform = GetWorldTransform(transform.parent);
                    localRotation = MirrorRotation(QuaternionDouble.Inverse(parentWorldTransform.rotation) * localRotation, parentWorldTransform.scaleSigns);
                }

                if (animated)
                {
                    SetAnimatedProperty(transform, k_LocalRotationPropertyPath, property => property.quaternionValue = (Quaternion)localRotation);
                }
                else
                {
                    // Setting local euler angles, closest to the previous ones, keeps the values displayed in local space clean.
                    if (transform.rotationOrder == RotationOrder.OrderZXY)
                        transform.SetLocalEulerAngles(RemoveAngleNoise(localRotation.GetClosestEulerAngles(transform.GetLocalEulerAngles(RotationOrder.OrderZXY))), RotationOrder.OrderZXY);
                    else
                        transform.localRotation = (Quaternion)localRotation;

                    if (transform.parent != null)
                        transform.SendTransformChangedScale(); // Force scale update, needed if the transform has a non-uniformly scaled parent.
                }

                m_EulerHints[i] = eulerAngles;
                m_WorldTransforms.Clear(); // The children of the transform have rotated too, and can also be targets.
            }

            Undo.SetCurrentGroupName($"Set World Rotation in {(Selection.knownCount == 1 ? m_Transforms[0].name : "Selected Objects")}");
            Recompute(false);
        }

        // Selected ancestors are set before their descendants, which they would move again otherwise.
        // The indices are kept, as the expressions are evaluated from the index of each target in the selection.
        int[] GetIndicesFromAncestorsToDescendants()
        {
            var depths = new int[m_Transforms.Length];
            var indices = new int[m_Transforms.Length];
            for (int i = 0; i < m_Transforms.Length; i++)
            {
                indices[i] = i;
                for (Transform parent = m_Transforms[i] != null ? m_Transforms[i].parent : null; parent != null; parent = parent.parent)
                {
                    depths[i]++;
                }
            }

            System.Array.Sort(depths, indices);
            return indices;
        }

        void SetSpecificAxes(ref Vector3Double target, EditorGUI.NumberFieldValue[] values, int changedAxes, Vector3Double valueAtEditStart, int targetIndex)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                if ((changedAxes & (1 << axis)) == 0)
                    continue;

                var value = values[axis];
                if (value.expression != null)
                {
                    double targetValue = valueAtEditStart[axis];
                    if (value.expression.Evaluate(ref targetValue, targetIndex, m_Transforms.Length))
                        target[axis] = ClampToFloatRange(targetValue);
                }
                else
                {
                    target[axis] = ClampToFloatRange(value.doubleVal);
                }
            }
        }

        // Unlike MathUtils.ClampToFloat, keeps the double precision.
        static double ClampToFloatRange(double value) => System.Math.Clamp(value, float.MinValue, float.MaxValue);

        // Each target needs its own serialized object, as the same world value gives a different local value for each target,
        // while the multi-object property of the inspector sets the same value on all of them.
        static void SetAnimatedProperty(Object target, string propertyPath, System.Action<SerializedProperty> setValue)
        {
            using var serializedObject = new SerializedObject(target);
            setValue(serializedObject.FindProperty(propertyPath));
            serializedObject.ApplyModifiedProperties();
        }

        Vector3 GetWorldEulerAngles(int targetIndex, Transform transform)
        {
            WorldTransform worldTransform = GetWorldTransform(transform);
            QuaternionDouble rotation = worldTransform.rotation;

            Vector3? hint = m_EulerHints[targetIndex];
            if (hint.HasValue && QuaternionDouble.Angle(QuaternionDouble.Euler(hint.Value), rotation) < k_RotationHintTolerance)
                return hint.Value;

            // Without a previous value, the sum of the local angles of the hierarchy is a reference matching the angles
            // displayed in local space when the parents are not rotated, and close to the intended angles otherwise.
            Vector3Double reference = hint.HasValue ? hint.Value : worldTransform.summedLocalEulerAngles;
            Vector3 eulerAngles = RemoveAngleNoise(rotation.GetClosestEulerAngles(reference));
            m_EulerHints[targetIndex] = eulerAngles;
            return eulerAngles;
        }

        // Computes the transforms not computed yet from the closest computed parent, or from the root,
        // so that each transform of the hierarchy is computed once per recompute.
        WorldTransform GetWorldTransform(Transform transform)
        {
            m_UncomputedTransforms.Clear();
            WorldTransform worldTransform = k_RootWorldTransform;
            for (Transform current = transform; current != null; current = current.parent)
            {
                if (m_WorldTransforms.TryGetValue(current, out worldTransform))
                    break;

                worldTransform = k_RootWorldTransform;
                m_UncomputedTransforms.Add(current);
            }

            for (int i = m_UncomputedTransforms.Count - 1; i >= 0; i--)
            {
                Transform current = m_UncomputedTransforms[i];
                worldTransform = GetChildWorldTransform(worldTransform, current);
                m_WorldTransforms[current] = worldTransform;
            }

            return worldTransform;
        }

        static WorldTransform GetChildWorldTransform(in WorldTransform parent, Transform child)
        {
            QuaternionDouble localRotation = GetLocalRotation(child, out Vector3 localEulerAngles);
            var localPosition = (Vector3Double)child.localPosition;
            var localScale = (Vector3Double)child.localScale;

            var localAxisX = new Vector3Double(localScale.x, 0, 0);
            var localAxisY = new Vector3Double(0, localScale.y, 0);
            var localAxisZ = new Vector3Double(0, 0, localScale.z);
            return new WorldTransform
            {
                rotation = parent.rotation * MirrorRotation(localRotation, parent.scaleSigns),
                summedLocalEulerAngles = parent.summedLocalEulerAngles + Vector3.Scale(localEulerAngles, GetMirroredRotationSigns(parent.scaleSigns)),
                position = parent.position + parent.TransformVector(localPosition),
                axisX = parent.TransformVector(localRotation * localAxisX),
                axisY = parent.TransformVector(localRotation * localAxisY),
                axisZ = parent.TransformVector(localRotation * localAxisZ),
                // The local values are exact, so only the terms of the computation contribute to the error.
                positionErrorScale = parent.positionErrorScale + parent.TransformErrorScale(Abs(localPosition)),
                axisXErrorScale = parent.TransformErrorScale(RotateErrorScale(localRotation, Abs(localAxisX))),
                axisYErrorScale = parent.TransformErrorScale(RotateErrorScale(localRotation, Abs(localAxisY))),
                axisZErrorScale = parent.TransformErrorScale(RotateErrorScale(localRotation, Abs(localAxisZ))),
                scaleSigns = Vector3.Scale(parent.scaleSigns, GetScaleSigns(child.localScale)),
            };
        }

        // Same as scaleMulQuat in the native transform hierarchy, used to compute world rotations. It is its own inverse.
        static QuaternionDouble MirrorRotation(QuaternionDouble rotation, Vector3 scaleSigns)
        {
            Vector3 signs = GetMirroredRotationSigns(scaleSigns);
            return new QuaternionDouble(rotation.x * signs.x, rotation.y * signs.y, rotation.z * signs.z, rotation.w);
        }

        // Signs applied by MirrorRotation to the axis of the rotation, and so to its euler angles.
        static Vector3 GetMirroredRotationSigns(Vector3 scaleSigns) =>
            new Vector3(scaleSigns.y * scaleSigns.z, scaleSigns.x * scaleSigns.z, scaleSigns.x * scaleSigns.y);

        // As the native code, a negative zero is negative.
        static Vector3 GetScaleSigns(Vector3 scale) => new Vector3(GetSign(scale.x), GetSign(scale.y), GetSign(scale.z));

        static float GetSign(float value) => System.BitConverter.SingleToInt32Bits(value) < 0 ? -1 : 1;

        // Error scale of rotation * vector, for a vector with the given error scale: the magnitudes of the terms of each
        // entry of the rotation matrix, applied to it. The terms of the entries mixing two axes are exactly zero when the
        // rotation does not mix them, so that a large value on an axis does not hide a small value on another one.
        // Also valid for the inverse rotation.
        static Vector3Double RotateErrorScale(QuaternionDouble rotation, Vector3Double errorScale)
        {
            double xx = rotation.x * rotation.x;
            double yy = rotation.y * rotation.y;
            double zz = rotation.z * rotation.z;
            double xy = System.Math.Abs(rotation.x * rotation.y) + System.Math.Abs(rotation.w * rotation.z);
            double xz = System.Math.Abs(rotation.x * rotation.z) + System.Math.Abs(rotation.w * rotation.y);
            double yz = System.Math.Abs(rotation.y * rotation.z) + System.Math.Abs(rotation.w * rotation.x);
            return new Vector3Double(
                (1 + 2 * (yy + zz)) * errorScale.x + 2 * xy * errorScale.y + 2 * xz * errorScale.z,
                2 * xy * errorScale.x + (1 + 2 * (xx + zz)) * errorScale.y + 2 * yz * errorScale.z,
                2 * xz * errorScale.x + 2 * yz * errorScale.y + (1 + 2 * (xx + yy)) * errorScale.z);
        }

        static Vector3Double Abs(Vector3Double vector) => new Vector3Double(System.Math.Abs(vector.x), System.Math.Abs(vector.y), System.Math.Abs(vector.z));

        // Uses the local euler angles when possible, as they are the exact values entered by the user,
        // while the local rotation quaternion has rounding errors (e.g. sin(45°) in a 90 degrees rotation).
        // The euler angles can differ from the quaternion used by the engine by up to the hint tolerance,
        // so they are only used when they match it within its float precision.
        static QuaternionDouble GetLocalRotation(Transform transform, out Vector3 localEulerAngles)
        {
            Quaternion localRotation = transform.localRotation;
            QuaternionDouble rotation = ((QuaternionDouble)localRotation).normalized;
            if (transform.rotationOrder == RotationOrder.OrderZXY)
            {
                localEulerAngles = transform.GetLocalEulerAngles(RotationOrder.OrderZXY);
                QuaternionDouble eulerRotation = QuaternionDouble.Euler(localEulerAngles);
                return QuaternionDouble.Angle(eulerRotation, rotation) <= k_FloatRotationPrecision ? eulerRotation : rotation;
            }

            // Wrapped to [-180, 180], as eulerAngles are in [0, 360] and summing them would offset the reference by multiples of 360 degrees.
            Vector3 eulerAngles = localRotation.eulerAngles;
            localEulerAngles = new Vector3(Mathf.DeltaAngle(0, eulerAngles.x), Mathf.DeltaAngle(0, eulerAngles.y), Mathf.DeltaAngle(0, eulerAngles.z));
            return rotation;
        }

        Vector3Double GetWorldPosition(Transform transform)
        {
            WorldTransform worldTransform = GetWorldTransform(transform);
            return RemoveNoise(worldTransform.position, worldTransform.positionErrorScale);
        }

        // Inverse of GetWorldPosition. On an axis where a parent has a zero scale, the world position
        // cannot be changed, so the current value of the transform on this axis is kept.
        Vector3 GetLocalPosition(Transform transform, Vector3Double worldPosition)
        {
            // Parents from the closest to the root.
            m_ParentSpaces.Clear();
            var currentPosition = (Vector3Double)transform.localPosition;
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            {
                QuaternionDouble rotation = GetLocalRotation(parent, out _);
                var parentSpace = new ParentSpace
                {
                    position = parent.localPosition,
                    inverseRotation = QuaternionDouble.Inverse(rotation),
                    scale = parent.localScale,
                    currentPositionInSpace = currentPosition,
                };
                m_ParentSpaces.Add(parentSpace);
                currentPosition = parentSpace.position + rotation * Vector3Double.Scale(currentPosition, parentSpace.scale);
            }

            Vector3Double position = worldPosition;
            Vector3Double errorScale = Abs(worldPosition);
            for (int i = m_ParentSpaces.Count - 1; i >= 0; i--)
            {
                ParentSpace parentSpace = m_ParentSpaces[i];
                position = parentSpace.inverseRotation * (position - parentSpace.position);
                errorScale = RotateErrorScale(parentSpace.inverseRotation, errorScale + Abs(parentSpace.position));

                for (int axis = 0; axis < 3; axis++)
                {
                    if (parentSpace.scale[axis] != 0)
                    {
                        position[axis] /= parentSpace.scale[axis];
                        errorScale[axis] /= System.Math.Abs(parentSpace.scale[axis]);
                    }
                    else
                    {
                        position[axis] = parentSpace.currentPositionInSpace[axis];
                        errorScale[axis] = System.Math.Abs(position[axis]);
                    }
                }
            }

            return (Vector3)RemoveNoise(position, errorScale);
        }

        // Same as Transform.lossyScale: the diagonal of the world rotation-scale matrix once the world rotation is removed.
        Vector3 GetWorldScale(Transform transform)
        {
            WorldTransform worldTransform = GetWorldTransform(transform);
            QuaternionDouble inverseRotation = QuaternionDouble.Inverse(worldTransform.rotation);
            var worldScale = new Vector3Double(
                (inverseRotation * worldTransform.axisX).x,
                (inverseRotation * worldTransform.axisY).y,
                (inverseRotation * worldTransform.axisZ).z);
            var errorScale = new Vector3Double(
                RotateErrorScale(inverseRotation, Abs(worldTransform.axisX) + worldTransform.axisXErrorScale).x,
                RotateErrorScale(inverseRotation, Abs(worldTransform.axisY) + worldTransform.axisYErrorScale).y,
                RotateErrorScale(inverseRotation, Abs(worldTransform.axisZ) + worldTransform.axisZErrorScale).z);

            return (Vector3)RemoveNoise(worldScale, errorScale);
        }

        // Sets to zero the components that are only noise relative to the estimate of their rounding error.
        // A component computed exactly has an error scale equal to its magnitude, so it is never removed.
        static Vector3Double RemoveNoise(Vector3Double values, Vector3Double errorScale)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                if (System.Math.Abs(values[axis]) <= k_NoiseThreshold * errorScale[axis])
                    values[axis] = 0;
            }

            return values;
        }

        static Vector3 RemoveAngleNoise(Vector3Double eulerAngles)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                if (System.Math.Abs(eulerAngles[axis]) <= k_AngleNoiseThreshold)
                    eulerAngles[axis] = 0;
            }

            return (Vector3)eulerAngles;
        }
    }
}
