// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define PROFILE_SCENE_TEMPLATE
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;


namespace UnityEditor.SceneTemplate
{
    public sealed class InstantiationResult
    {
        internal InstantiationResult(Scene scene, SceneAsset sceneAsset)
        {
            this.scene = scene;
            this.sceneAsset = sceneAsset;
        }

        public Scene scene { get; internal set; }
        public SceneAsset sceneAsset { get; internal set; }
    }

    struct InMemorySceneState
    {
        public string guid;
        public string path;
        public string rootFolder;
        public bool hasCloneableDependencies;
        public bool hasSubScene;
        public string dependencyFolderName;

        public static InMemorySceneState None = new InMemorySceneState();

        public bool valid => !string.IsNullOrEmpty(guid);

        internal static InMemorySceneState Import(string sessionData)
        {
            if (string.IsNullOrEmpty(sessionData))
                return None;
            return JsonUtility.FromJson<InMemorySceneState>(sessionData);
        }

        internal string Export(bool format = false)
        {
            return JsonUtility.ToJson(this, format);
        }
    }

    internal sealed class ClonePathInfo
    {
        public DependencyInfo dependency;
        public string dependencyPath;
        public string clonePath;

        public ClonePathInfo(DependencyInfo dependency, string dependencyPath)
        {
            this.dependency = dependency;
            this.dependencyPath = dependencyPath;
        }

        public override string ToString()
        {
            return dependencyPath;
        }
    }

    public static class SceneTemplateService
    {
        public delegate void NewTemplateInstantiating(SceneTemplateAsset sceneTemplateAsset, string newSceneOutputPath, bool additiveLoad);
        public delegate void NewTemplateInstantiated(SceneTemplateAsset sceneTemplateAsset, Scene scene, SceneAsset sceneAsset, bool additiveLoad);

        public static event NewTemplateInstantiating newSceneTemplateInstantiating;
        public static event NewTemplateInstantiated newSceneTemplateInstantiated;

        const string k_SceneTemplateServiceBaseSessionKey = "SceneTemplateServiceSession";
        static readonly string k_TempFolderBaseSessionKey = $"{k_SceneTemplateServiceBaseSessionKey}_TempFolder";
        static readonly string k_RegisteredTempFolderSessionKey = $"{k_SceneTemplateServiceBaseSessionKey}_RegisteredTempFolder";
        static readonly string k_InMemorySceneStateSessionKey = $"{k_SceneTemplateServiceBaseSessionKey}_InMemorySceneState";

        static string s_TempFolderBase;
        const string k_MountPoint = "SceneTemplates";
        const string k_InMemoryTempFolder = "InMemory";
        static readonly string k_InMemoryTempFolderGuid = Hash128.Compute("SceneTemplates/InMemory").ToString();
        static InMemorySceneState s_CurrentInMemorySceneState = InMemorySceneState.None;

        // For testing
        internal static bool registeredTempFolder { get; private set; }
        internal static InMemorySceneState currentInMemorySceneState => s_CurrentInMemorySceneState;

        private static void ClearInMemorySceneState()
        {
            s_CurrentInMemorySceneState = InMemorySceneState.None;
        }

        public static InstantiationResult Instantiate(SceneTemplateAsset sceneTemplate, bool loadAdditively, string newSceneOutputPath = null)
        {
            return Instantiate(sceneTemplate, loadAdditively, newSceneOutputPath, SceneTemplateAnalytics.SceneInstantiationType.Scripting);
        }

        public static SceneTemplateAsset CreateSceneTemplate(string sceneTemplatePath)
        {
            return CreateTemplateFromScene(null, sceneTemplatePath, SceneTemplateAnalytics.TemplateCreationType.Scripting);
        }

        public static SceneTemplateAsset CreateTemplateFromScene(SceneAsset sourceSceneAsset, string sceneTemplatePath)
        {
            return CreateTemplateFromScene(sourceSceneAsset, sceneTemplatePath, SceneTemplateAnalytics.TemplateCreationType.Scripting);
        }

        internal static InstantiationResult Instantiate(SceneTemplateAsset sceneTemplate, bool loadAdditively, string newSceneOutputPath, SceneTemplateAnalytics.SceneInstantiationType instantiationType)
        {
            if (!sceneTemplate.isValid)
            {
                throw new Exception("templateScene is empty");
            }

            if (EditorApplication.isUpdating)
            {
                Debug.LogFormat(LogType.Warning, LogOption.None, null, "Cannot instantiate a new scene while updating the editor is disallowed.");
                return null;
            }

            // If we are loading additively, we cannot add a new Untitled scene if another unsaved Untitled scene is already opened
            if (loadAdditively && SceneTemplateUtils.HasSceneUntitled())
            {
                Debug.LogFormat(LogType.Warning, LogOption.None, null, "Cannot instantiate a new scene additively while an unsaved Untitled scene already exists.");
                return null;
            }

            var sourceScenePath = AssetDatabase.GetAssetPath(sceneTemplate.templateScene);
            if (String.IsNullOrEmpty(sourceScenePath))
            {
                throw new Exception("Cannot find path for sceneTemplate: " + sceneTemplate.ToString());
            }

            if (!Application.isBatchMode && !loadAdditively && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return null;

            var instantiateEvent = new SceneTemplateAnalytics.SceneInstantiationEvent(sceneTemplate, instantiationType)
            {
                additive = loadAdditively
            };

            sceneTemplate.UpdateDependencies();
            var hasAnyCloneableDependencies = sceneTemplate.hasCloneableDependencies;

            SceneAsset newSceneAsset = null;
            Scene newScene;

            var templatePipeline = sceneTemplate.CreatePipeline();

            if (!InstantiateInMemoryScene(sceneTemplate, sourceScenePath, ref newSceneOutputPath, out var rootFolder, out var isTempMemory))
            {
                instantiateEvent.isCancelled = true;
                SceneTemplateAnalytics.SendSceneInstantiationEvent(instantiateEvent);
                return null;
            }

            templatePipeline?.BeforeTemplateInstantiation(sceneTemplate, loadAdditively, isTempMemory ? null : newSceneOutputPath);
            newSceneTemplateInstantiating?.Invoke(sceneTemplate, isTempMemory ? null : newSceneOutputPath, loadAdditively);

            newSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(newSceneOutputPath);

            Dictionary<string, string> refPathMap = null;
            var idMap = new Dictionary<int, int>();
            if (hasAnyCloneableDependencies)
            {
                if (!CopyCloneableDependencies(sceneTemplate, newSceneOutputPath, out refPathMap, out var clonedAssets))
                {
                    AssetDatabase.DeleteAsset(newSceneOutputPath);
                    return null;
                }
                idMap.Add(sceneTemplate.templateScene.GetInstanceID(), newSceneAsset.GetInstanceID());
                ReferenceUtils.RemapAssetReferences(refPathMap, idMap);

                foreach (var clone in clonedAssets)
                {
                    if (clone)
                        EditorUtility.SetDirty(clone);
                }
                AssetDatabase.SaveAssets();
            }

            newScene = EditorSceneManager.OpenScene(newSceneOutputPath, loadAdditively ? OpenSceneMode.Additive : OpenSceneMode.Single);

            if (hasAnyCloneableDependencies)
            {
                EditorSceneManager.RemapAssetReferencesInScene(newScene, refPathMap, idMap);
            }

            EditorSceneManager.SaveScene(newScene, newSceneOutputPath);

            if (isTempMemory)
            {
                newSceneAsset = null;
                newScene.SetPathAndGuid("", newScene.guid);
                s_CurrentInMemorySceneState.guid = newScene.guid;
                s_CurrentInMemorySceneState.rootFolder = rootFolder;
                s_CurrentInMemorySceneState.hasCloneableDependencies = hasAnyCloneableDependencies;
                s_CurrentInMemorySceneState.dependencyFolderName = Path.GetFileNameWithoutExtension(newSceneOutputPath);
                s_CurrentInMemorySceneState.hasSubScene = sceneTemplate.dependencies.Any(dep => dep.dependency is SceneAsset);
            }

            SceneTemplateAnalytics.SendSceneInstantiationEvent(instantiateEvent);
            templatePipeline?.AfterTemplateInstantiation(sceneTemplate, newScene, loadAdditively, newSceneOutputPath);
            newSceneTemplateInstantiated?.Invoke(sceneTemplate, newScene, newSceneAsset, loadAdditively);

            SceneTemplateUtils.SetLastFolder(newSceneOutputPath);

            return new InstantiationResult(newScene, newSceneAsset);
        }

        internal static SceneTemplateAsset CreateTemplateFromScene(SceneAsset sourceSceneAsset, string sceneTemplatePath, SceneTemplateAnalytics.TemplateCreationType creationType)
        {
            var sourceScenePath = sourceSceneAsset == null ? null : AssetDatabase.GetAssetPath(sourceSceneAsset);
            if (sourceSceneAsset != null && sourceScenePath != null && String.IsNullOrEmpty(sceneTemplatePath))
            {
                var newSceneAssetName = $"{Path.GetFileNameWithoutExtension(sourceScenePath)}.{SceneTemplateAsset.extension}";
                sceneTemplatePath = Path.Combine(Path.GetDirectoryName(sourceScenePath), newSceneAssetName).Replace("\\", "/");
                sceneTemplatePath = AssetDatabase.GenerateUniqueAssetPath(sceneTemplatePath);
            }

            if (string.IsNullOrEmpty(sceneTemplatePath))
            {
                throw new Exception("No path specified for new Scene template");
            }

            var sceneTemplate = ScriptableObject.CreateInstance<SceneTemplateAsset>();
            AssetDatabase.CreateAsset(sceneTemplate, sceneTemplatePath);

            if (!String.IsNullOrEmpty(sourceScenePath))
            {
                if (SceneManager.GetActiveScene().path == sourceScenePath && SceneManager.GetActiveScene().isDirty)
                {
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                }

                sceneTemplate.BindScene(sourceSceneAsset);
            }

            EditorUtility.SetDirty(sceneTemplate);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(sceneTemplatePath);

            var sceneCreationEvent = new SceneTemplateAnalytics.SceneTemplateCreationEvent();
            sceneCreationEvent.SetData(sceneTemplate, creationType);
            SceneTemplateAnalytics.SendSceneTemplateCreationEvent(sceneCreationEvent);

            SceneTemplateUtils.SetLastFolder(sceneTemplatePath);

            Selection.SetActiveObjectWithContext(sceneTemplate, null);

            return sceneTemplate;
        }

        internal static MonoScript CreateNewSceneTemplatePipeline(string folder)
        {
            var path = EditorUtility.SaveFilePanelInProject(L10n.Tr("Create new Scene Template Pipeline"),
                "NewSceneTemplatePipeline", "cs",
                L10n.Tr("Please enter a file name for the new Scene Template Pipeline."),
                folder);
            return CreateNewSceneTemplatePipelineAtPath(path);
        }

        internal static MonoScript CreateNewSceneTemplatePipelineAtPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var templatePath = AssetsMenuUtility.GetScriptTemplatePath(ScriptTemplate.CSharp_NewSceneTemplatePipelineScript);
            var scriptAsset = ProjectWindowUtil.CreateScriptAssetFromTemplate(filePath, templatePath) as MonoScript;
            scriptAsset?.SetScriptTypeWasJustCreatedFromComponentMenu();
            AssetDatabase.Refresh();
            return scriptAsset;
        }

        static bool InstantiateInMemoryScene(SceneTemplateAsset sceneTemplate, string sourceScenePath, ref string newSceneOutputPath, out string rootFolder, out bool isTempMemory)
        {
            isTempMemory = false;
            if (string.IsNullOrEmpty(newSceneOutputPath))
            {
                if (!registeredTempFolder)
                    RegisterInMemoryTempFolder();

                var instanceName = "Untitled.unity";
                var instancePath = $"{k_MountPoint}/{k_InMemoryTempFolder}/{Guid.NewGuid():N}/{instanceName}";
                newSceneOutputPath = instancePath;
                isTempMemory = true;
            }

            if (Path.IsPathRooted(newSceneOutputPath))
                newSceneOutputPath = FileUtil.GetProjectRelativePath(newSceneOutputPath);

            rootFolder = Path.GetDirectoryName(newSceneOutputPath);
            if (rootFolder != null && !Directory.Exists(rootFolder))
                Directory.CreateDirectory(rootFolder);

            if (!AssetDatabase.CopyAsset(sourceScenePath, newSceneOutputPath))
            {
                Debug.LogError($"Could not copy scene \"{sourceScenePath}\" to \"{newSceneOutputPath}\"");
                return false;
            }

            return true;
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            RegisterSceneEventListeners();
            RegisterApplicationEvent();
        }

        static void RegisterApplicationEvent()
        {
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += AfterAssemblyReload;
        }

        static void BeforeAssemblyReload()
        {
            StoreSessionState();
            UnregisterInMemoryTempFolder();
        }

        static void AfterAssemblyReload()
        {
            var restored = RestoreSessionState();
            if (restored && registeredTempFolder)
                RegisterInMemoryTempFolder(s_TempFolderBase);
        }

        static void UpdateSceneTemplatesOnSceneSave(Scene scene)
        {
            var infos = SceneTemplateUtils.GetSceneTemplateInfos();
            foreach (var sceneTemplateInfo in infos)
            {
                if (sceneTemplateInfo.IsInMemoryScene || sceneTemplateInfo.isReadonly || sceneTemplateInfo.sceneTemplate == null)
                    continue;

                var scenePath = sceneTemplateInfo.sceneTemplate.GetTemplateScenePath();
                if (string.IsNullOrEmpty(scenePath))
                    continue;
                if (!scene.path.Equals(scenePath, StringComparison.Ordinal))
                    continue;
                sceneTemplateInfo.sceneTemplate.UpdateDependencies();
            }
        }

        private static void RegisterSceneEventListeners()
        {
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.newSceneCreated += OnNewSceneCreated;

            // We can't really rely on sceneClosed, as this event happens just before
            // a domain reload when entering playmode, and we don't really want
            // to lose the current in memory state at this point.
            // EditorSceneManager.sceneClosed += OnSceneClosed;
        }

        static void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            if (!s_CurrentInMemorySceneState.valid || mode != NewSceneMode.Single)
                return;
            if (s_CurrentInMemorySceneState.guid != scene.guid)
                ClearInMemorySceneState();
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!s_CurrentInMemorySceneState.valid || mode != OpenSceneMode.Single)
                return;

            if (s_CurrentInMemorySceneState.guid != scene.guid)
                ClearInMemorySceneState();
        }

        static void OnSceneSaving(Scene scene, string path)
        {
            if (scene.guid == s_CurrentInMemorySceneState.guid)
                s_CurrentInMemorySceneState.path = path;
        }

        static void OnSceneSaved(Scene scene)
        {
            UpdateSceneTemplatesOnSceneSave(scene);

            if (!s_CurrentInMemorySceneState.valid)
                return;

            if (scene.path != s_CurrentInMemorySceneState.path)
                return;

            HandleSceneSave(scene);
            ClearInMemorySceneState();
        }

        static void HandleSceneSave(Scene scene)
        {
            {
                var result = scene.path;

                var directory = Path.GetDirectoryName(result);
                var filenameWithoutExt = Path.GetFileNameWithoutExtension(result);

                if (s_CurrentInMemorySceneState.hasCloneableDependencies)
                {
                    var oldDependencyFolder = Path.Combine(s_CurrentInMemorySceneState.rootFolder, s_CurrentInMemorySceneState.dependencyFolderName);
                    var newDependencyFolder = Path.Combine(directory, filenameWithoutExt);
                    if (Directory.Exists(newDependencyFolder))
                        newDependencyFolder = AssetDatabase.GenerateUniqueAssetPath(newDependencyFolder);
                    var errorMsg = AssetDatabase.MoveAsset(oldDependencyFolder, newDependencyFolder);
                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        Debug.LogError(errorMsg);
                        return;
                    }
                }

                SceneTemplateUtils.DeleteAsset(s_CurrentInMemorySceneState.rootFolder);
                if (s_CurrentInMemorySceneState.hasSubScene)
                {
                    EditorApplication.delayCall += () =>
                    {
                        var success = EditorSceneManager.ReloadScene(scene);
                        if (!success)
                        {
                            Debug.LogError($"Failed to reload scene {scene.path}");
                        }
                        ClearInMemorySceneState();
                    };
                }
                else
                {
                    var success = EditorSceneManager.ReloadScene(scene);
                    if (!success)
                    {
                        Debug.LogError($"Failed to reload scene {scene.path}");
                    }
                    ClearInMemorySceneState();
                }
            }
        }

        private static bool CopyCloneableDependencies(SceneTemplateAsset sceneTemplate, string newSceneOutputPath, out Dictionary<string, string> refPathMap, out List<Object> clonedAssets)
        {
            clonedAssets = new List<Object>();
            refPathMap = new Dictionary<string, string>();
            var outputSceneFileName = Path.GetFileNameWithoutExtension(newSceneOutputPath);
            var outputSceneDirectory = Path.GetDirectoryName(newSceneOutputPath);
            var dependencyFolder = Path.Combine(outputSceneDirectory, outputSceneFileName);
            var needsCleanup = false;
            var createdDirectory = false;
            if (!Directory.Exists(dependencyFolder))
            {
                Directory.CreateDirectory(dependencyFolder);
                createdDirectory = true;
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                var dependencyPaths = sceneTemplate.dependencies
                    .Where(d => d.instantiationMode == TemplateInstantiationMode.Clone)
                    .Select(d => new ClonePathInfo(d, AssetDatabase.GetAssetPath(d.dependency))).ToArray();

                var nullPath = dependencyPaths.FirstOrDefault(d => string.IsNullOrEmpty(d.dependencyPath));
                if (nullPath != null && nullPath.dependency != null)
                {
                    Debug.LogError("Cannot find dependency path for: " + nullPath.dependency);
                    if (createdDirectory)
                        Directory.Delete(dependencyFolder);
                    return false;
                }

                // Gather all dependencies. Extract their name. For duplicate clone paths format a new name including the former path (without Assets/).
                var clonePathToInfos = SetupClonePathInfos(dependencyFolder, dependencyPaths);
                refPathMap = clonePathToInfos.ToDictionary(d => d.dependencyPath, d => d.clonePath);

                if (!AssetDatabase.CopyAssets(refPathMap.Keys.ToArray(), refPathMap.Values.ToArray()))
                {
                    Debug.LogError("Failed to copy dependencies");
                    needsCleanup = true;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            if (needsCleanup)
            {
                var failedPaths = new List<string>();
                if (!AssetDatabase.DeleteAssets(refPathMap.Values.Where(path => File.Exists(path)).ToArray(), failedPaths))
                    Debug.LogError("Failed to clean copied dependencies: \n" + string.Join('\n', failedPaths));
                if (createdDirectory)
                    Directory.Delete(dependencyFolder);
                return false;
            }

            foreach (var clonedDepPath in refPathMap.Values)
            {
                var clonedDependency = AssetDatabase.LoadMainAssetAtPath(clonedDepPath);
                if (clonedDependency == null || !clonedDependency)
                {
                    Debug.LogError("Cannot load cloned dependency path at: " + clonedDepPath);
                    continue;
                }
                clonedAssets.Add(clonedDependency);
            }

            return true;
        }

        internal static IEnumerable<ClonePathInfo> SetupClonePathInfos(string dependencyFolder, IEnumerable<ClonePathInfo> infos)
        {
            var clonePathToInfo = new Dictionary<string, ClonePathInfo>();
            foreach (var depInfo in infos)
            {
                var clonedDepName = Path.GetFileName(depInfo.dependencyPath);
                depInfo.clonePath = Path.Combine(dependencyFolder, clonedDepName).Replace("\\", "/");
                if (clonePathToInfo.TryGetValue(depInfo.clonePath, out var existing))
                {
                    if (existing.clonePath == depInfo.clonePath)
                    {
                        existing.clonePath = CreateUniqueAssetName(dependencyFolder, existing.dependencyPath);
                    }
                    depInfo.clonePath = CreateUniqueAssetName(dependencyFolder, depInfo.dependencyPath);
                }
                clonePathToInfo[depInfo.clonePath] = depInfo;
            }
            return clonePathToInfo.Values;
        }

        internal static string CreateUniqueAssetName(string folder, string path)
        {
            var tokens = path.Split('/');
            var uniqueName = string.Join("_", tokens.Skip(1));
            return Path.Combine(folder, uniqueName).Replace("\\", "/");
        }

        static void RegisterInMemoryTempFolder()
        {
            if (registeredTempFolder)
                return;

            RegisterInMemoryTempFolder(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        }

        static void RegisterInMemoryTempFolder(string tempFolderBase)
        {
            s_TempFolderBase = tempFolderBase;
            var inMemoryFolderPath = Path.Combine(s_TempFolderBase, k_MountPoint, k_InMemoryTempFolder);
            if (!Directory.Exists(inMemoryFolderPath))
                Directory.CreateDirectory(inMemoryFolderPath);
            AssetDatabase.RegisterRedirectedAssetFolder(k_MountPoint, k_InMemoryTempFolder, inMemoryFolderPath, false, k_InMemoryTempFolderGuid);
            AssetDatabase.Refresh();
            registeredTempFolder = true;
        }

        static void UnregisterInMemoryTempFolder()
        {
            if (!registeredTempFolder)
                return;

            AssetDatabase.UnregisterRedirectedAssetFolder(k_MountPoint, k_InMemoryTempFolder);
            registeredTempFolder = false;
        }

        static void StoreSessionState()
        {
            SessionState.SetString(k_TempFolderBaseSessionKey, s_TempFolderBase);
            SessionState.SetBool(k_RegisteredTempFolderSessionKey, registeredTempFolder);

            var json = s_CurrentInMemorySceneState.Export();
            SessionState.SetString(k_InMemorySceneStateSessionKey, json);
        }

        static bool RestoreSessionState()
        {
            var json = SessionState.GetString(k_InMemorySceneStateSessionKey, null);
            if (string.IsNullOrEmpty(json))
                return false;
            s_CurrentInMemorySceneState = InMemorySceneState.Import(json);

            s_TempFolderBase = SessionState.GetString(k_TempFolderBaseSessionKey, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            registeredTempFolder = SessionState.GetBool(k_RegisteredTempFolderSessionKey, false);

            // To make sure that we don't affect future session in case something happens, we delete everything
            SessionState.EraseString(k_InMemorySceneStateSessionKey);
            SessionState.EraseString(k_TempFolderBaseSessionKey);
            SessionState.EraseBool(k_RegisteredTempFolderSessionKey);

            return true;
        }

        #region MenuActions
        [MenuItem("File/Save As Scene Template...", false, 172)]
        private static void SaveTemplateFromCurrentScene()
        {
            var currentScene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(currentScene.path))
            {
                var suggestedScenePath = SceneTemplateUtils.SaveFilePanelUniqueName(L10n.Tr("Save scene"), "Assets", "newscene", "unity");
                if (string.IsNullOrEmpty(suggestedScenePath) || !EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), suggestedScenePath))
                    return;
            }

            var sceneTemplateFile = SceneTemplateUtils.SaveFilePanelUniqueName(L10n.Tr("Save scene"), Path.GetDirectoryName(currentScene.path), Path.GetFileNameWithoutExtension(currentScene.path), SceneTemplateAsset.extension);
            if (string.IsNullOrEmpty(sceneTemplateFile))
                return;

            var sourceSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScene.path);
            CreateTemplateFromScene(sourceSceneAsset, sceneTemplateFile, SceneTemplateAnalytics.TemplateCreationType.SaveCurrentSceneAsTemplateMenu);
        }

        [CommandHandler("Menu/File/NewSceneTemplate")]
        private static void NewSceneDialog(CommandExecuteContext context)
        {
            FileNewScene();
        }

        internal static void FileNewScene()
        {
            if (SceneTemplateProjectSettings.Get().newSceneOverride == SceneTemplateProjectSettings.NewSceneOverride.BuiltinScene)
            {
                EditorApplication.FileMenuNewScene();
            }
            else if (!EditorApplication.isPlaying)
            {
                SceneTemplateDialog.ShowWindow();
            }
            else
            {
                Debug.LogWarning("Cannot open the New Scene dialog while playing.");
            }
        }

        [CommandHandler("Menu/File/InstantiateDefaultScene")]
        static void InstantiateDefaultScene(CommandExecuteContext context)
        {
            if (SceneTemplatePreferences.Get().newDefaultSceneOverride == SceneTemplatePreferences.NewDefaultSceneOverride.DefaultBuiltin)
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                return;
            }

            var templateInfos = SceneTemplateUtils.GetSceneTemplateInfos();
            var templateInfo = templateInfos.FirstOrDefault(info => info.isPinned && !info.IsInMemoryScene);
            if (templateInfo == null)
                templateInfo = templateInfos.FirstOrDefault(info => !info.isPinned && !info.IsInMemoryScene);

            if (templateInfo != null && templateInfo.sceneTemplate)
            {
                Instantiate(templateInfo.sceneTemplate, false);
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        [MenuItem("Assets/Create/Scene/Scene Template From Scene", priority = 3, secondaryPriority = 2)]
        private static void CreateTemplateFromScene()
        {
            var sourceSceneAsset = Selection.activeObject as SceneAsset;
            if (sourceSceneAsset == null)
                return;

            CreateTemplateFromScene(sourceSceneAsset, null, SceneTemplateAnalytics.TemplateCreationType.CreateFromTargetSceneMenu);
        }

        [MenuItem("Assets/Create/Scene/Scene Template From Scene", true)]
        private static bool ValidateCreateTemplateFromScene()
        {
            return Selection.activeObject is SceneAsset;
        }

        #endregion
    }
}
