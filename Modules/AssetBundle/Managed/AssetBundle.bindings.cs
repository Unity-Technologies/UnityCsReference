// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngineInternal;

namespace UnityEngine
{
    public enum AssetBundleLoadResult
    {
        Success,
        Cancelled,
        NotMatchingCrc,
        FailedCache,
        NotValidAssetBundle,
        NoSerializedData,
        NotCompatible,
        AlreadyLoaded,
        FailedRead,
        FailedDecompression,
        FailedWrite,
        FailedDeleteRecompressionTarget,
        RecompressionTargetIsLoaded,
        RecompressionTargetExistsButNotArchive
    };

    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadFromFileAsyncOperation.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadFromMemoryAsyncOperation.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadFromManagedStreamAsyncOperation.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadAssetOperation.h")]
    [NativeHeader("Runtime/Scripting/ScriptingExportUtility.h")]
    [NativeHeader("Runtime/Scripting/ScriptingObjectWithIntPtrField.h")]
    [NativeHeader("Runtime/Scripting/ScriptingUtility.h")]
    [NativeHeader("AssetBundleScriptingClasses.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleSaveAndLoadHelper.h")]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleUtility.h")]
    [ExcludeFromPreset]
    public partial class AssetBundle : Object
    {
        private AssetBundle() {}

        [Obsolete("mainAsset has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        public Object mainAsset
        {
            get { return returnMainAsset(this); }
        }

        [FreeFunction("LoadMainObjectFromAssetBundle", true)]
        internal static extern Object returnMainAsset(AssetBundle bundle);

        [FreeFunction("UnloadAllAssetBundles")]
        public extern static void UnloadAllAssetBundles(bool unloadAllObjects);

        [FreeFunction("GetAllAssetBundles")]
        internal extern static AssetBundle[] GetAllLoadedAssetBundles_Native();
        public static IEnumerable<AssetBundle> GetAllLoadedAssetBundles()
        {
            return GetAllLoadedAssetBundles_Native();
        }

        [FreeFunction("LoadFromFileAsync")]
        internal extern static AssetBundleCreateRequest LoadFromFileAsync_Internal(string path, uint crc, ulong offset);

        public static AssetBundleCreateRequest LoadFromFileAsync(string path)
        {
            return LoadFromFileAsync_Internal(path, 0, 0);
        }

        public static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc)
        {
            return LoadFromFileAsync_Internal(path, crc, 0);
        }

        public static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc, ulong offset)
        {
            return LoadFromFileAsync_Internal(path, crc, offset);
        }

        [FreeFunction("LoadFromFile")]
        internal extern static AssetBundle LoadFromFile_Internal(string path, uint crc, ulong offset);

        public static AssetBundle LoadFromFile(string path)
        {
            return LoadFromFile_Internal(path, 0, 0);
        }

        public static AssetBundle LoadFromFile(string path, uint crc)
        {
            return LoadFromFile_Internal(path, crc, 0);
        }

        public static AssetBundle LoadFromFile(string path, uint crc, ulong offset)
        {
            return LoadFromFile_Internal(path, crc, offset);
        }

        [FreeFunction("LoadFromMemoryAsync")]
        internal extern static AssetBundleCreateRequest LoadFromMemoryAsync_Internal(byte[] binary, uint crc);

        public static AssetBundleCreateRequest LoadFromMemoryAsync(byte[] binary)
        {
            return LoadFromMemoryAsync_Internal(binary, 0);
        }

        public static AssetBundleCreateRequest LoadFromMemoryAsync(byte[] binary, uint crc)
        {
            return LoadFromMemoryAsync_Internal(binary, crc);
        }

        [FreeFunction("LoadFromMemory")]
        internal extern static AssetBundle LoadFromMemory_Internal(byte[] binary, uint crc);

        public static AssetBundle LoadFromMemory(byte[] binary)
        {
            return LoadFromMemory_Internal(binary, 0);
        }

        public static AssetBundle LoadFromMemory(byte[] binary, uint crc)
        {
            return LoadFromMemory_Internal(binary, crc);
        }

        internal static void ValidateLoadFromStream(System.IO.Stream stream)
        {
            if (stream == null)
                throw new System.ArgumentNullException("ManagedStream object must be non-null", "stream");
            if (!stream.CanRead)
                throw new System.ArgumentException("ManagedStream object must be readable (stream.CanRead must return true)", "stream");
            if (!stream.CanSeek)
                throw new System.ArgumentException("ManagedStream object must be seekable (stream.CanSeek must return true)", "stream");
        }

        public static AssetBundleCreateRequest LoadFromStreamAsync(System.IO.Stream stream, uint crc, uint managedReadBufferSize)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamAsyncInternal(stream, crc, managedReadBufferSize);
        }

        public static AssetBundleCreateRequest LoadFromStreamAsync(System.IO.Stream stream, uint crc)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamAsyncInternal(stream, crc, 0);
        }

        public static AssetBundleCreateRequest LoadFromStreamAsync(System.IO.Stream stream)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamAsyncInternal(stream, 0, 0);
        }

        public static AssetBundle LoadFromStream(System.IO.Stream stream, uint crc, uint managedReadBufferSize)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamInternal(stream, crc, managedReadBufferSize);
        }

        public static AssetBundle LoadFromStream(System.IO.Stream stream, uint crc)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamInternal(stream, crc, 0);
        }

        public static AssetBundle LoadFromStream(System.IO.Stream stream)
        {
            ValidateLoadFromStream(stream);
            return LoadFromStreamInternal(stream, 0, 0);
        }

        [FreeFunction("LoadFromStreamAsyncInternal")]
        internal extern static AssetBundleCreateRequest LoadFromStreamAsyncInternal(System.IO.Stream stream, uint crc,
            uint managedReadBufferSize);

        [FreeFunction("LoadFromStreamInternal")]
        internal extern static AssetBundle LoadFromStreamInternal(System.IO.Stream stream, uint crc,
            uint managedReadBufferSize);

        public extern bool isStreamedSceneAssetBundle
        {
            [NativeMethod("GetIsStreamedSceneAssetBundle")]
            get;
        }

        [NativeMethod("Contains")]
        public extern bool Contains(string name);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
        public Object Load(string name) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
        public Object Load<T>(string name) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method Load has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAsset instead and check the documentation for details.", true)]
        Object Load(string name, Type type) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAsync has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAssetAsync instead and check the documentation for details.", true)]
        AssetBundleRequest LoadAsync(string name, Type type) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
        Object[] LoadAll(Type type) { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
        public UnityEngine.Object[] LoadAll() { return null; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Method LoadAll has been deprecated. Script updater cannot update it as the loading behaviour has changed. Please use LoadAllAssets instead and check the documentation for details.", true)]
        public T[] LoadAll<T>() where T : Object { return null; }

        public Object LoadAsset(string name)
        {
            return LoadAsset(name, typeof(Object));
        }

        public T LoadAsset<T>(string name) where T : Object
        {
            return (T)LoadAsset(name, typeof(T));
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        public Object LoadAsset(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAsset_Internal(name, type);
        }

        [NativeThrows]
        [NativeMethod("LoadAsset_Internal")]
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        private extern Object LoadAsset_Internal(string name, Type type);

        public AssetBundleRequest LoadAssetAsync(string name)
        {
            return LoadAssetAsync(name, typeof(UnityEngine.Object));
        }

        public AssetBundleRequest LoadAssetAsync<T>(string name)
        {
            return LoadAssetAsync(name, typeof(T));
        }

        public AssetBundleRequest LoadAssetAsync(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetAsync_Internal(name, type);
        }

        public Object[] LoadAssetWithSubAssets(string name)
        {
            return LoadAssetWithSubAssets(name, typeof(Object));
        }

        internal static T[] ConvertObjects<T>(Object[] rawObjects) where T : Object
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }

        public T[] LoadAssetWithSubAssets<T>(string name) where T : Object
        {
            return ConvertObjects<T>(LoadAssetWithSubAssets(name, typeof(T)));
        }

        public Object[] LoadAssetWithSubAssets(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssets_Internal(name, type);
        }

        public AssetBundleRequest LoadAssetWithSubAssetsAsync(string name)
        {
            return LoadAssetWithSubAssetsAsync(name, typeof(UnityEngine.Object));
        }

        public AssetBundleRequest LoadAssetWithSubAssetsAsync<T>(string name)
        {
            return LoadAssetWithSubAssetsAsync(name, typeof(T));
        }

        public AssetBundleRequest LoadAssetWithSubAssetsAsync(string name, Type type)
        {
            if (name == null)
            {
                throw new System.NullReferenceException("The input asset name cannot be null.");
            }
            if (name.Length == 0)
            {
                throw new System.ArgumentException("The input asset name cannot be empty.");
            }
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssetsAsync_Internal(name, type);
        }

        public UnityEngine.Object[] LoadAllAssets()
        {
            return LoadAllAssets(typeof(UnityEngine.Object));
        }

        public T[] LoadAllAssets<T>() where T : Object
        {
            return ConvertObjects<T>(LoadAllAssets(typeof(T)));
        }

        public UnityEngine.Object[] LoadAllAssets(Type type)
        {
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssets_Internal("", type);
        }

        public AssetBundleRequest LoadAllAssetsAsync()
        {
            return LoadAllAssetsAsync(typeof(UnityEngine.Object));
        }

        public AssetBundleRequest LoadAllAssetsAsync<T>()
        {
            return LoadAllAssetsAsync(typeof(T));
        }

        public AssetBundleRequest LoadAllAssetsAsync(Type type)
        {
            if (type == null)
            {
                throw new System.NullReferenceException("The input type cannot be null.");
            }

            return LoadAssetWithSubAssetsAsync_Internal("", type);
        }

        [Obsolete("This method is deprecated.Use GetAllAssetNames() instead.", false)]
        public string[] AllAssetNames()
        {
            return GetAllAssetNames();
        }

        [NativeThrows]
        [NativeMethod("LoadAssetAsync_Internal")]
        private extern AssetBundleRequest LoadAssetAsync_Internal(string name, Type type);

        [NativeMethod("Unload")]
        public extern void Unload(bool unloadAllLoadedObjects);

        [NativeMethod("GetAllAssetNames")]
        public extern string[] GetAllAssetNames();

        [NativeMethod("GetAllScenePaths")]
        public extern string[] GetAllScenePaths();

        [NativeThrows]
        [NativeMethod("LoadAssetWithSubAssets_Internal")]
        internal extern Object[] LoadAssetWithSubAssets_Internal(string name, Type type);

        [NativeThrows]
        [NativeMethod("LoadAssetWithSubAssetsAsync_Internal")]
        private extern AssetBundleRequest LoadAssetWithSubAssetsAsync_Internal(string name, Type type);

        public static AssetBundleRecompressOperation RecompressAssetBundleAsync(string inputPath, string outputPath, BuildCompression method, UInt32 expectedCRC = 0, ThreadPriority priority = ThreadPriority.Low)
        {
            return RecompressAssetBundleAsync_Internal(inputPath, outputPath, method, expectedCRC, priority);
        }

        [NativeThrows]
        [FreeFunction("RecompressAssetBundleAsync_Internal")]
        internal static extern AssetBundleRecompressOperation RecompressAssetBundleAsync_Internal(string inputPath, string outputPath, BuildCompression method, UInt32 expectedCRC, ThreadPriority priority);
    }
}
