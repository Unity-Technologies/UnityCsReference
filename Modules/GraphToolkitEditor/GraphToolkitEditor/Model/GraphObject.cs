// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for graph assets. Uses Unity serialization by default.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract class GraphObject : ScriptableObject, ISerializationCallbackReceiver, IObjectClonedCallbackReceiver
    {
        static byte[] s_ComputeFileHashBuffer = new byte[1024];

        [SerializeReference, HideInInspector]
        GraphModel m_GraphModel;

        // it is important that these five fields are serialized on domain reload. They have not to be serialized in the asset file. Luckily private fields without [SerializeField] are serialized on domain reload.
        GUID m_AssetGuid;
        bool m_Dirty;
        long m_LastWriteTime;
        long m_LastWriteSize;
        Hash128 m_LastWriteHash;
        // Compatibility for motion
        bool m_IsSubAsset = false;

        internal bool IsSubAsset => m_IsSubAsset;

        internal bool IsLocalSubgraphMigrated;

        /// <summary>
        /// The graph model stored in the asset.
        /// </summary>
        public GraphModel GraphModel => m_GraphModel;

        /// <summary>
        /// The type of the graph stored in the asset.
        /// </summary>
        /// <see cref="Graph"/>
        public virtual Type GraphType { get; internal set; }

        /// <summary>
        /// The GUID of the asset file used to store this graph. If the asset is not stored in a file, returns default(GUID)
        /// </summary>
        public virtual GUID AssetFileGuid
        {
            get
            {
                if (m_AssetGuid == default)
                {
                    string filePath = AssetDatabase.GetAssetPath(this);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        return AssetDatabase.GUIDFromAssetPath(filePath);
                    }
                }
                return m_AssetGuid;
            }
        }

        /// <summary>
        /// The path on disk of the graph object.
        /// </summary>
        public string FilePath
        {
            get
            {
                var guid = AssetFileGuid;
                if (guid == default)
                    return null;
                return AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        /// <summary>
        /// The dirty state of the asset (true if it needs to be saved)
        /// </summary>
        public bool Dirty
        {
            get => IsSaveAndLoadManagedByAssetDatabase ? EditorUtility.IsDirty(this) : m_Dirty;
            set
            {
                if (IsSaveAndLoadManagedByAssetDatabase)
                {
                    if (value)
                    {
                        EditorUtility.SetDirty(this);
                    }
                    else
                    {
                        EditorUtility.ClearDirty(this);
                    }
                }
                else
                {
                    m_Dirty = value;
                }
            }
        }

        internal bool IsSaveAndLoadManagedByAssetDatabase
        {
            get
            {
                if (m_AssetGuid == default)
                {
                    return AssetDatabase.IsNativeAsset(this);
                }

                return false;
            }
        }

        /// <summary>
        /// Initializes <see cref="GraphModel"/> to a new graph.
        /// </summary>
        /// <param name="graphModelType">The type of <see cref="GraphModel"/> to create.</param>
        public virtual void CreateMainGraph(Type graphModelType)
        {
            InitializeMainGraphModel(graphModelType);
        }

        /// <summary>
        /// Retrieves a graph by its guid. The graphs searched are the main graph plus the local sub graphs, recursively.
        /// </summary>
        /// <param name="guid">The guid of the graph to retrieve.</param>
        /// <returns>The graph model with the given guid, or null if not found.</returns>
        public virtual GraphModel GetGraphModelByGuid(Hash128 guid)
        {
            if (m_GraphModel != null)
            {
                if (m_GraphModel.Guid == guid)
                {
                    return m_GraphModel;
                }

                return m_GraphModel.GetGraphModelByGuid(guid);
            }

            return null;
        }

        /// <summary>
        /// Creates a file to store the asset and binds the graph object to it.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="overwriteIfExists">If there is already a file at <paramref name="path"/>, the file will be overwritten if this parameter is true. If the parameter is false, a unique path will be generated for the file.</param>
        /// <returns>The file path.</returns>
        public virtual string AttachToAssetFile(string path, bool overwriteIfExists)
        {
            if (path != null)
            {
                if (!overwriteIfExists)
                {
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                }

                var invalidPathChars = EditorBridge.GetInvalidFilenameChars();
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    var directories = directory.Split(Path.DirectorySeparatorChar);
                    for (var i = 0; i < directories.Length; i++)
                    {
                        if (directories[i].IndexOfAny(invalidPathChars) != -1)
                        {
                            throw new ArgumentException(
                                $"The assetPath {path} contains invalid characters. Invalid characters are {new string(invalidPathChars)} . Remove those characters from the assetPath");
                        }
                    }
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(path))
                    AssetDatabase.DeleteAsset(path);

                DoCreateAssetFile(path);

                var extension = Path.GetExtension(path);
                if (!string.IsNullOrEmpty(extension) && GraphObjectFactory.KnowsExtension(extension))
                    GraphToolkitAnalytics.SendGraphToolCreatedEvent(extension);
            }

            return path;
        }

        /// <summary>
        /// Loads a <see cref="GraphObject"/> at the given path.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <typeparam name="T">The expected type, derived from <see cref="GraphObject"/>.</typeparam>
        /// <returns>A <see cref="GraphObject"/> of type <typeparamref name="T"/> or null if the asset couldn't be loaded.</returns>
        /// <remarks>This will work with foreign assets that are declared with <see cref="GraphObjectDefinitionAttribute"/> or native assets if the <paramref name="filePath"/> has a ".asset" extension.</remarks>
        public static T LoadGraphObjectAtPath<T>(string filePath) where T : GraphObject
        {
            return LoadGraphObjectAtPath(filePath, typeof(T)) as T;
        }

        /// <summary>
        /// Loads a <see cref="GraphObject"/> at the given path.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="graphObjectType">The expected type, derived from <see cref="GraphObject"/>.</param>
        /// <returns>A <see cref="GraphObject"/> of type <paramref name="graphObjectType"/> or null if the asset couldn't be loaded.</returns>
        /// <remarks>This will work with foreign assets that are declared with <see cref="GraphObjectDefinitionAttribute"/> or native assets if the <paramref name="filePath"/> has a ".asset" extension.</remarks>
        public static GraphObject LoadGraphObjectAtPath(string filePath, Type graphObjectType = null)
        {
            var asset = GraphObjectFactory.LoadGraphObjectAtPath(filePath, graphObjectType, false);

            return asset;
        }

        /// <summary>
        /// Loads a <see cref="GraphObject"/> at the given path into a new copy, whether or it is already existing in memory.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="graphObjectType">The expected type, derived from <see cref="GraphObject"/>.</param>
        /// <returns>A <see cref="GraphObject"/> of type <paramref name="graphObjectType"/> or null if the asset couldn't be loaded.</returns>
        /// <remarks>The generated asset must be explicitly destroyed. Importers should use this method when loading a <see cref="GraphObject"/> during the import process.</remarks>
        public static GraphObject LoadGraphObjectCopyAtPathAndForget(string filePath, Type graphObjectType = null)
        {
            var asset = GraphObjectFactory.LoadGraphObjectAtPath(filePath, graphObjectType, true);

            return asset;
        }

        /// <summary>
        /// Default implementation of loading a graph object from disk.
        /// </summary>
        /// <param name="filePath">The file path where the <see cref="GraphObject"/> is.</param>
        /// <typeparam name="T">The type of <see cref="GraphObject"/> expected. </typeparam>
        /// <returns>The <see cref="GraphObject"/> loaded at the give <paramref name="filePath"/> or a new <see cref="GraphObject"/> of type T.</returns>
        /// <remarks>This will be used unless the file has a ".asset" extension or a derived type define a static "LoadGraphObjectFromFileOnDisk" method.</remarks>
        /// <seealso cref="GraphObjectDefinitionAttribute"/>
        public static GraphObject DefaultLoadGraphObjectFromFileOnDisk<T>(string filePath) where T : GraphObject
        {
            var objects = InternalEditorUtility.LoadSerializedFileAndForget(filePath);

            foreach (var obj in objects)
            {
                if (obj is T graphObject)
                {
                    return graphObject;
                }
            }

            return CreateInstance<T>();
        }

        void DoCreateAssetFile(string filePath)
        {
            if (GraphObjectFactory.FilePathHasNativeAssetExtension(filePath))
            {
                AssetDatabase.CreateAsset(this, filePath);
                Save();
            }
            else
            {
                // We need to write the asset on disk and then import it to get a valid GUID from the AssetDatabase.
                try
                {
                    SaveGraphObjectToFileOnDisk(filePath);
                }
                catch (IOException e)
                {
                    Debug.LogError($"Error while saving new asset: {filePath}. {e.Message}");
                    return;
                }

                // Grouping these statements in an AssetEditingScope allows them to be run in a batch without interruption.
                // This is important because importing the asset can trigger postprocessors (in particular the WindowAssetPostprocessingWatcher)
                // which in turn might load and register another instance of this graph object. This will cause the below call to GraphObjectFactory.RegisterNewGraphObject()
                // to log a warning because it (rightfully) expects that a graph object at this given file path should not have already been registered.
                using (new AssetDatabase.AssetEditingScope())
                {
                    AssetDatabase.ImportAsset(filePath);
                    var guid = AssetDatabase.GUIDFromAssetPath(filePath);

                    if (guid != default)
                    {
                        GraphObjectFactory.RegisterNewGraphObject(this, guid);
                        AfterLoadForeignAsset(guid);
                    }

                    Dirty = false;
                }
            }
        }

        /// <summary>
        /// Saves the asset to the file if dirty.
        /// </summary>
        /// <seealso cref="OnBeforeSavingGraphObject"/>
        /// <seealso cref="OnGraphObjectSaved"/>
        public virtual void Save()
        {
            if (!OnBeforeSavingGraphObject())
                return;

            if (Dirty == false)
                return;

            // AssetFileGuid != default if and only if asset has an ADB asset file and is not just in memory.
            if (AssetFileGuid != default)
            {
                if (IsSaveAndLoadManagedByAssetDatabase)
                {
                    AssetDatabase.SaveAssetIfDirty(AssetFileGuid);
                }
                else
                {
                    string filePath = FilePath;
                    try
                    {
                        SaveGraphObjectToFileOnDisk(filePath);
                    }
                    catch (IOException e)
                    {
                        Debug.LogError($"Error while saving asset: {filePath}. {e.Message}");
                        return;
                    }
                    UpdateFileSystemInfos();
                    Dirty = false;
                    AssetDatabase.ImportAsset(filePath);
                }
                OnGraphObjectSaved();
            }
        }

        /// <summary>
        /// Save this <see cref="GraphObject"/> on disk at the given path. Will only be called for foreign assets.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <remarks>The AssetFileGuid is not updated by this method and might not yet be valid. Default implementation is symmetric with <see cref="DefaultLoadGraphObjectFromFileOnDisk{T}"/>.</remarks>
        protected virtual void SaveGraphObjectToFileOnDisk(string path)
        {
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { this }, path, true);
        }

        /// <summary>
        /// Creates a copy of the <see cref="GraphObject"/> along with all associated <see cref="ScriptableObject"/>s that require cloning.
        /// </summary>
        /// <returns>
        /// The cloned objects, the first element being the clone of the graph itself.
        /// </returns>
        public Object[] Clone()
        {
            var originalToCloneMap = new Dictionary<Object, Object>();
            var clonedObjects = new List<Object>();

            CloneAssets(clonedObjects, originalToCloneMap);

            foreach (var clonedObject in clonedObjects)
            {
                if (clonedObject is IObjectClonedCallbackReceiver asset)
                    asset.OnAfterAssetClone(originalToCloneMap);
            }

            return clonedObjects.ToArray();
        }

        /// <inheritdoc />
        public virtual void CloneAssets(List<Object> clones, Dictionary<Object, Object> originalToCloneMap)
        {
            var assetClone = Instantiate(this);
            assetClone.name = name; // don't add "(Clone)" suffix to the name

            originalToCloneMap[this] = assetClone;
            clones.Add(assetClone);

            GraphModel.CloneAssets(clones, originalToCloneMap);
        }

        /// <inheritdoc />
        public virtual void OnAfterAssetClone(IReadOnlyDictionary<Object, Object> originalToCloneMap)
        {
            GraphModel.OnAfterAssetClone(originalToCloneMap);
        }

        /// <summary>
        /// Implementation of OnEnable event function.
        /// </summary>
        /// <remarks>Overrides to this method must call this method.</remarks>
        protected virtual void OnEnable()
        {
            m_GraphModel?.OnEnable();

            // OnEnable is called when the AssetDatabase reloads the asset (e.g. when source control reverts the asset on disk).
            // OnEnable is also called when entering play mode, but we don't want to reload the graph in that case.
            if ((!EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying) && IsSaveAndLoadManagedByAssetDatabase)
            {
                var assetFileGuid = AssetFileGuid;
                if (assetFileGuid != default)
                {
                    var windows = Resources.FindObjectsOfTypeAll<GraphViewEditorWindow>();
                    foreach (var window in windows)
                    {
                        if (window.GraphTool?.ToolState == null)
                            continue;

                        if (window.GraphTool.ToolState.CurrentGraph.RefersToFile(assetFileGuid) && window.GraphTool.ToolState.GraphModel != null)
                        {
                            var graphModel = window.GraphTool.ToolState.GraphModel;
                            using var toolStateUpdater = window.GraphTool.ToolState.UpdateScope;
                            toolStateUpdater.LoadGraph(graphModel, null);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Implementation of OnDisable event function.
        /// </summary>
        /// <remarks>Overrides to this method must call this method.</remarks>
        protected virtual void OnDisable()
        {
            if (m_GraphModel == null)
                return;

            m_GraphModel.OnDisable();
        }

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
        }

        /// <inheritdoc />
        public virtual void OnAfterDeserialize()
        {
            m_GraphModel?.SetGraphObject(this);
            UndoStateRecorder.PerformCompleteRestore();
        }

        /// <summary>
        /// Create a new instance of a <see cref="GraphModel"/> of the given type.
        /// </summary>
        /// <param name="graphModelType">A type derived from <see cref="GraphModel"/>.</param>
        /// <returns>A new instance of a <see cref="GraphModel"/> of the given type.</returns>
        public GraphModel CreateGraphModel(Type graphModelType)
        {
            Debug.Assert(typeof(GraphModel).IsAssignableFrom(graphModelType));
            var graphModel = (GraphModel)Activator.CreateInstance(graphModelType);
            graphModel?.SetGraphObject(this);
            return graphModel;
        }

        /// <summary>
        /// If the asset is a foreign asset, destroy it.
        /// </summary>
        /// <remarks>Native assets lifetime is managed by the AssetDatabase and should not be destroyed directly.</remarks>
        public virtual void DestroyObjects()
        {
            if (!IsSaveAndLoadManagedByAssetDatabase)
            {
                DestroyImmediate(this);
            }
        }

        /// <summary>
        ///  Called after <see cref="LoadGraphObjectAtPath"/> has successfully loaded this <see cref="GraphObject"/> from disk.
        /// </summary>
        /// <remarks>It will not be called if the <see cref="GraphObject"/> is already loaded.</remarks>
        protected virtual void OnAfterLoad() { }

        /// <summary>
        /// Called after the graph object has been saved.
        /// </summary>
        protected virtual void OnGraphObjectSaved() { }

        /// <summary>
        /// Called before the graph object will be saved.
        /// </summary>
        /// <returns>True if you want the save to continue. False if you want the save to do nothing.</returns>
        protected virtual bool OnBeforeSavingGraphObject() => true;

        /// <summary>
        /// Unload the current object from memory. It will be reloaded from disk on the next load.
        /// </summary>
        public void UnloadObject()
        {
            if (IsSaveAndLoadManagedByAssetDatabase)
                Resources.UnloadAsset(this);
            else
                DestroyObjects();
        }

        /// <summary>
        /// Saves all currently dirty foreign <see cref="GraphObject"/>s.
        /// </summary>
        /// <remarks>Call <see cref="AssetDatabase.SaveAssets"/> to save native <see cref="GraphObject"/>s.</remarks>
        public static void SaveAllGraphs()
        {
            GraphObjectFactory.SaveAllGraphs();
        }

        /// <summary>
        /// Detaches the subgraphs sub assets from the asset file.
        /// </summary>
        internal void RemoveObsoleteSubgraphAssets()
        {
            //This shouldn't be called in OnEnable because sometimes unity deserialize and call OnEnable on the assets multiple times in Unity 6 while keeping the removed sub assets removed.
            //In that case the sub assets would be removed the first time and at the second migration they will not be found.
            bool found = false;
            if (AssetDatabase.IsMainAsset(this))
            {
                var objects = AssetDatabase.LoadAllAssetsAtPath(FilePath);

                foreach (var obj in objects)
                {
                    if (obj is not GraphObject graphObject)
                        continue;
                    if (graphObject == this)
                        continue;
                    if (!graphObject.IsLocalSubgraphMigrated)
                        continue;

                    AssetDatabase.RemoveObjectFromAsset(graphObject);
                    DestroyImmediate(graphObject);
                    found = true;
                }
            }

            if (found)
            {
                AssetDatabase.SaveAssetIfDirty(AssetFileGuid);
            }
        }

        void InitializeMainGraphModel(Type graphModelType)
        {
            m_GraphModel = CreateGraphModel(graphModelType);
            m_GraphModel?.OnEnable();
            Dirty = true;
        }

        internal void AfterLoadForeignAsset(GUID assetGuid)
        {
            hideFlags = HideFlags.HideAndDontSave;
            m_AssetGuid = assetGuid;
            var filePath = FilePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                name = Path.GetFileNameWithoutExtension(filePath);
                UpdateFileSystemInfos();
            }
        }

        /// <summary>
        /// Detach the GraphModel from this asset.
        /// </summary>
        internal void DetachGraphModel()
        {
            if (m_GraphModel != null)
                GraphVisualization.Registry.UnregisterAllForGraph(m_GraphModel.Guid);
            m_GraphModel = null;
        }

        void UpdateFileSystemInfos()
        {
            var filePath = FilePath;

            if (!File.Exists(filePath))
            {
                m_LastWriteTime = 0;
                m_LastWriteSize = 0;
                m_LastWriteHash = new Hash128();
                return;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);

                m_LastWriteTime = fileInfo.LastWriteTimeUtc.ToFileTimeUtc();
                m_LastWriteSize = fileInfo.Length;

                m_LastWriteHash = ComputeFileHash(filePath);
            }
            catch (IOException e)
            {
                Debug.LogError($"Error while updating file info: {filePath}. {e.Message}");
                throw;
            }
        }

        Hash128 ComputeFileHash(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);

            Hash128 hash = new Hash128();

            int actuallyRead = stream.Read(s_ComputeFileHashBuffer, 0, s_ComputeFileHashBuffer.Length);
            while (actuallyRead > 0)
            {
                hash.Append(s_ComputeFileHashBuffer, 0, actuallyRead);
                if (actuallyRead < s_ComputeFileHashBuffer.Length)
                    break;
                actuallyRead = stream.Read(s_ComputeFileHashBuffer, 0, s_ComputeFileHashBuffer.Length);
            }
            stream.Close();

            return hash;
        }

        internal bool CheckHasChangedOnDisk()
        {
            //This mimics what the asset database does in HashDB : first check the last write time and size, then the hash if the two former have changed.
            var filePath = FilePath;

            if (!File.Exists(filePath))
            {
                return true;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);

                bool lastWriteChanged = fileInfo.LastWriteTimeUtc.ToFileTimeUtc() != m_LastWriteTime || fileInfo.Length != m_LastWriteSize;

                if (lastWriteChanged)
                {
                    Hash128 newHash = ComputeFileHash(filePath);

                    if (m_LastWriteHash != newHash)
                        return true;

                    // If the hash is the same, we can update the last write time and size.
                    m_LastWriteTime = fileInfo.LastWriteTimeUtc.ToFileTimeUtc();
                    m_LastWriteSize = fileInfo.Length;
                }
            }
            catch (IOException e)
            {
                Debug.LogError($"Error while checking file info: {filePath}. {e.Message}");
            }

            return false;
        }

        internal void CallOnLoadObject()
        {
            OnAfterLoad();
        }

        internal void ShowGraphObjectInProjectWindow()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                Debug.LogWarning("Could not show the asset in the project window because its file path is null or empty.");
                return;
            }

            EditorUtility.FocusProjectWindow();
            var obj = AssetDatabase.LoadMainAssetAtPath(FilePath);
            if (obj == null)
            {
                Debug.LogWarning("Could not show the asset in the project window because it could not be loaded at path: " + FilePath);
                return;
            }

            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        /// <summary>
        /// Apply all change needed for migration:
        /// assembly Unity.GraphToolkit.Internal renamed to UnityEditor.GraphToolkitModule
        /// </summary>
        /// <param name="filePath"></param>
        public static bool MigrateFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            var fullFile = File.ReadAllText(filePath);

            if (fullFile.Contains("asm: Unity.GraphToolkit.Editor}") ||
                fullFile.Contains("asm: Unity.GraphToolkit.Internal.Editor}") ||
                fullFile.Contains("asm: Unity.GraphToolkit.Editor.Tests}") ||
                fullFile.Contains("asm: Unity.GraphToolkit.Internal.Editor.Tests}"))
            {
                fullFile = fullFile.Replace("asm: Unity.GraphToolkit.Editor}", "asm: UnityEditor.GraphToolkitModule}");
                fullFile = fullFile.Replace("asm: Unity.GraphToolkit.Internal.Editor}", "asm: UnityEditor.GraphToolkitModule}");
                fullFile = fullFile.Replace("asm: Unity.GraphToolkit.Editor.Tests}", "asm: Unity.Modules.GraphToolkit.Internal.Tests.Editor}");
                fullFile = fullFile.Replace("asm: Unity.GraphToolkit.Internal.Editor.Tests}", "asm: Unity.Modules.GraphToolkit.Internal.Tests.Editor}");
                File.WriteAllText(filePath, fullFile);
                Debug.Log("GraphObject File has been migrated: " + filePath);
                return true;
            }
            return false;

        }

    }
}
