// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Events;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Build profile initialization metadata. Serialized as part of the <see cref="BuildProfileContext"/>
    /// when a build profile is created. Used to track initialization state across domain reloads.
    /// </summary>
    [Serializable]
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal class BuildProfileInitialization
    {
        public const int kPreconfiguredSettingsVariantNotSet = -2;

        public enum State
        {
            /// <summary>
            /// Initial state of a created build profile.
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// End state, when set the build profile is ready for use and this object
            /// is no longer needed.
            /// </summary>
            Ready = 1,

            /// <summary>
            /// Build profile was created with packages to install,
            /// begins and polls package installation.
            /// </summary>
            InstallingPackages,

            /// <summary>
            /// Package installation failed.
            /// </summary>
            InstallingError,

            /// <summary>
            /// Derived platform requires GUID activation via EditorPrefs before
            /// the platform module can be used. Waits for the domain reload
            /// triggered by <see cref="BuildProfileModuleUtil.RequestScriptCompilation"/>
            /// before proceeding to <see cref="InstallingPackages"/> or <see cref="Ready"/>.
            /// </summary>
            AwaitingDomainReload,

            /// <summary>
            /// State where any required extensions (e.g. build profile extension,
            /// SDK platform extension) is pending.
            /// </summary>
            AwaitingExtension,
        }

        /// <summary>
        /// Asset database GUID for the build profile this opbject data is for.
        /// </summary>
        [SerializeField]
        public string assetGUID;

        /// <summary>
        /// Platform preconfigured settings variant to apply after package installation.
        /// </summary>
        [SerializeField]
        public int preconfiguredSettingsVariant = kPreconfiguredSettingsVariantNotSet;

        /// <summary>
        /// Package installation progress tracker. Null if no packages to install.
        /// </summary>
        [SerializeField]
        public BuildProfilePackageAddInfo packageAddInfo;

        /// <summary>
        /// Optional callback invoked when the build profile is <see cref="State.Ready"/>.
        /// </summary>
        [SerializeField]
        public UnityEvent<BuildProfile> onBuildProfileCreateCompletion;

        /// <summary>
        /// Current build profile initialization state.
        /// </summary>
        [SerializeField]
        public State state = State.Unknown;

        /// <summary>
        /// Set to true when entering <see cref="State.AwaitingDomainReload"/> and automatically
        /// cleared by the domain reload (non-serialized). Guards against advancing the state machine
        /// on the synchronous OnState call that fires before the reload has occurred.
        /// </summary>
        [NonSerialized]
        bool m_ReloadPending;

        /// <summary>
        /// Set to true when entering <see cref="State.AwaitingExtension"/> if the profile is expecting
        /// extensions to be loaded in the current editor session. Cleared on domain reload (non-serialized).
        /// Determines whether the bootstrap spinner should be shown until the first post-install domain reload completes.
        /// </summary>
        [NonSerialized]
        bool m_EnteredAwaitingExtensionThisSession;

        /// <summary>
        /// Factory method to create a <see cref="BuildProfileInitialization"/> instance.
        /// </summary>
        public static BuildProfileInitialization Create(
            BuildProfile profile,
            string profileGUID,
            string[] packagesToAdd,
            int preconfiguredSettingsVariant,
            UnityAction<BuildProfile> onProfileReady)
        {
            var init = new BuildProfileInitialization()
            {
                assetGUID = profileGUID,
                preconfiguredSettingsVariant = preconfiguredSettingsVariant,
            };

            if (packagesToAdd.Length != 0)
            {
                init.packageAddInfo = new BuildProfilePackageAddInfo()
                {
                    packagesToAdd = packagesToAdd,
                };
            }

            if (onProfileReady != null)
            {
                init.onBuildProfileCreateCompletion = new UnityEvent<BuildProfile>();
                init.onBuildProfileCreateCompletion.AddPersistentListener(onProfileReady, UnityEventCallState.EditorAndRuntime);
            }

            return init;
        }

        /// <summary>
        /// Returns true if the initialization work is finished.
        /// </summary>
        public bool IsDone() => state == State.Ready;

        /// <summary>
        /// Determines if the bootstrap spinner view should be shown for this profile.
        /// </summary>
        public bool ShouldShowBootstrapView()
        {
            if (state == State.Ready)
                return false;

            if (state == State.AwaitingExtension)
            {
                if (packageAddInfo == null)
                    return false;

                return m_EnteredAwaitingExtensionThisSession;
            }

            return true;
        }

        /// <summary>
        /// Checks if the build profile's required extensions (e.g. build profile extension,
        /// SDK platform extension) are available.
        /// </summary>
        bool AreRequiredExtensionsAvailable(BuildProfile profile)
        {
            var extensionGuid = profile.isMultiTarget ? profile.selectedPlatformGuid : profile.platformGuid;
            if (BuildProfileModuleUtil.GetBuildProfileExtension(extensionGuid) == null)
                return false;

            if (profile.isMultiTarget && !BuildTargetDiscovery.TryGetSDKPlatformExtension(profile.platformGuid, out _))
                return false;

            return true;
        }

        /// <summary>
        /// Updates initialization work based on the current state. Expects
        /// to be called on <see cref="BuildProfile.OnEnable"/> and during
        /// async work completion.
        /// </summary>
        /// <param name="profile">Profile instance as referenced by <see cref="assetGUID"/>.</param>
        public State OnState(BuildProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            switch (state)
            {
                case State.Unknown:
                {
                    if (RequiresDerivedPlatformActivation(profile))
                        NextState(State.AwaitingDomainReload, profile);
                    else if (packageAddInfo != null)
                        NextState(State.InstallingPackages, profile);
                    else if (!AreRequiredExtensionsAvailable(profile))
                        NextState(State.AwaitingExtension, profile);
                    else
                        NextState(State.Ready, profile);
                    break;
                }
                case State.AwaitingDomainReload:
                    if (!m_ReloadPending)
                    {
                        if (packageAddInfo != null)
                            NextState(State.InstallingPackages, profile);
                        else if (!AreRequiredExtensionsAvailable(profile))
                            NextState(State.AwaitingExtension, profile);
                        else
                            NextState(State.Ready, profile);
                    }
                    break;
                case State.InstallingPackages:
                    if (packageAddInfo != null)
                    {
                        AddPackageInstallationCallbacks(profile);
                        if (packageAddInfo.IsPackageRequestDone())
                        {
                            // Expect no packages left to install on profile enable.
                            // This ensures that any package compilation has finished.
                            packageAddInfo.OnPackageAddComplete?.Invoke();
                            NextState(!AreRequiredExtensionsAvailable(profile) ? State.AwaitingExtension : State.Ready, profile);
                        }
                        else
                            packageAddInfo.RequestPackageInstallation();
                    }
                    else
                    {
                        NextState(State.InstallingError, profile);
                    }
                    break;
                case State.InstallingError:
                    Debug.LogError(JsonUtility.ToJson(this.packageAddInfo?.GetPackageAddProgressInfo()));
                    if (!AreRequiredExtensionsAvailable(profile))
                        NextState(State.AwaitingExtension, profile);
                    else
                        NextState(State.Ready, profile);
                    break;
                case State.AwaitingExtension:
                    if (AreRequiredExtensionsAvailable(profile))
                        NextState(State.Ready, profile);
                    break;
                case State.Ready:
                    // No-op
                    break;
                default:
                    Debug.LogWarning($"Unhandled build profile initialization state: {state} - {assetGUID}");
                    break;
            }

            return state;
        }

        /// <summary>
        /// Transitions to the next state.
        /// </summary>
        void NextState(State next, BuildProfile profile)
        {
            if (next == state)
                return;

            var previous = state;
            state = next;
            OnEnterState(next, previous, profile);
            OnState(profile);
        }

        /// <summary>
        /// Returns true when the profile's platform is a derived platform that has not yet
        /// been activated via its EditorPrefs key. Activation must happen before the platform
        /// module can load, which requires a domain reload.
        /// </summary>
        static bool RequiresDerivedPlatformActivation(BuildProfile profile)
        {
            var platformId = profile.platformGuid;
            if (!BuildTargetDiscovery.BuildPlatformIsDerivedPlatform(platformId))
                return false;

            return EditorPrefs.GetInt(platformId.ToString(), 0) == 0;
        }

        /// <summary>
        /// Perform one-time actions upon entering a new state.
        /// </summary>
        void OnEnterState(State state, State previousState, BuildProfile profile)
        {
            switch (state)
            {
                case State.AwaitingDomainReload:
                {
                    m_ReloadPending = true;
                    var platformId = profile.platformGuid;
                    EditorPrefs.SetInt(platformId.ToString(), 1);
                    EditorPrefs.SetString("LastEnabledPlatformGUID", platformId.ToString());
                    BuildProfileModuleUtil.RequestScriptCompilation(null);
                    break;
                }
                case State.AwaitingExtension:
                {
                    var extensionGuid = profile.isMultiTarget ? profile.selectedPlatformGuid : profile.platformGuid;
                    m_EnteredAwaitingExtensionThisSession = previousState != State.InstallingError
                        && Modules.ModuleManager.IsPlatformSupportLoadedByGuid(extensionGuid);
                    break;
                }
                case State.Ready:
                {
                    if (preconfiguredSettingsVariant != kPreconfiguredSettingsVariantNotSet)
                    {
                        profile.NotifyBuildProfileExtensionOfCreation(preconfiguredSettingsVariant);
                    }
                    onBuildProfileCreateCompletion?.Invoke(profile);
                    Cleanup();
                    break;
                }
            }
        }

        /// <summary>
        /// Sets up the callbacks for package installation progress and completion.
        /// On domain reload these callbacks should be re-attached to ensure proper notification.
        /// </summary>
        /// <param name="profile">Profile instance as referenced by <see cref="assetGUID"/>.</param>
        void AddPackageInstallationCallbacks(BuildProfile profile)
        {
            var packageAddInfo = this.packageAddInfo;
            packageAddInfo.OnPackageAddProgress = () =>
            {
                profile.OnPackageAddProgress?.Invoke();
            };
            packageAddInfo.OnPackageAddComplete = () =>
            {
                profile.OnPackageAddComplete?.Invoke();
            };
        }

        /// <summary>
        /// Cleanup internal event handlers and serialized references.
        /// </summary>
        void Cleanup()
        {
            onBuildProfileCreateCompletion?.RemoveAllListeners();
            onBuildProfileCreateCompletion = null;

            packageAddInfo?.Cleanup();
            packageAddInfo = null;
        }

        bool IsCallbackPersistable(UnityAction callback) => callback.Method.IsStatic || callback.Target is UnityEngine.Object;
    }
}
