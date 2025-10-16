// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Placeholder displayed when an editor cannot be created for a field.
    /// </summary>
    [UnityRestricted]
    internal class MissingFieldEditor : BaseModelPropertyField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingFieldEditor"/> class.
        /// </summary>
        /// <param name="commandTarget">The view to use to dispatch commands when the field is edited.</param>
        /// <param name="fieldLabel">The field label.</param>
        public MissingFieldEditor(ICommandTarget commandTarget, string fieldLabel)
            : base(commandTarget)
        {
            var label = new Label(fieldLabel);
            var text = new Label("Missing editor");

            Setup(label, text, null);

            hierarchy.Add(label);
            hierarchy.Add(text);
        }

        /// <inheritdoc />
        public override void UpdateDisplayedValue()
        {
        }
    }
}
