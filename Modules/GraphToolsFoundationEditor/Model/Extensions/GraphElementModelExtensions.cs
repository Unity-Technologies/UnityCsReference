// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Extension methods for <see cref="GraphElementModel"/>.
    /// </summary>
    static class GraphElementModelExtensions
    {
        /// <summary>
        /// Test if this model has a capability.
        /// </summary>
        /// <param name="self">Element model to test</param>
        /// <param name="capability">Capability to check for</param>
        /// <returns>true if the model has the capability, false otherwise</returns>
        public static bool HasCapability(this GraphElementModel self, Capabilities capability)
        {
            for (var i = 0; i < self.Capabilities.Count; i++)
            {
                if (capability.Id == self.Capabilities[i].Id)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Set a capability for a model.
        /// </summary>
        /// <param name="self">Element model to affect</param>
        /// <param name="capability">Capability to set</param>
        /// <param name="active">true to set the capability, false to remove it</param>
        public static void SetCapability(this GraphElementModel self, Capabilities capability, bool active)
        {
            if (!(self.Capabilities is IList<Capabilities> capabilities))
                return;

            if (active)
            {
                if (!self.HasCapability(capability))
                    capabilities.Add(capability);
            }
            else
            {
                capabilities.Remove(capability);
            }
        }

        /// <summary>
        /// Remove all capabilities from a model.
        /// </summary>
        /// <param name="self">The model to remove capabilites from</param>
        public static void ClearCapabilities(this GraphElementModel self)
        {
            if (self.Capabilities is List<Capabilities> capabilities)
            {
                capabilities.Clear();
            }
        }

        /// <summary>
        /// Test if a model has the capability to be selected.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsSelectable(this GraphElementModel self)
        {
            return self.HasCapability(Capabilities.Selectable);
        }

        /// <summary>
        /// Test if a model has the capability to be collapsed.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsCollapsible(this GraphElementModel self)
        {
            return self.HasCapability(Capabilities.Collapsible);
        }

        /// <summary>
        /// Test if a model has the capability to be resized.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsResizable(this GraphElementModel self)
        {
            return self.HasCapability(Capabilities.Resizable);
        }

        /// <summary>
        /// Tests if a model has the capability to be moved.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsMovable(this GraphElementModel self)
        {
            return self.HasCapability(Capabilities.Movable);
        }

        /// <summary>
        /// Tests if a model has the capability to be deleted.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsDeletable(this GraphElementModel self)
        {
            return self.HasCapability(Capabilities.Deletable);
        }

        /// <summary>
        /// Tests if a model has the capability to be dropped.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsDroppable(this GraphElementModel self)
        {
            return self.HasCapability(Capabilities.Droppable);
        }

        /// <summary>
        /// Tests if a model has the capability to be renamed.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsRenamable(this GraphElementModel self)
        {
            return self.HasCapability(Capabilities.Renamable);
        }

        /// <summary>
        /// Tests if a model has the capability to be copied.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsCopiable(this GraphElementModel self)
        {
            return self.HasCapability(Capabilities.Copiable);
        }

        /// <summary>
        /// Tests if a model has the capability to change color.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsColorable(this GraphElementModel self)
        {
            return self.HasCapability(Capabilities.Colorable);
        }

        /// <summary>
        /// Tests if a model has the capability to be ascended.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsAscendable(this GraphElementModel self)
        {
            return self.HasCapability(Capabilities.Ascendable);
        }

        /// <summary>
        /// Test if a model needs a container to be used.
        /// </summary>
        /// <param name="self">Model to test.</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool NeedsContainer(this GraphElementModel self)
        {
            return self.HasCapability(Capabilities.NeedsContainer);
        }
    }
}
