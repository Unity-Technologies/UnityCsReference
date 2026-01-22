// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    internal abstract class ChartWidget
    {
        readonly ChartModel m_Model;
        readonly VisualElement m_Root;

        public ChartWidget(ChartModel model, VisualElement root)
        {
            var groupElem = new VisualElement();
            groupElem.name = GetType().Name;
            groupElem.StretchToParentSize();
            root.Add(groupElem);

            m_Model = model;
            m_Root = groupElem;

            root.generateVisualContent += UpdateGeometry;
        }

        protected VisualElement Root => m_Root;
        protected ChartModel Model => m_Model;

        protected abstract void UpdateGeometry(MeshGenerationContext mgc);
    }
}
