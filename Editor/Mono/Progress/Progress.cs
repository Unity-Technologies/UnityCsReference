// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEditor
{
    public static partial class Progress
    {
        [DebuggerDisplay("name={name}")]
        public class Item
        {
            struct CachedValue<T>
            {
                readonly Func<int, T> m_UpdateCallback;
                T m_LocalValue;
                bool m_Updated;

                public T GetValue(int id)
                {
                    if (!m_Updated)
                    {
                        m_LocalValue = m_UpdateCallback(id);
                        m_Updated = true;
                    }

                    return m_LocalValue;
                }

                public CachedValue(Func<int, T> updateCallback)
                {
                    m_UpdateCallback = updateCallback;
                    m_Updated = false;
                    m_LocalValue = default(T);
                }

                public void Dirty()
                {
                    m_Updated = false;
                }

                public bool IsDirty()
                {
                    return !m_Updated;
                }
            }

            CachedValue<string> m_Name = new CachedValue<string>(GetName);
            CachedValue<string> m_Description = new CachedValue<string>(GetDescription);
            CachedValue<float> m_Progress = new CachedValue<float>(GetProgress);
            CachedValue<int> m_ParentId = new CachedValue<int>(GetParentId);
            CachedValue<DateTime> m_StartTime = new CachedValue<DateTime>(id => MSecToDateTime(GetStartDateTime(id)));
            CachedValue<DateTime> m_UpdateTime = new CachedValue<DateTime>(id => MSecToDateTime(GetUpdateDateTime(id)));
            CachedValue<Status> m_Status = new CachedValue<Status>(GetStatus);
            CachedValue<Options> m_Options = new CachedValue<Options>(GetOptions);
            CachedValue<TimeDisplayMode> m_TimeDisplayMode = new CachedValue<TimeDisplayMode>(GetTimeDisplayMode);
            CachedValue<TimeSpan> m_RemainingTime = new CachedValue<TimeSpan>(id => SecToTimeSpan(GetRemainingTime(id)));
            DateTime m_LastRemainingTime;

            public string name => m_Name.GetValue(id);
            public string description => m_Description.GetValue(id);
            public int id { get; internal set; }
            public float progress => m_Progress.GetValue(id);
            public int parentId => m_ParentId.GetValue(id);
            public DateTime startTime => m_StartTime.GetValue(id);
            public DateTime updateTime => m_UpdateTime.GetValue(id);
            public Status status => m_Status.GetValue(id);
            public Options options => m_Options.GetValue(id);
            public TimeDisplayMode timeDisplayMode => m_TimeDisplayMode.GetValue(id);

            public TimeSpan remainingTime
            {
                get
                {
                    if (m_RemainingTime.IsDirty())
                    {
                        m_LastRemainingTime = DateTime.Now;
                    }

                    var duration = (DateTime.Now - m_LastRemainingTime).Duration();
                    return m_RemainingTime.GetValue(id) - new TimeSpan(duration.Days, duration.Hours, duration.Minutes, duration.Seconds);
                }
            }

            public bool finished => status != Status.Running;
            public bool running => status == Status.Running;
            public bool responding => !running || (DateTime.Now.ToUniversalTime() - updateTime) <= TimeSpan.FromSeconds(5f);
            public bool cancellable => IsCancellable(id);
            public bool indefinite => running && (progress == -1f || (options & Options.Indefinite) == Options.Indefinite);
            public float elapsedTime => (float)(updateTime - startTime).TotalSeconds;
            public bool exists => Exists(id);

            internal Item(int id)
            {
                this.id = id;
            }

            public void Report(float newProgress)
            {
                Progress.Report(id, newProgress);
            }

            public void Report(float newProgress, string newDescription)
            {
                Progress.Report(id, newProgress, newDescription);
            }

            public bool Cancel()
            {
                return Progress.Cancel(id);
            }

            public void Finish(Status finishedStatus = Status.Succeeded)
            {
                Progress.Finish(id, finishedStatus);
            }

            public int Remove()
            {
                return Progress.Remove(id);
            }

            public void RegisterCancelCallback(Func<bool> callback)
            {
                Progress.RegisterCancelCallback(id, callback);
            }

            public void UnregisterCancelCallback()
            {
                Progress.UnregisterCancelCallback(id);
            }

            public void SetDescription(string newDescription)
            {
                Progress.SetDescription(id, newDescription);
            }

            public void SetTimeDisplayMode(TimeDisplayMode mode)
            {
                Progress.SetTimeDisplayMode(id, mode);
            }

            public void SetRemainingTime(long seconds)
            {
                Progress.SetRemainingTime(id, seconds);
            }

            public void ClearRemainingTime()
            {
                Progress.ClearRemainingTime(id);
            }

            internal void Dirty()
            {
                // Dirty only those that can change.
                m_Description.Dirty();
                m_Progress.Dirty();
                m_UpdateTime.Dirty();
                m_Status.Dirty();
                m_RemainingTime.Dirty();
                m_TimeDisplayMode.Dirty();
            }

            internal static DateTime MSecToDateTime(long msec)
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(msec);
            }

            internal static TimeSpan SecToTimeSpan(long sec)
            {
                return new TimeSpan(0, 0, 0, (int)sec);
            }
        }

        public class TaskReport
        {
            public TaskReport(float progress = -1f, string description = null)
            {
                this.progress = progress;
                this.description = description;
                error = null;
            }

            public float progress { get; internal set; }
            public string description { get; internal set; }
            public string error { get; internal set; }
        }

        public class TaskError : TaskReport
        {
            public TaskError(string error)
                : base(0f)
            {
                this.error = error;
            }
        }

        class Task
        {
            public int id { get; internal set; }
            public Func<int, object, IEnumerator> handler { get; internal set; }
            public object userData { get; internal set; }
            public Stack<IEnumerator> iterators { get; internal set; }
        }
    }
}
