// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
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
        public static DataWatchService sharedInstance = new DataWatchService();
        private TimerEventScheduler m_Scheduler = new TimerEventScheduler();


        struct Spy
        {
            public readonly int handleID;
            public readonly Action<Object> onDataChanged;

            public Spy(int handleID, Action<Object> onDataChanged)
            {
                this.handleID = handleID;
                this.onDataChanged = onDataChanged;
            }
        }

        private class Watchers
        {
            public List<Spy> spyList;
            public ChangeTrackerHandle tracker;
            public IScheduledItem scheduledItem;
            public Object watchedObject;
            private DataWatchService service
            {
                get
                {
                    return DataWatchService.sharedInstance;
                }
            }

            public bool isModified { get; set; }

            public Watchers(Object watched)
            {
                spyList = new List<Spy>();
                tracker = ChangeTrackerHandle.AcquireTracker(watched);
                watchedObject = watched;
            }

            public void AddSpy(int handle, Action<Object> onDataChanged)
            {
                spyList.Add(new Spy(handle, onDataChanged));
            }

            public bool IsEmpty()
            {
                return spyList == null;
            }

            public void OnTimerPoolForChanges(TimerState ts)
            {
                if (PollForChanges())
                {
                    isModified = false;
                    service.NotifyDataChanged(this);
                }
            }

            public bool PollForChanges()
            {
                if (watchedObject == null) //object could have been deleted
                {
                    isModified = true;
                }
                else
                {
                    //we need to poll the tracker first if it exists, since it needs to update the object checksum
                    if (tracker.PollForChanges())
                    {
                        isModified = true;
                    }
                }

                return isModified;
            }
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
                Watchers w;
                if (m_Watched.TryGetValue(cv.target, out w))
                {
                    w.isModified = true;
                }
            }
            return modifications;
        }

        public void ForceDirtyNextPoll(Object obj)
        {
            Watchers watch;
            if (m_Watched.TryGetValue(obj, out watch))
            {
                watch.tracker.ForceDirtyNextPoll();
                watch.isModified = true;
            }
        }

        // go through all trackers and poll their native revisions
        public void PollNativeData()
        {
            //using the scheduler allows us to automatically throttle data polling
            m_Scheduler.UpdateScheduledEvents();
        }

        static List<Spy> notificationTmpSpies = new List<Spy>();

        private void NotifyDataChanged(Watchers w)
        {
            notificationTmpSpies.Clear();
            notificationTmpSpies.AddRange(w.spyList);

            foreach (Spy spy in notificationTmpSpies)
            {
                spy.onDataChanged(w.watchedObject);
            }

            if (w.watchedObject == null)
            {
                DoRemoveWatcher(w);
            }
        }

        private void DoRemoveWatcher(Watchers watchers)
        {
            m_Watched.Remove(watchers.watchedObject);
            m_Scheduler.Unschedule(watchers.scheduledItem);
            watchers.tracker.ReleaseTracker();
        }

        static int s_WatchID;

        public IDataWatchHandle AddWatch(Object watched, Action<Object> onDataChanged)
        {
            if (watched == null)
                throw new ArgumentException("Object watched cannot be null");

            DataWatchHandle handle = new DataWatchHandle(++s_WatchID, this, watched);
            m_Handles[handle.id] = handle;

            Watchers watchers;
            if (!m_Watched.TryGetValue(watched, out watchers))
            {
                watchers = new Watchers(watched);
                m_Watched[watched] = watchers;

                watchers.scheduledItem = m_Scheduler.ScheduleUntil(watchers.OnTimerPoolForChanges, 0, 0, null); //we poll as often as possible
            }
            watchers.spyList.Add(new Spy(handle.id, onDataChanged));
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
                            if (watchers.IsEmpty())
                            {
                                DoRemoveWatcher(watchers);
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

