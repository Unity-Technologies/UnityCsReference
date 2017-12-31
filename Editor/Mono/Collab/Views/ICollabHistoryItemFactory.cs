// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Collaboration;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Collaboration
{
    internal interface ICollabHistoryItemFactory
    {
        IEnumerable<RevisionData> GenerateElements(IEnumerable<Revision> revsRevisions, int mTotalRevisions, int startIndex, string tipRev, string inProgressRevision, bool revisionActionsEnabled, bool buildServiceEnabled, string currentUser);
    }
}
