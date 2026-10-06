// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

[UxmlElement]
internal partial class Sidebar : ScrollView
{
    private SidebarRow m_CurrentlySelectedRow;
    private Foldout m_CloudFoldout;
    private Foldout m_RegistriesFoldout;

    private bool m_FoldoutsCreated = false;

    private readonly string k_FoldoutClassName = "sidebarFoldout";

    private readonly IPageManager m_PageManager;
    private readonly IPackageManagerPrefs m_PackageManagerPrefs;

    public Sidebar() : this(
        ServicesContainer.instance.Resolve<PageManager>(),
        ServicesContainer.instance.Resolve<IPackageManagerPrefs>())
    {
    }

    public Sidebar(IPageManager pageManager, IPackageManagerPrefs packageManagerPrefs)
    {
        m_PageManager = pageManager;
        m_PackageManagerPrefs = packageManagerPrefs;

        horizontalScroller.slider.tabIndex = -1;
        verticalScroller.slider.tabIndex = -1;

        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    private void CreateRowsAndFoldouts()
    {
        if (m_FoldoutsCreated)
            return;

        var projectFoldout = CreateAndAddFoldout(L10n.Tr("Project", null));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectPage.k_Id)));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectUpdatesPage.k_Id)));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(SamplesPage.k_Id)));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectNonCompliancePage.k_Id)));
        projectFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(InProjectErrorsAndWarningsPage.k_Id)));

        var sourcesFoldout = CreateAndAddFoldout(L10n.Tr("Sources", null));
        sourcesFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(MyAssetsPage.k_Id)));
        sourcesFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(UnityRegistryPage.k_Id)));
        sourcesFoldout.Add(CreateSidebarRow(m_PageManager.GetPage(BuiltInPage.k_Id)));

        m_CloudFoldout = CreateAndAddFoldout(L10n.Tr("Cloud", null));
        m_RegistriesFoldout = CreateAndAddFoldout(L10n.Tr("My Registries", null));

        m_FoldoutsCreated = true;
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        CreateRowsAndFoldouts();

        m_PageManager.onActivePageChanged += OnActivePageChanged;
        m_PageManager.onExtensionPagesChanged += UpdateExtensionPageRelatedRows;
        m_PageManager.onScopedRegistryPagesChanged += UpdateScopedRegistryRelatedRows;
        m_PageManager.onStateChanged += OnStateChanged;

        // Arrow keys go through NavigationMoveEvent to match UI Toolkit convention; KeyDown covers the rest (Page up/down etc).
        RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        RegisterCallback<NavigationMoveEvent>(OnNavigationMoveShortcut, TrickleDown.TrickleDown);
        RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);

        UpdateExtensionPageRelatedRows();
        UpdateScopedRegistryRelatedRows();

        var foldouts = Children().FilterByType<Foldout>().ToNewArray(childCount);
        if (m_PackageManagerPrefs.orderedSidebarFoldoutsExpandedStatus?.Length == foldouts.Length)
            for (var i = 0; i < foldouts.Length; i++)
                foldouts[i].value = m_PackageManagerPrefs.orderedSidebarFoldoutsExpandedStatus[i];

        OnActivePageChanged(m_PageManager.activePage);
    }

    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        m_PageManager.onActivePageChanged -= OnActivePageChanged;
        m_PageManager.onExtensionPagesChanged -= UpdateExtensionPageRelatedRows;
        m_PageManager.onScopedRegistryPagesChanged -= UpdateScopedRegistryRelatedRows;
        m_PageManager.onStateChanged -= OnStateChanged;

        UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        UnregisterCallback<NavigationMoveEvent>(OnNavigationMoveShortcut, TrickleDown.TrickleDown);
        UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);

        m_PackageManagerPrefs.orderedSidebarFoldoutsExpandedStatus = Children().FilterByType<Foldout>().SelectAsEnumerable(i => i.value).ToNewArray(childCount);
    }

    private Foldout CreateAndAddFoldout(string foldoutName)
    {
        var foldout = new Foldout {text = foldoutName};
        foldout.AddToClassList(k_FoldoutClassName);
        foldout.tooltip = foldoutName;
        foldout.tabIndex = -1;
        var toggle = foldout.Q<Toggle>();
        if (toggle != null)
            toggle.tabIndex = -1;
        Add(foldout);
        return foldout;
    }

    private SidebarRow CreateSidebarRow(IPage page)
    {
        var pageId = page.id;
        var sidebarRow = new SidebarRow(page.id, page.displayName, page.icon);
        sidebarRow.OnLeftClick(() => OnRowClick(pageId));
        UIUtils.SetElementDisplay(sidebarRow, page.visible);
        return sidebarRow;
    }

    private void OnRowClick(string pageId)
    {
        if (pageId == m_PageManager.activePage.id)
            return;

        m_PageManager.activePage = m_PageManager.GetPage(pageId);
        PackageManagerWindowAnalytics.SendEvent("changeFilter");
    }

    private void OnActivePageChanged(IPage page)
    {
        m_CurrentlySelectedRow?.SetSelected(false);
        m_CurrentlySelectedRow = GetRow(page.id);
        m_CurrentlySelectedRow?.SetSelected(true);

        if (m_CurrentlySelectedRow != null && SidebarHasKeyboardFocus())
            m_CurrentlySelectedRow.Focus();
    }

    private bool SidebarHasKeyboardFocus()
    {
        return focusController?.focusedElement is VisualElement focused && Contains(focused);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        var target = evt.target as VisualElement;
        while (target != null && target != this)
        {
            if (target is SidebarRow or Foldout)
                return;
            target = target.parent;
        }
        focusController?.IgnoreEvent(evt);
        m_CurrentlySelectedRow?.Focus();
    }

    private void OnNavigationMoveShortcut(NavigationMoveEvent evt)
    {
        if (!UIUtils.IsElementVisible(this))
            return;

        if (evt.target is SidebarRow currentRow)
        {
            if (evt.direction is NavigationMoveEvent.Direction.Up or NavigationMoveEvent.Direction.Down)
            {
                var next = FindNextVisibleRow(currentRow, evt.direction == NavigationMoveEvent.Direction.Up);
                if (next != null)
                {
                    ActivateRow(next);
                    evt.StopPropagation();
                }
            }
        }
    }

    private void OnKeyDownShortcut(KeyDownEvent evt)
    {
        if (!UIUtils.IsElementVisible(this) || evt.target is not SidebarRow)
            return;

        switch (evt.keyCode)
        {
            case KeyCode.Home:
                if (SelectFirst(reverse: false))
                    evt.StopPropagation();
                break;
            case KeyCode.End:
                if (SelectFirst(reverse: true))
                    evt.StopPropagation();
                break;
            case KeyCode.PageUp:
                if (SelectByPage(reverse: true))
                    evt.StopPropagation();
                break;
            case KeyCode.PageDown:
                if (SelectByPage(reverse: false))
                    evt.StopPropagation();
                break;
            // Without StopPropagation, macOS plays an "unhandled key" beep for these.
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
            case KeyCode.Space:
            case KeyCode.DownArrow:
            case KeyCode.UpArrow:
                evt.StopPropagation();
                break;
        }
    }

    private void ActivateRow(SidebarRow row)
    {
        OnRowClick(row.pageId);
        row.Focus();
    }

    private bool SelectByPage(bool reverse)
    {
        if (m_CurrentlySelectedRow == null)
            return SelectFirst(reverse);

        var pageSize = PageStepCount();
        var target = m_CurrentlySelectedRow;
        for (var i = 0; i < pageSize; i++)
        {
            var next = FindNextVisibleRow(target, reverse);
            if (next == null || next == target)
                break;
            target = next;
        }

        if (target == m_CurrentlySelectedRow)
            return false;

        ActivateRow(target);
        return true;
    }

    private bool SelectFirst(bool reverse)
    {
        var target = FindFirstVisibleRow(reverse);
        if (target == null || target == m_CurrentlySelectedRow)
            return false;
        ActivateRow(target);
        return true;
    }

    private int PageStepCount()
    {
        var rowHeight = m_CurrentlySelectedRow?.resolvedStyle.height ?? 0f;
        if (rowHeight <= 0f)
            return 1;
        // We do a `-1` to an estimated page size because we overlap by approximately one row when paging so the previous edge row stays as an anchor.
        return System.Math.Max(1, Mathf.FloorToInt(worldBound.height / rowHeight) - 1);
    }

    // The internal modifier is used (instead of private) to give our test project access to these properties/methods
    internal IEnumerable<SidebarRow> EnumerateVisibleRows()
    {
        foreach (var foldout in Children().FilterByType<Foldout>())
        {
            if (!foldout.value)
                continue;
            foreach (var row in foldout.Children().FilterByType<SidebarRow>())
                if (UIUtils.IsElementVisible(row))
                    yield return row;
        }
    }

    private SidebarRow FindFirstVisibleRow(bool reverse)
    {
        SidebarRow result = null;
        foreach (var row in EnumerateVisibleRows())
        {
            result = row;
            if (!reverse)
                return result;
        }
        return result;
    }

    private SidebarRow FindNextVisibleRow(SidebarRow current, bool reverse)
    {
        var currentFoldout = current != null ? UIUtils.GetParentOfType<Foldout>(current) : null;
        if (currentFoldout is not { value: true } || !UIUtils.IsElementVisible(current))
            return FindFirstVisibleRow(reverse);

        var step = reverse ? -1 : 1;
        for (var i = currentFoldout.IndexOf(current) + step; i >= 0 && i < currentFoldout.childCount; i += step)
        {
            if (currentFoldout.ElementAt(i) is SidebarRow row && UIUtils.IsElementVisible(row))
                return row;
        }

        // Spill into the next expanded foldout and take its first (or last) visible row.
        for (var i = IndexOf(currentFoldout) + step; i >= 0 && i < childCount; i += step)
        {
            if (ElementAt(i) is not Foldout foldout || !foldout.value)
                continue;
            for (var j = reverse ? foldout.childCount - 1 : 0; j >= 0 && j < foldout.childCount; j += step)
            {
                if (foldout.ElementAt(j) is SidebarRow row && UIUtils.IsElementVisible(row))
                    return row;
            }
        }

        return null;
    }

    private void OnStateChanged(PageStateChangeArgs args)
    {
        var row = GetRow(args.page.id);
        if (row == null)
            return;

        if (args.visible)
            row.UpdateIcon(args.icon);
        UIUtils.SetElementDisplay(row, args.visible);
    }

    private void SyncFoldoutWithPages(Foldout foldout, IEnumerable<IPage> pages)
    {
        var oldRows = foldout.Children().FilterByType<SidebarRow>().ToNewDictionary(r => r.pageId);

        foldout.Clear();
        foreach (var page in pages)
            foldout.Add(oldRows.GetValueOrDefault(page.id) ?? CreateSidebarRow(page));

        UIUtils.SetElementDisplay(foldout, foldout.childCount > 0);
    }

    private void UpdateExtensionPageRelatedRows()
    {
        SyncFoldoutWithPages(m_CloudFoldout, m_PageManager.orderedExtensionPages);
    }

    private void UpdateScopedRegistryRelatedRows()
    {
        SyncFoldoutWithPages(m_RegistriesFoldout, m_PageManager.orderedScopedRegistryPages);
    }

    public SidebarRow GetRow(string pageId)
    {
        foreach (var foldout in Children())
            foreach (var item in foldout.Children())
                if (item is SidebarRow row && row.pageId == pageId)
                    return row;
        return null;
    }
}
