// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// An element that is attached to another element.
    /// </summary>
    abstract class Marker : GraphElement
    {
        static readonly CustomStyleProperty<int> k_HorizontalDistanceProperty = new CustomStyleProperty<int>("--horizontal-distance");
        static readonly CustomStyleProperty<int> k_VerticalDistanceProperty = new CustomStyleProperty<int>("--vertical-distance");
        static readonly int k_DefaultDistanceValue = 6;

        protected VisualElement m_Target;
        protected VisualElement m_OriginalParent;
        protected Vector2 m_Distance;

        protected Attacher Attacher { get; private set; }
        protected SpriteAlignment Alignment { get; private set; }
        public abstract GraphElementModel ParentModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Marker"/> class.
        /// </summary>
        protected Marker()
        {
            m_Distance = new Vector2(k_DefaultDistanceValue, k_DefaultDistanceValue);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected virtual void Attach()
        {
            var visualElement = ParentModel?.GetView<GraphElement>(RootView);
            if (visualElement != null)
            {
                AttachTo(visualElement, SpriteAlignment.RightCenter);
            }
        }

        protected void AttachTo(VisualElement target, SpriteAlignment alignment)
        {
            if (target == m_Target)
                return;

            Detach();
            m_Target = target;
            Alignment = alignment;
            target.RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
            CreateAttacher();
        }

        protected virtual void Detach()
        {
            if (m_Target != null)
            {
                m_Target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
                m_Target = null;
            }

            ReleaseAttacher();
            m_OriginalParent = null;
        }

        protected void OnTargetDetachedFromPanel(DetachFromPanelEvent evt)
        {
            ReleaseAttacher();
            if (m_Target != null)
            {
                m_OriginalParent = hierarchy.parent;
                RemoveFromHierarchy();

                m_Target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
                m_Target.RegisterCallback<AttachToPanelEvent>(OnTargetAttachedToPanel);
            }
        }

        protected void OnTargetAttachedToPanel(AttachToPanelEvent evt)
        {
            if (m_Target != null)
            {
                m_Target.RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);

                //we re-add ourselves to the hierarchy
                m_OriginalParent?.hierarchy.Add(this);
                ReleaseAttacher();
                // the attacher will complain if reattaching too quickly when the hierarchy just entered the panel
                // ie. when switching back to the vs window tab
                schedule.Execute(CreateAttacher).StartingIn(0);
            }
        }

        protected void ReleaseAttacher()
        {
            if (Attacher != null)
            {
                Attacher.Detach();
                Attacher = null;
            }
        }

        protected void CreateAttacher()
        {
            Attacher = new Attacher(this, m_Target, Alignment) { Distance = m_Distance };
        }

        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            base.OnCustomStyleResolved(e);

            if (e.customStyle.TryGetValue(k_HorizontalDistanceProperty, out var distX) && e.customStyle.TryGetValue(k_VerticalDistanceProperty, out var distY))
                m_Distance = new Vector2(distX, distY);
        }

        protected void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Attach();
        }

        protected void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ReleaseAttacher();
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (ParentModel?.IsSelectable() ?? false)
                ClickSelector ??= CreateClickSelector();
        }

        /// <inheritdoc />
        public override bool CanBePartitioned()
        {
            // We override GraphElement's behavior because a marker doesn't have a GraphElementModel.
            return true;
        }
    }
}
