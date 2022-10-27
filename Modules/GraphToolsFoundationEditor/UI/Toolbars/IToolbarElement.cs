// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface for toolbar element that can be updated by an observer.
    /// </summary>
    interface IToolbarElement
    {
        /// <summary>
        /// Update the element to reflect changes made to the model.
        /// </summary>
        /// <remarks>For example, implementation can disable the toolbar element if there is no opened graph.</remarks>
        void Update();
    }
}
