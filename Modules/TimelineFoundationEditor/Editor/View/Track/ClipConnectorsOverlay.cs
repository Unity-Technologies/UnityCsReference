// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    partial class ClipConnectorsOverlay : CanvasElement
    {
        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new ClipConnectorsOverlay();
        }

        const string k_Style = "clipConnectorsOverlay";

        static readonly StylesheetResource k_Stylesheet = Internals.UIResources.StylesheetFactory.Get<ClipConnectorsOverlay>();
        static readonly CustomStyleProperty<int> k_TriangleSize = new CustomStyleProperty<int>("--connector-size");
        static readonly CustomStyleProperty<Color> k_TriangleColor = new CustomStyleProperty<Color>("--connector-color");

        Track m_Track;
        CanvasTransform m_CanvasTransform;

        public ClipConnectorsOverlay()
        {
            pickingMode = PickingMode.Ignore;
            focusable = false;
            generateVisualContent += GenerateVisualContent;

            this.AddToTimelineClassList(k_Style);
            k_Stylesheet.ApplyTo(this);
        }

        public void Initialize(Track track)
        {
            m_Track = track;
        }

        public override void PositionInCanvas(CanvasTransform canvasTransform)
        {
            m_CanvasTransform = canvasTransform;
            MarkDirtyRepaint();
        }

        void GenerateVisualContent(MeshGenerationContext obj)
        {
            if (m_Track == null)
                return;

            customStyle.TryGetValue(k_TriangleColor, out Color triangleColor);
            customStyle.TryGetValue(k_TriangleSize, out int triangleSize);

            foreach (Item item in m_Track.Items)
            {
                if (!item.isClip || !item.Next().isClip || !m_CanvasTransform.displayRange.Intersects(item.end))
                    continue;
                var timePos = new Vector2(m_CanvasTransform.TimeToPixel(item.end), layout.y);
                obj.DrawTriangle(
                    timePos - (Vector2.right * triangleSize),
                    timePos - (Vector2.left * triangleSize),
                    timePos + (Vector2.up * triangleSize),
                    triangleColor);
            }
        }
    }
}
