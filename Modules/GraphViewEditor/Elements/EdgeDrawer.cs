// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public interface IEdgeDrawerContainer
    {
        void EdgeDirty();
    }
    public class EdgeDrawer : VisualElement
    {
        public GraphElement element
        {
            get; set;
        }
        public EdgeDrawer()
        {
            pickingMode = PickingMode.Ignore;
        }

        public virtual bool EdgeIsInThisDrawer(Edge edge)
        {
            return (edge.input != null && edge.input.node == element) ||
                (edge.output != null && edge.output.node == element);
        }

        public override void DoRepaint()
        {
            GraphView view = GetFirstAncestorOfType<GraphView>();

            GL.PushMatrix();
            Matrix4x4 invTrans = worldTransform.inverse;
            GL.modelview = GL.modelview * invTrans;

            view.edges.ForEach(edge =>
                {
                    if (EdgeIsInThisDrawer(edge) && edge.layer < element.layer)
                    {
                        GL.PushMatrix();
                        Matrix4x4 trans = edge.edgeControl.worldTransform;
                        GL.modelview = GL.modelview * trans;
                        edge.edgeControl.DoRepaint();
                        GL.PopMatrix();
                    }
                });

            GL.PopMatrix();
        }
    }
}
