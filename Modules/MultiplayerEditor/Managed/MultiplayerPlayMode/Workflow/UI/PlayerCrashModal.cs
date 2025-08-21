// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class PlayerCrashModal
    {
        public enum Choices
        {
            Restart,
            Continue,
        }

        public static Choices DisplayPlayerCrashModal(string player)
        {
            var option = EditorUtility.DisplayDialog(
                $"{player} unexpectedly stopped",
                $"It appears that {player} has unexpectedly stopped.\nDo you want to restart it?",
                "Yes",
                "No");

            return option ? Choices.Restart : Choices.Continue;
        }
    }
}
