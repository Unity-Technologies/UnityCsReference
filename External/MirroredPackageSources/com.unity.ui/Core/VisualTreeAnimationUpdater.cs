using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    internal class VisualElementAnimationSystem : BaseVisualTreeUpdater
    {
        long CurrentTimeMs()
        {
            return Panel.TimeSinceStartupMs();
        }

        HashSet<IValueAnimationUpdate> m_Animations = new HashSet<IValueAnimationUpdate>();
        List<IValueAnimationUpdate> m_IterationList = new List<IValueAnimationUpdate>();

        bool m_HasNewAnimations = false;
        bool m_IterationListDirty = false;

        private static readonly string s_Description = "Animation Update";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

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

        long lastUpdate;
        public override void Update()
        {
            long now = Panel.TimeSinceStartupMs();

            if (m_IterationListDirty)
            {
                m_IterationList = m_Animations.ToList();
                m_IterationListDirty = false;
            }

            if (m_HasNewAnimations || lastUpdate != now)
            {
                foreach (var anim in m_IterationList)
                {
                    anim.Tick(now);
                }

                m_HasNewAnimations = false;
                lastUpdate = now;
            }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
        }
    }
}
