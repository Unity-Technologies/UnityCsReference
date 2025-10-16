// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// State component that stores lists of external assets that have been imported, moved or deleted since the last update.
    /// </summary>
    [UnityRestricted]
    internal class ExternalAssetsStateComponent : StateComponent<ExternalAssetsStateComponent.StateUpdater>
    {
        [UnityRestricted]
        internal class StateUpdater : BaseUpdater<ExternalAssetsStateComponent>
        {
            /// <summary>
            /// Adds a list of imported assets to the state component.
            /// </summary>
            /// <param name="importedAssets">The list of paths to the imported assets.</param>
            public void AddImportedAssets(IReadOnlyList<string> importedAssets)
            {
                m_State.m_ImportedAssets.AddRange(importedAssets);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Adds a list of moved assets to the state component.
            /// </summary>
            /// <param name="movedAssets">The list of paths to the moved assets.</param>
            /// <param name="originalAssetPaths">The original paths of the assets.</param>
            public void AddMovedAssets(IReadOnlyList<string> movedAssets, IReadOnlyList<string> originalAssetPaths)
            {
                for (var i = 0; i < movedAssets.Count; i++)
                {
                    m_State.m_MovedAssets.Add((movedAssets[i], originalAssetPaths[i]));
                }
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Adds a list of deleted assets to the state component.
            /// </summary>
            /// <param name="deletedAssets">The list of paths to the deleted assets.</param>
            public void AddDeletedAssets(IReadOnlyList<string> deletedAssets)
            {
                m_State.m_DeletedAssets.AddRange(deletedAssets);
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        List<string> m_ImportedAssets = new();
        List<(string currentPath, string originalPath)> m_MovedAssets = new();
        List<string> m_DeletedAssets = new();

        /// <summary>
        /// The list of recently imported assets.
        /// </summary>
        public IReadOnlyList<string> ImportedAssets => m_ImportedAssets;

        /// <summary>
        /// The list of recently moved assets.
        /// </summary>
        public IReadOnlyList<(string currentPath, string originalPath)> MovedAssets => m_MovedAssets;

        /// <summary>
        /// The list of recently deleted assets.
        /// </summary>
        public IReadOnlyList<string> DeletedAssets => m_DeletedAssets;

        // For use at the end of the Update cycle only.
        internal void Reset()
        {
            m_ImportedAssets.Clear();
            m_MovedAssets.Clear();
            m_DeletedAssets.Clear();
        }
    }
}
