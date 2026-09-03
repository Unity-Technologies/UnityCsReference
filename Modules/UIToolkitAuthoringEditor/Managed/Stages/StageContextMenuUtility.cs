// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Hierarchy;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal static class StageContextMenuUtility
{
    internal static readonly string EditFolderName = L10n.Tr("Edit", null);
    internal static readonly string Cut = L10n.Tr("Cut", null);
    internal static readonly string Copy = L10n.Tr("Copy", null);
    internal static readonly string Paste = L10n.Tr("Paste", null);
    internal static readonly string Rename = L10n.Tr("Rename", null);
    internal static readonly string Duplicate = L10n.Tr("Duplicate", null);
    internal static readonly string Delete = L10n.Tr("Delete", null);
    static readonly string k_SelectAll = L10n.Tr("Select All", null);
    static readonly string k_DeselectAll = L10n.Tr("Deselect All", null);
    static readonly string k_InvertSelection = L10n.Tr("Invert Selection", null);
    static readonly string k_SelectChildren = L10n.Tr("Select Children", null);
    internal static readonly string OpenInUIBuilder = L10n.Tr("Open in UI Builder", null);
    internal static readonly string OpenInstanceInUIBuilderInIsolation = L10n.Tr("Open Instance in UI Builder/In Isolation", null);
    internal static readonly string OpenInstanceInUIBuilderInContext = L10n.Tr("Open Instance in UI Builder/In Context", null);
    internal static readonly string OpenInstanceInIsolation = L10n.Tr("Open Instance/In Isolation", null);
    internal static readonly string OpenInstanceInContext = L10n.Tr("Open Instance/In Context", null);
    internal static readonly string UnpackTemplate = L10n.Tr("Unpack Template", null);
    internal static readonly string UnpackTemplateCompletely = L10n.Tr("Unpack Template Completely", null);
    internal static readonly string CreateTemplate = L10n.Tr("Create Template...", null);
    internal static readonly string ShowInProject = L10n.Tr("Show in Project", null);

    /// <summary>
    /// The operations <see cref="PopulateEditOperations"/> appends, in order, for the tests that assert on them.
    /// These are the bare names: the label each one is appended under also carries the shortcut of the Edit menu
    /// item it mirrors.
    /// </summary>
    internal static readonly string[] EditOperations = { Cut, Copy, Paste, Rename, Duplicate, Delete };

    public static void PopulateMenu(HierarchyView view, in HierarchyNode node, VisualElement element, DropdownMenu menu,
        IHierarchyEditorNodeTypeHandler handler)
    {
        PopulateEditOperations(view, in node, menu, handler);
        menu.AppendSeparator();
        PopulateFrameOperations(element, menu);
        menu.AppendSeparator();
        PopulateSelectionOperations(view, menu);
        menu.AppendSeparator();
        PopulateOpenActions(element, menu);
        menu.AppendSeparator();
        PopulateTemplateOperations(menu, element, CommandSources.Hierarchy);
        menu.AppendSeparator();
        PopulateElementOperations(menu);
    }

    /// <summary>
    /// The UI Stage menu for a right-click that missed every row. There is no element to act on, so it offers
    /// only what applies to the document as a whole: paste, the selection commands, and the element library.
    /// </summary>
    public static void PopulateStageMenu(HierarchyView view, DropdownMenu menu, IHierarchyEditorNodeTypeHandler handler)
    {
        AppendActionWithHotKey(menu, Paste, view.OnPaste, handler.CanPaste(view));
        menu.AppendSeparator();
        PopulateSelectionOperations(view, menu);
        menu.AppendSeparator();
        PopulateElementOperations(menu);
    }

    /// <summary>
    /// The Main Stage menu. Its edit operations are only offered while Main Stage authoring is enabled: with the
    /// setting off the scene documents are shown read-only, and are edited by opening them in a stage instead.
    /// </summary>
    /// <remarks>
    /// Unlike the UI Stage, the rows here can belong to several documents at once, and some of them — the
    /// <c>Foo.uxml</c> document rows, a control's internals — are not authored elements at all. Which of the
    /// operations that leaves available is decided per row by the handler, against the same edit flags the rest
    /// of Main Stage authoring uses, so every one of them can come out disabled.
    /// </remarks>
    public static void PopulateMainStageMenu(HierarchyView view, in HierarchyNode node, VisualElement element,
        DropdownMenu menu, IHierarchyEditorNodeTypeHandler handler)
    {
        if (UIToolkitStageUtility.IsAuthoringEnabledInMainStage)
        {
            PopulateEditOperations(view, in node, menu, handler);
            menu.AppendSeparator();
        }

        PopulateFrameOperations(element, menu);
        menu.AppendSeparator();

        IPanelComponent panelComponent;
        VisualTreeAsset vtaSource;
        VisualElementAsset vea;

        if (element is IPanelComponentRootElement rootElement)
        {
            panelComponent = rootElement.panelComponent;
            vtaSource = panelComponent.visualTreeAsset;
            vea = vtaSource?.visualTree;
        }
        else
        {
            panelComponent = element.GetFirstAncestorOfType<IPanelComponentRootElement>().panelComponent;
            vtaSource = panelComponent.visualTreeAsset;
            vea = element.visualElementAsset;

            if (vea == null)
            {
                vea = element.GetFirstAncestorWhere(ve => ve.visualElementAsset != null).visualElementAsset;
            }
        }

        var isPanelComponentRootElement = element is IPanelComponentRootElement;

        var ancestorInstances = new List<TemplateAsset>();

        element.GenerateSubDocumentPath(ancestorInstances);

        if (isPanelComponentRootElement)
        {
            menu.AppendAction(
                "Select VisualTreeAsset Asset",
                ma => { EditorGUIUtility.PingObject(ma.userData as VisualTreeAsset); },
                ma => (ma.userData as VisualTreeAsset) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled,
                vtaSource);
        }

        if (vtaSource != null)
        {
            // For a TemplateContainer, Open in UI Builder navigates to the template's own file, not the containing document.
            // The root element must be checked first: it is also a TemplateContainer but has no backing TemplateAsset.
            VisualTreeAsset openInBuilderVta;
            int openInBuilderSelectedId;
            if (isPanelComponentRootElement)
            {
                openInBuilderVta = vtaSource;
                openInBuilderSelectedId = vea?.id ?? -1;
            }
            else if (element is TemplateContainer templateContainer)
            {
                openInBuilderVta = (templateContainer.visualElementAsset as TemplateAsset)?.ResolveTemplate() ?? templateContainer.templateSource ?? vtaSource;
                openInBuilderSelectedId = -1;
            }
            else
            {
                openInBuilderVta = element.visualTreeAssetSource
                    ? element.visualTreeAssetSource
                    : element.GetFirstAncestorWhere(ve => ve.visualTreeAssetSource)?.visualTreeAssetSource ?? vtaSource;
                openInBuilderSelectedId = vea?.id ?? -1;
            }

            PopulateOpenActions(menu, element, openInBuilderVta, openInBuilderSelectedId, vea?.visualTreeAsset, ancestorInstances);

            if (ancestorInstances.Count == 0)
            {
                menu.AppendAction(
                    "Open Asset",
                    _ =>
                    {
                        VisualElementEditingStage.GoToStage(new VisualTreeAssetEditingContext(
                            vtaSource,
                            element.GetPanelSettings()
                        ), BreadcrumbBar.SeparatorStyle.Arrow);
                        UIToolkitStageUtility.RequestSelectionOnNextUpdate(new[] { vea });
                    });
            }

            var canBeReferenced = VisualElementReferenceTools.TryCreateReference(element, out var pr, out var authoringIdPath, false, true) && authoringIdPath.path.Length > 0;
            menu.AppendAction(
                "Find References In Scene",
                a =>
                {
                    var prId = pr.GetEntityId().GetHashCode();
                    var pathString = authoringIdPath.PathToCsvString(VisualElementReferenceSceneQueryEngineFilter.PathSeperatorToken);
                    var filter = $"ref={prId} {VisualElementReferenceSceneQueryEngineFilter.FilterId}=[{pathString}]";
                    SearchableEditorWindow.SetSearchText(filter, HierarchyType.GameObjects);
                },
                canBeReferenced ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            AppendShowInProject(menu, element);
        }

        if (UIToolkitStageUtility.IsAuthoringEnabledInMainStage && element != null)
        {
            menu.AppendSeparator();
            PopulateTemplateOperations(menu, element, CommandSources.Hierarchy);
        }

        menu.AppendSeparator();
        PopulateElementOperations(menu);
    }

    static void PopulateFrameOperations(VisualElement element, DropdownMenu menu)
    {
        menu.AppendAction("Frame Selection", _ => RequestFramingCommand.Execute(CommandSources.Hierarchy, element, orientToFace: false));
        menu.AppendAction("Frame and Align to View", _ => RequestFramingCommand.Execute(CommandSources.Hierarchy, element, orientToFace: true));
    }

    internal static void PopulateOpenActions(VisualElement element, DropdownMenu menu)
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

        PopulateOpenActions(menu, element, openInBuilderVta, openInBuilderSelectedId, vea?.visualTreeAsset, ancestorInstances);
        AppendShowInProject(menu, element);
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
        VisualTreeAsset openInBuilderSelectedSource,
        List<TemplateAsset> ancestorInstances)
    {
        menu.AppendAction(
            OpenInUIBuilder,
            _ =>
            {
                if (openInBuilderVta == null)
                    return;
                new LoadUIDocumentCommand
                {
                    selectedId = openInBuilderSelectedId,
                    selectedSourceDocument = openInBuilderSelectedSource,
                }.OpenInBuilder(openInBuilderVta);
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
                new LoadUIDocumentCommand
                {
                    subDocumentOptions = SubDocumentOptions.Isolation, subDocuments = ancestorVTAs
                }.OpenInBuilder(rootVisualTreeAsset);
            });

        // ancestorInstances is outermost-first and ends at the instance being selected, so its
        // enclosing instances are the rest of the list read backwards.
        var selectedInstanceIds = new List<int>();
        for (var i = ancestorInstances.Count - 2; i >= 0; --i)
            selectedInstanceIds.Add(ancestorInstances[i].id);

        menu.AppendAction(
            OpenInstanceInUIBuilderInContext,
            _ =>
            {
                new LoadUIDocumentCommand
                {
                    selectedId = ancestorInstances[^1].id,
                    selectedInstanceIds = selectedInstanceIds,
                    selectedSourceDocument = ancestorInstances[^1].visualTreeAsset,
                    subDocumentOptions = SubDocumentOptions.InContext,
                    subDocuments = ancestorVTAs,
                    contextInstances = ancestorInstances
                }.OpenInBuilder(rootVisualTreeAsset);
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
        AppendActionWithHotKey(menu, Cut, view.OnCut, handler.CanCut(view));
        AppendActionWithHotKey(menu, Copy, view.OnCopy, handler.CanCopy(view));
        AppendActionWithHotKey(menu, Paste, view.OnPaste, handler.CanPaste(view));

        var n = node;
        AppendActionWithHotKey(menu, Rename, () => { view.OnSetName(n); }, handler.CanSetName(view, node));

        AppendActionWithHotKey(menu, Duplicate, view.OnDuplicate, handler.CanDuplicate(view));
        AppendActionWithHotKey(menu, Delete, view.OnDelete, handler.CanDelete(view));
    }

    static void PopulateSelectionOperations(HierarchyView view, DropdownMenu menu)
    {
        AppendActionWithHotKey(menu, k_SelectAll, () => { view.SelectAll(exposedOnly:true); });
        AppendActionWithHotKey(menu, k_DeselectAll, view.DeselectAll);
        AppendActionWithHotKey(menu, k_InvertSelection, view.ToggleSelection);
        AppendActionWithHotKey(menu, k_SelectChildren, view.SelectChildrenAndExpandRecursive);
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

    public static void AppendAction(DropdownMenu menu, string name, string hotkey, Action action, bool enabled = true)
    {
        var status = enabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        if (string.IsNullOrEmpty(hotkey))
            menu.AppendAction(name, _ => action?.Invoke(), status);
        else
            menu.AppendAction($"{name} {hotkey}", _ => action?.Invoke(), status);
    }

    public static void AppendActionWithHotKey(DropdownMenu menu, string name, Action action, bool enabled = true)
        => AppendAction(menu, name, Menu.GetHotkey(EditFolderName + "/" + name), action, enabled);

    /// <summary>
    /// Appends stage-specific contextual actions shared between the hierarchy and viewport context menus:
    /// Unpack Template / Unpack Template Completely (when template instance(s) are selected), or
    /// Create Template (when a single regular fully-editable element is selected).
    /// </summary>
    public static void PopulateTemplateOperations(DropdownMenu menu, VisualElement element,
        CommandSources.CommandSource source)
        => PopulateTemplateOperations(menu, element != null ? new[] { element } : null, source, includeShowInProject: false);

    /// <inheritdoc cref="PopulateTemplateOperations(DropdownMenu,VisualElement,CommandSources.CommandSource)"/>
    public static void PopulateTemplateOperations(DropdownMenu menu, IReadOnlyList<VisualElement> elements,
        CommandSources.CommandSource source, bool includeShowInProject = true)
    {
        if (elements == null || elements.Count == 0)
            return;

        using var templateHandle = ListPool<TemplateAsset>.Get(out var nearestTemplates);
        using var seenHandle = HashSetPool<TemplateAsset>.Get(out var seen);
        using var liveElementsHandle = ListPool<VisualElement>.Get(out var liveTemplateElements);

        foreach (var element in elements)
        {
            for (var current = element; current != null; current = current.parent)
            {
                if (current.visualElementAsset is TemplateAsset ta &&
                    UIToolkitStageUtility.GetEditFlags(current) == VisualElementEditFlags.FullyEditable)
                {
                    if (seen.Add(ta))
                    {
                        nearestTemplates.Add(ta);
                        liveTemplateElements.Add(current);
                    }
                    break;
                }
            }
        }

        if (nearestTemplates.Count > 0)
        {
            var templates = nearestTemplates.ToArray();
            // Capture parents before the lambda fires; the template instances are replaced by Execute.
            var liveParents = new VisualElement[liveTemplateElements.Count];
            for (var i = 0; i < liveTemplateElements.Count; i++)
                liveParents[i] = liveTemplateElements[i].hierarchy.parent;
            var canUnpack = UnpackTemplatesCommand.Validate(templates.AsSpan());

            menu.AppendAction(UnpackTemplate, _ =>
            {
                UnpackTemplatesCommand.Execute(source, templates, unpackCompletely: false);
                UIToolkitStageUtility.ScopePendingSelectionRequestsTo(liveParents);
            }, canUnpack ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction(UnpackTemplateCompletely, _ =>
            {
                UnpackTemplatesCommand.Execute(source, templates, unpackCompletely: true);
                UIToolkitStageUtility.ScopePendingSelectionRequestsTo(liveParents);
            }, canUnpack ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (includeShowInProject && nearestTemplates.Count == 1)
            {
                var linkedVta = nearestTemplates[0].ResolveTemplate();
                if (linkedVta != null)
                {
                    menu.AppendSeparator();
                    menu.AppendAction(ShowInProject, _ =>
                    {
                        Selection.activeObject = linkedVta;
                        EditorGUIUtility.PingObject(linkedVta);
                    });
                }
            }
        }
        else if (elements.Count == 1)
        {
            var element = elements[0];
            var isFullyEditable = UIToolkitStageUtility.GetEditFlags(element) == VisualElementEditFlags.FullyEditable;
            if (isFullyEditable && element.visualElementAsset?.visualTreeAsset != null)
                menu.AppendAction(CreateTemplate, _ => DoCreateTemplate(element, source));
        }
    }

    static void DoCreateTemplate(VisualElement element, CommandSources.CommandSource source)
    {
        var vea = element.visualElementAsset;
        var originalVta = vea?.visualTreeAsset;
        if (vea == null || originalVta == null)
            return;

        var defaultName = vea.TryGetAttributeValue("name", out var n) && !string.IsNullOrEmpty(n)
            ? n
            : element.typeName;
        var relativePath = EditorUtility.SaveFilePanelInProject("Save UXML", defaultName, "uxml",
            "Save element as a UXML template");
        if (string.IsNullOrEmpty(relativePath))
            return;

        if (relativePath == AssetDatabase.GetAssetPath(originalVta))
        {
            EditorUtility.DisplayDialog("Invalid Path", "Cannot overwrite the currently edited UXML file.", "OK");
            return;
        }

        var parentVea = vea.parentAsset as VisualElementAsset ?? originalVta.visualTree;

        using (ListPool<UxmlAsset>.Get(out var roots))
        {
            roots.Add(vea);
            var uxml = VisualTreeAssetExporter.Default.ToUxmlString(originalVta, roots, VisualTreeAssetExporter.ExportOptions.Default);
            File.WriteAllText(Path.GetFullPath(relativePath), uxml);
        }

        AssetDatabase.Refresh();

        var templateVta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(relativePath);
        if (templateVta == null)
        {
            Debug.LogError($"[UI Toolkit Authoring] Failed to load template at: {relativePath}");
            return;
        }

        var parent = element.parent;
        CreateTemplateFromElementCommand.Execute(source, vea, parentVea, templateVta);
        UIToolkitStageUtility.ScopePendingSelectionRequestsTo(parent);
    }

    static void AppendShowInProject(DropdownMenu menu, VisualElement element)
    {
        for (var current = element; current != null; current = current.parent)
        {
            if (current.visualElementAsset is TemplateAsset ta)
            {
                var linkedVta = ta.ResolveTemplate();
                if (linkedVta != null)
                    menu.AppendAction(ShowInProject, _ =>
                    {
                        Selection.activeObject = linkedVta;
                        EditorGUIUtility.PingObject(linkedVta);
                    });
                return;
            }
        }
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
