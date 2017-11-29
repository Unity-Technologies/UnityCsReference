// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Collaboration;

namespace UnityEditor.Collaboration
{
    internal class RevisionsResult
    {
        public List<Revision> Revisions = new List<Revision>();
        public int RevisionsInRepo = -1;

        public int Count {get { return Revisions.Count; }}

        public void Clear()
        {
            Revisions.Clear();
            RevisionsInRepo = -1;
        }
    }

    internal interface IRevisionsService
    {
        RevisionsResult GetRevisions(int offset, int count);
        string tipRevision { get; }
    }

    internal class RevisionsService : IRevisionsService
    {
        protected Collab collab;
        protected RevisionsResult history;
        protected int historyOffset = 0;

        public RevisionsService(Collab collabInstance)
        {
            collab = collabInstance;
            history = new RevisionsResult();
        }

        public RevisionsResult GetRevisions(int offset, int count)
        {
            // For now, clear out the local cache and just load what they ask for
            history.Clear();
            // TODO: Handle exception if call fails
            var data = collab.GetRevisionsData(true, offset, count);
            history.Revisions.AddRange(data.Revisions);
            history.RevisionsInRepo = data.RevisionsInRepo;
            historyOffset = data.RevisionOffset;

            return history;
        }

        public string tipRevision {get { return collab.collabInfo.tip; }}
    }
}
