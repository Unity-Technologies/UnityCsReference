// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.SceneTemplate
{
    public enum TemplateInstantiationMode
    {
        Clone,
        Reference
    }

    [Serializable]
    [ExcludeFromPreset]
    [AssetFileNameExtension("scenetemplate")]
    public class SceneTemplateAsset : ScriptableObject
    {
        internal const string extension = "scenetemplate";
        internal const string icon = "Icons/SceneTemplate.png";

        public SceneAsset templateScene;

        public string templateName;

        public string description;

        public Texture2D preview;

        public DependencyInfo[] dependencies;

        public MonoScript templatePipeline;

        public bool isValid => templateScene;

        public bool addToDefaults;

        internal bool hasCloneableDependencies => dependencies.Any(dep => dep.instantiationMode == TemplateInstantiationMode.Clone);

        void OnEnable()
        {
            if (dependencies == null)
                dependencies = new DependencyInfo[0];
        }

        internal void BindScene(SceneAsset scene)
        {
            templateScene = scene;
            dependencies = new DependencyInfo[0];
            UpdateDependencies();
        }

        internal bool UpdateDependencies()
        {
            if (!isValid)
            {
                dependencies = new DependencyInfo[0];
                return false;
            }

            var scenePath = AssetDatabase.GetAssetPath(templateScene.GetInstanceID());
            if (string.IsNullOrEmpty(scenePath))
            {
                dependencies = new DependencyInfo[0];
                return false;
            }

            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            var sceneFolder = Path.GetDirectoryName(scenePath).Replace("\\", "/");
            var sceneCloneableDependenciesFolder = Path.Combine(sceneFolder, sceneName).Replace("\\", "/");

            var depList = new List<Object>();
            ReferenceUtils.GetSceneDependencies(scenePath, depList);

            var newDependenciesAdded = false;

            dependencies = depList.Select(d =>
            {
                var oldDependencyInfo = dependencies.FirstOrDefault(di => di.dependency.GetInstanceID() == d.GetInstanceID());
                if (oldDependencyInfo != null)
                    return oldDependencyInfo;

                newDependenciesAdded = true;

                var depTypeInfo = SceneTemplateProjectSettings.Get().GetDependencyInfo(d);
                var dependencyPath = AssetDatabase.GetAssetPath(d);
                var instantiationMode = depTypeInfo.defaultInstantiationMode;
                if (depTypeInfo.supportsModification && !string.IsNullOrEmpty(dependencyPath))
                {
                    var assetFolder = Path.GetDirectoryName(dependencyPath).Replace("\\", "/");
                    if (assetFolder == sceneCloneableDependenciesFolder)
                    {
                        instantiationMode = TemplateInstantiationMode.Clone;
                    }
                }

                return new DependencyInfo()
                {
                    dependency = d,
                    instantiationMode = instantiationMode
                };
            }).ToArray();

            var isAssetReadOnly = SceneTemplateUtils.IsAssetReadOnly(scenePath);
            if (newDependenciesAdded && !isAssetReadOnly)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }

            return newDependenciesAdded;
        }

        internal void AddThumbnailToAsset(Texture2D thumbnail)
        {
            if (!isValid)
                return;

            var assetPath = AssetDatabase.GetAssetPath(this);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var oldTexture = allAssets.FirstOrDefault(obj => obj is Texture2D);
            if (oldTexture != null)
                AssetDatabase.RemoveObjectFromAsset(oldTexture);

            AssetDatabase.AddObjectToAsset(thumbnail, assetPath);

            // You need to dirty and save the asset if you want the thumbnail to appear
            // in the project browser and the object selector.
            EditorUtility.SetDirty(thumbnail);
            AssetDatabase.SaveAssets();
        }

        internal static bool IsValidPipeline(MonoScript script)
        {
            if (script == null)
                return false;

            // If we just created this script from the button, it won't have any class
            // until a domain reload.
            if (script.GetScriptTypeWasJustCreatedFromComponentMenu())
                return true;

            var scriptType = script.GetClass();
            if (!typeof(ISceneTemplatePipeline).IsAssignableFrom(scriptType))
                return false;

            return true;
        }

        internal ISceneTemplatePipeline CreatePipeline()
        {
            if (!IsValidPipeline(templatePipeline))
                return null;

            var pipelineInstance = Activator.CreateInstance(templatePipeline.GetClass()) as ISceneTemplatePipeline;
            return pipelineInstance;
        }

        internal string GetTemplateScenePath()
        {
            if (!templateScene)
                return null;
            return AssetDatabase.GetAssetPath(templateScene.GetInstanceID());
        }

        [MenuItem("Assets/Create/Scene Template", priority = 201)]
        private static void CreateNewDefaultTemplate()
        {
            var template = CreateInstance<SceneTemplateAsset>();
            ProjectWindowUtil.CreateAsset(template, $"New SceneTemplate.{extension}");
        }
    }

    [Serializable]
    [DebuggerDisplay("{dependency} - {instantiationMode}")]
    public sealed class DependencyInfo
    {
        public Object dependency;
        public TemplateInstantiationMode instantiationMode;
    }
}
