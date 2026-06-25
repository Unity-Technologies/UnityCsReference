// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.AdaptivePerformance;

namespace UnityEditor.AdaptivePerformance.Editor
{
    internal class AdaptivePerformanceLoaderInfo : IEquatable<AdaptivePerformanceLoaderInfo>
    {
        public Type loaderType;
        public string assetName;
        public AdaptivePerformanceLoader instance;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is AdaptivePerformanceLoaderInfo && Equals((AdaptivePerformanceLoaderInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (loaderType != null ? loaderType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (instance != null ? instance.GetHashCode() : 0);
                return hashCode;
            }
        }

        public bool Equals(AdaptivePerformanceLoaderInfo other)
        {
            return other != null && Equals(loaderType, other.loaderType) && Equals(instance, other.instance);
        }

        static readonly string[] s_LoaderblockList = { "DummyLoader", "SampleLoader", "AdaptivePerformanceLoaderHelper" };

        internal static void GetAllKnownLoaderInfos(List<AdaptivePerformanceLoaderInfo> newInfos)
        {
            var loaderTypes = TypeLoaderExtensions.GetAllTypesWithInterface<AdaptivePerformanceLoader>();
            foreach (Type loaderType in loaderTypes)
            {
                if (loaderType.IsAbstract)
                    continue;

                bool isBlocked = false;
                foreach (var loaderName in s_LoaderblockList)
                {
                    if (loaderName == loaderType.Name)
                    {
                        isBlocked = true;
                        break;
                    }

                }

                if (isBlocked)
                    continue;

                var assets = AssetDatabase.FindAssets(String.Format("t:{0}", loaderType));
                if (assets == null || assets.Length == 0)
                {
                    AdaptivePerformanceLoaderInfo info = new AdaptivePerformanceLoaderInfo();
                    info.loaderType = loaderType;
                    newInfos.Add(info);
                }
                else
                {
                    foreach (var asset in assets)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(asset);

                        AdaptivePerformanceLoaderInfo info = new AdaptivePerformanceLoaderInfo();
                        info.loaderType = loaderType;
                        info.instance = AssetDatabase.LoadAssetAtPath(path, loaderType) as AdaptivePerformanceLoader;
                        info.assetName = Path.GetFileNameWithoutExtension(path);
                        newInfos.Add(info);
                    }
                }
            }
        }
    }
}
