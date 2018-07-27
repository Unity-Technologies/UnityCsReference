// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor
{
    internal class StylePicker
    {
        List<GUIView> m_ExploredViews = new List<GUIView>();
        GUIView m_ExploredView;
        EditorWindow m_BoundWindow;

        bool m_UpdateExploredGUIStyle;
        Vector2 m_MouseCursorScreenPos;

        public GUIView ExploredView => m_ExploredView;
        public GUIStyle ExploredStyle { get; set; }
        public int ExploredDrawInstructionIndex { get; private set; }
        public bool IsPicking { get; set; }
        public ElementHighlighter Highlighter { get; private set; }
        public Func<GUIView, bool> CanInspectView { get; set; }

        public StylePicker(EditorWindow window, ElementHighlighter highlighter = null)
        {
            m_BoundWindow = window;
            Highlighter = highlighter;
            CanInspectView = view => true;
        }

        public void StartExploreStyle()
        {
            // Debug.Log("Start Style Explorer");
            Assert.IsFalse(IsPicking);
            IsPicking = true;
            EditorApplication.update += FindStyleUnderMouse;
            GUIViewDebuggerHelper.GetViews(m_ExploredViews);
        }

        public void StopExploreStyle()
        {
            // Debug.Log("Start Style Explorer");
            IsPicking = false;
            EditorApplication.update -= FindStyleUnderMouse;
            m_ExploredViews.Clear();
            m_ExploredView = null;
            ExploredStyle = null;
            ExploredDrawInstructionIndex = -1;
            m_UpdateExploredGUIStyle = false;

            if (Highlighter != null)
            {
                Highlighter.ClearElement();
            }

            m_BoundWindow.Repaint();
        }

        public void OnGUI()
        {
            m_UpdateExploredGUIStyle = false;
            if (!IsPicking)
                return;

            // NOTE: we can only StopDebugging and StartDebugging OUTSIDE of OnGUI or else we crash (popclip/pushclip mismatch)
            // Only keep the data necessary to Start/Stop Debug in next Update tick.
            m_MouseCursorScreenPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            m_UpdateExploredGUIStyle = true;
        }

        private void UpdateExploredGUIStyle(Vector2 screenPos)
        {
            GUIView mouseUnderView = GetViewUnderMouse(screenPos, m_ExploredViews);
            ExploredDrawInstructionIndex = -1;
            ExploredStyle = null;
            if (mouseUnderView != m_ExploredView)
            {
                if (m_ExploredView)
                {
                    GUIViewDebuggerHelper.StopDebugging();
                    // Debug.Log("Stop debugging: " + GetViewName(m_ExploredView));
                }

                m_ExploredView = CanInspectView(mouseUnderView) ? mouseUnderView : null;

                if (m_ExploredView)
                {
                    // Start debugging
                    GUIViewDebuggerHelper.DebugWindow(m_ExploredView);
                    // Debug.Log("Start debugging: " + GetViewName(m_ExploredView));

                    // Since we have attached the debugger, this view hasn't logged its repaint steps yet.
                    m_ExploredView.Repaint();
                }
            }

            if (m_ExploredView)
            {
                var drawInstructions = new List<IMGUIDrawInstruction>();
                GUIViewDebuggerHelper.GetDrawInstructions(drawInstructions);

                var localPosition = new Vector2(screenPos.x - mouseUnderView.screenPosition.x,
                    screenPos.y - mouseUnderView.screenPosition.y);
                GUIStyle mouseUnderStyle = null;

                /** Note: no perfect way to find the style under cursor:
                    - Lots of style rect overlap
                    - by starting with the end, we hope to follow the "Last drawn instruction is the one on top"
                    - Some styles are "transparent" and drawn last (TabWindowBackground and such)
                    - We try to go with the smallest rect that fits
                */
                Rect styleRect = new Rect(0, 0, 10000, 10000);
                var smallestRectArea = styleRect.width * styleRect.height;
                for (var i = drawInstructions.Count - 1; i >= 0; --i)
                {
                    var instr = drawInstructions[i];
                    if (instr.rect.Contains(localPosition) && smallestRectArea > instr.rect.width * instr.rect.height)
                    {
                        mouseUnderStyle = instr.usedGUIStyle;
                        styleRect = instr.rect;
                        smallestRectArea = instr.rect.width * instr.rect.height;
                        ExploredDrawInstructionIndex = i;

                        // Debug.Log(GetViewName(m_ExploredView) + " - Found Style: " + instr.usedGUIStyle.name);
                    }
                }

                if (Highlighter != null && mouseUnderStyle != null)
                {
                    // Debug.Log(GetViewName(m_ExploredView) + " - Highlight Style: " + mouseUnderStyle.name);
                    Highlighter.HighlightElement(m_ExploredView.visualTree, styleRect, mouseUnderStyle);
                }

                ExploredStyle = mouseUnderStyle;
            }
        }

        private void FindStyleUnderMouse()
        {
            // Keep sending Repaint to the master view so it can update the mouse position
            m_BoundWindow.Repaint();

            if (m_UpdateExploredGUIStyle)
            {
                UpdateExploredGUIStyle(m_MouseCursorScreenPos);
            }
        }

        internal static GUIView GetViewUnderMouse(Vector2 mouseScreenPos, List<GUIView> views)
        {
            var windowUnderMouse = EditorWindow.mouseOverWindow;
            if (windowUnderMouse)
            {
                return views.Find(v => GetEditorWindow(v) == windowUnderMouse);
            }

            return views.Find(v => v.screenPosition.Contains(mouseScreenPos));
        }

        internal static EditorWindow GetEditorWindow(GUIView view)
        {
            var hostView = view as HostView;
            if (hostView != null)
                return hostView.actualView;

            return null;
        }

        internal static string GetViewName(GUIView view)
        {
            var editorWindow = GetEditorWindow(view);
            if (editorWindow != null)
                return editorWindow.titleContent.text;

            return view.GetType().Name;
        }
    }
}
