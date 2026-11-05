// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Banner at the top of each tab's scrolling content while the selected build comes from another project.
    /// </summary>
    [UxmlElement]
    internal sealed partial class ImportedBuildBanner : VisualElement
    {
        public static readonly string ussClassName = "imported-build-banner";
        public static readonly string iconUssClassName = ussClassName + "__icon";
        public static readonly string labelUssClassName = ussClassName + "__label";

        internal const string k_Message =
            "<b>Imported build - </b>This report is from another project. Some actions are unavailable, such as selecting assets.";

        public ImportedBuildBanner()
        {
            AddToClassList(ussClassName);

            // Same icon as the Profiler's Load button.
            var icon = new Image { image = EditorGUIUtility.IconContent("Import").image };
            icon.AddToClassList(iconUssClassName);
            Add(icon);

            var label = new Label(k_Message) { enableRichText = true };
            label.AddToClassList(labelUssClassName);
            Add(label);

            SetVisible(false);
        }

        internal bool IsShown => style.display.value == DisplayStyle.Flex;

        public void SetVisible(bool visible)
        {
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Builds recorded before the project GUID was stored count as this project's own.
        public static bool IsImported(BuildEntry build, GUID currentProjectGUID)
        {
            return build != null && !build.ProjectGUID.Empty() && build.ProjectGUID != currentProjectGUID;
        }
    }
}
