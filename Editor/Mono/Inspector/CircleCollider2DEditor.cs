// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(CircleCollider2D))]
    [CanEditMultipleObjects]
    class CircleCollider2DEditor : Collider2DEditorBase
    {
        SerializedProperty m_Radius;

        public override void OnEnable()
        {
            base.OnEnable();
            m_Radius = serializedObject.FindProperty("m_Radius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Collider"), this);

            GUILayout.Space(5);
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_Radius);

            FinalizeInspectorGUI();
        }
    }

    [EditorTool("Edit Circle Collider 2D", typeof(CircleCollider2D))]
    class CircleCollider2DTool : PrimitiveCollider2DTool<CircleCollider2D>
    {
        readonly SphereBoundsHandle m_BoundsHandle = new SphereBoundsHandle();

        protected override PrimitiveBoundsHandle boundsHandle { get { return m_BoundsHandle; } }

        protected override void CopyColliderSizeToHandle(CircleCollider2D collider)
        {
            m_BoundsHandle.radius = collider.radius * GetRadiusScaleFactor(collider);
        }

        protected override bool CopyHandleSizeToCollider(CircleCollider2D collider)
        {
            float oldRadius = collider.radius;
            float scaleFactor = GetRadiusScaleFactor(collider);
            collider.radius = Mathf.Approximately(scaleFactor, 0f) ? 0f : m_BoundsHandle.radius / GetRadiusScaleFactor(collider);

            // test for size change after using property setter in case input data was sanitized
            return collider.radius != oldRadius;
        }

        static float GetRadiusScaleFactor(CircleCollider2D collider)
        {
            Vector3 lossyScale = collider.transform.lossyScale;
            return Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
        }
    }
}
