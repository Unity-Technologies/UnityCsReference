// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// Generic base class for BlockAsset importers: creates the BlockAsset ScriptableObject from an
    /// empty import source file. Subclass and add [ScriptedImporter] for the desired file extension.
    /// </summary>
    /// <typeparam name="TInterface">The IBlock interface type (e.g., IModelImporterBlock)</typeparam>
    /// <typeparam name="TBlockAsset">The BlockAsset type to create (e.g., ModelImporterBlockAsset)</typeparam>
    [UnityEngine.Internal.ExcludeFromDocs]
    public abstract class BlockAssetImporter<TInterface, TBlockAsset> : ScriptedImporter
        where TInterface : class, IBlock
        where TBlockAsset : BlockAssetBase<TInterface>, new()
    {
        /// <summary>
        /// The block collection that will be stored in the imported BlockAsset.
        /// This is edited in the inspector and stored in the .meta file.
        /// </summary>
        public BlockCollection<TInterface> blockCollection = new BlockCollection<TInterface>();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var blockAsset = ScriptableObject.CreateInstance<TBlockAsset>();
            blockAsset.blockCollection = blockCollection;
            // The importer object is reused across imports: round-trip so the asset owns its own block instances.
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(blockAsset), blockAsset);
            blockAsset.hideFlags = HideFlags.HideInInspector;
            blockAsset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            ctx.AddObjectToAsset("BlockAsset", blockAsset);
            ctx.SetMainObject(blockAsset);
        }
    }
}
