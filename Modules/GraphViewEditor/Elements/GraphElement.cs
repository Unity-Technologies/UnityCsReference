// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    abstract class GraphElement : DataWatchContainer, ISelectable
    {
        GraphElementPresenter m_Presenter;

        public Color elementTypeColor { get; set; }

        public virtual int layer { get { return 0; } }

        protected GraphElement()
        {
            ClearClassList();
            AddToClassList("graphElement");
            elementTypeColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);
        }

        public T GetPresenter<T>() where T : GraphElementPresenter
        {
            return presenter as T;
        }

        ClickSelector m_ClickSelector;

        public GraphElementPresenter presenter
        {
            get { return m_Presenter; }
            set
            {
                if (m_Presenter == value)
                    return;
                RemoveWatch();
                if (m_ClickSelector != null)
                {
                    this.RemoveManipulator(m_ClickSelector);
                    m_ClickSelector = null;
                }
                m_Presenter = value;

                if (IsSelectable())
                {
                    m_ClickSelector = new ClickSelector();
                    this.AddManipulator(m_ClickSelector);
                }
                OnDataChanged();
                AddWatch();
            }
        }

        protected override UnityEngine.Object[] toWatch
        {
            get { return presenter.GetObjectsToWatch(); }
        }

        public override void OnDataChanged()
        {
            if (presenter == null)
            {
                return;
            }

            // propagate selection but why?
            foreach (var child in this)
            {
                var graphElement = child as GraphElement;
                if (graphElement != null)
                {
                    GraphElementPresenter childPresenter = graphElement.presenter;
                    if (childPresenter != null)
                    {
                        childPresenter.selected = presenter.selected;
                    }
                }
            }

            if (presenter.selected)
            {
                AddToClassList("selected");
            }
            else
            {
                RemoveFromClassList("selected");
            }

            SetPosition(presenter.position);
        }

        public virtual bool IsSelectable()
        {
            return (presenter.capabilities & Capabilities.Selectable) == Capabilities.Selectable;
        }

        public virtual Vector3 GetGlobalCenter()
        {
            var center = layout.center;
            var globalCenter = new Vector3(center.x + parent.layout.x, center.y + parent.layout.y);
            return parent.worldTransform.MultiplyPoint3x4(globalCenter);
        }

        public virtual void SetPosition(Rect newPos)
        {
            // set absolute position from presenter
            layout = newPos;
        }

        public virtual void OnSelected()
        {
        }

        public virtual void Select(GraphView selectionContainer, bool additive)
        {
            if (selectionContainer != null)
            {
                if (!selectionContainer.selection.Contains(this))
                {
                    if (!additive)
                        selectionContainer.ClearSelection();

                    selectionContainer.AddToSelection(this);
                }
            }
        }

        public virtual void Unselect(GraphView selectionContainer)
        {
            if (selectionContainer != null && parent == selectionContainer.contentViewContainer)
            {
                if (selectionContainer.selection.Contains(this))
                {
                    selectionContainer.RemoveFromSelection(this);
                }
            }
        }

        public virtual bool IsSelected(GraphView selectionContainer)
        {
            if (selectionContainer != null && parent == selectionContainer.contentViewContainer)
            {
                if (selectionContainer.selection.Contains(this))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
