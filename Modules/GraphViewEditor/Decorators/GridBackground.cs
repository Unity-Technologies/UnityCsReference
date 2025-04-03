// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    public class GridBackground : ImmediateModeElement
    {
        static CustomStyleProperty<float> s_SpacingProperty = new CustomStyleProperty<float>("--spacing");
        static CustomStyleProperty<int> s_ThickLinesProperty = new CustomStyleProperty<int>("--thick-lines");
        static CustomStyleProperty<Color> s_LineColorProperty = new CustomStyleProperty<Color>("--line-color");
        static CustomStyleProperty<Color> s_ThickLineColorProperty = new CustomStyleProperty<Color>("--thick-line-color");
        static CustomStyleProperty<Color> s_GridBackgroundColorProperty = new CustomStyleProperty<Color>("--grid-background-color");

        static readonly float s_DefaultSpacing = 50f;
        static readonly int s_DefaultThickLines = 10;
        static readonly Color s_DefaultLineColor = new Color(0f, 0f, 0f, 0.18f);
        static readonly Color s_DefaultThickLineColor = new Color(0f, 0f, 0f, 0.38f);
        static readonly Color s_DefaultGridBackgroundColor = new Color(0.17f, 0.17f, 0.17f, 1.0f);

        float m_Spacing = s_DefaultSpacing;
        private float spacing => m_Spacing;

        int m_ThickLines = s_DefaultThickLines;
        private int thickLines => m_ThickLines;

        Color m_LineColor = s_DefaultLineColor;
        private Color lineColor => m_LineColor * playModeTintColor;

        Color m_ThickLineColor = s_DefaultThickLineColor;
        private Color thickLineColor => m_ThickLineColor * playModeTintColor;

        Color m_GridBackgroundColor = s_DefaultGridBackgroundColor;
        private Color gridBackgroundColor => m_GridBackgroundColor * playModeTintColor;

        private VisualElement m_Container;

        public GridBackground()
        {
            pickingMode = PickingMode.Ignore;

            this.StretchToParentSize();

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        private Vector3 Clip(Rect clipRect, Vector3 _in)
        {
            if (_in.x < clipRect.xMin)
                _in.x = clipRect.xMin;
            if (_in.x > clipRect.xMax)
                _in.x = clipRect.xMax;

            if (_in.y < clipRect.yMin)
                _in.y = clipRect.yMin;
            if (_in.y > clipRect.yMax)
                _in.y = clipRect.yMax;

            return _in;
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            float spacingValue = 0f;
            int thicklinesValue = 0;
            Color thicklineColorValue = Color.clear;
            Color lineColorValue = Color.clear;
            Color gridColorValue = Color.clear;

            ICustomStyle customStyle = e.customStyle;
            if (customStyle.TryGetValue(s_SpacingProperty, out spacingValue))
                m_Spacing = spacingValue;

            if (customStyle.TryGetValue(s_ThickLinesProperty, out thicklinesValue))
                m_ThickLines = thickLines;

            if (customStyle.TryGetValue(s_ThickLineColorProperty, out thicklineColorValue))
                m_ThickLineColor = thicklineColorValue;

            if (customStyle.TryGetValue(s_LineColorProperty, out lineColorValue))
                m_LineColor = lineColorValue;

            if (customStyle.TryGetValue(s_GridBackgroundColorProperty, out gridColorValue))
                m_GridBackgroundColor = gridColorValue;
        }

        protected override void ImmediateRepaint()
        {
            VisualElement target = parent;

            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("GridBackground can only be added to a GraphView");
            }
            m_Container = graphView.contentViewContainer;
            Rect clientRect = graphView.layout;

            // Since we're always stretch to parent size, we will use (0,0) as (x,y) coordinates
            clientRect.x = 0;
            clientRect.y = 0;

#pragma warning disable CS0618 // Type or member is obsolete
            var containerScale = new Vector3(m_Container.transform.matrix.GetColumn(0).magnitude,
                m_Container.transform.matrix.GetColumn(1).magnitude,
                m_Container.transform.matrix.GetColumn(2).magnitude);
            var containerTranslation = m_Container.transform.matrix.GetColumn(3);
#pragma warning restore CS0618 // Type or member is obsolete
            var containerPosition = m_Container.layout;

            // background
            HandleUtility.ApplyWireMaterial();

            GL.Begin(GL.QUADS);
            GL.Color(gridBackgroundColor);
            GL.Vertex(new Vector3(clientRect.x, clientRect.y));
            GL.Vertex(new Vector3(clientRect.xMax, clientRect.y));
            GL.Vertex(new Vector3(clientRect.xMax, clientRect.yMax));
            GL.Vertex(new Vector3(clientRect.x, clientRect.yMax));
            GL.End();

            // vertical lines
            Vector3 from = new Vector3(clientRect.x, clientRect.y, 0.0f);
            Vector3 to = new Vector3(clientRect.x, clientRect.height, 0.0f);

            var tx = Matrix4x4.TRS(containerTranslation, Quaternion.identity, Vector3.one);

            from = tx.MultiplyPoint(from);
            to = tx.MultiplyPoint(to);

            from.x += (containerPosition.x * containerScale.x);
            from.y += (containerPosition.y * containerScale.y);
            to.x += (containerPosition.x * containerScale.x);
            to.y += (containerPosition.y * containerScale.y);

            float thickGridLineX = from.x;
            float thickGridLineY = from.y;

            // Update from/to to start at beginning of clientRect
            from.x = (from.x % (spacing * (containerScale.x)) - (spacing * (containerScale.x)));
            to.x = from.x;

            from.y = clientRect.y;
            to.y = clientRect.y + clientRect.height;

            while (from.x < clientRect.width)
            {
                from.x += spacing * containerScale.x;
                to.x += spacing * containerScale.x;

                GL.Begin(GL.LINES);
                GL.Color(lineColor);
                GL.Vertex(Clip(clientRect, from));
                GL.Vertex(Clip(clientRect, to));
                GL.End();
            }

            float thickLineSpacing = (spacing * thickLines);
            from.x = to.x = (thickGridLineX % (thickLineSpacing * (containerScale.x)) - (thickLineSpacing * (containerScale.x)));

            while (from.x < clientRect.width + thickLineSpacing)
            {
                GL.Begin(GL.LINES);
                GL.Color(thickLineColor);
                GL.Vertex(Clip(clientRect, from));
                GL.Vertex(Clip(clientRect, to));
                GL.End();

                from.x += (spacing * containerScale.x * thickLines);
                to.x += (spacing * containerScale.x * thickLines);
            }

            // horizontal lines
            from = new Vector3(clientRect.x, clientRect.y, 0.0f);
            to = new Vector3(clientRect.x + clientRect.width, clientRect.y, 0.0f);

            from.x += (containerPosition.x * containerScale.x);
            from.y += (containerPosition.y * containerScale.y);
            to.x += (containerPosition.x * containerScale.x);
            to.y += (containerPosition.y * containerScale.y);

            from = tx.MultiplyPoint(from);
            to = tx.MultiplyPoint(to);

            from.y = to.y = (from.y % (spacing * (containerScale.y)) - (spacing * (containerScale.y)));
            from.x = clientRect.x;
            to.x = clientRect.width;

            while (from.y < clientRect.height)
            {
                from.y += spacing * containerScale.y;
                to.y += spacing * containerScale.y;

                GL.Begin(GL.LINES);
                GL.Color(lineColor);
                GL.Vertex(Clip(clientRect, from));
                GL.Vertex(Clip(clientRect, to));
                GL.End();
            }

            thickLineSpacing = spacing * thickLines;
            from.y = to.y = (thickGridLineY % (thickLineSpacing * (containerScale.y)) - (thickLineSpacing * (containerScale.y)));

            while (from.y < clientRect.height + thickLineSpacing)
            {
                GL.Begin(GL.LINES);
                GL.Color(thickLineColor);
                GL.Vertex(Clip(clientRect, from));
                GL.Vertex(Clip(clientRect, to));
                GL.End();

                from.y += spacing * containerScale.y * thickLines;
                to.y += spacing * containerScale.y * thickLines;
            }
        }
    }
}
