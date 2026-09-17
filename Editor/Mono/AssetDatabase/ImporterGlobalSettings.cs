// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    /// <summary>
    /// A settings singleton that asset importers read, kept in sync with asset import workers.
    /// </summary>
    /// <remarks>
    /// Unity synchronizes the values with asset import workers whenever an AssetDatabase refresh starts
    /// importing assets, so an importer reads the same values whether it runs in the Editor or on a worker.
    /// This includes changes that you have not saved to disk.
    ///
    /// Changing a value does not synchronize it on its own. Unity picks the change up the next time it starts
    /// importing assets, so imports that are already running keep the values they started with.
    ///
    /// Add the <see cref="UnityEditor.FilePathAttribute"/> to your class to also persist the values between
    /// Editor sessions, and call <c>Save</c> to write them to disk. Unity synchronizes settings without this
    /// attribute in the same way, but only keeps them for the current session.
    /// </remarks>
    public class ImporterGlobalSettings<T> : ScriptableObject where T : ScriptableObject
    {
        const string k_OverlayPathPrefix = $"ImporterGlobalSettings/";

        [NoAutoStaticsCleanup] // reconstructed from a backup on reload and re-linked in the ctor; lazily recreated if null
        static T s_Instance;

        /// <summary>
        /// Gets the instance of the ImporterGlobalSettings Singleton. Unity creates the Singleton instance when this property is accessed for the first time. If you use the FilePathAttribute, then Unity loads the data on the first access as well.
        /// </summary>
        public static T instance
        {
            get
            {
                if (s_Instance == null)
                    CreateAndLoad();

                return s_Instance;
            }
        }

        // On domain reload ScriptableObject objects gets reconstructed from a backup. We therefore set the s_Instance here
        protected ImporterGlobalSettings()
        {
            if (s_Instance != null)
            {
                Debug.LogError("ImporterGlobalSettings already exists. Did you query the settings in a constructor?");
                return;
            }

            object casted = this;
            s_Instance = casted as T;
            System.Diagnostics.Debug.Assert(s_Instance != null);

            // Registering the singleton for recording in the SourceAssetDB and synchronization on workers.
            ImporterGlobalSettingsRegistry.Register(typeof(T), GetOverlayPath(), this);
        }

        private static void CreateAndLoad()
        {
            System.Diagnostics.Debug.Assert(s_Instance == null);

            // If worker, load it from SourceAssetDB
            if (AssetDatabase.IsAssetImportWorkerProcess())
            {
                InternalEditorUtility.LoadSerializedFileAndForget("mem://" + GetOverlayPath());
            }

            // Either no overlay was synced yet, or this is the main editor, which owns the on-disk state.
            if (s_Instance == null)
            {
                // If a file exists then load it and deserialize it.
                // This creates an instance of T which will set s_Instance in the constructor. Then it will deserialize it and call relevant serialization callbacks.
                string filePath = GetFilePath();
                if (!string.IsNullOrEmpty(filePath))
                    InternalEditorUtility.LoadSerializedFileAndForget(filePath);
            }

            if (s_Instance == null)
            {
                T t = CreateInstance<T>();
                t.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
            }

            System.Diagnostics.Debug.Assert(s_Instance != null);
        }

        private static string GetOverlayPath()
        {
            return k_OverlayPathPrefix + typeof(T).FullName;
        }

        /// <summary>
        /// Saves the current state of the ImporterGlobalSettings.
        /// Call Save to save the current state of the ImporterGlobalSettings to disk for persistence. If you call this function and your class has no FilePathAttribute, then saving has no effect.
        /// Note: Don't call this method from ScriptableObject.OnValidate because the singleton can be in the process of reading its data from a file, which causes an error.
        /// </summary>
        /// <param name="saveAsText">If true then the file is saved as text, if false it is saved as binary.</param>
        protected virtual void Save(bool saveAsText)
        {
            if (s_Instance == null)
            {
                Debug.LogError("Cannot save ImporterGlobalSettings: no instance!");
                return;
            }

            // Import worker are consummers only for these singletons
            if (AssetDatabase.IsAssetImportWorkerProcess())
            {
                Debug.LogWarning($"Saving ImporterGlobalSettings '{GetType()}' has no effect on an asset import worker process.");
                return;
            }

            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                string folderPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { s_Instance }, filePath, saveAsText);
            }
            else
            {
                Debug.LogWarning($"Saving has no effect. Your class '{GetType()}' is missing the FilePathAttribute. Use this attribute to specify where to save your ImporterGlobalSettings.\nOnly call Save() and use this attribute if you want your state to survive between sessions of Unity.");
            }
        }

        /// <summary>
        /// Get the file path where this ImporterGlobalSettings is saved to.
        /// If you call this function and your class has no FilePathAttribute, then an empty string is returned.
        /// </summary>
        /// <returns>The file path where this ImporterGlobalSettings is saved to.</returns>
        protected static string GetFilePath()
        {
            var attr = typeof(T).GetCustomAttribute<FilePathAttribute>(inherit: true);
            return attr?.filepath ?? string.Empty;
        }
    }

    /// <summary>
    /// The ImporterGlobalSettings instances that exist in this process, registered as they are created.
    /// On the main editor the registry is what gets captured and overlaid onto the asset import workers; on a
    /// worker it is used to drop the states once a new overlay has been synced.
    /// </summary>
    static partial class ImporterGlobalSettingsRegistry
    {
        [AutoStaticsCleanupOnCodeReload]
        // Suppressed at the declaration rather than at the call site so that it also covers the constructor of
        // every class deriving from ImporterGlobalSettings, which inherits the side effect.
        [IgnoreForUAL0015("Registrations are cleared when the CodeLoaded scope exits and re-made by the ImporterGlobalSettings constructor when the instances are reconstructed")]
        static readonly Dictionary<Type, (string overlayPath, UnityEngine.Object instance)> s_States =
            new Dictionary<Type, (string, UnityEngine.Object)>();

        internal static void Register(Type stateType, string overlayPath, UnityEngine.Object instance)
        {
            s_States[stateType] = (overlayPath, instance);
        }

        /// <summary>
        /// Invoked on the main editor (from native AssetDatabase::RecordImporterGlobalSettingsOverlays) to collect the
        /// live states along with the path each one is overlaid under (matching index in both arrays).
        /// </summary>
        [RequiredByNativeCode]
        internal static UnityEngine.Object[] CaptureImporterGlobalSettings(out string[] overlayPaths)
        {
            Debug.Assert(!AssetDatabase.IsAssetImportWorkerProcess(), "CaptureImporterGlobalSettings should only be called in the main Editor (not on workers)");

            var paths = new List<string>(s_States.Count);
            var instances = new List<UnityEngine.Object>(s_States.Count);

            foreach (var state in s_States.Values)
            {
                if (state.instance == null)
                    continue;

                paths.Add(state.overlayPath);
                instances.Add(state.instance);
            }

            overlayPaths = paths.ToArray();
            return instances.ToArray();
        }

        /// <summary>
        /// Invoked on import workers (via AssetImportWorkerClient) after they are re-prepared following a
        /// change on the main editor, so that the next access reloads from the refreshed overlay.
        /// </summary>
        [RequiredByNativeCode]
        internal static void ReloadImporterGlobalSettings()
        {
            Debug.Assert(AssetDatabase.IsAssetImportWorkerProcess(), "ReloadImporterGlobalSettings should only be called on an Import Worker");

            var instances = new List<UnityEngine.Object>(s_States.Count);
            foreach (var state in s_States.Values)
                instances.Add(state.instance);

            s_States.Clear();

            foreach (var instance in instances)
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
