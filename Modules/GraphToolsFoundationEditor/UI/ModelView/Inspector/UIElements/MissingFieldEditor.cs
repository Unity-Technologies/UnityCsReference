// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Placeholder displayed when an editor cannot be created for a field.
    /// </summary>
    class MissingFieldEditor : BaseModelPropertyField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingFieldEditor"/> class.
        /// </summary>
        /// <param name="commandTarget">The view to use to dispatch commands when the field is edited.</param>
        /// <param name="fieldLabel">The field label.</param>
        public MissingFieldEditor(ICommandTarget commandTarget, string fieldLabel)
            : base(commandTarget)
        {
            LabelElement = new Label($"Missing editor for: {fieldLabel}.");
            Add(LabelElement);
        }

        /// <inheritdoc />
        public override void UpdateDisplayedValue()
        {
        }
    }
}
