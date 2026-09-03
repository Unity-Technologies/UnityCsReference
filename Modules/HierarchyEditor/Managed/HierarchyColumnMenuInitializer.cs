// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: NativeHierarchyContainer not yet converted
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.HierarchyV2;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Hierarchy.Editor
{
    internal static partial class HierarchyColumnMenuInitializer
    {
        [OnCodeLoaded]
        static void Initialize()
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
