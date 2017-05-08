// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    class DataWatchHandle : IDataWatchHandle
    {
        public readonly int id;
        public WeakReference service;

        public Object watched { get; private set; }

        public DataWatchHandle(int id, DataWatchService service, Object watched)
        {
            this.id = id;
            this.service = new WeakReference(service);
            this.watched = watched;
        }

        public bool disposed
        {
            get
            {
                return ReferenceEquals(watched, null);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                throw new InvalidOperationException("DataWatchHandle was already disposed of");
            }
            if (service != null && service.IsAlive)
            {
                (service.Target as DataWatchService).RemoveWatch(this);
            }
            service = null;
            watched = null;
        }
    }

    internal class DataWatchService : IDataWatchService
    {
        private HashSet<Object> m_DirtySet = new HashSet<Object>();

        struct Spy
        {
            public readonly int handleID;
            public readonly VisualElement watcher;
            public readonly Action onDataChanged;

            public Spy(int handleID, VisualElement watcher, Action onDataChanged)
            {
                this.handleID = handleID;
                this.watcher = watcher;
                this.onDataChanged = onDataChanged;
            }
        }

        struct Watchers
        {
            public List<Spy> spyList;
            public ChangeTrackerHandle tracker;
        }

        private Dictionary<int, DataWatchHandle> m_Handles = new Dictionary<int, DataWatchHandle>();
        private Dictionary<Object, Watchers> m_Watched = new Dictionary<Object, Watchers>();

        public DataWatchService()
        {
            // TODO probably just do this when panel becomes active and stop when being inactive
            Undo.postprocessModifications += PostProcessUndo;
        }

        ~DataWatchService()
        {
            Undo.postprocessModifications -= PostProcessUndo;
        }

        // callback to watch undo stack for changes
        // TODO: add the hook before animation strips some changes in record mode
        public UndoPropertyModification[] PostProcessUndo(UndoPropertyModification[] modifications)
        {
            foreach (var m in modifications)
            {
                var cv = m.currentValue;
                if (cv == null || cv.target == null)
                    continue;
                if (m_Watched.ContainsKey(cv.target))
                {
                    m_DirtySet.Add(cv.target);
                }
            }
            return modifications;
        }

        // go through all trackers and poll their native revisions
        public void PollNativeData()
        {
            foreach (var w in m_Watched)
            {
                // Unity Object can be destroyed without us knowing
                // They will compare to null but the C# object is still valid
                // Other wise we check the object for changes
                if (w.Key == null || w.Value.tracker.PollForChanges())
                {
                    m_DirtySet.Add(w.Key);
                }
            }
        }

        public void ProcessNotificationQueue()
        {
            PollNativeData();

            // copy because dirty call could come in while we roll along
            var tmpDirty = m_DirtySet;
            m_DirtySet = new HashSet<Object>();

            // since callbacks when being invoked we make copies for safe iteration
            var tmpSpys = new List<Spy>();

            // get the set of Dirties from C++ and clear it.
            foreach (var o in tmpDirty)
            {
                Watchers watchers;
                if (m_Watched.TryGetValue(o, out watchers))
                {
                    tmpSpys.Clear();
                    tmpSpys.AddRange(watchers.spyList);

                    foreach (Spy spy in tmpSpys)
                    {
                        if (spy.watcher.panel != null)
                        {
                            // for any watches trigger callbacks
                            spy.onDataChanged();
                        }
                        else
                        {
                            Debug.Log("Leaking Data Spies from element: " + spy.watcher);
                        }
                    }
                }
            }

            tmpDirty.Clear();
        }

        static int s_WatchID;

        public IDataWatchHandle AddWatch(VisualElement watcher, Object watched, Action onDataChanged)
        {
            if (watched == null)
                throw new ArgumentException("Object watched cannot be null");

            DataWatchHandle handle = new DataWatchHandle(++s_WatchID, this, watched);
            m_Handles[handle.id] = handle;

            Watchers watchers;
            if (!m_Watched.TryGetValue(watched, out watchers))
            {
                watchers = new Watchers
                {
                    spyList = new List<Spy>(),
                    tracker = ChangeTrackerHandle.AcquireTracker(watched),
                };
                m_Watched[watched] = watchers;
            }
            watchers.spyList.Add(new Spy(handle.id, watcher, onDataChanged));
            return handle;
        }

        public void RemoveWatch(IDataWatchHandle handle)
        {
            DataWatchHandle handleImpl = (DataWatchHandle)handle;

            if (m_Handles.Remove(handleImpl.id))
            {
                Watchers watchers;
                if (m_Watched.TryGetValue(handleImpl.watched, out watchers))
                {
                    List<Spy> spyList = watchers.spyList;
                    for (int i = 0; i < spyList.Count; i++)
                    {
                        Spy spy = spyList[i];
                        if (spy.handleID == handleImpl.id)
                        {
                            spyList.RemoveAt(i);
                            if (watchers.spyList.Count == 0)
                            {
                                watchers.tracker.ReleaseTracker();
                                m_Watched.Remove(handleImpl.watched);
                            }
                            return;
                        }
                    }
                }
            }
            throw new ArgumentException("Data watch was not registered");
        }
    }
}

