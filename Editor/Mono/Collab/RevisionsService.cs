// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Collaboration;
using UnityEditor.Connect;
using UnityEngine.Scripting;

namespace UnityEditor.Collaboration
{
    delegate void RevisionsDelegate(RevisionsResult revisionsResult);

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
        event RevisionsDelegate FetchRevisionsCallback;
        void GetRevisions(int offset, int count);
        string tipRevision { get; }
        string currentUser { get; }
    }

    internal class RevisionsService : IRevisionsService
    {
        public event RevisionsDelegate FetchRevisionsCallback;
        protected Collab collab;
        protected UnityConnect connect;
        private static RevisionsService instance;

        public string tipRevision { get { return collab.collabInfo.tip; } }
        public string currentUser { get { return connect.GetUserInfo().userName; } }

        public RevisionsService(Collab collabInstance, UnityConnect connectInstance)
        {
            collab = collabInstance;
            connect = connectInstance;
            instance = this;
        }

        public void GetRevisions(int offset, int count)
        {
            // Only send down request for the desired data.
            Collab.GetRevisionsData(true, offset, count);
        }

        [RequiredByNativeCode]
        private static void OnFetchRevisions(IntPtr nativeData)
        {
            RevisionsService service = instance;
            if (service == null || service.FetchRevisionsCallback == null)
                return;

            RevisionsResult history = null;
            if (nativeData != IntPtr.Zero)
            {
                RevisionsData data = Collab.PopulateRevisionsData(nativeData);
                history = new RevisionsResult();
                history.Revisions.AddRange(data.Revisions);
                history.RevisionsInRepo = data.RevisionsInRepo;
            }

            service.FetchRevisionsCallback(history);
        }
    }
}
