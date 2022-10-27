// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.GraphToolsFoundation.Editor
{
    [CustomEditor(typeof(GraphAsset), true)]
    class GraphAssetInspector_Internal : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var graph = ((GraphAsset)target)?.GraphModel;
            if (graph != null)
            {
                EditorGUILayout.LabelField("Stencil Properties");

                EditorGUI.indentLevel++;
                ((Stencil)graph.Stencil)?.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }

            base.OnInspectorGUI();
        }
    }
}
