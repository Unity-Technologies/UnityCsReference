using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderPaneContent : VisualElement
    {
        BuilderPane m_Pane;
        List<VisualElement> m_Focusables = new List<VisualElement>();
        VisualElement m_PrimaryFocusable;

        public VisualElement primaryFocusable
        {
            get { return m_PrimaryFocusable; }
            protected set
            {
                if (m_PrimaryFocusable == value)
                    return;

                if (m_PrimaryFocusable != null)
                {
                    m_PrimaryFocusable.UnregisterCallback<FocusEvent>(OnChildFocus);
                    m_PrimaryFocusable.UnregisterCallback<BlurEvent>(OnChildBlur);
                }

                m_PrimaryFocusable = value;
                m_PrimaryFocusable.RegisterCallback<FocusEvent>(OnChildFocus);
                m_PrimaryFocusable.RegisterCallback<BlurEvent>(OnChildBlur);
            }
        }

        public BuilderPane pane
        {
            get { return m_Pane; }
        }

        public BuilderPaneContent()
        {
        }

        protected void AddFocusable(VisualElement focusable)
        {
            m_Focusables.Add(focusable);

            focusable.RegisterCallback<FocusEvent>(OnChildFocus);
            focusable.RegisterCallback<BlurEvent>(OnChildBlur);
            focusable.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected void RemoveFocusable(VisualElement focusable)
        {
            if (!m_Focusables.Remove(focusable))
                return;

            focusable.UnregisterCallback<FocusEvent>(OnChildFocus);
            focusable.UnregisterCallback<BlurEvent>(OnChildBlur);
            focusable.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.eventTypeId == AttachToPanelEvent.TypeId())
            {
                OnAttachToPanelDefaultAction();
            }
        }

        protected virtual void InitEllipsisMenu()
        {
            // Override this method to add Actions to the pane's ellipsis menu.
        }

        protected virtual void OnAttachToPanelDefaultAction()
        {
            m_Pane = GetFirstAncestorOfType<BuilderPane>();
            m_Pane?.RegisterCallback<FocusEvent>(OnPaneFocus);
            InitEllipsisMenu();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var focusable = evt.target as VisualElement;
            focusable.UnregisterCallback<FocusEvent>(OnChildFocus);
            focusable.UnregisterCallback<BlurEvent>(OnChildBlur);
        }

        void OnChildFocus(FocusEvent evt)
        {
            if (m_Pane != null)
                m_Pane.pseudoStates = m_Pane.pseudoStates | PseudoStates.Focus;
        }

        void OnChildBlur(BlurEvent evt)
        {
            if (m_Pane != null)
                m_Pane.pseudoStates = m_Pane.pseudoStates & ~PseudoStates.Focus;
        }

        void OnPaneFocus(FocusEvent evt)
        {
            primaryFocusable?.Focus();
        }
    }
}
