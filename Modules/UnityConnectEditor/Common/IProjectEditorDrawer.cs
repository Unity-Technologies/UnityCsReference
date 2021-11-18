// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Connect
{
    /// <summary>
    /// The common interface for any UI containers that change the project's state regarding services.
    /// </summary>
    public interface IProjectEditorDrawer
    {
        /// <summary>
        /// An event that fires whenever the UI changes the project's state regarding services.
        /// </summary>
        event Action stateChangeButtonFired;

        /// <summary>
        /// Retrieves the <see cref="VisualElement"/> that draws the project state-changing UI.
        /// </summary>
        /// <returns>Returns a <see cref="VisualElement"/></returns>
        VisualElement GetVisualElement();
    }
}
