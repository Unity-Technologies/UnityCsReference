// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Discovers and live-tracks the full asset dependency graph reachable from a panel's root
/// <see cref="VisualTreeAsset"/>(s) — nested templates, referenced/theme stylesheets, and <c>@import</c>
/// chains — and reports each discovered asset to <see cref="UIAssetRegistry"/> (read-only).
/// </summary>
/// <remarks>
/// The built-in live-reload element walk only reaches instantiated templates and directly-attached
/// stylesheets, missing non-instantiated nested templates and imported stylesheets. This tracker closes that
/// gap by computing the dependency closure explicitly and registering each asset with the panel's existing
/// <see cref="ILiveReloadSystem"/> via the authoring-tracker path, reusing its dirty-count polling and
/// import notifications. When any tracked asset changes, the graph is re-walked so membership stays correct.
/// The panel's own content is watched too (see <see cref="IVisualElementChangeProcessor"/>), because which
/// documents it hosts is what the closure starts from.
/// </remarks>
sealed class PanelDependencyTracker : IVisualElementChangeProcessor
{
    readonly UIAssetRegistry m_Registry;
    readonly Panel m_Panel;
    readonly object m_Owner;
    readonly ILiveReloadSystem m_LiveReload;
    readonly Action<List<VisualTreeAsset>> m_CollectRoots;
    readonly Func<UnityEngine.Object, UIAssetAccess> m_ResolveAccess;

    readonly RegistryVtaTracker m_VtaTracker;
    readonly RegistryStyleSheetTracker m_SheetTracker;

    readonly HashSet<VisualTreeAsset> m_TrackedVtas = new();
    readonly HashSet<StyleSheet> m_TrackedSheets = new();

    bool m_RewalkScheduled;
    int m_RewalkScheduledFrame;
    bool m_Disposed;

    internal PanelDependencyTracker(UIAssetRegistry registry, Panel panel, object owner,
        Action<List<VisualTreeAsset>> collectRoots, Func<UnityEngine.Object, UIAssetAccess> resolveAccess)
    {
        m_Registry = registry;
        m_Panel = panel;
        m_Owner = owner;
        m_LiveReload = panel?.liveReloadSystem;
        m_CollectRoots = collectRoots;
        m_ResolveAccess = resolveAccess ?? (_ => UIAssetAccess.ReadOnly);
        m_VtaTracker = new RegistryVtaTracker(this);
        m_SheetTracker = new RegistryStyleSheetTracker(this);

        // A panel exists before the documents it hosts are cloned into it: a scene panel is created — and so
        // tracked, and so attached here — by its first panel component, which only adds its root element
        // afterwards. The first walk therefore legitimately finds no roots, and nothing would ever ask for
        // another one: with nothing discovered, no live-reload tracker is registered either. Watching the
        // panel's own content is what closes that loop.
        m_Panel?.RegisterChangeProcessor(this);
    }

    /// <summary>Recomputes the dependency closure and diff-applies the changes to the registry and live-reload system.</summary>
    internal void Rewalk()
    {
        if (m_Disposed)
            return;

        using var _roots = ListPool<VisualTreeAsset>.Get(out var roots);
        m_CollectRoots?.Invoke(roots);

        using var _vtas = HashSetPool<VisualTreeAsset>.Get(out var vtas);
        using var _sheets = HashSetPool<StyleSheet>.Get(out var sheets);
        foreach (var root in roots)
            Collect(root, vtas, sheets);

        // Additions (register once) and access refresh (every walk, so a changed edit target re-resolves).
        foreach (var vta in vtas)
        {
            if (m_TrackedVtas.Add(vta))
                m_LiveReload?.RegisterAuthoringTrackerForAsset(m_VtaTracker, vta);
            m_Registry.Open(vta, m_Owner, m_ResolveAccess(vta));
        }
        foreach (var sheet in sheets)
        {
            if (m_TrackedSheets.Add(sheet))
                m_LiveReload?.RegisterAuthoringTrackerForAsset(m_SheetTracker, sheet);
            m_Registry.Open(sheet, m_Owner, m_ResolveAccess(sheet));
        }

        // Removals (assets that dropped out of the closure).
        using var _removedVtas = ListPool<VisualTreeAsset>.Get(out var removedVtas);
        foreach (var vta in m_TrackedVtas)
            if (!vtas.Contains(vta))
                removedVtas.Add(vta);
        foreach (var vta in removedVtas)
        {
            m_TrackedVtas.Remove(vta);
            m_LiveReload?.UnregisterAuthoringTrackerForAsset(m_VtaTracker, vta);
            m_Registry.Close(vta, m_Owner);
        }

        using var _removedSheets = ListPool<StyleSheet>.Get(out var removedSheets);
        foreach (var sheet in m_TrackedSheets)
            if (!sheets.Contains(sheet))
                removedSheets.Add(sheet);
        foreach (var sheet in removedSheets)
        {
            m_TrackedSheets.Remove(sheet);
            m_LiveReload?.UnregisterAuthoringTrackerForAsset(m_SheetTracker, sheet);
            m_Registry.Close(sheet, m_Owner);
        }
    }

    // A tracked asset changed. Defer the re-walk: this can fire from inside the live-reload system's own
    // iteration over its tracker sets, and Rewalk registers/unregisters trackers, which would mutate those
    // sets mid-iteration.
    internal void OnDependencyChanged()
    {
        if (m_Disposed)
            return;

        // Coalesce the burst of notifications a single live-reload tick produces, but only within one frame: a
        // delayCall can be dropped without running in some editor states, and a latch that outlives its frame
        // would then stay set forever and silently ignore every later dependency change. A redundant Rewalk is
        // a harmless no-op diff.
        if (m_RewalkScheduled && m_RewalkScheduledFrame == Time.frameCount)
            return;

        m_RewalkScheduled = true;
        m_RewalkScheduledFrame = Time.frameCount;
        EditorApplication.delayCall += DeferredRewalk;
    }

    void DeferredRewalk()
    {
        m_RewalkScheduled = false;
        if (m_Disposed)
            return;
        Rewalk();
        // Any dependency change may also flip an asset's dirty state; let the registry re-evaluate.
        m_Registry.RefreshDirtyState();
    }

    // Registration is the contract's "you missed this frame's changes, process the whole tree", which is
    // exactly a walk — and, for a panel whose components had not cloned their documents in yet when it was
    // attached, the first walk that can actually see them.
    void IVisualElementChangeProcessor.BeginProcessing(BaseVisualElementPanel panel) => Rewalk();

    void IVisualElementChangeProcessor.ProcessChanges(BaseVisualElementPanel panel, AuthoringChanges changes)
    {
        // Only which documents the panel hosts decides the closure; everything reachable from one is found by
        // walking it. A panel component recreates its root element whenever it (re)clones its document, so
        // this covers a document appearing, disappearing, and being swapped for another.
        if (ContainsPanelComponentRoot(changes.addedOrMovedElements) ||
            ContainsPanelComponentRoot(changes.removedFromPanel))
            OnDependencyChanged();
    }

    void IVisualElementChangeProcessor.EndProcessing(BaseVisualElementPanel panel)
    {
    }

    static bool ContainsPanelComponentRoot(HashSet<VisualElement> elements)
    {
        foreach (var element in elements)
            if (element is IPanelComponentRootElement)
                return true;

        return false;
    }

    internal void Dispose()
    {
        if (m_Disposed)
            return;
        m_Disposed = true;

        m_Panel?.UnregisterChangeProcessor(this);

        foreach (var vta in m_TrackedVtas)
        {
            m_LiveReload?.UnregisterAuthoringTrackerForAsset(m_VtaTracker, vta);
            m_Registry.Close(vta, m_Owner);
        }
        foreach (var sheet in m_TrackedSheets)
        {
            m_LiveReload?.UnregisterAuthoringTrackerForAsset(m_SheetTracker, sheet);
            m_Registry.Close(sheet, m_Owner);
        }
        m_TrackedVtas.Clear();
        m_TrackedSheets.Clear();
    }

    static void Collect(VisualTreeAsset root, HashSet<VisualTreeAsset> vtas, HashSet<StyleSheet> sheets)
    {
        if (root == null || !vtas.Add(root))
            return;

        using var _ = ListPool<StyleSheet>.Get(out var direct);
        root.GetAllReferencedStyleSheets(direct);
        foreach (var sheet in direct)
            CollectSheet(sheet, sheets);

        // templateDependencies yields one deduped level of nested templates; recurse to pull in the whole
        // graph, including templates that are never instantiated.
        foreach (var child in root.templateDependencies)
            Collect(child, vtas, sheets);
    }

    /// <summary>
    /// Whether the document <paramref name="component"/> renders, or anything that document depends on — nested
    /// templates, referenced style sheets and their <c>@import</c> chains — is one of <paramref name="assets"/>.
    /// </summary>
    internal static bool DependsOnAny(IPanelComponent component, IReadOnlyCollection<UnityEngine.Object> assets)
    {
        var root = component?.visualTreeAsset;
        if (root == null || assets == null || assets.Count == 0)
            return false;

        using var _vtas = HashSetPool<VisualTreeAsset>.Get(out var vtas);
        using var _sheets = HashSetPool<StyleSheet>.Get(out var sheets);
        Collect(root, vtas, sheets);

        foreach (var asset in assets)
        {
            switch (asset)
            {
                case VisualTreeAsset vta when vtas.Contains(vta):
                case StyleSheet sheet when sheets.Contains(sheet):
                    return true;
            }
        }

        return false;
    }

    static void CollectSheet(StyleSheet sheet, HashSet<StyleSheet> sheets)
    {
        if (sheet == null || !sheets.Add(sheet))
            return;

        // Recurse the @import chain. imports is one level of direct imports; the sheets.Add visited-guard
        // dedupes and breaks cycles, giving the full recursive closure.
        var imports = sheet.imports;
        if (imports == null)
            return;
        foreach (var import in imports)
            CollectSheet(import.styleSheet, sheets);
    }

    /// <summary>Fills <paramref name="roots"/> with the VisualTreeAssets hosted by a panel's scene components.</summary>
    internal static void CollectPanelComponentRoots(Panel panel, List<VisualTreeAsset> roots)
        => CollectPanelComponentRoots(panel, null, roots);

    /// <summary>
    /// Fills <paramref name="assets"/> with the full dependency closure — documents, nested templates,
    /// referenced stylesheets and their <c>@import</c> chains — reachable from the panel components of
    /// <paramref name="panel"/> that <paramref name="acceptComponent"/> accepts.
    /// </summary>
    /// <remarks>
    /// A panel is shared by every component using the same <see cref="PanelSettings"/>, so its components can
    /// live in different scenes. Callers that reason about one scene (what is about to close, what stays open)
    /// need the closure of a subset of a panel's components, not of the panel as a whole.
    /// </remarks>
    internal static void CollectPanelComponentDependencies(Panel panel, Func<IPanelComponent, bool> acceptComponent,
        HashSet<UnityEngine.Object> assets)
    {
        if (panel?.visualTree == null || assets == null)
            return;

        using var rootVtasHandle = ListPool<VisualTreeAsset>.Get(out var roots);
        CollectPanelComponentRoots(panel, acceptComponent, roots);

        using var vtaHandle = HashSetPool<VisualTreeAsset>.Get(out var vtas);
        using var ssHandle = HashSetPool<StyleSheet>.Get(out var sheets);
        foreach (var root in roots)
            Collect(root, vtas, sheets);

        foreach (var vta in vtas)
            assets.Add(vta);
        foreach (var sheet in sheets)
            assets.Add(sheet);
    }

    static void CollectPanelComponentRoots(Panel panel, Func<IPanelComponent, bool> acceptComponent,
        List<VisualTreeAsset> roots)
    {
        if (panel?.visualTree == null)
            return;

        panel.visualTree.Query<VisualElement>().ForEach(element =>
        {
            if (element is not IPanelComponentRootElement rootElement)
                return;

            var component = rootElement.panelComponent;
            if (component == null || (acceptComponent != null && !acceptComponent(component)))
                return;

            var vta = component.visualTreeAsset;
            if (vta != null)
                roots.Add(vta);
        });
    }

    sealed class RegistryVtaTracker : BaseLiveReloadVisualTreeAssetTracker, IAuthoringLiveReloadAssetTracker<VisualTreeAsset>
    {
        readonly PanelDependencyTracker m_Owner;
        internal RegistryVtaTracker(PanelDependencyTracker owner) => m_Owner = owner;
        internal override void OnVisualTreeAssetChanged() => m_Owner.OnDependencyChanged();
    }

    sealed class RegistryStyleSheetTracker : LiveReloadStyleSheetAssetTracker, IAuthoringLiveReloadAssetTracker<StyleSheet>
    {
        readonly PanelDependencyTracker m_Owner;
        internal RegistryStyleSheetTracker(PanelDependencyTracker owner) => m_Owner = owner;
        public override void OnTrackedAssetChanged() => m_Owner.OnDependencyChanged();
    }
}
