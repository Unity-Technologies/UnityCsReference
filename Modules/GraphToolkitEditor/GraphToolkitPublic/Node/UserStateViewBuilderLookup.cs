// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;

namespace Unity.GraphToolkit.Editor;

class UserStateViewBuilderLookup : UserModelViewBuilderLookup<IUserStateView>
{
    protected override Type BaseModelType => typeof(State);
    protected override Type ViewGenericTypeDef => typeof(StateView<>);

    public IUserStateView Build(State state, IStateView view)
    {
        if (state == null)
            return null;

        var constructor = GetOrFindConstructor(state.GetType());
        if (constructor == null)
            return null;

        var instance = TryInstantiate(constructor);
        if (instance == null)
            return null;

        instance.Initialize(state, view);
        return instance;
    }

    internal class TestAccess
    {
        readonly UserStateViewBuilderLookup m_Lookup;

        public TestAccess(UserStateViewBuilderLookup lookup)
        {
            m_Lookup = lookup;
        }

        public int ConstructorCacheCount => m_Lookup.m_ConstructorCache.Count;

        public bool TryGetCachedConstructor(Type stateType, out ConstructorInfo constructor)
        {
            return m_Lookup.m_ConstructorCache.TryGetValue(stateType, out constructor);
        }
    }
}
