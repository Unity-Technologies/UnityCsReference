// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Web;
using UnityEditorInternal;
using UnityEditor.Connect;
using UnityEditor.SceneManagement;

namespace UnityEditor.Collaboration
{
    internal delegate void StateChangedDelegate(CollabInfo info);

    //*undocumented
    // We want to raise this exception from Cpp code but it fails
    //public class CollabException: Exception
    //{
    //  public CollabException(string message) : base(message)
    //  {
    //  }
    //}

    [InitializeOnLoad]
    internal partial class Collab
    {
        public event StateChangedDelegate StateChanged;

        private static Collab s_Instance;
        private static bool s_IsFirstStateChange = true;

        public String projectBrowserSingleSelectionPath { get; set; }

        public String projectBrowserSingleMetaSelectionPath { get; set; }

        public string[] currentProjectBrowserSelection;

        [Flags]
        public enum CollabStates : ulong
        {
            kCollabNone             =   0,
            kCollabLocal            =   1,

            kCollabSynced           =   1 << 1,
            kCollabOutOfSync        =   1 << 2,
            kCollabIgnored          =   1 << 3,
            kCollabCheckedOutLocal  =   1 << 4,
            kCollabCheckedOutRemote =   1 << 5,
            kCollabDeletedLocal     =   1 << 6,
            kCollabDeletedRemote    =   1 << 7,
            kCollabAddedLocal       =   1 << 8,
            kCollabAddedRemote      =   1 << 9,
            kCollabConflicted       =   1 << 10,
            kCollabMovedLocal       =   1 << 11,
            kCollabMovedRemote      =   1 << 12,
            kCollabUpdating         =   1 << 13,
            kCollabReadOnly         =   1 << 14,
            kCollabMetaFile         =   1 << 15,
            kCollabUseMine          =   1 << 16,
            kCollabUseTheir         =   1 << 17,
            kCollabMerged           =   1 << 18,
            kCollabPendingMerge     =   1 << 19,
            kCollabFolderMetaFile   =   1 << 20,
            KCollabContentChanged   =   1 << 21,
            KCollabContentConflicted =   1 << 22,
            KCollabContentDeleted   =   1 << 23,

            // always keep most significant
            kCollabInvalidState     =   1 << 30,

            kAnyLocalChanged =              (kCollabAddedLocal | kCollabCheckedOutLocal | kCollabDeletedLocal | kCollabMovedLocal),
            kAnyLocalEdited =               (kCollabAddedLocal | kCollabCheckedOutLocal | kCollabMovedLocal)
        };

        internal enum CollabStateID { None, Uninitialized, Initialized };

        public static string[] clientType =
        {
            "Cloud Server",
            "Mock Server"
        };

        internal static string editorPrefCollabClientType = "CollabConfig_Client";

        public static string GetProjectClientType()
        {
            var cvalue = EditorUserSettings.GetConfigValue(editorPrefCollabClientType);
            return string.IsNullOrEmpty(cvalue) ? clientType[0] : cvalue;
        }

        [MenuItem("Window/Collab/Get Revisions", false, 1000, true)]
        public static void TestGetRevisions()
        {
            Revision[] revisions = instance.GetRevisions();
            if (revisions.Length == 0)
            {
                Debug.Log("No revisions");
                return;
            }

            int num = revisions.Length;
            foreach (Revision revision in revisions)
            {
                Debug.Log("Revision #" + num + ": " + revision.revisionID);
                num--;
            }
        }

        public static Collab instance
        {
            get
            {
                return s_Instance;
            }
        }

        static Collab()
        {
            s_Instance = new Collab();
            s_Instance.projectBrowserSingleSelectionPath = string.Empty;
            s_Instance.projectBrowserSingleMetaSelectionPath = string.Empty;
            JSProxyMgr.GetInstance().AddGlobalObject("unity/collab", s_Instance);
        }

        public void CancelJobWithoutException(int jobType)
        {
            try
            {
                CancelJobByType(jobType);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log("Cannot cancel job, reason:" + ex.Message);
            }
        }

        public CollabStates GetAssetState(string guid)
        {
            return (CollabStates)GetAssetStateInternal(guid);
        }

        public CollabStates GetSelectedAssetState()
        {
            return (CollabStates)GetSelectedAssetStateInternal();
        }

        public void UpdateEditorSelectionCache()
        {
            var result = new List<string>();

            foreach (var elem in Selection.assetGUIDsDeepSelection)
            {
                var path = AssetDatabase.GUIDToAssetPath(elem);
                result.Add(path);

                var meta = path + ".meta";
                if (File.Exists(meta))
                {
                    result.Add(meta);
                }
            }
            currentProjectBrowserSelection = result.ToArray();
        }

        public CollabInfo GetCollabInfo()
        {
            return collabInfo;
        }

        public static bool IsDiffToolsAvailable()
        {
            return InternalEditorUtility.GetAvailableDiffTools().Length > 0;
        }

        public void SaveAssets()
        {
            AssetDatabase.SaveAssets();
        }

        public static void SwitchToDefaultMode()
        {
            bool in2D = EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D;
            var sv = SceneView.lastActiveSceneView;
            if (sv != null && sv.in2DMode != in2D)
            {
                sv.in2DMode = in2D;
            }
        }

        private static void OnStateChanged()
        {
            // register only once
            if (s_IsFirstStateChange)
            {
                s_IsFirstStateChange = false;
                UnityConnect.instance.StateChanged += OnUnityConnectStateChanged;
            }
            var handler = instance.StateChanged;
            if (handler != null)
            {
                handler(instance.collabInfo);
            }
        }

        private static void PublishDialog(string changelist)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var dialog = CollabPublishDialog.ShowCollabWindow(changelist);

            if (dialog.Options.DoPublish)
                Collab.instance.Publish(dialog.Options.Comments, true);
        }

        private static void CannotPublishDialog(string infoMessage)
        {
            CollabCannotPublishDialog.ShowCollabWindow(infoMessage);
        }

        private static void OnUnityConnectStateChanged(ConnectInfo state)
        {
            instance.SendNotification();
        }
    };
}
