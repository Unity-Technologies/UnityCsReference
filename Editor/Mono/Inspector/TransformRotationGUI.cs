// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [System.Serializable]
    internal class TransformRotationGUI
    {
        private GUIContent rotationContent = EditorGUIUtility.TrTextContent("Rotation", "The local rotation of this Game Object relative to the parent.");
        private const float kQuaternionFloatPrecision = 1e-6f;

        EditorGUI.NumberFieldValue[] m_EulerFloats =
        {
            new EditorGUI.NumberFieldValue(0.0f),
            new EditorGUI.NumberFieldValue(0.0f),
            new EditorGUI.NumberFieldValue(0.0f)
        };

        SerializedProperty m_Rotation;
        Object[] targets;

        private static int s_FoldoutHash = "Foldout".GetHashCode();
        private static readonly GUIContent[] s_XYZLabels = {EditorGUIUtility.TextContent("X"), EditorGUIUtility.TextContent("Y"), EditorGUIUtility.TextContent("Z")};

        public void OnEnable(SerializedProperty m_Rotation, GUIContent label)
        {
            this.m_Rotation = m_Rotation;
            this.targets = m_Rotation.serializedObject.targetObjects;
            rotationContent = label;
        }

        public void RotationField()
        {
            RotationField(false);
        }

        public void RotationField(bool disabled)
        {
            Transform transform0 = targets[0] as Transform;
            Vector3 eulerAngles0 = transform0.GetLocalEulerAngles(transform0.rotationOrder);

            int differentRotationMask = 0b000;
            bool differentRotationOrder = false;
            for (int i = 1; i < targets.Length; i++)
            {
                Transform otherTransform = (targets[i] as Transform);
                if (differentRotationMask != 0b111)
                {
                    Vector3 otherLocalEuler = otherTransform.GetLocalEulerAngles(otherTransform.rotationOrder);
                    for (int j = 0; j < 3; j++)
                    {
                        if (otherLocalEuler[j] != eulerAngles0[j])
                            differentRotationMask |= 1 << j;
                    }
                }

                differentRotationOrder |= otherTransform.rotationOrder != transform0.rotationOrder;
            }

            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
            GUIContent label = EditorGUI.BeginProperty(r, rotationContent, m_Rotation);

            if (m_Rotation.isLiveModified)
            {
                Vector3 eulerValue = m_Rotation.quaternionValue.eulerAngles;
                m_EulerFloats[0].doubleVal = Mathf.Floor(eulerValue.x / kQuaternionFloatPrecision) * kQuaternionFloatPrecision;
                m_EulerFloats[1].doubleVal = Mathf.Floor(eulerValue.y / kQuaternionFloatPrecision) * kQuaternionFloatPrecision;
                m_EulerFloats[2].doubleVal = Mathf.Floor(eulerValue.z / kQuaternionFloatPrecision) * kQuaternionFloatPrecision;
            }
            else
            {
                m_EulerFloats[0].doubleVal = eulerAngles0.x;
                m_EulerFloats[1].doubleVal = eulerAngles0.y;
                m_EulerFloats[2].doubleVal = eulerAngles0.z;
            }

            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, r);
            if (AnimationMode.InAnimationMode() && transform0.rotationOrder != RotationOrder.OrderZXY)
            {
                string rotationLabel = differentRotationOrder ? "Mixed" : transform0.rotationOrder.ToString().Substring(RotationOrder.OrderXYZ.ToString().Length - 3);
                label.text = label.text + " (" + rotationLabel + ")";
            }

            // Using manual 3 float fields here instead of MultiFloatField or Vector3Field
            // since we want to query expression validity of each individually, and
            // so that the label and the fields can be disabled separately, similar to
            // regular property fields. Also want to avoid superfluous label, which
            // creates a focus target even when there's no content (Case 953241).
            r = EditorGUI.MultiFieldPrefixLabel(r, id, label, 3);
            r.height = EditorGUIUtility.singleLineHeight;
            int eulerChangedMask = 0;
            using (new EditorGUI.DisabledScope(disabled))
            {
                var eCount = m_EulerFloats.Length;
                float w = (r.width - (eCount - 1) * EditorGUI.kSpacingSubLabel) / eCount;
                Rect nr = new Rect(r) {width = w};
                var prevWidth = EditorGUIUtility.labelWidth;
                var prevIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                SerializedProperty rotation = m_Rotation.Copy();

                for (int i = 0; i < m_EulerFloats.Length; i++)
                {
                    rotation.Next(true);
                    EditorGUI.BeginProperty(nr, s_XYZLabels[i], rotation);

                    EditorGUIUtility.labelWidth = EditorGUI.GetLabelWidth(s_XYZLabels[i]);
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.showMixedValue = (differentRotationMask & (1 << i)) != 0;
                    EditorGUI.FloatField(nr, s_XYZLabels[i], ref m_EulerFloats[i]);
                    if (EditorGUI.EndChangeCheck() && m_EulerFloats[i].hasResult)
                        eulerChangedMask |= 1 << i;

                    if (Event.current.type == EventType.ContextClick && nr.Contains(Event.current.mousePosition))
                    {
                        var childProperty = m_Rotation.Copy();
                        childProperty.Next(true);
                        int childPropertyIndex = i;
                        while (childPropertyIndex > 0)
                        {
                            childProperty.Next(false);
                            childPropertyIndex--;
                        }

                        EditorGUI.DoPropertyContextMenu(childProperty);
                        Event.current.Use();
                    }

                    nr.x += w + EditorGUI.kSpacingSubLabel;

                    EditorGUI.EndProperty();
                }
                EditorGUIUtility.labelWidth = prevWidth;
                EditorGUI.indentLevel = prevIndent;
            }

            if (eulerChangedMask != 0)
            {
                eulerAngles0 = new Vector3(
                    MathUtils.ClampToFloat(m_EulerFloats[0].doubleVal),
                    MathUtils.ClampToFloat(m_EulerFloats[1].doubleVal),
                    MathUtils.ClampToFloat(m_EulerFloats[2].doubleVal));
                Undo.RecordObjects(targets, "Inspector");  // Generic undo title as remove duplicates will discard the name.
                Undo.SetCurrentGroupName(string.Format("Set Rotation"));
                for (var idx = 0; idx < targets.Length; ++idx)
                {
                    var tr = targets[idx] as Transform;
                    if (tr == null)
                        continue;
                    var trEuler = tr.GetLocalEulerAngles(tr.rotationOrder);
                    // if we have any per-object expressions just entered, we need to evaluate
                    // it for each object with their own individual input value
                    for (int c = 0; c < 3; ++c)
                    {
                        if ((eulerChangedMask & (1 << c)) != 0)
                        {
                            if (m_EulerFloats[c].expression != null)
                            {
                                double trEulerComp = eulerAngles0[c];
                                if (m_EulerFloats[c].expression.Evaluate(ref trEulerComp, idx, targets.Length))
                                    trEuler[c] = MathUtils.ClampToFloat(trEulerComp);
                            }
                            else
                            {
                                trEuler[c] = MathUtils.ClampToFloat(eulerAngles0[c]);
                            }
                        }
                    }

                    if (m_Rotation.isLiveModified)
                    {
                        m_Rotation.quaternionValue = Quaternion.Euler(trEuler.x, trEuler.y, trEuler.z);
                    }
                    else
                    {
                        tr.SetLocalEulerAngles(trEuler, tr.rotationOrder);
                        if (tr.parent != null)
                            tr.SendTransformChangedScale(); // force scale update, needed if tr has non-uniformly scaled parent.
                    }
                }
                m_Rotation.serializedObject.SetIsDifferentCacheDirty();
            }

            EditorGUI.showMixedValue = false;

            if (differentRotationOrder)
            {
                EditorGUILayout.HelpBox("Transforms have different rotation orders, keyframes saved will have the same value but not the same local rotation", MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }
    }
}
