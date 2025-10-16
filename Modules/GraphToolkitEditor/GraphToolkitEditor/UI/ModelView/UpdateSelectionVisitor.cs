// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Visitor class used to recursively update a <see cref="ChildView"/> and its <see cref="ModelViewPart"/>s using data from the view's model.
    /// </summary>
    [UnityRestricted]
    internal class UpdateSelectionVisitor : ViewUpdateVisitor
    {
        /// <summary>
        /// A static instance to be used when the change hints are not known.
        /// </summary>
        public static readonly UpdateSelectionVisitor Visitor = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateFromModelVisitor"/> class.
        /// </summary>
        public UpdateSelectionVisitor()
        {
        }

        /// <inheritdoc />
        public override void Update(ChildView view)
        {
            view.UpdateUISelection(this);
        }

        /// <inheritdoc />
        public override void Update(ModelViewPart part)
        {
            //Could no find any part the needed to update itself based on selection.
        }
    }
}
