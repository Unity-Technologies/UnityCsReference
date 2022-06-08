// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [EditorTool("Edit Sphere Collider", typeof(SphereCollider))]
    class SphereColliderTool : PrimitiveColliderTool<SphereCollider>
    {
        readonly SphereBoundsHandle m_BoundsHandle = new SphereBoundsHandle();

        protected override PrimitiveBoundsHandle boundsHandle
        {
            get { return m_BoundsHandle; }
        }

        protected override void CopyColliderPropertiesToHandle(SphereCollider collider)
        {
            m_BoundsHandle.center = TransformColliderCenterToHandleSpace(collider.transform, collider.center);
            m_BoundsHandle.radius = collider.radius * GetRadiusScaleFactor(collider);
        }

        protected override void CopyHandlePropertiesToCollider(SphereCollider collider)
        {
            collider.center = TransformHandleCenterToColliderSpace(collider.transform, m_BoundsHandle.center);
            float scaleFactor = GetRadiusScaleFactor(collider);
            collider.radius = Mathf.Approximately(scaleFactor, 0f) ? 0f : m_BoundsHandle.radius / scaleFactor;
        }

        static float GetRadiusScaleFactor(SphereCollider collider)
        {
            float result = 0f;
            Vector3 lossyScale = collider.transform.lossyScale;

            for (int axis = 0; axis < 3; ++axis)
                result = Mathf.Max(result, Mathf.Abs(lossyScale[axis]));

            return result;
        }
    }

    [CustomEditor(typeof(SphereCollider))]
    [CanEditMultipleObjects]
    class SphereColliderEditor : Collider3DEditorBase
    {
        SerializedProperty m_Center;
        SerializedProperty m_Radius;

        public override void OnEnable()
        {
            base.OnEnable();

            m_Center = serializedObject.FindProperty("m_Center");
            m_Radius = serializedObject.FindProperty("m_Radius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Collider"), this);
            GUILayout.Space(5);
            EditorGUILayout.PropertyField(m_IsTrigger, BaseStyles.triggerContent);
            EditorGUILayout.PropertyField(m_ProvidesContacts, BaseStyles.providesContacts);
            EditorGUILayout.PropertyField(m_Material, BaseStyles.materialContent);
            EditorGUILayout.PropertyField(m_Center, BaseStyles.centerContent);
            EditorGUILayout.PropertyField(m_Radius);

            ShowLayerOverridesProperties();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
