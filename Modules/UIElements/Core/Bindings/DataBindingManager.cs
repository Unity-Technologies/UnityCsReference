// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine.Assertions;
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

    sealed class DataBindingManager : IDisposable
    {
        private  readonly List<BindingData> m_BindingDataLocalPool = new List<BindingData>(64);

        static readonly PropertyName k_RequestBindingPropertyName = "__unity-binding-request";
        static readonly BindingId k_ClearBindingsToken = "$__BindingManager--ClearAllBindings";

        internal static BindingLogLevel globalLogLevel = BindingLogLevel.All;

        BindingLogLevel? m_LogLevel;

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

        struct BindingDataCollection : IDisposable
        {
            Dictionary<BindingId, BindingData> m_BindingPerId;
            List<BindingData> m_Bindings;

            public static BindingDataCollection Create()
            {
                var collection = new BindingDataCollection();
                collection.m_BindingPerId = DictionaryPool<BindingId, BindingData>.Get();
                collection.m_Bindings = ListPool<BindingData>.Get();
                return collection;
            }

            public void AddBindingData(BindingData bindingData)
            {
                if (m_BindingPerId.TryGetValue(bindingData.target.bindingId, out var toRemove))
                {
                    m_Bindings.Remove(toRemove);
                }

                m_BindingPerId[bindingData.target.bindingId] = bindingData;
                m_Bindings.Add(bindingData);
            }

            public bool TryGetBindingData(in BindingId bindingId, out BindingData data)
            {
                return m_BindingPerId.TryGetValue(bindingId, out data);
            }

            public bool RemoveBindingData(BindingData bindingData)
            {
                if (!m_BindingPerId.TryGetValue(bindingData.target.bindingId, out var toRemove))
                    return false;

                return m_Bindings.Remove(toRemove) && m_BindingPerId.Remove(toRemove.target.bindingId);
            }

            public List<BindingData> GetBindings()
            {
                return m_Bindings;
            }

            public int GetBindingCount()
            {
                return m_Bindings.Count;
            }

            public void Dispose()
            {
                if (m_BindingPerId != null)
                    DictionaryPool<BindingId, BindingData>.Release(m_BindingPerId);
                m_BindingPerId = null;
                if (m_Bindings != null)
                    ListPool<BindingData>.Release(m_Bindings);
                m_Bindings = null;
            }
        }

        internal class BindingData
        {
            public long version;
            public BindingTarget target;
            public Binding binding;

            private DataSourceContext m_LastContext;

            public object localDataSource { get; set; }

            public void Reset()
            {
                ++version;
                target = default;
                binding = default;
                localDataSource = default;
                m_LastContext = default;
                m_SourceToUILastUpdate = null;
                m_UIToSourceLastUpdate = null;
            }

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

                    if (boundElements.Contains(element))
                        results.Add(element);

                    Recurse(element, depth);
                }
            }

            readonly BaseVisualElementPanel m_Panel;
            readonly HierarchicalBindingsSorter m_BindingSorter;
            readonly Dictionary<VisualElement, BindingDataCollection> m_BindingDataPerElement;
            readonly HashSet<VisualElement> m_BoundElements;
            readonly List<VisualElement> m_OrderedBindings;
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

            public IEnumerable<VisualElement> GetUnorderedBoundElements()
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
                m_OnPropertyChanged = OnPropertyChanged;
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

            public void StartTrackingBinding(VisualElement element, BindingData binding)
            {
                BindingDataCollection collection;
                if (m_BoundElements.Add(element))
                {
                    collection = BindingDataCollection.Create();
                    m_BindingDataPerElement.Add(element, collection);
                    element.RegisterCallback(m_OnPropertyChanged, m_BindingDataPerElement);
                }
                else if (!m_BindingDataPerElement.TryGetValue(element, out collection))
                {
                    throw new InvalidOperationException("Trying to add a binding to an element which doesn't have a binding collection. This is an internal bug. Please report using `Help > Report a Bug...`");
                }

                binding.binding.MarkDirty();
                collection.AddBindingData(binding);
                m_BindingDataPerElement[element] = collection;
                SetDirty();
            }

            private EventCallback<PropertyChangedEvent, Dictionary<VisualElement, BindingDataCollection>> m_OnPropertyChanged;

            private void OnPropertyChanged(PropertyChangedEvent evt, Dictionary<VisualElement, BindingDataCollection> bindingCollection)
            {
                if (evt.target is not VisualElement target)
                    throw new InvalidOperationException($"Trying to track property changes on a non '{nameof(VisualElement)}'. This is an internal bug. Please report using `Help > Report a Bug...`");

                if (!bindingCollection.TryGetValue(target, out var collection))
                    throw new InvalidOperationException($"Trying to track property changes on a '{nameof(VisualElement)}' that is not being tracked. This is an internal bug. Please report using `Help > Report a Bug...`");

                if (collection.TryGetBindingData(evt.property, out var bindingData) &&
                    target.TryGetBinding(evt.property, out var current) &&
                    bindingData.binding == current)
                    m_Panel.dataBindingManager.m_DetectedChangesFromUI.Add(new ChangesFromUI(bindingData));
            }

            public void StopTrackingBinding(VisualElement element, BindingData binding)
            {
                if (m_BoundElements.Contains(element) && m_BindingDataPerElement.TryGetValue(element, out var collection))
                {
                    collection.RemoveBindingData(binding);
                    if (collection.GetBindingCount() == 0)
                    {
                        StopTrackingElement(element);
                        element.UnregisterCallback(m_OnPropertyChanged);
                    }
                    else
                    {
                        m_BindingDataPerElement[element] = collection;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Trying to remove a binding to an element which doesn't have a binding collection. This is an internal bug. Please report using `Help > Report a Bug...`");
                }

                SetDirty();
            }

            public void StopTrackingElement(VisualElement element)
            {
                if (m_BindingDataPerElement.TryGetValue(element, out var collection))
                {
                    collection.Dispose();
                }

                m_BindingDataPerElement.Remove(element);
                m_BoundElements.Remove(element);
                SetDirty();
            }

            public void Dispose()
            {
                foreach (var kvp in m_BindingDataPerElement)
                {
                    kvp.Value.Dispose();
                }

                m_BindingDataPerElement.Clear();
                m_BoundElements.Clear();
                m_OrderedBindings.Clear();
            }

            void OrderBindings(VisualElement root)
            {
                m_OrderedBindings.Clear();
                m_BindingSorter.boundElements = m_BoundElements;
                m_BindingSorter.results = m_OrderedBindings;
                m_BindingSorter.Traverse(root);
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
                info.lastVersion = long.MinValue;
                info.refCount = 0;
                info.detectedChangesNoAlloc?.Clear();
                m_SourceInfosPool.Add(info);
            }

            class SourceInfo
            {
                private HashSet<PropertyPath> m_DetectedChanges;

                public long lastVersion { get; set; }
                public int refCount { get; set; }
                public HashSet<PropertyPath> detectedChanges => m_DetectedChanges ??= new HashSet<PropertyPath>();
                public HashSet<PropertyPath> detectedChangesNoAlloc => m_DetectedChanges;
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
                    if (m_VisitedElements.Contains(element))
                        return;

                    if (depth > 0 && null != element.dataSource)
                        return;

                    m_VisitedElements.Add(element);
                    m_DataSourceTracker.RemoveHierarchyDataSourceContextFromElement(element);

                    Recurse(element, depth);
                }
            }

            readonly DataBindingManager m_DataBindingManager;

            private readonly Dictionary<VisualElement, DataSourceContext> m_ResolvedHierarchicalDataSourceContext;

            readonly Dictionary<Binding, int> m_BindingRefCount;
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
                m_BindingRefCount = new Dictionary<Binding, int>();
                var dataSourceComparer = new ObjectComparer();
                m_SourceInfos = new Dictionary<object, SourceInfo>(dataSourceComparer);
                m_SourcesToRemove = new HashSet<object>(dataSourceComparer);

                m_InvalidateResolvedDataSources = new InvalidateDataSourcesTraversal(this);
                m_Handler = TrackPropertyChanges;
                m_VisualElementHandler = OnVisualElementPropertyChanged;
            }

            internal void IncreaseBindingRefCount(ref BindingData bindingData)
            {
                var binding = bindingData.binding;
                if (null == binding)
                    return;

                if (!m_BindingRefCount.TryGetValue(binding, out var refCount))
                {
                    refCount = 0;
                }

                if (binding is IDataSourceProvider dataSourceProvider)
                {
                    IncreaseRefCount(dataSourceProvider.dataSource);
                    bindingData.localDataSource = dataSourceProvider.dataSource;
                }

                m_BindingRefCount[binding] = refCount + 1;
            }

            internal void DecreaseBindingRefCount(ref BindingData bindingData)
            {
                var binding = bindingData.binding;
                if (null == binding)
                    return;

                if (!m_BindingRefCount.TryGetValue(binding, out var refCount))
                {
                    throw new InvalidOperationException("Trying to release a binding that isn't tracked. This is an internal bug. Please report using `Help > Report a Bug...`");
                }

                if (refCount == 1)
                {
                    m_BindingRefCount.Remove(binding);
                }
                else
                {
                    m_BindingRefCount[binding] = refCount - 1;
                }

                if (binding is IDataSourceProvider dataSourceProvider)
                    DecreaseRefCount(dataSourceProvider.dataSource);
            }

            internal void IncreaseRefCount(object dataSource)
            {
                if (null == dataSource)
                    return;

                m_SourcesToRemove.Remove(dataSource);

                if (!m_SourceInfos.TryGetValue(dataSource, out var info))
                {
                    m_SourceInfos[dataSource] = info = GetPooledSourceInfo();
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

            public void ClearChangesFromSource(object dataSource)
            {
                if (!m_SourceInfos.TryGetValue(dataSource, out var info))
                    return;

                info.detectedChangesNoAlloc?.Clear();
            }

            public void InvalidateCachedDataSource(HashSet<VisualElement> elements, HashSet<VisualElement> removedElements)
            {
                var toInvalidate = ListPool<VisualElement>.Get();

                try
                {
                    foreach (var element in elements)
                        toInvalidate.Add(element);

                    m_InvalidateResolvedDataSources.Invalidate(toInvalidate, removedElements);
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

                if (bindingData.binding is IDataSourceProvider dataSourceProvider)
                {
                    localDataSource = dataSourceProvider.dataSource;
                    localDataSourcePath = dataSourceProvider.dataSourcePath;
                }

                var lastLocalDataSource = bindingData.localDataSource;
                var resolvedDataSource = localDataSource;
                PropertyPath resolvedDataSourcePath = localDataSourcePath;

                try
                {
                    if (null == localDataSource)
                    {
                        // We need to untrack previous local source.
                        DecreaseRefCount(lastLocalDataSource);

                        var resolvedHierarchicalContext = GetHierarchicalDataSourceContext(element);
                        resolvedDataSource = resolvedHierarchicalContext.dataSource;
                        resolvedDataSourcePath = !localDataSourcePath.IsEmpty
                            ? PropertyPath.Combine(resolvedHierarchicalContext.dataSourcePath, localDataSourcePath)
                            : resolvedHierarchicalContext.dataSourcePath;

                        return new DataSourceContext(resolvedDataSource, resolvedDataSourcePath);
                    }

                    // We need to update the source
                    if (localDataSource != lastLocalDataSource)
                    {
                        DecreaseRefCount(lastLocalDataSource);
                        IncreaseRefCount(localDataSource);
                    }
                }
                finally
                {
                    bindingData.localDataSource = localDataSource;
                    var newResolvedContext = new DataSourceContext(resolvedDataSource, resolvedDataSourcePath);

                    bindingData.context = newResolvedContext;
                }

                return new DataSourceContext(resolvedDataSource, resolvedDataSourcePath);
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

            public bool TryGetLastVersion(object source, out long version)
            {
                if (null != source && m_SourceInfos.TryGetValue(source, out var sourceInfo))
                {
                    version = sourceInfo.lastVersion;
                    return true;
                }

                version = -1;
                return false;
            }

            public void UpdateVersion(object source, long version)
            {
                var info = m_SourceInfos[source];
                info.lastVersion = version;
                m_SourceInfos[source] = info;
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
                    if (!current.dataSourcePath.IsEmpty)
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
                m_BindingRefCount.Clear();
                m_SourcesToRemove.Clear();
                m_SourceInfosPool.Clear();
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

        internal bool TryGetLastVersion(object source, out long version)
        {
            return m_DataSourceTracker.TryGetLastVersion(source, out version);
        }

        internal void UpdateVersion(object source, long version)
        {
            m_DataSourceTracker.UpdateVersion(source, version);
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

        internal IEnumerable<VisualElement> GetUnorderedBoundElements()
        {
            return m_BindingsTracker.GetUnorderedBoundElements();
        }

        internal List<ChangesFromUI> GetChangedDetectedFromUI()
        {
            return m_DetectedChangesFromUI;
        }

        internal HashSet<PropertyPath> GetChangedDetectedFromSource(object dataSource)
        {
            return m_DataSourceTracker.GetChangesFromSource(dataSource);
        }

        internal void ClearChangesFromSource(object dataSource)
        {
            m_DataSourceTracker.ClearChangesFromSource(dataSource);
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
            Assert.IsFalse(((PropertyPath) bindingId).IsEmpty, $"[UI Toolkit] Could not register binding on element of type '{element.GetType().Name}': target property path is empty.");

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

        internal static IEnumerable<(Binding binding, BindingId bindingId)> GetBindingRequests(VisualElement element)
        {
            var requests = (List<BindingRequest>) element.GetProperty(k_RequestBindingPropertyName);
            if (requests == null)
            {
                yield break;
            }

            // We'll only return at most a single request per target property, as all the other requests will be discarded in the end.
            var visited = HashSetPool<BindingId>.Get();
            try
            {
                for (var i = requests.Count - 1; i >= 0; --i)
                {
                    var request = requests[i];
                    if (visited.Add(request.bindingId))
                        yield return (request.binding, request.bindingId);
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
                if (bindingId != request.bindingId)
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
            return data;
        }

        public void ReleasePoolBindingData(BindingData data)
        {
            data.Reset();
            m_BindingDataLocalPool.Add(data);
        }
    }
}
