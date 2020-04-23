namespace UnityEngine.UIElements
{
    internal enum HierarchyChangeType
    {
        Add,
        Remove,
        Move
    }

    internal abstract class BaseVisualTreeHierarchyTrackerUpdater : BaseVisualTreeUpdater
    {
        enum State
        {
            Waiting,
            TrackingAddOrMove,
            TrackingRemove,
        }

        private State m_State = State.Waiting;

        private VisualElement m_CurrentChangeElement;
        private VisualElement m_CurrentChangeParent;

        protected abstract void OnHierarchyChange(VisualElement ve, HierarchyChangeType type);

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & VersionChangeType.Hierarchy) == VersionChangeType.Hierarchy)
            {
                switch (m_State)
                {
                    case State.Waiting:
                        ProcessNewChange(ve);
                        break;
                    case State.TrackingRemove:
                        ProcessRemove(ve);
                        break;
                    case State.TrackingAddOrMove:
                        ProcessAddOrMove(ve);
                        break;
                }
            }
        }

        public override void Update()
        {
            Debug.Assert(m_State == State.TrackingAddOrMove || m_State == State.Waiting);
            if (m_State == State.TrackingAddOrMove)
            {
                // Still waiting for a parent add change
                // which means that last change was a move
                OnHierarchyChange(m_CurrentChangeElement, HierarchyChangeType.Move);
                m_State = State.Waiting;
            }

            m_CurrentChangeElement = null;
            m_CurrentChangeParent = null;
        }

        private void ProcessNewChange(VisualElement ve)
        {
            // Children are always the first to receive a Hierarchy change
            m_CurrentChangeElement = ve;
            m_CurrentChangeParent = ve.parent;

            if (m_CurrentChangeParent == null && ve.panel != null)
            {
                // The changed element is the VisualTree root so it has to be a move.
                OnHierarchyChange(m_CurrentChangeElement, HierarchyChangeType.Move);
                m_State = State.Waiting;
            }
            else
            {
                m_State = m_CurrentChangeParent == null ? State.TrackingRemove : State.TrackingAddOrMove;
            }
        }

        private void ProcessAddOrMove(VisualElement ve)
        {
            Debug.Assert(m_CurrentChangeParent != null);
            if (m_CurrentChangeParent == ve)
            {
                OnHierarchyChange(m_CurrentChangeElement, HierarchyChangeType.Add);
                m_State = State.Waiting;
            }
            else
            {
                // This is a new change, last change was a move
                OnHierarchyChange(m_CurrentChangeElement, HierarchyChangeType.Move);
                ProcessNewChange(ve);
            }
        }

        private void ProcessRemove(VisualElement ve)
        {
            OnHierarchyChange(m_CurrentChangeElement, HierarchyChangeType.Remove);
            if (ve.panel != null)
            {
                // This is the parent (or VisualTree root) of the removed children
                m_CurrentChangeParent = null;
                m_CurrentChangeElement = null;
                m_State = State.Waiting;
            }
            else
            {
                m_CurrentChangeElement = ve;
            }
        }
    }
}
