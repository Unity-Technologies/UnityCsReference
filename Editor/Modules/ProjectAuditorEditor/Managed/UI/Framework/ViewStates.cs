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
        // foldout preferences
        public bool filters = true;
        public bool dependencies = true;

        // diagnostic preferences
        // TODO: we used to have mutedIssues as a global-state but now it's view-specific
        // does it make sense to go back to use this?
        //public bool mutedIssues;

        public string upgradeTargetVersion;
    }
}
