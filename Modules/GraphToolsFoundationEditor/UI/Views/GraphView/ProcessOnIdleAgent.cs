// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The state component for the <see cref="ProcessOnIdleAgent"/>.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    class ProcessOnIdleStateComponent : StateComponent<ProcessOnIdleStateComponent.StateUpdater>
    {
        /// <summary>
        /// The state updater.
        /// </summary>
        public class StateUpdater : BaseUpdater<ProcessOnIdleStateComponent>
        {
            /// <summary>
            /// Records the fact that the mouse was idle.
            /// </summary>
            public void SetTriggerUpdate()
            {
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }
    }

    /// <summary>
    /// An agent responsible for triggering graph processing when the mouse stays idle for some period of time.
    /// </summary>
    class ProcessOnIdleAgent
    {
        internal const int idleTimeBeforeGraphProcessingMs_Internal = 1000;
        const int k_IdleTimeBeforeGraphProcessingMsPlayMode = 1000;

        readonly Stopwatch m_IdleTimer;
        Preferences m_Preferences;

        /// <summary>
        /// The state of the agent.
        /// </summary>
        public ProcessOnIdleStateComponent StateComponent { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessOnIdleAgent"/> class.
        /// </summary>
        /// <param name="preferences">The tool preferences.</param>
        public ProcessOnIdleAgent(Preferences preferences)
        {
            m_IdleTimer = new Stopwatch();
            m_Preferences = preferences;
            StateComponent = new ProcessOnIdleStateComponent();
        }

        void ResetTimer()
        {
            m_IdleTimer.Restart();
        }

        /// <summary>
        /// Stops the timer used to compute the mouse idle delay.
        /// </summary>
        public void StopTimer()
        {
            m_IdleTimer.Stop();
        }

        /// <summary>
        /// Callback for <see cref="MouseMoveEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        public void OnMouseMove(MouseMoveEvent e)
        {
            ResetTimer();
        }

        /// <summary>
        /// Updates the agent state if the mouse stays idle long enough.
        /// </summary>
        public void Execute()
        {
            if (m_Preferences.GetBool(BoolPref.OnlyProcessWhenIdle))
            {
                if (!m_IdleTimer.IsRunning)
                {
                    ResetTimer();
                }
            }
            else
            {
                if (m_IdleTimer.IsRunning)
                {
                    StopTimer();
                }
            }

            var elapsedTime = m_IdleTimer.ElapsedMilliseconds;
            if (elapsedTime >= (EditorApplication.isPlaying
                ? k_IdleTimeBeforeGraphProcessingMsPlayMode
                : idleTimeBeforeGraphProcessingMs_Internal))
            {
                m_IdleTimer.Restart();

                using (var updater = StateComponent.UpdateScope)
                {
                    updater.SetTriggerUpdate();
                }
            }
        }
    }
}
