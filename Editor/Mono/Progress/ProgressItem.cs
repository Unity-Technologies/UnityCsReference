// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace UnityEditor
{
    [DebuggerDisplay("name={name}")]
    public class ProgressItem
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
        }

        CachedValue<string> m_Name = new CachedValue<string>(Progress.GetName);
        CachedValue<string> m_Description = new CachedValue<string>(Progress.GetDescription);
        CachedValue<float> m_Progress = new CachedValue<float>(Progress.GetProgress);
        CachedValue<int> m_ParentId = new CachedValue<int>(Progress.GetParentId);
        CachedValue<DateTime> m_StartTime = new CachedValue<DateTime>(id => MSecToDateTime(Progress.GetStartDateTime(id)));
        CachedValue<DateTime> m_UpdateTime = new CachedValue<DateTime>(id => MSecToDateTime(Progress.GetUpdateDateTime(id)));
        CachedValue<ProgressStatus> m_Status = new CachedValue<ProgressStatus>(Progress.GetStatus);
        CachedValue<ProgressOptions> m_Options = new CachedValue<ProgressOptions>(Progress.GetOptions);

        public string name => m_Name.GetValue(id);
        public string description => m_Description.GetValue(id);
        public int id { get; internal set; }
        public float progress => m_Progress.GetValue(id);
        public int parentId => m_ParentId.GetValue(id);
        public DateTime startTime => m_StartTime.GetValue(id);
        public DateTime updateTime => m_UpdateTime.GetValue(id);
        public ProgressStatus status => m_Status.GetValue(id);
        public ProgressOptions options => m_Options.GetValue(id);

        public bool finished => status != ProgressStatus.Running;
        public bool running => status == ProgressStatus.Running;
        public bool responding => !running || (DateTime.Now.ToUniversalTime() - updateTime) <= TimeSpan.FromSeconds(5f);
        public bool cancellable => Progress.IsCancellable(id);
        public bool indefinite => running && (progress == -1f || (options & ProgressOptions.Indefinite) == ProgressOptions.Indefinite);
        public float elapsedTime => (float)(updateTime - startTime).TotalSeconds;

        internal ProgressItem(int id)
        {
            this.id = id;
        }

        public bool Cancel()
        {
            return Progress.Cancel(id);
        }

        internal void Dirty()
        {
            // Dirty only those that can change.
            m_Description.Dirty();
            m_Progress.Dirty();
            m_UpdateTime.Dirty();
            m_Status.Dirty();
        }

        static DateTime MSecToDateTime(long msec)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(msec);
        }
    }
}
