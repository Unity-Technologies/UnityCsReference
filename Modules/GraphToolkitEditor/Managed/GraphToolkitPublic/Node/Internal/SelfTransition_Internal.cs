// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor.Implementation;

namespace Unity.GraphToolkit.Editor
{
    public abstract partial class SelfTransition
    {
        [NonSerialized]
        internal UserSelfTransitionModelImp m_Implementation;

        internal UserSelfTransitionModelImp GetImplementation()
        {
            if (m_Implementation == null)
            {
                CreateImplementation();
            }

            return m_Implementation;
        }

        internal void CreateImplementation()
        {
            new UserSelfTransitionModelImp().InitCustomTransition(this);
        }

        internal void SetImplementation(UserSelfTransitionModelImp implementation)
        {
            m_Implementation = implementation;
        }
    }
}
