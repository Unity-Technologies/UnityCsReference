// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Collaboration;
using UnityEditor.Connect;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Collaboration
{
    delegate void RevisionsDelegate(RevisionsResult revisionsResult);
    delegate void SingleRevisionDelegate(Revision? revision);
    internal class RevisionsResult
    {
        public List<Revision> Revisions = new List<Revision>();
        public int RevisionsInRepo = -1;

        public int Count { get { return Revisions.Count; } }

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
        public event SingleRevisionDelegate FetchSingleRevisionCallback;

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

        public void GetRevision(string revId)
        {
            Collab.GetSingleRevisionData(true, revId);
        }

        [RequiredByNativeCode]
        private static void onFetchSingleRevision(IntPtr ptr)
        {
            Revision? ret = null;
            if (instance.FetchSingleRevisionCallback != null && ptr != IntPtr.Zero)
            {
                Revision nativeStruct = Collab.PopulateSingleRevisionData(ptr);
                // this copies the content as it's a struct not a class.
                ret = nativeStruct;
            }

            instance.FetchSingleRevisionCallback(ret);
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
