// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class MixedTypeFieldEditor : BaseModelPropertyField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MixedTypeFieldEditor"/> class.
        /// </summary>
        /// <param name="commandTarget">The view to use to dispatch commands when the field is edited.</param>
        /// <param name="label">The label of the field.</param>
        public MixedTypeFieldEditor(ICommandTarget commandTarget, string label)
            : base(commandTarget)
        {
            this.AddPackageStylesheet("Field.uss");

            var field = new TextField(label) { value = "\u2014", isReadOnly = true };
            Setup(field.labelElement, field, null);

            hierarchy.Add(field);
        }

        /// <inheritdoc />
        public override void UpdateDisplayedValue() { }
    }
}
