// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal static class MultiplayerStatusToolbarUtilities
    {
        internal static MainToolbarContent GetStatusDropdownContent()
        {
            var state = ScenarioRunner.GetScenarioStatus();
            var stage = state.CurrentStage;

            var labelText = state.OverallStatus.State.ToString();
            var tooltipText = "";
            var iconImage = default(Texture2D);

            switch (state.OverallStatus.State)
            {
                case ExecutionState.Running:
                    iconImage = Icons.GetImage(state.IsExecutingLaunchingStages() ? Icons.ImageName.Loading : Icons.ImageName.CompletedTask);
                    labelText = LaunchingScenarioWindow.GetLabelForStage(stage);
                    break;
                case ExecutionState.Failed:
                    tooltipText = "Scenario has failed. See console for details.";
                    iconImage = Icons.GetImage(Icons.ImageName.Error);
                    break;
                case ExecutionState.Invalid:
                    labelText = "Idle";
                    iconImage = Icons.GetImage(Icons.ImageName.Idle);
                    break;
                default:
                    iconImage = Icons.GetImage(Icons.ImageName.Idle);
                    break;
            }

            return new MainToolbarContent(labelText, iconImage, tooltipText);
        }

        internal static void ShowStatusPopup(Rect buttonRect)
        {
            UnityEditor.PopupWindow.Show(
                new Rect(buttonRect.x + buttonRect.width - PlaymodeStatusPopupContent.windowSize.x, buttonRect.y, buttonRect.width, buttonRect.height),
                new PlaymodeStatusPopupContent());
        }
    }
}
