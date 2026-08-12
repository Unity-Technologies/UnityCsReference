// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // Builds the inline documentation "?" icon shared by foldout headers in the UI Builder
    // (PersistedFoldout) and the UI Toolkit authoring style inspector (OverrideFoldout), so the
    // two stay in sync (UUM-147138).
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal static class DocumentationLinkIcon
    {
        internal const string iconName = "documentation-link-icon";

        // Inserts a "?" icon immediately after the foldout header `label`, showing `tooltip`; when
        // `url` is non-empty the icon opens it on click. No-op when `tooltip` is empty, `label` is
        // unparented, or the icon already exists. Pointer/click events are isolated so clicking the
        // icon doesn't also toggle the surrounding foldout.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static void AddAfterLabel(Label label, string tooltip, string url = null)
        {
            if (string.IsNullOrEmpty(tooltip) || label?.parent == null || label.parent.Q(iconName) != null)
                return;

            // The header label flex-grows to fill the row; stop that so the icon sits right after
            // the text instead of being pushed to the far right of the header.
            label.style.flexGrow = 0;

            var icon = new Image
            {
                name = iconName,
                image = EditorGUIUtility.IconContent("_Help").image,
                tooltip = tooltip,
                pickingMode = PickingMode.Position,
            };
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.marginLeft = 4;
            icon.style.flexShrink = 0;
            icon.style.opacity = 0.8f;
            icon.RegisterCallback<MouseEnterEvent>(_ => icon.style.opacity = 1f);
            icon.RegisterCallback<MouseLeaveEvent>(_ => icon.style.opacity = 0.8f);
            // Isolate the icon from the foldout header so clicking it doesn't also toggle the section.
            icon.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            icon.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            if (!string.IsNullOrEmpty(url))
                icon.AddManipulator(new Clickable(() => Help.BrowseURL(url)));

            label.parent.Insert(label.parent.IndexOf(label) + 1, icon);
        }
    }
}
