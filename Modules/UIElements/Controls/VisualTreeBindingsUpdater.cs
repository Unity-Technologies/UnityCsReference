// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;

namespace UnityEngine.UIElements
{
    internal class VisualTreeBindingsUpdater : BaseVisualTreeHierarchyTrackerUpdater
    {
        public override string description
        {
            get { return "Update Bindings"; }
        }

        private readonly HashSet<VisualElement> m_ElementsWithBindings = new HashSet<VisualElement>();
        private readonly Dictionary<VisualElement,bool> m_ElementsToTrack = new Dictionary<VisualElement,bool>();
        private const int kMinUpdateDelay = 100;
        private long m_LastUpdateTime = 0;

        IBinding GetUpdaterFromElement(VisualElement ve)
        {
            return (ve as IBindable)?.binding;
        }

        void StartTrackingRecursive(VisualElement ve)
        {
            var u = GetUpdaterFromElement(ve);
            if (u != null)
            {
                m_ElementsToTrack[ve]	= true;
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
            m_ElementsToTrack[ve]	= false;

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
				m_ElementsToTrack[ve]	= GetUpdaterFromElement(ve) != null;
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
            foreach (var pair in m_ElementsToTrack)
            {
				if(!pair.Value)
				{
	                m_ElementsWithBindings.Remove(pair.Key);
				}
				else if(GetUpdaterFromElement(pair.Key) != null)
				{
                    m_ElementsWithBindings.Add(pair.Key);
				}
            }

            m_ElementsToTrack.Clear();
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
                    m_ElementsToTrack[element]	= false;
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

        internal void PollElementsWithBindings(Action<VisualElement, IBinding> callback)
        {
            Profiler.BeginSample("Binding.PollElementsWithBindings");

            PerformTrackingOperations();

            if (m_ElementsWithBindings.Count > 0)
            {
                foreach (VisualElement element in m_ElementsWithBindings)
                {
                    var updater = GetUpdaterFromElement(element);

                    if (updater == null || element.elementPanel != panel)
                    {
                        updater?.Release();
                        m_ElementsToTrack[element]	= false;
                    }
                    else
                    {
                        callback(element, updater);
                    }
                }
            }

            Profiler.EndSample();
        }
    }
}
