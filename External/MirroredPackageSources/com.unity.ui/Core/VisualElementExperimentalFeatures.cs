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

    /// <summary>
    /// Base class for objects that are part of the UIElements visual tree.
    /// </summary>
    /// <remarks>
    /// VisualElement contains several features that are common to all controls in UIElements, such as layout, styling and event handling.
    /// Several other classes derive from it to implement custom rendering and define behaviour for controls.
    /// </remarks>
    public partial class VisualElement : IExperimentalFeatures
    {
        /// <summary>
        /// Returns the UIElements experimental interfaces.
        /// </summary>
        public IExperimentalFeatures experimental { get { return this; } }

        ITransitionAnimations IExperimentalFeatures.animation { get { return this; } }
    }
}
