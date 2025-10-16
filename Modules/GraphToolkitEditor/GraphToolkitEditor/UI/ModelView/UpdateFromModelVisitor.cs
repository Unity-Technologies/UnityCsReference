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
    internal class UpdateFromModelVisitor : ViewUpdateVisitor
    {
        /// <summary>
        /// A static instance to be used when the change hints are not known.
        /// </summary>
        public static readonly UpdateFromModelVisitor genericUpdateFromModelVisitor = new(ChangeHintList.Unspecified);

        /// <summary>
        /// Hints about the changes made to the model.
        /// </summary>
        public ChangeHintList ChangeHints { get; private set; }

        internal void Reset(ChangeHintList hints)
        {
            ChangeHints = hints;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateFromModelVisitor"/> class.
        /// </summary>
        /// <param name="changeHints">Hints about the changes made to the model.</param>
        public UpdateFromModelVisitor(ChangeHintList changeHints)
        {
            ChangeHints = changeHints;
        }

        /// <inheritdoc />
        public override void Update(ChildView view)
        {
            view.UpdateUIFromModel(this);
        }

        /// <inheritdoc />
        public override void Update(ModelViewPart part)
        {
            part.UpdateUIFromModel(this);
        }
    }
}
