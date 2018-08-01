// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace Unity.Experimental.EditorMode
{
    [UnityEngine.Internal.ExcludeFromDocs]
    internal sealed class UnsupportedWindowOverride : EditorWindowOverride<EditorWindow>
    {
        public override void OnEnable()
        {
            InvokeOnGUIEnabled = false;
            Window.rootVisualContainer.visible = false;

            var unsupported = new VisualElement();
            Root.Add(unsupported);
            unsupported.StretchToParentSize();
            unsupported.style.alignItems = Align.Center;
            unsupported.style.justifyContent = Justify.Center;

            var label = new Label($"Unsupported in {EditorModes.CurrentModeName}");
            label.style.alignSelf = Align.Center;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            unsupported.Add(label);
            unsupported.Add(new Button(ReturnToDefault) { text = "Return to Default Mode" });
        }

        public override void OnDisable()
        {
            Window.rootVisualContainer.visible = true;
            InvokeOnGUIEnabled = true;
        }

        public override void OnBecameVisible()
        {
            Root.StretchToParentSize();
        }

        private void ReturnToDefault()
        {
            EditorModes.RequestDefaultMode();
        }
    }
}
