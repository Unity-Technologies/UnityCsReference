// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

internal sealed partial class VisualElementSelectionRegistry : IVisualElementChangeProcessor
{
    [AutoStaticsCleanupOnCodeReload]
    static VisualElementSelectionRegistry s_Instance;

    public static VisualElementSelectionRegistry Instance => s_Instance;

    [OnCodeInitializing]
    static void Bootstrap()
    {
        s_Instance ??= new VisualElementSelectionRegistry();
        EditorApplication.delayCall += () => s_Instance.Initialize();
    }

    [OnCodeUnloading]
    static void Teardown()
    {
        s_Instance?.Shutdown();
        s_Instance = null;
    }

    sealed class Entry
    {
        public UISelectionObject SelectionObject;
        public VisualElement Instance;
        public Panel Panel;
        public object Scope;
        public AuthoringIdPath Path;
        public bool IsStable;
    }

    // Identity scope: the owning panel component for scene documents (so multiple documents hosted in
    // one panel, or two instances of the same UXML, don't collide on the reserved root id / a shared
    // in-document path), or the panel itself for the editing stage's authoring clone (single document).
    readonly struct StableKey : IEquatable<StableKey>
    {
        public readonly object Scope;
        public readonly AuthoringIdPath Path;

        public StableKey(object scope, AuthoringIdPath path)
        {
            Scope = scope;
            Path = path;
        }

        public bool Equals(StableKey other) => ReferenceEquals(Scope, other.Scope) && Path.Equals(other.Path);
        public override bool Equals(object obj) => obj is StableKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(Scope), Path.GetHashCode());
    }

    // Reverse lookup for every currently tracked element instance (stable and ephemeral). Required
    // because a removed element may already be detached (parent == null), so its identity can no
    // longer be recomputed at removal time.
    readonly Dictionary<VisualElement, Entry> m_ElementToEntry = new();

    // Persistent identity index; the source of cross-clone selection survival.
    readonly Dictionary<StableKey, Entry> m_StableIndex = new();

    // Per-panel remap list (old instance -> new instance) computed while processing a change batch,
    // consumed by the node handlers to keep their node maps stable across re-clones.
    readonly Dictionary<Panel, List<VisualElementRemap>> m_FrameRemaps = new();
    [NoAutoStaticsCleanup] // shared empty sentinel, safe to persist
    static readonly List<VisualElementRemap> s_EmptyRemaps = new();

    readonly List<Panel> m_TrackedScenePanels = new();
    readonly List<int> m_PathBuffer = new();

    readonly HashSet<VisualElementEditingStage> m_StagePanels = new();

    bool m_Initialized;

    /// <summary>Raised when a scene panel starts being tracked. Node handlers mirror this to build nodes.</summary>
    public event Action<Panel> PanelTracked;

    /// <summary>Raised when a scene panel stops being tracked.</summary>
    public event Action<Panel> PanelUntracked;

    public IReadOnlyList<Panel> TrackedScenePanels => m_TrackedScenePanels;

    public void EnsureInitialized() => Initialize();

    void Initialize()
    {
        if (m_Initialized)
            return;
        m_Initialized = true;

        UIElementsRuntimeUtility.onCreatePanel += OnCreatePanel;
        UIElementsRuntimeUtility.onWillDestroyPanel += OnWillDestroyPanel;
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged += OnEnableInSceneAuthoringChanged;
        UIToolkitAuthoringSettings.MainStageAuthoringChanged += OnMainStageAuthoringChanged;

        if (UIToolkitAuthoringSettings.EnableInSceneUIAuthoring)
            AdoptExistingScenePanels(pingRoot: false);
    }

    void Shutdown()
    {
        if (!m_Initialized)
            return;
        m_Initialized = false;

        UIElementsRuntimeUtility.onCreatePanel -= OnCreatePanel;
        UIElementsRuntimeUtility.onWillDestroyPanel -= OnWillDestroyPanel;
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged -= OnEnableInSceneAuthoringChanged;
        UIToolkitAuthoringSettings.MainStageAuthoringChanged -= OnMainStageAuthoringChanged;

        UntrackAllScenePanels();
        UntrackAllEditablePanels();
    }

    void OnCreatePanel(IRuntimePanel panel)
    {
        if (!UIToolkitAuthoringSettings.EnableInSceneUIAuthoring)
            return;
        if (panel is BaseRuntimePanel runtimePanel && runtimePanel.ownerObject is not PanelElement.PanelOwner)
            TrackScenePanel(runtimePanel);
    }

    void OnWillDestroyPanel(IRuntimePanel panel)
    {
        if (panel is Panel p)
            UntrackScenePanel(p);
    }

    void OnEnableInSceneAuthoringChanged(bool enabled)
    {
        if (enabled)
            AdoptExistingScenePanels(pingRoot: true);
        else
            UntrackAllScenePanels();
    }

    // Scene (Main Stage) element editability is governed by EnableMainStageAuthoring, so re-evaluate every
    // live scene selection when it toggles. Re-assigning the edit flags is what notifies the inspector, so a
    // selected element's inspector locks and unlocks without a re-selection.
    void OnMainStageAuthoringChanged(bool enabled)
    {
        foreach (var pair in m_ElementToEntry)
        {
            var entry = pair.Value;
            if (ReferenceEquals(entry.Instance, pair.Key) && m_TrackedScenePanels.Contains(entry.Panel))
                RefreshSelectionObject(entry, pair.Key, entry.Panel);
        }
    }

    void AdoptExistingScenePanels(bool pingRoot)
    {
        using var _ = ListPool<Panel>.Get(out var panels);
        UIElementsUtility.GetAllPanels(panels, ContextType.Player);
        foreach (var panel in panels)
        {
            if (panel is BaseRuntimePanel runtimePanel && runtimePanel.ownerObject is not PanelElement.PanelOwner)
                TrackScenePanel(runtimePanel);
        }

        if (pingRoot)
            EditorApplication.delayCall += PingFirstTrackedRoot;
    }

    void TrackScenePanel(Panel panel)
    {
        if (m_TrackedScenePanels.Contains(panel))
            return;

        m_TrackedScenePanels.Add(panel);
        panel.RegisterChangeProcessor(this);
        PanelTracked?.Invoke(panel);
    }

    void UntrackScenePanel(Panel panel)
    {
        if (!m_TrackedScenePanels.Remove(panel))
            return;

        PanelUntracked?.Invoke(panel);
        panel.UnregisterChangeProcessor(this);
        DestroyEntriesForPanel(panel);
    }

    void UntrackAllScenePanels()
    {
        while (m_TrackedScenePanels.Count > 0)
            UntrackScenePanel(m_TrackedScenePanels[^1]);
    }

    void UntrackAllEditablePanels()
    {
        if (m_StagePanels.Count == 0)
            return;

        using var _ = ListPool<VisualElementEditingStage>.Get(out var panels);
        panels.AddRange(m_StagePanels);
        foreach (var panel in panels)
            UntrackStagePanel(panel);
    }

    void PingFirstTrackedRoot()
    {
        foreach (var panel in m_TrackedScenePanels)
        {
            var root = panel.visualTree.Q<PanelRendererRootElement>();
            var selectionObject = root?.GetSelectionObject();
            if (selectionObject)
            {
                EditorGUIUtility.PingObject(selectionObject.GetEntityId());
                return;
            }
        }
    }

    void IVisualElementChangeProcessor.BeginProcessing(BaseVisualElementPanel panel)
    {
        if (panel is not Panel p)
            return;

        ProcessSubtree(p.visualTree, p);
    }

    void IVisualElementChangeProcessor.ProcessChanges(BaseVisualElementPanel panel, AuthoringChanges changes)
    {
        if (panel is not Panel p)
            return;

        var remaps = GetOrCreateFrameRemapList(p);
        remaps.Clear();

        // Process additions/moves before removals so a same-batch re-clone reclaims the existing
        // selection object (and its EntityId) before the old instance's removal would destroy it.
        foreach (var element in changes.addedOrMovedElements)
        {
            if (IsSelectable(element))
                AddOrUpdate(element, p, remaps);
        }

        foreach (var element in changes.removedFromPanel)
            Remove(element);
    }

    void IVisualElementChangeProcessor.EndProcessing(BaseVisualElementPanel panel)
    {
        if (panel is Panel p)
            DestroyEntriesForPanel(p);
    }

    public EntityId GetOrCreateEntityId(VisualElement element)
    {
        if (element == null)
            return EntityId.None;

        if (m_ElementToEntry.TryGetValue(element, out var entry))
            return entry.SelectionObject.GetEntityId();

        if (element.panel is not Panel panel || !IsTracked(panel) || !IsSelectable(element))
            return EntityId.None;

        AddOrUpdate(element, panel, null);
        return m_ElementToEntry.TryGetValue(element, out entry)
            ? entry.SelectionObject.GetEntityId()
            : EntityId.None;
    }

    public bool TryGetSelectionObject(VisualElement element, out UISelectionObject selectionObject)
    {
        if (element != null && m_ElementToEntry.TryGetValue(element, out var entry))
        {
            selectionObject = entry.SelectionObject;
            return true;
        }

        selectionObject = null;
        return false;
    }

    /// <summary>
    /// The old-instance -> new-instance remaps produced while processing the current change batch of
    /// <paramref name="panel"/>. Node handlers apply these to keep their node maps (and thus tree
    /// expansion/selection state) stable across re-clones. Valid only within the same change batch.
    /// </summary>
    public List<VisualElementRemap> GetFrameRemaps(Panel panel)
    {
        return panel != null && m_FrameRemaps.TryGetValue(panel, out var list) ? list : s_EmptyRemaps;
    }

    public bool IsTracked(Panel panel) => panel != null && (m_TrackedScenePanels.Contains(panel) || ContainsPanel(m_StagePanels, panel));

    static bool ContainsPanel(HashSet<VisualElementEditingStage> set, Panel panel)
    {
        foreach (var stage in set)
        {
            if (stage.GetAuthoringPanel() == panel)
                return true;
        }

        return false;
    }

    public void TrackStagePanel(VisualElementEditingStage stage)
    {
        if (!stage)
            return;

        var isNew = m_StagePanels.Add(stage);
        if (isNew)
            stage.GetAuthoringPanel().RegisterChangeProcessor(this);
    }

    public void UntrackStagePanel(VisualElementEditingStage stage)
    {
        if (stage == null || !m_StagePanels.Remove(stage))
            return;

        var panel = stage.GetAuthoringPanel();
        panel.UnregisterChangeProcessor(this);
        DestroyEntriesForPanel(panel);
    }

    void ProcessSubtree(VisualElement element, Panel panel)
    {
        if (IsSelectable(element))
            AddOrUpdate(element, panel, null);

        var hierarchy = element.hierarchy;
        for (var i = 0; i < hierarchy.childCount; ++i)
            ProcessSubtree(hierarchy[i], panel);
    }

    void AddOrUpdate(VisualElement element, Panel panel, List<VisualElementRemap> remaps)
    {
        var stable = VisualElementReferenceTools.TryGetInMemoryPath(element, m_PathBuffer);

        // Already tracked instance (a plain move, or re-processing within a batch).
        if (m_ElementToEntry.TryGetValue(element, out var existing))
        {
            if (stable && existing.IsStable && !PathEquals(m_PathBuffer, existing.Path))
                RekeyEntry(existing, element, panel);

            existing.Instance = element;
            RefreshSelectionObject(existing, element, panel);
            element.SetSelectionObject(existing.SelectionObject);
            return;
        }

        if (stable)
        {
            var scope = GetScope(element, panel);
            var ids = m_PathBuffer.ToArray();
            var key = new StableKey(scope, new AuthoringIdPath(ids));

            if (m_StableIndex.TryGetValue(key, out var entry))
            {
                // Reclaim: a new clone of an already-known identity. Transfer the same selection
                // object (preserving its EntityId) from the old instance to the new one.
                var old = entry.Instance;
                if (old != null && old != element)
                {
                    remaps?.Add(new VisualElementRemap(old, element));
                    old.ClearSelectionObject();
                    m_ElementToEntry.Remove(old);
                }

                entry.Instance = element;
                m_ElementToEntry[element] = entry;
                RefreshSelectionObject(entry, element, panel);
                element.SetSelectionObject(entry.SelectionObject);
            }
            else
            {
                var selectionObject = CreateSelectionObject(element, panel);
                entry = new Entry
                {
                    SelectionObject = selectionObject,
                    Instance = element,
                    Panel = panel,
                    Scope = scope,
                    Path = key.Path,
                    IsStable = true,
                };
                m_StableIndex[key] = entry;
                m_ElementToEntry[element] = entry;
                element.SetSelectionObject(selectionObject);
            }
        }
        else
        {
            // Code-created / temporary element: no stable identity, tracked per instance and
            // destroyed on removal (no cross-clone survival).
            var selectionObject = CreateSelectionObject(element, panel);
            var entry = new Entry
            {
                SelectionObject = selectionObject,
                Instance = element,
                Panel = panel,
                IsStable = false,
            };
            m_ElementToEntry[element] = entry;
            element.SetSelectionObject(selectionObject);
        }
    }

    void Remove(VisualElement element)
    {
        if (element == null)
            return;

        if (!m_ElementToEntry.TryGetValue(element, out var entry))
        {
            element.ClearSelectionObject();
            return;
        }

        m_ElementToEntry.Remove(element);
        element.ClearSelectionObject();

        // If the entry was already reclaimed by a newer instance this batch, its Instance no longer
        // points at this element, so we keep the (reused) selection object alive.
        if (entry.Instance == element)
        {
            if (entry.IsStable)
                m_StableIndex.Remove(new StableKey(entry.Scope, entry.Path));
            DestroySelectionObject(entry.SelectionObject);
        }
    }

    void RekeyEntry(Entry entry, VisualElement element, Panel panel)
    {
        m_StableIndex.Remove(new StableKey(entry.Scope, entry.Path));
        entry.Path = new AuthoringIdPath(m_PathBuffer.ToArray());
        entry.Panel = panel;
        entry.Scope = GetScope(element, panel);
        m_StableIndex[new StableKey(entry.Scope, entry.Path)] = entry;
    }

    /// <summary>
    /// Re-files stable entries for <paramref name="panel"/> whose in-memory identity path changed with no
    /// corresponding panel change event. This happens on save: <see cref="VisualTreeAsset.HarmonizeIds"/>
    /// reissues the backing <see cref="VisualElementAsset"/> ids of the still-live tree just before it is
    /// re-cloned. Without re-filing, the re-clone can no longer match the renumbered elements to their
    /// existing entries, so the live selection (and its <see cref="EntityId"/>) is silently lost.
    ///
    /// Harmonization can move an id from one element onto another (a new id may equal another element's old
    /// id — e.g. swapping two same-typed siblings), so this rekeys in two phases: every stale key is
    /// removed before any new key is inserted, so re-filing one entry can never clobber an entry that has
    /// not been processed yet.
    /// </summary>
    public void ResyncStablePaths(Panel panel)
    {
        if (panel == null)
            return;

        using var _ = ListPool<Entry>.Get(out var rekeyed);

        // Phase 1: drop the stale key of every entry whose live path no longer matches, and stamp the
        // fresh path/scope onto the entry (but don't re-insert yet).
        foreach (var pair in m_ElementToEntry)
        {
            var element = pair.Key;
            var entry = pair.Value;

            if (!entry.IsStable || !ReferenceEquals(entry.Panel, panel) || !ReferenceEquals(entry.Instance, element))
                continue;
            if (!VisualElementReferenceTools.TryGetInMemoryPath(element, m_PathBuffer) || PathEquals(m_PathBuffer, entry.Path))
                continue;

            m_StableIndex.Remove(new StableKey(entry.Scope, entry.Path));
            entry.Path = new AuthoringIdPath(m_PathBuffer.ToArray());
            entry.Scope = GetScope(element, panel);
            rekeyed.Add(entry);
        }

        // Phase 2: re-insert under the new keys, now that no stale key can shadow them.
        foreach (var entry in rekeyed)
            m_StableIndex[new StableKey(entry.Scope, entry.Path)] = entry;
    }

    public void ResyncStablePathsForAllPanels()
    {
        foreach (var panel in m_TrackedScenePanels)
            ResyncStablePaths(panel);
        foreach (var stage in m_StagePanels)
            ResyncStablePaths(stage.GetAuthoringPanel());
    }

    UISelectionObject CreateSelectionObject(VisualElement element, Panel panel)
    {
        var editFlags = GetEditFlags(element, panel);

        UISelectionObject selectionObject;
        if (element is IPanelComponentRootElement rootElement)
        {
            var vtaSelection = ScriptableObject.CreateInstance<VisualTreeAssetSelection>();
            vtaSelection.PanelComponent = rootElement.panelComponent;
            vtaSelection.PanelSettings = rootElement.panelComponent?.panelSettings;
            selectionObject = vtaSelection;
        }
        else
        {
            var veSelection = ScriptableObject.CreateInstance<VisualElementSelection>();
            veSelection.Element = element;
            veSelection.EditFlags = editFlags;
            selectionObject = veSelection;
        }

        selectionObject.IsReadOnly = editFlags == VisualElementEditFlags.None;
        selectionObject.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
        return selectionObject;
    }

    void RefreshSelectionObject(Entry entry, VisualElement element, Panel panel)
    {
        var editFlags = GetEditFlags(element, panel);

        switch (entry.SelectionObject)
        {
            case VisualElementSelection veSelection:
                veSelection.Element = element;
                veSelection.EditFlags = editFlags;
                break;
            case VisualTreeAssetSelection vtaSelection when element is IPanelComponentRootElement rootElement:
                vtaSelection.PanelComponent = rootElement.panelComponent;
                vtaSelection.PanelSettings = rootElement.panelComponent?.panelSettings;
                break;
        }

        entry.SelectionObject.IsReadOnly = editFlags == VisualElementEditFlags.None;
    }

    void DestroyEntriesForPanel(Panel panel)
    {
        using (ListPool<VisualElement>.Get(out var toRemove))
        {
            foreach (var pair in m_ElementToEntry)
            {
                if (ReferenceEquals(pair.Value.Panel, panel))
                    toRemove.Add(pair.Key);
            }

            foreach (var element in toRemove)
            {
                if (!m_ElementToEntry.Remove(element, out var entry))
                    continue;

                element.ClearSelectionObject();
                if (entry.Instance == element)
                {
                    if (entry.IsStable)
                        m_StableIndex.Remove(new StableKey(entry.Scope, entry.Path));
                    DestroySelectionObject(entry.SelectionObject);
                }
            }
        }

        if (m_FrameRemaps.Remove(panel, out var remaps))
            ListPool<VisualElementRemap>.Release(remaps);
    }

    VisualElementEditFlags GetEditFlags(VisualElement element, Panel panel)
    {
        foreach (var kvp in m_StagePanels)
        {
            if (kvp.GetAuthoringPanel() == panel)
            {
                return kvp.Context.GetElementEditFlags(element);
            }
        }

        if (m_TrackedScenePanels.Contains(panel))
            return UIToolkitStageUtility.GetMainStageEditFlags(element);

        return VisualElementEditFlags.None;
    }

    // The identity scope: the owning panel component for scene documents, else the panel itself.
    object GetScope(VisualElement element, Panel panel)
    {
        return (object)GetRootComponent(element) ?? panel;
    }

    static IPanelComponent GetRootComponent(VisualElement element)
    {
        if (element is IPanelComponentRootElement root)
            return root.panelComponent;
        return element.GetFirstAncestorOfType<IPanelComponentRootElement>()?.panelComponent;
    }

    List<VisualElementRemap> GetOrCreateFrameRemapList(Panel panel)
    {
        if (!m_FrameRemaps.TryGetValue(panel, out var list))
        {
            list = ListPool<VisualElementRemap>.Get();
            m_FrameRemaps[panel] = list;
        }

        return list;
    }

    static bool IsSelectable(VisualElement element)
        => element != null && element is not PanelRootElement && element is not PanelElement.PanelElementRootVisualElement;

    static bool PathEquals(List<int> buffer, in AuthoringIdPath path)
    {
        var span = path.path;
        if (span.Length != buffer.Count)
            return false;
        for (var i = 0; i < buffer.Count; ++i)
        {
            if (span[i] != buffer[i])
                return false;
        }

        return true;
    }

    static void DestroySelectionObject(UISelectionObject selectionObject)
    {
        if (selectionObject == null)
            return;

        Undo.ClearUndo(selectionObject);
        Object.DestroyImmediate(selectionObject);
    }
}
