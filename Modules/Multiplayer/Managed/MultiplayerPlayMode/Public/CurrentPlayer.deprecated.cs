// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;

namespace Unity.Multiplayer.PlayMode
{
    public static partial class CurrentPlayer
    {
        /// <summary>
        /// Returns the tag(s) assigned to the currently running player.
        /// </summary>
        /// <example>
        /// <code>
        /// void Update()
        /// {
        ///     if (CurrentPlayer.ReadOnlyTags().Contains("YellowTeam")) {
        ///         ...
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <returns>Returns the array of assigned tags</returns>
        [Obsolete("ReadOnlyTags has been deprecated. Use CurrentPlayer.Tags which has better performance properties.", false)]
        public static string[] ReadOnlyTags()
        {
            var tagsList = Tags;
            var result = new string[tagsList.Count];
            for (int i = 0; i < tagsList.Count; i++)
            {
                result[i] = tagsList[i];
            }
            return result;
        }
    }
}
