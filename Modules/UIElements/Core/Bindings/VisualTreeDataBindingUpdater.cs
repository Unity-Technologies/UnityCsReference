// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Properties;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements
{
    enum BindingUpdateStage
    {
        UpdateUI,
        UpdateSource
    }

    class VisualTreeDataBindingsUpdater : BaseVisualTreeUpdater
    {
        readonly struct VersionInfo
        {
            public readonly object source;
            public readonly long version;

            public VersionInfo(object source, long version)
            {
                this.source = source;
                this.version = version;
            }
        }

        static readonly ProfilerMarker s_UpdateProfilerMarker = new ProfilerMarker("UIElements.UpdateRuntimeBindings");
        static readonly ProfilerMarker s_ProcessBindingRequestsProfilerMarker = new ProfilerMarker("Process Binding Requests");
        static readonly ProfilerMarker s_ProcessDataSourcesProfilerMarker = new ProfilerMarker("Process Data Sources");
        static readonly ProfilerMarker s_ShouldUpdateBindingProfilerMarker = new ProfilerMarker("Should Update Binding");
        static readonly ProfilerMarker s_UpdateBindingProfilerMarker = new ProfilerMarker("Update Binding");

        DataBindingManager bindingManager => panel.dataBindingManager;

        public override ProfilerMarker profilerMarker => s_UpdateProfilerMarker;

        readonly BindingUpdater m_Updater = new();
        readonly List<VisualElement> m_BindingRegistrationRequests = new();
        readonly HashSet<VisualElement> m_DataSourceChangedRequests = new();
        readonly HashSet<VisualElement> m_RemovedElements = new();

        public VisualTreeDataBindingsUpdater()
        {
            panelChanged += OnPanelChanged;
        }

        protected void OnHierarchyChange(VisualElement ve, HierarchyChangeType type, IReadOnlyList<VisualElement> additionalContext = null)
        {
            // Invalidating cached data sources can do up to a full hierarchy traversal, so if nothing is registered, we
            // can safely skip the invalidation step completely.
            if (bindingManager.GetBoundElementsCount() == 0 && bindingManager.GetTrackedDataSourcesCount() == 0)
                return;

            switch (type)
            {
                case HierarchyChangeType.RemovedFromParent:
                    m_DataSourceChangedRequests.Remove(ve);
                    m_RemovedElements.Add(ve);
                    break;
                case HierarchyChangeType.AddedToParent:
                case HierarchyChangeType.ChildrenReordered:
                    m_RemovedElements.Remove(ve);
                    m_DataSourceChangedRequests.Add(ve);
                    break;
                case HierarchyChangeType.AttachedToPanel:
                    for (var i = 0; i < additionalContext.Count; ++i)
                    {
                        var added = additionalContext[i];
                        m_RemovedElements.Remove(added);
                        m_DataSourceChangedRequests.Add(ve);
                    }
                    break;
                case HierarchyChangeType.DetachedFromPanel:
                    for (var i = 0; i < additionalContext.Count; ++i)
                    {
                        var removed = additionalContext[i];
                        m_DataSourceChangedRequests.Remove(ve);
                        m_RemovedElements.Add(removed);
                    }
                    break;
            }

            bindingManager.DirtyBindingOrder();
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & VersionChangeType.BindingRegistration) == VersionChangeType.BindingRegistration)
                m_BindingRegistrationRequests.Add(ve);

            if ((versionChangeType & VersionChangeType.DataSource) == VersionChangeType.DataSource)
                m_DataSourceChangedRequests.Add(ve);
        }

        void CacheAndLogBindingResult(bool appliedOnUiCache, in DataBindingManager.BindingData bindingData, in BindingResult result)
        {
            var logLevel = bindingManager.logLevel;

            if (logLevel == BindingLogLevel.None)
            {
                // Log nothing.
            }
            else if (logLevel == BindingLogLevel.Once)
            {
                BindingResult previousResult;
                if (appliedOnUiCache)
                    bindingManager.TryGetLastUIBindingResult(bindingData, out previousResult);
                else
                    bindingManager.TryGetLastSourceBindingResult(bindingData, out previousResult);

                if (previousResult.status != result.status || previousResult.message != result.message)
                {
                    LogResult(result);
                }
            }
            else
            {
                LogResult(result);
            }

            if (appliedOnUiCache)
                bindingManager.CacheUIBindingResult(bindingData, result);
            else
                bindingManager.CacheSourceBindingResult(bindingData, result);
        }

        void LogResult(in BindingResult result)
        {
            if (string.IsNullOrWhiteSpace(result.message))
                return;

            var panelName = (panel as Panel)?.name ?? panel.visualTree.name;
            Debug.LogWarning($"{result.message} ({panelName})");
        }

        private readonly List<VisualElement> m_BoundsElement = new();
        private readonly List<VersionInfo> m_VersionChanges = new();
        private readonly HashSet<object> m_TrackedObjects = new();
        private readonly HashSet<Binding> m_RanUpdate = new();
        private readonly HashSet<object> m_KnownSources = new();
        private readonly HashSet<Binding> m_DirtyBindings = new();
        private BaseVisualElementPanel m_AttachedPanel;

        public override void Update()
        {
            ProcessAllBindingRequests();
            ProcessDataSourceChangedRequests();

            ProcessPropertyChangedEvents(m_RanUpdate);

            m_BoundsElement.AddRange(bindingManager.GetBoundElements());
            foreach (var element in m_BoundsElement)
            {
                var bindings = bindingManager.GetBindingData(element);
                for (var i = 0; i < bindings.Count; ++i)
                {
                    var bindingData = bindings[i];
                    PropertyPath resolvedDataSourcePath;
                    object source;
                    using (s_ShouldUpdateBindingProfilerMarker.Auto())
                    {
                        var resolvedContext = bindingManager.GetResolvedDataSourceContext(element, bindingData);
                        source = resolvedContext.dataSource;
                        resolvedDataSourcePath = resolvedContext.dataSourcePath;

                        var (changed, version) = GetDataSourceVersion(source);

                        // Getting the version can trigger an update, for example by refreshing the SerializedObject
                        // in that case the element with the binding might disappear (a managed reference changed type for example)
                        if (bindingData.binding == null) continue;

                        // We want to track the earliest version of the source, in case one of the bindings changes it
                        if (null != source && m_TrackedObjects.Add(source))
                            m_VersionChanges.Add(new VersionInfo(source, version));

                        if (bindingData.binding.isDirty)
                             m_DirtyBindings.Add(bindingData.binding);

                        if (!m_Updater.ShouldProcessBindingAtStage(bindingData.binding, BindingUpdateStage.UpdateUI, changed, m_DirtyBindings.Contains(bindingData.binding)))
                            continue;

                        if (bindingData.binding.updateTrigger == BindingUpdateTrigger.OnSourceChanged && source is INotifyBindablePropertyChanged && !bindingData.binding.isDirty)
                        {
                            var changedPaths = bindingManager.GetChangedDetectedFromSource(source);
                            if (null == changedPaths || changedPaths.Count == 0)
                                continue;

                            var processBinding = false;

                            foreach (var path in changedPaths)
                            {
                                if (IsPrefix(path, resolvedDataSourcePath))
                                {
                                    processBinding = true;
                                    break;
                                }
                            }

                            if (!processBinding)
                                continue;
                        }
                    }

                    if (null != source)
                        m_KnownSources.Add(source);

                    var wasDirty = bindingData.binding.isDirty;
                    bindingData.binding.ClearDirty();

                    var context = new BindingContext
                    (
                        element,
                        bindingData.target.bindingId,
                        resolvedDataSourcePath,
                        source
                    );

                    BindingResult result = default;
                    var bindingVersion = bindingData.version;
                    using (s_UpdateBindingProfilerMarker.Auto())
                    {
                        result = m_Updater.UpdateUI(in context, bindingData.binding);
                    }

                    CacheAndLogBindingResult(true, bindingData, result);

                    // Ensure that the binding update didn't remove itself in a synced fashion (i.e. removing itself from the panel)
                    if (bindingData.version == bindingVersion)
                    {
                        switch (result.status)
                        {
                            case BindingStatus.Success:
                                m_RanUpdate.Add(bindingData.binding);
                                break;
                            case BindingStatus.Pending when wasDirty:
                                bindingData.binding.MarkDirty();
                                break;
                            case BindingStatus.Pending:
                                // Intentionally left empty.
                                break;
                        }
                    }
                }
            }

            foreach (var versionInfo in m_VersionChanges)
            {
                bindingManager.UpdateVersion(versionInfo.source, versionInfo.version);
            }

            ProcessPropertyChangedEvents(m_RanUpdate);

            foreach (var touchedSource in m_KnownSources)
            {
                bindingManager.ClearChangesFromSource(touchedSource);
            }

            m_BoundsElement.Clear();
            m_VersionChanges.Clear();
            m_TrackedObjects.Clear();
            m_RanUpdate.Clear();
            m_KnownSources.Clear();
            m_DirtyBindings.Clear();

            bindingManager.ClearSourceCache();
        }

        private (bool changed, long version) GetDataSourceVersion(object source)
        {
            if (bindingManager.TryGetLastVersion(source, out var version))
            {
                // If the data source is not versioned, we touch the version every update to keep it "fresh"
                if (source is not IDataSourceViewHashProvider versioned)
                    return (null != source, version + 1);

                var currentVersion = versioned.GetViewHashCode();

                // Version didn't change, no need to update the UI
                return currentVersion == version ? (false, version) : (true, currentVersion);
            }
            else if (source is IDataSourceViewHashProvider versioned)
            {
                return (true, versioned.GetViewHashCode());
            }

            return (null != source, 0L);
        }

        private bool IsPrefix(in PropertyPath prefix, in PropertyPath path)
        {
            if (path.Length < prefix.Length)
                return false;

            for (var i = 0; i < prefix.Length; ++i)
            {
                var prefixPart = prefix[i];
                var part = path[i];

                if (prefixPart.Kind != part.Kind)
                    return false;

                switch (prefixPart.Kind)
                {
                    case PropertyPathPartKind.Name:
                        if (prefixPart.Name != part.Name)
                            return false;
                        break;
                    case PropertyPathPartKind.Index:
                        if (prefixPart.Index != part.Index)
                            return false;
                        break;
                    case PropertyPathPartKind.Key:
                        if (prefixPart.Key != part.Key)
                            return false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return true;
        }

        void ProcessDataSourceChangedRequests()
        {
            using var marker = s_ProcessDataSourcesProfilerMarker.Auto();
            if (m_DataSourceChangedRequests.Count == 0 && m_RemovedElements.Count == 0)
                return;

            // skip elements that don't belong here. This can happen when a binding request happen while
            // an element is inside a panel, but then removed before this updater can run.
            m_DataSourceChangedRequests.RemoveWhere(e => null == e.panel);

            bindingManager.InvalidateCachedDataSource(m_DataSourceChangedRequests, m_RemovedElements);
            m_DataSourceChangedRequests.Clear();
            m_RemovedElements.Clear();
        }

        private void OnPanelChanged(BaseVisualElementPanel p)
        {
            if (m_AttachedPanel == p)
                return;

            if (null != m_AttachedPanel)
                m_AttachedPanel.hierarchyChanged -= OnHierarchyChange;

            m_AttachedPanel = p;

            if (null != m_AttachedPanel)
                m_AttachedPanel.hierarchyChanged += OnHierarchyChange;
        }

        protected override void Dispose(bool disposing)
        {
            ProcessAllBindingRequests();
            ProcessDataSourceChangedRequests();
            base.Dispose(disposing);
            bindingManager.Dispose();
        }

        void ProcessAllBindingRequests()
        {
            using var marker = s_ProcessBindingRequestsProfilerMarker.Auto();

            for (var i = 0; i < m_BindingRegistrationRequests.Count; ++i)
            {
                var element = m_BindingRegistrationRequests[i];
                // skip elements that don't belong here. This can happen when a binding request happen while
                // an element is inside a panel, but then removed before this updater can run.
                if (element.panel != panel)
                    continue;

                ProcessBindingRequests(element);
            }

            m_BindingRegistrationRequests.Clear();
        }

        void ProcessBindingRequests(VisualElement element)
        {
            bindingManager.ProcessBindingRequests(element);
        }

        void ProcessPropertyChangedEvents(HashSet<Binding> ranUpdate)
        {
            var data = bindingManager.GetChangedDetectedFromUI();

            for (var index = 0; index < data.Count; index++)
            {
                var change = data[index];
                if (!change.IsValid)
                    continue;

                var bindingData = change.bindingData;
                var binding = bindingData.binding;

                var element = bindingData.target.element;

                if (!m_Updater.ShouldProcessBindingAtStage(binding, BindingUpdateStage.UpdateSource, true, false))
                    continue;

                if (ranUpdate.Contains(binding))
                    continue;

                var resolvedContext = bindingManager.GetResolvedDataSourceContext(bindingData.target.element, bindingData);
                var source = resolvedContext.dataSource;
                var resolvedSourcePath = resolvedContext.dataSourcePath;

                var toDataSourceContext = new BindingContext
                (
                    element,
                    bindingData.target.bindingId,
                    resolvedSourcePath,
                    source
                );
                var result = m_Updater.UpdateSource(in toDataSourceContext, binding);
                CacheAndLogBindingResult(false, bindingData, result);

                if (result.status == BindingStatus.Success)
                {
                    // Binding was unregistered during the update.
                    if (!change.IsValid)
                        continue;

                    var wasDirty = bindingData.binding.isDirty;
                    bindingData.binding.ClearDirty();

                    var context = new BindingContext
                    (
                        element,
                        bindingData.target.bindingId,
                        resolvedSourcePath,
                        source
                    );
                    result = m_Updater.UpdateUI(in context, binding);
                    CacheAndLogBindingResult(true, bindingData, result);

                    if (result.status == BindingStatus.Pending)
                    {
                        if (wasDirty)
                            bindingData.binding.MarkDirty();
                        else
                            bindingData.binding.ClearDirty();
                    }
                }
            }

            data.Clear();
        }

        internal void PollElementsWithBindings(Action<VisualElement, IBinding> callback)
        {
            if (bindingManager.GetBoundElementsCount() > 0)
            {
                foreach (var element in bindingManager.GetUnorderedBoundElements())
                {
                    if (element.elementPanel == panel)
                    {
                        callback(element, null);
                    }
                }
            }
        }
    }
}
