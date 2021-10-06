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

        private Vector3 m_EulerAngles;

        EditorGUI.NumberFieldValue[] m_EulerFloats =
        {
            new EditorGUI.NumberFieldValue(0.0f),
            new EditorGUI.NumberFieldValue(0.0f),
            new EditorGUI.NumberFieldValue(0.0f)
        };
        // Some random rotation that will never be the same as the current one
        private Vector3  m_OldEulerAngles = new Vector3(1000000, 10000000, 1000000);
        private RotationOrder m_OldRotationOrder = RotationOrder.OrderZXY;

        SerializedProperty m_Rotation;
        Object[] targets;

        private static int s_FoldoutHash = "Foldout".GetHashCode();
        private static readonly GUIContent[] s_XYZLabels = {EditorGUIUtility.TextContent("X"), EditorGUIUtility.TextContent("Y"), EditorGUIUtility.TextContent("Z")};

        public void OnEnable(SerializedProperty m_Rotation, GUIContent label)
        {
            this.m_Rotation = m_Rotation;
            this.targets = m_Rotation.serializedObject.targetObjects;
            this.m_OldRotationOrder = (targets[0] as Transform).rotationOrder;
            rotationContent = label;
        }

        public void RotationField()
        {
            RotationField(false);
        }

        public void RotationField(bool disabled)
        {
            Transform t = targets[0] as Transform;
            Vector3 localEuler = t.GetLocalEulerAngles(t.rotationOrder);
            if (
                m_OldEulerAngles.x != localEuler.x ||
                m_OldEulerAngles.y != localEuler.y ||
                m_OldEulerAngles.z != localEuler.z ||
                m_OldRotationOrder != t.rotationOrder
            )
            {
                m_EulerAngles = t.GetLocalEulerAngles(t.rotationOrder);
                m_OldRotationOrder = t.rotationOrder;
            }

            var targetRotationOrder = t.rotationOrder;
            bool differentRotation = false;
            bool differentRotationOrder = false;
            for (int i = 1; i < targets.Length; i++)
            {
                Transform otherTransform = (targets[i] as Transform);
                if (!differentRotation)
                {
                    Vector3 otherLocalEuler = otherTransform.GetLocalEulerAngles(otherTransform.rotationOrder);
                    differentRotation = (otherLocalEuler.x != localEuler.x || otherLocalEuler.y != localEuler.y || otherLocalEuler.z != localEuler.z);
                }

                differentRotationOrder |= otherTransform.rotationOrder != targetRotationOrder;
            }

            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
            GUIContent label = EditorGUI.BeginProperty(r, rotationContent, m_Rotation);
            m_EulerFloats[0].doubleVal = m_EulerAngles.x;
            m_EulerFloats[1].doubleVal = m_EulerAngles.y;
            m_EulerFloats[2].doubleVal = m_EulerAngles.z;

            EditorGUI.showMixedValue = differentRotation;

            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, r);
            string rotationLabel = "";
            if (AnimationMode.InAnimationMode() && t.rotationOrder != RotationOrder.OrderZXY)
            {
                if (differentRotationOrder)
                {
                    rotationLabel = "Mixed";
                }
                else
                {
                    rotationLabel = (t.rotationOrder).ToString();
                    rotationLabel = rotationLabel.Substring(rotationLabel.Length - 3);
                }

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
            bool hasExpressions = false;
            using (new EditorGUI.DisabledScope(disabled))
            {
                var eCount = m_EulerFloats.Length;
                float w = (r.width - (eCount - 1) * EditorGUI.kSpacingSubLabel) / eCount;
                Rect nr = new Rect(r) {width = w};
                var prevWidth = EditorGUIUtility.labelWidth;
                var prevIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                for (int i = 0; i < m_EulerFloats.Length; i++)
                {
                    EditorGUIUtility.labelWidth = EditorGUI.GetLabelWidth(s_XYZLabels[i]);
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.FloatField(nr, s_XYZLabels[i], ref m_EulerFloats[i]);
                    if (EditorGUI.EndChangeCheck() && m_EulerFloats[i].hasResult)
                    {
                        eulerChangedMask |= 1 << i;
                        if (m_EulerFloats[i].expression != null)
                            hasExpressions = true;
                    }
                    nr.x += w + EditorGUI.kSpacingSubLabel;
                }
                EditorGUIUtility.labelWidth = prevWidth;
                EditorGUI.indentLevel = prevIndent;
            }

            if (eulerChangedMask != 0)
            {
                m_EulerAngles = new Vector3(
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
                    var trEuler = m_EulerAngles;
                    // if we have any per-object expressions just entered, we need to evaluate
                    // it for each object with their own individual input value
                    if (hasExpressions)
                    {
                        trEuler = tr.GetLocalEulerAngles(tr.rotationOrder);
                        for (int c = 0; c < 3; ++c)
                        {
                            if ((eulerChangedMask & (1 << c)) != 0 && m_EulerFloats[c].expression != null)
                            {
                                double trEulerComp = trEuler[c];
                                if (m_EulerFloats[c].expression.Evaluate(ref trEulerComp, idx, targets.Length))
                                    trEuler[c] = MathUtils.ClampToFloat(trEulerComp);
                            }
                        }
                    }
                    tr.SetLocalEulerAngles(trEuler, tr.rotationOrder);
                    if (tr.parent != null)
                        tr.SendTransformChangedScale(); // force scale update, needed if tr has non-uniformly scaled parent.
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
