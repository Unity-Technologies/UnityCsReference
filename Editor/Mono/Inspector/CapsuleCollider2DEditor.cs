// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEditor.EditorTools;

namespace UnityEditor
{
    [CustomEditor(typeof(CapsuleCollider2D))]
    [CanEditMultipleObjects]
    class CapsuleCollider2DEditor : Collider2DEditorBase
    {
        SerializedProperty m_Size;
        SerializedProperty m_Direction;

        public override void OnEnable()
        {
            base.OnEnable();

            m_Size = serializedObject.FindProperty("m_Size");
            m_Direction = serializedObject.FindProperty("m_Direction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Collider"), target);

            GUILayout.Space(5);
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_Size);
            EditorGUILayout.PropertyField(m_Direction);

            serializedObject.ApplyModifiedProperties();

            FinalizeInspectorGUI();
        }
    }

    [EditorTool("Edit Capsule Collider 2D", typeof(CapsuleCollider2D))]
    class CapsuleCollider2DTool : PrimitiveCollider2DTool<CapsuleCollider2D>
    {
        readonly CapsuleBoundsHandle m_BoundsHandle = new CapsuleBoundsHandle();

        protected override PrimitiveBoundsHandle boundsHandle
        {
            get { return m_BoundsHandle; }
        }

        protected override void CopyColliderSizeToHandle(CapsuleCollider2D collider)
        {
            Vector3 handleHeightAxis, handleRadiusAxis;
            GetHandleVectorsInWorldSpace(collider, out handleHeightAxis, out handleRadiusAxis);
            m_BoundsHandle.height = m_BoundsHandle.radius = 0f;
            m_BoundsHandle.height = handleHeightAxis.magnitude;
            m_BoundsHandle.radius = handleRadiusAxis.magnitude * 0.5f;
        }

        protected override bool CopyHandleSizeToCollider(CapsuleCollider2D collider)
        {
            // transform handle axes into world space
            Vector3 localDiameterAxis, localHeightAxis;

            if (collider.direction == CapsuleDirection2D.Horizontal)
            {
                localDiameterAxis = Vector3.up;
                localHeightAxis = Vector3.right;
            }
            else
            {
                localDiameterAxis = Vector3.right;
                localHeightAxis = Vector3.up;
            }

            Vector3 worldHeight = Handles.matrix * (localHeightAxis * m_BoundsHandle.height);
            Vector3 worldDiameter = Handles.matrix * (localDiameterAxis * m_BoundsHandle.radius * 2f);

            // project collider's diameter and height axes onto world x/y plane and scale by handle values
            Matrix4x4 colliderTransformMatrix = collider.transform.localToWorldMatrix;
            Vector3 projectedDiameter = ProjectOntoWorldPlane(colliderTransformMatrix * localDiameterAxis).normalized * worldDiameter.magnitude;
            Vector3 projectedHeight = ProjectOntoWorldPlane(colliderTransformMatrix * localHeightAxis).normalized * worldHeight.magnitude;

            // project results back in collider's local space
            projectedDiameter = ProjectOntoColliderPlane(projectedDiameter, colliderTransformMatrix);
            projectedHeight = ProjectOntoColliderPlane(projectedHeight, colliderTransformMatrix);

            Vector2 oldSize = collider.size;

            collider.size = colliderTransformMatrix.inverse * (projectedDiameter + projectedHeight);

            // test for size change after using property setter in case input data was sanitized
            return collider.size != oldSize;
        }

        protected override Quaternion GetHandleRotation(CapsuleCollider2D collider)
        {
            Vector3 diameterVector, heightVector;
            GetHandleVectorsInWorldSpace(collider, out heightVector, out diameterVector);
            return Quaternion.LookRotation(Vector3.forward, heightVector);
        }

        void GetHandleVectorsInWorldSpace(CapsuleCollider2D collider, out Vector3 handleHeightVector, out Vector3 handleDiameterVector)
        {
            Matrix4x4 colliderTransformMatrix = collider.transform.localToWorldMatrix;
            Vector3 x = ProjectOntoWorldPlane(colliderTransformMatrix * (Vector3.right * collider.size.x));
            Vector3 y = ProjectOntoWorldPlane(colliderTransformMatrix * (Vector3.up * collider.size.y));

            if (collider.direction == CapsuleDirection2D.Horizontal)
            {
                handleDiameterVector = y;
                handleHeightVector = x;
            }
            else
            {
                handleDiameterVector = x;
                handleHeightVector = y;
            }
        }
    }
}
