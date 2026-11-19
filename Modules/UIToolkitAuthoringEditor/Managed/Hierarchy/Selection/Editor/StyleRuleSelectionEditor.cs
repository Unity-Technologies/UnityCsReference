// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.UIToolkit.Editor;

[CustomEditor(typeof(StyleRuleSelection))]
class StyleRuleSelectionEditor : UISelectionEditor
{
    protected override UISelectionInspectorContent CreateContent() => new StyleRuleSelectionInspectorContent(Context);
}
