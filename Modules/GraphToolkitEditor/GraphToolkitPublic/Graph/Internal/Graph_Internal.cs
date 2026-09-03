// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;
using Unity.Scripting.LifecycleManagement;

namespace Unity.GraphToolkit.Editor
{
    public partial class Graph : IGraphInternal
    {
        internal GraphModelImp m_Implementation;

        [AutoStaticsCleanupOnCodeReload]
        static Node.OptionDefinitionContext s_OptionDefinitionContext = new();

        void IGraphInternal.SetImplementation(GraphModelImp implementation)
        {
            m_Implementation = implementation;
        }

        void IGraphInternal.CheckImplementation() => CheckImplementation();

        void IGraphInternal.OnGraphChanged(ILogger logger) => OnGraphChanged((GraphLogger)logger);

        internal void CheckImplementation()
        {
            if (m_Implementation == null)
            {
                throw new InvalidOperationException("Only Graph instances returned by either GraphDatabase.LoadGraph or GraphDatabase.CreateGraph are valid.");
            }
        }

        internal void CallOnDefineSubgraphNodeOptions(IOptionsDefinition context)
        {
            s_OptionDefinitionContext.OptionsDefinition = context;
            OnDefineSubgraphNodeOptions(s_OptionDefinitionContext);
            s_OptionDefinitionContext.Finish();
        }

        IEnumerable<Type> IGraphInternal.InvokeBuildAvailableVariableTypes(IReadOnlyCollection<Type> baseSupportedTypes)
            => BuildAvailableVariableTypes(baseSupportedTypes);

        IEnumerable<Type> IGraphInternal.InvokeBuildAvailableConstantTypes(IReadOnlyCollection<Type> baseSupportedTypes)
            => BuildAvailableConstantTypes(baseSupportedTypes);
    }
}
