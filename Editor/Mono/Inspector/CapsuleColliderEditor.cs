// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(CapsuleCollider))]
    [CanEditMultipleObjects]
    internal class CapsuleColliderEditor : PrimitiveCollider3DEditor
    {
        SerializedProperty m_Center;
        SerializedProperty m_Radius;
        SerializedProperty m_Height;
        SerializedProperty m_Direction;

        private readonly CapsuleBoundsHandle m_BoundsHandle = new CapsuleBoundsHandle();

        public override void OnEnable()
        {
            base.OnEnable();

            m_Center = serializedObject.FindProperty("m_Center");
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_Height = serializedObject.FindProperty("m_Height");
            m_Direction = serializedObject.FindProperty("m_Direction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorEditButtonGUI();
            EditorGUILayout.PropertyField(m_IsTrigger);
            EditorGUILayout.PropertyField(m_Material);
            EditorGUILayout.PropertyField(m_Center);
            EditorGUILayout.PropertyField(m_Radius);
            EditorGUILayout.PropertyField(m_Height);
            EditorGUILayout.PropertyField(m_Direction);

            serializedObject.ApplyModifiedProperties();
        }

        protected override PrimitiveBoundsHandle boundsHandle { get { return m_BoundsHandle; } }

        protected override void CopyColliderPropertiesToHandle()
        {
            CapsuleCollider collider = (CapsuleCollider)target;
            m_BoundsHandle.center = TransformColliderCenterToHandleSpace(collider.transform, collider.center);
            float radiusScaleFactor;
            Vector3 sizeScale =
                GetCapsuleColliderHandleScale(collider.transform.lossyScale, collider.direction, out radiusScaleFactor);
            m_BoundsHandle.height = m_BoundsHandle.radius = 0f;
            m_BoundsHandle.height = collider.height * Mathf.Abs(sizeScale[collider.direction]);
            m_BoundsHandle.radius = collider.radius * radiusScaleFactor;
            switch (collider.direction)
            {
                case 0:
                    m_BoundsHandle.heightAxis = CapsuleBoundsHandle.HeightAxis.X;
                    break;
                case 1:
                    m_BoundsHandle.heightAxis = CapsuleBoundsHandle.HeightAxis.Y;
                    break;
                case 2:
                    m_BoundsHandle.heightAxis = CapsuleBoundsHandle.HeightAxis.Z;
                    break;
            }
        }

        protected override void CopyHandlePropertiesToCollider()
        {
            CapsuleCollider collider = (CapsuleCollider)target;
            collider.center = TransformHandleCenterToColliderSpace(collider.transform, m_BoundsHandle.center);
            float radiusScaleFactor;
            Vector3 sizeScale =
                GetCapsuleColliderHandleScale(collider.transform.lossyScale, collider.direction, out radiusScaleFactor);
            sizeScale = InvertScaleVector(sizeScale);
            // only apply changes to collider radius/height if scale factor from transform is non-zero
            if (radiusScaleFactor != 0f)
                collider.radius = m_BoundsHandle.radius / radiusScaleFactor;
            if (sizeScale[collider.direction] != 0f)
                collider.height = m_BoundsHandle.height * Mathf.Abs(sizeScale[collider.direction]);
        }

        protected override void OnSceneGUI()
        {
            // prevent possibility that user increases height if radius scale is zero and user drags (non-moving) radius handles to exceed height extents
            CapsuleCollider collider = (CapsuleCollider)target;
            float radiusScaleFactor;
            GetCapsuleColliderHandleScale(collider.transform.lossyScale, collider.direction, out radiusScaleFactor);
            boundsHandle.axes = PrimitiveBoundsHandle.Axes.All;
            if (radiusScaleFactor == 0f)
            {
                switch (collider.direction)
                {
                    case 0:
                        boundsHandle.axes = PrimitiveBoundsHandle.Axes.X;
                        break;
                    case 1:
                        boundsHandle.axes = PrimitiveBoundsHandle.Axes.Y;
                        break;
                    case 2:
                        boundsHandle.axes = PrimitiveBoundsHandle.Axes.Z;
                        break;
                }
            }

            base.OnSceneGUI();
        }

        private Vector3 GetCapsuleColliderHandleScale(Vector3 lossyScale, int capsuleDirection, out float radiusScaleFactor)
        {
            radiusScaleFactor = 0f;
            for (int axis = 0; axis < 3; ++axis)
            {
                if (axis != capsuleDirection)
                    radiusScaleFactor = Mathf.Max(radiusScaleFactor, Mathf.Abs(lossyScale[axis]));
            }
            for (int axis = 0; axis < 3; ++axis)
            {
                if (axis != capsuleDirection)
                    lossyScale[axis] = Mathf.Sign(lossyScale[axis]) * radiusScaleFactor;
            }
            return lossyScale;
        }
    }
}
