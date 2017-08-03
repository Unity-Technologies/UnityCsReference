// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.Debugger
{
    class PanelDebug : BasePanelDebug
    {
        internal uint highlightedElement;
        private List<RepaintData> m_RepaintDatas = new List<RepaintData>();

        internal override bool RecordRepaint(VisualElement visualElement)
        {
            if (!enabled) return false;
            m_RepaintDatas.Add(new RepaintData(visualElement.controlid,
                    visualElement.worldBound,
                    Color.HSVToRGB(visualElement.controlid * 11 % 32 / 32.0f, .6f, 1.0f)));
            return true;
        }

        internal override bool EndRepaint()
        {
            if (!enabled) return false;

            RepaintData onTopElement = null;
            foreach (var repaintData in m_RepaintDatas)
            {
                var color = repaintData.color;
                if (highlightedElement != 0)
                    if (highlightedElement != repaintData.controlId)
                    {
                        color = Color.gray;
                    }
                    else
                    {
                        onTopElement = repaintData;
                        continue;
                    }
                PickingData.DrawRect(repaintData.contentRect, color);
            }
            m_RepaintDatas.Clear();
            if (onTopElement != null)
                PickingData.DrawRect(onTopElement.contentRect, onTopElement.color);

            return true;
        }

        public class RepaintData
        {
            public readonly Color color;
            public readonly Rect contentRect;
            public readonly uint controlId;

            public RepaintData(uint controlId, Rect contentRect, Color color)
            {
                this.contentRect = contentRect;
                this.color = color;
                this.controlId = controlId;
            }
        }
    }
}
