// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public abstract class GraphElement : VisualElement, ISelectable
    {
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

        public virtual string title
        {
            get { return name; }
            set {}
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
                    pseudoStates |= PseudoStates.Selected;
                }
                else
                {
                    pseudoStates &= ~PseudoStates.Selected;
                }
            }
        }

        protected GraphElement()
        {
            ClearClassList();
            AddToClassList("graphElement");
            elementTypeColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

            persistenceKey = Guid.NewGuid().ToString();
        }

        static GraphElement()
        {
            RegisterAll();
        }

        ClickSelector m_ClickSelector;

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
            // This can be overridden by derived class to get notified when a manipulator
            // has *finished* changing the layout (size or position) of this element.
        }

        public virtual Rect GetPosition()
        {
            return layout;
        }

        public virtual void SetPosition(Rect newPos)
        {
            layout = newPos;
        }

        public virtual void OnSelected()
        {
            if (IsAscendable() && style.positionType != PositionType.Relative)
                BringToFront();
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
            if (gView != null && selectionContainer.Contains(this))
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
            if (gView != null && selectionContainer.Contains(this))
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
            if (gView != null && selectionContainer.Contains(this))
            {
                if (gView.selection.Contains(this))
                {
                    return true;
                }
            }

            return false;
        }

        static void RegisterAll()
        {
            VisualElementFactoryRegistry.RegisterFactory(new Pill.UxmlFactory());
        }
    }
}
