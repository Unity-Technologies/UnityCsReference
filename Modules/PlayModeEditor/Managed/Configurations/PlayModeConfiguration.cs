// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    /// <summary>
    /// Base class for play mode config assets. This is used to configure the play mode behavior.
    /// </summary>
    abstract class PlayModeConfiguration : ScriptableObject
    {
        [SerializeField]
        string m_Description;

        /// <summary>
        /// Returns the description of the play mode config.
        /// </summary>
        public virtual string Description
        {
            get => m_Description;
            protected set => m_Description = value;
        }

        /// <summary>
        /// Returns true if the play mode config supports pause and step.
        /// </summary>
        public abstract bool SupportsPauseAndStep { get; }

        /// <summary>
        /// Implement this method to prepare and execute the play mode. The method will be called when the play mode is requested to start.
        /// </summary>
        /// <param name="cancellationToken">The cancelation token that can be used for detecting when the engine request for the task to be canceled.</param>
        /// <returns>Returns the asynchronous task.</returns>
        public abstract Task ExecuteStartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Implement this method to resume the execution of the play mode after a domain reload.
        /// </summary>
        /// <param name="cancellationToken">The cancelation token that can be used for detecting when the engine request for the task to be canceled.</param>
        public virtual void ExecuteResume(CancellationToken cancellationToken) { }

        /// <summary>
        /// Implement this method to exit the play mode. The method will be called when the play mode is requested to stop.
        /// </summary>
        public abstract void ExecuteStop();

        /// <summary>
        /// Implement this method to create additional UI elements for the play mode top bar when this play mode config is active.
        /// </summary>
        /// <returns>
        /// Returns the visual element that will be added to the top bar.
        /// </returns>
        public virtual VisualElement CreateTopbarUI() => null;

        /// <summary>
        /// Provide an icon to be shown in the playmode dropdown.
        /// </summary>
        /// <returns>
        /// Returns an icon that will show next to the configuration.
        /// </returns>
        public virtual Texture2D Icon => EditorGUIUtility.FindTexture("UnityLogo");

        public virtual bool IsConfigurationValid(out string reasonForInvalidConfiguration)
        {
            reasonForInvalidConfiguration = null;
            return true;
        }

        /// <summary>
        /// Implement this method to be notified of when the User selects this Play Mode Configuration.
        /// </summary>
        public virtual void OnConfigSelected() { }

        /// <summary>
        /// Implement this method to be notified of when the User de-selects this Play Mode Configuration.
        /// </summary>
        public virtual void OnConfigDeselected() { }

        /// <summary>
        /// Override this method to manage the de-selection behavior of the Play Mode Configuration.
        /// This is particularly useful for situations where you need to 'lock' an active configuration
        /// to ensure stability or prevent unintended changes while the current configuration is in use.
        /// </summary>
        /// <returns>
        /// Returns false if the user is allowed to switch away from the current configuration.
        /// </returns>
        public virtual bool WantsToDeselectConfiguration() => true;
    }
}
