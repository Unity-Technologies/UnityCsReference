// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    [Obsolete("Use HorizontalPortContainerPart instead.")]
    class PortContainerPart : HorizontalPortContainerPart
    {
        [Obsolete("Use HorizontalPortContainerPart.Create instead.")]
        public static PortContainerPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            return model is PortNodeModel
                ? new PortContainerPart(name, model, ownerElement, parentClassName)
                : null;
        }

        /// <inheritdoc />
        protected PortContainerPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName, horizontalPortFilter) { }
    }

    /// <summary>
    /// The part to build the UI for horizontal port containers.
    /// </summary>
    class HorizontalPortContainerPart : BasePortContainerPart
    {
        public static readonly string ussClassName = "ge-horizontal-port-container-part";
        public static readonly string portsUssName = "horizontal-port-container";

        /// <summary>
        /// Creates a new instance of the <see cref="HorizontalPortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="portFilter">A filter used to select the ports to display in the container.</param>
        /// <returns>A new instance of <see cref="PortContainerPart"/>.</returns>
        public static HorizontalPortContainerPart Create(string name, Model model, ModelView ownerElement,
            string parentClassName, Func<PortModel, bool> portFilter = null)
        {
            return model is PortNodeModel
                ? new HorizontalPortContainerPart(name, model, ownerElement, parentClassName, portFilter)
                : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HorizontalPortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="portFilter">A filter used to select the ports to display in the container.</param>
        protected HorizontalPortContainerPart(string name, Model model, ModelView ownerElement, string parentClassName, Func<PortModel, bool> portFilter)
            : base(name, model, ownerElement, parentClassName, portsUssName, ussClassName,
                portFilter == null ? horizontalPortFilter : p => horizontalPortFilter(p) && portFilter(p)) { }
    }
}
