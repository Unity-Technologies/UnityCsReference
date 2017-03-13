// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(OcclusionPortal)), CanEditMultipleObjects]
    internal class OcclusionPortalEditor : Editor
    {
        private static readonly int s_HandleControlIDHint = typeof(OcclusionPortalEditor).Name.GetHashCode();
        private readonly BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle(s_HandleControlIDHint);

        SerializedProperty m_Center;
        SerializedProperty m_Size;

        protected virtual void OnEnable()
        {
            m_Center = serializedObject.FindProperty("m_Center");
            m_Size = serializedObject.FindProperty("m_Size");

            m_BoundsHandle.SetColor(Handles.s_ColliderHandleColor);
        }

        public override void OnInspectorGUI()
        {
            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.Collider,
                "Edit Bounds",
                PrimitiveBoundsHandle.editModeButton,
                GetWorldBounds(m_Center.vector3Value, m_Size.vector3Value),
                this
                );

            base.OnInspectorGUI();
        }

        protected virtual void OnSceneGUI()
        {
            if (EditMode.editMode != EditMode.SceneViewEditMode.Collider || !EditMode.IsOwner(this))
                return;

            OcclusionPortal portal = target as OcclusionPortal;

            // this.serializedObject will not work within OnSceneGUI if multiple targets are selected
            SerializedObject so = new SerializedObject(portal);
            so.Update();

            using (new Handles.DrawingScope(portal.transform.localToWorldMatrix))
            {
                SerializedProperty centerProperty = so.FindProperty(m_Center.propertyPath);
                SerializedProperty sizeProperty = so.FindProperty(m_Size.propertyPath);
                m_BoundsHandle.center = centerProperty.vector3Value;
                m_BoundsHandle.size = sizeProperty.vector3Value;

                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    centerProperty.vector3Value = m_BoundsHandle.center;
                    sizeProperty.vector3Value = m_BoundsHandle.size;
                    so.ApplyModifiedProperties();
                }
            }
        }

        private Bounds GetWorldBounds(Vector3 center, Vector3 size)
        {
            Bounds localBounds = new Bounds(center, size);
            Vector3 max = localBounds.max;
            Vector3 min = localBounds.min;
            Matrix4x4 portalTransformMatrix = ((OcclusionPortal)target).transform.localToWorldMatrix;
            Bounds worldBounds = new Bounds(portalTransformMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z)), Vector3.zero);
            worldBounds.Encapsulate(portalTransformMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z)));
            worldBounds.Encapsulate(portalTransformMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z)));
            worldBounds.Encapsulate(portalTransformMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z)));
            worldBounds.Encapsulate(portalTransformMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z)));
            worldBounds.Encapsulate(portalTransformMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z)));
            worldBounds.Encapsulate(portalTransformMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z)));
            worldBounds.Encapsulate(portalTransformMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z)));
            worldBounds.Encapsulate(portalTransformMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z)));
            return worldBounds;
        }
    }
}
