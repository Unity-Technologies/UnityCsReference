// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for model views.
    /// </summary>
    abstract class View : VisualElement
    {
        /// <summary>
        /// Instantiates and initializes the VisualElements that makes the UI.
        /// </summary>
        public abstract void BuildUI();

        /// <summary>
        /// Updates the UI using data from the model.
        /// </summary>
        public abstract void UpdateFromModel();
    }
}
