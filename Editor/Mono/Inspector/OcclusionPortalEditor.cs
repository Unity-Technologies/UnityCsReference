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
        private const string k_CenterPath = "m_Center";
        private const string k_SizePath = "m_Size";

        private readonly BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

        protected virtual void OnEnable()
        {
            m_BoundsHandle.SetColor(Handles.s_ColliderHandleColor);
        }

        public override void OnInspectorGUI()
        {
            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.Collider,
                "Edit Bounds",
                PrimitiveBoundsHandle.editModeButton,
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
                SerializedProperty centerProperty = so.FindProperty(k_CenterPath);
                SerializedProperty sizeProperty = so.FindProperty(k_SizePath);
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

        internal override Bounds GetWorldBoundsOfTarget(Object targetObject)
        {
            var so = new SerializedObject(targetObject);
            Vector3 center = so.FindProperty(k_CenterPath).vector3Value;
            Vector3 size = so.FindProperty(k_SizePath).vector3Value;

            var localBounds = new Bounds(center, size);
            Vector3 max = localBounds.max;
            Vector3 min = localBounds.min;
            Matrix4x4 portalTransformMatrix = ((OcclusionPortal)targetObject).transform.localToWorldMatrix;
            var worldBounds = new Bounds(portalTransformMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z)), Vector3.zero);
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
