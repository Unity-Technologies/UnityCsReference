// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Inspector
{
    [CustomEditor(typeof(UIRenderer))]
    [CanEditMultipleObjects]
    class UIRendererEditor : RendererEditorBase
    {
        public override void OnInspectorGUI() { }

        public override VisualElement CreateInspectorGUI() { return null; }
    }
}
