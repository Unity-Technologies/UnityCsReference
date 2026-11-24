// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AssetImporters;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class BlocksTabUI : BaseAssetImporterTabUI
    {
        public BlocksTabUI(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {
        }

        internal override void OnEnable() {}

        public override void OnInspectorGUI() {}

        public override VisualElement CreateInspectorGUI() => panelContainer.CreateCombinedImportCustomizationSection();
    }
}
