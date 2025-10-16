// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets
{
    [Serializable]
    struct ResourcePath
    {
        public int pathIndex;
        public int subAssetNameIndex;

        public bool isValid => pathIndex != subAssetNameIndex && pathIndex >= 0;

        public ResourcePath(int pathIdx, int subAssetNameIdx = -1)
        {
            pathIndex = pathIdx;
            subAssetNameIndex = subAssetNameIdx;
        }

        public override string ToString() => $"ResourcePathIndex: {pathIndex}, SubAssetNameIndex: {subAssetNameIndex}";
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    struct ResolvedResourcePath : IEquatable<ResolvedResourcePath>
    {
        public string path { get; }
        public string subAssetName { get; }
        public bool isPathValid => !string.IsNullOrEmpty(path);
        public bool hasSubAssetName => !string.IsNullOrEmpty(subAssetName);

        /// <summary>
        /// Creates a new ResolvedResourcePath from a resource path string.
        /// Parses the string to extract the main path and any sub-asset name that starts with `#`.
        /// For example "Path/MyAsset.asset#SubAssetName" will result in:
        /// path: "Path/MyAsset.asset" and subAssetName: "SubAssetName".
        /// </summary>
        /// <param name="resourcePath"></param>
        public ResolvedResourcePath(string resourcePath)
        {
            path = resourcePath;
            subAssetName = null;

            if (!string.IsNullOrEmpty(path))
            {
                // Does the path contain a sub-asset name?
                subAssetName = null;

                var subAssetIndex = resourcePath.IndexOf("#");
                if (subAssetIndex != -1)
                {
                    // Split the path and sub-asset name
                    subAssetName = resourcePath.Substring(subAssetIndex + 1);
                    path = resourcePath.Substring(0, subAssetIndex);
                }
            }
        }

        /// <summary>
        /// Creates a new ResolvedResourcePath with a specified path and sub-asset name.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="subAssetName"></param>
        public ResolvedResourcePath(string path, string subAssetName)
        {
            this.path = path;
            this.subAssetName = subAssetName;
        }

        public T LoadResource<T>(float dpiScaling = 1.0f) where T: Object
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (!hasSubAssetName)
            {
                return Panel.LoadResource(path, typeof(T), dpiScaling) as T;
            }

            var assets = Resources.LoadAll(path, typeof(T));
            foreach (var asset in assets)
            {
                if (asset.name == subAssetName)
                    return asset as T;
            }

            return null;
        }

        public bool Equals(ResolvedResourcePath other)
        {
            return string.Equals(path, other.path, StringComparison.Ordinal) &&
                   string.Equals(subAssetName, other.subAssetName, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            if (hasSubAssetName)
                return $"{path}#{subAssetName}";
            return path;
        }
    }
}
