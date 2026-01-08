// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEngine.UIElements;

using VisualElementCellToggleEditor = Unity.Hierarchy.HierarchyViewCellValueEditor<UnityEngine.UIElements.VisualElement, UnityEngine.UIElements.Toggle, bool>;

namespace Unity.UIToolkit.Editor;

internal static class VisualElementHierarchyHandlerColumns
{
    [HierarchyViewCellDescriptor(HierarchyWindowColumnActive.k_ColumnId, typeof(HierarchyVisualElementHandler))]
    internal static void CreateActiveColumnMainStage(HierarchyViewCellDescriptor desc)
    {
        CreateActiveColumn(desc);
    }

    [HierarchyViewCellDescriptor(HierarchyWindowColumnActive.k_ColumnId, typeof(VisualElementEditingNodeHandler))]
    internal static void CreateActiveColumnUIStage(HierarchyViewCellDescriptor desc)
    {
        CreateActiveColumn(desc);
    }

    internal static void CreateActiveColumn(HierarchyViewCellDescriptor desc)
    {
        desc.BindCell = cell =>
        {
            var handler = cell.Handler as VisualElementNodeTypeHandler;
            if (handler == null)
                return;

            var ed = HierarchyViewColumnUtility.CreateCellValueEditor<HierarchyNode, Toggle, bool>(
                cell.Node, cell,
                getModelValue: ed => handler.GetEnabled(ed.Cell.View, ed.Model),
                setModelValue: (ed, value) =>
                {
                    if (!handler.SetEnabled(ed.Cell.View, ed.Model, value))
                    {
                        ed.Element.SetValueWithoutNotify(handler.GetEnabled(ed.Cell.View, ed.Model));
                    }
                },
                isDefaultValue: (ed, value) => value == true);

            ed.Element.SetEnabled(false);
        };
        desc.UnbindCell = cell =>
        {
            if (cell.userData is VisualElementCellToggleEditor editor)
            {
                editor.Unbind();
            }
        };
    }
}
