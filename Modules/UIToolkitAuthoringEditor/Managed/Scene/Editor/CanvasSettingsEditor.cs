// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[CustomEditor(typeof(CanvasSettings))]
sealed class CanvasSettingsEditor : UnityEditor.Editor
{
    protected override void OnHeaderGUI() { /* Intentionally left empty. */ }
    public override bool UseDefaultMargins() => false; // No artificial padding
    public override VisualElement CreateInspectorGUI() => new CanvasSettingsInspector { Settings = (CanvasSettings)target };
}
