// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;


namespace UnityEngine
{
    [VisibleToOtherModules("UnityEditor.ContentLoadModule")]
    internal interface IRootAssetProvider
    {
        T GetRootAssetByType<T>() where T : Object;

        T GetRootAssetByKey<T>(string path) where T : Object;

        T GetRootAssetByName<T>(string name) where T : Object;

        // appendedPathsSet- Accumulates all the paths that are added to the list. This is used to deduplicate, by path, between RootAssetProviders. This ensures that only the first discovered root asset with a particular path is included.
        void AppendUniqueRootAssetsOfType<T>(List<T> assets, HashSet<string> appendedPathsSet) where T : Object;
    }

    internal sealed class RootAssetProviderHandler
    {
        List<IRootAssetProvider> m_RootAssetProviders;

        internal RootAssetProviderHandler()
        {
            m_RootAssetProviders = new List<IRootAssetProvider>();
            AddRootAssetProvider(new ContentRootAssetProvider());
        }

        internal T GetRootAssetByType<T>() where T : Object
        {
            foreach (var provider in m_RootAssetProviders)
            {
                var asset = provider.GetRootAssetByType<T>();
                if (asset != null)
                    return asset;
            }

            return null;
        }

        internal T GetRootAssetByKey<T>(string path) where T : Object
        {
            foreach (var provider in m_RootAssetProviders)
            {
                var asset = provider.GetRootAssetByKey<T>(path);
                if (asset != null)
                    return asset;
            }

            return null;
        }

        internal T GetRootAssetByName<T>(string name) where T : Object
        {
            foreach (var provider in m_RootAssetProviders)
            {
                var asset = provider.GetRootAssetByName<T>(name);
                if (asset != null)
                    return asset;
            }

            return null;
        }

        internal List<T> GetAllRootAssetsOfType<T>() where T : Object
        {
            HashSet<string> appendedPathsSet = new HashSet<string>();
            List<T> assets = new List<T>();
            foreach (var provider in m_RootAssetProviders)
            {
                provider.AppendUniqueRootAssetsOfType<T>(assets, appendedPathsSet);
            }
            return assets;
        }

        internal void AddRootAssetProvider(IRootAssetProvider provider)
        {
            m_RootAssetProviders.Add(provider);
        }
    }
}
