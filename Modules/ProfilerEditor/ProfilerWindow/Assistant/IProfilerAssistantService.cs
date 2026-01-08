// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Profiling.Editor
{
    /// <summary>
    /// Defines the role of an Ask Assistant service.
    /// </summary>
    /// <remarks>
    /// This attribute is used to bind classes that provide Ask Assistant services to the specific role -
    /// e.g. Profiler Window assistant or Project Auditor assistant.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal class AskAssistantServiceRoleAttribute : Attribute
    {
        public AskAssistantServiceRoleAttribute(string role)
        {
            Role = role;
        }
        public string Role { get; }
    }

    /// <summary>
    /// Allows packages to implement Ask Assistant services for different contexts.
    /// </summary>
    internal interface IAskAssistantService : IDisposable
    {
        /// <summary>
        /// Initializes the Ask Assistant service.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        bool Initialize();

        /// <summary>
        /// Specifies assistant context information. Contains information equivalent to an attachment.
        /// </summary>
        public readonly struct Context
        {
            public Context(string payload, string type, string displayName, object metadata)
            {
                this.payload = payload;
                this.type = type;
                this.displayName = displayName;
                this.metadata = metadata;
            }

            private readonly string payload;
            public readonly string type;
            private readonly string displayName;
            private readonly object metadata;

            public string Payload => payload;

            public string Type => type;

            public string DisplayName => displayName;

            public object Metadata => metadata;
        }



        /// <summary>
        /// Displays a popup window that allows the user to interact with an assistant.
        /// </summary>
        /// <param name="parentRect">The rectangular area on the screen used to position the popup.</param>
        /// <param name="attachment">The context or attachment associated with the assistant interaction.</param>
        /// <param name="prompt">The initial prompt or message to display in the popup.</param>
        /// <param name="systemPrompt">An optional system-level prompt to provide additional context for the assistant. Can be <see
        /// langword="null"/>.</param>
        void ShowAskAssistantPopup(Rect parentRect, Context attachment, string prompt);
    }
}
