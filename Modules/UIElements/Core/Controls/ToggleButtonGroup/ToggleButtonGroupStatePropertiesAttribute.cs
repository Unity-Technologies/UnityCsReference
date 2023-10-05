// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Defines how a serialized <see cref="ToggleButtonGroupState"/> will be initialized in the inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ToggleButtonGroupStatePropertiesAttribute : PropertyAttribute
    {
        /// <summary>
        /// See <see cref="ToggleButtonGroup.isMultipleSelection"/>.
        /// </summary>
        public bool allowMultipleSelection { get; }

        /// <summary>
        /// See <see cref="ToggleButtonGroup.allowEmptySelection"/>.
        /// </summary>
        public bool allowEmptySelection { get; }

        /// <summary>
        /// The initial length of the <see cref="ToggleButtonGroupState"/>. 
        /// </summary>
        public int length { get; }

        /// <summary>
        /// Defines how the toggle button group will be initialized.
        /// </summary>
        /// <param name="allowMultipleSelection">If multiple buttons can be selected.</param>
        /// <param name="allowEmptySelection">If an empty selection is possible.</param>
        /// <param name="length">The length of the group.</param>
        public ToggleButtonGroupStatePropertiesAttribute(bool allowMultipleSelection = true, bool allowEmptySelection = true, int length = -1)
        {
            this.allowMultipleSelection = allowMultipleSelection;
            this.allowEmptySelection = allowEmptySelection;
            this.length = length;
        }
    }
}
