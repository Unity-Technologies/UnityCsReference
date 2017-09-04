// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
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

        private class OverlayWindow : IComparable<OverlayWindow>
        {
            public GUIContent m_Title;
            public SceneViewOverlay.WindowFunction m_SceneViewFunc;
            public int m_PrimaryOrder;      // lower order is below high order
            public int m_SecondaryOrder;    // used for primary order that are equal (should be unique)
            public Object m_Target;

            public int CompareTo(OverlayWindow other)
            {
                int result =  other.m_PrimaryOrder.CompareTo(m_PrimaryOrder);
                if (result == 0)
                    result = other.m_SecondaryOrder.CompareTo(m_SecondaryOrder);
                return result;
            }
        };
        public delegate void WindowFunction(Object target, SceneView sceneView);

        static List<OverlayWindow> m_Windows;

        Rect m_WindowRect = new Rect(0, 0, 0, 0);

        SceneView m_SceneView;

        float k_WindowPadding = 9f;

        public SceneViewOverlay(SceneView sceneView)
        {
            m_SceneView = sceneView;
            m_Windows = new List<OverlayWindow>();
        }

        public void Begin()
        {
            if (!m_SceneView.m_ShowSceneViewWindows)
                return;

            if (Event.current.type == EventType.Layout)
                m_Windows.Clear();

            m_SceneView.BeginWindows();
        }

        public void End()
        {
            if (!m_SceneView.m_ShowSceneViewWindows)
                return;

            m_Windows.Sort();

            if (m_Windows.Count > 0)
            {
                m_WindowRect.x = 0;
                m_WindowRect.y = 0;
                m_WindowRect.width = m_SceneView.position.width;
                m_WindowRect.height = m_SceneView.position.height;
                m_WindowRect = GUILayout.Window("SceneViewOverlay".GetHashCode(), m_WindowRect, WindowTrampoline, "", "SceneViewOverlayTransparentBackground");
            }

            m_SceneView.EndWindows();
        }

        private void WindowTrampoline(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            float paddingOffset = -k_WindowPadding;
            foreach (OverlayWindow win in m_Windows)
            {
                GUILayout.Space(k_WindowPadding + paddingOffset);
                paddingOffset = 0f;
                EditorGUIUtility.ResetGUIState();
                GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
                EditorStyles.UpdateSkinCache(1);              // EditorResources.h defines this as the index for the dark skin
                GUILayout.BeginVertical(win.m_Title, GUI.skin.window);
                win.m_SceneViewFunc(win.m_Target, m_SceneView);
                GUILayout.EndVertical();
            }
            EditorStyles.UpdateSkinCache();              // Sets the cache back according to the user selected skin
            GUILayout.EndVertical();
            Rect inputEaterRect = GUILayoutUtility.GetLastRect();
            EatMouseInput(inputEaterRect);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void EatMouseInput(Rect position)
        {
            SceneView.AddCursorRect(position, MouseCursor.Arrow);

            int id = GUIUtility.GetControlID("SceneViewOverlay".GetHashCode(), FocusType.Passive, position);
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

        public static void Window(GUIContent title, WindowFunction sceneViewFunc, int order, WindowDisplayOption option)
        {
            Window(title, sceneViewFunc, order, null, option);
        }

        public static void Window(GUIContent title, WindowFunction sceneViewFunc, int order, Object target, WindowDisplayOption option)
        {
            if (Event.current.type != EventType.Layout)
                return;

            foreach (OverlayWindow overlayWindow in m_Windows)
            {
                if (option == WindowDisplayOption.OneWindowPerTarget && overlayWindow.m_Target == target && target !=  null)
                    return;

                if (option == WindowDisplayOption.OneWindowPerTitle && (overlayWindow.m_Title == title || overlayWindow.m_Title.text == title.text))
                    return;
            }

            OverlayWindow newWindow = new OverlayWindow();
            newWindow.m_Title = title;
            newWindow.m_SceneViewFunc = sceneViewFunc;
            newWindow.m_PrimaryOrder = order;
            newWindow.m_SecondaryOrder = m_Windows.Count; // just use a value that is unique across overlays
            newWindow.m_Target = target;

            m_Windows.Add(newWindow);
        }
    }
}
