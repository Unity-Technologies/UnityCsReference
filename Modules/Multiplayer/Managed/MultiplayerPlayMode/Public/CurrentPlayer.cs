// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
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
        internal static Type s_EditorApiType = typeof(CurrentPlayerApi);

        static CurrentPlayerApi s_CurrentPlayerApi;

        private static void EnsureInitialized()
        {
            if (s_CurrentPlayerApi != null)
                return;

            Assert.IsTrue(s_EditorApiType != typeof(CurrentPlayerApi), "CurrentPlayerApi type for editor must be set before use.");
            s_CurrentPlayerApi = (CurrentPlayerApi)Activator.CreateInstance(s_EditorApiType);
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReloadLatestTagsOnEnterPlaymode()
        {
            s_CurrentPlayerApi = null;
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
