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
        /// Checks if the progress operation has been cancelled.
        /// </summary>
        /// <value>True if cancelled, otherwise false.</value>
        bool IsCancelled { get; }

        /// <summary>
        /// Cancels the progress object.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Initializes the progress object.
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="total">Number of steps</param>
        AsyncProgressState Start(string title, int total);

        /// <summary>
        /// Advances the progress object by one step.
        /// </summary>
        /// <param name="state">The state data returned by Start</param>
        /// <param name="description">Updated message</param>
        void Advance(AsyncProgressState state, string description = "");

        /// <summary>
        /// Clear and hide the progress object.
        /// </summary>
        /// <param name="state">The state data returned by Start</param>
        void Clear(AsyncProgressState state);

        /// <summary>
        /// Initializes the root progress object.
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="description">Description</param>
        /// <param name="total">Number of steps</param>
        AsyncProgressState StartRoot(string title, string description, int total);

        /// <summary>
        /// Advances the root progress object by one step.
        /// </summary>
        /// <param name="state">The state data returned by StartParent</param>
        void AdvanceRoot(AsyncProgressState state);

        /// <summary>
        /// Clear and hide the root progress object.
        /// </summary>
        /// <param name="state">The state data returned by StartParent</param>
        void ClearRoot(AsyncProgressState state);
    }

    /// <summary>
    /// Stores the state of an individual progress item during analysis.
    /// </summary>
    public class AsyncProgressState
    {
        internal int Id;
        internal int Current;
        internal int Total;
    }
}
