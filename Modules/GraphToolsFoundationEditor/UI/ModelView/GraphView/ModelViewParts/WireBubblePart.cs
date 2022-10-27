// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for a text bubble on a wire.
    /// </summary>
    class WireBubblePart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-wire-bubble-part";

        /// <summary>
        /// Creates a new instance of the <see cref="WireBubblePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="WireBubblePart"/>.</returns>
        public static WireBubblePart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is WireModel)
            {
                return new WireBubblePart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected WireBubble m_WireBubble;

        /// <inheritdoc />
        public override VisualElement Root => m_WireBubble;

        /// <summary>
        /// Initializes a new instance of the <see cref="WireBubblePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected WireBubblePart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            m_WireBubble = new WireBubble { name = PartName };
            m_WireBubble.AddToClassList(ussClassName);
            m_WireBubble.AddToClassList(m_ParentClassName.WithUssElement(PartName));
            container.Add(m_WireBubble);
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_WireBubble.AddStylesheet_Internal("WireBubblePart.uss");
        }

        internal static bool WireShouldShowLabel_Internal(WireModel model, SelectionStateComponent selectionStateComponent)
        {
            return model.FromPort != null
                   && model.FromPort.HasReorderableWires
                   && model.FromPort.GetConnectedWires().Count > 1
                   && (selectionStateComponent.IsSelected(model.FromPort.NodeModel)
                       || model.FromPort.GetConnectedWires().Any(selectionStateComponent.IsSelected));
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (!(m_Model is WireModel wireModel) || !(m_OwnerElement is Wire wire))
                return;

            if (wire.GraphView?.GraphViewModel != null && WireShouldShowLabel_Internal(wireModel, wire.GraphView.GraphViewModel.SelectionState))
            {
                var attachPoint = wire.WireControl as VisualElement ?? wire;
                m_WireBubble.SetAttacherOffset(Vector2.zero);
                m_WireBubble.text = wireModel.WireLabel;
                m_WireBubble.AttachTo(attachPoint, SpriteAlignment.Center);
                m_WireBubble.style.visibility = StyleKeyword.Null;
            }
            else
            {
                m_WireBubble.Detach();
                m_WireBubble.style.visibility = Visibility.Hidden;
            }
        }

    }
}
