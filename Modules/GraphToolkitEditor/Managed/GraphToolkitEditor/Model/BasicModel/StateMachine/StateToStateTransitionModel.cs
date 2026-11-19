// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A transition between two states.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class StateToStateTransitionModel : TransitionSupportModel
    {
        /// <inheritdoc />
        public override bool IsSelfTransition => FromPort == ToPort;
    }
}
