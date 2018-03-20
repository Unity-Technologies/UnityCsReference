// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngineInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Managed counterpart to ResourceRequestScripting (ResourceManagerUtility.h)
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class ResourceRequest : AsyncOperation
    {
        internal string m_Path;
        internal Type m_Type;
        public Object asset { get { return Resources.Load(m_Path, m_Type); } }
    }

    // The Resources class allows you to find and access Objects including assets.
    [NativeHeader("Runtime/Export/Resources.bindings.h")]
    [NativeHeader("Runtime/Misc/ResourceManagerUtility.h")]
    public sealed partial class Resources
    {
        internal static T[] ConvertObjects<T>(Object[] rawObjects) where T : Object
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }

        [TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
        [FreeFunction("Resources_Bindings::FindObjectsOfTypeAll")]
        extern public static Object[] FindObjectsOfTypeAll(Type type);

        public static T[] FindObjectsOfTypeAll<T>() where T : Object
        {
            return ConvertObjects<T>(FindObjectsOfTypeAll(typeof(T)));
        }

        // Loads an asset stored at /path/ in a Resources folder.

        public static Object Load(string path)
        {
            return Load(path, typeof(Object));
        }

        public static T Load<T>(string path) where T : Object
        {
            return (T)Load(path, typeof(T));
        }

        // Loads an asset stored at /path/ in a Resources folder.
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        [NativeThrows]
        [FreeFunction("Resources_Bindings::Load")]
        extern public static Object Load(string path, [NotNull] Type systemTypeInstance);

        public static ResourceRequest LoadAsync(string path)
        {
            return LoadAsync(path, typeof(Object));
        }

        public static ResourceRequest LoadAsync<T>(string path) where T : Object
        {
            return LoadAsync(path, typeof(T));
        }

        public static ResourceRequest LoadAsync(string path, Type type)
        {
            ResourceRequest req = LoadAsyncInternal(path, type);
            req.m_Path = path;
            req.m_Type = type;
            return req;
        }

        [FreeFunction("Resources_Bindings::LoadAsyncInternal")]
        extern internal static ResourceRequest LoadAsyncInternal(string path, Type type);

        // Loads all assets in a folder or file at /path/ in a Resources folder.
        [NativeThrows]
        [FreeFunction("Resources_Bindings::LoadAll")]
        extern public static Object[] LoadAll([NotNull] string path, [NotNull] Type systemTypeInstance);

        // Loads all assets in a folder or file at /path/ in a Resources folder.

        public static Object[] LoadAll(string path)
        {
            return LoadAll(path, typeof(Object));
        }

        public static T[] LoadAll<T>(string path) where T : Object
        {
            return ConvertObjects<T>(LoadAll(path, typeof(T)));
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [FreeFunction("GetScriptingBuiltinResource")]
        extern public static Object GetBuiltinResource([NotNull] Type type, string path);

        public static T GetBuiltinResource<T>(string path) where T : Object
        {
            return (T)GetBuiltinResource(typeof(T), path);
        }

        // Unloads /assetToUnload/ from memory.
        [FreeFunction("Scripting::UnloadAssetFromScripting")]
        extern public static void UnloadAsset(Object assetToUnload);

        // Unloads assets that are not used.
        [FreeFunction("Resources_Bindings::UnloadUnusedAssets")]
        extern public static AsyncOperation UnloadUnusedAssets();
    }
}
