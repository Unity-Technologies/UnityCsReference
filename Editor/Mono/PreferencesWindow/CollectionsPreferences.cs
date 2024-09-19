// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Analytics;


internal class JobsMenuProvider: SettingsProvider
{
    private static int savedJobWorkerCount = JobsUtility.JobWorkerCount;

    class JobsProperties
    {
        public static readonly GUIContent jobSystem = EditorGUIUtility.TrTextContent("Job System");
        public static readonly GUIContent useJobThreads = EditorGUIUtility.TrTextContent("Use Job Threads");
        public static readonly GUIContent enableJobsDebugger = EditorGUIUtility.TrTextContent("Enable Jobs Debugger");
        public static readonly GUIContent leakDetectionLevel = EditorGUIUtility.TrTextContent("Leak Detection Level");
        public static readonly GUIContent graphicsThreadingMode = EditorGUIUtility.TrTextContent("Graphics Threading Mode");
        public static readonly GUIContent allowGraphicsJobsInEditor = EditorGUIUtility.TrTextContent("Allow Graphics Jobs in Editor");
    }

    [SettingsProvider]
    private static SettingsProvider JobsPreferencesItem()
    {
        var provider = new JobsMenuProvider("Preferences/Jobs", GetSearchKeywordsFromGUIContentProperties<JobsProperties>()) { label = "Jobs" };
        provider.guiHandler = searchContext => { OnGUI(searchContext, provider.ShowJobsProvider); };
        return provider;
    }

    private void ShowJobsProvider(string searchContext)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(JobsProperties.jobSystem, EditorStyles.boldLabel);
        GUILayout.EndHorizontal();

        var originalWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 200f;
        EditorGUILayout.LabelField("For safety, these values are reset on editor restart.");

        bool madeChange = false;

        bool oldWorkerCount = (JobsUtility.JobWorkerCount > 0);
        bool newWorkerCount = EditorGUILayout.Toggle(JobsProperties.useJobThreads, oldWorkerCount);
        if (newWorkerCount != oldWorkerCount)
        {
            madeChange = true;
            SwitchUseJobThreads();
        }

        bool oldUseJobsDebugger = JobsUtility.JobDebuggerEnabled;
        var newUseJobsDebugger = EditorGUILayout.Toggle(JobsProperties.enableJobsDebugger, JobsUtility.JobDebuggerEnabled);
        if (newUseJobsDebugger != oldUseJobsDebugger)
        {
            madeChange = true;
            SetUseJobsDebugger(newUseJobsDebugger);
        }

        var oldLeakDetectionMode = NativeLeakDetection.Mode;
        var newLeakDetectionMode = (NativeLeakDetectionMode)EditorGUILayout.EnumPopup(JobsProperties.leakDetectionLevel, oldLeakDetectionMode);
        if (newLeakDetectionMode != oldLeakDetectionMode)
        {
            madeChange = true;
            SetLeakDetection(newLeakDetectionMode);
        }

        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        GUILayout.Label(JobsProperties.graphicsThreadingMode, EditorStyles.boldLabel);
        GUILayout.EndHorizontal();

        //Starting here
        EditorGUI.BeginChangeCheck();

        bool oldAllowEditorGraphicsJobs = PlayerSettings.GetEditorGfxJobOverride();
        bool newAllowEditorGraphicsJobs = oldAllowEditorGraphicsJobs;

        if (GUI.enabled)
        {
            newAllowEditorGraphicsJobs = EditorGUILayout.Toggle(JobsProperties.allowGraphicsJobsInEditor, oldAllowEditorGraphicsJobs);
        }
        else
        {
            EditorGUILayout.Toggle(JobsProperties.allowGraphicsJobsInEditor, oldAllowEditorGraphicsJobs);
        }

        if (EditorGUI.EndChangeCheck() && (newAllowEditorGraphicsJobs != oldAllowEditorGraphicsJobs))
        {
            madeChange = true;
            SetAllowEditorGraphicsJobs(newAllowEditorGraphicsJobs);
            //From Player Settings graphics jobs on/off change: 

            bool restartEditor = CheckApplyEditorGraphicsJobsModeChange();
            if (restartEditor)
            {
                EditorApplication.RequestCloseAndRelaunchWithCurrentArguments();
                GUIUtility.ExitGUI();
            }
        }

        if (madeChange)
            Telemetry.LogMenuPreferences(new Telemetry(new Telemetry.MenuPreferencesEvent
            {
                allowJobInEditor = newAllowEditorGraphicsJobs,
                useJobsThreads = newUseJobsDebugger,
                enableJobsDebugger = newUseJobsDebugger,
                nativeLeakDetectionMode = newLeakDetectionMode
            }));

        EditorGUIUtility.labelWidth = originalWidth;
    }

    private static void OnGUI(string searchContext, Action<string> drawAction)
    {
        using (new SettingsWindow.GUIScope())
            drawAction(searchContext);
    }

    static void SwitchUseJobThreads()
    {
        if (JobsUtility.JobWorkerCount > 0)
        {
            savedJobWorkerCount = JobsUtility.JobWorkerCount;
            JobsUtility.JobWorkerCount = 0;
        }
        else
        {
            JobsUtility.JobWorkerCount = savedJobWorkerCount;
            if (savedJobWorkerCount == 0)
            {
                JobsUtility.ResetJobWorkerCount();
            }
        }
    }

    static bool CheckApplyEditorGraphicsJobsModeChange()
    {
        bool doRestart = false;
        // If we have dirty scenes we need to save or discard changes before we restart editor.
        // Otherwise user will get a dialog later on where they can click cancel and put editor in a bad device state.
        var dirtyScenes = new List<Scene>();
        for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (scene.isDirty)
                dirtyScenes.Add(scene);
        }
        if (dirtyScenes.Count != 0)
        {
            var result = EditorUtility.DisplayDialogComplex("Changing editor graphics jobs mode",
                "You've changed the active graphics jobs mode. This requires a restart of the Editor. Do you want to save the Scene when restarting?",
                "Save and Restart", "Cancel Changing API", "Discard Changes and Restart");
            if (result == 1)
            {
                doRestart = false; // Cancel was selected
            }
            else
            {
                doRestart = true;
                if (result == 0) // Save and Restart was selected
                {
                    for (int i = 0; i < dirtyScenes.Count; ++i)
                    {
                        var saved = EditorSceneManager.SaveScene(dirtyScenes[i]);
                        if (saved == false)
                        {
                            doRestart = false;
                        }
                    }
                }
                else // Discard Changes and Restart was selected
                {
                    for (int i = 0; i < dirtyScenes.Count; ++i)
                        EditorSceneManager.ClearSceneDirtiness(dirtyScenes[i]);
                }
            }
        }
        else
        {
            doRestart = EditorUtility.DisplayDialog("Changing editor graphics jobs mode",
                "You've changed the active graphics jobs mode. This requires a restart of the Editor.",
                "Restart Editor", "Not now");
        }
        
        return doRestart;
    }

    static void SetAllowEditorGraphicsJobs(bool value)
    {
        PlayerSettings.SetEditorGfxJobOverride(value);
    }

    static void SetUseJobsDebugger(bool value)
    {
        JobsUtility.JobDebuggerEnabled = value;
    }

    static void SetLeakDetection(NativeLeakDetectionMode value)
    {
        switch (value)
        {
            case NativeLeakDetectionMode.Disabled:
                {
                    Debug.LogWarning("Leak detection has been disabled. Leak warnings will not be generated, and all leaks up to now are forgotten.");
                    break;
                }
            case NativeLeakDetectionMode.Enabled:
                {
                    Debug.Log("Leak detection has been enabled. Leak warnings will be generated upon domain reload.");
                    break;
                }
            case NativeLeakDetectionMode.EnabledWithStackTrace:
                {
                    Debug.Log("Leak detection with stack traces has been enabled. Leak warnings will be generated upon domain reload.");
                    break;
                }
            default:
                {
                    throw new Exception($"Unhandled {nameof(NativeLeakDetectionMode)}");
                }
        }

        NativeLeakDetection.Mode = value;
    }

    [AnalyticInfo(eventName: "collectionsMenuPreferences", vendorKey: "unity.collections")]
    internal struct Telemetry : IAnalytic
    {
        public Telemetry(MenuPreferencesEvent data) { m_data = data; }

        [Serializable]
        internal struct MenuPreferencesEvent : IAnalytic.IData
        {
            public bool enableJobsDebugger;
            public bool useJobsThreads;
            public bool allowJobInEditor;
            public NativeLeakDetectionMode nativeLeakDetectionMode;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_data;
            return data != null;
        }

        public static void LogMenuPreferences(Telemetry analytics)
        {
        EditorAnalytics.SendAnalytic(analytics);
        }
      
        MenuPreferencesEvent m_data;
    }

    public JobsMenuProvider(string path, IEnumerable<string> keywords = null)
            : base(path, SettingsScope.User, keywords)
    {
    }

}
