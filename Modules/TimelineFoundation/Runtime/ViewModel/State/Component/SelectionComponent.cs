// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    /// <summary>
    /// Manages the state of Sequence selection
    /// </summary>
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal class SelectionComponent : Component<SelectionData>
    {
        /// <summary>
        /// Creates a SelectionComponent
        /// </summary>
        /// <param name="selectionProvider">Source of Sequence selection and selection change events.</param>
        /// <exception cref="InvalidOperationException">The selectionProvider is null</exception>
        public SelectionComponent(ISelectionProvider selectionProvider)
        {
            m_SelectionProvider = selectionProvider ?? throw new ArgumentNullException(nameof(selectionProvider),
                "A valid selection provider must be provided");

            SequenceSelection selection = selectionProvider.selection;

            m_CurrentSelection = new SelectionContainer(selection.tracks, selection.clips, selection.markers, selection.transitions);
            m_LastSelection = SelectionContainer.Empty;
            m_NewlySelected = SelectionContainer.Empty;
            m_NewlyDeselected = SelectionContainer.Empty;
            m_SelectionProvider.selectionChanged += OnSelectionChanged;
        }

        public override void Dispose()
        {
            base.Dispose();
            m_SelectionProvider.selectionChanged -= OnSelectionChanged;
        }

        protected override SelectionData GenerateReadOnlyData()
        {
            m_LastSelection = m_CurrentSelection;
            return new SelectionData(m_CurrentSelection, m_NewlySelected, m_NewlyDeselected);
        }

        public SelectionContainer CurrentSelection => m_CurrentSelection;

        /// <summary>
        /// When the selection provider updates the selection, this method performs a diff and marks the data as dirty
        /// </summary>
        /// <param name="selection">Current selection provided by ISelectionProvider.</param>
        void OnSelectionChanged(SequenceSelection selection)
        {
            m_CurrentSelection = new SelectionContainer(selection.tracks, selection.clips, selection.markers, selection.transitions);

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_NewlyDeselected =
                new SelectionContainer
                (
                    new List<UniqueID>(m_LastSelection.tracks.Except(m_CurrentSelection.tracks)),
                    new List<UniqueID>(m_LastSelection.clips.Except(m_CurrentSelection.clips)),
                    new List<UniqueID>(m_LastSelection.markers.Except(m_CurrentSelection.markers)),
                    new List<UniqueID>(m_LastSelection.transitions.Except(m_CurrentSelection.transitions))
                );

            m_NewlySelected =
                new SelectionContainer
                (
                    new List<UniqueID>(m_CurrentSelection.tracks.Except(m_LastSelection.tracks)),
                    new List<UniqueID>(m_CurrentSelection.clips.Except(m_LastSelection.clips)),
                    new List<UniqueID>(m_CurrentSelection.markers.Except(m_LastSelection.markers)),
                    new List<UniqueID>(m_CurrentSelection.transitions.Except(m_LastSelection.transitions))
                );
#pragma warning restore UA2001

            MarkAsDirty();
        }

        public void ChangeSelection(IEnumerable<UniqueID> toSelect)
        {
            m_SelectionProvider.SetSelection(toSelect);
        }

        public void Select(IEnumerable<UniqueID> toSelect)
        {
            m_SelectionProvider.Select(toSelect);
        }

        public void Deselect(IEnumerable<UniqueID> toDeselect)
        {
            m_SelectionProvider.Deselect(toDeselect);
        }

        public void ToggleSelection(IEnumerable<UniqueID> toToggle)
        {
            m_SelectionProvider.ToggleSelection(toToggle);
        }

        //Provides access to Sequence selection and provides an event on selection change
        ISelectionProvider m_SelectionProvider;

        //The cached data to provide in GenerateReadOnlyData
        SelectionContainer m_CurrentSelection;
        SelectionContainer m_NewlySelected;
        SelectionContainer m_NewlyDeselected;
        SelectionContainer m_LastSelection;
    }
}
