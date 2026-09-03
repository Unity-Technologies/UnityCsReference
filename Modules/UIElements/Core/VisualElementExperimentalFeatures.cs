// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Give access to UIElement experimental features.
    /// </summary>
    public interface IExperimentalFeatures
    {
        /// <summary>
        /// Returns the animation experimental interface.
        /// </summary>
        ITransitionAnimations animation {get;}
    }

    public partial class VisualElement : IExperimentalFeatures
    {
        /// <summary>
        /// Returns the UIElements experimental interfaces.
        /// </summary>
        public IExperimentalFeatures experimental { get { return this; } }

        ITransitionAnimations IExperimentalFeatures.animation { get { return this; } }
    }
}
