// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(SphereCollider))]
    [CanEditMultipleObjects]
    internal class SphereColliderEditor : PrimitiveCollider3DEditor
    {
        SerializedProperty m_Center;
        SerializedProperty m_Radius;
        private readonly SphereBoundsHandle m_BoundsHandle = new SphereBoundsHandle();

        public override void OnEnable()
        {
            base.OnEnable();

            m_Center = serializedObject.FindProperty("m_Center");
            m_Radius = serializedObject.FindProperty("m_Radius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorEditButtonGUI();
            EditorGUILayout.PropertyField(m_IsTrigger);
            EditorGUILayout.PropertyField(m_Material);
            EditorGUILayout.PropertyField(m_Center);
            EditorGUILayout.PropertyField(m_Radius);

            serializedObject.ApplyModifiedProperties();
        }

        protected override PrimitiveBoundsHandle boundsHandle { get { return m_BoundsHandle; } }

        protected override void CopyColliderPropertiesToHandle()
        {
            SphereCollider collider = (SphereCollider)target;
            m_BoundsHandle.center = TransformColliderCenterToHandleSpace(collider.transform, collider.center);
            m_BoundsHandle.radius = collider.radius * GetRadiusScaleFactor();
        }

        protected override void CopyHandlePropertiesToCollider()
        {
            SphereCollider collider = (SphereCollider)target;
            collider.center = TransformHandleCenterToColliderSpace(collider.transform, m_BoundsHandle.center);
            float scaleFactor = GetRadiusScaleFactor();
            collider.radius =
                Mathf.Approximately(scaleFactor, 0f) ? 0f : m_BoundsHandle.radius / GetRadiusScaleFactor();
        }

        private float GetRadiusScaleFactor()
        {
            float result = 0f;
            Vector3 lossyScale = ((SphereCollider)target).transform.lossyScale;
            for (int axis = 0; axis < 3; ++axis)
            {
                result = Mathf.Max(result, Mathf.Abs(lossyScale[axis]));
            }
            return result;
        }
    }
}
