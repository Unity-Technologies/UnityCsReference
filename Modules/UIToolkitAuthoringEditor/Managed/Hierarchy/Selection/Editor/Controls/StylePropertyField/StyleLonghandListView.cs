// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Allows <see cref="StylePropertyBinding"/> to route per-property affordance elements on composite
/// fields that own multiple sub-properties (e.g. transition and animation lists).
/// </summary>
internal interface IPropertyMappedAffordanceField
{
    void GetAffordanceElements(StylePropertyId propertyId, List<FieldAffordanceElement> elements);
}

/// <summary>
/// One longhand "column" of a <see cref="StyleLonghandListView{TData}"/>: its change-flag, binding id,
/// style id, USS name, control-level override class, backing list, default value and the projection out of
/// the per-row data record. Supplying this table lets the base drive the transfer/notify/override/affordance
/// loops generically so each control only declares its columns.
/// </summary>
sealed class LonghandDescriptor<TData>
{
    public int flag;
    public BindingId bindingId;
    public StylePropertyId stylePropertyId;
    public string ussName => new StylePropertyName(stylePropertyId).ToString();
    public string overriddenUssClassName;
    public IList backingList;
    public object defaultValue;
    public Func<TData, object> read;
    public readonly HashSet<FieldAffordanceElement> affordances = new();
}

class RowChangedEvent<TData> : EventBase<RowChangedEvent<TData>>
{
    public FoldoutLonghandField<TData> field;
    public TData data;
    public int changeType;
    public int index;

    public RowChangedEvent()
    {
        bubbles = true;
    }
}

/// <summary>
/// Base for a foldout row that edits one entry of a multi-longhand list (a transition or an animation). Owns
/// the shared per-field affordance + override-bar + context-menu wiring and the generic override refresh;
/// subclasses build the concrete widgets and project them to/from the row's data record.
/// </summary>
internal abstract class FoldoutLonghandField<TData> : OverrideFoldout
{
    readonly VisualElement m_ListView;
    readonly List<(int flag, FieldAffordanceElement affordance, OverrideBarManipulator overrideBar)> m_Longhands = new();
    readonly OverrideBarManipulator m_FoldoutOverride;

    TData m_Data;
    int m_Mask;

    public int index { get; set; }
    public TData data => m_Data;
    protected int mask => m_Mask;

    protected FoldoutLonghandField(VisualElement listView, string ussClassName, string ussPath, string ussDarkSkinPath, string ussLightSkinPath)
    {
        if (EditorGUIUtility.Load(ussPath) is StyleSheet baseSheet)
            styleSheets.Add(baseSheet);
        if (EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? ussDarkSkinPath : ussLightSkinPath) is StyleSheet skinSheet)
            styleSheets.Add(skinSheet);

        AddToClassList(ussClassName);
        m_ListView = listView;
        m_FoldoutOverride = new OverrideBarManipulator { target = listView, OverrideContainer = overrideContainer };
    }

    /// <summary>
    /// Wires the shared per-field affordance element, override bar and context menu, and adds the field to
    /// the row. The subclass creates/configures the widget (class lists, tooltip, change callback) first.
    /// </summary>
    protected void RegisterLonghand(int flag, VisualElement field)
    {
        var affordance = new FieldAffordanceElement();
        field.hierarchy.Insert(0, affordance);
        var overrideBar = new OverrideBarManipulator { target = m_ListView, OverrideContainer = field };
        var manipulator = new ContextualMenuManipulator(evt => affordance.OnContextualMenuPopulate(evt));
        manipulator.acceptClicksIfDisabled = true;
        field.AddManipulator(manipulator);
        m_Longhands.Add((flag, affordance, overrideBar));
        Add(field);
    }

    public FieldAffordanceElement GetAffordance(int flag)
    {
        foreach (var (f, affordance, _) in m_Longhands)
        {
            if (f == flag)
                return affordance;
        }
        return null;
    }

    public void SetData(TData value, int overridesMask)
    {
        m_Mask = overridesMask;
        m_Data = value;
        RefreshValues();
        RefreshOverrides();
    }

    protected abstract void RefreshValues();

    void RefreshOverrides()
    {
        m_FoldoutOverride.IsOverridden = m_Mask != 0;
        foreach (var (flag, _, overrideBar) in m_Longhands)
            overrideBar.IsOverridden = (m_Mask & flag) != 0;
    }

    protected void OnChanged<T>(ChangeEvent<T> evt, int flag, TData newData)
    {
        using var nested = RowChangedEvent<TData>.GetPooled();
        nested.elementTarget = this;
        nested.field = this;
        nested.changeType = flag;
        nested.index = index;
        nested.data = newData;
        SetData(newData, m_Mask | flag);
        SendEvent(nested);
        evt.StopPropagation();
    }
}

/// <summary>
/// Base for the authoring multi-longhand list controls (transition, animation): a <see cref="ListView"/> of
/// foldout rows over N parallel longhand lists exposed as <c>[CreateProperty]</c> on the subclass. Owns the
/// add/remove (keep-last-never-empty), transfer/notify, per-index compose (modulo-wrap), override tracking
/// and the per-longhand affordance fan-out (<see cref="IPropertyMappedAffordanceField"/>) generically, driven
/// by the subclass <see cref="Descriptors"/> table. Internals use an int bitmask; the typed change enum stays
/// on the subclass.
/// </summary>
internal abstract class StyleLonghandListView<TData> : BindableElement, IPropertyMappedAffordanceField
{
    readonly List<TData> m_Data = new();
    readonly Dictionary<string, HashSet<ITrackablePropertyProvider>> m_TrackedProviders = new();
    readonly Dictionary<int, Action<DropdownMenu>> m_ContextMenus = new();
    readonly ListView m_ListView;

    int m_OverridesMask;
    int m_AllMask = -1;
    bool m_AffordanceUpdatePending;

    protected List<TData> Data => m_Data;

    protected abstract IReadOnlyList<LonghandDescriptor<TData>> Descriptors { get; }
    protected abstract FoldoutLonghandField<TData> CreateRow();
    protected abstract TData ComposeData(int index);
    protected abstract TData MakeDefaultData();

    // Hook for an imperative host: raised after a user edit has updated the backing lists (and Refresh ran).
    // The animation subclass overrides it to emit its host change event; the authoring host has no override and
    // relies on the [CreateProperty] two-way binding instead. <paramref name="cleared"/> is true when removing
    // the last row fell back to the keep-last default (so a host that deletes-on-empty can distinguish it).
    protected virtual void RaiseHostChangeEvent(int changeType, bool structural, bool cleared) {}

    // Stores a per-longhand context-menu populator, applied to each row's affordance on bind (virtualization
    // safe). An imperative host (the UI Builder) supplies its own Unset/Set-inline/Set-variable menu here; the
    // authoring host leaves this empty and lets StylePropertyBinding own the affordance menus.
    protected void SetLonghandContextMenuCore(StylePropertyId stylePropertyId, Action<DropdownMenu> populateMenu)
    {
        foreach (var d in Descriptors)
        {
            if (d.stylePropertyId == stylePropertyId)
            {
                m_ContextMenus[d.flag] = populateMenu;
                return;
            }
        }
    }

    // Replaces a backing list's contents without rebuilding rows; the caller batches several of these and calls
    // RefreshFromExternalPush once (see the animation subclass's bulk push).
    protected static void ReplaceBackingList<T>(List<T> backing, List<T> value)
    {
        backing.Clear();
        if (value != null)
            backing.AddRange(value);
    }

    // Rebuilds after values arrive from outside the control (a data-source update or a host bulk push) and
    // clears the lock that removing the last row set, unless the incoming rows are that fallback row itself.
    protected void RefreshFromExternalPush()
    {
        Refresh();
        if (m_Data.Count != 1 || !EqualityComparer<TData>.Default.Equals(m_Data[0], MakeDefaultData()))
            m_ListView.allowRemove = true;
    }

    int AllMask
    {
        get
        {
            if (m_AllMask < 0)
            {
                m_AllMask = 0;
                foreach (var d in Descriptors)
                    m_AllMask |= d.flag;
            }
            return m_AllMask;
        }
    }

    protected int OverridesMask
    {
        get => m_OverridesMask;
        set
        {
            if (m_OverridesMask == value)
                return;
            m_OverridesMask = value;
            RefreshOverrideClasses();
        }
    }

    protected StyleLonghandListView(string ussClassName, string listViewName, string ussPath, string ussDarkSkinPath, string ussLightSkinPath)
    {
        AddToClassList(ussClassName);

        if (EditorGUIUtility.Load(ussPath) is StyleSheet baseSheet)
            styleSheets.Add(baseSheet);
        if (EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? ussDarkSkinPath : ussLightSkinPath) is StyleSheet skinSheet)
            styleSheets.Add(skinSheet);

        m_ListView = new ListView
        {
            name = listViewName,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            itemsSource = m_Data,
            makeItem = MakeRow,
            bindItem = BindRow,
            unbindItem = UnbindRow,
            allowAdd = true,
            allowRemove = true,
            showAddRemoveFooter = true,
            selectionType = SelectionType.Multiple
        };
        m_ListView.onAdd += _ => OnAdded();
        m_ListView.onRemove += OnRemoved;
        Add(m_ListView);
        RegisterCallback<TrackPropertyEvent>(evt => Track(evt.provider));
    }

    VisualElement MakeRow()
    {
        var row = CreateRow();
        row.RegisterCallback<RowChangedEvent<TData>>(OnItemChanged);
        return row;
    }

    void RefreshOverrideClasses()
    {
        foreach (var d in Descriptors)
            EnableInClassList(d.overriddenUssClassName, (m_OverridesMask & d.flag) != 0);
    }

    void Track(ITrackablePropertyProvider provider)
    {
        provider.OnTrackedPropertyChanged += PropertyChanged;
    }

    void Untrack(ITrackablePropertyProvider provider, string propertyName)
    {
        RemoveFromTrackedProperties(provider, propertyName);
        provider.OnTrackedPropertyChanged -= PropertyChanged;
    }

    void OnItemChanged(RowChangedEvent<TData> evt)
    {
        foreach (var d in Descriptors)
        {
            if ((evt.changeType & d.flag) != 0)
                Set(d, evt.index, evt.data);
        }
        Refresh();
        RaiseHostChangeEvent(evt.changeType, false, false);
    }

    void Set(LonghandDescriptor<TData> d, int index, TData value)
    {
        m_ListView.allowRemove = true;

        // Expand a CSS-repeated (shorter-than-rows) longhand to explicit per-row values before writing, so
        // editing one row does not alias onto the sibling rows that ValueAt's modulo-wrap was projecting it to.
        if ((m_OverridesMask & d.flag) == 0 || d.backingList.Count < m_Data.Count)
            Transfer(d.flag);

        d.backingList[index] = d.read(value);
        NotifyPropertyChanged(d.bindingId);
    }

    void Transfer(int mask)
    {
        foreach (var d in Descriptors)
        {
            if ((mask & d.flag) == 0)
                continue;
            d.backingList.Clear();
            for (var i = 0; i < m_Data.Count; ++i)
                d.backingList.Add(d.read(m_Data[i]));
        }
    }

    void Notify(int mask)
    {
        foreach (var d in Descriptors)
        {
            if ((mask & d.flag) != 0)
                NotifyPropertyChanged(d.bindingId);
        }
    }

    // Adds a default-valued entry to every longhand that is currently authored (or all of them when nothing
    // is set yet). Public so the inspector's "Add" affordance can drive it directly.
    internal void OnAdded()
    {
        m_ListView.allowRemove = true;

        var mask = m_OverridesMask == 0 ? AllMask : m_OverridesMask;
        foreach (var d in Descriptors)
        {
            if ((mask & d.flag) == 0)
                continue;
            d.backingList.Add(d.defaultValue);
            NotifyPropertyChanged(d.bindingId);
        }

        Refresh();
        RaiseHostChangeEvent(mask, true, false);

        // The usual GeometryChangedEvent doesn't seem to work at all here.
        schedule.Execute(ScrollToLastItem).StartingIn(20);
    }

    void ScrollToLastItem()
    {
        var sv = GetFirstAncestorOfType<ScrollView>();
        sv?.ScrollTo(m_ListView.Q<Button>(BaseListView.footerAddButtonName));
    }

    // Drives the footer-remove path directly, for a host or test that doesn't route through the ListView's
    // footer button (whose geometry may be unavailable in batch mode).
    internal void RemoveSelected() => OnRemoved(m_ListView);

    void OnRemoved(BaseListView listView)
    {
        using var _ = ListPool<int>.Get(out var selection);
        selection.AddRange(listView.selectedIds);
        selection.Sort();

        // If nothing was selected, remove the last item.
        if (selection.Count == 0)
            selection.Add(m_Data.Count - 1);

        for (var i = selection.Count - 1; i >= 0; --i)
        {
            var selectedId = selection[i];
            if (selectedId >= 0 && selectedId < m_Data.Count)
                m_Data.RemoveAt(selection[i]);
        }

        listView.ClearSelection();

        var mask = m_OverridesMask != 0 ? m_OverridesMask : AllMask;

        // Never fully remove all entries - keep a single default row so the last row and its affordances stay
        // available, matching the UI Builder.
        var cleared = false;
        if (m_Data.Count == 0)
        {
            m_Data.Add(MakeDefaultData());
            m_ListView.allowRemove = false;
            mask = AllMask;
            cleared = true;
        }

        Transfer(mask);
        Notify(mask);
        Refresh();
        RaiseHostChangeEvent(mask, true, cleared);
    }

    void BindRow(VisualElement element, int index)
    {
        if (element is not FoldoutLonghandField<TData> field)
            return;

        field.index = index;
        field.SetData(m_Data[index], m_OverridesMask);
        field.EnableInClassList("last-item", index == m_Data.Count - 1);

        foreach (var d in Descriptors)
        {
            var affordance = field.GetAffordance(d.flag);
            d.affordances.Add(affordance);
            if (affordance != null && m_ContextMenus.TryGetValue(d.flag, out var populateMenu))
                affordance.populateMenuItems = populateMenu;
        }

        ScheduleAffordanceBindingUpdate();
    }

    // StylePropertyBinding.Update runs before BindRow populates the affordance sets, so the affordance context
    // menus are never set up. Schedule a deferred MarkDirty so the bindings re-run with populated affordances.
    void ScheduleAffordanceBindingUpdate()
    {
        if (m_AffordanceUpdatePending)
            return;
        m_AffordanceUpdatePending = true;
        schedule.Execute(MarkAffordanceBindingsDirty);
    }

    void MarkAffordanceBindingsDirty()
    {
        m_AffordanceUpdatePending = false;
        foreach (var d in Descriptors)
            GetBinding(d.bindingId)?.MarkDirty();
    }

    void UnbindRow(VisualElement element, int index)
    {
        if (element is not FoldoutLonghandField<TData> field)
            return;

        field.index = -1;
        field.SetData(default, 0);

        foreach (var d in Descriptors)
            d.affordances.Remove(field.GetAffordance(d.flag));

        m_AffordanceUpdatePending = false;
    }

    protected void Refresh()
    {
        m_Data.Clear();
        var count = GetMaxCount();
        for (var i = 0; i < count; ++i)
            m_Data.Add(ComposeData(i));
        m_ListView.RefreshItems();
    }

    int GetMaxCount()
    {
        var count = 0;
        foreach (var d in Descriptors)
            count = Math.Max(count, d.backingList.Count);
        return count;
    }

    // Shared [CreateProperty] setter body: replace the backing list and rebuild.
    protected void SetBackingList<T>(List<T> backing, List<T> value)
    {
        if (AreEquivalent(backing, value))
            return;
        backing.Clear();
        if (value != null)
            backing.AddRange(value);
        RefreshFromExternalPush();
    }

    // Per-index read with CSS longhand-repetition semantics: a shorter list wraps to match the longest.
    protected static T ValueAt<T>(List<T> list, int index, T defaultValue)
    {
        if (list == null || list.Count == 0)
            return defaultValue;
        if (list.Count > index)
            return list[index];
        return list[index % list.Count];
    }

    protected static bool AreEquivalent<T>(List<T> lhs, List<T> rhs)
    {
        if (lhs == null)
            return rhs == null;
        if (rhs == null)
            return false;
        if (lhs.Count != rhs.Count)
            return false;
        for (var i = 0; i < lhs.Count; ++i)
        {
            if (!EqualityComparer<T>.Default.Equals(lhs[i], rhs[i]))
                return false;
        }
        return true;
    }

    public void GetAffordanceElements(StylePropertyId propertyId, List<FieldAffordanceElement> elements)
    {
        foreach (var d in Descriptors)
        {
            if (d.stylePropertyId == propertyId)
            {
                elements.AddRange(d.affordances);
                return;
            }
        }
    }

    void PropertyChanged(ITrackablePropertyProvider provider, string propertyName, TrackedPropertyType type)
    {
        switch (type)
        {
            case TrackedPropertyType.StopTracking:
                Untrack(provider, propertyName);
                break;
            case TrackedPropertyType.MarkOverride:
                AddToTrackedProperties(provider, propertyName);
                break;
            case TrackedPropertyType.ClearOverride:
                RemoveFromTrackedProperties(provider, propertyName);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    void AddToTrackedProperties(ITrackablePropertyProvider provider, string propertyName)
    {
        if (!m_TrackedProviders.TryGetValue(propertyName, out var providerSet))
            m_TrackedProviders[propertyName] = providerSet = new HashSet<ITrackablePropertyProvider>();
        providerSet.Add(provider);

        if (!StylePropertyUtil.cSharpNameToUssName.TryGetValue(propertyName, out var ussName))
            ussName = propertyName;
        foreach (var d in Descriptors)
        {
            if (d.ussName == ussName)
            {
                OverridesMask |= d.flag;
                break;
            }
        }
        UpdateOverriddenState();
    }

    void RemoveFromTrackedProperties(ITrackablePropertyProvider provider, string propertyName)
    {
        if (!m_TrackedProviders.TryGetValue(propertyName, out var providerSet))
            return;
        providerSet.Remove(provider);

        if (providerSet.Count == 0)
        {
            m_TrackedProviders.Remove(propertyName);
            if (!StylePropertyUtil.cSharpNameToUssName.TryGetValue(propertyName, out var ussName))
                ussName = propertyName;
            foreach (var d in Descriptors)
            {
                if (d.ussName == ussName)
                {
                    OverridesMask &= ~d.flag;
                    break;
                }
            }
        }
        UpdateOverriddenState();
    }

    void UpdateOverriddenState()
    {
        m_ListView.RefreshItems();
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
