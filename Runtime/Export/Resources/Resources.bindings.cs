// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngineInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    // Managed counterpart to ResourceRequestScripting (ResourceManagerUtility.h)
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class ResourceRequest : AsyncOperation
    {
        internal string m_Path;
        internal Type m_Type;

        protected virtual Object GetResult()
        {
            return Resources.Load(m_Path, m_Type);
        }

        public Object asset { get { return GetResult(); } }

        public ResourceRequest() { }

        protected ResourceRequest(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static ResourceRequest ConvertToManaged(IntPtr ptr) => new ResourceRequest(ptr);
        }
    }

    [NativeHeader("Runtime/Export/Resources/Resources.bindings.h")]
    [NativeHeader("Runtime/Misc/ResourceManagerUtility.h")]
    internal static class ResourcesAPIInternal
    {
        [TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
        [FreeFunction("Resources_Bindings::FindObjectsOfTypeAll")]
        public extern static Object[] FindObjectsOfTypeAll(Type type);

        [FreeFunction("GetShaderNameRegistry().FindShader")]
        public extern static Shader FindShaderByName(string name);

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        [NativeThrows]
        [FreeFunction("Resources_Bindings::Load")]
        public extern static Object Load(string path, [NotNull] Type systemTypeInstance);

        [NativeThrows]
        [FreeFunction("Resources_Bindings::LoadAll")]
        public extern static Object[] LoadAll([NotNull] string path, [NotNull] Type systemTypeInstance);

        [FreeFunction("Resources_Bindings::GetAllPaths")]
        public extern static string[] GetAllPaths([NotNull] string path);

        [FreeFunction("Resources_Bindings::LoadAsyncInternal")]
        extern internal static ResourceRequest LoadAsyncInternal(string path, Type type);

        [FreeFunction("Scripting::UnloadAssetFromScripting")]
        public extern static void UnloadAsset(Object assetToUnload);
    }

    public class ResourcesAPI
    {
        static ResourcesAPI s_DefaultAPI = new ResourcesAPI();
        // Internal code must use ActiveAPI over overrideAPI to properly fallback to default api handling
        internal static ResourcesAPI ActiveAPI => overrideAPI ?? s_DefaultAPI;

        public static ResourcesAPI overrideAPI { get; set; }

        protected internal ResourcesAPI() {}
        protected internal virtual Object[] FindObjectsOfTypeAll(Type systemTypeInstance) => ResourcesAPIInternal.FindObjectsOfTypeAll(systemTypeInstance);
        protected internal virtual Shader FindShaderByName(string name) => ResourcesAPIInternal.FindShaderByName(name);
        protected internal virtual Object Load(string path, Type systemTypeInstance) => ResourcesAPIInternal.Load(path, systemTypeInstance);
        protected internal virtual Object[] LoadAll(string path, Type systemTypeInstance) => ResourcesAPIInternal.LoadAll(path, systemTypeInstance);
        protected internal virtual ResourceRequest LoadAsync(string path, Type systemTypeInstance)
        {
            var req = ResourcesAPIInternal.LoadAsyncInternal(path, systemTypeInstance);
            req.m_Path = path;
            req.m_Type = systemTypeInstance;
            return req;
        }

        protected internal virtual void UnloadAsset(Object assetToUnload) => ResourcesAPIInternal.UnloadAsset(assetToUnload);
    }

    // The Resources class allows you to find and access Objects including assets.
    [NativeHeader("Runtime/Export/Resources/Resources.bindings.h")]
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

        public static Object[] FindObjectsOfTypeAll(Type type)
        {
            return ResourcesAPI.ActiveAPI.FindObjectsOfTypeAll(type);
        }

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

        public static Object Load(string path, Type systemTypeInstance)
        {
            return ResourcesAPI.ActiveAPI.Load(path, systemTypeInstance);
        }

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
            return ResourcesAPI.ActiveAPI.LoadAsync(path, type);
        }

        // Loads all assets in a folder or file at /path/ in a Resources folder.
        public static Object[] LoadAll(string path, Type systemTypeInstance)
        {
            return ResourcesAPI.ActiveAPI.LoadAll(path, systemTypeInstance);
        }

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
        [FreeFunction("GetScriptingBuiltinResource", ThrowsException = true)]
        extern public static Object GetBuiltinResource([NotNull] Type type, string path);

        public static T GetBuiltinResource<T>(string path) where T : Object
        {
            return (T)GetBuiltinResource(typeof(T), path);
        }

        // Unloads /assetToUnload/ from memory.
        public static void UnloadAsset(Object assetToUnload)
        {
            ResourcesAPI.ActiveAPI.UnloadAsset(assetToUnload);
        }

        [FreeFunction("Scripting::UnloadAssetFromScripting")]
        extern static void UnloadAssetImplResourceManager(Object assetToUnload);

        // Unloads assets that are not used.
        [FreeFunction("Resources_Bindings::UnloadUnusedAssets")]
        extern public static AsyncOperation UnloadUnusedAssets();

        [FreeFunction("Resources_Bindings::InstanceIDToObject")]
        public extern static Object InstanceIDToObject(int instanceID);

        [FreeFunction("Resources_Bindings::IsInstanceLoaded")]
        internal extern static bool IsInstanceLoaded(int instanceID);

        [FreeFunction("Resources_Bindings::InstanceIDToObjectList")]
        extern private static void InstanceIDToObjectList(IntPtr instanceIDs, int instanceCount, List<Object> objects);

        public static unsafe void InstanceIDToObjectList(NativeArray<int> instanceIDs, List<Object> objects)
        {
            if (!instanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(instanceIDs));
            if (objects == null)
                throw new ArgumentNullException(nameof(objects));

            if (instanceIDs.Length == 0)
            {
                objects.Clear();
                return;
            }

            InstanceIDToObjectList((IntPtr)instanceIDs.GetUnsafeReadOnlyPtr(), instanceIDs.Length, objects);
        }

        [FreeFunction("Resources_Bindings::InstanceIDsToValidArray")]
        private static extern unsafe void InstanceIDsToValidArray_Internal(IntPtr instanceIDs, int instanceCount, IntPtr validArray, int validArrayCount);

        public static unsafe void InstanceIDsToValidArray(NativeArray<int> instanceIDs, NativeArray<bool> validArray)
        {
            if (!instanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(instanceIDs));
            if (!validArray.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(validArray));
            if (instanceIDs.Length != validArray.Length)
                throw new ArgumentException("Size mismatch! Both arrays must be the same length.");
            if(instanceIDs.Length == 0)
                return;

            InstanceIDsToValidArray_Internal((IntPtr)instanceIDs.GetUnsafeReadOnlyPtr(), instanceIDs.Length, (IntPtr)validArray.GetUnsafePtr(), validArray.Length);
        }

        public static unsafe void InstanceIDsToValidArray(ReadOnlySpan<int> instanceIDs, Span<bool> validArray)
            {
                if (instanceIDs.Length != validArray.Length)
                    throw new ArgumentException("Size mismatch! Both arrays must be the same length.");
                if(instanceIDs.Length == 0)
                    return;

                fixed(int* instanceIDsPtr = instanceIDs)
                fixed(bool* validArrayPtr = validArray)
                {
                    InstanceIDsToValidArray_Internal((IntPtr)instanceIDsPtr, instanceIDs.Length, (IntPtr)validArrayPtr, validArray.Length);
                }
            }
        }
}
