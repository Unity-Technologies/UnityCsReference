// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Provides methods to create and manage an object which can represent progress of the project analysis process.
    /// </summary>
    public interface IProgress
    {
        /// <summary>
        /// Initializes the progress object.
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="description">Description</param>
        /// <param name="total">Number of steps</param>
        void Start(string title, string description, int total);

        /// <summary>
        /// Advances the progress object by one step.
        /// </summary>
        /// <param name="description">Updated message</param>
        void Advance(string description = "");

        /// <summary>
        /// Clear and hide the progress object.
        /// </summary>
        void Clear();

        /// <summary>
        /// Checks if the progress operation has been cancelled.
        /// </summary>
        /// <value>True if cancelled, otherwise false.</value>
        bool IsCancelled { get; }
    }
}
