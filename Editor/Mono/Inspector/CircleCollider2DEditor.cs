// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(CircleCollider2D))]
    [CanEditMultipleObjects]
    internal class CircleCollider2DEditor : PrimitiveCollider2DEditor
    {
        private SerializedProperty m_Radius;
        private readonly SphereBoundsHandle m_BoundsHandle = new SphereBoundsHandle();

        public override void OnEnable()
        {
            base.OnEnable();

            m_Radius = serializedObject.FindProperty("m_Radius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorEditButtonGUI();

            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_Radius);

            serializedObject.ApplyModifiedProperties();

            FinalizeInspectorGUI();
        }

        protected override PrimitiveBoundsHandle boundsHandle { get { return m_BoundsHandle; } }

        protected override void CopyColliderSizeToHandle()
        {
            CircleCollider2D collider = (CircleCollider2D)target;
            m_BoundsHandle.radius = collider.radius * GetRadiusScaleFactor();
        }

        protected override bool CopyHandleSizeToCollider()
        {
            CircleCollider2D collider = (CircleCollider2D)target;

            float oldRadius = collider.radius;
            float scaleFactor = GetRadiusScaleFactor();
            collider.radius =
                Mathf.Approximately(scaleFactor, 0f) ? 0f : m_BoundsHandle.radius / GetRadiusScaleFactor();

            // test for size change after using property setter in case input data was sanitized
            return collider.radius != oldRadius;
        }

        private float GetRadiusScaleFactor()
        {
            Vector3 lossyScale = ((Component)target).transform.lossyScale;
            return Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
        }
    }
}
