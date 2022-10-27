// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Stores the <see cref="DeclarationModel"/> to highlight.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    class DeclarationHighlighterStateComponent : StateComponent<DeclarationHighlighterStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="DeclarationHighlighterStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<DeclarationHighlighterStateComponent>
        {
            /// <summary>
            /// Sets the list of declaration to highlight.
            /// </summary>
            /// <param name="sourceStateHashGuid">The unique identifier of the object that requests elements to be highlighted.</param>
            /// <param name="declarations">The declarations to highlight.</param>
            public void SetHighlightedDeclarations(Hash128 sourceStateHashGuid, IEnumerable<DeclarationModel> declarations)
            {
                var newDeclarations = declarations.Select(m => m.Guid).ToList();
                var changedDeclarations = new HashSet<SerializableGUID>(newDeclarations);

                if (m_State.m_HighlightedDeclarations.TryGetValue(sourceStateHashGuid, out var currentDeclarations))
                {
                    // changedDeclarations = changedDeclarations XOR currentDeclarations
                    changedDeclarations.SymmetricExceptWith(currentDeclarations);
                }

                m_State.m_HighlightedDeclarations[sourceStateHashGuid] = newDeclarations;

                m_State.CurrentChangeset.ChangedModels.UnionWith(changedDeclarations);
                m_State.SetUpdateType(UpdateType.Partial);
            }
        }

        ChangesetManager<SimpleChangeset> m_ChangesetManager = new ChangesetManager<SimpleChangeset>();

        /// <inheritdoc />
        public override IChangesetManager ChangesetManager => m_ChangesetManager;

        SimpleChangeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        // The highlighted declaration, grouped by the id of the object (usually a view) that
        // asked for the elements to be highlighted.
        Dictionary<Hash128, List<SerializableGUID>> m_HighlightedDeclarations;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeclarationHighlighterStateComponent" /> class.
        /// </summary>
        public DeclarationHighlighterStateComponent()
        {
            m_HighlightedDeclarations = new Dictionary<Hash128, List<SerializableGUID>>();
        }

        /// <summary>
        /// Gets the highlighted state of a declaration model.
        /// </summary>
        /// <param name="model">The declaration model.</param>
        /// <returns>True is the UI for the model should be highlighted. False otherwise.</returns>
        public bool GetDeclarationModelHighlighted(DeclarationModel model)
        {
            if (model != null)
            {
                foreach (var declarationForView in m_HighlightedDeclarations)
                {
                    if (declarationForView.Value.Contains(model.Guid))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <returns>The aggregated changeset.</returns>
        public SimpleChangeset GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is DeclarationHighlighterStateComponent highlighterStateComponent)
            {
                var newDeclarations = new HashSet<SerializableGUID>(highlighterStateComponent.m_HighlightedDeclarations.Values.SelectMany(v => v));

                var changedDeclarations = new HashSet<SerializableGUID>(m_HighlightedDeclarations.Values.SelectMany(v => v));

                changedDeclarations.SymmetricExceptWith(newDeclarations);
                CurrentChangeset.ChangedModels.UnionWith(changedDeclarations);
                SetUpdateType(UpdateType.Partial);

                m_HighlightedDeclarations = highlighterStateComponent.m_HighlightedDeclarations;
                highlighterStateComponent.m_HighlightedDeclarations = null;
            }
        }
    }
}
