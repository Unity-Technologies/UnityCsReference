// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    /// <summary>
    /// Base class for play mode scenario assets. This is used to configure the play mode behavior.
    /// </summary>
    public abstract class PlayModeScenario : ScriptableObject
    {
        [SerializeField]
        string m_Description;

        PlayModeScenarioState m_State;

        /// <summary>
        /// Returns the description of the play mode config.
        /// </summary>
        internal string Description
        {
            get => m_Description;
            set => m_Description = value;
        }

        /// <summary>
        /// Returns true if the play mode config supports pause and step.
        /// </summary>
        internal abstract bool SupportsPauseAndStep { get; }

        internal PlayModeScenarioState GetState() => m_State;

        internal void SetState(PlayModeScenarioState state)
        {
            m_State = state;
            ScenarioManagerProvider.instance.ReportScenarioStateChanged();
        }

        /// <summary>
        /// Implement this method to prepare and execute the play mode. The method will be called when the play mode is requested to start.
        /// </summary>
        internal abstract void ExecuteStart();

        /// <summary>
        /// Implement this method to exit the play mode. The method will be called when the play mode is requested to stop.
        /// </summary>
        internal abstract void ExecuteStop();

        /// <summary>
        /// Implement this method to create additional UI elements for the play mode top bar when this play mode config is active.
        /// </summary>
        /// <returns>
        /// Returns the visual element that will be added to the top bar.
        /// </returns>
        internal virtual VisualElement CreateTopbarUI() => null;

        /// <summary>
        /// Implement this method to create the UI elements for the active scenario window when this play mode config is selected.
        /// </summary>
        /// <returns>
        /// Returns the visual element that will be added to the scenario window.
        /// </returns>
        internal virtual VisualElement CreateScenarioUI() => null;

        /// <summary>
        /// Provide an icon to be shown in the playmode dropdown.
        /// </summary>
        /// <returns>
        /// Returns an icon that will show next to the configuration.
        /// </returns>
        internal virtual Texture2D Icon => EditorGUIUtility.FindTexture("UnityLogo");

        internal virtual bool IsValid(out string reasonForInvalidConfiguration)
        {
            reasonForInvalidConfiguration = null;
            return true;
        }

        /// <summary>
        /// Implement this method to be notified of when the User selects this Play Mode Configuration.
        /// </summary>
        internal virtual void OnSelected() { }

        /// <summary>
        /// Implement this method to be notified of when the User de-selects this Play Mode Configuration.
        /// </summary>
        internal virtual void OnDeselected() { }

        /// <summary>
        /// Override this method to manage the de-selection behavior of the Play Mode Configuration.
        /// This is particularly useful for situations where you need to 'lock' an active configuration
        /// to ensure stability or prevent unintended changes while the current configuration is in use.
        /// </summary>
        /// <returns>
        /// Returns true if the user is allowed to switch away from the current configuration.
        /// </returns>
        internal virtual bool WantsToDeselect() => true;
    }
}
