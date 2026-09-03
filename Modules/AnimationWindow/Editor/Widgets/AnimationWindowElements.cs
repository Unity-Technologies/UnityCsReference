// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: MecanimAnimation not yet converted
using System;
using UnityEngine.UIElements;
using Unity.Timeline.Foundation.Widgets;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    abstract class AnimationWindowElement : VisualElement
    {
        protected AnimEditor m_AnimEditor;

        const string ussClassName = "animation-element";

        protected void CreateUI(Action onGUIHandler, string inheritedUssClassName)
        {
            var imguiContainer = new IMGUIContainer(onGUIHandler);
            Add(imguiContainer);
            AddToClassList(ussClassName);
            AddToClassList(inheritedUssClassName);

            this.ApplyStyleSheet("StyleSheets/Animation/AnimationWindowElements.uss");
        }

        public void Initialize(AnimEditor animEditor)
        {
            m_AnimEditor = animEditor;
        }
    }

    [UxmlElement]
    partial class MainContentElement : AnimationWindowElement
    {
        public const string ussClassName = "animation-mainContent";

        public MainContentElement()
        {
            CreateUI(() =>
            {
                if (m_AnimEditor == null)
                    return;

                m_AnimEditor.Initialize();
                #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
                m_AnimEditor.MainContentOnGUI(contentRect);
                #pragma warning restore UAL0015
            }, ussClassName);
        }
    }

    [UxmlElement]
    partial class DopeSheetElement : AnimationWindowElement
    {
        public const string ussClassName = "animation-dopeSheet";

        public DopeSheetElement()
        {
            CreateUI(() =>
            {
                if (m_AnimEditor == null)
                    return;

                m_AnimEditor.Initialize();
                #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
                m_AnimEditor.DopeSheetOnGUI(contentRect);
                #pragma warning restore UAL0015
            }, ussClassName);
        }
    }

    [UxmlElement]
    partial class CurveEditorElement : AnimationWindowElement
    {
        public const string ussClassName = "animation-curveEditor";

        public CurveEditorElement()
        {
            CreateUI(() =>
            {
                if (m_AnimEditor == null)
                    return;

                m_AnimEditor.Initialize();
                #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
                m_AnimEditor.CurveEditorOnGUI(contentRect);
                #pragma warning restore UAL0015
            }, ussClassName);
        }
    }

    [UxmlElement]
    partial class HierarchyElement : AnimationWindowElement
    {
        public const string ussClassName = "animation-hierarchy";

        public HierarchyElement()
        {
            CreateUI(() =>
            {
                if (m_AnimEditor == null)
                    return;

                m_AnimEditor.Initialize();

                using (new EditorGUI.DisabledScope(m_AnimEditor.state.disabled || m_AnimEditor.state.animatorIsOptimized))
                {
                    #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
                    m_AnimEditor.HierarchyOnGUI(contentRect);
                    #pragma warning restore UAL0015
                }
            }, ussClassName);
        }
    }

    [UxmlElement]
    partial class AnimationEventTimelineElement : AnimationWindowElement
    {
        public const string ussClassName = "animation-eventTimeline";

        public AnimationEventTimelineElement()
        {
            CreateUI(() =>
            {
                if (m_AnimEditor == null)
                    return;

                m_AnimEditor.Initialize();
                #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
                m_AnimEditor.EventLineOnGUI(contentRect);
                #pragma warning restore UAL0015
            }, ussClassName);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
