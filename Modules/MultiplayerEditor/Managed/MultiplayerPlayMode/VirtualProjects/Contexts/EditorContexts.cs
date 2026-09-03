// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: HeadlessRuntime not yet converted
#pragma warning disable UAL0010,UAL0011,UAL0012,UAL0013,UAL0014 // AutoStaticsCleanup: HeadlessRuntime not yet converted
using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [InitializeOnLoad]
    static partial class EditorContexts
    {
        public static event Action OnInitialized
        {
            add
            {
                if (IsInitialized)
                {
                    value?.Invoke();
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

        [AutoStaticsCleanupOnCodeReload] // pending callbacks delegate; stale handlers after reload pin old ALC
        static Action s_PendingOnInitializedCallbacks;

        [AutoStaticsCleanupOnCodeReload] // init gate; must reset to false so SendReadyEvent re-runs after reload
        public static bool IsInitialized { get; set; }

        [AutoStaticsCleanupOnCodeReload] // context object; stale after reload
        static MainEditorContext s_MainEditorContext;
        [AutoStaticsCleanupOnCodeReload] // context object; stale after reload
        static CloneContext s_CloneContext;

        [OnCodeLoaded]
        static void Initialize()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            SendReadyEvent();
        }

        static void SendReadyEvent()
        {
            if (VirtualProjectsEditor.IsClone)
            {
                s_CloneContext = new CloneContext();
            }
            else
            {
                if (!CommandLineParameters.IsUMPE())
                {
                    s_MainEditorContext = new MainEditorContext();
                }
            }

            if (!CommandLineParameters.ReadNoDownChainDependencies() && !CommandLineParameters.IsUMPE())
            {
                IsInitialized = true;
                s_PendingOnInitializedCallbacks?.Invoke();
                s_PendingOnInitializedCallbacks = null;
            }
        }

        public static MainEditorContext MainEditorContext
        {
            get
            {
                if (VirtualProjectsEditor.IsClone)
                {
                    throw new NotSupportedException("Main Editor functionality cannot be accessed from Clones.");
                }

                return s_MainEditorContext;
            }
        }

        public static CloneContext CloneContext
        {
            get
            {
                if (!VirtualProjectsEditor.IsClone && !CommandLineParameters.IsUMPE())
                {
                    throw new NotSupportedException("Clone functionality cannot be accessed from the Main Editor.");
                }

                return s_CloneContext;
            }
        }
    }
}
#pragma warning restore UAL0010,UAL0011,UAL0012,UAL0013,UAL0014
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
