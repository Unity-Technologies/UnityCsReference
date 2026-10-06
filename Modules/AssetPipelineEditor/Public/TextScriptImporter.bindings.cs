// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AssetImporters;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [global::UnityEngine.NativeClass("TextScriptImporter", PersistentTypeId = 1031)]
    [NativeHeader("Modules/AssetPipelineEditor/Public/TextScriptImporter.h")]
    internal class TextScriptImporter : AssetImporter
    {
        internal TextScriptImporter(global::UnityEngine.EntityId id) : base(id) {}
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
