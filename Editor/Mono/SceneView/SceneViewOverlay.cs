// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class SceneViewOverlay
    {
        // This enum is for better overview of the ordering of our builtin overlays
        public enum Ordering
        {
            // Lower order is below high order when showed together
            Camera = -100,
            ClothConstraints = 0,
            ClothSelfAndInterCollision = 100,
            OcclusionCulling = 200,
            Lightmapping = 300,
            NavMesh = 400,
            PhysicsDebug = 450,
            TilemapRenderer = 500,
            ParticleEffect = 600
        }

        public enum WindowDisplayOption
        {
            MultipleWindowsPerTarget,
            OneWindowPerTarget,
            OneWindowPerTitle
        }

        public delegate void WindowFunction(Object target, SceneView sceneView);

        static List<OverlayWindow> s_Windows;

        readonly SceneView m_SceneView;

        const float k_WindowPadding = 9f;

        GUIStyle m_TitleStyle;

        public SceneViewOverlay(SceneView sceneView)
        {
            m_SceneView = sceneView;
            if (s_Windows == null)
                s_Windows = new List<OverlayWindow>();
        }

        public void Begin()
        {
            if (Event.current.type == EventType.Layout)
                s_Windows.Clear();

            if (m_TitleStyle == null)
            {
                m_TitleStyle = new GUIStyle(GUI.skin.window);
                m_TitleStyle.padding.top = m_TitleStyle.padding.bottom;
            }

            m_SceneView.BeginWindows();
        }

        static class Styles
        {
            public static readonly GUIStyle sceneViewOverlayTransparentBackground = "SceneViewOverlayTransparentBackground";
        }

        public void End()
        {
            s_Windows.Sort();

            if (s_Windows.Count > 0)
            {
                var sceneViewGUIRect = m_SceneView.cameraRect;
                var windowOverlayRect = new Rect(sceneViewGUIRect.x, 0f, sceneViewGUIRect.width, m_SceneView.position.height);
                GUILayout.Window("SceneViewOverlay".GetHashCode(), windowOverlayRect, WindowTrampoline, "", Styles.sceneViewOverlayTransparentBackground);
            }

            m_SceneView.EndWindows();
        }

        float m_LastWidth;

        void WindowTrampoline(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(210));

            var paddingOffset = -k_WindowPadding;

            foreach (OverlayWindow win in s_Windows)
            {
                if (!m_SceneView.m_ShowSceneViewWindows && win.editorWindow != m_SceneView)
                    continue;

                GUILayout.Space(k_WindowPadding + paddingOffset);
                paddingOffset = 0f;
                EditorGUIUtility.ResetGUIState();
                if (win.canCollapse)
                {
                    GUILayout.BeginVertical(m_TitleStyle);

                    win.expanded = EditorGUILayout.Foldout(win.expanded, win.title, true);

                    if (win.expanded)
                        win.sceneViewFunc(win.target, m_SceneView);
                    GUILayout.EndVertical();
                }
                else
                {
                    GUILayout.BeginVertical(win.title, GUI.skin.window);
                    win.sceneViewFunc(win.target, m_SceneView);
                    GUILayout.EndVertical();
                }
            }

            GUILayout.EndVertical();

            var inputEaterRect = GUILayoutUtility.GetLastRect();
            EatMouseInput(inputEaterRect);

            if (Event.current.type == EventType.Repaint)
                m_LastWidth = inputEaterRect.width;

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        static void EatMouseInput(Rect position)
        {
            SceneView.AddCursorRect(position, MouseCursor.Arrow);

            var id = GUIUtility.GetControlID("SceneViewOverlay".GetHashCode(), FocusType.Passive, position);
            switch (Event.current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                        Event.current.Use();
                    break;
                case EventType.ScrollWheel:
                    if (position.Contains(Event.current.mousePosition))
                        Event.current.Use();
                    break;
            }
        }

        // pass window parameter to render in sceneviews that are not the active view.
        public static void Window(GUIContent title, WindowFunction sceneViewFunc, int order, WindowDisplayOption option)
        {
            Window(title, sceneViewFunc, order, null, option);
        }

        // pass window parameter to render in sceneviews that are not the active view.
        public static void Window(GUIContent title, WindowFunction sceneViewFunc, int order, Object target, WindowDisplayOption option, EditorWindow window = null)
        {
            if (Event.current.type != EventType.Layout)
                return;

            foreach (var overlayWindow in s_Windows)
            {
                if (option == WindowDisplayOption.OneWindowPerTarget && overlayWindow.target == target && target !=  null)
                    return;

                if (option == WindowDisplayOption.OneWindowPerTitle && (overlayWindow.title == title || overlayWindow.title.text == title.text))
                    return;
            }

            var newWindow = new OverlayWindow(title, sceneViewFunc, order, target, option)
            {
                secondaryOrder = s_Windows.Count,
                canCollapse = false
            };


            s_Windows.Add(newWindow);
        }

        public static void ShowWindow(OverlayWindow window)
        {
            if (Event.current.type != EventType.Layout)
                return;

            foreach (var overlayWindow in s_Windows)
            {
                if (window.option == WindowDisplayOption.OneWindowPerTarget && overlayWindow.target == window.target && window.target !=  null)
                    return;

                if (window.option == WindowDisplayOption.OneWindowPerTitle && (overlayWindow.title == window.title || overlayWindow.title.text == window.title.text))
                    return;
            }

            window.secondaryOrder = s_Windows.Count;

            s_Windows.Add(window);
        }
    }

    internal class OverlayWindow : IComparable<OverlayWindow>
    {
        public OverlayWindow(GUIContent title, SceneViewOverlay.WindowFunction guiFunction, int primaryOrder, Object target,
                             SceneViewOverlay.WindowDisplayOption option)
        {
            this.title = title;
            this.sceneViewFunc = guiFunction;
            this.primaryOrder = primaryOrder;
            this.option = option;
            this.target = target;
            this.canCollapse = true;
            this.expanded = true;
        }

        public SceneViewOverlay.WindowFunction sceneViewFunc { get;  }
        public int primaryOrder { get; }
        public int secondaryOrder { get; set; }
        public Object target { get; }
        public EditorWindow editorWindow { get; set; }

        public SceneViewOverlay.WindowDisplayOption option { get; } =
            SceneViewOverlay.WindowDisplayOption.MultipleWindowsPerTarget;

        public bool canCollapse { get; set; }
        public bool expanded { get; set; }

        public GUIContent title { get; }

        public int CompareTo(OverlayWindow other)
        {
            var result =  other.primaryOrder.CompareTo(primaryOrder);
            if (result == 0)
                result = other.secondaryOrder.CompareTo(secondaryOrder);
            return result;
        }
    }
}
