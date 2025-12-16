// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [VisibleToOtherModules("UnityEditor.ContentLoadModule")]
    internal sealed partial class RootAssetProviderHelpers
    {
        public static T GetRootAssetByKey<T>(Dictionary<string, Object> keyToObject, string path) where T : Object
        {
            if (!keyToObject.ContainsKey(path))
                return null;

            return keyToObject[path] as T;
        }

        public static T GetRootAssetByName<T>(Dictionary<string, Object> keyToObject, string name) where T : Object
        {
            foreach (var obj in keyToObject.Values)
            {
                if (obj.name == name && obj is T)
                    return (T)obj;
            }

            return null;
        }

        public static List<T> AppendAllRootAssetsOfType<T>(Dictionary<Type, List<string>> typeToPathList, Dictionary<string, Object> pathToObject, HashSet<string> appendedPathsSet) where T : Object
        {
            Type key = typeof(T);
            List<T> list = new List<T>();
            foreach (var type in typeToPathList.Keys)
            {
                if (key.IsAssignableFrom(type))
                {
                    foreach (var path in typeToPathList[type])
                    {
                        if (appendedPathsSet.Contains(path))
                            continue;

                        list.Add((T)pathToObject[path]);
                        appendedPathsSet.Add(path);
                    }
                }
            }

            return list;
        }

        public static T GetRootAssetByType<T>(Dictionary<Type, List<string>> typeToPathList, Dictionary<string, Object> pathToObject) where T : Object
        {
            var key = typeof(T);
            foreach (var type in typeToPathList.Keys)
            {
                if (key.IsAssignableFrom(type))
                {
                    return (T)pathToObject[typeToPathList[type][0]];
                }
            }

            return null;
        }
    }
}
