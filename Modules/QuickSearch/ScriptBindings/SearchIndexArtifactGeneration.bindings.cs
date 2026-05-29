// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Experimental;
using UnityEngine.Bindings;

namespace UnityEditor.Search
{
    [NativeHeader("Modules/QuickSearch/SearchIndexArtifactGeneration.h")]
    static partial class SearchIndexArtifactGeneration
    {
        /// <summary>
        /// Creates artifact keys for the given asset paths and importer types. Puts the results into the provided artifactImportDataBatch.
        /// </summary>
        /// <param name="assetPaths">Assets paths to generate the artifacts from. This array does not need to be full, see <see cref="assetPathCount"/>.</param>
        /// <param name="assetPathCount">Actual number of asset paths in the <see cref="assetPaths"/> array. This parameter is useful when batching this operation.</param>
        /// <param name="importerTypes">Array containing the different importer types. One per artifact.</param>
        /// <param name="artifactImportDataBatch">A <see cref="SearchIndexArtifactImportData.Batch"/> where the generated artifact keys will be stored.</param>
        static void CreateArtifactKeys(string[] assetPaths, int assetPathCount, Type[] importerTypes, in SearchIndexArtifactImportData.Batch artifactImportDataBatch)
        {
            CreateArtifactKeys(assetPaths, assetPathCount, importerTypes, artifactImportDataBatch.ArtifactKeys);
        }

        static extern void CreateArtifactKeys(string[] assetPaths, int assetPathCount, Type[] importerTypes, Span<ArtifactKey> artifactKeys);

        /// <summary>
        /// Updates the on demand progress status for the given artifact import data. Places available or failed artifacts at the beginning of the batch.
        /// </summary>
        /// <param name="artifactImportDataBatch">A <see cref="SearchIndexArtifactImportData.Batch"/> that will be updated.</param>
        /// <returns>The number of available or failed artifacts.</returns>
        public static ulong UpdateOnDemandArtifactsProgress(in SearchIndexArtifactImportData.Batch artifactImportDataBatch)
        {
            return UpdateOnDemandArtifactsProgress(artifactImportDataBatch.ImporterHashCodes, artifactImportDataBatch.OutOfProcessImportStates, artifactImportDataBatch.ArtifactKeys, artifactImportDataBatch.ImportResultIds);
        }

        static extern ulong UpdateOnDemandArtifactsProgress(Span<int> importerHashCodes, Span<OutOfProcessImportState> onDemandStates, Span<ArtifactKey> artifactKeys, Span<ImportResultID> importResultIds);

        /// <summary>
        /// Requests the production of artifacts for the given artifact import data.
        /// </summary>
        /// <param name="artifactImportDataBatch">A <see cref="SearchIndexArtifactImportData.Batch"/> that will be updated. This method updates the import result id of each element.</param>
        public static void RequestProduceArtifacts(in SearchIndexArtifactImportData.Batch artifactImportDataBatch)
        {
            RequestProduceArtifacts(artifactImportDataBatch.ArtifactKeys, artifactImportDataBatch.ImportResultIds);
        }

        static extern void RequestProduceArtifacts(Span<ArtifactKey> artifactKeys, Span<ImportResultID> importResultIds);
    }
}
