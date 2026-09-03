// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;

namespace Unity.GraphToolkit.Editor;

class UserNodeViewBuilderLookup : UserModelViewBuilderLookup<IUserNodeView>
{
    protected override Type BaseModelType => typeof(Node);
    protected override Type ViewGenericTypeDef => typeof(NodeView<>);

    public IUserNodeView Build(Node node, INodeView view)
    {
        if (node == null)
            return null;

        var constructor = GetOrFindConstructor(node.GetType());
        if (constructor == null)
            return null;

        var instance = TryInstantiate(constructor);
        if (instance == null)
            return null;

        instance.Initialize(node, view);
        return instance;
    }

    internal class TestAccess
    {
        readonly UserNodeViewBuilderLookup m_Lookup;

        public TestAccess(UserNodeViewBuilderLookup lookup)
        {
            m_Lookup = lookup;
        }

        public int ConstructorCacheCount => m_Lookup.m_ConstructorCache.Count;

        public bool TryGetCachedConstructor(Type nodeType, out ConstructorInfo constructor)
        {
            return m_Lookup.m_ConstructorCache.TryGetValue(nodeType, out constructor);
        }
    }
}
