// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Status to report the result of a binding update.
    /// </summary>
    public enum BindingStatus
    {
        /// <summary>
        /// Indicates that the binding has successfully processed the update.
        /// </summary>
        Success,
        /// <summary>
        /// Indicates that the binding has failed to process the update.
        /// </summary>
        Failure,
        /// <summary>
        /// Indicates that the binding has not yet completed the update.
        /// </summary>
        Pending
    }

    /// <summary>
    /// Provides information about the binding update.
    /// </summary>
    public readonly struct BindingResult
    {
        /// <summary>
        /// The status from the binding update.
        /// </summary>
        public BindingStatus status { get; }

        /// <summary>
        /// Gets the message associated with the binding update.
        /// </summary>
        /// <remarks>
        /// This message is ignored when <see cref="status"/> is set to <see cref="BindingStatus.Success"/>.
        /// </remarks>
        public string message { get; }

        /// <summary>
        /// Constructs a binding result.
        /// </summary>
        /// <remarks>
        /// The message is ignored when <see cref="status"/> is set to <see cref="BindingStatus.Success"/>.
        /// </remarks>
        /// <param name="status">The status of the binding.</param>
        /// <param name="message">The message linked to the status.</param>
        public BindingResult(BindingStatus status, string message = null)
        {
            this.status = status;
            this.message = message;
        }
    }
}
