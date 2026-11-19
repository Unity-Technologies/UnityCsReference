// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for model views.
    /// </summary>
    [UnityRestricted]
    internal abstract class View : VisualElement
    {
        /// <summary>
        /// Instantiates the VisualElements that makes the UI.
        /// </summary>
        public abstract void BuildUITree();
    }
}
