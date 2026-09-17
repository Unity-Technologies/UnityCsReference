// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Multiplayer.Center.Common;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// Description of each <see cref="GameGenre"/>.
    /// </summary>
    /// <remarks>
    /// This is used to change the Multiplayer Center UI by ordering, changing the name,
    /// and changing the content of the game genre without touching the public enum itself.
    /// </remarks>
    class GameGenreList : EnumBasedDescription<GameGenre, GameGenreDescription>
    {
        protected override GameGenreDescription CreateNewDescription(GameGenre type)
        {
            return new GameGenreDescription
            {
                Genre = type,
                Name = type.ToString(),
            };
        }
    }

    [Serializable]
    struct GameGenreDescription : IEnumDescription<GameGenre>, IListViewDisplayName
    {
        public GameGenre Type => Genre;

        [ReadOnlyInspector]
        public GameGenre Genre;
        public string Name;

        [TextArea]
        public string Description;
        [Tooltip("Use this to define a header template for the game genre.")]
        public VisualTreeAsset Header;
        public Vector2 Complexity;
        [TextArea]
        public string ComplexityDescription;
        public string ComplexitySummary;

        public Vector2 Cost;
        [TextArea]
        public string CostDescription;
        public string CostSummary;

        [TextArea]
        public string Architecture;
        public string ArchitectureSummary;

        [CreateProperty]
        public string DisplayName => Name;
    }
}
