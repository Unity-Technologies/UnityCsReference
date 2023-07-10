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
        const string vendorKey = "unity.scene-template";

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
            if (template.dependencies == null)
                return false;

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
        internal class SceneInstantiationEvent : IAnalytic.IData
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

        [AnalyticInfo(eventName: "SceneInstantiationEvent", vendorKey: vendorKey)]
        internal class SceneInstantiationEventAnalytic : IAnalytic
        {
            public SceneInstantiationEventAnalytic(SceneInstantiationEvent data)
            {
                m_data = data;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }

            private SceneInstantiationEvent m_data = null;
        }

        internal enum TemplateCreationType
        {
            CreateFromTargetSceneMenu,
            SaveCurrentSceneAsTemplateMenu,
            Scripting
        }

        [Serializable]
        internal class SceneTemplateCreationEvent : IAnalytic.IData
        {
            public string sceneName;
            public List<AnalyticDepInfo> dependencyInfos = new List<AnalyticDepInfo>();
            public string templateCreationType;
            public int numberOfTemplatesInProject;
            public bool hasCloneableDependencies;

            public void SetData(SceneTemplateAsset template, TemplateCreationType templateCreationType)
            {
                this.templateCreationType = Enum.GetName(typeof(TemplateCreationType), templateCreationType);
                sceneName = AssetDatabase.GetAssetPath(template.templateScene);
                hasCloneableDependencies = FillAnalyticDepInfos(template, dependencyInfos);
                numberOfTemplatesInProject = SceneTemplateUtils.GetSceneTemplates().Count();
            }
        }

        [AnalyticInfo(eventName: "SceneTemplateCreationEvent", vendorKey: vendorKey)]
        internal class SceneTemplateCreationEventAnalytic : IAnalytic
        {
            public SceneTemplateCreationEventAnalytic(SceneTemplateCreationEvent data)
            {
                m_data = data;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }

            private SceneTemplateCreationEvent m_data = null;
        }

        internal static string Version;
  
        static SceneTemplateAnalytics()
        {
        }

        internal static void SendSceneInstantiationEvent(SceneInstantiationEvent evt)
        {
            evt.Done();
            SceneInstantiationEventAnalytic analytic = new SceneInstantiationEventAnalytic(evt);
            EditorAnalytics.SendAnalytic(analytic);
            
        }

        internal static void SendSceneTemplateCreationEvent(SceneTemplateCreationEvent evt)
        {
            SceneTemplateCreationEventAnalytic analytic = new SceneTemplateCreationEventAnalytic(evt);
            EditorAnalytics.SendAnalytic(analytic);
        }


    }
}
