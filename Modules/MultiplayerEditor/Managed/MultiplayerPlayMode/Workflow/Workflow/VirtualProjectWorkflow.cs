// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.PlayMode.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static partial class VirtualProjectWorkflow
    {
        public static event Action<bool> OnInitialized
        {
            add
            {
                if (IsInitialized)
                {
                    value?.Invoke(IsMainEditor);
                    return;
                }

                s_PendingOnInitializedCallbacks += value;
            }
            remove
            {
                if (IsInitialized)
                {
                    return;
                }

                s_PendingOnInitializedCallbacks -= value;
            }
        }

        [AutoStaticsCleanupOnCodeReload] // static event; stale handlers after reload pin old ALC
        public static event Action<bool> OnDisabled;

        [AutoStaticsCleanupOnCodeReload] // init gate; must reset so initialization re-runs after reload
        // Re-established on every code load by Initialize() below, so the reset gate makes the workflow
        // initialize again rather than staying half-configured.
        [IgnoreForUAL0015("Init gate re-run on every code load by Initialize()")]
        public static bool IsInitialized { get; private set; }
        [AutoStaticsCleanupOnCodeReload] // set during init; must reset so it's re-evaluated after reload
        // Re-evaluated on every code load by the initialization Initialize() drives.
        [IgnoreForUAL0015("Re-evaluated on every code load by the workflow initialization")]
        public static bool IsMainEditor { get; private set; }

        public static readonly string k_MppmPackageJson = "Library/VP/MPPMVersion.json";

        internal const string k_ReactivateAfterPackageChangeKey = "vp_ReactivatePlayersAfterPackageChange";

        [AutoStaticsCleanupOnCodeReload] // version info read from file; must re-read after reload
        // Every read path goes through HasVersionChanged, which calls ReadVersionInfo first, so the
        // version strings are re-read from ProjectVersion.txt before they are compared.
        [IgnoreForUAL0015("Re-read from ProjectVersion.txt by ReadVersionInfo before every comparison")]
        private static string s_EditorVersion;
        [AutoStaticsCleanupOnCodeReload]
        // Re-read from ProjectVersion.txt by ReadVersionInfo at the start of every HasVersionChanged.
        [IgnoreForUAL0015("Re-read from ProjectVersion.txt by ReadVersionInfo before every comparison")]
        private static string s_EditorChangeset;
        [AutoStaticsCleanupOnCodeReload]
        // Re-read from the package manager by ReadVersionInfo at the start of every HasVersionChanged.
        [IgnoreForUAL0015("Re-read from the package manager by ReadVersionInfo before every comparison")]
        private static string s_PackageVersion;



        [AutoStaticsCleanupOnCodeReload] // pending callbacks delegate; stale handlers after reload pin old ALC
        // Queue of not-yet-invoked initialization callbacks. Initialize() runs on every code load and
        // re-subscribes, so the drained queue refills for the new scope.
        [IgnoreForUAL0015("Pending-callback queue refilled by Initialize() on the next code load")]
        static Action<bool> s_PendingOnInitializedCallbacks;

        [AutoStaticsCleanupOnCodeReload] // workflow context; stale after reload
        // Recreated by InitializeMPPMContexts, which Initialize() drives on every code load.
        [IgnoreForUAL0015("Workflow context recreated on every code load by InitializeMPPMContexts")]
        static WorkflowMainEditorContext s_WorkflowMainEditorContext;
        [AutoStaticsCleanupOnCodeReload] // workflow context; stale after reload
        // Recreated by InitializeMPPMContexts, which Initialize() drives on every code load.
        [IgnoreForUAL0015("Workflow context recreated on every code load by InitializeMPPMContexts")]
        static WorkflowCloneContext s_WorkflowCloneContext;

        [InitializeOnLoadMethod]
        private static void ValidateOnPackageChanged()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            // Deactivate clones while the registration changes, and bring them back after.
            Events.registeringPackages += _ => DeactivateClonesForPackageChange();

            Events.registeredPackages += args =>
            {
                // If users decide to upgrade the package with clones open this could cause unexpected behaviour
                // We however are unable to warn them about this as when this event occurs MultiplayerPlaymode
                // has not been initiated yet so we cannot see any open clones until after the deletion of the folder occurs
                ValidateVersionsChange();
                ReactivateClonesAfterPackageChange();
            };
        }

        internal static void DeactivateClonesForPackageChange()
        {
            if (VirtualProjectsEditor.IsClone || MultiplayerPlaymode.Players == null)
                return;

            var reactivate = new List<string>();
            for (var i = 0; i < MultiplayerPlaymode.Players.Length; i++)
            {
                var player = MultiplayerPlaymode.Players[i];
                if (player.Type != PlayerType.Clone)
                    continue;

                if (player.PlayerState != PlayerState.Launched && player.PlayerState != PlayerState.Launching)
                    continue;

                if (!player.Deactivate(out var deactivationError))
                {
                    MppmLog.Warning($"Could not close {player.Name} for a package change: {deactivationError}");
                    continue;
                }

                reactivate.Add(i.ToString());
                MppmLog.Debug($"Closing {player.Name} for a package change; it will be reopened afterwards");
            }

            SessionState.SetString(k_ReactivateAfterPackageChangeKey, string.Join(",", reactivate));
        }

        internal static void ReactivateClonesAfterPackageChange()
        {
            if (VirtualProjectsEditor.IsClone || MultiplayerPlaymode.Players == null)
                return;

            var reactivate = SessionState.GetString(k_ReactivateAfterPackageChangeKey, string.Empty);
            if (string.IsNullOrEmpty(reactivate))
                return;

            SessionState.EraseString(k_ReactivateAfterPackageChangeKey);

            // If MPPM package removed, then leave the clones closed then
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
            {
                MppmLog.Debug("Not reopening virtual players: the package change disabled Multiplayer Play Mode");
                return;
            }

            foreach (var index in reactivate.Split(','))
            {
                if (!int.TryParse(index, out var playerIndex) || playerIndex >= MultiplayerPlaymode.Players.Length)
                    continue;

                var player = MultiplayerPlaymode.Players[playerIndex];
                if (player.Activate(out var activationError))
                {
                    MppmLog.Debug($"Reopened {player.Name} after a package change");
                }
                else
                {
                    MppmLog.Warning($"Could not reopen {player.Name} after a package change: {activationError}. Activate it again from the Play Mode Scenarios.");
                }
            }
        }


        // The state set up here (the workflow contexts and SystemDataStore's file-system delegates) is
        // cleared on code reload, so it has to be re-established on every load. A static constructor
        // would only run once per domain, leaving SystemDataStore.s_FileSystemDelegates null while every
        // read path dereferences it unguarded.
        [OnCodeLoaded]
        static void Initialize()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            ValidateVersionsChange();
            DuplicateKeyChecker.Clear();
            EditorContexts.OnInitialized += () =>
            {
                Debug.Assert(!CommandLineParameters.ReadNoDownChainDependencies());
                SystemDataStore.Initialize(FileSystem.Delegates, ParsingSystem.Delegates);
                var systemDataStore = VirtualProjectsEditor.IsClone
                    ? SystemDataStore.GetClone()
                    : SystemDataStore.GetMain();

                if (systemDataStore.GetIsMppmActive())
                {
                    InitializeMPPMContexts();
                }
            };
        }


        private static bool HasVersionChanged()
        {
            ReadVersionInfo();

            if (!File.Exists(k_MppmPackageJson))
            {
                return true;
            }

            string versionJson = File.ReadAllText(k_MppmPackageJson);
            VersionInfo versionInfoRead = JsonUtility.FromJson<VersionInfo>(versionJson);

            if (string.IsNullOrEmpty(s_PackageVersion))
                return false;

            if (versionInfoRead.EditorVersion != s_EditorVersion || versionInfoRead.EditorChangeset != s_EditorChangeset || versionInfoRead.PackageVersion != s_PackageVersion)
            {
                return true;
            }

            return false;
        }

        private static void ValidateVersionsChange()
        {
            if (HasVersionChanged())
            {
                ClearVirtualProjectFolder();
                VersionInfo versionInfo = new VersionInfo
                {
#pragma warning disable UAL0018 // versionInfo is a local descriptor serialized to JSON on the next line and discarded; the version strings do not outlive this call
                    PackageVersion = s_PackageVersion,
                    EditorVersion = s_EditorVersion,
                    EditorChangeset = s_EditorChangeset,
#pragma warning restore UAL0018
                };
                string json = JsonUtility.ToJson(versionInfo, prettyPrint: true);

                File.WriteAllText(k_MppmPackageJson, json);
            }
        }

        internal static void ClearVirtualProjectFolder()
        {
            if (Directory.Exists(Paths.CurrentProjectVirtualProjectsFolder))
            {
                FileSystem.Delegates.DeleteDirectoryFunc(Paths.CurrentProjectVirtualProjectsFolder);
            }

            FileSystem.Delegates.CreateDirectoryFunc(Paths.CurrentProjectVirtualProjectsFolder);
        }

        private static void ReadVersionInfo()
        {
            // Read the current version of the editor and change set from project version file
            var path = "ProjectSettings/ProjectVersion.txt";
            string[] lines = Array.Empty<string>();

            if (File.Exists(path))
                lines = File.ReadAllLines(path);



            foreach (string line in lines)
            {
                if (line.StartsWith("m_EditorVersion:"))
                {
                    s_EditorVersion = line.Split(':')[1].Trim();
                }
                else if (line.StartsWith("m_EditorVersionWithRevision:"))
                {
                    string versionWithRevision = line.Split(':')[1].Trim();
                    s_EditorChangeset = "";
                    if (versionWithRevision.Contains(" "))
                    {
                        s_EditorChangeset = versionWithRevision.Split(' ')[1].Trim('(', ')');
                    }
                }
            }

            // Read the current version of the package from package manager
            if (UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ProjectDataStore).Assembly) != null)
                s_PackageVersion = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ProjectDataStore).Assembly).version;

        }

        [Serializable]
        class VersionInfo
        {
            public string PackageVersion;
            public string EditorVersion;
            public string EditorChangeset;
        }

        // Should only being called by MultiplayerPlayModeSettings
        public static void UpdateMPPMRuntimeState(bool isMppmActive)
        {
            if (!IsInitialized && isMppmActive)
            {
                InitializeMPPMContexts();
            }
            else if (IsInitialized && !isMppmActive)
            {
                DisableMPPMContexts();
            }
        }

        static void InitializeMPPMContexts()
        {
            if (VirtualProjectsEditor.IsClone)
            {
                s_WorkflowCloneContext = new WorkflowCloneContext(EditorContexts.CloneContext);
            }
            else
            {
                s_WorkflowMainEditorContext = new WorkflowMainEditorContext(EditorContexts.MainEditorContext);
            }

            IsInitialized = true;
            IsMainEditor = s_WorkflowMainEditorContext != null;
            s_PendingOnInitializedCallbacks?.Invoke(IsMainEditor);
            s_PendingOnInitializedCallbacks = null;
        }

        static void DisableMPPMContexts()
        {
            OnDisabled?.Invoke(IsMainEditor);
            IsInitialized = false;
            IsMainEditor = default;
            s_WorkflowCloneContext = null;
            s_WorkflowMainEditorContext = null;
            DuplicateKeyChecker.Clear();
        }

        internal static WorkflowMainEditorContext WorkflowMainEditorContext
        {
            get
            {
                if (VirtualProjectsEditor.IsClone)
                {
                    throw new NotSupportedException("Main Editor functionality cannot be accessed from clones.");
                }

                return s_WorkflowMainEditorContext;
            }
        }

        internal static WorkflowCloneContext WorkflowCloneContext
        {
            get
            {
                if (!VirtualProjectsEditor.IsClone)
                {
                    throw new NotSupportedException("Clone functionality cannot be accessed from the main Editor.");
                }

                return s_WorkflowCloneContext;
            }
        }
    }
}
