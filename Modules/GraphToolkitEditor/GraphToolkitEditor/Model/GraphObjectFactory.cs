// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Pool;

using InstanceID = System.Int32;

namespace Unity.GraphToolkit.Editor
{
    static class GraphObjectFactory
    {
        static readonly MethodInfo k_DefaultLoadGraphObjectAtPathMethod;

        static GraphObjectFactory()
        {
            EditorApplication.focusChanged += OnFocusChanged;
            EditorBridge.RegisterFileSavedCallback(OnFileSave);

            k_DefaultLoadGraphObjectAtPathMethod = GetMethodInfo(t => GraphObject.DefaultLoadGraphObjectFromFileOnDisk<GraphObject>(t)).GetGenericMethodDefinition();
            var graphObjectTypes = TypeCache.GetTypesWithAttribute<GraphObjectDefinitionAttribute>();

            foreach (var graphObjectType in graphObjectTypes)
            {
                if (!typeof(GraphObject).IsAssignableFrom(graphObjectType))
                {
                    Debug.LogError($"GraphObjectDefinitionAttribute is only valid on types that inherit from GraphObject. Type was:{graphObjectType.FullName}");
                    continue;
                }

                var attributes = graphObjectType.GetCustomAttributes<GraphObjectDefinitionAttribute>();
                foreach (var attribute in attributes)
                {
                    if (IsNativeAssetExtension(attribute.Extension))
                    {
                        Debug.LogError($"You cannot use the native asset extension .{attribute.Extension} for a GraphObjectDefinitionAttribute. Used on type: {graphObjectType.FullName}");
                        continue;
                    }

                    RegisterGraphObjectType(attribute.Extension, graphObjectType);
                }
            }

            var graphEditorWindowTypes = TypeCache.GetTypesWithAttribute<GraphEditorWindowDefinitionAttribute>();
            foreach (var graphEditorWindowType in graphEditorWindowTypes)
            {
                if (!typeof(GraphViewEditorWindow).IsAssignableFrom(graphEditorWindowType))
                {
                    Debug.LogError("GraphEditorWindowDefinitionAttribute is only valid on types that inherit from GraphViewEditorWindow.");
                    continue;
                }

                var attributes = graphEditorWindowType.GetCustomAttributes<GraphEditorWindowDefinitionAttribute>();

                foreach (var attribute in attributes)
                {
                    if (attribute.GraphObjectType == null)
                    {
                        Debug.LogError($"GraphEditorWindowDefinitionAttribute.GraphObjectType is null. Used on {graphEditorWindowType.FullName}");
                        continue;
                    }

                    if (!typeof(GraphObject).IsAssignableFrom(attribute.GraphObjectType))
                    {
                        Debug.LogError($"GraphEditorWindowDefinitionAttribute.GraphObjectType must inherit from GraphObject. Used on {graphEditorWindowType.FullName}");
                        continue;
                    }

                    RegisterGraphEditorWindowType(attribute.GraphObjectType, graphEditorWindowType);
                }
            }
        }

        static void OnFileSave()
        {
            SaveAllGraphs();
        }

        static void RegisterGraphEditorWindowType(Type graphObjectType, Type graphEditorWindowType)
        {
            if (!s_WindowTypeForGraphObjectType.TryAdd(graphObjectType, graphEditorWindowType))
            {
                Debug.LogError($"A window type for {graphObjectType.FullName} has already been registered. Ignoring {graphEditorWindowType.FullName}");
            }
        }

        internal static bool IsNativeAssetExtension(string extension)
        {
            return extension == k_NativeAssetExtension;
        }

        internal static bool FilePathHasNativeAssetExtension(string filePath)
        {
            return FilePathHasExtension(filePath, k_NativeAssetExtension);
        }

        static bool FilePathHasExtension(string filePath, string extension)
        {
            //Convoluted to not allocate a string
            return filePath != null && extension != null && filePath.Length > extension.Length && filePath.EndsWith(extension) && filePath[filePath.Length - extension.Length - 1] == '.';
        }

        static readonly string k_NativeAssetExtension = "asset";
        [OnOpenAsset(1000)]
        public static bool OpenGraphAsset(InstanceID instanceId, int line)
        {
            var graphObject = TryLoadGraphObjectFromInstanceId(instanceId);
            if (graphObject != null)
            {
                var windowType = GetWindowTypeForGraphObject(graphObject.GetType());

                if (windowType != null)
                {
                    GraphViewEditorWindow.ShowGraphInExistingOrNewWindow(graphObject, windowType);
                    return true;
                }
            }

            return false;
        }

        internal static Type GetWindowTypeForGraphObject(Type graphObjectType)
        {
            while (graphObjectType != null)
            {
                if (s_WindowTypeForGraphObjectType.TryGetValue(graphObjectType, out var windowType))
                {
                    return windowType;
                }
                graphObjectType = graphObjectType.BaseType;
            }

            return null;
        }

        public static MethodInfo GetMethodInfo(Expression<GraphObjectDefinitionAttribute.LoadGraphObjectLoader> expression)
        {
            return ((MethodCallExpression)expression.Body).Method;
        }

        static GraphObjectDefinitionAttribute.LoadGraphObjectLoader MakeLoaderDelegate(Type graphObjectType)
        {
            var loader = GetCustomLoaderMethod(graphObjectType);

            return (GraphObjectDefinitionAttribute.LoadGraphObjectLoader)Delegate.CreateDelegate(typeof(GraphObjectDefinitionAttribute.LoadGraphObjectLoader), loader ?? k_DefaultLoadGraphObjectAtPathMethod.MakeGenericMethod(graphObjectType));
        }


        internal static bool KnowsExtension(string extension)
        {
            if (extension.Length > 0 && extension[0] == '.')
            {
                extension = extension.Substring(1);
            }
            return s_GraphObjectInfosByExtension.ContainsKey(extension);
        }

        static MethodInfo GetCustomLoaderMethod(Type graphObjectType)
        {
            MethodInfo invalidLoader = null;
            var currentGraphObjectType = graphObjectType;
            while (currentGraphObjectType != null && currentGraphObjectType != typeof(GraphObject))
            {
                var loaders = currentGraphObjectType.GetMember(GraphObjectDefinitionAttribute.LoadGraphObjectFromFileOnDiskMethodName, MemberTypes.Method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                foreach (var loaderMember in loaders)
                {
                    var loader = (MethodInfo)loaderMember;

                    if (!loader.IsStatic)
                    {
                        invalidLoader = loader;
                        continue;
                    }
                    var parameters = loader.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                    {
                        invalidLoader = loader;
                        continue;
                    }

                    if (loader.ReturnType != typeof(GraphObject))
                    {
                        invalidLoader = loader;
                        continue;
                    }

                    return loader;
                }
                currentGraphObjectType = currentGraphObjectType.BaseType;
            }

            // If we found no valid loader, but we found an invalid one, log an error to help the user fix it.
            if (invalidLoader != null)
            {
                if (!invalidLoader.IsStatic)
                {
                    Debug.LogError($"{graphObjectType.FullName}.{GraphObjectDefinitionAttribute.LoadGraphObjectFromFileOnDiskMethodName} must be static. It will be ignored.");
                }
                if (!invalidLoader.IsPublic)
                {
                    Debug.LogError($"{graphObjectType.FullName}.{GraphObjectDefinitionAttribute.LoadGraphObjectFromFileOnDiskMethodName} must be public. It will be ignored.");
                }
                else
                {
                    var parameters = invalidLoader.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                    {
                        Debug.LogError($"{graphObjectType.FullName}.{GraphObjectDefinitionAttribute.LoadGraphObjectFromFileOnDiskMethodName} must have a single string parameter. It will be ignored.");
                    }
                    else if (invalidLoader.ReturnType != typeof(GraphObject))
                    {
                        Debug.LogError($"{graphObjectType.FullName}.{GraphObjectDefinitionAttribute.LoadGraphObjectFromFileOnDiskMethodName} must return a GraphObject. It will be ignored.");
                    }
                }

                Debug.LogError($"{graphObjectType.FullName}.{GraphObjectDefinitionAttribute.LoadGraphObjectFromFileOnDiskMethodName} must have a single string parameter and return a GraphObject. It will be ignored.");
            }

            return null;
        }

        struct GraphObjectInfos
        {
            public Type graphObjectType;
            public GraphObjectDefinitionAttribute.LoadGraphObjectLoader loaderFunction;
        }

        static Dictionary<string, GraphObjectInfos> s_GraphObjectInfosByExtension = new();
        static Dictionary<Type, Type> s_WindowTypeForGraphObjectType = new();
        static Dictionary<GUID, GraphObject> s_LoadedGraphObjects;

        static string InstanceIdToFileExtension(InstanceID instanceId)
        {
            var filePath = AssetDatabase.GetAssetPath((EntityId)instanceId);
            if (!string.IsNullOrEmpty(filePath))
            {
                var extension = Path.GetExtension(filePath);
                if (!string.IsNullOrEmpty(extension))
                    return extension.Substring(1);
            }

            return null;
        }

        static GraphObject TryLoadGraphObjectFromInstanceId(InstanceID instanceId)
        {
            if (AssetDatabase.IsNativeAsset((EntityId)instanceId))
            {
                return EditorUtility.EntityIdToObject((EntityId)instanceId) as GraphObject;
            }

            var extension = InstanceIdToFileExtension(instanceId);
            if (extension == null)
                return null;
            if (s_GraphObjectInfosByExtension.TryGetValue(extension, out var graphObjectInfos) && graphObjectInfos.loaderFunction != null)
            {
                var filePath = AssetDatabase.GetAssetPath((EntityId)instanceId);
                return LoadGraphObjectAtPath(graphObjectInfos.loaderFunction, filePath, false);
            }

            return null;
        }

        static GraphObject LoadGraphObjectAtPath(GraphObjectDefinitionAttribute.LoadGraphObjectLoader function, string filePath, bool forgetLoadedAsset)
        {
            if (!File.Exists(filePath))
                return null;
            var guid = AssetDatabase.GUIDFromAssetPath(filePath);
            if (guid == default)
                return null;

            var graphObject = forgetLoadedAsset ? null : GetLoadedAsset(guid);
            if (graphObject != null)
                return graphObject;

            try
            {
                graphObject = function(filePath);
            }
            catch (IOException e)
            {
                Debug.LogError($"Exception while loading graph object at path: {filePath}. {e.Message}");
                return null;
            }

            if (!forgetLoadedAsset)
            {
                s_LoadedGraphObjects[guid] = graphObject;
            }

            if (graphObject != null)
            {
                graphObject.AfterLoadForeignAsset(guid);
            }

            if (forgetLoadedAsset)
            {
                // This is a temporary object that should not be kept in domain reloads.
                graphObject.hideFlags &= ~HideFlags.DontUnloadUnusedAsset;
            }
            if (graphObject != null)
            {
                graphObject.CallOnLoadObject();
            }

            return graphObject;
        }

        static GraphObject GetLoadedAsset(GUID assetGUID)
        {
            UpdateLoadedGraphObjects();

            return s_LoadedGraphObjects.GetValueOrDefault(assetGUID);
        }

        static void UpdateLoadedGraphObjects()
        {
            if (s_LoadedGraphObjects == null) //After a domain reload, we need to rebuild the asset list
            {
                s_LoadedGraphObjects = new Dictionary<GUID, GraphObject>();
                var existingObjects = Resources.FindObjectsOfTypeAll<GraphObject>();

                foreach (var go in existingObjects)
                {
                    if (!go.IsSaveAndLoadManagedByAssetDatabase && go.hideFlags.HasFlag(HideFlags.DontUnloadUnusedAsset) && !go.IsSubAsset)
                    {
                        var goAssetFileGuid = go.AssetFileGuid;
                        if (goAssetFileGuid != default && File.Exists(AssetDatabase.GUIDToAssetPath(goAssetFileGuid)))
                            s_LoadedGraphObjects[goAssetFileGuid] = go;
                    }
                }
            }
        }

        public static GraphObject LoadGraphObjectAtPath(string filePath, Type graphObjectType, bool forgetLoadedAsset)
        {
            var extension = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(extension))
                return null;

            extension = extension.Substring(1);
            if (IsNativeAssetExtension(extension))
            {
                if (forgetLoadedAsset)
                {
                    Debug.LogError("It is not possible to forget a loaded asset when loading a native asset.");
                    return null;
                }
                graphObjectType ??= typeof(GraphObject);

                return (GraphObject)AssetDatabase.LoadAssetAtPath(filePath, graphObjectType);
            }
            if (s_GraphObjectInfosByExtension.TryGetValue(extension, out var graphObjectInfos) && graphObjectInfos.loaderFunction != null)
            {
                return LoadGraphObjectAtPath(graphObjectInfos.loaderFunction, filePath, forgetLoadedAsset);
            }

            return null;
        }

        public static void RegisterGraphObjectType(string extension, Type graphObjectType)
        {
            if (!s_GraphObjectInfosByExtension.TryAdd(extension, new GraphObjectInfos() { graphObjectType = graphObjectType, loaderFunction = MakeLoaderDelegate(graphObjectType) }))
            {
                Debug.LogError($"A handling for {extension} has already been registered in GraphObjectDefinitionAttribute. Ignoring {graphObjectType.FullName}");
            }
        }

        public static void RegisterNewGraphObject(GraphObject graphObject, GUID assetFileGUID)
        {
            var existingAsset = GetLoadedAsset(assetFileGUID);

            if (existingAsset != null && existingAsset != graphObject)
            {
                Debug.LogWarning($"A new asset is created at the same path as an existing asset. The existing asset will be unloaded. Path: {AssetDatabase.GUIDToAssetPath(assetFileGUID)}");
                existingAsset.UnloadObject();
            }

            s_LoadedGraphObjects[assetFileGUID] = graphObject;
        }

        internal static void OnFocusChanged(bool focused)
        {
            if (!focused) return;

            UpdateLoadedGraphObjects();

            using var dispose2 = ListPool<GraphObject>.Get(out var loadedGraphObjectsCopy);
            loadedGraphObjectsCopy.AddRange(s_LoadedGraphObjects.Values);
            foreach (var graphObject in loadedGraphObjectsCopy)
            {
                if (graphObject == null)
                    continue;

                if (!graphObject.IsSaveAndLoadManagedByAssetDatabase && graphObject.CheckHasChangedOnDisk())
                {
                    string path = graphObject.FilePath;
                    if (!string.IsNullOrEmpty(path))
                    {
                        graphObject.UnloadObject();
                    }
                }
            }
        }

        public static void OnAssetDeleted(string assetPath)
        {
            if (s_LoadedGraphObjects?.TryGetValue(AssetDatabase.GUIDFromAssetPath(assetPath), out var graphObject) == true && graphObject != null)
            {
                graphObject.DestroyObjects();
            }
        }

        public static void SaveAllGraphs()
        {
            UpdateLoadedGraphObjects();
            foreach (var graphObject in s_LoadedGraphObjects.Values)
            {
                if (graphObject != null)
                {
                    graphObject.Save();
                }
            }
        }

        public static IReadOnlyList<string> GetExtensionsForAssetType(Type assetType)
        {
            var extensions = new List<string>();
            foreach (var infos in s_GraphObjectInfosByExtension)
            {
                if (assetType.IsAssignableFrom(infos.Value.graphObjectType))
                {
                    extensions.Add(infos.Key);
                }
            }

            return extensions;
        }

        public static IReadOnlyCollection<string> GetExtensions()
        {
            return s_GraphObjectInfosByExtension.Keys;
        }

        public static Type GetGraphObjectTypeForExtension(string extension)
        {
            return s_GraphObjectInfosByExtension.GetValueOrDefault(extension).graphObjectType;
        }
    }
}
