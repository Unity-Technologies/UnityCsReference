// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Shared row context menu for asset tables and the inspector's reference rows.
    /// Reads the asset path from the cell's <see cref="VisualElement.userData"/> at populate
    /// time so the menu always reflects the row currently bound to that cell.
    /// </summary>
    internal static class AssetRowContextMenu
    {
        public static void AttachTo(VisualElement cell)
        {
            cell.AddManipulator(new ContextualMenuManipulator(OnPopulate));
        }

        private static void OnPopulate(ContextualMenuPopulateEvent evt)
        {
            var path = (evt.currentTarget as VisualElement)?.userData as string;
            Populate(evt.menu, path);
        }

        // Pure helper exposed for unit tests so they can assert against a fresh DropdownMenu
        // without dispatching events through the panel. The inspector's rows pass a selectInTable
        // action; the tables, which are the table, pass none and get the two-item menu.
        internal static void Populate(DropdownMenu menu, string assetPath, Action selectInTable = null)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            menu.AppendAction("Copy Path", _ => AssetActions.CopyPath(assetPath));

            var showStatus = AssetActions.CanShowInProject(assetPath)
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
            menu.AppendAction("Show in Project", _ => AssetActions.ShowInProject(assetPath), showStatus);

            if (selectInTable != null)
                menu.AppendAction("Select in Asset Table", _ => selectInTable());
        }
    }
}
