// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

using UnityEngine.VFX;
namespace UnityEditor.VFX
{
    [CustomEditor(typeof(VisualEffect))]
    [CanEditMultipleObjects]
    class VisualEffectEditorDefault : Editor
    {
        class Styles
        {
            public static GUIContent message = EditorGUIUtility.TrTextContent("The Visual Effect component requires the com.unity.visualeffectgraph package.");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(Styles.message);
        }
    }
}
