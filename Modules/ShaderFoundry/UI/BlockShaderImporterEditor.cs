// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderFoundry
{
    // We implement a no-op custom editor for the importer solely to get rid of the default
    // header rendered when inspecting a block shader asset.
    [CustomEditor(typeof(BlockShaderImporter))]
    internal sealed class BlockShaderImporterEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            hideInspector = true;
            return null;
        }
    }
}
