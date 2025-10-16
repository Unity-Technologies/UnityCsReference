// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for models backing a <see cref="RootView"/>.
    /// </summary>
    [UnityRestricted]
    internal abstract class RootViewModel : Model
    {
        protected RootViewModel() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootViewModel"/> class.
        /// </summary>
        /// <param name="guid">The unique identifier for this root view model.</param>
        /// <remarks>
        /// Use this constructor when implementing a subclass of <see cref="RootViewModel"/> to support a <see cref="RootView"/>.
        /// </remarks>
        protected RootViewModel(Hash128 guid)
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
