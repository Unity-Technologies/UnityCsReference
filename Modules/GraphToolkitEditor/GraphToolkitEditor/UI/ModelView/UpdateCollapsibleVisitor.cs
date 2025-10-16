// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// interface for collapsible container elements.
    /// </summary>
    [UnityRestricted]
    internal interface ICollapsibleContainer
    {
        void UpdateCollapsible(UpdateCollapsibleVisitor visitor);
    }
    /// <summary>
    /// Visitor to update transition collapse state.
    /// </summary>
    [UnityRestricted]
    internal class UpdateCollapsibleVisitor : ViewUpdateVisitor
    {
        /// <inheritdoc cref="ViewUpdateVisitor()"/>
        public UpdateCollapsibleVisitor()
        {
        }

        /// <inheritdoc />
        public override void Update(ChildView view)
        {
            if (view is ICollapsibleContainer collapsibleContainer)
                collapsibleContainer.UpdateCollapsible(this);
        }

        /// <inheritdoc />
        public override void Update(ModelViewPart part)
        {
            // no collapsible part yet
            /*if (part is ICollapsibleContainer collapsibleContainer)
                collapsibleContainer.UpdateCollapsible(this);*/
        }
    }
}
