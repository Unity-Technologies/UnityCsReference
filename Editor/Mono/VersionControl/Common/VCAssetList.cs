// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.VersionControl
{
    // A class which encapsualtes a list of VC assets.  This class was made to add some extra functionaility
    // for filtering and counting items in the list of certain types.
    public class AssetList : List<Asset>
    {
        public AssetList() {}
        public AssetList(AssetList src)
        {
            // Deep Copy
            //foreach (Asset asset in src)
            //  Add(new Asset(asset));
        }

        // Filter a list of assets by a given set of states
        public AssetList Filter(bool includeFolder, params Asset.States[] states)
        {
            AssetList filter = new AssetList();

            if (includeFolder == false && (states == null || states.Length == 0))
                return filter;

            foreach (Asset asset in this)
            {
                if (asset.isFolder)
                {
                    if (includeFolder)
                        filter.Add(asset);
                }
                else
                {
                    if (asset.IsOneOfStates(states))
                    {
                        filter.Add(asset);
                    }
                }
            }

            return filter;
        }

        // Count the list of assets by given a set of states.
        // TODO: This is called quite often so it may be an idea to cache this
        public int FilterCount(bool includeFolder, params Asset.States[] states)
        {
            int count = 0;

            if (includeFolder == false && states == null)
                return this.Count;

            foreach (Asset asset in this)
            {
                if (asset.isFolder)
                    ++count;
                else
                {
                    if (asset.IsOneOfStates(states))
                    {
                        ++count;
                    }
                }
            }

            return count;
        }

        // Create an optimised list of assets by removing children of folders in the same list
        public AssetList FilterChildren()
        {
            AssetList unique = new AssetList();
            unique.AddRange(this);

            foreach (Asset asset in this)
                unique.RemoveAll(p => p.IsChildOf(asset));

            return unique;
        }
    }
}
