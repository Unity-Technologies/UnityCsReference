using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    internal class VisualTreeBindingsUpdater : BaseVisualTreeHierarchyTrackerUpdater
    {
        private static readonly string s_Description = "Update Bindings";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        private readonly HashSet<VisualElement> m_ElementsWithBindings = new HashSet<VisualElement>();
        private readonly HashSet<VisualElement> m_ElementsToAdd = new HashSet<VisualElement>();
        private readonly HashSet<VisualElement> m_ElementsToRemove = new HashSet<VisualElement>();
        private const int kMinUpdateDelay = 100;
        private long m_LastUpdateTime = 0;

        static ProfilerMarker s_MarkerUpdate = new ProfilerMarker("Bindings.Update");
        static ProfilerMarker s_MarkerPoll = new ProfilerMarker("Bindings.PollElementsWithBindings");

        IBinding GetUpdaterFromElement(VisualElement ve)
        {
            return (ve as IBindable)?.binding;
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

        void StartTrackingRecursive(VisualElement ve)
        {
            var u = GetUpdaterFromElement(ve);
            if (u != null)
            {
                StartTracking(ve);
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
                if (GetUpdaterFromElement(ve) != null)
                {
                    StartTracking(ve);
                }
                else
                {
                    StopTracking(ve);
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

        public void PerformTrackingOperations()
        {
            foreach (var element in m_ElementsToAdd)
            {
                var updater = GetUpdaterFromElement(element);
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

            PerformTrackingOperations();

            if (m_ElementsWithBindings.Count > 0)
            {
                long currentTimeMs = CurrentTime();
                if (m_LastUpdateTime + kMinUpdateDelay < currentTimeMs)
                {
                    UpdateBindings();
                    m_LastUpdateTime = currentTimeMs;
                }
            }
        }

        private List<IBinding> updatedBindings = new List<IBinding>();
        private void UpdateBindings()
        {
            s_MarkerUpdate.Begin();
            foreach (VisualElement element in m_ElementsWithBindings)
            {
                var updater = GetUpdaterFromElement(element);

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
                    var updater = GetUpdaterFromElement(element);

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
