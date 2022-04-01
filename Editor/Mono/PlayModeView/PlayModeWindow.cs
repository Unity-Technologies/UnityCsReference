// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    public class PlayModeWindow
    {
        static Type s_SimulatorWindowType =
            PlayModeView.GetAvailableWindowTypes().Keys.FirstOrDefault(x => x.Name.Contains("SimulatorWindow"));

        PlayModeWindow()
        {
        }

        public enum PlayModeViewTypes
        {
            GameView,
            SimulatorView
        }

        public static void GetRenderingResolution(out uint width, out uint height)
        {
            var view = GetOrCreateWindow();
            width = (uint)view.targetSize.x;
            height = (uint)view.targetSize.y;
        }

        public static void SetCustomRenderingResolution(uint width, uint height, string baseName)
        {
            var win = GetOrCreateWindow();
            var gameView = win as GameView;

            if (gameView != null)
            {
                gameView.SetCustomResolution(new Vector2(width, height), baseName);
            }
            else
            {
                Debug.LogError($"The {win.GetType().Name} does not support custom resolution");
            }
        }

        public static void SetPlayModeFocused(bool focused)
        {
            GetOrCreateWindow().enterPlayModeBehavior = focused
                ? PlayModeView.EnterPlayModeBehavior.PlayFocused
                : PlayModeView.EnterPlayModeBehavior.PlayMaximized;
        }

        public static bool GetPlayModeFocused()
        {
            return GetOrCreateWindow().enterPlayModeBehavior == PlayModeView.EnterPlayModeBehavior.PlayFocused;
        }

        public static PlayModeViewTypes GetViewType()
        {
            var type = GetOrCreateWindow().GetType();
            if (type == typeof(GameView))
                return PlayModeViewTypes.GameView;
            if (type == s_SimulatorWindowType)
                return PlayModeViewTypes.SimulatorView;

            throw new Exception("Unsupported PlayModeView type");
        }

        public static void SetViewType(PlayModeViewTypes type)
        {
            var view = GetOrCreateWindow();
            switch (type)
            {
                case PlayModeViewTypes.GameView:
                    view.SwapMainWindow(typeof(GameView));
                    return;
                case PlayModeViewTypes.SimulatorView:
                {
                    // DeviceSim is a Module that might be disabled in the future.
                    if (s_SimulatorWindowType == null)
                    {
                        throw new Exception("Cannot find the SimulatorWindow type.");
                    }
                    view.SwapMainWindow(s_SimulatorWindowType);
                    return;
                }
                default:
                    throw new Exception("Unsupported PlayModeView type");
            }
        }

        static PlayModeView GetOrCreateWindow()
        {
            var view = PlayModeView.GetMainPlayModeView();
            if (view == null)
            {
                return EditorWindow.CreateWindow<GameView>();
            }

            return view;
        }
    }
}
