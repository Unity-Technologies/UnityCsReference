// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
