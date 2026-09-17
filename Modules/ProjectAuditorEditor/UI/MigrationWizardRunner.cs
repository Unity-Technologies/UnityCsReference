// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal sealed class MigrationStep
    {
        public string Title;
        public Func<bool> IsComplete;
        public Action Begin;

        // The run ends when it reaches such a step, handing off to the user.
        public bool IsManualStep;

        // A precondition no step of the run can satisfy. The run stops on reaching this step rather
        // than beginning one that could only fail; the view says what is missing.
        public Func<bool> CanBegin;

        // A transient condition, unlike CanBegin: the run holds on this step and retries on the next tick.
        public Func<bool> IsBlocked;

        public Func<string> GetError;
    }

    internal sealed class MigrationWizardRunner
    {
        // SessionState survives the install's domain reload but deliberately not an editor restart.
        internal const string k_RunActiveKey = "ProjectAuditor.MigrationWizard.RunActive";

        const double k_TickIntervalSeconds = 0.5;

        readonly List<MigrationStep> m_Steps;
        readonly Action m_OnChanged;
        readonly Func<bool> m_IsAlive;

        bool m_Running;
        double m_NextTickTime;
        int m_CurrentIndex = -1;
        int m_BegunIndex = -1;
        string m_LastError;

        public MigrationWizardRunner(List<MigrationStep> steps, Action onChanged, Func<bool> isAlive)
        {
            m_Steps = steps;
            m_OnChanged = onChanged;
            m_IsAlive = isAlive;
        }

        public bool IsRunning => m_Running;
        public int CurrentIndex => m_CurrentIndex;
        public int StepCount => m_Steps.Count;
        public string LastError => m_LastError;

        public string StatusMessage =>
            m_Running && m_CurrentIndex >= 0 && m_CurrentIndex < m_Steps.Count
                ? $"{m_Steps[m_CurrentIndex].Title}…"
                : string.Empty;

        // The URP install completes via a domain reload, so a run interrupted by it resumes from this.
        public static bool HasInterruptedRun => SessionState.GetBool(k_RunActiveKey, false);

        int FirstIncompleteIndex()
        {
            for (int i = 0; i < m_Steps.Count; i++)
            {
                if (!m_Steps[i].IsComplete())
                    return i;
            }
            return -1;
        }

        public void Start()
        {
            if (m_Running)
                return;

            m_LastError = null;
            m_Running = true;
            m_BegunIndex = -1;
            // Set for the repaint that follows this call; Advance re-derives it before beginning anything.
            m_CurrentIndex = FirstIncompleteIndex();

            SessionState.SetBool(k_RunActiveKey, true);

            // Nothing begins on this pass: Start runs mid-layout from a button handler, so a zero deadline defers
            // the first step to the next editor update.
            m_NextTickTime = 0.0;
            EditorApplication.update += Tick;

            m_OnChanged();
        }

        public void Stop()
        {
            m_Running = false;
            m_CurrentIndex = -1;
            m_BegunIndex = -1;
            EditorApplication.update -= Tick;
            SessionState.EraseBool(k_RunActiveKey);
            m_OnChanged();
        }

        // The view can be destroyed mid-run: detach without calling into it, leaving k_RunActiveKey set so reopening resumes.
        bool Abandoned()
        {
            if (m_IsAlive())
                return false;

            m_Running = false;
            EditorApplication.update -= Tick;
            return true;
        }

        // Internal so tests can reach the abandonment path without the editor's update loop.
        internal void Tick()
        {
            if (!m_Running)
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (Abandoned())
                return;

            // Throttled because a step's IsComplete can query project-wide state.
            if (EditorApplication.timeSinceStartup < m_NextTickTime)
                return;
            m_NextTickTime = EditorApplication.timeSinceStartup + k_TickIntervalSeconds;

            Advance();
        }

        internal void Advance()
        {
            while (true)
            {
                var next = FirstIncompleteIndex();

                // Every step complete, or the next one is the user's to run: either way the run is over.
                if (next < 0 || m_Steps[next].IsManualStep)
                {
                    Stop();
                    return;
                }

                var step = m_Steps[next];

                if (step.CanBegin != null && !step.CanBegin())
                {
                    Stop();
                    return;
                }

                m_CurrentIndex = next;

                // Held rather than failed: the next tick retries, with m_CurrentIndex already on this step.
                if (step.IsBlocked != null && step.IsBlocked())
                    return;

                if (m_BegunIndex != next)
                {
                    m_BegunIndex = next;
                    step.Begin?.Invoke();
                    m_OnChanged();
                }

                var error = step.GetError?.Invoke();
                if (!string.IsNullOrEmpty(error))
                {
                    m_LastError = error;
                    Stop();
                    return;
                }

                if (!step.IsComplete())
                    return;
            }
        }
    }
}
