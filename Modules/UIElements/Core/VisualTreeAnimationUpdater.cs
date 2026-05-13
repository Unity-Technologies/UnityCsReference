// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.Profiling;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    internal class VisualElementAnimationSystem : BaseVisualTreeUpdater
    {
        HashSet<IValueAnimationUpdate> m_Animations = new HashSet<IValueAnimationUpdate>();
        List<IValueAnimationUpdate> m_IterationList = new List<IValueAnimationUpdate>();
        List<UIAnimationBinder> m_AnimationBinders;

        bool m_HasNewAnimations = false;
        bool m_IterationListDirty = false;

        private static readonly string s_Description = "UIElements.UpdateAnimation";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;


        private static readonly string s_StylePropertyAnimationDescription = "UIElements.UpdateAnimationProperties";
        private static readonly ProfilerMarker s_StylePropertyAnimationProfilerMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, s_StylePropertyAnimationDescription);
        private static ProfilerMarker stylePropertyAnimationProfilerMarker => s_StylePropertyAnimationProfilerMarker;

        // Set of elements whose clip-related resolved style (unity-animation-clip / animation-play-state)
        // has changed since the last update. Forwarded to panel.styleAnimationSystem, which owns clip
        // playback so it can be skipped when the panel uses the empty animation system.
        HashSet<VisualElement> m_DirtyElements;

        public void UnregisterAnimation(IValueAnimationUpdate anim)
        {
            if (anim is UIAnimationBinder b)
            {
                UnregisterAnimationBinder(b);
            }
            else
            {
                m_Animations.Remove(anim);
                m_IterationListDirty = true;
            }
        }

        public void UnregisterAnimations(List<IValueAnimationUpdate> anims)
        {
            foreach (var a in anims)
            {
                if (a is UIAnimationBinder b)
                {
                    UnregisterAnimationBinder(b);
                }
                else
                {
                    m_Animations.Remove(a);
                }
            }

            m_IterationListDirty = true;
        }

        public void RegisterAnimation(IValueAnimationUpdate anim)
        {
            if (anim is UIAnimationBinder b)
            {
                RegisterAnimationBinder(b);
            }
            else
            {
                m_Animations.Add(anim);
                m_HasNewAnimations = true;
                m_IterationListDirty = true;
            }

        }

        public void RegisterAnimations(List<IValueAnimationUpdate> anims)
        {
            foreach (var a in anims)
            {
                if (a is UIAnimationBinder b)
                {
                    RegisterAnimationBinder(b);
                } else
                {
                    m_Animations.Add(a);
                }
            }

            m_HasNewAnimations = true;
            m_IterationListDirty = true;
        }

        private void RegisterAnimationBinder(UIAnimationBinder binder)
        {
            m_AnimationBinders ??= new List<UIAnimationBinder>();
            m_AnimationBinders.Add(binder);
        }

        private void UnregisterAnimationBinder(UIAnimationBinder binder)
        {
            m_AnimationBinders?.Remove(binder);
        }

        void RebuildIterationList()
        {
            m_IterationList.Clear();
            m_IterationList.AddRange(m_Animations);
            m_IterationListDirty = false;
        }

        double lastUpdate;
        public override void Update()
        {
            double now = panel.TimeSinceStartupSeconds();
            long nowMs = (long)(now * 1000.0);

            if (m_IterationListDirty)
                RebuildIterationList();

            if (m_HasNewAnimations || lastUpdate != now)
            {
                foreach (var anim in m_IterationList)
                {
                    anim.Tick(nowMs);
                }

                m_HasNewAnimations = false;
                lastUpdate = now;
            }

            if (m_AnimationBinders != null)
            {
                ReapplyAnimationBinderValues();
            }

            var styleAnim = panel.styleAnimationSystem;
            ForwardDirtyElementsToStyleAnimation(styleAnim);

            using (stylePropertyAnimationProfilerMarker.Auto())
            {
                styleAnim.Update(now);
            }
        }

        internal bool hasActiveAnimationBinders => m_AnimationBinders != null && m_AnimationBinders.Count > 0;

        // Re-apply the last sampled binder values without advancing time, used by
        // VisualTreeStyleUpdater so animation overrides survive style re-resolution.
        internal void ReapplyAnimationBinderValues()
        {
            // if the native Unity Object was destroyed, it calls UnregisterRootElement(),
            // that will in turn call m_AnimationBinders?.Remove(binder).
            for (int i = m_AnimationBinders.Count - 1; i >= 0; i--)
                m_AnimationBinders[i].ApplyAnimatedValues();
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & VersionChangeType.AnimationProperty) == 0)
                return;

            m_DirtyElements ??= new HashSet<VisualElement>();
            m_DirtyElements.Add(ve);
        }

        void ForwardDirtyElementsToStyleAnimation(IStylePropertyAnimationSystem styleAnim)
        {
            if (m_DirtyElements == null || m_DirtyElements.Count == 0)
                return;

            foreach (var ve in m_DirtyElements)
            {
                styleAnim.UpdateElementClipAnimation(
                    ve,
                    ve.resolvedStyle.unityAnimationClip,
                    ve.resolvedStyle.animationPlayState);
            }

            m_DirtyElements.Clear();
        }
    }
}
