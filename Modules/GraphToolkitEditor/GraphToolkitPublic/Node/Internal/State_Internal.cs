// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor.Implementation;
using Unity.Scripting.LifecycleManagement;

namespace Unity.GraphToolkit.Editor
{
    public abstract partial class State
    {
        [NonSerialized]
        internal StateModel m_Implementation;

        // State has its own pooled context so that defining a state cannot clobber the builders in use by a node define.
        [AutoStaticsCleanupOnCodeReload]
        static Node.OptionDefinitionContext s_OptionDefinitionContext = new();

        internal void CallOnDefineOptions(IOptionsDefinition context)
        {
            s_OptionDefinitionContext.OptionsDefinition = context;
            try
            {
                OnDefineOptions(s_OptionDefinitionContext);
            }
            finally
            {
                // The context is shared by every state, so a throwing callback must not leave pending options behind.
                s_OptionDefinitionContext.Finish();
            }
        }

        StateModel IState.StateModel => GetImplementation();

        internal StateModel GetImplementation()
        {
            if (m_Implementation == null)
            {
                CreateImplementation();
            }

            return m_Implementation;
        }

        internal void CreateImplementation()
        {
            new UserStateModelImp().InitCustomState(this);
        }

        internal void SetImplementation(StateModel implementation)
        {
            m_Implementation = implementation;
        }
    }
}
