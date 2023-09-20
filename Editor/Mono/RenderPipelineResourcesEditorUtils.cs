// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Utility class for <see cref="RenderPipelineResources"/> in Editor
    /// </summary>
    static class RenderPipelineResourcesEditorUtils
    {
        public enum ResultStatus
        {
            NothingToUpdate, //There was nothing to reload
            InvalidPathOrNameFound, //Encountered a path that do not exist
            ResourceReloaded, //Some resources got reloaded
        }
        
        /// <summary>
        /// Looks for resources in the given <paramref name="resource"/> object and reload
        /// the ones that are missing or broken.
        /// This version will still return null value without throwing error if the issue
        /// is due to AssetDatabase being not ready. But in this case the assetDatabaseNotReady
        /// result will be true.
        /// </summary>
        /// <param name="resource">The object containing reload-able resources</param>
        /// <returns> The status </returns>
        public static ResultStatus TryReloadContainedNullFields(IRenderPipelineResources resource)
        {
            try
            {
                if (new Reloader(resource).hasChanged)
                    return ResultStatus.ResourceReloaded;
                else
                    return ResultStatus.NothingToUpdate;
            }
            catch (InvalidImportException e)
            {
                Debug.LogError(e.Message);
                return ResultStatus.InvalidPathOrNameFound;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        
        // Important: This is not meant to work with resources in Unity. This is only for packages and User code.
        // If we need to make it work for Unity core, one want to update Reloader.GetRootPathForType(...)
        struct Reloader
        {
            IRenderPipelineResources mainContainer;
            string root;
            public bool hasChanged { get; private set; }

            public Reloader(IRenderPipelineResources container)
            {
                mainContainer = container;
                hasChanged = false;
                root = GetRootPathForType(container.GetType());
                ReloadNullFields(container);
            }

            static string GetRootPathForType(Type type)
            {
                var packageInfo = PackageManager.PackageInfo.FindForAssembly(type.Assembly);
                return packageInfo == null ? "Assets/" : $"Packages/{packageInfo.name}/";
            }

            (string[] paths, SearchType location, bool isField) GetResourcesPaths(FieldInfo fieldInfo)
            {
                var attr = fieldInfo.GetCustomAttribute<ResourcePathsBaseAttribute>(inherit: false);
                return (attr?.paths, attr?.location ?? default, attr?.isField ?? default);
            }

            string GetFullPath(string path, SearchType location)
                => location == SearchType.ProjectPath
                ? $"{root}{path}"
                : path;

            bool IsNull(System.Object container, FieldInfo info)
                => IsNull(info.GetValue(container));

            bool IsNull(System.Object field)
                => field == null || field.Equals(null);

            bool ConstructArrayIfNeeded(System.Object container, FieldInfo info, int length)
            {
                if (IsNull(container, info) || ((Array)info.GetValue(container)).Length != length)
                {
                    info.SetValue(container, Activator.CreateInstance(info.FieldType, length));
                    return true;
                }

                return false;
            }

            void ReloadNullFields(System.Object container)
            {
                foreach (var fieldInfo in container.GetType()
                    .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    //Skip element that do not have path
                    (string[] paths, SearchType location, bool isField) = GetResourcesPaths(fieldInfo);
                    if (paths == null)
                        continue;

                    //Field case: reload if null
                    if (isField)
                    {
                        hasChanged |= SetAndLoadIfNull(container, fieldInfo, GetFullPath(paths[0], location), location);
                        continue;
                    }

                    //Array case: Find each null element and reload them
                    hasChanged |= ConstructArrayIfNeeded(container, fieldInfo, paths.Length);
                    var array = (Array)fieldInfo.GetValue(container);
                    for (int index = 0; index < paths.Length; ++index)
                        hasChanged |= SetAndLoadIfNull(array, index, GetFullPath(paths[index], location), location);
                }
            }

            bool SetAndLoadIfNull(System.Object container, FieldInfo info, string path, SearchType location)
            {
                if (IsNull(container, info))
                {
                    info.SetValue(container, Load(path, info.FieldType, location));
                    return true;
                }

                return false;
            }

            bool SetAndLoadIfNull(Array array, int index, string path, SearchType location)
            {
                var element = array.GetValue(index);
                if (IsNull(element))
                {
                    array.SetValue(Load(path, array.GetType().GetElementType(), location), index);
                    return true;
                }

                return false;
            }

            UnityEngine.Object Load(string path, Type type, SearchType location)
            {
                // Check if asset exist.
                // Direct loading can be prevented by AssetDatabase being reloading.
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (location == SearchType.ProjectPath && String.IsNullOrEmpty(guid))
                    throw new InvalidImportException($"Failed to find {path} in {location}.");

                UnityEngine.Object result = type == typeof(Shader)
                    ? LoadShader(path, location)
                    : LoadNonShaderAssets(path, type, location);
                
                if (IsNull(result))
                    switch (location)
                    {
                        case SearchType.ProjectPath: throw new InvalidImportException($"Cannot load. Path {path} is correct but AssetDatabase cannot load now.");
                        case SearchType.ShaderName: throw new InvalidImportException($"Failed to find {path} in {location}.");
                        case SearchType.BuiltinPath: throw new InvalidImportException($"Failed to find {path} in {location}.");
                        case SearchType.BuiltinExtraPath: throw new InvalidImportException($"Failed to find {path} in {location}.");
                    }
                    
                return result;
            }

            UnityEngine.Object LoadShader(string path, SearchType location)
            {
                switch (location)
                {
                    case SearchType.ShaderName:
                    case SearchType.BuiltinPath:
                    case SearchType.BuiltinExtraPath:
                        return Shader.Find(path);
                    case SearchType.ProjectPath:
                        return AssetDatabase.LoadAssetAtPath(path, typeof(Shader));
                    default:
                        throw new NotImplementedException($"Unknown {location}");
                }
            }

            UnityEngine.Object LoadNonShaderAssets(string path, Type type, SearchType location)
                => location switch
                {
                    SearchType.BuiltinPath => UnityEngine.Resources.GetBuiltinResource(type, path), //log error if path is wrong and return null
                    SearchType.BuiltinExtraPath => AssetDatabase.GetBuiltinExtraResource(type, path), //log error if path is wrong and return null
                    SearchType.ProjectPath => AssetDatabase.LoadAssetAtPath(path, type), //return null if path is wrong
                    SearchType.ShaderName => throw new ArgumentException($"{nameof(SearchType.ShaderName)} is only available for Shaders."),
                    _ => throw new NotImplementedException($"Unknown {location}")
                };
        }
    }
}
