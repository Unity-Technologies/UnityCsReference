// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;

namespace Unity.GraphToolkit.Editor
{
    public partial class StateMachine : IGraphInternal
    {
        internal StateMachineImp m_Implementation;

        void IGraphInternal.SetImplementation(GraphModelImp implementation)
        {
            m_Implementation = implementation as StateMachineImp;
        }

        void IGraphInternal.CheckImplementation() => CheckImplementation();

        void IGraphInternal.OnGraphChanged(ILogger logger) => OnStateMachineChanged((StateMachineLogger)logger);

        internal void CheckImplementation()
        {
            if (m_Implementation == null)
            {
                throw new InvalidOperationException("Only StateMachine instances returned by either StateMachineDatabase.LoadStateMachine or StateMachineDatabase.CreateStateMachine are valid.");
            }
        }

        IEnumerable<Type> IGraphInternal.InvokeBuildAvailableVariableTypes(IReadOnlyCollection<Type> baseSupportedTypes)
            => BuildAvailableVariableTypes();

        IEnumerable<Type> IGraphInternal.InvokeBuildAvailableConstantTypes(IReadOnlyCollection<Type> baseSupportedTypes)
            => null;
    }
}
