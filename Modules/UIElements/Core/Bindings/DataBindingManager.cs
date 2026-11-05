// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Options to change the log level for warnings that occur during the update of data bindings.
    /// </summary>
    /// <remarks>
    /// This option can be changed using <see cref="Binding.SetGlobalLogLevel"/> or <see cref="Binding.SetPanelLogLevel"/>.
    /// Changing the global level won't change the log level of a panel if it already has an override.
    /// </remarks>
    public enum BindingLogLevel
    {
        /// <summary>
        /// Never log warnings.
        /// </summary>
        [InspectorName("No logs")]
        None,
        /// <summary>
        /// Log warnings only once when the result of the binding changes.
        /// </summary>
        [InspectorName("One log per result")]
        Once,
        /// <summary>
        /// Log warnings to the console when a binding is updated.
        /// </summary>
        [InspectorName("All logs")]
        All,
    }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    sealed partial class DataBindingManager : IDisposable
    {
        private  readonly List<BindingData> m_BindingDataLocalPool = new List<BindingData>(64);

        static readonly PropertyName k_RequestBindingPropertyName = "__unity-binding-request";
        static readonly BindingId k_ClearBindingsToken = "$__BindingManager--ClearAllBindings";


        [NoAutoStaticsCleanup]
        internal static BindingLogLevel globalLogLevel = BindingLogLevel.All;

        BindingLogLevel? m_LogLevel;

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal BindingLogLevel logLevel
        {
            get => m_LogLevel ?? globalLogLevel;
            set => m_LogLevel = value;
        }

        // Also used in tests.
        internal void ResetLogLevel()
        {
            m_LogLevel = null;
        }

        readonly struct BindingRequest
        {
            public readonly BindingId bindingId;
            public readonly Binding binding;
            public readonly bool shouldProcess;

            public BindingRequest(in BindingId bindingId, Binding binding, bool shouldProcess = true)
            {
                this.bindingId = bindingId;
                this.binding = binding;
                this.shouldProcess = shouldProcess;
            }

            public BindingRequest CancelRequest()
            {
                return new BindingRequest(bindingId, binding, false);
            }
        }

        // The bindings of a single element. Elements rarely have more than a few bindings, so they are searched linearly
        // and the lookup dictionary is only created for elements with many bindings.
        internal sealed class BindingDataCollection
        {
            const int k_DictionaryThreshold = 8;

            readonly List<BindingData> m_Bindings = new List<BindingData>();
            Dictionary<BindingId, BindingData> m_BindingPerId;

            // The element using this collection, null while the collection is pooled.
            public VisualElement owner { get; set; }

            public void AddBindingData(BindingData bindingData)
            {
                var bindingId = bindingData.target.bindingId;
                var index = IndexOf(bindingId);
                if (index >= 0)
                    m_Bindings.RemoveAt(index);

                m_Bindings.Add(bindingData);

                if (null != m_BindingPerId)
                    m_BindingPerId[bindingId] = bindingData;
                else if (m_Bindings.Count > k_DictionaryThreshold)
                {
                    m_BindingPerId = new Dictionary<BindingId, BindingData>(m_Bindings.Count);
                    foreach (var data in m_Bindings)
                        m_BindingPerId[data.target.bindingId] = data;
                }
            }

            public bool TryGetBindingData(in BindingId bindingId, out BindingData data)
            {
                if (null != m_BindingPerId)
                    return m_BindingPerId.TryGetValue(bindingId, out data);

                var index = IndexOf(bindingId);
                data = index >= 0 ? m_Bindings[index] : null;
                return index >= 0;
            }

            public bool RemoveBindingData(BindingData bindingData)
            {
                var index = IndexOf(bindingData.target.bindingId);
                if (index < 0)
                    return false;

                m_BindingPerId?.Remove(m_Bindings[index].target.bindingId);
                m_Bindings.RemoveAt(index);
                return true;
            }

            public List<BindingData> GetBindings()
            {
                return m_Bindings;
            }

            public int GetBindingCount()
            {
                return m_Bindings.Count;
            }

            public void Clear()
            {
                m_Bindings.Clear();
                m_BindingPerId = null;
                owner = null;
            }

            int IndexOf(in BindingId bindingId)
            {
                for (var i = 0; i < m_Bindings.Count; ++i)
                {
                    if (m_Bindings[i].target.bindingId == bindingId)
                        return i;
                }

                return -1;
            }
        }

        internal class BindingData
        {
            public long version;
            public BindingTarget target;
            public Binding binding;
            public IDataSourceProvider dataSourceProvider;

            private DataSourceContext m_LastContext;

            public object localDataSource { get; set; }
            public PropertyPath localDataSourcePath;
            public bool hasHierarchicalContext { get; private set; }
            public bool isContextResolved { get; set; }
            public DataSourceContext hierarchicalContext { get; private set; }

            public void Reset()
            {
                ++version;
                target = default;
                binding = default;
                dataSourceProvider = default;
                localDataSource = default;
                localDataSourcePath = default;
                m_LastContext = default;
                InvalidateHierarchicalContext();
                m_SourceToUILastUpdate = null;
                m_UIToSourceLastUpdate = null;
                sourceReader = null;
                sourceInfo = null;
                lastFailureMessage = null;
            }

            public void SetHierarchicalContext(in DataSourceContext hierarchical)
            {
                if (hasHierarchicalContext &&
                    hierarchicalContext.dataSource == hierarchical.dataSource &&
                    hierarchicalContext.dataSourcePath == hierarchical.dataSourcePath)
                    return;

                hierarchicalContext = hierarchical;
                hasHierarchicalContext = true;
                isContextResolved = false;
            }

            public void InvalidateHierarchicalContext()
            {
                hierarchicalContext = default;
                hasHierarchicalContext = false;
                isContextResolved = false;
            }

            public ref readonly DataSourceContext contextRef => ref m_LastContext;

            public DataSourceContext context
            {
                get => m_LastContext;
                set
                {
                    if (m_LastContext.dataSource == value.dataSource && m_LastContext.dataSourcePath == value.dataSourcePath)
                        return;

                    var previous = m_LastContext;
                    m_LastContext = value;
                    binding.OnDataSourceChanged(new DataSourceContextChanged(target.element, target.bindingId, previous, value));
                    binding.MarkDirty();
                }
            }

            public BindingResult? m_SourceToUILastUpdate;
            public BindingResult? m_UIToSourceLastUpdate;
            public BindingUpdater.SourceReadStep<object> sourceReader;
            public BindingUpdater.FailureMessage lastFailureMessage;

            // The info of the source used by the last update, only valid while its source is the resolved source.
            public SourceInfo sourceInfo;
        }

        internal readonly struct ChangesFromUI
        {
            public readonly long version;
            public readonly Binding binding;
            public readonly BindingData bindingData;

            public ChangesFromUI(BindingData bindingData)
            {
                this.bindingData = bindingData;
                this.version = bindingData.version;
                this.binding = bindingData.binding;
            }

            public bool IsValid => version == bindingData.version && binding == bindingData.binding;
        }

        internal class SourceInfo
        {
            private HashSet<PropertyPath> m_DetectedChanges;

            // The tracked source, null while the info is pooled.
            public object source { get; set; }
            public long lastVersion { get; set; }
            public int refCount { get; set; }
            public HashSet<PropertyPath> detectedChanges => m_DetectedChanges ??= new HashSet<PropertyPath>();
            public HashSet<PropertyPath> detectedChangesNoAlloc => m_DetectedChanges;

            // Per binding update state, only valid when the stamp matches the stamp of the current update.
            public int versionStamp { get; set; }
            public long versionAtUpdateStart { get; set; }
            public int changesStamp { get; set; }
        }

        [NoAutoStaticsCleanup]
        static readonly List<BindingData> s_Empty = new List<BindingData>();

        class HierarchyBindingTracker : IDisposable
        {
            class HierarchicalBindingsSorter : HierarchyTraversal
            {
                public HashSet<VisualElement> boundElements { get; set; }
                public List<VisualElement> results { get; set; }

                public override void TraverseRecursive(VisualElement element, int depth)
                {
                    if (boundElements.Count == results.Count)
                        return;

                    // Elements of the panel are flagged exactly while their bindings are tracked by its manager.
                    if ((element.flags & VisualElementFlags.HasTrackedBindings) != 0)
                        results.Add(element);

                    Recurse(element, depth);
                }
            }

            readonly BaseVisualElementPanel m_Panel;
            readonly HierarchicalBindingsSorter m_BindingSorter;
            readonly Dictionary<VisualElement, BindingDataCollection> m_BindingDataPerElement;
            readonly Stack<BindingDataCollection> m_CollectionPool = new Stack<BindingDataCollection>();
            readonly HashSet<VisualElement> m_BoundElements;
            readonly List<VisualElement> m_OrderedBindings;
            readonly List<BindingDataCollection> m_OrderedCollections = new List<BindingDataCollection>();
            bool m_IsDirty;

            public int GetTrackedElementsCount()
            {
                return m_BoundElements.Count;
            }

            public List<VisualElement> GetBoundElements()
            {
                if (m_IsDirty)
                    OrderBindings(m_Panel.visualTree);
                return m_OrderedBindings;
            }

            // Parallel to GetBoundElements, avoids looking up the collection of each element during the update.
            public List<BindingDataCollection> GetOrderedBindingCollections()
            {
                if (m_IsDirty)
                    OrderBindings(m_Panel.visualTree);
                return m_OrderedCollections;
            }

            public HashSet<VisualElement> GetUnorderedBoundElements()
            {
                return m_BoundElements;
            }

            public HierarchyBindingTracker(BaseVisualElementPanel panel)
            {
                m_Panel = panel;
                m_BindingSorter = new HierarchicalBindingsSorter();
                m_BindingDataPerElement = new Dictionary<VisualElement, BindingDataCollection>();
                m_BoundElements = new HashSet<VisualElement>();
                m_OrderedBindings = new List<VisualElement>();
                m_IsDirty = true;
            }

            public void SetDirty()
            {
                m_IsDirty = true;
            }

            public bool TryGetBindingCollection(VisualElement element, out BindingDataCollection collection)
            {
                return m_BindingDataPerElement.TryGetValue(element, out collection);
            }

            public bool IsTrackingElement(VisualElement element)
            {
                return m_BoundElements.Contains(element);
            }

            public bool IsTrackingAnyElementInHierarchy(VisualElement element, bool includeSelf)
            {
                if (m_BoundElements.Count == 0)
                    return false;

                if (includeSelf && (element.flags & VisualElementFlags.HasTrackedBindings) != 0)
                    return true;

                var count = element.hierarchy.childCount;
                for (var i = 0; i < count; ++i)
                {
                    if (IsTrackingAnyElementInHierarchy(element.hierarchy[i], true))
                        return true;
                }

                return false;
            }

            public bool IsTrackingAnyElement(IReadOnlyList<VisualElement> elements)
            {
                if (m_BoundElements.Count == 0)
                    return false;

                for (var i = 0; i < elements.Count; ++i)
                {
                    if ((elements[i].flags & VisualElementFlags.HasTrackedBindings) != 0)
                        return true;
                }

                return false;
            }

            public void StartTrackingBinding(VisualElement element, BindingData binding)
            {
                BindingDataCollection collection;
                if (m_BoundElements.Add(element))
                {
                    collection = m_CollectionPool.Count > 0 ? m_CollectionPool.Pop() : new BindingDataCollection();
                    collection.owner = element;
                    m_BindingDataPerElement.Add(element, collection);
                    element.flags |= VisualElementFlags.HasTrackedBindings;
                }
                else if (!m_BindingDataPerElement.TryGetValue(element, out collection))
                {
                    throw new InvalidOperationException("Trying to add a binding to an element which doesn't have a binding collection. This is an internal bug. Please report using `Help > Report a Bug...`");
                }

                binding.binding.MarkDirty();
                collection.AddBindingData(binding);
                SetDirty();
            }

            // Called by VisualElement.NotifyPropertyChanged for elements flagged with HasTrackedBindings.
            public void OnPropertyChanged(VisualElement target, in BindingId property)
            {
                if (!m_BindingDataPerElement.TryGetValue(target, out var collection))
                    throw new InvalidOperationException($"Trying to track property changes on a '{nameof(VisualElement)}' that is not being tracked. This is an internal bug. Please report using `Help > Report a Bug...`");

                if (!collection.TryGetBindingData(property, out var bindingData))
                    return;

                // A pending request for the same property replaces or removes the registered binding.
                var current = bindingData.binding;
                if (TryGetBindingRequest(target, property, out var requested) && requested != current)
                    return;

                if (!m_Panel.dataBindingManager.m_IgnoreUIChangesData.ShouldIgnoreChange(target, current, property))
                    m_Panel.dataBindingManager.m_DetectedChangesFromUI.Add(new ChangesFromUI(bindingData));
            }

            public void StopTrackingBinding(VisualElement element, BindingData binding)
            {
                if (m_BoundElements.Contains(element) && m_BindingDataPerElement.TryGetValue(element, out var collection))
                {
                    collection.RemoveBindingData(binding);
                    if (collection.GetBindingCount() == 0)
                        StopTrackingElement(element);
                }
                else
                {
                    throw new InvalidOperationException("Trying to remove a binding to an element which doesn't have a binding collection. This is an internal bug. Please report using `Help > Report a Bug...`");
                }

                SetDirty();
            }

            public void StopTrackingElement(VisualElement element)
            {
                if (m_BindingDataPerElement.Remove(element, out var collection))
                {
                    collection.Clear();
                    m_CollectionPool.Push(collection);
                }

                element.flags &= ~VisualElementFlags.HasTrackedBindings;
                m_BoundElements.Remove(element);
                SetDirty();
            }

            public void Dispose()
            {
                foreach (var element in m_BoundElements)
                    element.flags &= ~VisualElementFlags.HasTrackedBindings;

                m_BindingDataPerElement.Clear();
                m_CollectionPool.Clear();
                m_BoundElements.Clear();
                m_OrderedBindings.Clear();
                m_OrderedCollections.Clear();
            }

            void OrderBindings(VisualElement root)
            {
                m_OrderedBindings.Clear();
                m_BindingSorter.boundElements = m_BoundElements;
                m_BindingSorter.results = m_OrderedBindings;
                m_BindingSorter.Traverse(root);

                m_OrderedCollections.Clear();
                foreach (var element in m_OrderedBindings)
                    m_OrderedCollections.Add(m_BindingDataPerElement[element]);

                m_IsDirty = false;
            }
        }

        class HierarchyDataSourceTracker : IDisposable
        {
            private readonly List<SourceInfo> m_SourceInfosPool = new List<SourceInfo>();

            private SourceInfo GetPooledSourceInfo()
            {
                SourceInfo info;

                if (m_SourceInfosPool.Count > 0)
                {
                    info = m_SourceInfosPool[^1];
                    m_SourceInfosPool.RemoveAt(m_SourceInfosPool.Count - 1);
                }
                else
                    info = new SourceInfo();

                return info;
            }

            private void ReleasePooledSourceInfo(SourceInfo info)
            {
                info.source = null;
                info.lastVersion = long.MinValue;
                info.refCount = 0;
                info.detectedChangesNoAlloc?.Clear();
                m_SourceInfosPool.Add(info);
            }

            class InvalidateDataSourcesTraversal : HierarchyTraversal
            {
                readonly HierarchyDataSourceTracker m_DataSourceTracker;
                readonly HashSet<VisualElement> m_VisitedElements;

                public InvalidateDataSourcesTraversal(HierarchyDataSourceTracker dataSourceTracker)
                {
                    m_DataSourceTracker = dataSourceTracker;
                    m_VisitedElements = new HashSet<VisualElement>();
                }

                public HashSet<VisualElement> visitedElements => m_VisitedElements;

                public void Invalidate(List<VisualElement> addedOrMovedElements, HashSet<VisualElement> removedElements)
                {
                    m_VisitedElements.Clear();

                    for (var i = 0; i < addedOrMovedElements.Count; ++i)
                    {
                        var element = addedOrMovedElements[i];
                        Traverse(element);
                    }

                    foreach (var element in removedElements)
                    {
                        // If the element was visited as part of the addedOrMovedElements list, it means the removed
                        // element is still part of the hierarchy and was already treated .
                        if (m_VisitedElements.Contains(element))
                            continue;
                        Traverse(element);
                    }
                }

                public override void TraverseRecursive(VisualElement element, int depth)
                {
                    if (depth > 0 && null != element.dataSource)
                        return;

                    if (!m_VisitedElements.Add(element))
                        return;

                    m_DataSourceTracker.RemoveHierarchyDataSourceContextFromElement(element);

                    Recurse(element, depth);
                }
            }

            readonly DataBindingManager m_DataBindingManager;

            private readonly Dictionary<VisualElement, DataSourceContext> m_ResolvedHierarchicalDataSourceContext;

            readonly Dictionary<object, SourceInfo> m_SourceInfos;
            private readonly HashSet<object> m_SourcesToRemove;

            readonly InvalidateDataSourcesTraversal m_InvalidateResolvedDataSources;
            readonly EventHandler<BindablePropertyChangedEventArgs> m_Handler;
            readonly EventCallback<PropertyChangedEvent, VisualElement> m_VisualElementHandler;

            private class ObjectComparer : IEqualityComparer<object>
            {
                bool IEqualityComparer<object>.Equals(object x, object y)
                {
                    return ReferenceEquals(x, y) || EqualityComparer<object>.Default.Equals(x, y);
                }

                int IEqualityComparer<object>.GetHashCode(object obj)
                {
                    return RuntimeHelpers.GetHashCode(obj);
                }
            }

            public HierarchyDataSourceTracker(DataBindingManager manager)
            {
                m_DataBindingManager = manager;
                m_ResolvedHierarchicalDataSourceContext = new Dictionary<VisualElement, DataSourceContext>();
                var dataSourceComparer = new ObjectComparer();
                m_SourceInfos = new Dictionary<object, SourceInfo>(dataSourceComparer);
                m_SourcesToRemove = new HashSet<object>(dataSourceComparer);

                m_InvalidateResolvedDataSources = new InvalidateDataSourcesTraversal(this);
                m_Handler = TrackPropertyChanges;
                m_VisualElementHandler = OnVisualElementPropertyChanged;
            }

            internal void IncreaseBindingRefCount(ref BindingData bindingData)
            {
                if (null == bindingData.binding)
                    return;

                var dataSourceProvider = bindingData.dataSourceProvider;
                if (null != dataSourceProvider)
                {
                    IncreaseRefCount(dataSourceProvider.dataSource);
                    bindingData.localDataSource = dataSourceProvider.dataSource;
                }
            }

            internal void DecreaseBindingRefCount(ref BindingData bindingData)
            {
                if (null == bindingData.binding)
                    return;

                if (null != bindingData.dataSourceProvider)
                    DecreaseRefCount(bindingData.dataSourceProvider.dataSource);
            }

            internal void IncreaseRefCount(object dataSource)
            {
                if (null == dataSource)
                    return;

                // If the data source was scheduled to be removed, then the callbacks have already
                // been removed, so we should force them to be added.
                var shouldRegisterChangeTracking = m_SourcesToRemove.Remove(dataSource);

                if (!m_SourceInfos.TryGetValue(dataSource, out var info))
                {
                    m_SourceInfos[dataSource] = info = GetPooledSourceInfo();
                    info.source = dataSource;
                    shouldRegisterChangeTracking = true;
                }

                if (shouldRegisterChangeTracking)
                {
                    if (dataSource is INotifyBindablePropertyChanged notifier)
                        notifier.propertyChanged += m_Handler;

                    if (dataSource is VisualElement element)
                        element.RegisterCallback(m_VisualElementHandler, element);
                }

                ++info.refCount;
            }

            private void OnVisualElementPropertyChanged(PropertyChangedEvent evt, VisualElement element)
            {
                TrackPropertyChanges(element, evt.property);
            }

            internal void DecreaseRefCount(object dataSource)
            {
                if (null == dataSource)
                    return;

                if (!m_SourceInfos.TryGetValue(dataSource, out var info) || info.refCount == 0)
                    throw new InvalidOperationException("Trying to release a data source that isn't tracked. This is an internal bug. Please report using `Help > Report a Bug...`");


                if (info.refCount == 1)
                {
                    info.refCount = 0;
                    m_SourcesToRemove.Add(dataSource);

                    if (dataSource is INotifyBindablePropertyChanged notifier)
                        notifier.propertyChanged -= m_Handler;

                    if (dataSource is VisualElement element)
                        element.UnregisterCallback(m_VisualElementHandler);
                }
                else
                {
                    --info.refCount;
                }
            }

            public int GetRefCount(object dataSource)
            {
                return m_SourceInfos.TryGetValue(dataSource, out var info) ? info.refCount : 0;
            }

            public int GetTrackedDataSourcesCount()
            {
                return m_ResolvedHierarchicalDataSourceContext.Count;
            }

            public bool IsTrackingDataSource(VisualElement element)
            {
                return m_ResolvedHierarchicalDataSourceContext.ContainsKey(element);
            }

            public HashSet<PropertyPath> GetChangesFromSource(object dataSource)
            {
                return m_SourceInfos.TryGetValue(dataSource, out var info) ? info.detectedChangesNoAlloc : null;
            }

            public void InvalidateCachedDataSource(HashSet<VisualElement> elements, HashSet<VisualElement> removedElements)
            {
                var toInvalidate = ListPool<VisualElement>.Get();

                try
                {
                    foreach (var element in elements)
                        toInvalidate.Add(element);

                    m_InvalidateResolvedDataSources.Invalidate(toInvalidate, removedElements);

                    // Contexts must be recomputed after the whole traversal, as it can invalidate an ancestor of an already visited element.
                    UpdateHierarchicalDataSourceContexts(m_InvalidateResolvedDataSources.visitedElements);
                }
                finally
                {
                    ListPool<VisualElement>.Release(toInvalidate);
                }
            }

            public DataSourceContext GetResolvedDataSourceContext(VisualElement element, BindingData bindingData)
            {
                object localDataSource = null;
                PropertyPath localDataSourcePath = default;

                var dataSourceProvider = bindingData.dataSourceProvider;
                if (null != dataSourceProvider)
                {
                    localDataSource = dataSourceProvider.dataSource;
                    localDataSourcePath = dataSourceProvider.dataSourcePath;
                }

                if (bindingData.isContextResolved &&
                    localDataSource == bindingData.localDataSource &&
                    localDataSourcePath == bindingData.localDataSourcePath)
                    return bindingData.context;

                return ResolveDataSourceContext(element, bindingData, localDataSource, localDataSourcePath);
            }

            // Same as GetResolvedDataSourceContext, without copying the context. When the context needs to be
            // resolved, the returned reference reflects the binding data after the data source changed callbacks.
            public ref readonly DataSourceContext GetResolvedDataSourceContextRef(VisualElement element, BindingData bindingData)
            {
                object localDataSource = null;
                PropertyPath localDataSourcePath = default;

                var dataSourceProvider = bindingData.dataSourceProvider;
                if (null != dataSourceProvider)
                {
                    localDataSource = dataSourceProvider.dataSource;
                    localDataSourcePath = dataSourceProvider.dataSourcePath;
                }

                if (!bindingData.isContextResolved ||
                    localDataSource != bindingData.localDataSource ||
                    !PropertyPathEquality.Equals(localDataSourcePath, bindingData.localDataSourcePath))
                    ResolveDataSourceContext(element, bindingData, localDataSource, localDataSourcePath);

                return ref bindingData.contextRef;
            }

            DataSourceContext ResolveDataSourceContext(VisualElement element, BindingData bindingData, object localDataSource, in PropertyPath localDataSourcePath)
            {
                var lastLocalDataSource = bindingData.localDataSource;
                bindingData.localDataSource = localDataSource;
                bindingData.localDataSourcePath = localDataSourcePath;

                if (localDataSource != lastLocalDataSource)
                {
                    DecreaseRefCount(lastLocalDataSource);
                    IncreaseRefCount(localDataSource);
                }

                DataSourceContext resolvedContext;
                if (null != localDataSource)
                {
                    resolvedContext = new DataSourceContext(localDataSource, localDataSourcePath);
                }
                else
                {
                    if (!bindingData.hasHierarchicalContext)
                        bindingData.SetHierarchicalContext(GetHierarchicalDataSourceContext(element));

                    var hierarchicalContext = bindingData.hierarchicalContext;
                    resolvedContext = new DataSourceContext(
                        hierarchicalContext.dataSource,
                        PropertyPath.Combine(hierarchicalContext.dataSourcePath, localDataSourcePath));
                }

                bindingData.isContextResolved = true;
                bindingData.context = resolvedContext;
                return resolvedContext;
            }

            void UpdateHierarchicalDataSourceContexts(HashSet<VisualElement> invalidatedElements)
            {
                var bindingsTracker = m_DataBindingManager.m_BindingsTracker;
                var boundElements = bindingsTracker.GetUnorderedBoundElements();

                // Iterate over the smallest set to limit the number of lookups.
                if (invalidatedElements.Count <= boundElements.Count)
                {
                    foreach (var element in invalidatedElements)
                    {
                        if (bindingsTracker.TryGetBindingCollection(element, out var collection))
                            UpdateHierarchicalDataSourceContext(element, collection.GetBindings());
                    }
                }
                else
                {
                    foreach (var element in boundElements)
                    {
                        if (invalidatedElements.Contains(element) && bindingsTracker.TryGetBindingCollection(element, out var collection))
                            UpdateHierarchicalDataSourceContext(element, collection.GetBindings());
                    }
                }
            }

            void UpdateHierarchicalDataSourceContext(VisualElement element, List<BindingData> bindings)
            {
                if (element.panel == m_DataBindingManager.m_Panel)
                {
                    var hierarchicalContext = GetHierarchicalDataSourceContext(element);
                    for (var i = 0; i < bindings.Count; ++i)
                    {
                        bindings[i].SetHierarchicalContext(hierarchicalContext);
                    }
                }
                else
                {
                    for (var i = 0; i < bindings.Count; ++i)
                    {
                        bindings[i].InvalidateHierarchicalContext();
                    }
                }
            }

            private void TrackPropertyChanges(object sender, BindablePropertyChangedEventArgs args)
                => TrackPropertyChanges(sender, args.propertyName);

            private void TrackPropertyChanges(object sender, PropertyPath propertyPath)
            {
                if (!m_SourceInfos.TryGetValue(sender, out var info))
                    return;

                var list = info.detectedChanges;
                list.Add(propertyPath);
            }

            public SourceInfo GetSourceInfo(object source)
            {
                return null != source && m_SourceInfos.TryGetValue(source, out var info) ? info : null;
            }

            internal object GetHierarchyDataSource(VisualElement element)
            {
                return GetHierarchicalDataSourceContext(element).dataSource;
            }

            internal DataSourceContext GetHierarchicalDataSourceContext(VisualElement element)
            {
                if (m_ResolvedHierarchicalDataSourceContext.TryGetValue(element, out var context))
                    return context;

                var current = element;
                var path = default(PropertyPath);

                while (null != current)
                {
                    if (!current.isDataSourcePathEmpty)
                        path = PropertyPath.Combine(current.dataSourcePath, path);

                    if (null != current.dataSource)
                    {
                        var source = current.dataSource;
                        return m_ResolvedHierarchicalDataSourceContext[element] = new DataSourceContext(source, path);
                    }

                    current = current.hierarchy.parent;
                }

                return m_ResolvedHierarchicalDataSourceContext[element] = new DataSourceContext(null, path);
            }

            internal void RemoveHierarchyDataSourceContextFromElement(VisualElement element)
            {
                m_ResolvedHierarchicalDataSourceContext.Remove(element);
            }

            public void Dispose()
            {
                m_ResolvedHierarchicalDataSourceContext.Clear();
                m_SourcesToRemove.Clear();
                m_SourceInfosPool.Clear();
                foreach (var info in m_SourceInfos.Values)
                    info.source = null;
                m_SourceInfos.Clear();
            }

            public void ClearSourceCache()
            {
                foreach (var toRemove in m_SourcesToRemove)
                {
                    if (m_SourceInfos.TryGetValue(toRemove, out var info))
                    {
                        if (info.refCount == 0)
                        {
                            m_SourceInfos.Remove(toRemove);
                            ReleasePooledSourceInfo(info);
                        }
                        else
                        {
                            throw new InvalidOperationException("Trying to release a data source that is still being referenced. This is an internal bug. Please report using `Help > Report a Bug...`");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Trying to release a data source that isn't tracked. This is an internal bug. Please report using `Help > Report a Bug...`");
                    }
                }

                m_SourcesToRemove.Clear();
            }
        }

        readonly BaseVisualElementPanel m_Panel;
        readonly HierarchyDataSourceTracker m_DataSourceTracker;
        readonly HierarchyBindingTracker m_BindingsTracker;
        readonly List<ChangesFromUI> m_DetectedChangesFromUI;
        private IgnoreUIChangesData m_IgnoreUIChangesData;

        internal DataBindingManager(BaseVisualElementPanel panel)
        {
            m_Panel = panel;
            m_DataSourceTracker = new HierarchyDataSourceTracker(this);
            m_BindingsTracker = new HierarchyBindingTracker(panel);
            m_DetectedChangesFromUI = new List<ChangesFromUI>();
        }

        internal int GetTrackedDataSourcesCount()
        {
            return m_DataSourceTracker.GetTrackedDataSourcesCount();
        }

        internal bool IsTrackingDataSource(VisualElement element)
        {
            return m_DataSourceTracker.IsTrackingDataSource(element);
        }

        // Returns null when the source is null or not tracked.
        internal SourceInfo GetSourceInfo(object source)
        {
            return m_DataSourceTracker.GetSourceInfo(source);
        }

        // Same as GetSourceInfo, reusing the info cached on the binding data when it still belongs to the source.
        internal SourceInfo GetSourceInfo(object source, BindingData bindingData)
        {
            var info = bindingData.sourceInfo;
            if (null != info && null != source && ReferenceEquals(info.source, source))
                return info;

            info = m_DataSourceTracker.GetSourceInfo(source);
            bindingData.sourceInfo = info;
            return info;
        }

        [NoAutoStaticsCleanup]
        static readonly DataSourceContext s_NoContext = default;

        // Same as GetResolvedDataSourceContext, without copying the context.
        internal ref readonly DataSourceContext GetResolvedDataSourceContextRef(VisualElement element, BindingData bindingData)
        {
            if (element.panel != m_Panel)
                return ref s_NoContext;

            return ref m_DataSourceTracker.GetResolvedDataSourceContextRef(element, bindingData);
        }

        internal void CacheUIBindingResult(BindingData bindingData, BindingResult result)
        {
            bindingData.m_SourceToUILastUpdate = result;
        }

        internal bool TryGetLastUIBindingResult(BindingData bindingData, out BindingResult result)
        {
            if (bindingData.m_SourceToUILastUpdate.HasValue)
            {
                result = bindingData.m_SourceToUILastUpdate.Value;
                return true;
            }

            result = default;
            return false;
        }

        internal void CacheSourceBindingResult(BindingData bindingData, BindingResult result)
        {
            bindingData.m_UIToSourceLastUpdate = result;
        }

        internal bool TryGetLastSourceBindingResult(BindingData bindingData, out BindingResult result)
        {
            if (bindingData.m_UIToSourceLastUpdate.HasValue)
            {
                result = bindingData.m_UIToSourceLastUpdate.Value;
                return true;
            }

            result = default;
            return false;
        }

        internal DataSourceContext GetResolvedDataSourceContext(VisualElement element, BindingData bindingData)
        {
            return element.panel == m_Panel
                ? m_DataSourceTracker.GetResolvedDataSourceContext(element, bindingData)
                : default;
        }

        internal bool TryGetSource(VisualElement element, out object dataSource)
        {
            if (element.panel == m_Panel)
            {
                dataSource = m_DataSourceTracker.GetHierarchyDataSource(element);
                return true;
            }

            dataSource = null;
            return false;
        }

        // Internal for tests
        internal object TrackHierarchyDataSource(VisualElement element)
        {
            return element.panel == m_Panel
                ? m_DataSourceTracker.GetHierarchicalDataSourceContext(element).dataSource
                : null;
        }


        // Internal for tests
        internal int GetRefCount(object dataSource)
        {
            return m_DataSourceTracker.GetRefCount(dataSource);
        }

        internal int GetBoundElementsCount()
        {
            return m_BindingsTracker.GetTrackedElementsCount();
        }

        internal IEnumerable<VisualElement> GetBoundElements()
        {
            return m_BindingsTracker.GetBoundElements();
        }

        internal void OnPropertyChanged(VisualElement element, in BindingId property)
        {
            m_BindingsTracker.OnPropertyChanged(element, property);
        }

        internal List<BindingDataCollection> GetOrderedBindingCollections()
        {
            return m_BindingsTracker.GetOrderedBindingCollections();
        }

        // Uses the collection taken from GetOrderedBindingCollections when it still belongs to the element.
        internal List<BindingData> GetBindingData(VisualElement element, BindingDataCollection collection)
        {
            return collection.owner == element && element.panel == m_Panel
                ? collection.GetBindings()
                : GetBindingData(element);
        }

        internal IEnumerable<VisualElement> GetUnorderedBoundElements()
        {
            return m_BindingsTracker.GetUnorderedBoundElements();
        }

        struct IgnoreUIChangesData
        {
            public VisualElement element;
            public Binding binding;
            public BindingId bindingId;

            public bool ShouldIgnoreChange(VisualElement ve, Binding b, BindingId id)
            {
                return this.element == ve &&
                       this.binding == b &&
                       this.bindingId == id;
            }
        }

        public struct IgnoreUIChangesScope : IDisposable
        {
            private IgnoreUIChangesData m_ScopeData;
            private DataBindingManager manager;

            internal IgnoreUIChangesScope(
                DataBindingManager manager,
                VisualElement target,
                BindingId bindingId,
                Binding binding)
            {
                this.manager = manager;
                m_ScopeData = this.manager.m_IgnoreUIChangesData;
                this.manager.m_IgnoreUIChangesData = new IgnoreUIChangesData
                {
                    element = target,
                    binding = binding,
                    bindingId = bindingId
                };
            }

            public void Dispose()
            {
                manager.m_IgnoreUIChangesData = m_ScopeData;
            }
        }

        public IgnoreUIChangesScope IgnoreChangesScope(VisualElement target, BindingId bindingId, Binding binding)
        {
            return new IgnoreUIChangesScope(this, target, bindingId, binding);
        }

        internal List<ChangesFromUI> GetChangedDetectedFromUI()
        {
            return m_DetectedChangesFromUI;
        }

        internal HashSet<PropertyPath> GetChangedDetectedFromSource(object dataSource)
        {
            return m_DataSourceTracker.GetChangesFromSource(dataSource);
        }

        internal List<BindingData> GetBindingData(VisualElement element)
        {
            return element.panel == m_Panel
                ? m_BindingsTracker.TryGetBindingCollection(element, out var collection)
                    ? collection.GetBindings()
                    : s_Empty
                : s_Empty;
        }

        internal bool TryGetBindingData(VisualElement element, in BindingId bindingId, out BindingData bindingData)
        {
            bindingData = default;
            if (element.panel == m_Panel && m_BindingsTracker.TryGetBindingCollection(element, out var collection))
            {
                return collection.TryGetBindingData(bindingId, out bindingData);
            }

            bindingData = default;
            return false;
        }

        internal void RegisterBinding(VisualElement element, in BindingId bindingId, Binding binding)
        {
            Assert.IsFalse(null == binding);
            // The message is only built when the assertion fails.
            var isBindingIdEmpty = ((PropertyPath) bindingId).IsEmpty;
            if (isBindingIdEmpty)
                Assert.IsFalse(isBindingIdEmpty, $"[UI Toolkit] Could not register binding on element of type '{element.GetType().Name}': target property path is empty.");

            if (m_BindingsTracker.TryGetBindingCollection(element, out var collection) &&
                collection.TryGetBindingData(bindingId, out var bindingData))
            {
                bindingData.binding.OnDeactivated(new BindingActivationContext(element, bindingId));
                var currentResolvedContext = m_DataSourceTracker.GetResolvedDataSourceContext(element, bindingData);

                var provider = bindingData.binding as IDataSourceProvider;
                var newSource = provider?.dataSource;
                var newSourcePath = provider?.dataSourcePath ?? default;
                if (currentResolvedContext.dataSource != newSource || currentResolvedContext.dataSourcePath != newSourcePath)
                    bindingData.binding.OnDataSourceChanged(new DataSourceContextChanged(element, bindingId, currentResolvedContext, new DataSourceContext(newSource, newSourcePath)));
                m_DataSourceTracker.DecreaseBindingRefCount(ref bindingData);
            }

            var newBindingData = GetPooledBindingData(new BindingTarget(element, bindingId), binding);
            m_DataSourceTracker.IncreaseBindingRefCount(ref newBindingData);
            m_BindingsTracker.StartTrackingBinding(element, newBindingData);

            binding.OnActivated(new BindingActivationContext(element, bindingId));
        }

        internal void UnregisterBinding(VisualElement element, in BindingId bindingId)
        {
            if (!m_BindingsTracker.TryGetBindingCollection(element, out var collection))
                return;

            if (collection.TryGetBindingData(bindingId, out var bindingData))
            {
                var currentResolvedContext = m_DataSourceTracker.GetResolvedDataSourceContext(element, bindingData);
                var provider = bindingData.binding as IDataSourceProvider;
                var newSource = provider?.dataSource;
                var newSourcePath = provider?.dataSourcePath ?? default;
                if (currentResolvedContext.dataSource != newSource || currentResolvedContext.dataSourcePath != newSourcePath)
                    bindingData.binding.OnDataSourceChanged(new DataSourceContextChanged(element, bindingId, currentResolvedContext, new DataSourceContext(newSource, newSourcePath)));
                bindingData.binding.OnDeactivated(new BindingActivationContext(element, bindingId));
                m_DataSourceTracker.DecreaseBindingRefCount(ref bindingData);

                m_BindingsTracker.StopTrackingBinding(element, bindingData);
                ReleasePoolBindingData(bindingData);
            }
        }

        /// <summary>
        /// Transfers the currently registered bindings back to the element.
        /// </summary>
        /// <param name="element"></param>
        internal void TransferBindingRequests(VisualElement element)
        {
            if (!m_BindingsTracker.IsTrackingElement(element))
                return;

            if (m_BindingsTracker.TryGetBindingCollection(element, out var collection))
            {
                var bindings = collection.GetBindings();
                while (bindings.Count > 0)
                {
                    var binding = bindings[^1];
                    CreateBindingRequest(element, binding.target.bindingId, binding.binding, isTransferring:true);
                    UnregisterBinding(element, binding.target.bindingId);
                }
            }

            m_BindingsTracker.StopTrackingElement(element);
        }

        public void InvalidateCachedDataSource(HashSet<VisualElement> addedOrMovedElements, HashSet<VisualElement> removedElements)
        {
            m_DataSourceTracker.InvalidateCachedDataSource(addedOrMovedElements, removedElements);
        }

        public void Dispose()
        {
            m_BindingsTracker.Dispose();
            m_DataSourceTracker.Dispose();
            m_DetectedChangesFromUI.Clear();
        }

        public static void CreateBindingRequest(VisualElement target, in BindingId bindingId, Binding binding)
        {
            CreateBindingRequest(target, in bindingId, binding, false);
        }

        private static void CreateBindingRequest(VisualElement target, in BindingId bindingId, Binding binding, bool isTransferring)
        {
            var requests = (List<BindingRequest>) target.GetProperty(k_RequestBindingPropertyName);
            if (requests == null)
            {
                requests = new List<BindingRequest>();
                target.SetProperty(k_RequestBindingPropertyName, requests);
            }

            var shouldProcessSelf = true;

            // When processing multiple requests to the same binding id, we should only process the very last one.
            for (var i = 0; i < requests.Count; ++i)
            {
                var request = requests[i];
                if (request.bindingId == bindingId)
                {
                    if (isTransferring)
                        shouldProcessSelf = false;
                    else
                        requests[i] = request.CancelRequest();
                }
            }

            requests.Add(new BindingRequest(bindingId, binding, shouldProcessSelf));
        }

        public static void CreateClearAllBindingsRequest(VisualElement target)
        {
            CreateBindingRequest(target, k_ClearBindingsToken, null);
        }

        public void ProcessBindingRequests(VisualElement element)
        {
            var requests = (List<BindingRequest>) element.GetProperty(k_RequestBindingPropertyName);
            if (requests == null)
                return;

            for (var index = 0; index < requests.Count; index++)
            {
                var request = requests[index];
                if (!request.shouldProcess)
                    continue;

                if (request.bindingId == k_ClearBindingsToken)
                {
                    ClearAllBindings(element);
                    continue;
                }

                if (request.bindingId == BindingId.Invalid)
                {
                    var panel = element.panel;
                    var panelName = (panel as Panel)?.name ?? panel.visualTree.name;
                    Debug.LogError(
                        $"[UI Toolkit] Trying to set a binding on `{(string.IsNullOrWhiteSpace(element.name) ? "<no name>" : element.name)} ({TypeUtility.GetTypeDisplayName(element.GetType())})` without setting the \"property\" attribute is not supported ({panelName}).");
                    continue;
                }

                if (request.binding != null)
                    RegisterBinding(element, request.bindingId, request.binding);
                else
                    UnregisterBinding(element, request.bindingId);
            }

            requests.Clear();
        }

        void ClearAllBindings(VisualElement element)
        {
            var list = ListPool<BindingData>.Get();
            try
            {
                list.AddRange(GetBindingData(element));
                foreach (var bindingData in list)
                {
                    UnregisterBinding(element, bindingData.target.bindingId);
                }
            }
            finally
            {
                ListPool<BindingData>.Release(list);
            }
        }

        internal static bool AnyPendingBindingRequests(VisualElement element)
        {
            var requests = (List<BindingRequest>) element.GetProperty(k_RequestBindingPropertyName);
            if (requests == null)
                return false;

            return requests.Count > 0;
        }

        internal static void GetBindingRequests(VisualElement element, List<(Binding binding, BindingId bindingId)> bindingRequests)
        {
            var requests = (List<BindingRequest>) element.GetProperty(k_RequestBindingPropertyName);
            if (requests == null)
            {
                return;
            }

            // We'll only return at most a single request per target property, as all the other requests will be discarded in the end.
            var visited = HashSetPool<BindingId>.Get();
            try
            {
                for (var i = requests.Count - 1; i >= 0; --i)
                {
                    var request = requests[i];
                    if (visited.Add(request.bindingId))
                        bindingRequests.Add((request.binding, request.bindingId));
                }
            }
            finally
            {
                HashSetPool<BindingId>.Release(visited);
            }
        }

        internal static bool TryGetBindingRequest(VisualElement element, in BindingId bindingId, out Binding binding)
        {
            var requests = (List<BindingRequest>) element.GetProperty(k_RequestBindingPropertyName);
            if (requests == null)
            {
                binding = null;
                return false;
            }

            // Here, we will only return the last request for the provided target property, as it is the one that will be used in the end.
            for (var i = requests.Count - 1; i >= 0; --i)
            {
                var request = requests[i];
                if (bindingId != request.bindingId || !request.shouldProcess)
                    continue;
                binding = request.binding;
                return true;
            }

            binding = null;
            return false;
        }

        public void DirtyBindingOrder()
        {
            m_BindingsTracker.SetDirty();
        }

        internal bool IsTrackingAnyElementInHierarchy(VisualElement element, bool includeSelf)
        {
            return m_BindingsTracker.IsTrackingAnyElementInHierarchy(element, includeSelf);
        }

        internal bool IsTrackingAnyElement(IReadOnlyList<VisualElement> elements)
        {
            return m_BindingsTracker.IsTrackingAnyElement(elements);
        }

        public void TrackDataSource(object previous, object current)
        {
            m_DataSourceTracker.DecreaseRefCount(previous);
            m_DataSourceTracker.IncreaseRefCount(current);
        }

        // Internal for tests
        internal (int boundElementsCount, int trackedDataSourcesCount) GetTrackedInfo()
        {
            var boundElements = m_BindingsTracker.GetTrackedElementsCount();
            var dataSources = m_DataSourceTracker.GetTrackedDataSourcesCount();
            return (boundElements, dataSources);
        }

        public void ClearSourceCache()
        {
            m_DataSourceTracker.ClearSourceCache();
        }

        public BindingData GetPooledBindingData(BindingTarget target, Binding binding)
        {
            BindingData data;

            if (m_BindingDataLocalPool.Count > 0)
            {
                data = m_BindingDataLocalPool[^1];
                m_BindingDataLocalPool.RemoveAt(m_BindingDataLocalPool.Count - 1);
            }
            else
                data = new BindingData();

            data.target = target;
            data.binding = binding;
            data.dataSourceProvider = binding as IDataSourceProvider;
            return data;
        }

        public void ReleasePoolBindingData(BindingData data)
        {
            data.Reset();
            m_BindingDataLocalPool.Add(data);
        }
    }
}
