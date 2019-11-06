// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental
{
    public abstract class AssetsModifiedProcessor
    {
        public HashSet<string> assetsReportedChanged { get; set; }

        protected void ReportAssetChanged(string assetChanged)
        {
            if (assetsReportedChanged == null)
                throw new InvalidOperationException("Cannot call ReportAssetChanged outside of the OnAssetsModified callback");

            assetsReportedChanged.Add(assetChanged);
        }

        //Note: changedAssets including added and moved assets may be a usability issue. Review before making public.
        ///<summary>Fired when the [[AssetDatabase]] detects Asset changes before any Assets are imported.</summary>
        ///<param name="changedAssets">Paths to the Assets whose file contents have changed. Includes all added and moved Assets.</param>
        ///<param name="addedAssets">Paths to added Assets.</param>
        ///<param name="deletedAssets">Paths to deleted Assets.</param>
        ///<param name="movedAssets">Array of AssetMoveInfo that contains the previous and current location of any moved Assets.</param>
        ///<description> An Asset will only be reported moved if its .meta file is moved as well.</description>
        protected abstract void OnAssetsModified(string[] changedAssets, string[] addedAssets, string[] deletedAssets, AssetMoveInfo[] movedAssets);

        internal void Internal_OnAssetsModified(string[] changedAssets, string[] addedAssets, string[] deletedAssets, AssetMoveInfo[] movedAssets)
        {
            OnAssetsModified(changedAssets, addedAssets, deletedAssets, movedAssets);
        }
    }
}
