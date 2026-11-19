// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Internal contract implemented by the public graph wrapper types (<see cref="Graph"/> and
    /// <see cref="StateMachine"/>).
    /// </summary>
    /// <remarks>
    /// It lets the graph framework drive a wrapper polymorphically — instantiating it, binding it to its backing
    /// <see cref="GraphModelImp"/>, and forwarding lifecycle callbacks — without coupling the framework to a shared
    /// public base type. <see cref="Graph"/> and <see cref="StateMachine"/> are independent public classes; this
    /// interface is internal and never appears in the public API.
    /// </remarks>
    interface IGraphInternal
    {
        /// <summary>
        /// The name of the graph.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Binds this graph to its backing implementation.
        /// </summary>
        void SetImplementation(GraphModelImp implementation);

        /// <summary>
        /// Validate if the backing implementation is set.
        /// </summary>
        void CheckImplementation();

        /// <summary>
        /// Called when the graph is created or loaded in the editor.
        /// </summary>
        void OnEnable();

        /// <summary>
        /// Called when the graph is unloaded, or goes out of scope in the editor.
        /// </summary>
        void OnDisable();

        /// <summary>
        /// Called after the graph has changed.
        /// </summary>
        /// <remarks>
        /// The logger is typed as the internal <see cref="ILogger"/> contract, because <see cref="Graph"/> and
        /// <see cref="StateMachine"/> declare different logger types on their public callbacks. Each wrapper casts
        /// it back to the logger type its own callback declares.
        /// </remarks>
        void OnGraphChanged(ILogger logger);

        /// <summary>
        /// Invokes the method that builds the set of variable types offered in the blackboard for variable creation.
        /// </summary>
        IEnumerable<Type> InvokeBuildAvailableVariableTypes(IReadOnlyCollection<Type> baseSupportedTypes);

        /// <summary>
        /// Invokes the method that builds the set of types offered in the graph item library for constant nodes.
        /// </summary>
        IEnumerable<Type> InvokeBuildAvailableConstantTypes(IReadOnlyCollection<Type> baseSupportedTypes);
    }
}
