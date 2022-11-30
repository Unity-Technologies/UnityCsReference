// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class NotSupportedOnRenderPipelineInspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return new HelpBox("This component is not supported on the currently active render pipeline.", HelpBoxMessageType.Warning);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This component is not supported on the currently active render pipeline.", MessageType.Warning);
        }
    }
}
