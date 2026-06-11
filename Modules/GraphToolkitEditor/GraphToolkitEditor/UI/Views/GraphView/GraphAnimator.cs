// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Drives <see cref="IAnimatableView"/> elements on the editor update loop while any are playing.
    /// </summary>
    [UnityRestricted]
    internal sealed class GraphAnimator : IDisposable
    {
        const double k_MaxDeltaTimeSeconds = 0.1;

        readonly GraphView m_GraphView;
        readonly HashSet<IAnimatableView> m_Active = new();
        readonly HashSet<IAnimatableView> m_Paused = new();

        double m_LastEditorTime;
        bool m_Subscribed;
        bool m_Disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphAnimator"/> class.
        /// </summary>
        /// <param name="graphView">The graph view that owns animatable elements.</param>
        public GraphAnimator(GraphView graphView)
        {
            m_GraphView = graphView;
        }

        /// <summary>
        /// Starts playback for an animatable and includes it in the editor update loop until stopped.
        /// If the same animatable is already playing, updates its speed by calling
        /// <see cref="IAnimatableView.BeginAnimating"/> again.
        /// </summary>
        /// <param name="animatableView">The animatable to play.</param>
        /// <param name="animationSpeed">Speed passed to <see cref="IAnimatableView.BeginAnimating"/>.</param>
        public void Play(IAnimatableView animatableView, float animationSpeed)
        {
            if (m_Disposed || animatableView == null)
                return;

            if (m_Active.Contains(animatableView))
            {
                animatableView.BeginAnimating(animationSpeed);
                return;
            }
            else if (m_Paused.Contains(animatableView))
            {
                Resume(animatableView, animationSpeed);
                return;
            }

            if (m_Active.Count == 0)
            {
                m_LastEditorTime = EditorApplication.timeSinceStartup;
                if (!m_Subscribed)
                {
                    EditorApplication.update += EditorTick;
                    m_Subscribed = true;
                }
            }

            m_Active.Add(animatableView);
            animatableView.BeginAnimating(animationSpeed);
        }

        public void Resume(IAnimatableView animatableView, float animationSpeed)
        {
            if (animatableView == null
                || m_Active.Contains(animatableView)
                || !m_Paused.Contains(animatableView))
                return;

            m_Active.Add(animatableView);
            m_Paused.Remove(animatableView);
            animatableView.BeginAnimating(animationSpeed);
        }

        public void Pause(IAnimatableView animatableView)
        {
            if (animatableView == null
                || !m_Active.Contains(animatableView)
                || m_Paused.Contains(animatableView))
                return;

            m_Paused.Add(animatableView);
            m_Active.Remove(animatableView);
        }

        /// <summary>
        /// Stops playback for an animatable and removes it from the update loop.
        /// </summary>
        /// <param name="animatableView">The animatable to stop.</param>
        public void Stop(IAnimatableView animatableView)
        {
            if (animatableView == null
                || (!m_Active.Contains(animatableView) && !m_Paused.Contains(animatableView)))
                return;

            animatableView.StopAnimating();
            m_Active.Remove(animatableView);
            m_Paused.Remove(animatableView);
            TryUnsubscribeIfEmpty();
        }

        void TryUnsubscribeIfEmpty()
        {
            if (m_Active.Count != 0 || m_Paused.Count != 0 || !m_Subscribed)
                return;

            EditorApplication.update -= EditorTick;
            m_Subscribed = false;
        }

        void EditorTick()
        {
            if (m_Disposed || (m_Active.Count == 0 && m_Paused.Count == 0))
            {
                TryUnsubscribeIfEmpty();
                return;
            }

            if (m_GraphView.panel == null)
                return;

            double now = EditorApplication.timeSinceStartup;
            double deltaTime = now - m_LastEditorTime;
            m_LastEditorTime = now;
            if (deltaTime < 0)
                deltaTime = 0;
            if (deltaTime > k_MaxDeltaTimeSeconds)
                deltaTime = k_MaxDeltaTimeSeconds;

            foreach (var animatable in m_Active)
            {
                animatable.AnimationUpdate(deltaTime);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;

            if (m_Subscribed)
            {
                EditorApplication.update -= EditorTick;
                m_Subscribed = false;
            }

            foreach (var animatable in m_Active)
            {
                animatable.StopAnimating();
            }

            m_Active.Clear();

            foreach (var animatable in m_Paused)
            {
                animatable.StopAnimating();
            }

            m_Paused.Clear();
        }
    }
}
