// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define SCENE_TEMPLATE_ANALYTICS_LOGGING
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Analytics;

namespace UnityEditor.SceneTemplate
{
    internal static class SceneTemplateAnalytics
    {
        [Serializable]
        internal class AnalyticDepInfo
        {
            public AnalyticDepInfo(DependencyInfo info)
            {
                dependencyType = info.dependency.GetType().ToString();
                _instantiationMode = info.instantiationMode;
                instantiationMode = Enum.GetName(typeof(TemplateInstantiationMode), info.instantiationMode);
                count = 1;
            }

            internal TemplateInstantiationMode _instantiationMode;
            public string dependencyType;
            public int count;
            public string instantiationMode;
        }

        internal static bool FillAnalyticDepInfos(SceneTemplateAsset template, List<AnalyticDepInfo> infos)
        {
            var hasCloneableDependencies = false;
            var tempDepInfos = new Dictionary<string, List<AnalyticDepInfo>>();
            if (template.dependencies != null)
            {
                foreach (var dep in template.dependencies)
                {
                    if (dep.instantiationMode == TemplateInstantiationMode.Clone)
                        hasCloneableDependencies = true;

                    if (!dep.dependency || dep.dependency == null)
                        continue;

                    var typeName = dep.dependency.GetType().FullName;
                    if (tempDepInfos.TryGetValue(typeName, out var infosPerType))
                    {
                        var foundInfo = infosPerType.Find(info => info._instantiationMode == dep.instantiationMode);
                        if (foundInfo != null)
                        {
                            foundInfo.count++;
                        }
                        else
                        {
                            infosPerType.Add(new AnalyticDepInfo(dep));
                        }
                    }
                    else
                    {
                        infosPerType = new List<AnalyticDepInfo>();
                        infosPerType.Add(new AnalyticDepInfo(dep));
                        tempDepInfos.Add(typeName, infosPerType);
                    }
                }
            }

            foreach (var kvp in tempDepInfos)
            {
                foreach (var depInfo in kvp.Value)
                {
                    infos.Add(depInfo);
                }
            }

            return hasCloneableDependencies;
        }

        internal enum SceneInstantiationType
        {
            NewSceneMenu,
            TemplateDoubleClick,
            Scripting,
            EmptyScene,
            DefaultScene
        }

        [Serializable]
        internal class SceneInstantiationEvent
        {
            private DateTime m_StartTime;

            public long elapsedTimeMs => (long)(DateTime.Now - m_StartTime).TotalMilliseconds;
            public string sceneName;
            public List<AnalyticDepInfo> dependencyInfos = new List<AnalyticDepInfo>();
            public string instantiationType;
            public long duration;
            public bool isCancelled;
            public bool additive;
            public bool hasCloneableDependencies;
            public SceneInstantiationEvent(SceneTemplateAsset template, SceneInstantiationType instantiationType)
            {
                this.instantiationType = Enum.GetName(typeof(SceneInstantiationType), instantiationType);
                sceneName = AssetDatabase.GetAssetPath(template.templateScene);
                hasCloneableDependencies = FillAnalyticDepInfos(template, dependencyInfos);
                m_StartTime = DateTime.Now;
            }

            public SceneInstantiationEvent(SceneInstantiationType instantiationType)
            {
                this.instantiationType = Enum.GetName(typeof(SceneInstantiationType), instantiationType);
                m_StartTime = DateTime.Now;
            }

            public void Done()
            {
                if (duration == 0)
                    duration = elapsedTimeMs;
            }
        }

        internal enum TemplateCreationType
        {
            CreateFromTargetSceneMenu,
            SaveCurrentSceneAsTemplateMenu,
            Scripting
        }

        [Serializable]
        internal class SceneTemplateCreationEvent
        {
            public string sceneName;
            public List<AnalyticDepInfo> dependencyInfos = new List<AnalyticDepInfo>();
            public string templateCreationType;
            public int numberOfTemplatesInProject;
            public bool hasCloneableDependencies;

            public SceneTemplateCreationEvent(SceneTemplateAsset template, TemplateCreationType templateCreationType)
            {
                this.templateCreationType = Enum.GetName(typeof(TemplateCreationType), templateCreationType);
                sceneName = AssetDatabase.GetAssetPath(template.templateScene);
                hasCloneableDependencies = FillAnalyticDepInfos(template, dependencyInfos);
                numberOfTemplatesInProject = SceneTemplateUtils.GetSceneTemplatePaths().Count();
            }
        }

        enum EventName
        {
            SceneInstantiationEvent,
            SceneTemplateCreationEvent
        }

        internal static string Version;
        private static bool s_Registered;

        static SceneTemplateAnalytics()
        {
        }

        internal static void SendSceneInstantiationEvent(SceneInstantiationEvent evt)
        {
            evt.Done();
            Send(EventName.SceneInstantiationEvent, evt);
        }

        internal static void SendSceneTemplateCreationEvent(SceneTemplateCreationEvent evt)
        {
            Send(EventName.SceneTemplateCreationEvent, evt);
        }

        private static bool RegisterEvents()
        {
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
            {
                return false;
            }

            if (!EditorAnalytics.enabled)
            {
                Console.WriteLine("[ST] Editor analytics are disabled");
                return false;
            }

            if (s_Registered)
            {
                return true;
            }

            var allNames = Enum.GetNames(typeof(EventName));
            if (allNames.Any(eventName => !RegisterEvent(eventName)))
            {
                return false;
            }

            s_Registered = true;
            return true;
        }

        private static bool RegisterEvent(string eventName)
        {
            const string vendorKey = "unity.scene-template";
            var result = EditorAnalytics.RegisterEventWithLimit(eventName, 100, 1000, vendorKey);
            switch (result)
            {
                case AnalyticsResult.Ok:
                {
                    return true;
                }
                case AnalyticsResult.TooManyRequests:
                    // this is fine - event registration survives domain reload (native)
                    return true;
                default:
                {
                    Console.WriteLine($"[ST] Failed to register analytics event '{eventName}'. Result: '{result}'");
                    return false;
                }
            }
        }

        private static void Send(EventName eventName, object eventData)
        {
            if (!RegisterEvents())
            {
                return;
            }
            try
            {
                var result = EditorAnalytics.SendEventWithLimit(eventName.ToString(), eventData);
                if (result == AnalyticsResult.Ok)
                {
                }
                else
                {
                    Console.WriteLine($"[ST] Failed to send event {eventName}. Result: {result}");
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

    }
}
