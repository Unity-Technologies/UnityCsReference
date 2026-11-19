// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a self transition in a <see cref="StateMachine"/>.
    /// </summary>
    /// <remarks>
    /// A self transition is a <see cref="ITransition"/> that is anchored on a single <see cref="IState"/> rather
    /// than connecting two states, so its <see cref="ITransition.FromState"/> and <see cref="ITransition.ToState"/>
    /// are the same state. Use this interface to distinguish a self transition from a state-to-state transition,
    /// for example <c>if (transition is ISelfTransition selfTransition)</c>.
    /// This interface is implemented by Unity and is not intended to be implemented by user code.
    /// </remarks>
    public interface ISelfTransition : ITransition
    {
    }
}
