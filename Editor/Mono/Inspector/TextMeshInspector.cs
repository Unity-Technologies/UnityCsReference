// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;


namespace UnityEditor
{
    [CustomEditor(typeof(TextMesh))]
    [CanEditMultipleObjects]
    internal class TextMeshInspector : Editor
    {
        SerializedProperty m_Font;

        void OnEnable()
        {
            m_Font = serializedObject.FindProperty("m_Font");
        }

        public override void OnInspectorGUI()
        {
            Font oldFont = m_Font.hasMultipleDifferentValues ? null : (m_Font.objectReferenceValue as Font);
            DrawDefaultInspector();
            Font newFont = m_Font.hasMultipleDifferentValues ? null : (m_Font.objectReferenceValue as Font);
            if (newFont != null && newFont != oldFont)
            {
                foreach (TextMesh textMesh in targets)
                {
                    var renderer = textMesh.GetComponent<MeshRenderer>();
                    if (renderer)
                        renderer.sharedMaterial = newFont.material;
                }
            }
        }
    }
}
