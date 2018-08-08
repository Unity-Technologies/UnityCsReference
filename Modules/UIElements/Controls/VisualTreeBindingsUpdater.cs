// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;

namespace UnityEngine.Experimental.UIElements
{
    internal class VisualTreeBindingsUpdater : BaseVisualTreeHierarchyTrackerUpdater
    {
        public override string description
        {
            get { return "Update Bindings"; }
        }

        private readonly HashSet<VisualElement> m_ElementsWithBindings = new HashSet<VisualElement>();
        private readonly HashSet<VisualElement> m_ElementsToAdd = new HashSet<VisualElement>();
        private readonly HashSet<VisualElement> m_ElementsToRemove = new HashSet<VisualElement>();
        private const int kMinUpdateDelay = 100;
        private long m_LastUpdateTime = 0;

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

            int count = ve.shadow.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.shadow[i];
                StartTrackingRecursive(child);
            }
        }

        void StopTrackingRecursive(VisualElement ve)
        {
            StopTracking(ve);

            int count = ve.shadow.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.shadow[i];
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
            return Panel.TimeSinceStartup();
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
            Profiler.BeginSample("Binding.Update");
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
            Profiler.EndSample();
        }
    }
}
