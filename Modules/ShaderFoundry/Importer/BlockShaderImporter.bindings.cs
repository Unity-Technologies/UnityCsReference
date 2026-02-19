// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.ShaderFoundry;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/ShaderFoundry/Importer/BlockShaderImporter.h")]
    [NativeClass("ShaderFoundry::BlockShaderImporter")]
    internal sealed partial class BlockShaderImporter : AssetImporter
    {
        public extern bool EmbedBlocksInGeneratedShader { get; set; }

        public extern string GetShaderSource();

        [NativeMethod(IsThreadSafe = true)] internal static extern BlockShaderAssetDescription ImportManagedSyntaxTree(SyntaxTree tree,
            string assetName, string importAtPath);
        // TODO @ SHADERS SHADERS-90:
        // Figure out what extra bindings we want.

        // assetName is the name of the block shader asset to be generated.
        // importAtPath is the path used to resolve import statements.
        public static BlockShaderAssetDescription GenerateAssetData(SyntaxTree syntaxTree, string assetName,
            string importAtPath)
        {
            if (syntaxTree == null)
                throw new InvalidOperationException("Syntax tree must not be null.");
            if (!syntaxTree.GetRoot().IsValid)
                throw new InvalidOperationException("Syntax tree must have a FileRootNode.");
            if (string.IsNullOrEmpty(assetName))
                throw new InvalidOperationException("Asset name must not be empty.");
            if (string.IsNullOrEmpty(importAtPath))
                throw new InvalidOperationException("Import path must not be empty.");

            return ImportManagedSyntaxTree(syntaxTree, assetName, importAtPath);
        }
    }
}
