// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;

namespace UnityEngine.Experimental.UIElements
{
    internal class VisualTreeBindingsUpdater : BaseVisualTreeUpdater
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

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & VersionChangeType.Hierarchy) == VersionChangeType.Hierarchy)
            {
                if (ve.parent == null)
                {
                    StopTracking(ve);
                }
                else
                {
                    var u = GetUpdaterFromElement(ve);

                    if (u != null)
                    {
                        StartTracking(ve);
                    }
                }
            }

            if ((versionChangeType & VersionChangeType.Bindings) == VersionChangeType.Bindings)
            {
                var u = GetUpdaterFromElement(ve);

                if (u != null)
                {
                    StartTracking(ve);
                }
                else
                {
                    StopTracking(ve);
                }
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
                m_ElementsWithBindings.Add(element);
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

        private void UpdateBindings()
        {
            Profiler.BeginSample("Binding.Update");
            foreach (VisualElement element in m_ElementsWithBindings)
            {
                var updater = GetUpdaterFromElement(element);

                if (updater == null || element.elementPanel != panel)
                {
                    StopTracking(element);
                }
                else
                {
                    updater.Update();
                }
            }
            Profiler.EndSample();
        }
    }
}
