// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    [System.Serializable]
    internal class TransformRotationGUI
    {
        private GUIContent rotationContent = EditorGUIUtility.TrTextContent("Rotation", "The local rotation of this Game Object relative to the parent.");

        private Vector3 m_EulerAngles;
        private float[] m_EulerFloats = new float[3];
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
            m_EulerFloats[0] = m_EulerAngles.x;
            m_EulerFloats[1] = m_EulerAngles.y;
            m_EulerFloats[2] = m_EulerAngles.z;

            EditorGUI.showMixedValue = differentRotation;

            EditorGUI.BeginChangeCheck();

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

            // Using MultiFieldPrefixLabel/MultiFloatField here instead of Vector3Field
            // so that the label and the float fields can be disabled separately,
            // similar to other property-driven controls
            // Using MultiFloatField instead of Vector3Field to avoid superfluous label
            // (which creates a focus target even when there's no content (Case 953241))
            r = EditorGUI.MultiFieldPrefixLabel(r, id, label, 3);
            r.height = EditorGUIUtility.singleLineHeight;
            using (new EditorGUI.DisabledScope(disabled))
                EditorGUI.MultiFloatField(r, s_XYZLabels, m_EulerFloats);

            if (EditorGUI.EndChangeCheck())
            {
                m_EulerAngles = new Vector3(m_EulerFloats[0], m_EulerFloats[1], m_EulerFloats[2]);
                Undo.RecordObjects(targets, "Inspector");  // Generic undo title to be consistent with Position and Scale changes.
                foreach (Transform tr in targets)
                {
                    tr.SetLocalEulerAngles(m_EulerAngles, tr.rotationOrder);
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
