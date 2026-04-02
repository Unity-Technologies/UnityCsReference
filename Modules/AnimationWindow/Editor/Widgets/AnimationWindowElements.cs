// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

    class MainContentElement : AnimationWindowElement
    {
        [global::System.Runtime.CompilerServices.CompilerGenerated]
        [global::System.Serializable]
        internal new class UxmlSerializedData : global::UnityEngine.UIElements.VisualElement.UxmlSerializedData
        {

            public override object CreateInstance() => new MainContentElement();
        }

        public const string ussClassName = "animation-mainContent";

        public MainContentElement()
        {
            CreateUI(() =>
            {
                if (m_AnimEditor == null)
                    return;

                m_AnimEditor.Initialize();
                m_AnimEditor.MainContentOnGUI(contentRect);
            }, ussClassName);
        }
    }

    class DopeSheetElement : AnimationWindowElement
    {
        [global::System.Runtime.CompilerServices.CompilerGenerated]
        [global::System.Serializable]
        internal new class UxmlSerializedData : global::UnityEngine.UIElements.VisualElement.UxmlSerializedData
        {

            public override object CreateInstance() => new DopeSheetElement();
        }

        public const string ussClassName = "animation-dopeSheet";

        public DopeSheetElement()
        {
            CreateUI(() =>
            {
                if (m_AnimEditor == null)
                    return;

                m_AnimEditor.Initialize();
                m_AnimEditor.DopeSheetOnGUI(contentRect);
            }, ussClassName);
        }
    }

    class CurveEditorElement : AnimationWindowElement
    {
        [global::System.Runtime.CompilerServices.CompilerGenerated]
        [global::System.Serializable]
        internal new class UxmlSerializedData : global::UnityEngine.UIElements.VisualElement.UxmlSerializedData
        {

            public override object CreateInstance() => new CurveEditorElement();
        }

        public const string ussClassName = "animation-curveEditor";

        public CurveEditorElement()
        {
            CreateUI(() =>
            {
                if (m_AnimEditor == null)
                    return;

                m_AnimEditor.Initialize();
                m_AnimEditor.CurveEditorOnGUI(contentRect);
            }, ussClassName);
        }
    }

    class HierarchyElement : AnimationWindowElement
    {
        [global::System.Runtime.CompilerServices.CompilerGenerated]
        [global::System.Serializable]
        internal new class UxmlSerializedData : global::UnityEngine.UIElements.VisualElement.UxmlSerializedData
        {

            public override object CreateInstance() => new HierarchyElement();
        }

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
                    m_AnimEditor.HierarchyOnGUI(contentRect);
                }
            }, ussClassName);
        }
    }

    class AnimationEventTimelineElement : AnimationWindowElement
    {
        [global::System.Runtime.CompilerServices.CompilerGenerated]
        [global::System.Serializable]
        internal new class UxmlSerializedData : global::UnityEngine.UIElements.VisualElement.UxmlSerializedData
        {

            public override object CreateInstance() => new AnimationEventTimelineElement();
        }

        public const string ussClassName = "animation-eventTimeline";

        public AnimationEventTimelineElement()
        {
            CreateUI(() =>
            {
                if (m_AnimEditor == null)
                    return;

                m_AnimEditor.Initialize();
                m_AnimEditor.EventLineOnGUI(contentRect);
            }, ussClassName);
        }
    }
}
