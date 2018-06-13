// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor
{
    internal abstract class BaseSpeedTreeImporterTabUI : BaseAssetImporterTabUI
    {
        internal BaseSpeedTreeImporterTabUI(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {
        }

        protected IEnumerable<SpeedTreeImporter> importers
        {
            get { return (panelContainer as SpeedTreeImporterInspector).importers; }
        }

        protected bool upgradeMaterials
        {
            get { return (panelContainer as SpeedTreeImporterInspector).upgradeMaterials; }
        }
    }
}
