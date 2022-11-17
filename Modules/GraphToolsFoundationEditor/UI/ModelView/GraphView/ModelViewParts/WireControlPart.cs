// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for an <see cref="WireModel"/>, with user editable control points.
    /// </summary>
    class WireControlPart : BaseModelViewPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="WireControlPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="WireControlPart"/>.</returns>
        public static WireControlPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is WireModel)
            {
                return new WireControlPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected WireControl m_WireControl;

        /// <inheritdoc />
        public override VisualElement Root => m_WireControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="WireControlPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected WireControlPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            m_WireControl = new WireControl(m_OwnerElement as Wire) { name = PartName };
            m_WireControl.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_WireControl.RegisterCallback<MouseEnterEvent>(OnMouseEnterWire);
            m_WireControl.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveWire);
            m_WireControl.RegisterCallback<MouseDownEvent>(OnMouseDownWire);

            container.Add(m_WireControl);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_Model is WireModel wireModel)
            {
                m_WireControl.OutputOrientation_Internal = wireModel.FromPort?.Orientation ?? (wireModel.ToPort?.Orientation ?? PortOrientation.Horizontal);
                m_WireControl.InputOrientation_Internal = wireModel.ToPort?.Orientation ?? (wireModel.FromPort?.Orientation ?? PortOrientation.Horizontal);
            }

            m_WireControl.UpdateLayout();
            UpdateWireControlColors();
            m_WireControl.MarkDirtyRepaint();
        }

        protected void UpdateWireControlColors()
        {
            var parent = m_OwnerElement as Wire;

            if (parent?.IsSelected() ?? false)
            {
                m_WireControl.ResetColor();
            }
            else if (parent?.WireModel is IPlaceholder)
            {
                m_WireControl.SetColor(Color.red, Color.red);
            }
            else
            {
                var wireModel = m_Model as WireModel;
                var inputColor = Color.white;
                var outputColor = Color.white;

                if (wireModel?.ToPort != null)
                    inputColor = wireModel.ToPort.GetView<Port>(m_OwnerElement.RootView)?.PortColor ?? Color.white;
                else if (wireModel?.FromPort != null)
                    inputColor = wireModel.FromPort.GetView<Port>(m_OwnerElement.RootView)?.PortColor ?? Color.white;

                if (wireModel?.FromPort != null)
                    outputColor = wireModel.FromPort.GetView<Port>(m_OwnerElement.RootView)?.PortColor ?? Color.white;
                else if (wireModel?.ToPort != null)
                    outputColor = wireModel.ToPort.GetView<Port>(m_OwnerElement.RootView)?.PortColor ?? Color.white;

                if (parent?.IsGhostWire ?? false)
                {
                    inputColor = new Color(inputColor.r, inputColor.g, inputColor.b, 0.5f);
                    outputColor = new Color(outputColor.r, outputColor.g, outputColor.b, 0.5f);
                }

                m_WireControl.SetColor(inputColor, outputColor);
            }
        }

        protected void OnMouseEnterWire(MouseEnterEvent e)
        {
        }

        protected void OnMouseDownWire(MouseDownEvent e)
        {
            if (e.target == m_WireControl)
            {
                m_WireControl.ResetColor();
            }
        }

        protected void OnMouseLeaveWire(MouseLeaveEvent e)
        {
            if (e.target == m_WireControl)
            {
                UpdateWireControlColors();
            }
        }
    }
}
