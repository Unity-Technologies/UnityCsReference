// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [EditorTool("Edit Capsule Collider", typeof(CapsuleCollider))]
    class CapsuleColliderTool : PrimitiveColliderTool<CapsuleCollider>
    {
        readonly CapsuleBoundsHandle m_BoundsHandle = new CapsuleBoundsHandle();
        protected override PrimitiveBoundsHandle boundsHandle { get { return m_BoundsHandle; } }

        protected override void CopyColliderPropertiesToHandle(CapsuleCollider collider)
        {
            m_BoundsHandle.center = TransformColliderCenterToHandleSpace(collider.transform, collider.center);

            float radiusScaleFactor;
            Vector3 sizeScale = GetCapsuleColliderHandleScale(collider.transform.lossyScale, collider.direction, out radiusScaleFactor);

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

        protected override void CopyHandlePropertiesToCollider(CapsuleCollider collider)
        {
            collider.center = TransformHandleCenterToColliderSpace(collider.transform, m_BoundsHandle.center);

            float radiusScaleFactor;
            Vector3 sizeScale = GetCapsuleColliderHandleScale(collider.transform.lossyScale, collider.direction, out radiusScaleFactor);
            sizeScale = InvertScaleVector(sizeScale);

            // only apply changes to collider radius/height if scale factor from transform is non-zero
            if (radiusScaleFactor != 0f)
                collider.radius = m_BoundsHandle.radius / radiusScaleFactor;

            if (sizeScale[collider.direction] != 0f)
                collider.height = m_BoundsHandle.height * Mathf.Abs(sizeScale[collider.direction]);
        }

        public override void OnToolGUI(EditorWindow window)
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

            base.OnToolGUI(window);
        }

        static Vector3 GetCapsuleColliderHandleScale(Vector3 lossyScale, int capsuleDirection, out float radiusScaleFactor)
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

    [CustomEditor(typeof(CapsuleCollider))]
    [CanEditMultipleObjects]
    class CapsuleColliderEditor : Collider3DEditorBase
    {
        SerializedProperty m_Center;
        SerializedProperty m_Radius;
        SerializedProperty m_Height;
        SerializedProperty m_Direction;

        private static class Styles
        {
            public static readonly GUIContent directionContent = EditorGUIUtility.TrTextContent("Direction", "The axis of the capsule’s lengthwise orientation in the GameObject’s local space.");
        }

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

            EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Collider"), this);
            GUILayout.Space(5);
            EditorGUILayout.PropertyField(m_IsTrigger, BaseStyles.triggerContent);
            EditorGUILayout.PropertyField(m_Material, BaseStyles.materialContent);
            EditorGUILayout.PropertyField(m_Center, BaseStyles.centerContent);
            EditorGUILayout.PropertyField(m_Radius);
            EditorGUILayout.PropertyField(m_Height);
            EditorGUILayout.PropertyField(m_Direction, Styles.directionContent);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
