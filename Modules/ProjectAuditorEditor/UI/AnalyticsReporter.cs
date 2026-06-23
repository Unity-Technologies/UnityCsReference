// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;

namespace Unity.ProjectAuditor.Editor.UI
{
    static partial class AnalyticsReporter
    {
        const int k_MaxEventsPerHour = 100;
        const int k_MaxEventItems = 1000;
        const int k_MaxIssuesInAnalyzeSummary = 10;
        const int k_EventVersion = 2;

        const string k_VendorKey = "unity.projectauditor";
        const string k_EventTopicName = "projectAuditorUsage";

#pragma warning disable CS0649 // TODO - remove if we implement a code path for the new analytics API (whatever that is..)
        [AutoStaticsCleanupOnCodeReload]
        static bool s_EnableAnalytics;
#pragma warning restore CS0649

        public static void EnableAnalytics()
        {
            // TODO
        }

        public enum UIButton
        {
            // General UI
            Analyze,
            Export,
            AssemblySelect,
            AssemblySelectApply,
            AreaSelect,
            AreaSelectApply,
            Mute,
            Unmute,
            ShowMuted,
            OnlyCriticalIssues,
            Load,
            Save,

            // High level views
            Summary,
            ProjectSettings,

            // Code issues
            ApiCalls,
            CodeCompilerMessages,
            Generics,
            DomainReload,

            // Assets
            Assets,
            Shaders,
            ShaderCompilerMessages,
            ShaderVariants,
            ComputeShaderVariants,
            Materials,
            Textures,
            SpriteAtlases,
            AudioClip,
            Meshes,
            AnimatorControllers,
            AnimationClips,
            Avatars,
            AvatarMasks,

            // Build report
            BuildFiles,
            BuildSteps,

            // Assemblies
            Assemblies,
            PrecompiledAssemblies,
            Packages
        }

        // -------------------------------------------------------------------------------------------------------------

        [Serializable]
        struct ProjectAuditorEvent
        {
            // camelCase since these events get serialized to Json and naming convention in analytics is camelCase
            public string action;    // Name of the buttom
            public Int64 t_since_start; // Time since app start (in microseconds)
            public Int64 duration; // Duration of event in ticks - 100-nanosecond intervals.
            public Int64 ts; //Timestamp (milliseconds epoch) when action started.

            public ProjectAuditorEvent(string name, Analytic analytic)
            {
                action = name;
                t_since_start = SecondsToMicroseconds(analytic.GetStartTime());
                duration = SecondsToTicks(analytic.GetDurationInSeconds());
                ts = analytic.GetTimestamp();
            }
        }

        [Serializable]
        struct ProjectAuditorEventWithKeyValues
        {
            [Serializable]
            public struct EventKeyValue
            {
                public string key;
                public string value;
            }

            public string action;
            public Int64 t_since_start;
            public Int64 duration;
            public Int64 ts;
            public EventKeyValue[] action_params;

            public ProjectAuditorEventWithKeyValues(string name, Analytic analytic, Dictionary<string, string> payload)
            {
                action = name;
                t_since_start = SecondsToMicroseconds(analytic.GetStartTime());
                duration = SecondsToTicks(analytic.GetDurationInSeconds());
                ts = analytic.GetTimestamp();

                // Convert dictionary to a serializable array of key/value pairs
                if (payload != null && payload.Count > 0)
                {
                    action_params = new EventKeyValue[payload.Count];
                    var i = 0;
                    foreach (var kvp in payload)
                    {
                        action_params[i].key = kvp.Key;
                        action_params[i].value = kvp.Value;
                        ++i;
                    }
                }
                else
                {
                    action_params = null;
                }
            }
        }

        [Serializable]
        internal struct IssueStats
        {
            public string id;
            public int numOccurrences;
            public int numHotPathOccurrences;
        }

        [Serializable]
        class ButtonEventWithIssueStats
        {
            public string action;
            public Int64 t_since_start;
            public Int64 duration;
            public Int64 ts;

            public IssueStats[] issue_stats;

            public ButtonEventWithIssueStats(string name, Analytic analytic, IssueStats[] payload)
            {
                action = name;
                t_since_start = SecondsToMicroseconds(analytic.GetStartTime());
                duration = SecondsToTicks(analytic.GetDurationInSeconds());
                ts = analytic.GetTimestamp();
                issue_stats = payload;
            }
        }

        // -------------------------------------------------------------------------------------------------------------

        static string GetEventName(UIButton uiButton)
        {
            switch (uiButton)
            {
                // General UI
                case UIButton.Analyze:
                    return "analyze_button_click";
                case UIButton.Export:
                    return "export_button_click";
                case UIButton.AssemblySelect:
                    return "assembly_button_click";
                case UIButton.AssemblySelectApply:
                    return "assembly_apply";
                case UIButton.AreaSelect:
                    return "area_button_click";
                case UIButton.AreaSelectApply:
                    return "area_apply";
                case UIButton.Mute:
                    return "mute_button_click";
                case UIButton.Unmute:
                    return "unmute_button_click";
                case UIButton.ShowMuted:
                    return "show_muted_checkbox";
                case UIButton.OnlyCriticalIssues:
                    return "only_hotpath_checkbox";
                case UIButton.Load:
                    return "load_button_clicked";
                case UIButton.Save:
                    return "save_button_clicked";

                // High level views
                case UIButton.Summary:
                    return "summary_tab";
                case UIButton.ProjectSettings:
                    return "project_settings_tab";

                // Code issues
                case UIButton.ApiCalls:
                    return "api_tab";
                case UIButton.CodeCompilerMessages:
                    return "compiler_messages_tab";
                case UIButton.Generics:
                    return "generics_tab";
                case UIButton.DomainReload:
                    return "domain_reload_tab";

                // Assets
                case UIButton.Assets:
                    return "assets_tab";
                case UIButton.Shaders:
                    return "shaders_tab";
                case UIButton.ShaderCompilerMessages:
                    return "shader_compiler_messages_tab";
                case UIButton.ShaderVariants:
                    return "shader_variants_tab";
                case UIButton.ComputeShaderVariants:
                    return "compute_shader_variants_tab";
                case UIButton.Materials:
                    return "materials_tab";
                case UIButton.Textures:
                    return "textures_tab";
                case UIButton.SpriteAtlases:
                    return "sprite_atlases_tab";
                case UIButton.AudioClip:
                    return "audio_clip_tab";
                case UIButton.Meshes:
                    return "meshes_tab";
                case UIButton.AnimatorControllers:
                    return "animator_controllers_tab";
                case UIButton.AnimationClips:
                    return "animation_clips_tab";
                case UIButton.Avatars:
                    return "avatars_tab";
                case UIButton.AvatarMasks:
                    return "avatar_masks_tab";

                // Build report
                case UIButton.BuildFiles:
                    return "build_files_tab";
                case UIButton.BuildSteps:
                    return "build_steps_tab";

                // Assemblies
                case UIButton.Assemblies:
                    return "assemblies_tab";
                case UIButton.PrecompiledAssemblies:
                    return "precompiled_assemblies_tab";
                case UIButton.Packages:
                    return "packages_tab";

                default:
                    Debug.LogFormat("SendUIButtonEvent: Unsupported button type : {0}", uiButton);
                    return "";
            }
        }

        static Int64 SecondsToMilliseconds(float seconds)
        {
            return (Int64)(seconds * 1000);
        }

        static Int64 SecondsToTicks(float durationInSeconds)
        {
            return (Int64)(durationInSeconds * 10000);
        }

        static Int64 SecondsToMicroseconds(double seconds)
        {
            return (Int64)(seconds * 1000000);
        }

        // -------------------------------------------------------------------------------------------------------------

        static IssueStats[] CollectSelectionStats(IReadOnlyList<ReportItem> selectedIssues)
        {
            var selectionsDict = new Dictionary<string, IssueStats>();

            foreach (var issue in selectedIssues)
            {
                var id = issue.Id;

                IssueStats summary;
                if (!selectionsDict.TryGetValue(id, out summary))
                {
                    summary = new IssueStats
                    {
                        id = id
                    };
                    selectionsDict[id] = summary;
                }

                selectionsDict[id] = summary;
            }

            var selectionsArray =
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                selectionsDict.Values.OrderByDescending(x => x.numOccurrences).Take(5).ToArray();
#pragma warning restore UA2001

            return selectionsArray;
        }

        static IssueStats[] GetScriptIssuesSummary(Report report)
        {
            var statsDict = new Dictionary<string, IssueStats>();

            var scriptIssues = report.FindByCategory(IssueCategory.Code);
            foreach (var issue in scriptIssues)
            {
                var id = issue.Id;
                IssueStats stats;
                if (!statsDict.TryGetValue(id, out stats))
                {
                    stats = new IssueStats { id = id };
                }

                ++stats.numOccurrences;

                if (issue.IsMajorOrCritical())
                {
                    ++stats.numHotPathOccurrences;
                }

                statsDict[id] = stats;
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return statsDict.Values.OrderByDescending(x => x.numOccurrences).Take(k_MaxIssuesInAnalyzeSummary).ToArray();
#pragma warning restore UA2001
        }

        // -------------------------------------------------------------------------------------------------------------

        public static bool SendEvent(UIButton uiButton, Analytic analytic)
        {
            analytic.End();

            if (s_EnableAnalytics)
            {
                var uiButtonEvent = new ProjectAuditorEvent(GetEventName(uiButton), analytic);
                // TODO
            }
            return false;
        }

        public static bool SendEventWithKeyValues(UIButton uiButton, Analytic analytic, Dictionary<string, string> payload)
        {
            analytic.End();

            if (s_EnableAnalytics)
            {
                var uiButtonEvent = new ProjectAuditorEventWithKeyValues(GetEventName(uiButton), analytic, payload);
                // TODO
            }
            return false;
        }

        public static bool SendEventWithSelectionSummary(UIButton uiButton, Analytic analytic, IReadOnlyList<ReportItem> selectedIssues)
        {
            analytic.End();

            if (s_EnableAnalytics)
            {
                var payload = CollectSelectionStats(selectedIssues);

                var uiButtonEvent = new ButtonEventWithIssueStats(GetEventName(uiButton), analytic, payload);

                // TODO
            }
            return false;
        }

        public static bool SendEventWithAnalyzeSummary(UIButton uiButton, Analytic analytic, Report report)
        {
            analytic.End();

            if (s_EnableAnalytics)
            {
                var payload = GetScriptIssuesSummary(report);

                var uiButtonEvent = new ButtonEventWithIssueStats(GetEventName(uiButton), analytic, payload);

                // TODO
            }
            return false;
        }

        // -------------------------------------------------------------------------------------------------------------
        public class Analytic
        {
            double m_StartTime;
            float m_DurationInSeconds;
            Int64 m_Timestamp;
            bool m_Blocking;

            public Analytic()
            {
                m_StartTime = EditorApplication.timeSinceStartup;
                m_DurationInSeconds = 0;
                m_Timestamp = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                m_Blocking = true;
            }

            public void End()
            {
                m_DurationInSeconds = (float)(EditorApplication.timeSinceStartup - m_StartTime);
            }

            public double GetStartTime()
            {
                return m_StartTime;
            }

            public float GetDurationInSeconds()
            {
                return m_DurationInSeconds;
            }

            public Int64 GetTimestamp()
            {
                return m_Timestamp;
            }

            public bool GetBlocking()
            {
                return m_Blocking;
            }
        }

        public static Analytic BeginAnalytic()
        {
            return new Analytic();
        }
    }
}
