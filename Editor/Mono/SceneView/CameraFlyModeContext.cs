// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor
{
    class CameraFlyModeContext : IShortcutPriorityContext
    {
        public struct InputSamplingScope : IDisposable
        {
            public bool currentlyMoving => m_Context.active && !Mathf.Approximately(currentInputVector.sqrMagnitude, 0f);

            public Vector3 currentInputVector => m_Context.active ? s_CurrentInputVector : Vector3.zero;

            public bool inputVectorChanged => m_Context.active && !Mathf.Approximately((s_CurrentInputVector - m_Context.m_PreviousVector).sqrMagnitude, 0f);

            bool m_Disposed;
            readonly CameraFlyModeContext m_Context;

            public InputSamplingScope(CameraFlyModeContext context)
            {
                m_Disposed = false;
                m_Context = context;

                if (currentlyMoving && Mathf.Approximately(m_Context.m_PreviousVector.sqrMagnitude, 0f))
                    s_Timer.Begin();

                if (context.active)
                    ShortcutController.priorityContext = context;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                m_Context.m_PreviousVector = currentInputVector;
            }
        }

        public bool active { get; set; }

        Vector3 m_PreviousVector;

        public static float deltaTime => s_Timer.Update();

        static TimeHelper s_Timer = new TimeHelper();

        static Vector3 s_CurrentInputVector;

        [ClutchShortcut("3D Viewport/Fly Mode Forward", typeof(CameraFlyModeContext), "w")]
        [FormerlyPrefKeyAs("View/FPS Forward", "w")]
        static void WalkForward(ShortcutArguments args)
        {
            s_CurrentInputVector.z = args.state == ShortcutState.Begin ? 1f : s_CurrentInputVector.z > 0f ? 0f : s_CurrentInputVector.z;
        }

        [ClutchShortcut("3D Viewport/Fly Mode Backward", typeof(CameraFlyModeContext), "s")]
        [FormerlyPrefKeyAs("View/FPS Back", "s")]
        static void WalkBackward(ShortcutArguments args)
        {
            s_CurrentInputVector.z = args.state == ShortcutState.Begin ? -1f : s_CurrentInputVector.z < 0f ? 0f : s_CurrentInputVector.z;
        }

        [ClutchShortcut("3D Viewport/Fly Mode Left", typeof(CameraFlyModeContext), "a")]
        [FormerlyPrefKeyAs("View/FPS Strafe Left", "a")]
        static void WalkLeft(ShortcutArguments args)
        {
            s_CurrentInputVector.x = args.state == ShortcutState.Begin ? -1f : s_CurrentInputVector.x < 0f ? 0f : s_CurrentInputVector.x;
        }

        [ClutchShortcut("3D Viewport/Fly Mode Right", typeof(CameraFlyModeContext), "d")]
        [FormerlyPrefKeyAs("View/FPS Strafe Right", "d")]
        static void WalkRight(ShortcutArguments args)
        {
            s_CurrentInputVector.x = args.state == ShortcutState.Begin ? 1f : s_CurrentInputVector.x > 0f ? 0f : s_CurrentInputVector.x;
        }

        [ClutchShortcut("3D Viewport/Fly Mode Up", typeof(CameraFlyModeContext), "e")]
        [FormerlyPrefKeyAs("View/FPS Strafe Up", "e")]
        static void WalkUp(ShortcutArguments args)
        {
            s_CurrentInputVector.y = args.state == ShortcutState.Begin ? 1f : s_CurrentInputVector.y > 0f ? 0f : s_CurrentInputVector.y;
        }

        [ClutchShortcut("3D Viewport/Fly Mode Down", typeof(CameraFlyModeContext), "q")]
        [FormerlyPrefKeyAs("View/FPS Strafe Down", "q")]
        static void WalkDown(ShortcutArguments args)
        {
            s_CurrentInputVector.y = args.state == ShortcutState.Begin ? -1f : s_CurrentInputVector.y < 0f ? 0f : s_CurrentInputVector.y;
        }
    }
}
