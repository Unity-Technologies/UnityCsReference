// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A <see cref="CollapseButtonPart"/> that disables itself if the node cannot be collapsed.
    /// </summary>
    class NodeCollapseButtonPart : CollapseButtonPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NodeCollapseButtonPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="NodeCollapseButtonPart"/>.</returns>
        public new static NodeCollapseButtonPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is ICollapsible)
            {
                return new NodeCollapseButtonPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeCollapseButtonPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected NodeCollapseButtonPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();

            if (CollapseButton != null)
            {
                if (m_Model is PortNodeModel portHolder && portHolder.Ports != null)
                {
                    var allPortConnected = portHolder.Ports.All(port => port.IsConnected());
                    if (allPortConnected)
                    {
                        CollapseButton.pseudoStates |= PseudoStates.Disabled;
                    }
                    else
                    {
                        CollapseButton.pseudoStates &= ~PseudoStates.Disabled;
                    }
                }
            }
        }
    }
}
