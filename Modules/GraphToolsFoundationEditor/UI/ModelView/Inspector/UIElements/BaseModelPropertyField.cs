// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    abstract class BaseModelPropertyField : VisualElement
    {
        /// <summary>
        ///  The command dispatcher.
        /// </summary>
        public ICommandTarget CommandTarget { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomizableModelPropertyField"/> class.
        /// </summary>
        /// <param name="commandTarget">The view to use to dispatch commands when the field is edited.</param>
        protected BaseModelPropertyField(ICommandTarget commandTarget)
        {
            CommandTarget = commandTarget;
        }

        /// <summary>
        /// Updates the value displayed by the custom UI.
        /// </summary>
        /// <returns>True if the value was updated.</returns>
        public abstract bool UpdateDisplayedValue();
    }
}
