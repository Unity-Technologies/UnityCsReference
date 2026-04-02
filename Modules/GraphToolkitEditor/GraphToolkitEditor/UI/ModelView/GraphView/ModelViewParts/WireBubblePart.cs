// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the UI for a text bubble on a wire.
    /// </summary>
    [UnityRestricted]
    internal class WireBubblePart : BaseModelViewPart
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
        public static WireBubblePart Create(string name, Model model, ChildView ownerElement, string parentClassName)
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
        protected WireBubblePart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            m_WireBubble = new WireBubble { name = PartName };
            m_WireBubble.AddToClassList(ussClassName);
            m_WireBubble.AddToClassList(m_ParentClassName.WithUssElement(PartName));
            container.Add(m_WireBubble);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            m_WireBubble.AddPackageStylesheet("WireBubblePart.uss");
        }

        internal static bool WireShouldShowLabel(WireModel model, SelectionStateComponent selectionStateComponent)
        {
            if (model.HasCustomWireBubbleText)
                return true;
            return model.FromPort != null
                && model.FromPort.HasReorderableWires
                && model.FromPort.GetConnectedWires().Count > 1
                && (selectionStateComponent.IsSelected(model.FromPort.NodeModel)
                    || model.FromPort.GetConnectedWires().Exists(selectionStateComponent.IsSelected));
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (!(m_Model is WireModel wireModel) || !(m_OwnerElement is Wire wire))
                return;

            if (wire.GraphView?.GraphViewModel != null && WireShouldShowLabel(wireModel, wire.GraphView.GraphViewModel.SelectionState))
            {
                var attachPoint = wire.WireControl as VisualElement ?? wire;
                m_WireBubble.SetAttacherOffset(Vector2.zero);
                m_WireBubble.text = wireModel.WireBubbleText;
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
