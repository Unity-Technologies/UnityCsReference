// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEditor;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Observer that updates the external variable references of a graph model when the external assets state changes.
    /// </summary>
    [UnityRestricted]
    internal class ExternalVariablesUpdater : StateObserver
    {
        ExternalAssetsStateComponent m_ExternalAssetsState;
        GraphModelStateComponent m_GraphModelState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalVariablesUpdater"/> class.
        /// </summary>
        public ExternalVariablesUpdater(ExternalAssetsStateComponent externalAssetsState, GraphModelStateComponent graphModelState)
            : base(new IStateComponent[] { externalAssetsState },
                   new IStateComponent[] { graphModelState })
        {
            m_ExternalAssetsState = externalAssetsState;
            m_GraphModelState = graphModelState;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using var observation = this.ObserveState(m_ExternalAssetsState);
            if (observation.UpdateType != UpdateType.None)
            {
                var graphModel = m_GraphModelState.GraphModel;
                if (graphModel == null)
                    return;

                var needToUpdate = false;
                var externalSources = graphModel.GetExternalVariableDeclarationModelSources() ?? Array.Empty<ExternalVariableSource>();
                foreach (var externalSource in externalSources)
                {
                    if (externalSource is not GraphObjectVariableSource assetVariableSource)
                        continue;

                    var assetPath = AssetDatabase.GUIDToAssetPath(assetVariableSource.GetAssetGUID());
                    if (string.IsNullOrEmpty(assetPath))
                        continue;

                    if (m_ExternalAssetsState.ImportedAssets.Contains(assetPath))
                    {
                        needToUpdate = true;
                        break;
                    }

                    if (m_ExternalAssetsState.DeletedAssets.Contains(assetPath))
                    {
                        needToUpdate = true;
                        break;
                    }

                    foreach (var (currentPath, originalPath) in m_ExternalAssetsState.MovedAssets)
                    {
                        if (currentPath == assetPath || originalPath == assetPath)
                        {
                            needToUpdate = true;
                            break;
                        }
                    }
                }

                if (!needToUpdate)
                    return;

                using var updater = m_GraphModelState.UpdateScope;
                using var changeScope = graphModel.ChangeDescriptionScope;
                graphModel.UpdateExternalVariableDeclarationReferences();
                updater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
