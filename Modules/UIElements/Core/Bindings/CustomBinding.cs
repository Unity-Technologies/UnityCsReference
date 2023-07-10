// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base class for general purpose binding extensibility.
    /// </summary>
    /// <example>
    /// The following example creates a custom binding that can be used to display the current time.
    /// This could then be bound to the text field of a Label to create a clock.
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/CustomBinding_CurrentTime.cs"/>
    /// </example>
    [UxmlObject]
    public abstract partial class CustomBinding : Binding
    {
        /// <summary>
        /// Initializes and returns an instance of <see cref="CustomBinding"/>.
        /// </summary>
        protected CustomBinding()
        {
            updateTrigger = BindingUpdateTrigger.EveryUpdate;
        }

        /// <summary>
        /// Called when the binding system updates the binding.
        /// </summary>
        /// <param name="context">Context object containing the necessary information to resolve a binding.</param>
        /// <returns>A <see cref="BindingResult"/> indicating if the binding update succeeded or not.</returns>
        protected internal virtual BindingResult Update(in BindingContext context)
        {
            return new BindingResult(BindingStatus.Success);
        }
    }
}
