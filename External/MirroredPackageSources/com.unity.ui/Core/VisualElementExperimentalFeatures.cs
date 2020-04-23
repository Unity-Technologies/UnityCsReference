using System;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    public interface IExperimentalFeatures
    {
        ITransitionAnimations animation {get;}
    }

    public partial class VisualElement : IExperimentalFeatures
    {
        public IExperimentalFeatures experimental { get { return this; } }

        ITransitionAnimations IExperimentalFeatures.animation { get { return this; } }
    }
}
