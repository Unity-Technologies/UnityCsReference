// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [InitializeOnLoad]
    static class VirtualProjectWorkflow
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

        public static event Action<bool> OnDisabled;

        public static bool IsInitialized { get; private set; }
        public static bool IsMainEditor { get; private set; }

        public static readonly string k_MppmPackageJson = "Library/VP/MPPMVersion.json";

        private static string s_EditorVersion;
        private static string s_EditorChangeset;
        private static string s_PackageVersion;



        static Action<bool> s_PendingOnInitializedCallbacks;

        static WorkflowMainEditorContext s_WorkflowMainEditorContext;
        static WorkflowCloneContext s_WorkflowCloneContext;

        [InitializeOnLoadMethod]
        private static void ValidateOnPackageChanged()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            Events.registeredPackages += args =>
            {
                // If users decide to upgrade the package with clones open this could cause unexpected behaviour
                // We however are unable to warn them about this as when this event occurs MultiplayerPlaymode
                // has not been initiated yet so we cannot see any open clones until after the deletion of the folder occurs
                ValidateVersionsChange();
            };
        }


        static VirtualProjectWorkflow()
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
                    PackageVersion = s_PackageVersion,
                    EditorVersion = s_EditorVersion,
                    EditorChangeset = s_EditorChangeset,
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
