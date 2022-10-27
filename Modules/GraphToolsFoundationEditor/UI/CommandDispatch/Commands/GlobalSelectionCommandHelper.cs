// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Class to help deal with all the <see cref="SelectionStateComponent"/>s present in the state.
    /// </summary>
    /// <remarks>
    /// When selecting an element, all other selected elements in all <see cref="RootView"/>s should be unselected.
    /// Since each view has its own <see cref="SelectionStateComponent"/>, we need to clear the selection on each of them.
    /// See <see cref="SelectElementsCommand.DefaultCommandHandler"/> for an example of how to use this class.
    /// </remarks>
    class GlobalSelectionCommandHelper
    {
        /// <summary>
        /// A list of <see cref="SelectionStateComponent.StateUpdater"/>s that can be used with a `using` statement.
        /// </summary>
        public class UpdateScopeList : DisposableList<SelectionStateComponent.StateUpdater>
        {
            SelectionStateComponent m_MainSelectionState;

            /// <summary>
            /// The <see cref="SelectionStateComponent.StateUpdater"/> for the <see cref="SelectionStateComponent"/> passed in the constructor.
            /// </summary>
            public SelectionStateComponent.StateUpdater MainUpdateScope => this.First(u => u.IsUpdaterForState(m_MainSelectionState));

            /// <summary>
            /// Initializes a new instance of the <see cref="UpdateScopeList"/> class.
            /// </summary>
            public UpdateScopeList(SelectionStateComponent mainSelectionState, IEnumerable<SelectionStateComponent.StateUpdater> updaters)
                : base(updaters)
            {
                m_MainSelectionState = mainSelectionState;
            }
        }

        SelectionStateComponent m_MainSelectionState;
        List<SelectionStateComponent> m_AllSelectionStates;

        /// <summary>
        /// All the <see cref="SelectionStateComponent"/> present in the state cast as <see cref="IUndoableStateComponent"/>.
        /// </summary>
        public IEnumerable<IUndoableStateComponent> UndoableSelectionStates => m_AllSelectionStates;

        /// <summary>
        /// The <see cref="SelectionStateComponent.StateUpdater"/> for each of the <see cref="SelectionStateComponent"/> present in the state.
        /// </summary>
        public UpdateScopeList UpdateScopes
        {
            get
            {
                var selectionUpdateScopes = m_AllSelectionStates.Select(s => s.UpdateScope);
                return new UpdateScopeList(m_MainSelectionState, selectionUpdateScopes);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalSelectionCommandHelper"/> class.
        /// </summary>
        /// <param name="mainSelectionState">The main selection state, received as parameter by the command.</param>
        public GlobalSelectionCommandHelper(SelectionStateComponent mainSelectionState)
        {
            m_MainSelectionState = mainSelectionState;
            m_AllSelectionStates = mainSelectionState.State.AllStateComponents.OfType<SelectionStateComponent>().ToList();
        }
    }
}
