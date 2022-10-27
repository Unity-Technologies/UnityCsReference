// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for models backing a <see cref="RootView"/>.
    /// </summary>
    abstract class RootViewModel : Model
    {
        protected RootViewModel() { }

        protected RootViewModel(SerializableGUID guid)
        : base(guid)
        {
        }

        /// <summary>
        /// Adds the <see cref="StateComponent{TUpdater}"/>s of this object to the <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The state to which to add the components.</param>
        public abstract void AddToState(IState state);

        /// <summary>
        /// Removes the <see cref="StateComponent{TUpdater}"/>s of this object from the <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The state from which to remove the components.</param>
        public abstract void RemoveFromState(IState state);
    }
}
