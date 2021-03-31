// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AssetImporters;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetPipelineEditor/Public/TextScriptImporter.h")]
    internal class TextScriptImporter : AssetImporter
    {
    }

    [CustomEditor(typeof(TextScriptImporter))]
    internal class TextScriptImporterEditor : AssetImporterEditor
    {
        protected override bool needsApplyRevert => false;
        public override void OnInspectorGUI()
        {
        }
    }
}
