// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
        class BindingDataComparer : IEqualityComparer<BindingData>
        {
            public bool Equals(BindingData lhs, BindingData rhs)
            {
                return lhs.binding == rhs.binding
                       && lhs.target.element == rhs.target.element
                       && lhs.target.bindingId == rhs.target.bindingId;
            }

            public int GetHashCode(BindingData data)
            {
                return HashCode.Combine(
                    data.binding,
                    data.target.element,
                    data.target.bindingId);
            }
        }

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

            public BindingRequest(BindingId bindingId, Binding binding, bool shouldProcess = true)
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

            public bool TryGetBindingData(BindingId bindingId, out BindingData data)
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

        internal readonly struct BindingData
        {
            public readonly BindingTarget target;
            public readonly Binding binding;

            public BindingData(BindingTarget target, Binding binding)
            {
                this.target = target;
                this.binding = binding;
            }
        }

        readonly struct SourceOwner
        {
            public readonly VisualElement element;
            public readonly object dataSource;

            public SourceOwner(VisualElement element, object dataSource)
            {
                this.element = element;
                this.dataSource = dataSource;
            }
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
                    m_Panel.dataBindingManager.m_DetectedChangesFromUI.Add(bindingData);
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
            class InvalidateDataSourcesTraversal : HierarchyTraversal
            {
                readonly HierarchyDataSourceTracker m_DataSourceTracker;
                readonly HashSet<VisualElement> m_VisitedElements;

                bool m_ForceInvalidate;
                bool m_ProcessingRemovedElements;

                public InvalidateDataSourcesTraversal(HierarchyDataSourceTracker dataSourceTracker)
                {
                    m_DataSourceTracker = dataSourceTracker;
                    m_VisitedElements = new HashSet<VisualElement>();
                }

                public void Invalidate(List<VisualElement> addedOrMovedElements, HashSet<VisualElement> removedElements)
                {
                    m_VisitedElements.Clear();

                    m_ForceInvalidate = false;
                    m_ProcessingRemovedElements = false;
                    for (var i = 0; i < addedOrMovedElements.Count; ++i)
                    {
                        var element = addedOrMovedElements[i];
                        Traverse(element);
                    }

                    m_ForceInvalidate = true;
                    m_ProcessingRemovedElements = true;
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

                    m_VisitedElements.Add(element);

                    // Whenever there is a data source change, we must re-run the bindings again
                    foreach (var bindingData in m_DataSourceTracker.m_DataBindingManager.GetBindingData(element))
                    {
                        if (bindingData.binding is IDataSourceProvider {dataSource: null})
                        {
                            bindingData.binding.MarkDirty();
                        }
                    }

                    m_DataSourceTracker.RemoveHierarchyDataSourceFromElement(element);
                    m_DataSourceTracker.RemoveHierarchyDataSourcePathFromElement(element);

                    if (null != element.dataSource && !m_ProcessingRemovedElements)
                    {
                        m_DataSourceTracker.AddHierarchyDataSourceForElement(new SourceOwner(element, element.dataSource), element);
                        m_DataSourceTracker.AddHierarchyDataSourcePathForElement(element, m_DataSourceTracker.GetHierarchyDataSourcePath(element));
                    }

                    if (!m_ForceInvalidate && element.dataSource != null && depth > 0)
                        return;
                    Recurse(element, depth);
                }
            }

            readonly DataBindingManager m_DataBindingManager;
            readonly Dictionary<SourceOwner, List<VisualElement>> m_SourceOwnerForElements;
            readonly Dictionary<VisualElement, SourceOwner> m_ResolvedSourceOwnerPerElement;
            readonly Dictionary<VisualElement, PropertyPath> m_ResolvedHierarchicalDataSourcePathPerElement;

            readonly Dictionary<Binding, object> m_LastBindingLocalDataSource;
            readonly Dictionary<Binding, DataSourceContext> m_LastKnownDataSource;
            readonly Dictionary<Binding, int> m_BindingRefCount;
            readonly Dictionary<object, long> m_LastDataSourceVersion;
            readonly Dictionary<object, int> m_DataSourceRefCount;
            readonly Dictionary<object, List<PropertyPath>> m_DetectedChangesFromSource;
            readonly InvalidateDataSourcesTraversal m_InvalidateResolvedDataSources;
            readonly EventHandler<BindablePropertyChangedEventArgs> m_Handler;
            readonly EventCallback<PropertyChangedEvent, VisualElement> m_VisualElementHandler;

            public HierarchyDataSourceTracker(DataBindingManager manager)
            {
                m_DataBindingManager = manager;
                m_SourceOwnerForElements = new Dictionary<SourceOwner, List<VisualElement>>();
                m_ResolvedSourceOwnerPerElement = new Dictionary<VisualElement, SourceOwner>();
                m_ResolvedHierarchicalDataSourcePathPerElement = new Dictionary<VisualElement, PropertyPath>();
                m_LastBindingLocalDataSource = new Dictionary<Binding, object>();
                m_LastKnownDataSource = new Dictionary<Binding, DataSourceContext>();
                m_BindingRefCount = new Dictionary<Binding, int>();
                m_LastDataSourceVersion = new Dictionary<object, long>();
                m_DataSourceRefCount = new Dictionary<object, int>();
                m_DetectedChangesFromSource = new Dictionary<object, List<PropertyPath>>();

                m_InvalidateResolvedDataSources = new InvalidateDataSourcesTraversal(this);
                m_Handler = TrackPropertyChanges;
                m_VisualElementHandler = OnVisualElementPropertyChanged;
            }

            internal void IncreaseBindingRefCount(Binding binding)
            {
                if (null == binding)
                    return;

                if (!m_BindingRefCount.TryGetValue(binding, out var refCount))
                {
                    refCount = 0;

                    if (binding is IDataSourceProvider dataSourceProvider)
                    {
                        IncreaseRefCount(dataSourceProvider.dataSource);
                        m_LastBindingLocalDataSource[binding] = dataSourceProvider.dataSource;
                    }
                }

                m_BindingRefCount[binding] = refCount + 1;
            }

            internal void DecreaseBindingRefCount(Binding binding)
            {
                if (null == binding)
                    return;

                if (!m_BindingRefCount.TryGetValue(binding, out var refCount))
                {
                    throw new InvalidOperationException("Trying to release a binding that isn't tracked. This is an internal bug. Please report using `Help > Report a Bug...`");
                }

                if (refCount == 1)
                {
                    m_BindingRefCount.Remove(binding);
                    m_LastBindingLocalDataSource.Remove(binding);
                    m_LastKnownDataSource.Remove(binding);

                    if (binding is IDataSourceProvider dataSourceProvider)
                        DecreaseRefCount(dataSourceProvider.dataSource);
                }
                else
                {
                    m_BindingRefCount[binding] = refCount - 1;
                }
            }

            internal void IncreaseRefCount(object dataSource)
            {
                if (null == dataSource)
                    return;

                if (!m_DataSourceRefCount.TryGetValue(dataSource, out var refCount))
                {
                    refCount = 0;
                    if (dataSource is INotifyBindablePropertyChanged notifier)
                        notifier.propertyChanged += m_Handler;

                    if (dataSource is VisualElement element)
                        element.RegisterCallback(m_VisualElementHandler, element);
                }

                m_DataSourceRefCount[dataSource] = refCount + 1;
            }

            private void OnVisualElementPropertyChanged(PropertyChangedEvent evt, VisualElement element)
            {
                TrackPropertyChanges(element,evt.property);
            }

            internal void DecreaseRefCount(object dataSource)
            {
                if (null == dataSource)
                    return;

                if (!m_DataSourceRefCount.TryGetValue(dataSource, out var refCount))
                {
                    throw new InvalidOperationException("Trying to release a data source that isn't tracked. This is an internal bug. Please report using `Help > Report a Bug...`");
                }

                if (refCount == 1)
                {
                    m_DataSourceRefCount.Remove(dataSource);
                    m_LastDataSourceVersion.Remove(dataSource);
                    if (dataSource is INotifyBindablePropertyChanged notifier)
                        notifier.propertyChanged -= m_Handler;

                    if (dataSource is VisualElement element)
                        element.UnregisterCallback(m_VisualElementHandler);
                }
                else
                {
                    m_DataSourceRefCount[dataSource] = refCount - 1;
                }
            }

            public int GetRefCount(object dataSource)
            {
                return m_DataSourceRefCount.TryGetValue(dataSource, out var refCount) ? refCount : 0;
            }

            public int GetTrackedDataSourcesCount()
            {
                return m_ResolvedSourceOwnerPerElement.Count;
            }

            public bool IsTrackingDataSource(VisualElement element)
            {
                return m_ResolvedSourceOwnerPerElement.ContainsKey(element);
            }

            public List<PropertyPath> GetChangesFromSource(object dataSource)
            {
                return m_DetectedChangesFromSource.TryGetValue(dataSource, out var list) ? list : null;
            }

            public void ClearChangesFromSource(object dataSource)
            {
                if (m_DetectedChangesFromSource.TryGetValue(dataSource, out var list))
                {
                    ListPool<PropertyPath>.Release(list);
                    m_DetectedChangesFromSource.Remove(dataSource);
                }
            }

            public void InvalidateCachedDataSource(HashSet<VisualElement> elements, HashSet<VisualElement> removedElements)
            {
                var toInvalidate = ListPool<VisualElement>.Get();

                try
                {
                    // Check for the case where the data source was changed on the element, because we can simply remap
                    // the source owner.
                    foreach (var element in elements)
                    {
                        // Case where the data source is either added or removed on the element. We need to traverse
                        // children and invalidate them as well.
                        if (element.dataSource == null ||
                            !TryGetSourceOwner(element, out var currentSourceOwner) ||
                            currentSourceOwner.element != element)
                        {
                            toInvalidate.Add(element);
                            continue;
                        }

                        RemapHierarchyDataSource(element);
                        RemapHierarchyDataSourcePath(element);
                    }

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

                var previouslyRegistered = m_LastBindingLocalDataSource.TryGetValue(bindingData.binding, out var lastLocalDatasource);
                var resolvedDataSource = localDataSource;
                PropertyPath resolvedDataSourcePath = localDataSourcePath;

                try
                {
                    if (null == localDataSource)
                    {
                        if (previouslyRegistered)
                        {
                            // We need to untrack previous local source.
                            DecreaseRefCount(lastLocalDatasource);
                        }

                        resolvedDataSource = TrackHierarchyDataSource(element);
                        resolvedDataSourcePath = TrackHierarchyDataSourcePath(element);
                        if (!localDataSourcePath.IsEmpty)
                            resolvedDataSourcePath = PropertyPath.Combine(resolvedDataSourcePath, localDataSourcePath);
                        return new DataSourceContext(resolvedDataSource, resolvedDataSourcePath);
                    }

                    // We need to update the source
                    if (localDataSource != lastLocalDatasource)
                    {
                        if (previouslyRegistered)
                        {
                            DecreaseRefCount(lastLocalDatasource);
                            IncreaseRefCount(localDataSource);
                        }
                    }
                }
                finally
                {
                    m_LastBindingLocalDataSource[bindingData.binding] = localDataSource;
                    var newResolvedContext = new DataSourceContext(resolvedDataSource, resolvedDataSourcePath);

                    if (m_LastKnownDataSource.TryGetValue(bindingData.binding, out var previousResolvedDataSource))
                    {
                        if (previousResolvedDataSource.dataSource != resolvedDataSource ||
                            previousResolvedDataSource.dataSourcePath != resolvedDataSourcePath)
                        {
                            bindingData.binding.OnDataSourceChanged(new DataSourceContextChanged(element, bindingData.target.bindingId, previousResolvedDataSource, newResolvedContext));
                            bindingData.binding.MarkDirty();
                        }
                    }
                    else
                    {
                        if (null != resolvedDataSource || default != resolvedDataSourcePath)
                            bindingData.binding.OnDataSourceChanged(new DataSourceContextChanged(element, bindingData.target.bindingId,  default,  newResolvedContext));
                        bindingData.binding.MarkDirty();
                    }

                    m_LastKnownDataSource[bindingData.binding] = newResolvedContext;
                }

                return new DataSourceContext(resolvedDataSource, resolvedDataSourcePath);
            }

            private void TrackPropertyChanges(object sender, BindablePropertyChangedEventArgs args)
                => TrackPropertyChanges(sender, args.propertyName);

            private void TrackPropertyChanges(object sender, PropertyPath propertyPath)
            {
                if (!m_DataSourceRefCount.ContainsKey(sender))
                    return;

                if (!m_DetectedChangesFromSource.TryGetValue(sender, out var list))
                {
                    list = ListPool<PropertyPath>.Get();
                    m_DetectedChangesFromSource[sender] = list;
                }

                list.Add(propertyPath);
            }

            public bool TryGetLastVersion(object source, out long version)
            {
                if (null != source)
                    return m_LastDataSourceVersion.TryGetValue(source, out version);

                version = -1;
                return false;
            }

            public void UpdateVersion(object source, long version)
            {
                m_LastDataSourceVersion[source] = version;
            }

            public object TrackHierarchyDataSource(VisualElement element)
            {
                if (GetHierarchyDataSource(element, out var dataSource, out var sourceOwner))
                {
                    AddHierarchyDataSourceForElement(new SourceOwner(sourceOwner, dataSource), element);
                }

                return dataSource;
            }

            public PropertyPath TrackHierarchyDataSourcePath(VisualElement element)
            {
                var dataSourcePath = GetHierarchyDataSourcePath(element);
                AddHierarchyDataSourcePathForElement(element, dataSourcePath);

                return dataSourcePath;
            }

            internal object GetHierarchyDataSource(VisualElement element)
            {
                GetHierarchyDataSource(element, out var dataSource, out _);
                return dataSource;
            }

            // Returns whether or not the owner of the hierarchy data source changed.
            bool GetHierarchyDataSource(VisualElement element, out object dataSource, out VisualElement sourceOwnerElement)
            {
                var sourceElement = element;
                while (sourceElement != null)
                {
                    if (TryGetSourceOwner(sourceElement, out var sourceOwner))
                    {
                        dataSource = sourceOwner.dataSource;
                        sourceOwnerElement = sourceOwner.element;
                        return sourceElement != element;
                    }

                    if (sourceElement.dataSource != null)
                    {
                        dataSource = sourceElement.dataSource;
                        sourceOwnerElement = sourceElement;
                        return true;
                    }

                    sourceElement = sourceElement.hierarchy.parent;
                }

                dataSource = null;
                sourceOwnerElement = null;
                return false;
            }

            bool TryGetSourceOwner(VisualElement element, out SourceOwner sourceOwner)
            {
                return m_ResolvedSourceOwnerPerElement.TryGetValue(element, out sourceOwner);
            }

            void AddHierarchyDataSourceForElement(SourceOwner owner, VisualElement element)
            {
                m_ResolvedSourceOwnerPerElement[element] = owner;
                if (!m_SourceOwnerForElements.TryGetValue(owner, out var list))
                    m_SourceOwnerForElements[owner] = list = new List<VisualElement>();
                list.Add(element);
            }

            PropertyPath GetHierarchyDataSourcePath(VisualElement element)
            {
                var path = default(PropertyPath);

                while (null != element)
                {
                    if (!element.dataSourcePath.IsEmpty)
                        path = PropertyPath.Combine(element.dataSourcePath, path);

                    if (null != element.dataSource)
                        break;
                    element = element.hierarchy.parent;
                }

                return path;
            }

            void AddHierarchyDataSourcePathForElement(VisualElement element, PropertyPath dataSourcePath)
            {
                m_ResolvedHierarchicalDataSourcePathPerElement[element] = dataSourcePath;
            }

            internal void RemoveHierarchyDataSourceFromElement(VisualElement element)
            {
                if (!TryGetSourceOwner(element, out var owner))
                    return;

                m_ResolvedSourceOwnerPerElement.Remove(element);

                if (m_SourceOwnerForElements.TryGetValue(owner, out var list))
                    list.Remove(element);
            }

            internal void RemoveHierarchyDataSourcePathFromElement(VisualElement element)
            {
                m_ResolvedHierarchicalDataSourcePathPerElement.Remove(element);
            }

            void RemapHierarchyDataSource(VisualElement element)
            {
                if (!TryGetSourceOwner(element, out var currentSourceOwner))
                    return;

                var current = m_SourceOwnerForElements[currentSourceOwner];
                var newSourceOwner = new SourceOwner(element, element.dataSource);

                // Update source owner and dirty bindings
                m_ResolvedSourceOwnerPerElement[element] = newSourceOwner;

                foreach (var t in current)
                {
                    foreach (var bindingData in m_DataBindingManager.GetBindingData(t))
                    {
                        if (bindingData.binding is IDataSourceProvider {dataSource: null})
                            bindingData.binding.MarkDirty();
                    }

                    m_ResolvedSourceOwnerPerElement[t] = newSourceOwner;
                }

                m_SourceOwnerForElements[newSourceOwner] = current;

                // Clean-up
                if (!currentSourceOwner.dataSource.Equals(newSourceOwner.dataSource))
                    m_SourceOwnerForElements.Remove(currentSourceOwner);
            }

            void RemapHierarchyDataSourcePath(VisualElement element)
            {
                if (m_ResolvedHierarchicalDataSourcePathPerElement.ContainsKey(element))
                    m_ResolvedHierarchicalDataSourcePathPerElement[element] = GetHierarchyDataSourcePath(element);
            }

            public void Dispose()
            {
                m_SourceOwnerForElements.Clear();
                m_ResolvedSourceOwnerPerElement.Clear();
                m_LastBindingLocalDataSource.Clear();
                m_LastKnownDataSource.Clear();
                m_BindingRefCount.Clear();
                m_LastDataSourceVersion.Clear();
                m_DataSourceRefCount.Clear();

                foreach (var list in m_DetectedChangesFromSource.Values)
                {
                    ListPool<PropertyPath>.Release(list);
                }
                m_DetectedChangesFromSource.Clear();
            }
        }

        readonly BaseVisualElementPanel m_Panel;
        readonly HierarchyDataSourceTracker m_DataSourceTracker;
        readonly HierarchyBindingTracker m_BindingsTracker;
        readonly List<BindingData> m_DetectedChangesFromUI;
        readonly Dictionary<BindingData, BindingResult> m_LastUIBindingResultsCache;
        readonly Dictionary<BindingData, BindingResult> m_LastSourceBindingResultsCache;

        internal DataBindingManager(BaseVisualElementPanel panel)
        {
            m_Panel = panel;
            m_DataSourceTracker = new HierarchyDataSourceTracker(this);
            m_BindingsTracker = new HierarchyBindingTracker(panel);
            m_DetectedChangesFromUI = new List<BindingData>();
            var comparer = new BindingDataComparer();
            m_LastUIBindingResultsCache = new Dictionary<BindingData, BindingResult>(comparer);
            m_LastSourceBindingResultsCache = new Dictionary<BindingData, BindingResult>(comparer);
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
            m_LastUIBindingResultsCache[bindingData] = result;
        }

        internal bool TryGetLastUIBindingResult(BindingData bindingData, out BindingResult result)
        {
            return m_LastUIBindingResultsCache.TryGetValue(bindingData, out result);
        }

        internal void CacheSourceBindingResult(BindingData bindingData, BindingResult result)
        {
            m_LastSourceBindingResultsCache[bindingData] = result;
        }

        internal bool TryGetLastSourceBindingResult(BindingData bindingData, out BindingResult result)
        {
            return m_LastSourceBindingResultsCache.TryGetValue(bindingData, out result);
        }

        internal DataSourceContext GetResolvedDataSourceContext(VisualElement element, BindingData bindingData)
        {
            return element.panel == m_Panel
                ? m_DataSourceTracker.GetResolvedDataSourceContext(element, bindingData)
                : default;
        }

        // Internal for tests
        internal object TrackHierarchyDataSource(VisualElement element)
        {
            return element.panel == m_Panel
                ? m_DataSourceTracker.TrackHierarchyDataSource(element)
                : null;
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

        internal List<BindingData> GetChangedDetectedFromUI()
        {
            return m_DetectedChangesFromUI;
        }

        internal List<PropertyPath> GetChangedDetectedFromSource(object dataSource)
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

        internal bool TryGetBindingData(VisualElement element, BindingId bindingId, out BindingData bindingData)
        {
            bindingData = default;
            if (element.panel == m_Panel && m_BindingsTracker.TryGetBindingCollection(element, out var collection))
            {
                return collection.TryGetBindingData(bindingId, out bindingData);
            }

            bindingData = default;
            return false;
        }

        internal void RegisterBinding(VisualElement element, BindingId bindingId, Binding binding)
        {
            Assert.IsNotNull(binding);
            Assert.IsFalse(((PropertyPath)bindingId).IsEmpty, $"[UI Toolkit] Could not register binding on element of type '{element.GetType().Name}': target property path is empty.");

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
                m_DataSourceTracker.DecreaseBindingRefCount(bindingData.binding);
            }

            m_BindingsTracker.StartTrackingBinding(element, new BindingData(new BindingTarget(element, bindingId), binding));
            m_DataSourceTracker.IncreaseBindingRefCount(binding);
            binding.OnActivated(new BindingActivationContext(element, bindingId));
        }

        internal void UnregisterBinding(VisualElement element, BindingId bindingId)
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
                m_DataSourceTracker.DecreaseBindingRefCount(bindingData.binding);
            }

            m_BindingsTracker.StopTrackingBinding(element, new BindingData(new BindingTarget(element, bindingId), null));
        }

        /// <summary>
        /// Transfers the currently registered bindings back to the element.
        /// </summary>
        /// <param name="element"></param>
        internal void TransferBindingRequests(VisualElement element)
        {
            if (m_BindingsTracker.TryGetBindingCollection(element, out var collection))
            {
                var bindings = collection.GetBindings();
                while (bindings.Count > 0)
                {
                    var binding = bindings[^1];
                    CreateBindingRequest(element, binding.target.bindingId, binding.binding);
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

        public static void CreateBindingRequest(VisualElement target, BindingId bindingId, Binding binding)
        {
            var requests = (List<BindingRequest>) target.GetProperty(k_RequestBindingPropertyName);
            if (requests == null)
            {
                requests = new List<BindingRequest>();
                target.SetProperty(k_RequestBindingPropertyName, requests);
            }

            // When processing multiple requests to the same binding id, we should only process the very last one.
            for (var i = 0; i < requests.Count; ++i)
            {
                var request = requests[i];
                if (request.bindingId == bindingId)
                {
                    requests[i] = request.CancelRequest();
                }
            }
            requests.Add(new BindingRequest(bindingId, binding));
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
                    Debug.LogError($"[UI Toolkit] Trying to set a binding on `{(string.IsNullOrWhiteSpace(element.name) ? "<no name>" : element.name)} ({TypeUtility.GetTypeDisplayName(element.GetType())})` without setting the \"property\" attribute is not supported ({panelName}).");
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

        internal static bool TryGetBindingRequest(VisualElement element, BindingId bindingId, out Binding binding)
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
    }
}
