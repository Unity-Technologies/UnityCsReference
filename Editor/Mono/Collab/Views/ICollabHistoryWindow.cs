// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Collaboration;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Collaboration
{
    internal delegate void PageChangeAction(int page);
    internal delegate void RevisionAction(string revisionId, bool updateToRevision);
    internal delegate void ShowBuildAction(string revisionId);

    internal enum HistoryState
    {
        Error,
        Offline,
        Maintenance,
        LoggedOut,
        NoSeat,
        Disabled,
        Waiting,
        Ready,
    }

    internal enum BuildState
    {
        None,
        Configure,
        Success,
        Failed,
        InProgress,
    }

    internal struct RevisionData
    {
        public string id;
        public int index;
        public DateTime timeStamp;
        public string authorName;
        public string comment;

        // Whether this revision is on the client
        public bool obtained;
        public bool current;
        public bool inProgress;
        public bool enabled;

        public BuildState buildState;
        public int buildFailures;

        public ICollection<ChangeData> changes;
        public int changesTotal;
        public bool changesTruncated;
    }

    internal struct ChangeData
    {
        public string path;
        public string action;
    }

    internal interface ICollabHistoryWindow
    {
        void UpdateState(HistoryState state, bool force);
        void UpdateRevisions(IEnumerable<RevisionData> items, string tip, int totalRevisions, int currentPage);

        bool revisionActionsEnabled { get; set; }
        int itemsPerPage { set; }
        string inProgressRevision { get; set; }
        PageChangeAction OnPageChangeAction { set; }
        RevisionAction OnGoBackAction { set; }
        RevisionAction OnUpdateAction { set; }
        RevisionAction OnRestoreAction { set; }
        ShowBuildAction OnShowBuildAction { set; }
        Action OnShowServicesAction { set; }
    }
}
