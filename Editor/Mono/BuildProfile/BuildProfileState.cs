// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile
{
    [VisibleToOtherModules]
    internal enum ActionState
    {
        /// <summary>
        /// Action is visible and clickable.
        /// </summary>
        Enabled,

        /// <summary>
        /// Action is visible and non-clickable.
        /// </summary>
        Disabled,

        /// <summary>
        /// Action is hidden.
        /// </summary>
        Hidden
    }

    [VisibleToOtherModules]
    internal class BuildProfileWorkflowState
    {
        Action<BuildProfileWorkflowState> m_OnStateChanged;

        public BuildProfileWorkflowState(Action<BuildProfileWorkflowState> onStateChanged)
        {
            this.m_OnStateChanged = onStateChanged;
        }

        /// <summary>
        /// When set, build actions in the Build Profile Window will query the user for a build location.
        /// </summary>
        public bool askForBuildLocation { get; set; } = true;

        /// <summary>
        /// Name to be displayed in the build button name
        /// </summary>
        public string buildButtonDisplayName { get; set; } = L10n.Tr("Build");

        /// <summary>
        /// Name to be displayed in the build and run button name
        /// </summary>
        public string buildAndRunButtonDisplayName { get; set; } = L10n.Tr("Build And Run");

        /// <summary>
        /// Activate action allows a profile to be set as active profile.
        /// </summary>
        public ActionState activateAction { get; set; } = ActionState.Enabled;

        /// <summary>
        /// Allows invoking the Build Pipeline for the selected profile.
        /// </summary>
        public ActionState buildAction { get; set; } = ActionState.Enabled;

        /// <summary>
        /// Allows invoking the Build Pipeline for the selected profile with BuildAndRun flag set.
        /// </summary>
        public ActionState buildAndRunAction { get; set; } = ActionState.Enabled;

        /// <summary>
        /// Allows invoking of Cloud Build for the selected profile.
        /// </summary>
        public ActionState buildInCloudPackageAction { get; set; } = ActionState.Enabled;

        /// <summary>
        /// Additional actions shown in the Build Profile Window as generally defined by the Build Profile Extension.
        /// </summary>
        /// <remarks>
        /// Primary use case is for 'Run Last Build' action from console paltforms which implement
        /// module specific build callbacks.
        /// </remarks>
        public IList<(string displayName, bool isOn, Action callback, ActionState state)> additionalActions { get; set; }
            = new List<(string, bool, Action, ActionState)>();

        /// <summary>
        /// Reprocess the current state.
        /// </summary>
        public void Refresh() => m_OnStateChanged?.Invoke(this);

        /// <summary>
        /// Apply the next state, OnStateChanged callback should handle reducing the
        /// target state into the current one.
        /// </summary>
        public void Apply(BuildProfileWorkflowState next) => m_OnStateChanged?.Invoke(next);

        /// <summary>
        /// Merges two <see cref="ActionState"/> values.
        /// </summary>
        public static ActionState CalculateActionState(ActionState lhs, ActionState rhs)
        {
            if (lhs == ActionState.Hidden || rhs == ActionState.Hidden)
                return ActionState.Hidden;
            if (lhs == ActionState.Disabled || rhs == ActionState.Disabled)
                return ActionState.Disabled;
            return ActionState.Enabled;
        }

        /// <summary>
        /// Update the build action and the build and run action to the specified <see cref="ActionState"/> and refresh.
        /// </summary>
        public void UpdateBuildActionStates(ActionState buildActionState, ActionState buildAndRunActionState)
        {
            if (buildAction == buildActionState && buildAndRunAction == buildAndRunActionState)
                return;

            buildAction = buildActionState;
            buildAndRunAction = buildAndRunActionState;
            Refresh();
        }
    }
}
