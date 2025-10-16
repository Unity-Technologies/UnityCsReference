// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

namespace UnityEditor
{
    namespace ShaderFoundry
    {
        [CustomEditor(typeof(BlockShaderSourceArtifact))]
        internal sealed partial class BlockShaderSourceArtifactInspector : Editor
        {
            static internal class Content
            {
                public static readonly GUIContent header = EditorGUIUtility.TrTextContent("Generated ShaderLab source");
            }

            public override VisualElement CreateInspectorGUI()
            {
                var inspector = new VisualElement();
                inspector.Add(new Label(Content.header.text));
                inspector.Add(new TextField()
                {
                    enabledSelf = false,
                    multiline = true,
                    bindingPath = "shaderSource"
                });
                inspector.Bind(serializedObject);
                return inspector;
            }
        }
    }
}
