// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Provides the required context for the factory (e.g., <see cref="BlackboardViewFactoryExtensions"/>) to create the UI for variables in the blackboard.
    /// </summary>
    /// <remarks>
    /// 'BlackboardCreationContext' distinguishes between two types of UI elements that can be created for variables: the <see cref="BlackboardField"/>, which
    /// represents the UI for the variable itself, and the <see cref="BlackboardVariablePropertyView"/>, which displays the quick settings section of a variable, showing its properties.
    /// </remarks>
    [UnityRestricted]
    internal class BlackboardCreationContext : IViewContext
    {
        /// <summary>
        /// The context to create <see cref="BlackboardField"/>s in the blackboard.
        /// </summary>
        /// <remarks>
        /// 'VariableCreationContext' represents the context for creating the <see cref="BlackboardField"/> of a variable, which displays the variable in the blackboard without its quick access section
        /// The quick access section is created separately in a different context (see <see cref="VariablePropertyCreationContext"/>).
        /// </remarks>
        public static readonly BlackboardCreationContext VariableCreationContext = new BlackboardCreationContext();

        /// <summary>
        /// The context to create <see cref="BlackboardVariablePropertyView"/>s in the blackboard.
        /// </summary>
        /// <remarks>
        /// 'VariablePropertyCreationContext' represents the context for creating the <see cref="BlackboardVariablePropertyView"/> of a variable, which defines the UI for the quick access
        /// settings section in the blackboard. This section displays various properties of the variable for easy access. The main UI for the variable itself is created in a separate
        /// context (see <see cref="VariableCreationContext"/>).
        /// </remarks>
        public static readonly BlackboardCreationContext VariablePropertyCreationContext = new BlackboardCreationContext();

        public bool Equals(IViewContext other)
        {
            return ReferenceEquals(this, other);
        }
    }
}
