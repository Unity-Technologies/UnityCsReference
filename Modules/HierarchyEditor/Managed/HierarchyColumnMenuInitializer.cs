// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.HierarchyV2;

namespace Unity.Hierarchy.Editor
{
    [InitializeOnLoad]
    internal static class HierarchyColumnMenuInitializer
    {
        static HierarchyColumnMenuInitializer()
        {
            HierarchyViewColumnNavigate.ShowColumnMenuCallback = ShowEditorStyleMenu;
        }

        static void ShowEditorStyleMenu(VisualElement anchorElement, MultiColumnLayoutConfiguration layoutConfig)
        {
            var dropdownMenu = new DropdownMenu();
            var columns = layoutConfig.columns;

            // Add "Resize To Fit" option
            var canResizeToFit = true;
            foreach (var col in columns)
            {
                if (!col.visible)
                    continue;

                if (columns.stretchMode == Columns.StretchMode.GrowAndFill && col.stretchable)
                {
                    canResizeToFit = false;
                    break;
                }
            }

            if (canResizeToFit)
            {
                dropdownMenu.AppendAction("Resize To Fit", _ =>
                {
                    var header = layoutConfig.header;
                    // This should work in editor context
                    header?.ResizeToFit();
                });
            }
            else
            {
                dropdownMenu.AppendAction("Resize To Fit", null, _ => DropdownMenuAction.Status.Disabled);
            }

            dropdownMenu.AppendSeparator("");

            // Add column visibility toggles
            foreach (var column in columns)
            {
                var title = column.title ?? column.name;
                if (string.IsNullOrEmpty(title))
                    continue;

                var isPrimaryColumn = !string.IsNullOrEmpty(column.name) &&
                                      !string.IsNullOrEmpty(columns.primaryColumnName) &&
                                      columns.primaryColumnName == column.name;
                var isDisabled = isPrimaryColumn || !column.optional;

                var col = column;

                if (isDisabled)
                {
                    dropdownMenu.AppendAction(title, null,
                        _ => column.visible
                            ? DropdownMenuAction.Status.Checked | DropdownMenuAction.Status.Disabled
                            : DropdownMenuAction.Status.Disabled);
                }
                else
                {
                    dropdownMenu.AppendAction(title,
                        _ => { col.visible = !col.visible; },
                        _ => col.visible ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                }
            }

            dropdownMenu.DoDisplayEditorMenu(anchorElement.worldBound, anchorElement);
        }
    }
}
