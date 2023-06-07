// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// This file was introduced as part of the collab removal, it supports any versions of com.unity.collab-proxy under 1.17.7
// It allows for an error free upgrade by provinding mock classes for the removed ones.

using System;
using System.Diagnostics;
using System.Collections.Generic;

using UnityEditor.Connect;
using UnityEditor.PackageManager;
using UnityEngine;

#pragma warning disable 0067
#pragma warning disable 0618
namespace UnityEditor.Collaboration
{
    internal static class LogObsolete
    {
        static bool s_Initialized;
        static bool s_NeedsLogging;
        static Stopwatch s_Stopwatch = Stopwatch.StartNew();

        internal static void Log()
        {
            if (!s_Initialized)
                s_NeedsLogging = IsObsolete("com.unity.collab-proxy", "1.1");

            s_Initialized = true;

            if (!s_NeedsLogging)
                return;

            if (s_Stopwatch.ElapsedMilliseconds < 1000)
                return;

            UnityEngine.Debug.unityLogger.LogWarning(
                "com.unity.collab-proxy",
                "This version of the package is not supported, please upgrade to the latest version. https://unity.com/solutions/version-control");

            s_Stopwatch.Restart();
        }

        internal static bool IsObsolete(string packageName, string version)
        {
            UnityEditor.PackageManager.PackageInfo package = null;
            try
            {
                var list = Client.List(true);
                var stp = Stopwatch.StartNew();

                while(!list.IsCompleted && stp.ElapsedMilliseconds < 1000)
                    System.Threading.Thread.Sleep(100);

                if (list.Result == null)
                    return false;

                foreach(var p in list.Result)
                {
                    if (p.name == packageName)
                    {
                        package = p;
                        break;
                    }
                }
            }
            catch
            {
                return false;
            }

            if (package == null)
                return false;

            if (package.version == null)
                return false;

            return package.version.StartsWith(version);
        }
    }

    internal class Collab
    {
        static Collab s_instance = null;
        public static Collab instance {
            get
            {
                if (s_instance == null)
                    s_instance = new Collab();

                LogObsolete.Log();
                return s_instance;
            }
        }

        Collab()
        {
        }

        [Flags]
        public enum Operation
        {
            Noop = 0,
            Publish = 1 << 0,
            Update = 1 << 1,
            Revert = 1 << 2,
            GoBack = 1 << 3,
            Restore = 1 << 4,
            Diff = 1 << 5,
            ConflictDiff = 1 << 6,
            Exclude = 1 << 7,
            Include = 1 << 8,
            ChooseMine = 1 << 9,
            ChooseTheirs = 1 << 10,
            ExternalMerge = 1 << 11,
        }

        [Flags]
        public enum CollabStates : uint
        {
            kCollabNone = 0,
            kCollabLocal = 1,
            kCollabSynced = 1 << 1,
            kCollabOutOfSync = 1 << 2,
            kCollabIgnored = 1 << 3,
            kCollabCheckedOutLocal = 1 << 4,
            kCollabCheckedOutRemote = 1 << 5,
            kCollabDeletedLocal = 1 << 6,
            kCollabDeletedRemote = 1 << 7,
            kCollabAddedLocal = 1 << 8,
            kCollabAddedRemote = 1 << 9,
            kCollabConflicted = 1 << 10,
            kCollabMovedLocal = 1 << 11,
            kCollabMovedRemote = 1 << 12,
            kCollabUpdating = 1 << 13,
            kCollabReadOnly = 1 << 14,
            kCollabMetaFile = 1 << 15,
            kCollabUseMine = 1 << 16,
            kCollabUseTheir = 1 << 17,
            kCollabMerged = 1 << 18,
            kCollabPendingMerge = 1 << 19,
            kCollabFolderMetaFile = 1 << 20,
            KCollabContentChanged = 1 << 21,
            KCollabContentConflicted = 1 << 22,
            KCollabContentDeleted = 1 << 23,
            kCollabInvalidState = 1 << 30,
            kAnyLocalChanged = (kCollabAddedLocal | kCollabCheckedOutLocal | kCollabDeletedLocal | kCollabMovedLocal),
            kAnyLocalEdited = (kCollabAddedLocal | kCollabCheckedOutLocal | kCollabMovedLocal),
            kCollabAny = 0xFFFFFFFF
        }

        internal enum CollabStateID { None, Uninitialized, Initialized }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(Collab collab){ return IntPtr.Zero; }
        }

        public event StateChangedDelegate StateChanged;
        public event StateChangedDelegate RevisionUpdated;
        public event RevisionChangedDelegate RevisionUpdated_V2;
        public event StateChangedDelegate JobsCompleted;
        public event ErrorDelegate ErrorOccurred;
        public event SetErrorDelegate ErrorOccurred_V2;
        public event ErrorDelegate ErrorCleared;
        public event ChangeItemsChangedDelegate ChangeItemsChanged;
        public event ChangeItemsChangedDelegate SelectedChangeItemsChanged;
        public event StateChangedDelegate CollabInfoChanged;
        public static int GetRevisionsData(bool withChanges, int startIndex, int numRevisions){ return 0; }
        public static int GetSingleRevisionData(bool withChanges, string id){ return 0; }
        public static RevisionsData PopulateRevisionsData(IntPtr nativeData){ return new RevisionsData(); }
        public static Revision PopulateSingleRevisionData(IntPtr nativeData){ return new Revision(); }
        public static ShowToolbarAtPositionDelegate ShowToolbarAtPosition = null;
        public static IsToolbarVisibleDelegate IsToolbarVisible = null;
        public static CloseToolbarDelegate CloseToolbar = null;
        public static ShowHistoryWindowDelegate ShowHistoryWindow = null;
        public static ShowChangesWindowDelegate ShowChangesWindow = null;
        public static string[] clientType = Array.Empty<string>();
        internal static string editorPrefCollabClientType = string.Empty;
        public static string GetProjectClientType() { return string.Empty; }
        public static void SetVersionControl(IVersionControl instance){}
        internal static bool HasVersionControl(){ return false; }
        internal static void ShowChangesWindowView(){}
        internal static CollabStates GetAssetState(string assetGuid, string assetPath){ return (CollabStates)0; }
        public static void OnSettingStatusChanged(CollabSettingType type, CollabSettingStatus status){}
        public static bool InitializeSoftlocksViewController(){ return false; }
        public static bool IsDiffToolsAvailable(){ return false; }
        public static void SwitchToDefaultMode(){}
        public static void OnProgressEnabledSettingStatusChanged(CollabSettingType type, CollabSettingStatus status){}

        public CollabInfo collabInfo { get; }
        public void SetSeat(bool value){}
        public void RefreshSeatAvailabilityAsync(){}
        public string GetProjectGUID(){ return string.Empty; }
        public bool ShouldDoInitialCommit(){ return false; }
        public void ShowDifferences(string path){}
        public void SendNotification(){}
        public void SetError(int errorCode){}
        public void ClearError(int errorCode){}
        public void ClearErrors(){}
        public void ForceRefresh(bool refreshAssetDatabase){}
        public void SetCollabEnabledForCurrentProject(bool enabled){}
        public bool IsCollabEnabledForCurrentProject(){ return false; }
        public bool IsAssetIgnored(string path){ return false; }
        public bool ShouldTrackAsset(string path){ return false; }
        public string GetProjectPath(){ return string.Empty; }
        public bool IsConnected(){ return false; }
        public bool AnyJobRunning(){ return false; }
        public bool JobRunning(int a_jobID){ return false; }
        public CollabStates GetAssetState(string guid){ return (CollabStates)0; }
        public CollabStates GetSelectedAssetState(){ return (CollabStates)0; }
        public CollabStateID GetCollabState(){ return (CollabStateID)0; }
        public bool ValidateSelectiveCommit(){ return false; }
        public void Disconnect(){}
        public void CancelJobByType(int jobType, bool forceCancel){}
        public void DoInitialCommit(){}
        public void Update(string revisionID, bool updateToRevision){}
        public void RevertFile(string path, bool forceOverwrite){}
        public void RevertFiles(ChangeItem[] changeItems, bool forceOverwrite){}
        public void LaunchConflictExternalMerge(string path){}
        public void ShowConflictDifferences(string path){}
        public void ResyncSnapshot(){}
        public void GoBackToRevision(string revisionID, bool updateToRevision){}
        public void ResyncToRevision(string revisionID){}
        public Change[] GetCollabConflicts(){ return Array.Empty<Change>(); }
        public void CheckConflictsResolvedExternal(){}
        public bool AreTestsRunning(){ return false; }
        public void SetTestsRunning(bool running){}
        public void ClearAllFailures(){}
        public void ClearNextOperationFailure(){}
        public void ClearNextOperationFailureForFile(string path){}
        public string GetGUIDForTests(){ return string.Empty; }
        public void NewGUIDForTests(){}
        public void FailNextOperation(Collab.Operation operation, int code){}
        public void TimeOutNextOperation(Collab.Operation operation, int timeOutSec){}
        public void FailNextOperationForFile(string path, Collab.Operation operation, int code){}
        public void TimeOutNextOperationForFile(string path, Collab.Operation operation, int timeOutSec){}
        public void TestPostSoftLockAsCollaborator(string projectGuid, string projectPath, string machineGuid, string assetGuid){}
        public void TestClearSoftLockAsCollaborator(string projectGuid, string projectPath, string machineGuid, string softLockHash){}
        internal bool GetErrorInternal(int errorFilter, out UnityErrorInfo info){ info = new UnityErrorInfo(); return false; }
        public void Publish(string comment, bool useSelectedAssets, bool confirmMatchesPrevious){}
        public void PublishAssetsAsync(string comment, ChangeItem[] changes){}
        public void ClearSelectedChangesToPublish(){}
        public void SendCollabInfoNotification(){}
        public CollabFilters collabFilters = new CollabFilters();
        public String projectBrowserSingleSelectionPath { get; set; }
        public String projectBrowserSingleMetaSelectionPath { get; set; }
        public string[] currentProjectBrowserSelection;
        public void RefreshAvailableLocalChangesSynchronous(){}
        public bool GetError(UnityConnect.UnityErrorFilter errorFilter, out UnityErrorInfo info){ info = new UnityErrorInfo(); return false; }
        public void CancelJob(int jobType){}
        public void UpdateEditorSelectionCache(){}
        public CollabInfo GetCollabInfo(){ return new CollabInfo(); }
        public void SaveAssets(){}
        public void ShowInProjectBrowser(string filterString){}
        public bool SetConflictsResolvedMine(string[] paths){ return false; }
        public bool SetConflictsResolvedTheirs(string[] paths){ return false; }
        public PublishInfo GetChangesToPublish() { return new PublishInfo(); }
        public PublishInfo_V2 GetChangesToPublish_V2() { return new PublishInfo_V2(); }
        public void SetChangesToPublish(ChangeItem[] changes){}
        public ProgressInfo GetJobProgress(int jobId){ return new ProgressInfo(); }
    }

    internal enum CollabSettingType
    {
        InProgressEnabled = 0,
        InProgressProjectEnabled = 1,
        InProgressGlobalEnabled = 2
    }

    internal enum CollabSettingStatus
    {
        None = 0,
        Available = 1
    }

    internal class CollabSettingsManager
    {
        public delegate void SettingStatusChanged(CollabSettingType type, CollabSettingStatus status);
        public static Dictionary<CollabSettingType, SettingStatusChanged> statusNotifier = new Dictionary<CollabSettingType, SettingStatusChanged>();

        static CollabSettingsManager(){}

        public static bool IsAvailable(CollabSettingType type)
        {
            return false;
        }

        public static bool inProgressEnabled { get; }
    }

    internal class ProgressInfo
    {
        public enum ProgressType : uint
        {
            None = 0,
            Count = 1,
            Percent = 2,
            Both = 3
        }

        public int jobId { get { return 0; } }
        public string title { get { return string.Empty; } }
        public string extraInfo { get { return string.Empty; } }
        public int currentCount { get { return 0; } }
        public int totalCount { get { return 0; } }
        public bool completed { get { return true; } }
        public bool cancelled { get { return false; } }
        public bool canCancel { get { return false; } }
        public string lastErrorString { get { return string.Empty; } }
        public ulong lastError { get { return 0; } }
        public int percentComplete { get{ return 0; } }
        public bool isProgressTypeCount { get { return false; } }
        public bool isProgressTypePercent { get { return false; } }
        public bool errorOccured { get { return false; } }
    }

    internal class ChangeItem
    {
        public string Path { get; set; }
        public Change.RevertableStates RevertableState { get; set; }
        public string RelatedTo { get; set; }
        public string RevisionId { get; set; }
        public string Hash { get; set; }
        public Collab.CollabStates State { get; set; }
        public long Size { get; set; }
        public string DownloadPath { get; set; }
        public string FromPath { get; set; }
    }

    internal class PublishInfo
    {
        public Change[] changes;
        public bool filter;
    }

    internal class PublishInfo_V2
    {
        public ChangeItem[] changes;
        public bool filter;
    }

    internal class RevisionsResult
    {
        public List<Revision> Revisions = new List<Revision>();
        public int RevisionsInRepo = -1;
        public int Count { get { return 0; } }

        public void Clear(){}
    }

    internal interface IRevisionsService
    {
        event RevisionsDelegate FetchRevisionsCallback;
        void GetRevisions(int offset, int count);
        string tipRevision { get; }
        string currentUser { get; }
    }

    internal class RevisionsService : IRevisionsService
    {
        public event RevisionsDelegate FetchRevisionsCallback;
        public event SingleRevisionDelegate FetchSingleRevisionCallback;

        public string tipRevision { get { return string.Empty; } }
        public string currentUser { get { return string.Empty; } }

        public RevisionsService(Collab collabInstance, UnityConnect connectInstance)
        {
        }

        public void GetRevisions(int offset, int count){}

        public void GetRevision(string revId){}
    }

    internal class Change
    {
        public enum RevertableStates : uint
        {
            Revertable                         = 1 << 0,
            NotRevertable                      = 1 << 1,
            Revertable_File                    = 1 << 2,
            Revertable_Folder                  = 1 << 3,
            Revertable_EmptyFolder             = 1 << 4,
            NotRevertable_File                 = 1 << 5,
            NotRevertable_Folder               = 1 << 6,
            NotRevertable_FileAdded            = 1 << 7,
            NotRevertable_FolderAdded          = 1 << 8,
            NotRevertable_FolderContainsAdd    = 1 << 9,
            InvalidRevertableState             = (uint)1 << 31
        }

        public string path { get { return string.Empty; } }
        public Collab.CollabStates state { get { return (Collab.CollabStates)0; } }
        public bool isRevertable { get { return false; } }
        public RevertableStates revertableState { get { return (RevertableStates)0; } }
        public string relatedTo { get { return string.Empty; } }
        public bool isMeta { get { return false; } }
        public bool isConflict { get { return false; } }
        public bool isFolderMeta { get { return false; } }
        public bool isResolved { get { return false; } }
        public string localStatus { get { return string.Empty; } }
        public string remoteStatus { get { return string.Empty; } }
        public string resolveStatus { get { return string.Empty; } }

        internal bool HasState(Collab.CollabStates states)
        {
            return false;
        }

        internal bool HasRevertableState(RevertableStates revertableStates)
        {
            return false;
        }
    }

    internal abstract class AbstractFilters
    {
        public List<string[]> filters { get; set;}
        public abstract void InitializeFilters();
        public bool ContainsSearchFilter(string name, string searchString){ return false; }
        public void ShowInFavoriteSearchFilters(){}
        public void HideFromFavoriteSearchFilters(){}
    }

    internal class CollabFilters : AbstractFilters
    {
        public override void InitializeFilters(){}
        public void ShowInProjectBrowser(string filterString){}
        public void OnCollabStateChanged(CollabInfo info){}
    }


    internal delegate void StateChangedDelegate(CollabInfo info);
    internal delegate void RevisionChangedDelegate(CollabInfo info, string rev, string action);
    internal delegate void SetErrorDelegate(UnityErrorInfo error);
    internal delegate void ErrorDelegate();
    internal delegate bool ShowToolbarAtPositionDelegate(Rect screenRect);
    internal delegate bool IsToolbarVisibleDelegate();
    internal delegate void ShowHistoryWindowDelegate();
    internal delegate void ShowChangesWindowDelegate();
    internal delegate void CloseToolbarDelegate();
    internal delegate void ChangesChangedDelegate(Change[] changes, bool isFiltered);
    internal delegate void ChangeItemsChangedDelegate(ChangeItem[] changes, bool isFiltered);
    delegate void RevisionsDelegate(RevisionsResult revisionsResult);
    delegate void SingleRevisionDelegate(Revision? revision);

    internal struct CollabInfo
    {
        public bool ready { get { return true; } }
        public bool update { get { return false; } }
        public bool publish { get { return false; } }
        public bool inProgress { get { return false; } }
        public bool maintenance { get { return false; } }
        public bool conflict { get { return false; } }
        public bool refresh { get { return false; } }
        public bool seat { get { return false; } }
        public string tip { get { return string.Empty; } }
        public bool Equals(CollabInfo other){ return false; }
    }

    internal struct ChangeAction
    {
        public ChangeAction(string path = "", string action = ""){}
        public string path { get { return string.Empty; } }
        public string action { get { return string.Empty; } }
    }

    internal struct Revision
    {
        internal Revision(string revisionID = "", string authorName = "", string author = "", string comment = "", string reference = "", ulong timeStamp = 0, bool isObtained = false, ChangeAction[] entries = null, CloudBuildStatus[] buildStatuses = null){}
        public string authorName { get { return string.Empty;  } }
        public string author { get { return string.Empty;  } }
        public string comment { get { return string.Empty;  } }
        public string revisionID { get { return string.Empty;  } }
        public string reference { get { return string.Empty;  } }
        public ulong timeStamp { get { return 0;  } }
        public bool isObtained { get { return false;  } }
        public ChangeAction[] entries { get { return Array.Empty<ChangeAction>();  } }
        public CloudBuildStatus[] buildStatuses { get { return Array.Empty<CloudBuildStatus>();  } }
    }

    internal struct CloudBuildStatus
    {
        internal CloudBuildStatus(string platform = "", bool complete = false, bool success = false){}
        public string platform { get { return string.Empty; } }
        public bool complete { get { return false; } }
        public bool success { get { return false; } }
    }

    internal struct RevisionData
    {
        public string id;
        public int index;
        public DateTime timeStamp;
        public string authorName;
        public string comment;
        public bool obtained;
        public bool current;
        public bool inProgress;
        public bool enabled;
        public BuildState buildState;
        public int buildFailures;
        public ICollection<ChangeData> changes;
        public int changesTotal;
        public bool changesTruncated;
    }

    internal struct RevisionsData
    {
        public int RevisionsInRepo {get { return 0; }}
        public int RevisionOffset {get { return 0; }}
        public int ReturnedRevisions {get { return 0; }}
        public Revision[] Revisions {get { return Array.Empty<Revision>(); }}
    }

    internal enum HistoryState
    {
        Error,
        Offline,
        Maintenance,
        LoggedOut,
        NoSeat,
        Disabled,
        Waiting,
        Ready,
    }

    internal enum BuildState
    {
        None,
        Configure,
        Success,
        Failed,
        InProgress,
    }

    internal struct ChangeData
    {
        public string path;
        public string action;
    }
}
