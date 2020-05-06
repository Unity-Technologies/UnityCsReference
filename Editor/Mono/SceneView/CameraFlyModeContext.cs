// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor
{
    [PriorityContext, ReserveModifiers(ShortcutModifiers.Shift)]
    class CameraFlyModeContext : IShortcutToolContext
    {
        public struct InputSamplingScope : IDisposable
        {
            bool active => m_ArrowKeysActive || m_Context.active;

            public bool currentlyMoving => active && !Mathf.Approximately(currentInputVector.sqrMagnitude, 0f);

            public Vector3 currentInputVector => active ? s_CurrentInputVector : Vector3.zero;

            public bool inputVectorChanged => active && !Mathf.Approximately((s_CurrentInputVector - m_Context.m_PreviousVector).sqrMagnitude, 0f);

            readonly bool m_ArrowKeysActive;
            bool m_Disposed;
            readonly CameraFlyModeContext m_Context;

            // controlID will get hotControl if using arrow keys while shortcut context is not active
            // passing a value of zero disables the arrow keys
            public InputSamplingScope(CameraFlyModeContext context, ViewTool currentViewTool, int controlID, EditorWindow window, bool orthographic = false)
            {
                m_ArrowKeysActive = false;
                m_Disposed = false;
                m_Context = context;
                m_Context.active = currentViewTool == ViewTool.FPS;
                m_Context.m_Window = window;

                if (m_Context.active)
                {
                    ShortcutIntegration.instance.contextManager.RegisterToolContext(context);
                    ForceArrowKeysUp(orthographic);
                }
                else
                {
                    ShortcutIntegration.instance.contextManager.DeregisterToolContext(context);
                    m_ArrowKeysActive = DoArrowKeys(controlID, orthographic);
                }

                if (currentlyMoving && Mathf.Approximately(m_Context.m_PreviousVector.sqrMagnitude, 0f))
                    s_Timer.Begin();
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                m_Context.m_PreviousVector = currentInputVector;
            }

            static readonly Dictionary<KeyCode, Action<bool, ShortcutArguments>> s_ArrowKeyBindings =
                new Dictionary<KeyCode, Action<bool, ShortcutArguments>>
            {
                { KeyCode.UpArrow, (orthographic, args) => { if (orthographic) WalkUp(args); else WalkForward(args); } },
                { KeyCode.DownArrow, (orthographic, args) => { if (orthographic) WalkDown(args); else WalkBackward(args); } },
                { KeyCode.LeftArrow, (orthographic, args) => WalkLeft(args) },
                { KeyCode.RightArrow, (orthographic, args) => WalkRight(args) },
            };
            static readonly HashSet<KeyCode> s_ArrowKeysDown = new HashSet<KeyCode>();

            void ForceArrowKeysUp(bool orthographic)
            {
                foreach (KeyCode key in s_ArrowKeysDown)
                {
                    var action = s_ArrowKeyBindings[key];
                    action(orthographic, new ShortcutArguments { stage = ShortcutStage.End, context = m_Context });
                }

                s_ArrowKeysDown.Clear();
            }

            bool DoArrowKeys(int id, bool orthographic)
            {
                if (id == 0 || GUIUtility.hotControl != 0 && GUIUtility.hotControl != id)
                    return false;
                if (EditorGUI.actionKey)
                    return false;

                Action<bool, ShortcutArguments> action;
                var evt = Event.current;
                switch (evt.GetTypeForControl(id))
                {
                    case EventType.KeyDown:
                        if (!s_ArrowKeyBindings.TryGetValue(evt.keyCode, out action))
                            return false;
                        action(orthographic, new ShortcutArguments { stage = ShortcutStage.Begin, context = m_Context });
                        GUIUtility.hotControl = id;
                        s_ArrowKeysDown.Add(evt.keyCode);
                        evt.Use();
                        return true;
                    case EventType.KeyUp:
                        if (!s_ArrowKeyBindings.TryGetValue(evt.keyCode, out action))
                            return false;
                        action(orthographic, new ShortcutArguments { stage = ShortcutStage.End, context = m_Context });
                        s_ArrowKeysDown.Remove(evt.keyCode);
                        if (s_ArrowKeysDown.Count == 0)
                        {
                            GUIUtility.hotControl = 0;
                            s_CurrentInputVector = Vector3.zero;
                        }
                        evt.Use();
                        return true;
                    default:
                        return GUIUtility.hotControl == id;
                }
            }
        }

        public bool active { get; set; }

        EditorWindow m_Window;
        Vector3 m_PreviousVector;

        public static float deltaTime => s_Timer.Update();

        static TimeHelper s_Timer = new TimeHelper();

        static Vector3 s_CurrentInputVector;

        [ClutchShortcut("3D Viewport/Fly Mode Forward", typeof(CameraFlyModeContext), KeyCode.W)]
        [FormerlyPrefKeyAs("View/FPS Forward", "w")]
        static void WalkForward(ShortcutArguments args)
        {
            s_CurrentInputVector.z = args.stage == ShortcutStage.Begin ? 1f : s_CurrentInputVector.z > 0f ? 0f : s_CurrentInputVector.z;
            var context = (CameraFlyModeContext)args.context;
            context.m_Window.Repaint();
        }

        [ClutchShortcut("3D Viewport/Fly Mode Backward", typeof(CameraFlyModeContext), KeyCode.S)]
        [FormerlyPrefKeyAs("View/FPS Back", "s")]
        static void WalkBackward(ShortcutArguments args)
        {
            s_CurrentInputVector.z = args.stage == ShortcutStage.Begin ? -1f : s_CurrentInputVector.z < 0f ? 0f : s_CurrentInputVector.z;
            var context = (CameraFlyModeContext)args.context;
            context.m_Window.Repaint();
        }

        [ClutchShortcut("3D Viewport/Fly Mode Left", typeof(CameraFlyModeContext), KeyCode.A)]
        [FormerlyPrefKeyAs("View/FPS Strafe Left", "a")]
        static void WalkLeft(ShortcutArguments args)
        {
            s_CurrentInputVector.x = args.stage == ShortcutStage.Begin ? -1f : s_CurrentInputVector.x < 0f ? 0f : s_CurrentInputVector.x;
            var context = (CameraFlyModeContext)args.context;
            context.m_Window.Repaint();
        }

        [ClutchShortcut("3D Viewport/Fly Mode Right", typeof(CameraFlyModeContext), KeyCode.D)]
        [FormerlyPrefKeyAs("View/FPS Strafe Right", "d")]
        static void WalkRight(ShortcutArguments args)
        {
            s_CurrentInputVector.x = args.stage == ShortcutStage.Begin ? 1f : s_CurrentInputVector.x > 0f ? 0f : s_CurrentInputVector.x;
            var context = (CameraFlyModeContext)args.context;
            context.m_Window.Repaint();
        }

        [ClutchShortcut("3D Viewport/Fly Mode Up", typeof(CameraFlyModeContext), KeyCode.E)]
        [FormerlyPrefKeyAs("View/FPS Strafe Up", "e")]
        static void WalkUp(ShortcutArguments args)
        {
            s_CurrentInputVector.y = args.stage == ShortcutStage.Begin ? 1f : s_CurrentInputVector.y > 0f ? 0f : s_CurrentInputVector.y;
            var context = (CameraFlyModeContext)args.context;
            context.m_Window.Repaint();
        }

        [ClutchShortcut("3D Viewport/Fly Mode Down", typeof(CameraFlyModeContext), KeyCode.Q)]
        [FormerlyPrefKeyAs("View/FPS Strafe Down", "q")]
        static void WalkDown(ShortcutArguments args)
        {
            s_CurrentInputVector.y = args.stage == ShortcutStage.Begin ? -1f : s_CurrentInputVector.y < 0f ? 0f : s_CurrentInputVector.y;
            var context = (CameraFlyModeContext)args.context;
            context.m_Window.Repaint();
        }
    }
}
