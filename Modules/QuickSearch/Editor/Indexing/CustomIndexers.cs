// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_INDEXING

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Search
{
    delegate void CustomIndexerHandler(CustomObjectIndexerTarget context, ObjectIndexer indexer);

    /// <summary>
    /// Descriptor for the object that is about to be indexed. It stores a reference to the object itself as well a an already setup SerializedObject.
    /// </summary>
    public struct CustomObjectIndexerTarget
    {
        /// <summary>
        /// Object to be indexed.
        /// </summary>
        public Object target;
        /// <summary>
        /// Serialized representation of the object to be indexed.
        /// </summary>
        public SerializedObject serializedObject;
        /// <summary>
        /// Object Id. It is the object path in case of an asset or the GlobalObjectId in terms of a scene object.
        /// </summary>
        public string id;
        /// <summary>
        /// Document Index owning the object to index.
        /// </summary>
        public int documentIndex;
        /// <summary>
        /// Type of the object to index.
        /// </summary>
        public Type targetType;
    }

    /// <summary>
    /// Allow a user to register a custom Indexing function for a specific type. The registered function must be of type:
    /// static void Function(<see cref="CustomObjectIndexerTarget"/> context, <see cref="ObjectIndexer"/> indexer);
    /// <example>
    /// <code>
    /// [CustomObjectIndexer(typeof(Material))]
    /// internal static void MaterialShaderReferences(CustomObjectIndexerTarget context, ObjectIndexer indexer)
    /// {
    ///    var material = context.target as Material;
    ///    if (material == null)
    ///        return;
    ///
    ///    if (material.shader)
    ///    {
    ///        var fullShaderName = material.shader.name.ToLowerInvariant();
    ///        var shortShaderName = System.IO.Path.GetFileNameWithoutExtension(fullShaderName);
    ///        indexer.AddProperty("ref", shortShaderName, context.documentIndex, saveKeyword: false);
    ///        indexer.AddProperty("ref", fullShaderName, context.documentIndex, saveKeyword: false);
    ///    }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CustomObjectIndexerAttribute : Attribute
    {
        /// <summary>
        /// Each time an object of specific Type is indexed, the registered function will be called.
        /// </summary>
        public Type type { get; }

        /// <summary>
        /// Version of the custom indexer. Bump this number to have the indexer re-index the indexes.
        /// </summary>
        public int version { get; set; }

        /// <summary>
        /// Register a new Indexing function bound to the specific type.
        /// </summary>
        /// <param name="type">Type of object to be indexed.</param>
        public CustomObjectIndexerAttribute(Type type)
        {
            version = 0;
            this.type = type;
        }
    }

    static class CustomIndexers
    {
        private static readonly Dictionary<Type, List<CustomIndexerHandler>> s_CustomObjectIndexers = new Dictionary<Type, List<CustomIndexerHandler>>();

        public static IEnumerable<Type> types => s_CustomObjectIndexers.Keys;

        static CustomIndexers()
        {
            LoadCustomObjectIndexers();
        }

        public static IList<CustomIndexerHandler> GetHandlers(Type type)
        {
            return s_CustomObjectIndexers[type];
        }

        public static bool HasCustomIndexers(Type type, bool multiLevel = true)
        {
            if (!multiLevel)
                return s_CustomObjectIndexers.ContainsKey(type);

            var indexerTypes = s_CustomObjectIndexers.Keys;
            foreach (var indexerType in indexerTypes)
            {
                if (indexerType.IsAssignableFrom(type))
                    return true;
            }
            return false;
        }

        public static bool TryGetValue(Type objectType, out List<CustomIndexerHandler> customIndexers)
        {
            return s_CustomObjectIndexers.TryGetValue(objectType, out customIndexers);
        }

        public static Hash128 RefreshCustomIndexers()
        {
            Hash128 globalIndexersHash = default;
            foreach (var customIndexerMethodInfo in TypeCache.GetMethodsWithAttribute<CustomObjectIndexerAttribute>())
            {
                try
                {
                    var customIndexerAttribute = customIndexerMethodInfo.GetCustomAttribute<CustomObjectIndexerAttribute>();
                    var indexerType = customIndexerAttribute.type;
                    if (indexerType == null)
                        continue;

                    if (!ValidateCustomIndexerMethodSignature(customIndexerMethodInfo))
                        continue;

                    if (!(Delegate.CreateDelegate(typeof(CustomIndexerHandler), customIndexerMethodInfo) is CustomIndexerHandler customIndexerAction))
                        continue;

                    if (!s_CustomObjectIndexers.TryGetValue(indexerType, out var indexerList))
                    {
                        indexerList = new List<CustomIndexerHandler>();
                        s_CustomObjectIndexers.Add(indexerType, indexerList);
                    }
                    indexerList.Add(customIndexerAction);

                    var customIndexerHash = ComputeCustomIndexerHash(customIndexerMethodInfo, customIndexerAttribute);
                    HashUtilities.AppendHash(ref customIndexerHash, ref globalIndexersHash);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Cannot load CustomObjectIndexer with method: {customIndexerMethodInfo.Name} ({ex.Message})");
                }
            }
            return globalIndexersHash;
        }

        static void LoadCustomObjectIndexers()
        {
            var globalIndexersHash = RefreshCustomIndexers();
            if (!AssetDatabaseAPI.IsAssetImportWorkerProcess())
                EditorApplication.delayCall += () => AssetDatabaseAPI.RegisterCustomDependency(nameof(CustomObjectIndexerAttribute), globalIndexersHash);
        }

        static bool ValidateCustomIndexerMethodSignature(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                return false;

            if (methodInfo.ReturnType != typeof(void))
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, $"Method \"{methodInfo.Name}\" must return void.");
                return false;
            }

            var paramTypes = new[] { typeof(CustomObjectIndexerTarget), typeof(ObjectIndexer) };
            var parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length != paramTypes.Length)
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, $"Method \"{methodInfo.Name}\" must have {paramTypes.Length} parameter{(paramTypes.Length > 1 ? "s" : "")}.");
                return false;
            }

            for (var i = 0; i < paramTypes.Length; ++i)
            {
                if (parameterInfos[i].ParameterType != paramTypes[i])
                {
                    Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, $"The parameter \"{parameterInfos[i].Name}\" of method \"{methodInfo.Name}\" must be of type \"{paramTypes[i]}\".");
                    return false;
                }
            }

            return true;
        }

        static Hash128 ComputeCustomIndexerHash(MethodInfo mi, CustomObjectIndexerAttribute attr)
        {
            var id = $"{mi.DeclaringType.FullName}.{mi.Name}.{attr.version}";
            Hash128 dataHash = default;
            HashUtilities.ComputeHash128(System.Text.Encoding.ASCII.GetBytes(id), ref dataHash);
            return dataHash;
        }
    }
}
