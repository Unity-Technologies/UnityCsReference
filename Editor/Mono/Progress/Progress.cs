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
            CachedValue<int> m_CurrentStep = new CachedValue<int>(GetCurrentStep);
            CachedValue<int> m_TotalSteps = new CachedValue<int>(GetTotalSteps);
            CachedValue<string> m_StepsLabel = new CachedValue<string>(GetStepLabel);
            CachedValue<int> m_ParentId = new CachedValue<int>(GetParentId);
            CachedValue<DateTime> m_StartTime = new CachedValue<DateTime>(id => MSecToDateTime(GetStartDateTime(id)));
            CachedValue<DateTime> m_UpdateTime = new CachedValue<DateTime>(id => MSecToDateTime(GetUpdateDateTime(id)));
            CachedValue<Status> m_Status = new CachedValue<Status>(GetStatus);
            CachedValue<Options> m_Options = new CachedValue<Options>(GetOptions);
            CachedValue<TimeDisplayMode> m_TimeDisplayMode = new CachedValue<TimeDisplayMode>(GetTimeDisplayMode);
            CachedValue<TimeSpan> m_RemainingTime = new CachedValue<TimeSpan>(id => SecToTimeSpan(GetRemainingTime(id)));
            CachedValue<int> m_Priority = new CachedValue<int>(GetPriority);
            DateTime m_LastRemainingTime;

            public string name => m_Name.GetValue(id);
            public string description => m_Description.GetValue(id);
            public int id { get; internal set; }
            public float progress => m_Progress.GetValue(id);
            public int currentStep => m_CurrentStep.GetValue(id);
            public int totalSteps => m_TotalSteps.GetValue(id);
            public string stepLabel => m_StepsLabel.GetValue(id);
            public int parentId => m_ParentId.GetValue(id);
            public DateTime startTime => m_StartTime.GetValue(id);
            public DateTime updateTime => m_UpdateTime.GetValue(id);
            public Status status => m_Status.GetValue(id);
            public Options options => m_Options.GetValue(id);
            public TimeDisplayMode timeDisplayMode => m_TimeDisplayMode.GetValue(id);
            public int priority => m_Priority.GetValue(id);

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

            public bool finished => status != Status.Running && status != Status.Paused;
            public bool running => status == Status.Running;
            public bool paused => status == Status.Paused;
            public bool responding => !running || (DateTime.Now.ToUniversalTime() - updateTime) <= TimeSpan.FromSeconds(5f) || (priority >= (int)Priority.Idle && priority < (int)Priority.Normal);
            public bool cancellable => IsCancellable(id);
            public bool pausable => IsPausable(id);
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

            public void Report(int newCurrentStep, int newTotalSteps)
            {
                Progress.Report(id, newCurrentStep, newTotalSteps);
            }

            public void Report(float newProgress, string newDescription)
            {
                Progress.Report(id, newProgress, newDescription);
            }

            public void Report(int newCurrentStep, int newTotalSteps, string newDescription)
            {
                Progress.Report(id, newCurrentStep, newTotalSteps, newDescription);
            }

            public bool Cancel()
            {
                return Progress.Cancel(id);
            }

            public bool Pause()
            {
                return Progress.Pause(id);
            }

            public bool Resume()
            {
                return Progress.Resume(id);
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

            public void RegisterPauseCallback(Func<bool, bool> callback)
            {
                Progress.RegisterPauseCallback(id, callback);
            }

            public void UnregisterPauseCallback()
            {
                Progress.UnregisterPauseCallback(id);
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

            public void SetPriority(int priority)
            {
                Progress.SetPriority(id, priority);
            }

            public void SetPriority(Priority priority)
            {
                Progress.SetPriority(id, priority);
            }

            public void ClearRemainingTime()
            {
                Progress.ClearRemainingTime(id);
            }

            public void SetStepLabel(string label)
            {
                Progress.SetStepLabel(id, label);
            }

            internal void Dirty()
            {
                // Dirty only those that can change.
                m_Description.Dirty();
                m_Priority.Dirty();
                m_Progress.Dirty();
                m_CurrentStep.Dirty();
                m_TotalSteps.Dirty();
                m_StepsLabel.Dirty();
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
    }
}
