// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using System.Buffers;
using System.Diagnostics;
using UnityEditor.Experimental;
using UnityEngine;

namespace UnityEditor.Search
{
    using Task = SearchTask<TaskData>;

    static partial class SearchIndexArtifactGeneration
    {
        public delegate void ArtifactsImportedCallback(SearchIndexArtifactImportContext context, Task task, in SearchIndexArtifactImportData.Batch artifactImportDataBatch);
        public delegate void AllArtifactsProducedCallback(SearchIndexArtifactImportContext context, Task task);

        /// <summary>
        /// Gets the ArtifactKey for a given asset path in the context of the provided SearchDatabase settings.
        /// </summary>
        /// <param name="settings">SearchDatabase settings that dictate what importer to use for the given asset.</param>
        /// <param name="assetPath">Path of the asset used to generate the artifact key.</param>
        /// <returns>The artifact key of the specified asset.</returns>
        public static ArtifactKey GetAssetArtifactKey(SearchDatabase.Settings settings, string assetPath)
        {
            var indexImporterType = SearchDatabase.GetIndexImporterTypeForAsset(settings, assetPath);
            var assetGuid = AssetDatabase.GUIDFromAssetPath(assetPath);
            return AssetDatabaseExperimental.CreateArtifactKey(assetGuid, indexImporterType);
        }

        /// <summary>
        /// Resolves artifact in batches within a given time limit. This method will either request new artifacts to be produced or update the progress of already requested artifacts.
        /// When artifacts become available or fail, they are reported back through the onArtifactsImported callback. The method returns true if all artifacts have been imported or failed, otherwise false.
        /// Make sure to keep the context alive until all artifacts are imported AND have been process through the onArtifactsImported parameter. The onArtifactsImported callback will be called
        /// with a <see cref="SearchIndexArtifactImportData.Batch"/> pointing to data living in the context.
        /// </summary>
        /// <param name="context">The SearchIndexArtifactImportContext containing all live data for this artifact production session.</param>
        /// <param name="artifactProductionTask">The task for producing the artifacts. Used to report the amount of produced artifacts.</param>
        /// <param name="onArtifactsImported">Callback called when new imported or failed artifacts are available.</param>
        /// <returns>True if all artifacts have been imported or failed.</returns>
        public static bool ResolveArtifactsBatch(SearchIndexArtifactImportContext context, Task artifactProductionTask, ArtifactsImportedCallback onArtifactsImported)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed <= context.ArtifactProductionFrameTimeLimit)
            {
                if (!context.Db || artifactProductionTask.Canceled())
                    return true;
                if (context.ImportedCount >= context.TotalCount)
                    return true;

                // If no requested artifacts or all requested artifacts have been processed, request more artifacts.
                if (context.RequestedCount == 0 || context.ImportedCount == context.RequestedCount)
                {
                    // We request artifacts in batches because requesting too many artifacts at once can be slow. CreateArtifactsData and RequestProduceArtifacts
                    // scale up linearly with the number of artifacts requested, so we limit the number of artifacts requested at once to avoid blocking the main thread for too long.
                    // Also, the time for RequestProduceArtifacts seems to scale by the number of existing assets, their versions and dependencies
                    // (i.e. changing a custom dependency increases the time it takes to request production of an artifact), so we need to make sure to take a batch size that isn't too
                    // large to prevent us from fitting in our time limit.
                    // Also, when getting the span of source and import data, we use the ImportedCount as the starting point. This avoids potential issues
                    // when restarting pending artifacts, as those will already have been requested.

                    var batchSize = Math.Min(context.BatchSize, context.TotalCount - context.ImportedCount);
                    var productionBatch = context.ArtifactImportData.AsBatch(context.ImportedCount, batchSize);
                    var tempSources = ArrayPool<string>.Shared.Rent(batchSize);
                    for (var i = 0; i < batchSize; ++i)
                    {
                        tempSources[i] = context.Sources[context.ImportedCount + i];
                    }
                    CreateArtifactsData(context.Db, tempSources, batchSize, in productionBatch);
                    ArrayPool<string>.Shared.Return(tempSources);

                    RequestProduceArtifacts(in productionBatch);
                    context.RequestedCount = context.ImportedCount + batchSize;
                    context.LastArtifactUpdateTimeInSecond = EditorApplication.timeSinceStartup;

                    // Check timeLimit again, since requesting artifacts may take some time
                    if (sw.Elapsed > context.ArtifactProductionFrameTimeLimit)
                        break;
                }

                // Update the progress of all requested artifacts
                // This will move all available or failed artifacts to the front of the batch. This is needed so we can have a
                // contiguous spans of available or failed artifacts data to update their ImportResultId, and to have a contiguous region in memory for
                // those artifacts when we send them through onArtifactsImported callback.
                var updateBatch = context.ArtifactImportData.AsBatch(context.ImportedCount, context.RequestedCount - context.ImportedCount);

                // If the batch wasn't totally processed, request it again:
                if (context.RestartPendingArtifacts)
                {
                    RequestProduceArtifacts(in updateBatch);
                    // Check timeLimit again, since requesting artifacts may take some time
                    if (sw.Elapsed > context.ArtifactProductionFrameTimeLimit)
                        break;
                }

                var availableOrFailedArtifactsCount = (int)UpdateOnDemandArtifactsProgress(in updateBatch);

                // Only send report and callback if we have new imported artifacts
                if (availableOrFailedArtifactsCount > 0)
                {
                    var importedBatch = context.ArtifactImportData.AsBatch(context.ImportedCount, availableOrFailedArtifactsCount);
                    AssetDatabaseExperimental.LookupArtifacts(importedBatch.ArtifactKeys, importedBatch.ImportResultIds);

                    context.ImportedCount += availableOrFailedArtifactsCount;
                    context.LastArtifactUpdateTimeInSecond = EditorApplication.timeSinceStartup;
                    artifactProductionTask.Report(context.ImportedCount, context.TotalCount);
                    onArtifactsImported(context, artifactProductionTask, in importedBatch);
                }
                else if (context.SkipToNextFrameIfNoUpdate)
                {
                    // If there is no available or failed artifacts, break to avoid spinning for nothing.
                    // We can continue on the next frame.
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// Asynchronously gets all index dependencies. Calls onDependenciesScanned when done.
        /// </summary>
        /// <param name="db">SearchDatabase on which to get the index dependencies.</param>
        /// <param name="getDependenciesTask">Task that oversees the scanning of the dependencies.</param>
        /// <param name="onDependenciesScanned">Callback called when the dependencies have been collected.</param>
        public static void GetIndexDependenciesAsync(SearchDatabase db, Task getDependenciesTask, Action<IList<string>, Task> onDependenciesScanned)
        {
            List<string> paths = null;
            getDependenciesTask.RunThread(() =>
            {
                getDependenciesTask.Report("Scanning dependencies...");
                paths = db.index.GetDependencies();
            }, () => onDependenciesScanned(paths, getDependenciesTask));
        }

        /// <summary>
        /// Produces all artifacts over multiple frames.
        /// When artifacts become available or failed, they are reported back through the onArtifactsImported callback.
        /// Make sure to keep the context alive until all artifacts are imported AND have been process through the onArtifactsImported parameter. The onArtifactsImported callback will be called
        /// with a <see cref="SearchIndexArtifactImportData.Batch"/> pointing to data living in the context.
        /// </summary>
        /// <param name="context">The SearchIndexArtifactImportContext containing all live data for this artifact production session.</param>
        /// <param name="artifactProductionTask">The task for producing the artifacts. Used to report the amount of produced artifacts.</param>
        /// <param name="onArtifactsImported">Callback called when new imported or failed artifacts are available. The <see cref="SearchIndexArtifactImportData.Batch"/> points to data on the context, so do not dispose of the context until you are done
        /// with the artifacts in your onArtifactsImported handler.</param>
        /// <param name="onArtifactsProduced">Callback called when the production is done, i.e. all artifacts have been imported or failed.</param>
        public static void ProduceArtifacts(SearchIndexArtifactImportContext context, Task artifactProductionTask, ArtifactsImportedCallback onArtifactsImported, AllArtifactsProducedCallback onArtifactsProduced)
        {
            if (!context.Db || artifactProductionTask.Canceled())
                return;

            artifactProductionTask.Report($"Producing {context.TotalCount} artifacts...");
            artifactProductionTask.total = context.TotalCount;

            ResolveArtifacts(context, artifactProductionTask, onArtifactsImported, onArtifactsProduced);
        }

        /// <summary>
        /// Produces all artifacts over multiple frames. This method enqueues itself until all artifacts have been produced.
        /// When artifacts become available or fail, they are reported back through the onArtifactsImported callback.
        /// Make sure to keep the context alive until all artifacts are imported AND have been process through the onArtifactsImported parameter. The onArtifactsImported callback will be called
        /// with a <see cref="SearchIndexArtifactImportData.Batch"/> pointing to data living in the context.
        /// </summary>
        /// <param name="context">The SearchIndexArtifactImportContext containing all live data for this artifact production session.</param>
        /// <param name="artifactProductionTask">The task for producing the artifacts. Used to report the amount of produced artifacts.</param>
        /// <param name="onArtifactsImported">Callback called when new imported or failed artifacts are available. The <see cref="SearchIndexArtifactImportData.Batch"/> points to data on the context, so do not dispose of the context until you are done
        /// with the artifacts in your onArtifactsImported handler.</param>
        /// <param name="onArtifactsProduced">Callback called when the production is done, i.e. all artifacts have been imported or failed.</param>
        static void ResolveArtifacts(SearchIndexArtifactImportContext context, Task artifactProductionTask, ArtifactsImportedCallback onArtifactsImported, AllArtifactsProducedCallback onArtifactsProduced)
        {
            try
            {
                if (!context.Db || artifactProductionTask.Canceled())
                    return;

                if (ResolveArtifactsBatch(context, artifactProductionTask, onArtifactsImported))
                {
                    if (artifactProductionTask.Canceled())
                        return;
                    onArtifactsProduced(context, artifactProductionTask);
                    return;
                }

                // Enqueue to continue on next frame
                Dispatcher.Enqueue(() => ResolveArtifacts(context, artifactProductionTask, onArtifactsImported, onArtifactsProduced), context.ArtifactProductionDispatcherDelay.TotalSeconds);
            }
            catch (Exception err)
            {
                artifactProductionTask.ResolveException(err);
            }
        }

        /// <summary>
        /// Creates artifact import data for the given asset paths. This will set the initial values for each artifact import data, and set the actual artifact key for them.
        /// </summary>
        /// <param name="db">SearchDatabase that owns the artifacts and the index that will merge them.</param>
        /// <param name="assetPaths">Assets paths to generate the artifacts from.</param>
        /// <param name="populatedArtifactImportDataBatch">A <see cref="SearchIndexArtifactImportData.Batch"/> where the generated import data will be stored.</param>
        /// <exception cref="ArgumentException">Throws exception if the length of the batch does not match the actual number of asset paths.</exception>
        public static void CreateArtifactsData(SearchDatabase db, string[] assetPaths, in SearchIndexArtifactImportData.Batch populatedArtifactImportDataBatch)
        {
            CreateArtifactsData(db, assetPaths, assetPaths.Length, in populatedArtifactImportDataBatch);
        }

        /// <summary>
        /// Creates artifact import data for the given asset paths. This will set the initial values for each artifact import data, and set the actual artifact key for them.
        /// </summary>
        /// <param name="db">SearchDatabase that owns the artifacts and the index that will merge them.</param>
        /// <param name="assetPaths">Assets paths to generate the artifacts from. This array does not need to be full, see <see cref="assetPathCount"/>.</param>
        /// <param name="assetPathCount">Actual number of asset paths in the <see cref="assetPaths"/> array. This parameter is useful when batching this operation.</param>
        /// <param name="populatedArtifactImportDataBatch">A <see cref="SearchIndexArtifactImportData.Batch"/> where the generated import data will be stored.</param>
        /// <exception cref="ArgumentException">Throws exception if the length of the batch does not match the actual number of asset paths.</exception>
        public static void CreateArtifactsData(SearchDatabase db, string[] assetPaths, int assetPathCount, in SearchIndexArtifactImportData.Batch populatedArtifactImportDataBatch)
        {
            assetPathCount = Math.Min(assetPathCount, assetPaths.Length);
            if (populatedArtifactImportDataBatch.Length != assetPathCount)
                throw new ArgumentException("The length of populatedArtifactsImportData must match the length of assetPaths.");

            var importerTypes = new Type[assetPathCount];

            for (var i = 0; i < assetPathCount; ++i)
            {
                var importerHashCode = SearchDatabase.GetImporterHashCode(db.settings, assetPaths[i]);

                // Setup default values
                populatedArtifactImportDataBatch.ImporterHashCodes[i] = importerHashCode;
                populatedArtifactImportDataBatch.OnDemandStates[i] = OnDemandState.Unavailable;
                populatedArtifactImportDataBatch.ImportResultIds[i] = default;
                importerTypes[i] = SearchIndexArtifactImporter.GetIndexImporterType(importerHashCode);
            }
            CreateArtifactKeys(assetPaths, assetPathCount, importerTypes, in populatedArtifactImportDataBatch);
        }

        private static void PrintBatch(string title, in SearchIndexArtifactImportData.Batch batch, SearchIndexArtifactImportContext context, bool printAllProcessedAsset = false)
        {
            UnityEngine.Debug.Log($"{title} - batchSize:{batch.Length} ImportedCount:{context.ImportedCount} RequestedCount:{context.RequestedCount} Restart:{context.RestartPendingArtifacts}");
            for (var i = 0; i < batch.Length; ++i)
            {
                var path = AssetDatabase.GUIDToAssetPath(batch.ArtifactKeys[i].guid);
                UnityEngine.Debug.Log($"    #{i} {path} - {batch.ArtifactKeys[i].guid}");
            }

            if (printAllProcessedAsset)
            {
                UnityEngine.Debug.Log($"    Asset Processed Queue:{context.ArtifactImportData.Length}  imported: {context.ImportedCount}");
                for (var i = 0; i < context.ArtifactImportData.Length; ++i)
                {
                    var guid = context.ArtifactImportData.ArtifactKeys[i].guid;
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    UnityEngine.Debug.Log($"        #{i} {path} - {guid} : IsImported : {i < context.ImportedCount}");
                }
            }
        }
    }
}
