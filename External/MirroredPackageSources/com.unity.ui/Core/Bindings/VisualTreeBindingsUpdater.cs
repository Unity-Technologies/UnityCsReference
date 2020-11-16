using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    internal interface IBindingRequest
    {
        void Bind(VisualElement element);
        void Release();
    }


    internal class VisualTreeBindingsUpdater : BaseVisualTreeHierarchyTrackerUpdater
    {
        private static readonly PropertyName s_BindingRequestObjectVEPropertyName = "__unity-binding-request-object";
        private static readonly PropertyName s_AdditionalBindingObjectVEPropertyName = "__unity-additional-binding-object";

        private static readonly string s_Description = "Update Bindings";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        private static readonly ProfilerMarker s_ProfilerBindingRequestsMarker = new ProfilerMarker("Bindings.Requests");
        static ProfilerMarker s_MarkerUpdate = new ProfilerMarker("Bindings.Update");
        static ProfilerMarker s_MarkerPoll = new ProfilerMarker("Bindings.PollElementsWithBindings");
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        public static bool disableBindingsThrottling { get; set; } = false;

        private readonly HashSet<VisualElement> m_ElementsWithBindings = new HashSet<VisualElement>();
        private readonly HashSet<VisualElement> m_ElementsToAdd = new HashSet<VisualElement>();
        private readonly HashSet<VisualElement> m_ElementsToRemove = new HashSet<VisualElement>();
        private const int k_MinUpdateDelayMs = 100;
        private const int k_MaxBindingTimeMs = 100;
        private long m_LastUpdateTime = 0;


        private HashSet<VisualElement> m_ElementsToBind = new HashSet<VisualElement>();

        IBinding GetBindingObjectFromElement(VisualElement ve)
        {
            if (ve is IBindable bindable)
            {
                if (bindable.binding != null)
                    return bindable.binding;
            }

            return GetAdditionalBinding(ve);
        }

        void StartTracking(VisualElement ve)
        {
            m_ElementsToAdd.Add(ve);
            m_ElementsToRemove.Remove(ve);
        }

        void StopTracking(VisualElement ve)
        {
            m_ElementsToRemove.Add(ve);
            m_ElementsToAdd.Remove(ve);
        }

        // A temporary cache that is cleared every frame
        public Dictionary<object, object> temporaryObjectCache { get; private set; } = new Dictionary<object, object>();


        class RequestObjectListPool : ObjectListPool<IBindingRequest>
        {
        }

        // When this becomes more common, this should be a member list straight inside VisualElement
        public static void SetAdditionalBinding(VisualElement ve, IBinding b)
        {
            var current = GetAdditionalBinding(ve);
            current?.Release();
            ve.SetProperty(s_AdditionalBindingObjectVEPropertyName, b);
            ve.IncrementVersion(VersionChangeType.Bindings);
        }

        public static void ClearAdditionalBinding(VisualElement ve)
        {
            SetAdditionalBinding(ve, null);
        }

        public static IBinding GetAdditionalBinding(VisualElement ve)
        {
            return ve.GetProperty(s_AdditionalBindingObjectVEPropertyName) as IBinding;
        }

        public static void AddBindingRequest(VisualElement ve, IBindingRequest req)
        {
            List<IBindingRequest> l = ve.GetProperty(s_BindingRequestObjectVEPropertyName) as List<IBindingRequest>;

            if (l == null)
            {
                l = RequestObjectListPool.Get();
                ve.SetProperty(s_BindingRequestObjectVEPropertyName, l);
            }

            l.Add(req);
            ve.IncrementVersion(VersionChangeType.Bindings);
        }

        public static void RemoveBindingRequest(VisualElement ve, IBindingRequest req)
        {
            List<IBindingRequest> l = ve.GetProperty(s_BindingRequestObjectVEPropertyName) as List<IBindingRequest>;

            if (l != null)
            {
                req.Release();
                l.Remove(req);
                if (l.Count == 0)
                {
                    RequestObjectListPool.Release(l);
                    ve.SetProperty(s_BindingRequestObjectVEPropertyName, null);
                }
            }
        }

        public static void ClearBindingRequests(VisualElement ve)
        {
            List<IBindingRequest> l = ve.GetProperty(s_BindingRequestObjectVEPropertyName) as List<IBindingRequest>;

            if (l != null)
            {
                foreach (var r in l)
                {
                    r.Release();
                }

                RequestObjectListPool.Release(l);
                ve.SetProperty(s_BindingRequestObjectVEPropertyName, null);
            }
        }

        void StartTrackingRecursive(VisualElement ve)
        {
            var u = GetBindingObjectFromElement(ve);
            if (u != null)
            {
                StartTracking(ve);
            }

            var bindingRequest = ve.GetProperty(s_BindingRequestObjectVEPropertyName);
            if (bindingRequest != null)
            {
                m_ElementsToBind.Add(ve);
            }

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];
                StartTrackingRecursive(child);
            }
        }

        void StopTrackingRecursive(VisualElement ve)
        {
            StopTracking(ve);

            var bindingRequest = ve.GetProperty(s_BindingRequestObjectVEPropertyName);
            if (bindingRequest != null)
            {
                m_ElementsToBind.Remove(ve);
            }

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];
                StopTrackingRecursive(child);
            }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            base.OnVersionChanged(ve, versionChangeType);

            if ((versionChangeType & VersionChangeType.Bindings) == VersionChangeType.Bindings)
            {
                if (GetBindingObjectFromElement(ve) != null)
                {
                    StartTracking(ve);
                }
                else
                {
                    StopTracking(ve);
                }

                var bindingRequests = ve.GetProperty(s_BindingRequestObjectVEPropertyName);
                if (bindingRequests != null)
                {
                    m_ElementsToBind.Add(ve);
                }
            }
        }

        protected override void OnHierarchyChange(VisualElement ve, HierarchyChangeType type)
        {
            switch (type)
            {
                case HierarchyChangeType.Add:
                    StartTrackingRecursive(ve);
                    break;
                case HierarchyChangeType.Remove:
                    StopTrackingRecursive(ve);
                    break;
                default:
                    break;
            }
        }

        private static long CurrentTime()
        {
            return Panel.TimeSinceStartupMs();
        }

        public static bool ShouldThrottle(long startTime)
        {
            return !disableBindingsThrottling &&  (CurrentTime() - startTime) < k_MaxBindingTimeMs;
        }

        public void PerformTrackingOperations()
        {
            foreach (var element in m_ElementsToAdd)
            {
                var updater = GetBindingObjectFromElement(element);
                if (updater != null)
                {
                    m_ElementsWithBindings.Add(element);
                }
            }

            m_ElementsToAdd.Clear();


            foreach (var element in m_ElementsToRemove)
            {
                m_ElementsWithBindings.Remove(element);
            }

            m_ElementsToRemove.Clear();
        }

        public override void Update()
        {
            base.Update();

            if (m_ElementsToBind.Count > 0)
            {
                using (s_ProfilerBindingRequestsMarker.Auto())
                {
                    long startTime = CurrentTime();
                    while (m_ElementsToBind.Count > 0 && (CurrentTime() - startTime) < k_MaxBindingTimeMs)
                    {
                        var element = m_ElementsToBind.FirstOrDefault();

                        if (element != null)
                        {
                            m_ElementsToBind.Remove(element);

                            var bindingRequests =
                                element.GetProperty(s_BindingRequestObjectVEPropertyName) as List<IBindingRequest>;

                            if (bindingRequests != null)
                            {
                                element.SetProperty(s_BindingRequestObjectVEPropertyName, null);

                                foreach (var req in bindingRequests)
                                {
                                    req.Bind(element);
                                }

                                RequestObjectListPool.Release(bindingRequests);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            PerformTrackingOperations();

            if (m_ElementsWithBindings.Count > 0)
            {
                long currentTimeMs = CurrentTime();
                if (m_LastUpdateTime + k_MinUpdateDelayMs < currentTimeMs)
                {
                    UpdateBindings();
                    m_LastUpdateTime = currentTimeMs;
                }
            }

            if (m_ElementsToBind.Count == 0)
            {
                temporaryObjectCache.Clear(); //We don't want to keep references to stuff needlessly
            }
        }

        private List<IBinding> updatedBindings = new List<IBinding>();
        private void UpdateBindings()
        {
            s_MarkerUpdate.Begin();
            foreach (VisualElement element in m_ElementsWithBindings)
            {
                var updater = GetBindingObjectFromElement(element);

                if (updater == null || element.elementPanel != panel)
                {
                    updater?.Release();
                    StopTracking(element);
                }
                else
                {
                    updatedBindings.Add(updater);
                }
            }

            foreach (var u in updatedBindings)
            {
                u.PreUpdate();
            }

            foreach (var u in updatedBindings)
            {
                u.Update();
            }

            updatedBindings.Clear();
            s_MarkerUpdate.End();
        }

        internal void PollElementsWithBindings(Action<VisualElement, IBinding> callback)
        {
            s_MarkerPoll.Begin();

            PerformTrackingOperations();

            if (m_ElementsWithBindings.Count > 0)
            {
                foreach (VisualElement element in m_ElementsWithBindings)
                {
                    var updater = GetBindingObjectFromElement(element);

                    if (updater == null || element.elementPanel != panel)
                    {
                        updater?.Release();
                        StopTracking(element);
                    }
                    else
                    {
                        callback(element, updater);
                    }
                }
            }

            s_MarkerPoll.End();
        }
    }
}
