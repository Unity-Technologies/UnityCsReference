// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// A reference to a <see cref="ModelImporterBlockAsset"/>. Implements
    /// <see cref="IModelImporterBlock"/> so it can live in a model importer's block collection.
    /// </summary>
    [MovedFrom(false, "UnityEditor.AssetImporters.ImportBlocks")]
    [UnityEngine.Internal.ExcludeFromDocs]
    [Serializable]
    [ImportBlock(1, "Model Importer Block Asset Reference", "Reference the Blocks in a Model Importer Block Asset")]
    public class ModelImporterBlockAssetReference : BlockAssetReference<ModelImporterBlockAsset>,
        IModelImporterBlock
    {
    }
}
