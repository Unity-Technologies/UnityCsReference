// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Multiplayer.PlayMode
{
    class CurrentPlayerApi
    {
        public virtual bool IsMainEditor => false;
        private List<string> m_Tags = new();

        protected void SetTags(IEnumerable<string> tags)
        {
            m_Tags.Clear();
            if (tags != null)
            {
                m_Tags.AddRange(tags);
            }
        }

        public virtual IReadOnlyList<string> ReadOnlyTags() => m_Tags.AsReadOnly();

        public virtual void ReportResult(bool condition, string message = "", [CallerFilePath] string callingFilePath = "", [CallerLineNumber] int lineNumber = 0) { }
    }

    /// <summary>
    /// Utility class to access information about the multiplayer play mode player while in playmode.
    /// </summary>
    [MovedFrom(true, "Unity.Multiplayer.Playmode", "Unity.Multiplayer.Playmode")]
    public static partial class CurrentPlayer
    {
        static CurrentPlayerApi s_CurrentPlayerApi;

        private static void EnsureInitialized()
        {
            if (s_CurrentPlayerApi != null)
                return;

            // Ideally the CurrentPlayerEditor type should be injected when it's assembly is loaded.
            // However, the only event it currently has access to is InitializeOnLoadMethod, which can lead to issues
            // where user code gets invoked before, and therefore CurrentPlayer is not initialized yet.
            // It can be fixed by when events like OnAssemblyLoaded are available to editor code.
            // For now, we use reflection to make sure editor api is initialized.
            var editorApiType = Type.GetType("Unity.Multiplayer.PlayMode.Editor.CurrentPlayerEditor, UnityEditor.MultiplayerModule");
            Assert.IsNotNull(editorApiType, "CurrentPlayerEditor API type not found");
            s_CurrentPlayerApi = (CurrentPlayerApi)Activator.CreateInstance(editorApiType);
        }

        /// <summary>
        /// Indicates if the currently running player is the main editor.
        /// </summary>
        /// <value>Returns true if the currently running player is the main editor</value>
        public static bool IsMainEditor
        {
            get
            {
                EnsureInitialized();
                return s_CurrentPlayerApi.IsMainEditor;
            }
        }

        /// <summary>
        /// Returns the tag(s) assigned to the currently running player.
        /// This property is read-only and should be used to access the tags without modifying them.
        /// </summary>
        /// <example>
        /// <code>
        /// void Update()
        /// {
        ///     if (CurrentPlayer.Tags.Contains("YellowTeam")) {
        ///         ...
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <returns>Returns the array of assigned tags</returns>
        public static IReadOnlyList<string> Tags
        {
            get
            {
                EnsureInitialized();
                return s_CurrentPlayerApi.ReadOnlyTags();
            }
        }

        /// <summary>
        /// This allows for asserting on the main editor from any Player (Useful for Runtime tests involving MonoBehaviour)
        /// </summary>
        /// <param name="condition"> The Condition</param>
        /// <param name="message"> The Message </param>
        /// <param name="callingFilePath"> The Calling File Path</param>
        /// <param name="lineNumber"> The Line Number</param>
        /// <example>
        /// <code>
        /// void Update()
        /// {
        ///     CurrentPlayer.ReportResult(Condition, "We successfully reported from a test");
        /// }
        /// </code>
        /// </example>
        internal static void ReportResult(
            bool condition,
            string message = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            EnsureInitialized();
            s_CurrentPlayerApi.ReportResult(condition, message, callingFilePath, lineNumber);
        }
    }
}
