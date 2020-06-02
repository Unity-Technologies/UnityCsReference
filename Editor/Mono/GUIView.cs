// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.Scripting;

using FrameCapture = UnityEngine.Apple.FrameCapture;
using FrameCaptureDestination = UnityEngine.Apple.FrameCaptureDestination;


namespace UnityEditor
{
    // This is what we (not users) derive from to create various views. (Main Toolbar, etc.)
    [StructLayout(LayoutKind.Sequential)]
    internal partial class GUIView : View, IWindowModel
    {
        internal static event Action<GUIView> positionChanged = null;

        int m_DepthBufferBits = 0;
        int m_AntiAliasing = 1;
        bool m_AutoRepaintOnSceneChange = false;
        private IWindowBackend m_WindowBackend;

        protected EventInterests m_EventInterests;

        internal bool SendEvent(Event e)
        {
            int depth = SavedGUIState.Internal_GetGUIDepth();
            if (depth > 0)
            {
                SavedGUIState oldState = SavedGUIState.Create();
                var retval = Internal_SendEvent(e);
                if (retval)
                    EditorApplication.SignalTick();
                oldState.ApplyAndForget();
                return retval;
            }

            {
                var retval = Internal_SendEvent(e);
                if (retval)
                    EditorApplication.SignalTick();
                return retval;
            }
        }

        // Call into C++ here to move the underlying NSViews around
        internal override void SetWindow(ContainerWindow win)
        {
            base.SetWindow(win);
            Internal_Init(m_DepthBufferBits, m_AntiAliasing);
            if (win)
                Internal_SetWindow(win);
            Internal_SetAutoRepaint(m_AutoRepaintOnSceneChange);
            Internal_SetPosition(windowPosition);
            Internal_SetWantsMouseMove(m_EventInterests.wantsMouseMove);
            Internal_SetWantsMouseEnterLeaveWindow(m_EventInterests.wantsMouseMove);

            windowBackend?.SizeChanged();
        }

        internal void RecreateContext()
        {
            Internal_Recreate(m_DepthBufferBits, m_AntiAliasing);
        }

        Vector2 IWindowModel.size => windowPosition.size;

        public EventInterests eventInterests
        {
            get { return m_EventInterests; }
            set
            {
                m_EventInterests = value;

                windowBackend?.EventInterestsChanged();

                Internal_SetWantsMouseMove(wantsMouseMove);
                Internal_SetWantsMouseEnterLeaveWindow(wantsMouseEnterLeaveWindow);
            }
        }

        Action IWindowModel.onGUIHandler => OldOnGUI;

        public bool wantsMouseMove
        {
            get { return m_EventInterests.wantsMouseMove; }
            set
            {
                m_EventInterests.wantsMouseMove = value;
                windowBackend?.EventInterestsChanged();
                Internal_SetWantsMouseMove(wantsMouseMove);
            }
        }

        public bool wantsMouseEnterLeaveWindow
        {
            get { return m_EventInterests.wantsMouseEnterLeaveWindow; }
            set
            {
                m_EventInterests.wantsMouseEnterLeaveWindow = value;
                windowBackend?.EventInterestsChanged();
                Internal_SetWantsMouseEnterLeaveWindow(wantsMouseEnterLeaveWindow);
            }
        }

        public bool autoRepaintOnSceneChange
        {
            get { return m_AutoRepaintOnSceneChange; }
            set { m_AutoRepaintOnSceneChange = value; Internal_SetAutoRepaint(m_AutoRepaintOnSceneChange); }
        }

        public int depthBufferBits
        {
            get { return m_DepthBufferBits; }
            set { m_DepthBufferBits = value; }
        }

        public int antiAliasing
        {
            get { return m_AntiAliasing; }
            set { m_AntiAliasing = value; }
        }

        [Obsolete("AA is not supported on GUIViews", false)]
        public int antiAlias
        {
            get { return 1; }
            set { throw new NotSupportedException("AA is not supported on GUIViews"); }
        }

        internal IWindowBackend windowBackend
        {
            get { return m_WindowBackend; }
            set
            {
                if (m_WindowBackend != null)
                {
                    m_WindowBackend.OnDestroy(this);
                }

                m_WindowBackend = value;
                m_WindowBackend?.OnCreate(this);
            }
        }

        IWindowBackend IWindowModel.windowBackend
        {
            get { return windowBackend; }
            set { windowBackend = value; }
        }

        protected virtual void OnEnable()
        {
            windowBackend = EditorWindowBackendManager.GetBackend(this);
        }

        protected virtual void OnDisable()
        {
            windowBackend = null;
        }

        internal void ValidateWindowBackendForCurrentView()
        {
            if (!EditorWindowBackendManager.IsBackendCompatible(windowBackend, this))
            {
                //We create a new compatible backend
                windowBackend = EditorWindowBackendManager.GetBackend(this);
            }
        }

        protected virtual void OldOnGUI() {}

        // Without leaving this in here for MonoBehaviour::DoGUI(), GetMethod(MonoScriptCache::kGUI) will return null.
        // In that case, commands are not delegated (e.g., keyboard-based delete in Hierarchy/Project)
        protected virtual void OnGUI() {}

        protected override void SetPosition(Rect newPos)
        {
            Rect oldWinPos = windowPosition;

            base.SetPosition(newPos);
            if (oldWinPos == windowPosition)
            {
                Internal_SetPosition(windowPosition);
                return;
            }

            Internal_SetPosition(windowPosition);

            windowBackend?.SizeChanged();

            positionChanged?.Invoke(this);

            Repaint();
        }

        protected override void OnDestroy()
        {
            Internal_Close();

            base.OnDestroy();
        }

        // Draw resize handles, etc.
        internal virtual void DoWindowDecorationStart()
        {
        }

        internal virtual void DoWindowDecorationEnd()
        {
        }

        [RequiredByNativeCode]
        internal string GetViewName()
        {
            var hostView = this as HostView;
            if (hostView != null && hostView.actualView != null)
                return hostView.actualView.GetType().Name;

            return GetType().Name;
        }

        [RequiredByNativeCode]
        internal static string GetTypeNameOfMostSpecificActiveView()
        {
            var currentView = current;
            if (currentView == null)
                return string.Empty;

            var hostView = currentView as HostView;
            if (hostView != null && hostView.actualView != null)
                return hostView.actualView.GetType().FullName;

            return currentView.GetType().FullName;
        }

        public static void BeginOffsetArea(Rect screenRect, GUIContent content, GUIStyle style)
        {
            GUILayoutGroup g = EditorGUILayoutUtilityInternal.BeginLayoutArea(style, typeof(GUILayoutGroup));
            switch (Event.current.type)
            {
                case EventType.Layout:
                    g.resetCoords = false;
                    g.minWidth = g.maxWidth = screenRect.width;
                    g.minHeight = g.maxHeight = screenRect.height;
                    g.rect = Rect.MinMaxRect(0, 0, g.rect.xMax, g.rect.yMax);
                    break;
            }
            GUI.BeginGroup(screenRect, content, style);
        }

        public static void EndOffsetArea()
        {
            if (Event.current.type == EventType.Used)
                return;
            GUILayoutUtility.EndLayoutGroup();
            GUI.EndGroup();
        }

        // we already have renderdoc integration done in GUIView but in cpp
        // for metal we need a bit more convoluted logic and we can push more things to cs
        internal void CaptureMetalScene()
        {
            if (FrameCapture.IsDestinationSupported(FrameCaptureDestination.DevTools))
            {
                FrameCapture.BeginCaptureToXcode();
                RenderCurrentSceneForCapture();
                FrameCapture.EndCapture();
            }
            else if (FrameCapture.IsDestinationSupported(FrameCaptureDestination.GPUTraceDocument))
            {
                string path = EditorUtility.SaveFilePanel("Save Metal GPU Capture", "", PlayerSettings.productName + ".gputrace", "gputrace");
                if (System.String.IsNullOrEmpty(path))
                    return;

                FrameCapture.BeginCaptureToFile(path);
                RenderCurrentSceneForCapture();
                FrameCapture.EndCapture();

                System.Console.WriteLine("Metal capture saved to " + path);
                System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(path));
            }
        }
    }
} //namespace
