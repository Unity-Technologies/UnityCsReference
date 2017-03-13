// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class TimeControl
    {
        // currentTime will be clamped to preview range.
        // Make sure it's initially at the beginning, even if the clip start is negative.
        public float currentTime = Mathf.NegativeInfinity;
        public float nextCurrentTime
        {
            set { deltaTime = value - currentTime; m_NextCurrentTimeSet = true; }
        }
        private bool m_NextCurrentTimeSet = false;
        public float startTime = 0.0f;
        public float stopTime = 1.0f;
        public bool playSelection = false;
        public bool loop = true;
        public float playbackSpeed = 1.0f;
        private float m_DeltaTime = 0.0f;
        private bool m_DeltaTimeSet = false;
        public float deltaTime
        {
            get { return m_DeltaTime; }
            set { m_DeltaTime = value; m_DeltaTimeSet = true; }
        }
        public float normalizedTime
        {
            // Don't use InverseLerp and Lerp since they clamp between 0 and 1
            get { return (stopTime == startTime) ? 0 : ((currentTime - startTime) / (stopTime - startTime)); }
            set { currentTime = startTime * (1 - value) + stopTime * value; }
        }
        public bool playing
        {
            get { return m_Playing; }
            set
            {
                if (m_Playing != value)
                {
                    // Start Playing
                    if (value)
                    {
                        EditorApplication.update += InspectorWindow.RepaintAllInspectors;
                        m_LastFrameEditorTime = EditorApplication.timeSinceStartup;

                        if (m_ResetOnPlay)
                        {
                            nextCurrentTime = startTime;
                            m_ResetOnPlay = false;
                        }
                    }
                    // Stop Playing
                    else
                    {
                        EditorApplication.update -= InspectorWindow.RepaintAllInspectors;
                    }
                }

                m_Playing = value;
            }
        }

        private double m_LastFrameEditorTime = 0.0f;
        private bool m_Playing = false;
        private bool m_ResetOnPlay = false;
        private float m_MouseDrag = 0.0f;
        private bool m_WrapForwardDrag = false;

        private const float kStepTime = 0.01f;
        private const float kScrubberHeight = 21;
        private const float kPlayButtonWidth = 33;

        private class Styles
        {
            public GUIContent playIcon = EditorGUIUtility.IconContent("PlayButton");
            public GUIContent pauseIcon = EditorGUIUtility.IconContent("PauseButton");

            public GUIStyle playButton = "TimeScrubberButton";
            public GUIStyle timeScrubber = "TimeScrubber";
        }
        private static Styles s_Styles;

        private static readonly int kScrubberIDHash = "ScrubberIDHash".GetHashCode();
        public void DoTimeControl(Rect rect)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            var evt = Event.current;
            int id = EditorGUIUtility.GetControlID(kScrubberIDHash, FocusType.Keyboard);

            // Play/Pause Button + Scrubber
            Rect timelineRect = rect;
            timelineRect.height = kScrubberHeight;
            // Only Scrubber
            Rect scrubberRect = timelineRect;
            scrubberRect.xMin += kPlayButtonWidth;

            // Handle Input
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (rect.Contains(evt.mousePosition))
                    {
                        EditorGUIUtility.keyboardControl = id;
                    }
                    if (scrubberRect.Contains(evt.mousePosition))
                    {
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        EditorGUIUtility.hotControl = id;
                        m_MouseDrag = evt.mousePosition.x - scrubberRect.xMin;
                        nextCurrentTime = (m_MouseDrag * (stopTime - startTime) / scrubberRect.width + startTime);
                        m_WrapForwardDrag = false;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (EditorGUIUtility.hotControl == id)
                    {
                        m_MouseDrag += evt.delta.x * playbackSpeed;
                        // We want to not wrap if we immediately drag to the beginning, but we do want to wrap if we drag past the end.
                        if (loop && ((m_MouseDrag < 0.0f && m_WrapForwardDrag) || (m_MouseDrag > scrubberRect.width)))
                        {
                            // scrubing out of range was generating a big deltaTime in wrong time direction
                            // this new code prevent this and it is compliant with new and more robust v5.0 root motion looping of animation clip
                            if (m_MouseDrag > scrubberRect.width)
                            {
                                currentTime -= (stopTime - startTime);
                            }
                            else if (m_MouseDrag < 0)
                            {
                                currentTime += (stopTime - startTime);
                            }

                            m_WrapForwardDrag = true;
                            m_MouseDrag = Mathf.Repeat(m_MouseDrag, scrubberRect.width);
                        }
                        nextCurrentTime = (Mathf.Clamp(m_MouseDrag, 0.0f, scrubberRect.width) * (stopTime - startTime) / scrubberRect.width + startTime);
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (EditorGUIUtility.hotControl == id)
                    {
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        EditorGUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (EditorGUIUtility.keyboardControl == id)
                    {
                        // TODO: loop?
                        if (evt.keyCode == KeyCode.LeftArrow)
                        {
                            if (currentTime - startTime > kStepTime)
                                deltaTime = -kStepTime;
                            evt.Use();
                        }
                        if (evt.keyCode == KeyCode.RightArrow)
                        {
                            if (stopTime - currentTime > kStepTime)
                                deltaTime = kStepTime;
                            evt.Use();
                        }
                    }
                    break;
            }

            // background
            GUI.Box(timelineRect, GUIContent.none, s_Styles.timeScrubber);

            // Play/Pause Button
            playing = GUI.Toggle(timelineRect, playing, playing ? s_Styles.pauseIcon : s_Styles.playIcon, s_Styles.playButton);

            // Current time indicator
            float normalizedPosition = Mathf.Lerp(scrubberRect.x, scrubberRect.xMax, normalizedTime);
            TimeArea.DrawPlayhead(normalizedPosition, scrubberRect.yMin, scrubberRect.yMax, 2f, (EditorGUIUtility.keyboardControl == id) ? 1f : 0.5f);
        }

        public void OnDisable()
        {
            playing = false;
        }

        public void Update()
        {
            // If the deltaTime was not set, update it when playing
            if (!m_DeltaTimeSet)
            {
                if (playing)
                {
                    double timeSinceStartup = EditorApplication.timeSinceStartup;
                    deltaTime = (float)(timeSinceStartup - m_LastFrameEditorTime) * playbackSpeed;
                    m_LastFrameEditorTime = timeSinceStartup;
                }
                else
                    deltaTime = 0;
            }


            currentTime += deltaTime;

            // If the nextCurrentTime was set explicitly, we don't want to loop
            bool wrap = loop && playing && !m_NextCurrentTimeSet;
            if (wrap)
            {
                normalizedTime = Mathf.Repeat(normalizedTime, 1.0f);
            }
            else
            {
                if (normalizedTime > 1)
                {
                    playing = false;
                    m_ResetOnPlay = true;
                }
                normalizedTime = Mathf.Clamp01(normalizedTime);
            }

            m_DeltaTimeSet = false;
            m_NextCurrentTimeSet = false;
        }
    }//class TimeControl
}//namespace UnityEditor
