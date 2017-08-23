// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class GraphViewTypeFactory : BaseTypeFactory<GraphElementPresenter, GraphElement>
    {
        public GraphViewTypeFactory() : base(typeof(FallbackGraphElement))
        {
        }

        public override GraphElement Create(GraphElementPresenter key)
        {
            GraphElement elem = base.Create(key);
            if (elem != null)
            {
                elem.presenter = key;
            }
            return elem;
        }
    }
}
