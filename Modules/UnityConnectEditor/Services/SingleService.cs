// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    /// <summary>
    /// Base class for individual services.
    /// Used to add a service to the services window and access the services configurations.
    /// Extensions should be sealed singletons to avoid duplicate services clashes
    /// </summary>
    internal abstract class SingleService
    {
        const string k_EnableServiceFailedMessage = "Enabling service {0} failed. Message: {1}";
        const string k_DisableServiceFailedMessage = "Disabling service {0} failed. Message: {1}";
        const string k_AfterEnableExceptionMessage = "Exception occurred during service {0} after enable event and was not handled. Message: {1}";
        const string k_AfterDisableExceptionMessage = "Exception occurred during service {0} after disable event and was not handled. Message: {1}";
        const string k_NoUnityProjectIdMessage = "The {0} service cannot be enabled if the project doesn't have a Unity project ID. " +
            "Please go to the {0} service project settings and either create a new project ID or reuse an existing one.";
        const string k_NoCoppaComplianceMessage = "The {0} service cannot be enabled if the project doesn't have a specified COPPA compliance setting. " +
            "Please go to the {0} service project settings and configure COPPA compliance.";

        /// <summary>
        /// The name of the service, only used internally, not displayed
        /// </summary>
        public abstract string name { get; }

        /// <summary>
        /// This title will show up in the services window
        /// </summary>
        public abstract string title { get; }

        /// <summary>
        /// This description shows up under the title. There is a lot of room but keep this marketing-like
        /// </summary>
        public abstract string description { get; }

        /// <summary>
        /// The hard-disk path toward the service icon which is displayed beside the title.
        /// Should be something like: @"StyleSheets\ServicesWindow\Images\serviceIconAds.png"
        /// </summary>
        public abstract string pathTowardIcon { get; }

        /// <summary>
        /// Set this to true if you want to allow toggling the service on and off
        /// </summary>
        public abstract bool displayToggle { get; }

        /// <summary>
        /// Path to direct link the project settings to configure this service.
        /// Should be something like: "Project/Services/General"
        /// </summary>
        public abstract string projectSettingsPath { get; }

        public abstract string settingsProviderClassName { get; }

        /// <summary>
        /// Sets the topic of notification for this service
        /// </summary>
        public abstract Notification.Topic notificationTopic { get; }

        public virtual bool isPackage { get; }
        public abstract string packageName { get; }

        public virtual string serviceFlagName { get; }

        /// <summary>
        /// True if a project needs to be bound to a Unity project ID before the service can be enabled.
        /// (Should probably always be true)
        /// </summary>
        public virtual bool requiresBoundProject => true;

        public virtual bool shouldEnableOnProjectCreation => false;
        public virtual bool shouldSyncOnProjectRebind => false;

        /// <summary>
        /// True if a project needs to have a Coppa compliance set before the service can be enabled.
        /// Default value is alive. Override to set it true.
        /// </summary>
        public virtual bool requiresCoppaCompliance => false;

        public SingleService()
        {
            isPackage = false;
        }

        public virtual bool IsServiceEnabled()
        {
            return PlayerSettings.GetCloudServiceEnabled(name);
        }

        internal void EnableService(bool enable, bool shouldUpdateApiFlag = true)
        {
            //Last minute check for services dependencies
            ServicesRepository.InitializeServicesHandlers();
            HandleProjectLink(enable, shouldUpdateApiFlag);
        }

        void HandleProjectLink(bool enable, bool shouldUpdateApiFlag)
        {
            if (enable && requiresBoundProject && !UnityConnect.instance.projectInfo.projectBound)
            {
                NotificationManager.instance.Publish(notificationTopic, Notification.Severity.Warning, L10n.Tr(string.Format(k_NoUnityProjectIdMessage, name)));
                SettingsService.OpenProjectSettings(GeneralProjectSettings.generalProjectSettingsPath);
            }
            else
            {
                HandleCoppaCompliance(enable, shouldUpdateApiFlag);
            }
        }

        void HandleCoppaCompliance(bool enable, bool shouldUpdateApiFlag)
        {
            if (enable && requiresCoppaCompliance && UnityConnect.instance.projectInfo.COPPA == COPPACompliance.COPPAUndefined)
            {
                NotificationManager.instance.Publish(notificationTopic, Notification.Severity.Warning, L10n.Tr(string.Format(k_NoCoppaComplianceMessage, name)));
                SettingsService.OpenProjectSettings(GeneralProjectSettings.generalProjectSettingsPath);
            }
            else
            {
                HandleServiceEnabling(enable, shouldUpdateApiFlag);
            }
        }

        void HandleServiceEnabling(bool enable, bool shouldUpdateApiFlag)
        {
            var beforeArgs = new ServiceBeforeEventArgs();
            OnServiceBeforeEvent(enable, beforeArgs);
            if (beforeArgs.operationCancelled)
            {
                return;
            }
            try
            {
                InternalEnableService(enable, shouldUpdateApiFlag);
            }
            catch (Exception ex)
            {
                NotificationManager.instance.Publish(notificationTopic, Notification.Severity.Error,
                    enable ? string.Format(k_EnableServiceFailedMessage, title, ex.Message)
                    : string.Format(k_DisableServiceFailedMessage, title, ex.Message));
                return;
            }

            // The ServicesEditorWindow instance can be null if the window is closed.
            var servicesEditorWindow = ServicesEditorWindow.instance;
            if (servicesEditorWindow != null)
            {
                servicesEditorWindow.SetServiceStatusValue(name, enable);
            }

            try
            {
                OnServiceAfterEvent(enable, new EventArgs());
            }
            catch (Exception ex)
            {
                //Here report exception and swallow it: at this point the service was toggled successfully, but actors that acted afterward may have had issue.
                //It is their responsibility to handle things correctly
                NotificationManager.instance.Publish(notificationTopic, Notification.Severity.Error,
                    enable ? string.Format(k_AfterEnableExceptionMessage, title, ex.Message)
                    : string.Format(k_AfterDisableExceptionMessage, title, ex.Message));
            }
        }

        internal virtual void InitializeServiceEventHandlers()
        {
            //Do nothing by default
            //Here you should add your event handlers for beforeServiceEnable, AfterServiceEnable, BeforeServiceDisable and AfterServiceDisable toward other services
            //Do not setup handlers for events launched from other services in the constructor, as there is no guarantee to service you want to register to is available at that point.
        }

        void OnServiceBeforeEvent(bool enabled, ServiceBeforeEventArgs beforeArgs)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribe
            // immediately after the null check and before the event is raised.
            var handler = enabled ? serviceBeforeEnableEvent : serviceBeforeDisableEvent;

            // Event will be null if there are no subscribers
            handler?.Invoke(this, beforeArgs);
        }

        void OnServiceAfterEvent(bool enabled, EventArgs afterArgs)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribe
            // immediately after the null check and before the event is raised.
            var handler = enabled ? serviceAfterEnableEvent : serviceAfterDisableEvent;

            // Event will be null if there are no subscribers
            handler?.Invoke(this, afterArgs);
        }

        protected virtual void InternalEnableService(bool enable, bool shouldUpdateApiFlag)
        {
            if (shouldUpdateApiFlag && !string.IsNullOrEmpty(serviceFlagName))
            {
                UpdateServiceFlag(enable);
            }

            PlayerSettings.SetCloudServiceEnabled(name, enable);
        }

        public void UpdateServiceFlag(bool enable)
        {
            if (!string.IsNullOrEmpty(UnityConnect.instance.projectInfo.projectId))
            {
                ServicesConfiguration.instance.RequestCurrentProjectServiceFlagsApiUrl(currentProjectServiceFlagsApiUrl =>
                {
                    var payload = "{\"service_flags\":{\"" + serviceFlagName + "\":" + enable.ToString().ToLower() + "}}";
                    var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                    var serviceFlagRequest = new UnityWebRequest(currentProjectServiceFlagsApiUrl,
                        UnityWebRequest.kHttpVerbPUT) { downloadHandler = new DownloadHandlerBuffer(), uploadHandler = uploadHandler };
                    serviceFlagRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                    serviceFlagRequest.SetRequestHeader("Content-Type", "application/json;charset=UTF-8");
                    serviceFlagRequest.SendWebRequest();
                });
            }
        }

        /// <summary>
        /// A SingleService will launch this event before attempting to enable itself.
        /// Anything can listen in to take action before this occurs and abort the operation
        /// using the supplied ServiceBeforeEventArgs if required.
        /// </summary>
        internal event EventHandler<ServiceBeforeEventArgs> serviceBeforeEnableEvent;

        /// <summary>
        /// A SingleService will launch this event after successfully enabling itself.
        /// Anything can listen in to take action once this is done but nothing can rollback
        /// the service enabling at this point.
        /// </summary>
        internal event EventHandler<EventArgs> serviceAfterEnableEvent;

        /// <summary>
        /// A SingleService will launch this event before attempting to disable itself.
        /// Anything can listen in to take action before this occurs and abort the operation
        /// using the supplied ServiceBeforeEventArgs if required.
        /// </summary>
        internal event EventHandler<ServiceBeforeEventArgs> serviceBeforeDisableEvent;

        /// <summary>
        /// A SingleService will launch this event after successfully disabling itself.
        /// Anything can listen in to take action once this is done but nothing can rollback
        /// the service disabling at this point.
        /// </summary>
        internal event EventHandler<EventArgs> serviceAfterDisableEvent;
    }

    /// <summary>
    /// Arguments received on a BeforeEvent handler.
    /// Also allows to consider the success of the operation through the cancelOperation property: operation might have been cancelled by another handler.
    /// Can cancel the operation by calling the CancelOperation method.
    /// If you do cancel the operation, you should use the NotificationManager to explain to the user why the operation was cancelled
    /// </summary>
    internal class ServiceBeforeEventArgs : EventArgs
    {
        /// <summary>
        /// Verifies if the operation's state.  Might have been already cancelled by another Before handler.
        /// In which case, consider if not doing your own validations is the best course of action.
        /// </summary>
        public bool operationCancelled { get; private set; }

        /// <summary>
        /// Cancels the current operation, preventing the operation from happening and not calling After handlers
        /// If you do cancel the operation, you should use the NotificationManager to explain to the user why the operation was cancelled
        /// </summary>
        public void CancelOperation()
        {
            operationCancelled = true;
        }
    }
}
