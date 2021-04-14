// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Event sent after the custom style properties of a <see cref="VisualElement"/> have been resolved.
    /// </summary>
    public class CustomStyleResolvedEvent : EventBase<CustomStyleResolvedEvent>
    {
        /// <summary>
        /// Returns the custom style properties accessor for the targeted <see cref="VisualElement"/>.
        /// </summary>
        public ICustomStyle customStyle
        {
            get { return (target as VisualElement)?.customStyle; }
        }
    }
}
