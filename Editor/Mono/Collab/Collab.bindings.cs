// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Connect;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Collaboration
{
    [NativeHeader("Editor/Src/Collab/CollabInfo.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct CollabInfo
    {
        public bool ready { get { return m_Ready; } }
        public bool update { get { return m_Update; } }
        public bool publish { get { return m_Publish; } }
        public bool inProgress { get { return m_InProgress; } }
        public bool maintenance { get { return m_Maintenance; } }
        public bool conflict { get { return m_Conflict; } }
        public bool refresh { get { return m_Refresh; } }
        public bool seat { get { return m_HasSeat; } }
        public string tip { get { return m_Tip; } }

        public bool Equals(CollabInfo other)
        {
            return m_Update == other.m_Update &&
                m_Publish == other.m_Publish &&
                m_InProgress == other.m_InProgress &&
                m_Maintenance == other.m_Maintenance &&
                m_Conflict == other.m_Conflict &&
                m_Refresh == other.m_Refresh &&
                m_HasSeat == other.m_HasSeat &&
                m_Ready == other.m_Ready &&
                string.Equals(m_Tip, other.m_Tip);
        }

        bool m_Update;
        bool m_Publish;
        bool m_InProgress;
        bool m_Maintenance;
        bool m_Conflict;
        bool m_Refresh;
        bool m_HasSeat;
        bool m_Ready;
        string m_Tip;
    }

    [NativeHeader("Editor/Src/Collab/Collab.h")]
    [NativeHeader("Editor/Src/Collab/Collab.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [StaticAccessor("Collab::Get()", StaticAccessorType.Arrow)]
    partial class Collab
    {
        [NativeMethod("Get")]
        static extern IntPtr GetNativeCollab();

        public extern CollabInfo collabInfo { get; }

        public static extern int GetRevisionsData(
            bool withChanges, int startIndex, int numRevisions);

        public static extern RevisionsData PopulateRevisionsData(IntPtr nativeData);

        public extern void SetSeat(bool value);

        public extern string GetProjectGUID();

        public extern bool ShouldDoInitialCommit();

        [NativeMethod("DiffFileWithBaseAsync")]
        public extern void ShowDifferences(string path);

        public extern void SendNotification();

        public extern void SetError(int errorCode);

        public extern void ClearError(int errorCode);

        public extern void ClearErrors();

        public extern void ForceRefresh(bool refreshAssetDatabase);

        public extern void SetCollabEnabledForCurrentProject(bool enabled);

        public extern bool IsCollabEnabledForCurrentProject();

        public extern bool IsAssetIgnored(string path);

        public extern bool ShouldTrackAsset(string path);

        [ThreadAndSerializationSafe]
        public extern string GetProjectPath();

        [ThreadAndSerializationSafe]
        public extern bool IsConnected();

        [ThreadAndSerializationSafe]
        public extern bool AnyJobRunning();

        [ThreadAndSerializationSafe]
        public extern bool JobRunning(int a_jobID);

        public extern CollabStates GetAssetState(string guid);
        public extern CollabStates GetSelectedAssetState();
        public extern CollabStateID GetCollabState();

        [FreeFunction(HasExplicitThis = true)]
        public extern bool ValidateSelectiveCommit();

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void Disconnect();

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void CancelJobByType(int jobType, bool forceCancel);

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void DoInitialCommit();

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void Update(string revisionID, bool updateToRevision);

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void RevertFile(string path, bool forceOverwrite);

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void LaunchConflictExternalMerge(string path);

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void ShowConflictDifferences(string path);

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void ResyncSnapshot();

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void GoBackToRevision(string revisionID, bool updateToRevision);

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void ResyncToRevision(string revisionID);

        [NativeMethod("GetConflictsManager().GetAllConflicts")]
        public extern Change[] GetCollabConflicts();

        // Conflict Management
        [NativeMethod("GetConflictsManager().CheckConflictsResolvedExternal")]
        public extern void CheckConflictsResolvedExternal();

        [NativeMethod("GetTestHelper().AreTestsRunning")]
        public extern bool AreTestsRunning();

        [NativeMethod("GetTestHelper().SetTestsRunning")]
        public extern void SetTestsRunning(bool running);

        [NativeMethod("GetTestHelper().ClearOperationsFailure")]
        public extern void ClearAllFailures();

        [NativeMethod("GetTestHelper().UnmarkOperationFailure")]
        public extern void ClearNextOperationFailure();

        [NativeMethod("GetTestHelper().UnmarkOperationFailureForFile")]
        public extern void ClearNextOperationFailureForFile(string path);

        [NativeMethod("GetTestHelper().GetGUIDForTests")]
        public extern string GetGUIDForTests();

        [NativeMethod("GetTestHelper().NewGUIDForTests")]
        public extern void NewGUIDForTests();

        [NativeMethod("GetTestHelper().MarkOperationFailure")]
        public extern void FailNextOperation(Collab.Operation operation, int code);

        [NativeMethod("GetTestHelper().MarkOperationTimeOut")]
        public extern void TimeOutNextOperation(Collab.Operation operation, int timeOutSec);

        [NativeMethod("GetTestHelper().MarkOperationFailureForFile")]
        public extern void FailNextOperationForFile(string path, Collab.Operation operation, int code);

        [NativeMethod("GetTestHelper().MarkOperationTimeOutForFile")]
        public extern void TimeOutNextOperationForFile(string path, Collab.Operation operation, int timeOutSec);

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void TestPostSoftLockAsCollaborator(string projectGuid, string projectPath, string machineGuid,
            string assetGuid);

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        public extern void TestClearSoftLockAsCollaborator(string projectGuid, string projectPath, string machineGuid,
            string softLockHash);

        // Private helper methods for bindings
        [NativeMethod("GetConflictsManager().SetConflictsState")]
        extern bool SetConflictsResolved(string[] paths, CollabStates state);

        [NativeMethod("OnAssetBundleNameChanged")]
        extern void OnPostprocessAssetbundleNameChanged(string assetPath, string previousAssetBundleName, string newAssetBundleName);

        [NativeMethod("GetError")]
        internal extern bool GetErrorInternal(int errorFilter, out UnityErrorInfo info);

        [NativeMethod(HasExplicitThis = true, ThrowsException = true)]
        extern Change[] GetChangesToPublishInternal();

        [NativeMethod(HasExplicitThis = true, ThrowsException = true, IsThreadSafe = true)]
        extern void SetChangesToPublishInternal(ChangeItem[] changes);

        [NativeMethod(HasExplicitThis = true, ThrowsException = true, IsThreadSafe = true)]
        extern Change[] GetSelectedChangesInternal();

        [NativeMethod(Name = "GetJobProgress", HasExplicitThis = true, ThrowsException = true)]
        extern bool GetJobProgressInternal([Out] ProgressInfo info, int jobId);

        [NativeMethod(HasExplicitThis = true, ThrowsException = true)]
        public extern void Publish(string comment, bool useSelectedAssets, bool confirmMatchesPrevious);

        [NativeMethod(HasExplicitThis = true, ThrowsException = true, IsThreadSafe = true)]
        public extern void ClearSelectedChangesToPublish();

        [NativeMethod(HasExplicitThis = true, ThrowsException = true)]
        public extern SoftLock[] GetSoftLocks(string assetGuid);
    }

    // keep in sync with CollabSettingType in C++
    internal enum CollabSettingType
    {
        InProgressEnabled = 0,
        InProgressProjectEnabled = 1,
        InProgressGlobalEnabled = 2
    }

    // keep in sync with CollabSettingStatus in C++
    internal enum CollabSettingStatus
    {
        None = 0,
        Available = 1
    }

    [NativeHeader("Editor/Src/Collab/CollabSettingsManager.h")]
    [StaticAccessor("GetCollabSettingsManager()", StaticAccessorType.Dot)]
    internal class CollabSettingsManager
    {
        [RequiredByNativeCode]
        static void NotifyStatusListeners(CollabSettingType type, CollabSettingStatus status)
        {
            if (statusNotifier[type] != null)
                statusNotifier[type](type, status);
        }

        public delegate void SettingStatusChanged(CollabSettingType type, CollabSettingStatus status);
        public static Dictionary<CollabSettingType, SettingStatusChanged> statusNotifier = new Dictionary<CollabSettingType, SettingStatusChanged>();

        static CollabSettingsManager()
        {
            foreach (CollabSettingType type in Enum.GetValues(typeof(CollabSettingType)))
                statusNotifier[type] = null;
        }

        public static extern bool IsAvailable(CollabSettingType type);

        public static extern bool inProgressEnabled
        {
            get;
        }
    }
}
