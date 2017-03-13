// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    [CustomEditor(typeof(LightProbes))]
    class LightProbesInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            var lp = target as LightProbes;
            GUIStyle labelStyle = EditorStyles.wordWrappedMiniLabel;
            GUILayout.Label("Light probe count: " + lp.count, labelStyle);
            GUILayout.Label("Cell count: " + lp.cellCount, labelStyle);
            GUILayout.EndVertical();
        }
    }
}
