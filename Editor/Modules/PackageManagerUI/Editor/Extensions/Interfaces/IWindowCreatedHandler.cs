// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Arguments for handlers for the WindowCreated event
    /// </summary>
    internal struct WindowCreatedArgs
    {
        /// <summary>
        /// The handle to the Package Manager window
        /// </summary>
        public IWindow window { get; internal set; }
    }

    /// <summary>
    /// Interface for handlers for the WindowCreated event
    /// </summary>
    internal interface IWindowCreatedHandler
    {
        /// <summary>
        /// Called when the Package Manager window is created.
        /// </summary>
        /// <param name="args">The arguments for the window created event.</param>
        void OnWindowCreated(WindowCreatedArgs args);
    }
}
