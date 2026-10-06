// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Multiplayer.Center.Common;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.Center.Editor.Models
{
    /// <summary>
    /// Provides a list of <see cref="BuildingBlockDescription"/>s for each <see cref="GameGenre"/>.
    /// </summary>
    class GenreAssociatedBuildingBlocks: EnumBasedDescription<GameGenre, BuildingBlockList>
    {
        protected override BuildingBlockList CreateNewDescription(GameGenre type)
        {
            return new BuildingBlockList { m_Genre = type };
        }
    }

    /// <summary>
    /// Contains all the editable properties that describe a building block.
    /// </summary>
    [Serializable]
    struct BuildingBlockDescription
    {
        public BuildingBlockDescription() { }

        [SerializeField] private string m_Title = string.Empty;
        [SerializeField] private string m_Description = string.Empty;
        [SerializeField] private string m_AssetStoreURL = string.Empty;
        [SerializeField] private Texture m_Preview = null;

        [CreateProperty]
        public string Title => L10n.Tr(m_Title, null);

        [CreateProperty]
        public string Description => L10n.Tr(m_Description, null);

        [CreateProperty]
        public string AssetStoreURL => L10n.Tr(m_AssetStoreURL, null);

        [CreateProperty]
        public Texture Preview => m_Preview;
    }

    [Serializable]
    struct BuildingBlockList : IEnumDescription<GameGenre>, IListViewDisplayName
    {
        public BuildingBlockList() { }

        [ReadOnlyInspector,SerializeField]
        internal GameGenre m_Genre;

        [SerializeField]
        private BuildingBlockDescription[] m_Blocks = Array.Empty<BuildingBlockDescription>();

        [CreateProperty]
        public GameGenre Type => m_Genre;

        [CreateProperty]
        public string DisplayName => m_Genre.ToString("G");

        [CreateProperty]
        public int Length => m_Blocks.Length;
    }
}
