// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal static class StageContextMenuUtility
{
    static readonly string k_EditFolderName = L10n.Tr("Edit");
    static readonly string k_Cut = L10n.Tr("Cut");
    static readonly string k_Copy = L10n.Tr("Copy");
    static readonly string k_Paste = L10n.Tr("Paste");
    static readonly string k_Rename = L10n.Tr("Rename");
    static readonly string k_Duplicate = L10n.Tr("Duplicate");
    static readonly string k_Delete = L10n.Tr("Delete");
    static readonly string k_SelectAll = L10n.Tr("Select All");
    static readonly string k_DeselectAll = L10n.Tr("Deselect All");
    static readonly string k_InvertSelection = L10n.Tr("Invert Selection");
    static readonly string k_SelectChildren = L10n.Tr("Select Children");
    internal static readonly string OpenInUIBuilder = L10n.Tr("Open in UI Builder");
    internal static readonly string OpenInstanceInUIBuilderInIsolation = L10n.Tr("Open Instance in UI Builder/In Isolation");
    internal static readonly string OpenInstanceInUIBuilderInContext = L10n.Tr("Open Instance in UI Builder/In Context");
    internal static readonly string OpenInstanceInIsolation = L10n.Tr("Open Instance/In Isolation");
    internal static readonly string OpenInstanceInContext = L10n.Tr("Open Instance/In Context");

    public static void PopulateMenu(HierarchyView view, in HierarchyNode node, VisualElement element, DropdownMenu menu,
        IHierarchyEditorNodeTypeHandler handler)
    {
        PopulateEditOperations(view, in node, menu, handler);
        menu.AppendSeparator();
        PopulateOpenActions(element, menu);
        menu.AppendSeparator();
        PopulateElementOperations(menu);
    }

    static void PopulateOpenActions(VisualElement element, DropdownMenu menu)
    {
        var vtaSource = element.visualTreeAssetSource
            ? element.visualTreeAssetSource
            : element.GetFirstAncestorWhere(ve => ve.visualTreeAssetSource)?.visualTreeAssetSource;

        if (vtaSource == null)
            return;

        var vea = element.visualElementAsset ?? element.GetFirstAncestorWhere(ve => ve.visualElementAsset != null)
            ?.visualElementAsset;

        // For a TemplateContainer, Open in UI Builder navigates to the template's own file, not the containing document.
        VisualTreeAsset openInBuilderVta;
        int openInBuilderSelectedId;
        if (element is TemplateContainer templateContainer)
        {
            openInBuilderVta = (templateContainer.visualElementAsset as TemplateAsset)?.ResolveTemplate() ?? templateContainer.templateSource ?? vtaSource;
            openInBuilderSelectedId = -1;
        }
        else
        {
            openInBuilderVta = vtaSource;
            openInBuilderSelectedId = vea?.id ?? -1;
        }

        var ancestorInstances = new List<TemplateAsset>();
        element.GenerateSubDocumentPath(ancestorInstances);

        PopulateOpenActions(menu, element, openInBuilderVta, openInBuilderSelectedId, ancestorInstances);
    }

    /// <summary>
    /// Appends "Open in UI Builder" and, when the element is inside a template instance, the
    /// "Open Instance…" sub-menu items for both UI Builder (In Isolation / In Context) and the
    /// staging environment (In Isolation / In Context). The "Open Instance/In Context" stage
    /// action is disabled when the element is already the active staging context or falls outside
    /// the current sub-document scope.
    /// </summary>
    internal static void PopulateOpenActions(
        DropdownMenu menu,
        VisualElement element,
        VisualTreeAsset openInBuilderVta,
        int openInBuilderSelectedId,
        List<TemplateAsset> ancestorInstances)
    {
        menu.AppendAction(
            OpenInUIBuilder,
            _ =>
            {
                if (openInBuilderVta == null)
                    return;
                var cmd = new LoadUIDocumentCommand { selectedId = openInBuilderSelectedId };
                SessionState.SetString(LoadUIDocumentCommand.CommandId, EditorJsonUtility.ToJson(cmd));
                AssetDatabase.OpenAsset(openInBuilderVta.GetEntityId());
                SessionState.EraseString(LoadUIDocumentCommand.CommandId);
            });

        if (ancestorInstances.Count == 0)
            return;

        var rootVisualTreeAsset = ancestorInstances[0].visualTreeAsset;
        var ancestorVTAs = new List<VisualTreeAsset>();

        foreach (var ancestorInstance in ancestorInstances)
        {
            var ancestorVTA = ancestorInstance.ResolveTemplate();

            if (ancestorVTA != null)
                ancestorVTAs.Add(ancestorVTA);
        }

        menu.AppendAction(
            OpenInstanceInUIBuilderInIsolation,
            _ =>
            {
                var cmd = new LoadUIDocumentCommand
                {
                    subDocumentOptions = SubDocumentOptions.Isolation, subDocuments = ancestorVTAs
                };
                SessionState.SetString(LoadUIDocumentCommand.CommandId, EditorJsonUtility.ToJson(cmd));
                AssetDatabase.OpenAsset(rootVisualTreeAsset.GetEntityId());
                SessionState.EraseString(LoadUIDocumentCommand.CommandId);
            });

        menu.AppendAction(
            OpenInstanceInUIBuilderInContext,
            _ =>
            {
                var cmd = new LoadUIDocumentCommand
                {
                    selectedId = ancestorInstances[^1].id,
                    subDocumentOptions = SubDocumentOptions.InContext,
                    subDocuments = ancestorVTAs,
                    contextInstances = ancestorInstances
                };
                SessionState.SetString(LoadUIDocumentCommand.CommandId, EditorJsonUtility.ToJson(cmd));
                AssetDatabase.OpenAsset(rootVisualTreeAsset.GetEntityId());
                SessionState.EraseString(LoadUIDocumentCommand.CommandId);
            });

        GetOpenOptions(element, ancestorInstances, out _, out var canOpenInContext);

        menu.AppendAction(
            OpenInstanceInIsolation,
            _ => VisualElementEditingStage.GoToStage(new VisualTreeAssetEditingContext(
                rootVisualTreeAsset,
                ancestorInstances.ToArray(),
                SubDocumentOptions.Isolation,
                element.GetPanelSettings()
            ), BreadcrumbBar.SeparatorStyle.Line));

        menu.AppendAction(
            OpenInstanceInContext,
            _ => VisualElementEditingStage.GoToStage(new VisualTreeAssetEditingContext(
                rootVisualTreeAsset,
                ancestorInstances.ToArray(),
                SubDocumentOptions.InContext,
                element.GetPanelSettings()
            ), BreadcrumbBar.SeparatorStyle.Arrow),
            canOpenInContext ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
    }

    static void PopulateEditOperations(HierarchyView view, in HierarchyNode node, DropdownMenu menu, IHierarchyEditorNodeTypeHandler handler)
    {
        var cutMenu = k_EditFolderName + "/" + k_Cut;
        AppendAction(menu, k_Cut, Menu.GetHotkey(cutMenu), view.OnCut, handler.CanCut(view));
        var copyMenu = k_EditFolderName + "/" + k_Copy;
        AppendAction(menu, k_Copy, Menu.GetHotkey(copyMenu), view.OnCopy, handler.CanCopy(view));
        var pasteMenu = k_EditFolderName + "/" + k_Paste;
        AppendAction(menu, k_Paste, Menu.GetHotkey(pasteMenu), view.OnPaste, handler.CanPaste(view));

        var renameMenu = k_EditFolderName + "/" + k_Rename;
        var n = node;
        AppendAction(menu, k_Rename, Menu.GetHotkey(renameMenu), () => { view.OnSetName(n); }, handler.CanSetName(view, node));

        var duplicateMenu = k_EditFolderName + "/" + k_Duplicate;
        AppendAction(menu, k_Duplicate, Menu.GetHotkey(duplicateMenu), view.OnDuplicate, handler.CanDuplicate(view));

        var deleteMenu = k_EditFolderName + "/" + k_Delete;
        AppendAction(menu, k_Delete, Menu.GetHotkey(deleteMenu), view.OnDelete, handler.CanDelete(view));

        menu.AppendSeparator();
        var selectAllMenu = k_EditFolderName + "/" + k_SelectAll;
        AppendAction(menu, k_SelectAll, Menu.GetHotkey(selectAllMenu), () => { view.SelectAll(exposedOnly:true); });
        var deselectAllMenu = k_EditFolderName + "/" + k_DeselectAll;
        AppendAction(menu, k_DeselectAll, Menu.GetHotkey(deselectAllMenu), view.DeselectAll);
        var invertMenu = k_EditFolderName + "/" + k_InvertSelection;
        AppendAction(menu, k_InvertSelection, Menu.GetHotkey(invertMenu), view.ToggleSelection);
        var selectChildrenMenu = k_EditFolderName + "/" + k_SelectChildren;
        AppendAction(menu, k_SelectChildren, Menu.GetHotkey(selectChildrenMenu), view.SelectChildrenAndExpandRecursive);
    }

    public static void PopulateElementOperations(DropdownMenu menu)
    {
        // Use the top most group priority value as default
        var previousPriority = MenuItemGenerator.k_DefaultStandardElementsPriority;
        foreach (var basePath in MenuItemGenerator.k_BaseLibraryPaths)
        {
            var itemPath = $"{MenuItemGenerator.k_MenuPrefix}/{ControlTypeInfo.k_ContextMenuPrefix}/{basePath}";
            var menuItems = Menu.GetMenuItems(itemPath, includeSeparators: false, localized: true);

            if (menuItems.Length == 0)
            {
                menu.AppendAction(
                    $"{basePath}",
                    null,
                    _ => DropdownMenuAction.Status.Disabled
                );

                continue;
            }

            foreach (var menuItem in menuItems)
            {
                // Trim out the whole prefix string and append a new one. The +1 for the substring is for the end "/".
                var contextMenuPath = $"{basePath}/{menuItem.path.Substring(itemPath.Length + 1)}";
                var hotkey = Menu.GetHotkey(menuItem.path);
                if (!string.IsNullOrEmpty(hotkey))
                    contextMenuPath += " " + hotkey;

                // Append a separator based on the menu item's priority gap (10 is the minimum range required)
                if (menuItem.priority - previousPriority >= 10)
                    menu.AppendSeparator(basePath + "/");

                previousPriority = menuItem.priority;

                var menuItemPath = menuItem.path;
                menu.AppendAction(
                    contextMenuPath,
                    _ =>
                    {
                        // Bypass the GameObject menu (which adds as a sibling) when the path
                        // resolves to a known element type — the hierarchy context menu mirrors
                        // Unity's GameObject context menu and adds as a child of the right-clicked
                        // node. Other items (e.g. "UI Library...") fall through.
                        if (MenuItemGenerator.TryGetTypeForMenuPath(menuItemPath, out var type))
                            MenuUtility.AddElementAsLastChild(type);
                        else
                            EditorApplication.ExecuteMenuItem(menuItemPath);
                    },
                    _ => DropdownMenuAction.Status.Normal
                );
            }
        }
    }

    static void AppendAction(DropdownMenu menu, string name, Action action, bool enabled = true)
    {
        AppendAction(menu, name, hotkey: null, action, enabled);
    }

    static void AppendAction(DropdownMenu menu, string name, string hotkey, Action action, bool enabled = true)
    {
        var status = enabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        if (string.IsNullOrEmpty(hotkey))
            menu.AppendAction(name, _ => action?.Invoke(), status);
        else
            menu.AppendAction($"{name} {hotkey}", _ => action?.Invoke(), status);
    }

    public static void GetOpenOptions(VisualElement element, out bool showTemplateOptions, out bool canOpenInContext)
    {
        using var _ = ListPool<TemplateAsset>.Get(out var templateAssetPath);
        element.GenerateSubDocumentPath(templateAssetPath);
        GetOpenOptions(element, templateAssetPath, out showTemplateOptions, out canOpenInContext);
    }

    static void GetOpenOptions(VisualElement element, List<TemplateAsset> templateAssetPath, out bool showTemplateOptions, out bool canOpenInContext)
    {
        var stage = StageUtility.GetCurrentStage() as VisualElementEditingStage;
        var subDocPath = stage?.Context.SubDocumentPath;
        var isTemplateContainer = element is TemplateContainer;

        showTemplateOptions = isTemplateContainer || templateAssetPath.Count > 0;
        canOpenInContext = true;

        if (subDocPath is not { Length: > 0 })
            return;

        bool matchesSubDocuments;
        if (subDocPath.Length <= templateAssetPath.Count)
        {
            matchesSubDocuments = true;
            for (var i = 0; i < subDocPath.Length; i++)
            {
                if (subDocPath[i] != templateAssetPath[i])
                {
                    matchesSubDocuments = false;
                    break;
                }
            }
        }
        else
        {
            matchesSubDocuments = false;
        }

        bool isEditedTemplateContainer = isTemplateContainer
                                         && matchesSubDocuments
                                         && subDocPath.Length == templateAssetPath.Count;

        // canOpenInContext is false when the element is not under the currently edited sub-document
        // (it's outside the staging scope), when it is the edited template container itself, or when
        // it is a plain element at the root depth of the edited template. This mirrors the Enter Staging
        // arrow visibility rules in the Hierarchy — you cannot open-in-context what is already the
        // context, nor what lives outside it.
        if (matchesSubDocuments)
        {
            if (isEditedTemplateContainer)
                canOpenInContext = false;
            else if (!isTemplateContainer && subDocPath.Length == templateAssetPath.Count)
                canOpenInContext = false;
        }
        else
        {
            canOpenInContext = false;
        }
    }
}
