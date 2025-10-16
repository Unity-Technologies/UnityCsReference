// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    partial class GraphElementModel
    {
        protected List<Capabilities> m_Capabilities = new();

        /// <summary>
        /// The list of capabilities of the element.
        /// </summary>
        public IReadOnlyList<Capabilities> Capabilities => m_Capabilities;

        /// <summary>
        /// Indicates whether this model has a specified capability.
        /// </summary>
        /// <param name="capability">Capability to check for.</param>
        /// <returns><c>true</c> if the model has the capability.</returns>
        /// <remarks>
        /// This method checks whether the model has a specific capability. Examples of capabilities include <see cref="Capabilities.Renamable"/>,
        /// <see cref="Capabilities.Deletable"/>, and <see cref="Capabilities.Movable"/>.
        /// For a full list of available capabilities, see <see cref="Unity.GraphToolkit.Editor.Capabilities"/>.
        /// </remarks>
        public bool HasCapability(Capabilities capability)
        {
            if (Capabilities == null)
                return false;

            for (var i = 0; i < Capabilities.Count; i++)
            {
                if (capability.Id == Capabilities[i].Id)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Sets a capability for a model.
        /// </summary>
        /// <param name="capability">Capability to set</param>
        /// <param name="active">Whether the capability is enabled or disabled.</param>
        /// <remarks>
        /// This method allows you to set or remove capabilities for a model. Examples of capabilities include <see cref="Capabilities.Renamable"/>,
        /// <see cref="Capabilities.Deletable"/>, and <see cref="Capabilities.Movable"/>.
        /// For a full list of available capabilities, see <see cref="Unity.GraphToolkit.Editor.Capabilities"/>.
        /// </remarks>
        public void SetCapability(Capabilities capability, bool active)
        {
            if (Capabilities is not IList<Capabilities> capabilities)
                return;

            if (active)
            {
                if (!HasCapability(capability))
                    capabilities.Add(capability);
            }
            else
            {
                capabilities.Remove(capability);
            }
        }

        /// <summary>
        /// Removes all capabilities from a model.
        /// </summary>
        /// <remarks>
        /// This method clears all capabilities previously set on the model, which resets it to a state where no capabilities are active.
        /// </remarks>
        public void ClearCapabilities()
        {
            if (Capabilities is List<Capabilities> capabilities)
            {
                capabilities.Clear();
            }
        }

        /// <summary>
        /// Indicates whether a model has the capability to be selected.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.Selectable"/> capability, which determines whether it can be selected within the graph.
        /// </remarks>
        public bool IsSelectable()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.Selectable);
        }

        /// <summary>
        /// Indicates whether a model has the capability to be collapsed.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.Collapsible"/> capability, which allows it to be collapsed or expanded.
        /// </remarks>
        public bool IsCollapsible()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.Collapsible);
        }

        /// <summary>
        /// Indicates whether a model has the capability to be resized.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.Resizable"/> capability, which enables resizing.
        /// </remarks>
        public bool IsResizable()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.Resizable);
        }

        /// <summary>
        /// Indicates whether a model has the capability to be moved.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.Movable"/> capability, so it can be repositioned in the graph.
        /// </remarks>
        public bool IsMovable()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.Movable);
        }

        /// <summary>
        /// Indicates whether a model has the capability to be deleted.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.Deletable"/> capability, so it can be removed from the graph.
        /// </remarks>
        public bool IsDeletable()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.Deletable);
        }

        /// <summary>
        /// Indicates whether a model has the capability to be dropped.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.Droppable"/> capability, so it can be dragged and dropped.
        /// </remarks>
        public bool IsDroppable()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.Droppable);
        }

        /// <summary>
        /// Indicates whether a model has the capability to be renamed.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.Renamable"/> capability, so it can be renamed.
        /// </remarks>
        public bool IsRenamable()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.Renamable);
        }

        /// <summary>
        /// Indicates whether a model has the capability to be copied.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.Copiable"/> capability, so it can be duplicated in the graph.
        /// </remarks>
        public bool IsCopiable()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.Copiable);
        }

        /// <summary>
        /// Indicates whether a model has the capability to change color.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.Colorable"/> capability, which allows it to modify its color.
        /// </remarks>
        public bool IsColorable()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.Colorable);
        }

        /// <summary>
        /// Indicates whether a model has the capability to be ascended.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.Ascendable"/> capability, so it can be moved to front when selected.
        /// </remarks>
        public bool IsAscendable()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.Ascendable);
        }

        /// <summary>
        /// Indicates whether a model needs a container to be used.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.NeedsContainer"/> capability, which indicates that it requires a container to function properly.
        /// </remarks>
        public bool NeedsContainer()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.NeedsContainer);
        }

        /// <summary>
        /// Indicates whether a model can be disabled.
        /// </summary>
        /// <returns>True if it has the capability.</returns>
        /// <remarks>
        /// This method checks if the model has the <see cref="Capabilities.Disableable"/> capability and can be disabled in the graph.
        /// </remarks>
        public bool IsDisableable()
        {
            return HasCapability(Unity.GraphToolkit.Editor.Capabilities.Disableable);
        }
    }
}
