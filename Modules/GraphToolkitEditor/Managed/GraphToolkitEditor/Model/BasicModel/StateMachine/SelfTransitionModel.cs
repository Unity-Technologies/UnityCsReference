// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A <see cref="TransitionSupportModel"/> that is anchored on a single state, rather than connecting two states.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class SelfTransitionModel : TransitionSupportModel, ISelfTransition
    {
        /// <inheritdoc />
        public override bool IsSelfTransition => true;

        public override string Title => "Self Transition";
    }
}
