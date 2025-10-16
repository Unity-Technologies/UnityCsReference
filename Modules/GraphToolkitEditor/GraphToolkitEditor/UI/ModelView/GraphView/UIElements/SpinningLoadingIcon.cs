// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A spinning loading icon.
    /// </summary>
    [UnityRestricted]
    internal class SpinningLoadingIcon : VisualElement
    {
        /// <summary>
        /// The USS class name of a <see cref="SpinningLoadingIcon"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-spinning-loading-icon";

        /// <summary>
        /// The USS class name of the spinning loading icon.
        /// </summary>
        public static readonly string spinningLoadingIconUssClassName = ussClassName.WithUssElement(GraphElementHelper.iconName);

        /// <summary>
        /// The USS class name of the rotating transition of the loading icon.
        /// </summary>
        public static readonly string rotateIconUssClassName = ussClassName.WithUssModifier("rotate");

        GraphView m_GraphView;
        bool m_Attached;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpinningLoadingIcon"/> class.
        /// </summary>
        public SpinningLoadingIcon()
        {
            this.AddPackageStylesheet("SpinningLoadingIcon.uss");
            AddToClassList(ussClassName);

            var icon = new VisualElement { name = GraphElementHelper.iconName };
            icon.AddToClassList(spinningLoadingIconUssClassName);
            Add(icon);

            RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
            schedule.Execute(() => AddToClassList(rotateIconUssClassName)).StartingIn(100);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            m_GraphView ??= GetFirstAncestorOfType<GraphView>();
            if (!m_Attached)
            {
                m_GraphView.RegisterElementZoomLevelClass(this, GraphViewZoomMode.VerySmall, ussClassName.WithUssModifier(GraphElementHelper.mediumUssModifier));
                m_Attached = true;
            }
        }

        void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            if (m_Attached)
            {
                m_GraphView.UnregisterElementZoomLevelClass(this, GraphViewZoomMode.VerySmall);
                m_Attached = false;
            }
        }

        void OnTransitionEnd(TransitionEndEvent evt)
        {
            RemoveFromClassList(rotateIconUssClassName);
            schedule.Execute(() => AddToClassList(rotateIconUssClassName));
        }
    }
}
