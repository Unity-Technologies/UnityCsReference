// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine.Profiling;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    internal class VisualElementAnimationSystem : BaseVisualTreeUpdater
    {
        HashSet<IValueAnimationUpdate> m_Animations = new HashSet<IValueAnimationUpdate>();
        List<IValueAnimationUpdate> m_IterationList = new List<IValueAnimationUpdate>();

        bool m_HasNewAnimations = false;
        bool m_IterationListDirty = false;

        private static readonly string s_Description = "UIElements.UpdateAnimation";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;


        private static readonly string s_StylePropertyAnimationDescription = "UIElements.UpdateAnimationProperties";
        private static readonly ProfilerMarker s_StylePropertyAnimationProfilerMarker = new ProfilerMarker(s_StylePropertyAnimationDescription);
        private static ProfilerMarker stylePropertyAnimationProfilerMarker => s_StylePropertyAnimationProfilerMarker;

        public void UnregisterAnimation(IValueAnimationUpdate anim)
        {
            m_Animations.Remove(anim);
            m_IterationListDirty = true;
        }

        public void UnregisterAnimations(List<IValueAnimationUpdate> anims)
        {
            foreach (var a in anims)
                m_Animations.Remove(a);
            m_IterationListDirty = true;
        }

        public void RegisterAnimation(IValueAnimationUpdate anim)
        {
            m_Animations.Add(anim);

            m_HasNewAnimations = true;
            m_IterationListDirty = true;
        }

        public void RegisterAnimations(List<IValueAnimationUpdate> anims)
        {
            foreach (var a in anims)
                m_Animations.Add(a);

            m_HasNewAnimations = true;
            m_IterationListDirty = true;
        }

        double lastUpdate;
        public override void Update()
        {
            double now = panel.TimeSinceStartupSeconds();
            long nowMs = (long) (now * 1000.0);

            if (m_IterationListDirty)
            {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                m_IterationList = m_Animations.ToList();
#pragma warning restore RS0030
                m_IterationListDirty = false;
            }

            if (m_HasNewAnimations || lastUpdate != now)
            {
                foreach (var anim in m_IterationList)
                {
                    anim.Tick(nowMs);
                }

                m_HasNewAnimations = false;
                lastUpdate = now;
            }

            var styleAnim = panel.styleAnimationSystem;

            using (stylePropertyAnimationProfilerMarker.Auto())
            {
                styleAnim.Update(now);
            }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
        }
    }
}
