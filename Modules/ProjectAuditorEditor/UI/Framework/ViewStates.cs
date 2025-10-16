// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    /// <summary>
    /// Global UI states. Note that these preferences will not persist between sessions.
    /// </summary>
    [Serializable]
    internal class ViewStates
    {
        public const int DefaultMinFontSize = 12;
        public const int DefaultMaxFontSize = 22;

        // foldout preferences
        public bool info = true;
        public bool info2 = true;
        public bool filters = true;
        public bool dependencies = true;

        // diagnostic preferences
        public bool onlyCriticalIssues;
        // TODO: we used to have mutedIssues as a global-state but now it's view-specific
        // does it make sense to go back to use this?
        //public bool mutedIssues;

        public int fontSize = DefaultMinFontSize;
    }
}
