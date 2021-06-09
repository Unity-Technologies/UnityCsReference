// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(CustomCollider2D))]
    [CanEditMultipleObjects]
    class CustomCollider2DEditor : Collider2DEditorBase
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            // Only show properties if there's a single target.
            if (targets.Length == 1)
            {
                var customCollider = target as CustomCollider2D;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("Custom Shape Count", customCollider.customShapeCount);
                EditorGUILayout.IntField("Custom Vertex Count", customCollider.customVertexCount);
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();

            FinalizeInspectorGUI();
        }
    }
}
