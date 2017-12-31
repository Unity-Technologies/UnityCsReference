// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public abstract class GraphElement : DataWatchContainer, ISelectable
    {
        GraphElementPresenter m_Presenter;

        // TODO: Remove when removing presenters.
        public bool dependsOnPresenter { get; private set; }

        public Color elementTypeColor { get; set; }


        StyleValue<int> m_Layer;
        public int layer
        {
            get { return m_Layer.value; }
            set
            {
                if (m_Layer.value == value)
                    return;
                m_Layer = value;
            }
        }

        public void ResetLayer()
        {
            int prevLayer = m_Layer.value;
            m_Layer = StyleValue<int>.nil;
            effectiveStyle.ApplyCustomProperty(k_LayerProperty, ref m_Layer);
            UpdateLayer(prevLayer);
        }

        const string k_LayerProperty = "layer";

        protected override void OnStyleResolved(ICustomStyle style)
        {
            base.OnStyleResolved(style);
            int prevLayer = m_Layer.value;
            style.ApplyCustomProperty(k_LayerProperty, ref m_Layer);
            UpdateLayer(prevLayer);
        }

        private void UpdateLayer(int prevLayer)
        {
            if (prevLayer != m_Layer.value)
            {
                GraphView view = GetFirstAncestorOfType<GraphView>();
                if (view != null)
                {
                    view.ChangeLayer(this);
                }
            }
        }

        private Capabilities m_Capabilities;
        public Capabilities capabilities
        {
            get { return m_Capabilities; }
            set
            {
                if (m_Capabilities == value)
                    return;

                m_Capabilities = value;

                if (IsSelectable() && m_ClickSelector == null)
                {
                    m_ClickSelector = new ClickSelector();
                    this.AddManipulator(m_ClickSelector);
                }
                else if (!IsSelectable() && m_ClickSelector != null)
                {
                    this.RemoveManipulator(m_ClickSelector);
                    m_ClickSelector = null;
                }
            }
        }

        private bool m_Selected;
        public bool selected
        {
            get { return m_Selected; }
            set
            {
                // Set new value (toggle old value)
                if ((capabilities & Capabilities.Selectable) != Capabilities.Selectable)
                    return;

                if (m_Selected == value)
                    return;

                m_Selected = value;

                if (m_Selected)
                {
                    AddToClassList("selected");
                }
                else
                {
                    RemoveFromClassList("selected");
                }
            }
        }

        protected GraphElement()
        {
            dependsOnPresenter = false;
            ClearClassList();
            AddToClassList("graphElement");
            elementTypeColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

            persistenceKey = Guid.NewGuid().ToString();
        }

        // TODO: Remove when removing presenters.
        public T GetPresenter<T>() where T : GraphElementPresenter
        {
            return presenter as T;
        }

        ClickSelector m_ClickSelector;

        // TODO: Remove when removing presenters.
        public GraphElementPresenter presenter
        {
            get { return m_Presenter; }
            set
            {
                if (m_Presenter == value)
                    return;

                RemoveWatch();

                m_Presenter = value;

                dependsOnPresenter = m_Presenter != null;

                OnDataChanged();
                AddWatch();
            }
        }

        protected override UnityEngine.Object[] toWatch
        {
            get { return presenter == null ? null : presenter.GetObjectsToWatch(); }
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

            capabilities = m_Presenter.capabilities;

            selected = presenter.selected;

            SetPosition(presenter.position);
        }

        public virtual bool IsSelectable()
        {
            return (capabilities & Capabilities.Selectable) == Capabilities.Selectable;
        }

        public virtual bool IsMovable()
        {
            return (capabilities & Capabilities.Movable) == Capabilities.Movable;
        }

        public virtual bool IsResizable()
        {
            return (capabilities & Capabilities.Resizable) == Capabilities.Resizable;
        }

        public virtual bool IsDroppable()
        {
            return (capabilities & Capabilities.Droppable) == Capabilities.Droppable;
        }

        public virtual bool IsAscendable()
        {
            return (capabilities & Capabilities.Ascendable) == Capabilities.Ascendable;
        }

        public virtual Vector3 GetGlobalCenter()
        {
            var center = layout.center;
            var globalCenter = new Vector3(center.x + parent.layout.x, center.y + parent.layout.y);
            return parent.worldTransform.MultiplyPoint3x4(globalCenter);
        }

        // TODO: Temporary transition function.
        public virtual void UpdatePresenterPosition()
        {
            if (presenter == null)
                return;

            RemoveWatch();
            presenter.position = GetPosition();
            presenter.CommitChanges();
            AddWatch();
        }

        public virtual Rect GetPosition()
        {
            return layout;
        }

        public virtual void SetPosition(Rect newPos)
        {
            // set absolute position from presenter
            layout = newPos;
        }

        public virtual void OnSelected()
        {
            if (IsAscendable())
            {
                BringToFront();
            }
        }

        public virtual void OnUnselected()
        {
        }

        public virtual bool HitTest(Vector2 localPoint)
        {
            return ContainsPoint(localPoint);
        }

        public virtual void Select(VisualElement selectionContainer, bool additive)
        {
            var gView = selectionContainer as GraphView;
            if (gView != null &&
                (parent == gView.contentViewContainer ||
                 (parent != null && parent.parent == gView.contentViewContainer)))
            {
                if (!gView.selection.Contains(this))
                {
                    if (!additive)
                        gView.ClearSelection();

                    gView.AddToSelection(this);
                }
            }
        }

        public virtual void Unselect(VisualElement  selectionContainer)
        {
            var gView = selectionContainer as GraphView;
            if (gView != null &&
                (parent == gView.contentViewContainer ||
                 (parent != null && parent.parent == gView.contentViewContainer)))
            {
                if (gView.selection.Contains(this))
                {
                    gView.RemoveFromSelection(this);
                }
            }
        }

        public virtual bool IsSelected(VisualElement selectionContainer)
        {
            var gView = selectionContainer as GraphView;
            if (gView != null &&
                (parent == gView.contentViewContainer ||
                 (parent != null && parent.parent == gView.contentViewContainer)))
            {
                if (gView.selection.Contains(this))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
